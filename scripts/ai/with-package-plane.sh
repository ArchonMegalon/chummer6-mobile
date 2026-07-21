#!/usr/bin/env bash
set -euo pipefail

if [[ $# -eq 0 ]]; then
  echo "usage: $0 <dotnet-args...>" >&2
  exit 1
fi

export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-/tmp/dotnet-chummer-play}"
export HOME="${HOME:-/tmp}"
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
local_feed="${CHUMMER_PACKAGE_PLANE_LOCAL_FEED:-${repo_root}/.artifacts/nuget-local}"
export NUGET_PACKAGES="${NUGET_PACKAGES:-${repo_root}/.artifacts/nuget-packages}"
package_plane_lock_file="${CHUMMER_PACKAGE_PLANE_LOCK_FILE:-${repo_root}/.artifacts/package-plane/with-package-plane.lock}"
package_plane_lock_timeout_seconds="${CHUMMER_PACKAGE_PLANE_LOCK_TIMEOUT_SECONDS:-600}"
published_feed_sources="${CHUMMER_PUBLISHED_FEED_SOURCES:-}"
published_engine_contracts_version="${CHUMMER_PUBLISHED_ENGINE_CONTRACTS_VERSION:-}"
published_campaign_contracts_version="${CHUMMER_PUBLISHED_CAMPAIGN_CONTRACTS_VERSION:-}"
published_control_contracts_version="${CHUMMER_PUBLISHED_CONTROL_CONTRACTS_VERSION:-}"
published_play_contracts_version="${CHUMMER_PUBLISHED_PLAY_CONTRACTS_VERSION:-}"
published_ui_kit_version="${CHUMMER_PUBLISHED_UI_KIT_VERSION:-}"
verification_mode="${CHUMMER_VERIFY_MODE:-slice}"
allow_stub_packages="${CHUMMER_ALLOW_STUB_PACKAGES:-}"
verification_run_id="${CHUMMER_VERIFY_RUN_ID:-}"
workspace_root="$(cd "${repo_root}/.." && pwd)"
engine_contracts_project="${CHUMMER_PACKAGE_PLANE_ENGINE_CONTRACTS_PROJECT:-${workspace_root}/chummer-core-engine/Chummer.Contracts/Chummer.Contracts.csproj}"
campaign_contracts_project="${CHUMMER_PACKAGE_PLANE_CAMPAIGN_CONTRACTS_PROJECT:-${workspace_root}/chummer.run-services/Chummer.Campaign.Contracts/Chummer.Campaign.Contracts.csproj}"
control_contracts_project="${CHUMMER_PACKAGE_PLANE_CONTROL_CONTRACTS_PROJECT:-${workspace_root}/chummer.run-services/Chummer.Control.Contracts/Chummer.Control.Contracts.csproj}"
play_contracts_project="${CHUMMER_PACKAGE_PLANE_PLAY_CONTRACTS_PROJECT:-${workspace_root}/chummer.run-services/Chummer.Play.Contracts/Chummer.Play.Contracts.csproj}"
ui_kit_project="${CHUMMER_PACKAGE_PLANE_UI_KIT_PROJECT:-${workspace_root}/chummer-ui-kit/src/Chummer.Ui.Kit/Chummer.Ui.Kit.csproj}"

case "${verification_mode}" in
  scaffold|slice|integration|release) ;;
  *)
    echo "unsupported CHUMMER_VERIFY_MODE: ${verification_mode}" >&2
    exit 2
    ;;
esac

if [[ -z "${allow_stub_packages}" ]]; then
  case "${verification_mode}" in
    scaffold|slice) allow_stub_packages=1 ;;
    integration|release) allow_stub_packages=0 ;;
  esac
fi
case "${allow_stub_packages}" in
  0|1) ;;
  *)
    echo "CHUMMER_ALLOW_STUB_PACKAGES must be 0 or 1" >&2
    exit 2
    ;;
esac
if [[ "${verification_mode}" == "integration" || "${verification_mode}" == "release" ]]; then
  if [[ "${allow_stub_packages}" != "0" ]]; then
    echo "${verification_mode} verification forbids stub packages" >&2
    exit 2
  fi
fi
export CHUMMER_VERIFY_MODE="${verification_mode}"
export CHUMMER_ALLOW_STUB_PACKAGES="${allow_stub_packages}"

strict_verification_mode=false
if [[ "${verification_mode}" == "integration" || "${verification_mode}" == "release" ]]; then
  strict_verification_mode=true
  if [[ -z "${verification_run_id}" ]]; then
    echo "${verification_mode} package-plane verification requires CHUMMER_VERIFY_RUN_ID" >&2
    exit 2
  fi
