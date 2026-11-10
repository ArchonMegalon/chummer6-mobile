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
# Owner builds are an explicit development input, never sibling discovery.
engine_contracts_project="${CHUMMER_PACKAGE_PLANE_ENGINE_CONTRACTS_PROJECT:-}"
campaign_contracts_project="${CHUMMER_PACKAGE_PLANE_CAMPAIGN_CONTRACTS_PROJECT:-}"
control_contracts_project="${CHUMMER_PACKAGE_PLANE_CONTROL_CONTRACTS_PROJECT:-}"
play_contracts_project="${CHUMMER_PACKAGE_PLANE_PLAY_CONTRACTS_PROJECT:-}"
ui_kit_project="${CHUMMER_PACKAGE_PLANE_UI_KIT_PROJECT:-}"

case "${verification_mode}" in
  scaffold|slice|integration|release) ;;
  *)
    echo "unsupported CHUMMER_VERIFY_MODE: ${verification_mode}" >&2
    exit 2
    ;;
esac

if [[ -z "${allow_stub_packages}" ]]; then
  case "${verification_mode}" in
    scaffold) allow_stub_packages=1 ;;
    slice|integration|release) allow_stub_packages=0 ;;
  esac
fi
case "${allow_stub_packages}" in
  0|1) ;;
  *)
    echo "CHUMMER_ALLOW_STUB_PACKAGES must be 0 or 1" >&2
    exit 2
    ;;
esac
# verify.sh historically exports 1 for slice. That permission cannot make
# empty scaffold assemblies into product dependencies, even when inherited.
if [[ "${verification_mode}" == "slice" ]]; then
  allow_stub_packages=0
fi
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

source_bootstrap="${repo_root}/scripts/ai/bootstrap-owner-contracts-feed.py"
source_lock="${repo_root}/eng/package-plane.lock.json"
source_lock_sha256=""
source_restore_config=""
package_plane_source_kind="owner"
if [[ -n "${published_feed_sources}" ]]; then
  package_plane_source_kind="published"
fi

# Only the zero-input local slice chooses the public source lock automatically.
# Partial explicit owner inputs and external certification modes still fail closed.
if [[ "${verification_mode}" == "slice" && -z "${published_feed_sources}" \
   && -z "${engine_contracts_project}${campaign_contracts_project}${control_contracts_project}${play_contracts_project}${ui_kit_project}" \
   && -f "${source_bootstrap}" && -f "${source_lock}" ]]; then
  package_plane_source_kind="locked-public-source"
  source_lock_sha256="$(sha256sum "${source_lock}" | awk '{print $1}')"
  locked_version="$(python3 "${source_bootstrap}" --repo-root "${repo_root}" --print-version)"
  for version in "${published_engine_contracts_version}" "${published_campaign_contracts_version}" "${published_control_contracts_version}" "${published_play_contracts_version}" "${published_ui_kit_version}"; do
    if [[ -n "${version}" && "${version}" != "${locked_version}" ]]; then
      echo "public source bootstrap forbids version overrides that differ from eng/package-plane.lock.json" >&2
      exit 1
    fi
  done
  local_feed="${local_feed}/source-${source_lock_sha256}"
  source_restore_config="$(dirname "${package_plane_lock_file}")/source-${source_lock_sha256}.NuGet.Config"
fi
# Explicit development owners use the same consumer defaults; published-feed
# overrides remain explicit. The source lock validates these pins before builds.
default_package_version="$(python3 - "${repo_root}/Directory.Packages.props" <<'PYVERSION'
import sys, xml.etree.ElementTree as ET
print(ET.parse(sys.argv[1]).getroot().findtext('./PropertyGroup/ChummerEngineContractsPackageVersion'))
PYVERSION
)"

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
    "${source_lock_sha256}" \
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

require_package_sources() {
  if [[ "${package_plane_source_kind}" == "locked-public-source" ]]; then
    local action=--ensure
    local -a cache_args=()
    if [[ "${skip_package_refresh}" == true ]]; then
      action=--validate-only
    else
      cache_args+=(--repair-cache)
    fi
    python3 "${source_bootstrap}" --repo-root "${repo_root}" --feed "${local_feed}" "${action}"
    python3 "${source_bootstrap}" --repo-root "${repo_root}" --feed "${local_feed}" \
      --check-cache "${NUGET_PACKAGES}" "${cache_args[@]}"
    python3 "${source_bootstrap}" --repo-root "${repo_root}" --feed "${local_feed}" \
      --write-restore-config "${source_restore_config}"
    return 0
  fi
  if [[ -n "${published_feed_sources}" || "${allow_stub_packages}" == "1" ]]; then
    return 0
  fi

  local -a projects=("${engine_contracts_project}" "${campaign_contracts_project}" "${control_contracts_project}" "${play_contracts_project}" "${ui_kit_project}")
  local -a package_ids=(Chummer.Engine.Contracts Chummer.Campaign.Contracts Chummer.Control.Contracts Chummer.Play.Contracts Chummer.Ui.Kit)
  local -a versions=("${published_engine_contracts_version:-${default_package_version}}" "${published_campaign_contracts_version:-${default_package_version}}" "${published_control_contracts_version:-${default_package_version}}" "${published_play_contracts_version:-${default_package_version}}" "${published_ui_kit_version:-${default_package_version}}")
  local missing=false
  local index
  for index in "${!package_ids[@]}"; do
    if [[ ! -f "${projects[index]}" ]]; then
      echo "${verification_mode} verification requires owner or published package ${package_ids[index]} ${versions[index]}; stub fallback is disabled" >&2
      missing=true
    fi
  done
  if [[ "${missing}" == true ]]; then
    echo "No complete package source is configured. Set CHUMMER_PUBLISHED_FEED_SOURCES to an approved feed containing the exact pins, or supply all explicit CHUMMER_PACKAGE_PLANE_*_PROJECT inputs for an owner build. See docs/PACKAGE_BOOTSTRAP.md." >&2
    return 1
  fi
}

