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
local_feed="${repo_root}/.artifacts/nuget-local"
export NUGET_PACKAGES="${NUGET_PACKAGES:-${repo_root}/.artifacts/nuget-packages}"
published_feed_sources="${CHUMMER_PUBLISHED_FEED_SOURCES:-}"
published_engine_contracts_version="${CHUMMER_PUBLISHED_ENGINE_CONTRACTS_VERSION:-}"
published_campaign_contracts_version="${CHUMMER_PUBLISHED_CAMPAIGN_CONTRACTS_VERSION:-}"
published_control_contracts_version="${CHUMMER_PUBLISHED_CONTROL_CONTRACTS_VERSION:-}"
published_play_contracts_version="${CHUMMER_PUBLISHED_PLAY_CONTRACTS_VERSION:-}"
published_ui_kit_version="${CHUMMER_PUBLISHED_UI_KIT_VERSION:-}"
workspace_root="$(cd "${repo_root}/.." && pwd)"
engine_contracts_project="${workspace_root}/chummer-core-engine/Chummer.Contracts/Chummer.Contracts.csproj"
campaign_contracts_project="${workspace_root}/chummer.run-services/Chummer.Campaign.Contracts/Chummer.Campaign.Contracts.csproj"
control_contracts_project="${workspace_root}/chummer.run-services/Chummer.Control.Contracts/Chummer.Control.Contracts.csproj"
play_contracts_project="${workspace_root}/chummer.run-services/Chummer.Play.Contracts/Chummer.Play.Contracts.csproj"
ui_kit_project="${workspace_root}/chummer-ui-kit/src/Chummer.Ui.Kit/Chummer.Ui.Kit.csproj"

restore_args=()
skip_package_refresh=false

for arg in "$@"; do
  case "${arg}" in
    --no-restore|--no-build)
      skip_package_refresh=true
      ;;
  esac
done

pack_owner_package() {
  local project_path="$1"
  local package_id="$2"
  local package_version="$3"

  if [[ ! -f "${project_path}" ]]; then
    return 1
  fi

  dotnet pack "${project_path}" \
    --nologo \
    -c Release \
    -o "${local_feed}" \
    -p:PackageId="${package_id}" \
    -p:PackageVersion="${package_version}" >/dev/null
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
    if ! pack_owner_package "${engine_contracts_project}" "Chummer.Engine.Contracts" "${published_engine_contracts_version:-0.1.0-preview}"; then
      dotnet pack "${repo_root}/eng/package-stubs/EngineContractsStub/EngineContractsStub.csproj" --nologo -c Release -o "${local_feed}" >/dev/null
    fi
    if ! pack_owner_package "${campaign_contracts_project}" "Chummer.Campaign.Contracts" "${published_campaign_contracts_version:-0.1.0-preview}"; then
      dotnet pack "${repo_root}/eng/package-stubs/CampaignContractsStub/CampaignContractsStub.csproj" --nologo -c Release -o "${local_feed}" >/dev/null
    fi
    if ! pack_owner_package "${control_contracts_project}" "Chummer.Control.Contracts" "${published_control_contracts_version:-0.1.0-preview}"; then
      dotnet pack "${repo_root}/eng/package-stubs/ControlContractsStub/ControlContractsStub.csproj" --nologo -c Release -o "${local_feed}" >/dev/null
    fi
    if ! pack_owner_package "${play_contracts_project}" "Chummer.Play.Contracts" "${published_play_contracts_version:-0.1.0-preview}"; then
      dotnet pack "${repo_root}/eng/package-stubs/PlayContractsStub/PlayContractsStub.csproj" --nologo -c Release -o "${local_feed}" >/dev/null
    fi
    if ! pack_owner_package "${ui_kit_project}" "Chummer.Ui.Kit" "${published_ui_kit_version:-0.1.0-preview}"; then
      dotnet pack "${repo_root}/eng/package-stubs/UiKitStub/UiKitStub.csproj" --nologo -c Release -o "${local_feed}" >/dev/null
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

exec dotnet "$@" "${restore_args[@]}"