fi

mkdir -p "$(dirname "${package_plane_lock_file}")"
package_plane_attestation_file="${CHUMMER_PACKAGE_PLANE_ATTESTATION_FILE:-$(dirname "${package_plane_lock_file}")/restore-attestation.v1}"
package_plane_source_attestation_file="${package_plane_attestation_file}.source"
# Keep the shared package cache locked through the dotnet process and attestation write.
exec 9>"${package_plane_lock_file}"
if ! flock -w "${package_plane_lock_timeout_seconds}" 9; then
  echo "timed out waiting for package-plane lock: ${package_plane_lock_file}" >&2
  exit 1
fi

restore_args=()
skip_package_refresh=false
stub_packages_used=false

for arg in "$@"; do
  case "${arg}" in
    --no-restore|--no-build)
      skip_package_refresh=true
      ;;
  esac
done

dotnet_command="${1:-}"
attestation_issuing_command=false
case "${dotnet_command}" in
  restore|build) attestation_issuing_command=true ;;
esac
target_project_arg=""
case "${dotnet_command}" in
  restore|build|test|pack|publish)
    if [[ "${2:-}" == *.csproj ]]; then
      target_project_arg="${2}"
    fi
    ;;
  run)
    previous_arg=""
    for arg in "$@"; do
      if [[ "${previous_arg}" == "--project" && "${arg}" == *.csproj ]]; then
        target_project_arg="${arg}"
        break
      fi
      previous_arg="${arg}"
    done
    ;;
esac

