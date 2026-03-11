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
published_feed_sources="${CHUMMER_PUBLISHED_FEED_SOURCES:-}"
published_engine_contracts_version="${CHUMMER_PUBLISHED_ENGINE_CONTRACTS_VERSION:-}"
published_play_contracts_version="${CHUMMER_PUBLISHED_PLAY_CONTRACTS_VERSION:-}"
published_ui_kit_version="${CHUMMER_PUBLISHED_UI_KIT_VERSION:-}"

restore_args=()

if [[ -n "${published_feed_sources}" ]]; then
  restore_args+=(-p:RestoreSources="${published_feed_sources}" -p:RestoreIgnoreFailedSources=false)
else
  mkdir -p "${local_feed}"
  dotnet pack "${repo_root}/eng/package-stubs/EngineContractsStub/EngineContractsStub.csproj" --nologo -c Release -o "${local_feed}" >/dev/null
  dotnet pack "${repo_root}/eng/package-stubs/PlayContractsStub/PlayContractsStub.csproj" --nologo -c Release -o "${local_feed}" >/dev/null
  dotnet pack "${repo_root}/eng/package-stubs/UiKitStub/UiKitStub.csproj" --nologo -c Release -o "${local_feed}" >/dev/null
  restore_args+=(-p:RestoreSources="${local_feed}" -p:RestoreIgnoreFailedSources=true)
fi

if [[ -n "${published_engine_contracts_version}" ]]; then
  restore_args+=(-p:ChummerEngineContractsPackageVersion="${published_engine_contracts_version}")
fi

if [[ -n "${published_play_contracts_version}" ]]; then
  restore_args+=(-p:ChummerPlayContractsPackageVersion="${published_play_contracts_version}")
fi

if [[ -n "${published_ui_kit_version}" ]]; then
  restore_args+=(-p:ChummerUiKitPackageVersion="${published_ui_kit_version}")
fi

exec dotnet "$@" "${restore_args[@]}"