# Cached scaffolds cannot substitute for a configured source in the default
# mode. Check even when restore is skipped, before touching cache/provenance.
if [[ "${verification_mode}" == "slice" && "${skip_package_refresh}" == true ]]; then
  require_package_sources
fi
if [[ "${skip_package_refresh}" == true && ( "${strict_verification_mode}" == true || "${package_plane_source_kind}" == "locked-public-source" ) ]]; then
  if [[ -z "${target_project}" || "$(target_assets_sha256)" == "missing" ]] \
    || [[ ! -f "${package_plane_source_attestation_file}" ]] \
    || ! cmp -s "${package_plane_source_attestation_file}" <(package_plane_source_attestation_payload) \
    || [[ ! -f "${target_attestation_file}" ]] \
    || ! cmp -s "${target_attestation_file}" <(package_plane_target_attestation_payload); then
    echo "${verification_mode} --no-restore/--no-build invocation lacks matching same-run no-stub package-plane provenance" >&2
    exit 1
  fi
elif [[ "${skip_package_refresh}" == false ]]; then
  require_package_sources
  rm -f "${package_plane_source_attestation_file}"
  if [[ -n "${target_attestation_file}" ]]; then
    rm -f "${target_attestation_file}"
  fi
fi

pack_owner_package() {
  local project_path="$1"
  local package_id="$2"
  local package_version="$3"
  local -a package_id_args=()

  if [[ ! -f "${project_path}" ]]; then
    return 1
  fi
  if ! rg -q '<PackageId>[^<]+</PackageId>' "${project_path}"; then
    package_id_args+=("-p:PackageId=${package_id}")
  fi

  dotnet pack "${project_path}" \
    --nologo \
    -c Release \
    -o "${local_feed}" \
    "${package_id_args[@]}" \
    -p:PackageVersion="${package_version}" 9>&- >/dev/null
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
    -p:RootNamespace="${package_id}" 9>&- >/dev/null
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

if [[ "${package_plane_source_kind}" == "locked-public-source" ]]; then
  restore_args+=(-p:RestoreConfigFile="${source_restore_config}" \
    -p:RestoreSources="${local_feed};https://api.nuget.org/v3/index.json" \
    -p:RestoreAdditionalProjectSources= -p:RestoreFallbackFolders= -p:RestoreIgnoreFailedSources=false)
elif [[ -n "${published_feed_sources}" ]]; then
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
    pack_owner_or_allowed_stub "${engine_contracts_project}" "${repo_root}/eng/package-stubs/EngineContractsStub/EngineContractsStub.csproj" "Chummer.Engine.Contracts" "${published_engine_contracts_version:-${default_package_version}}"
    pack_owner_or_allowed_stub "${campaign_contracts_project}" "${repo_root}/eng/package-stubs/CampaignContractsStub/CampaignContractsStub.csproj" "Chummer.Campaign.Contracts" "${published_campaign_contracts_version:-${default_package_version}}"
    pack_owner_or_allowed_stub "${control_contracts_project}" "${repo_root}/eng/package-stubs/ControlContractsStub/ControlContractsStub.csproj" "Chummer.Control.Contracts" "${published_control_contracts_version:-${default_package_version}}"
    pack_owner_or_allowed_stub "${play_contracts_project}" "${repo_root}/eng/package-stubs/PlayContractsStub/PlayContractsStub.csproj" "Chummer.Play.Contracts" "${published_play_contracts_version:-${default_package_version}}"
    pack_owner_or_allowed_stub "${ui_kit_project}" "${repo_root}/eng/package-stubs/UiKitStub/UiKitStub.csproj" "Chummer.Ui.Kit" "${published_ui_kit_version:-${default_package_version}}"
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

# The wrapper owns the cache lock until the command and attestation finish.
# Build-server children must not retain it after this invocation returns.
if dotnet "$@" "${restore_args[@]}" 9>&-; then
  if [[ "${skip_package_refresh}" == false && ( "${strict_verification_mode}" == true || "${package_plane_source_kind}" == "locked-public-source" ) && "${attestation_issuing_command}" == true && -n "${target_project}" ]]; then
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