target_project=""
target_project_sha256=""
target_assets_file=""
target_attestation_file=""
if [[ -n "${target_project_arg}" ]]; then
  if [[ "${target_project_arg}" == /* ]]; then
    target_project="$(realpath -m "${target_project_arg}")"
  else
    target_project="$(realpath -m "${repo_root}/${target_project_arg}")"
  fi
  target_project_sha256="$(printf '%s' "${target_project}" | sha256sum | awk '{print $1}')"
  target_assets_file="$(dirname "${target_project}")/obj/project.assets.json"
  target_attestation_file="${package_plane_attestation_file}.${target_project_sha256}"
fi

package_plane_source_kind="owner"
if [[ -n "${published_feed_sources}" ]]; then
  package_plane_source_kind="published"
fi

package_plane_configuration_sha256() {
  printf '%s\0' \
    "${verification_mode}" \
    "${allow_stub_packages}" \
    "${package_plane_source_kind}" \
    "${published_feed_sources}" \
    "${published_engine_contracts_version}" \
    "${published_campaign_contracts_version}" \
    "${published_control_contracts_version}" \
    "${published_play_contracts_version}" \
    "${published_ui_kit_version}" \
    | sha256sum | awk '{print $1}'
}

local_package_inventory_sha256() {
  if [[ "${package_plane_source_kind}" == "published" ]]; then
    printf '%s\n' "published-feed"
    return 0
  fi

  local packages=()
  local package
  local inventory=""
  local digest
  shopt -s nullglob
  packages+=(
    "${local_feed}"/Chummer.Engine.Contracts.*.nupkg
    "${local_feed}"/Chummer.Campaign.Contracts.*.nupkg
    "${local_feed}"/Chummer.Control.Contracts.*.nupkg
    "${local_feed}"/Chummer.Play.Contracts.*.nupkg
    "${local_feed}"/Chummer.Ui.Kit.*.nupkg
  )
  shopt -u nullglob
  if [[ "${#packages[@]}" -ne 5 ]]; then
    printf '%s\n' "missing"
    return 0
  fi
  for package in "${packages[@]}"; do
    digest="$(sha256sum "${package}" | awk '{print $1}')"
    inventory+="$(basename "${package}"):${digest}"$'\n'
  done
  printf '%s' "${inventory}" | LC_ALL=C sort | sha256sum | awk '{print $1}'
}

target_assets_sha256() {
  if [[ -z "${target_assets_file}" || ! -f "${target_assets_file}" ]]; then
    printf '%s\n' "missing"
    return 0
  fi
  sha256sum "${target_assets_file}" | awk '{print $1}'
}

package_plane_source_attestation_payload() {
  printf '%s\n' \
    "contract=chummer6-mobile.package-plane-source-attestation/v1" \
    "verification_run_id=${verification_run_id}" \
    "verification_mode=${verification_mode}" \
    "source_kind=${package_plane_source_kind}" \
    "configuration_sha256=$(package_plane_configuration_sha256)" \
    "stub_packages_used=${stub_packages_used}" \
    "package_inventory_sha256=$(local_package_inventory_sha256)"
}

package_plane_target_attestation_payload() {
  printf '%s\n' \
    "contract=chummer6-mobile.package-plane-target-attestation/v1" \
    "verification_run_id=${verification_run_id}" \
    "verification_mode=${verification_mode}" \
    "source_kind=${package_plane_source_kind}" \
    "target_project_sha256=${target_project_sha256}" \
    "configuration_sha256=$(package_plane_configuration_sha256)" \
    "stub_packages_used=${stub_packages_used}" \
    "project_assets_sha256=$(target_assets_sha256)"
}

if [[ "${skip_package_refresh}" == true && "${strict_verification_mode}" == true ]]; then
  if [[ -z "${target_project}" || "$(target_assets_sha256)" == "missing" ]] \
    || [[ ! -f "${package_plane_source_attestation_file}" ]] \
    || ! cmp -s "${package_plane_source_attestation_file}" <(package_plane_source_attestation_payload) \
    || [[ ! -f "${target_attestation_file}" ]] \
    || ! cmp -s "${target_attestation_file}" <(package_plane_target_attestation_payload); then
    echo "${verification_mode} --no-restore/--no-build invocation lacks matching same-run no-stub package-plane provenance" >&2
    exit 1
  fi
elif [[ "${skip_package_refresh}" == false ]]; then
  rm -f "${package_plane_source_attestation_file}"
  if [[ -n "${target_attestation_file}" ]]; then
    rm -f "${target_attestation_file}"
  fi
fi

pack_owner_package() {
  local project_path="$1"
  local package_id="$2"
  local package_version="$3"
  local owner_restore_sources="${local_feed}"
  local owner_lock_file="${local_feed}/.locks/${package_id}.packages.lock.json"

  if [[ ! -f "${project_path}" ]]; then
    return 1
  fi

  # The Core contracts project can require SDK/runtime packs while producing
  # the first package in an otherwise private feed. Every downstream owner
  # contract must restore only from that feed so an ambient cache or a public
  # package with a colliding Chummer id cannot satisfy the package plane.
  if [[ "${package_id}" == "Chummer.Engine.Contracts" ]]; then
    owner_restore_sources+=";https://api.nuget.org/v3/index.json"
  fi
  mkdir -p "$(dirname "${owner_lock_file}")"

  dotnet pack "${project_path}" \
    --nologo \
    -c Release \
    -o "${local_feed}" \
    -p:RestoreSources="${owner_restore_sources}" \
    -p:RestoreIgnoreFailedSources=false \
    -p:RestorePackagesWithLockFile=true \
    -p:RestoreLockedMode=false \
    -p:NuGetLockFilePath="${owner_lock_file}" \
    -p:ChummerEngineContractsPackageVersion="${published_engine_contracts_version:-0.1.0-preview}" \
    -p:ChummerCampaignContractsPackageVersion="${published_campaign_contracts_version:-0.1.0-preview}" \
    -p:ChummerControlContractsPackageVersion="${published_control_contracts_version:-0.1.0-preview}" \
    -p:ChummerPlayContractsPackageVersion="${published_play_contracts_version:-0.1.0-preview}" \
    -p:ChummerUiKitPackageVersion="${published_ui_kit_version:-0.1.0-preview}" \
    -p:PackageId="${package_id}" \
    -p:PackageVersion="${package_version}"
}

pack_stub_package() {
  local project_path="$1"
  local package_id="$2"
  local package_version="$3"

  stub_packages_used=true

  dotnet pack "${project_path}" \
    --nologo \
    -c Release \
    -o "${local_feed}" \
    -p:PackageId="${package_id}" \
    -p:PackageVersion="${package_version}" \
    -p:AssemblyName="${package_id}" \
    -p:RootNamespace="${package_id}" >/dev/null
}

pack_owner_or_allowed_stub() {
  local owner_project_path="$1"
  local stub_project_path="$2"
  local package_id="$3"
  local package_version="$4"

  if pack_owner_package "${owner_project_path}" "${package_id}" "${package_version}"; then
    return 0
  fi
  if [[ "${allow_stub_packages}" == "1" ]]; then
    pack_stub_package "${stub_project_path}" "${package_id}" "${package_version}"
    return 0
  fi
  echo "${verification_mode} verification requires owner or published package ${package_id}; stub fallback is disabled" >&2
  return 1
}

if [[ -n "${published_feed_sources}" ]]; then
  restore_args+=(-p:RestoreSources="${published_feed_sources}" -p:RestoreIgnoreFailedSources=false)
else
  mkdir -p "${local_feed}"
  if [[ "${skip_package_refresh}" != true ]]; then
    rm -f "${local_feed}"/Chummer.Engine.Contracts.*.nupkg "${local_feed}"/Chummer.Campaign.Contracts.*.nupkg "${local_feed}"/Chummer.Control.Contracts.*.nupkg "${local_feed}"/Chummer.Play.Contracts.*.nupkg "${local_feed}"/Chummer.Ui.Kit.*.nupkg
    rm -rf "${NUGET_PACKAGES}/chummer.engine.contracts" "${NUGET_PACKAGES}/chummer.campaign.contracts" "${NUGET_PACKAGES}/chummer.control.contracts" "${NUGET_PACKAGES}/chummer.play.contracts" "${NUGET_PACKAGES}/chummer.ui.kit"
    find "${repo_root}/src" -path "*/obj/project.assets.json" -delete
    find "${repo_root}/src" -path "*/obj/project.nuget.cache" -delete
    find "${repo_root}/eng/package-stubs" -path "*/obj/project.assets.json" -delete
    find "${repo_root}/eng/package-stubs" -path "*/obj/project.nuget.cache" -delete
    pack_owner_or_allowed_stub "${engine_contracts_project}" "${repo_root}/eng/package-stubs/EngineContractsStub/EngineContractsStub.csproj" "Chummer.Engine.Contracts" "${published_engine_contracts_version:-0.1.0-preview}"
    pack_owner_or_allowed_stub "${campaign_contracts_project}" "${repo_root}/eng/package-stubs/CampaignContractsStub/CampaignContractsStub.csproj" "Chummer.Campaign.Contracts" "${published_campaign_contracts_version:-0.1.0-preview}"
    pack_owner_or_allowed_stub "${control_contracts_project}" "${repo_root}/eng/package-stubs/ControlContractsStub/ControlContractsStub.csproj" "Chummer.Control.Contracts" "${published_control_contracts_version:-0.1.0-preview}"
    pack_owner_or_allowed_stub "${play_contracts_project}" "${repo_root}/eng/package-stubs/PlayContractsStub/PlayContractsStub.csproj" "Chummer.Play.Contracts" "${published_play_contracts_version:-0.1.0-preview}"
    pack_owner_or_allowed_stub "${ui_kit_project}" "${repo_root}/eng/package-stubs/UiKitStub/UiKitStub.csproj" "Chummer.Ui.Kit" "${published_ui_kit_version:-0.1.0-preview}"
    if [[ "${strict_verification_mode}" == true && "$(local_package_inventory_sha256)" == "missing" ]]; then
      echo "${verification_mode} verification did not produce the exact five-package owner inventory" >&2
      exit 1
    fi
  fi
  restore_args+=(-p:RestoreSources="${local_feed}" -p:RestoreIgnoreFailedSources=true)
fi

if [[ -n "${published_engine_contracts_version}" ]]; then
  restore_args+=(-p:ChummerEngineContractsPackageVersion="${published_engine_contracts_version}")
fi

if [[ -n "${published_campaign_contracts_version}" ]]; then
  restore_args+=(-p:ChummerCampaignContractsPackageVersion="${published_campaign_contracts_version}")
fi

if [[ -n "${published_control_contracts_version}" ]]; then
  restore_args+=(-p:ChummerControlContractsPackageVersion="${published_control_contracts_version}")
fi

if [[ -n "${published_play_contracts_version}" ]]; then
  restore_args+=(-p:ChummerPlayContractsPackageVersion="${published_play_contracts_version}")
fi

if [[ -n "${published_ui_kit_version}" ]]; then
  restore_args+=(-p:ChummerUiKitPackageVersion="${published_ui_kit_version}")
fi

if dotnet "$@" "${restore_args[@]}"; then
  if [[ "${skip_package_refresh}" == false && "${strict_verification_mode}" == true && "${attestation_issuing_command}" == true && -n "${target_project}" ]]; then
    if [[ "$(target_assets_sha256)" == "missing" ]]; then
      echo "package-plane restore-capable command completed without target project.assets.json: ${target_project_arg}" >&2
      exit 1
    fi
    attestation_tmp="${target_attestation_file}.tmp.$$"
    source_attestation_tmp="${package_plane_source_attestation_file}.tmp.$$"
    mkdir -p "$(dirname "${target_attestation_file}")"
    (umask 077 && package_plane_source_attestation_payload >"${source_attestation_tmp}")
    mv -f "${source_attestation_tmp}" "${package_plane_source_attestation_file}"
    (umask 077 && package_plane_target_attestation_payload >"${attestation_tmp}")
    mv -f "${attestation_tmp}" "${target_attestation_file}"
  fi
  exit 0
else
  exit_code=$?
  exit "${exit_code}"
fi
