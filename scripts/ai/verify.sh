#!/usr/bin/env bash
set -euo pipefail

export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-/tmp/dotnet-chummer-play}"
export HOME="${HOME:-/tmp}"
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
package_plane_runner="${repo_root}/scripts/ai/with-package-plane.sh"
published_feed_sources="${CHUMMER_PUBLISHED_FEED_SOURCES:-}"

test -f README.md
test -f AGENTS.md
test -f WORKLIST.md
test -f Chummer.Play.slnx
test -f Directory.Build.props
test -f Directory.Packages.props
test -f global.json
test -f docs/chummer-play.design.v1.md
test -f docs/sync-model.md
test -f docs/offline-storage.md
test -f docs/migration-map.md
test -f feedback/2026-03-09-mobile-play-split-guide.md
test -f src/Chummer.Play.Web/Program.cs
test -f src/Chummer.Play.Web/PlayWebApplication.cs
test -f src/Chummer.Play.Web/PlayRouteHandlers.cs
test -f src/Chummer.Play.Web/BrowserSessionApiClient.cs
test -f src/Chummer.Play.Web/BrowserSessionCoachApiClient.cs
test -f src/Chummer.Play.Web/BrowserSessionEventLogStore.cs
test -f src/Chummer.Play.Web/BrowserSessionOfflineCacheService.cs
test -f src/Chummer.Play.Web/BrowserSessionOfflineQueueService.cs
test -f src/Chummer.Play.Core/PlayApi/BrowserSessionShellProbe.cs
test -f src/Chummer.Play.Core/PlayApi/PlaySessionModels.cs
test -f src/Chummer.Play.Core/Offline/IPlayEventLogStore.cs
test -f src/Chummer.Play.Core/Sync/IPlayOfflineCacheService.cs
test -f src/Chummer.Play.Core/Sync/IPlayOfflineQueueService.cs
test -f src/Chummer.Play.Core/Sync/OfflineQueueResults.cs
test -f src/Chummer.Play.Web/BrowserState/IBrowserKeyValueStore.cs
test -f src/Chummer.Play.Web/BrowserState/InMemoryBrowserKeyValueStore.cs
test -f src/Chummer.Play.Web/BrowserState/PlayBrowserStateKeys.cs
test -f src/Chummer.Play.Web/BrowserState/RuntimeBundleCacheEntry.cs
test -f src/Chummer.Play.Web/wwwroot/index.html
test -f src/Chummer.Play.Web/wwwroot/manifest.webmanifest
test -f src/Chummer.Play.Web/wwwroot/service-worker.js
test -f src/Chummer.Play.Web/wwwroot/icons/icon-192.svg
test -f src/Chummer.Play.Web/wwwroot/icons/icon-512.svg
test -f scripts/ai/with-package-plane.sh
test -f src/Chummer.Play.RegressionChecks/Chummer.Play.RegressionChecks.csproj
test -f src/Chummer.Play.RegressionChecks/Program.cs
test -f src/Chummer.Play.Core/Chummer.Play.Core.csproj
test -f src/Chummer.Play.Components/Chummer.Play.Components.csproj
test -f src/Chummer.Play.Player/Chummer.Play.Player.csproj
test -f src/Chummer.Play.Gm/Chummer.Play.Gm.csproj
test -f eng/package-stubs/EngineContractsStub/EngineContractsStub.csproj
test -f eng/package-stubs/PlayContractsStub/PlayContractsStub.csproj
test -f eng/package-stubs/UiKitStub/UiKitStub.csproj

if rg -n "Chummer\\.Contracts/" . >/dev/null 2>&1; then
  echo "copied contract source paths are not allowed in chummer-play" >&2
  exit 1
fi

if rg -n "<ProjectReference Include=\"\\.\\.\\\\\\.\\.\\\\|<ProjectReference Include=\"\\.\\./\\.\\./" src >/dev/null 2>&1; then
  echo "cross-repo project references are not allowed in chummer-play" >&2
  exit 1
fi

if find . -type d -name "Chummer.Contracts" | grep -q .; then
  echo "duplicated shared contract source is not allowed in chummer-play" >&2
  exit 1
fi

if find . -type d \( -name "Chummer.Engine.Contracts" -o -name "Chummer.Play.Contracts" -o -name "Chummer.Ui.Kit" \) | grep -q .; then
  echo "shared package source trees are not allowed in chummer-play" >&2
  exit 1
fi

if rg -n "EnableChummerPackageReferences" Directory.Build.props src README.md AGENTS.md WORKLIST.md docs >/dev/null 2>&1; then
  echo "package references must be unconditional across the play package plane" >&2
  exit 1
fi

rg -n "<PackageVersion Include=\"Chummer\\.Engine\\.Contracts\"" Directory.Packages.props >/dev/null
rg -n "<PackageVersion Include=\"Chummer\\.Play\\.Contracts\"" Directory.Packages.props >/dev/null
rg -n "<PackageVersion Include=\"Chummer\\.Ui\\.Kit\"" Directory.Packages.props >/dev/null
rg -n "<ChummerEngineContractsPackageId" Directory.Build.props >/dev/null
rg -n "<ChummerEngineContractsPackageVersion>" Directory.Packages.props >/dev/null
rg -n "<ChummerPlayContractsPackageVersion>" Directory.Packages.props >/dev/null
rg -n "<ChummerUiKitPackageVersion>" Directory.Packages.props >/dev/null
rg -n "<PackageReference Include=\"\\$\\(ChummerEngineContractsPackageId\\)\"" src/Chummer.Play.Core/Chummer.Play.Core.csproj >/dev/null
rg -n "<PackageReference Include=\"\\$\\(ChummerPlayContractsPackageId\\)\"" src/Chummer.Play.Core/Chummer.Play.Core.csproj >/dev/null
rg -n "<PackageReference Include=\"\\$\\(ChummerUiKitPackageId\\)\"" src/Chummer.Play.Components/Chummer.Play.Components.csproj >/dev/null

if rg -n "ChummerContractsPackageId|ChummerContractsPackageVersion" Directory.Build.props Directory.Packages.props src docs README.md AGENTS.md WORKLIST.md >/dev/null 2>&1; then
  echo "legacy engine contract package property naming is not allowed; use ChummerEngineContracts* properties" >&2
  exit 1
fi

mapfile -t package_references < <(
  rg -n --no-heading '<PackageReference Include="[^"]+"' src -g '*.csproj' \
    | sed -E 's/.*Include=\"([^\"]+)\".*/\1/'
)

for package_reference in "${package_references[@]}"; do
  case "${package_reference}" in
    '$(ChummerEngineContractsPackageId)'|'$(ChummerPlayContractsPackageId)'|'$(ChummerUiKitPackageId)')
      ;;
    Chummer.Engine.Contracts|Chummer.Play.Contracts|Chummer.Ui.Kit)
      echo "shared Chummer package references must use Directory.Build.props package id properties: ${package_reference}" >&2
      exit 1
      ;;
    Chummer.*)
      echo "unsupported Chummer package reference in play repo: ${package_reference}" >&2
      exit 1
      ;;
  esac
done

rg -n 'WL-012 .*dedicated `/api/play/\*` surface' WORKLIST.md >/dev/null
rg -n 'WL-013 .*browser offline cache ownership' WORKLIST.md >/dev/null
rg -n 'WL-014 .*installable play shell for PWA usage' WORKLIST.md >/dev/null
rg -n 'WL-009 .* done .*bootstrap' WORKLIST.md >/dev/null
rg -n 'WL-010 .* done .*BrowserSessionApiClient' WORKLIST.md >/dev/null
rg -n 'WL-011 .* done .*BrowserSessionEventLogStore' WORKLIST.md >/dev/null
rg -n 'published-feed cutover for `Chummer.Play.Contracts` and `Chummer.Ui.Kit`' docs/chummer-play.design.v1.md >/dev/null
rg -n 'Milestone 4 dedicated play API ownership: `WL-004` aligns the contract family and `WL-012` owns the executable `/api/play/projection`, `/api/play/reconnect`, and `/api/play/sync` route surface' docs/chummer-play.design.v1.md >/dev/null
rg -n 'Milestone 6 offline cache and local-first replay ownership: `WL-005` remains the sync/storage umbrella, `WL-011` owns browser-backed event-ledger persistence, and `WL-013` owns runtime bundle lineage, replay checkpoints, and resume metadata in browser storage' docs/chummer-play.design.v1.md >/dev/null
rg -n 'Milestone 8 installable PWA hardening: `WL-007` now closes the installability umbrella with media-cache lifecycle hardening, while `WL-014` owns the concrete manifest, baseline service-worker cache policy, quota/backpressure handling, and deep-link resume work' docs/chummer-play.design.v1.md >/dev/null
rg -n 'resume/reconnect continuity and runtime-bundle lineage coherence' docs/chummer-play.design.v1.md >/dev/null
rg -n 'BrowserSessionOfflineCacheService.*owns runtime bundle cache metadata' docs/offline-storage.md >/dev/null
rg -n '/api/play/projection.* /api/play/reconnect.* /api/play/sync|/api/play/bootstrap.*role-aware shell bootstrap' docs/sync-model.md >/dev/null
rg -n 'PlayApiRoutes\.Projection' src/Chummer.Play.Web >/dev/null
rg -n 'PlayApiRoutes\.Reconnect' src/Chummer.Play.Web >/dev/null
rg -n 'PlayApiRoutes\.Sync' src/Chummer.Play.Web >/dev/null
rg -n 'PlayApiRoutes\.Resume' src/Chummer.Play.Web >/dev/null
rg -n 'PlayApiRoutes\.CachePressure' src/Chummer.Play.Web >/dev/null
rg -n '"/play/\{sessionId\}"' src/Chummer.Play.Web >/dev/null
rg -n 'SelectShell\(bootstrapRequest\.Role, playerShell, gmShell\)' src/Chummer.Play.Web >/dev/null
rg -n '\[activeShell\]' src/Chummer.Play.Web >/dev/null
rg -n 'BuildQuickActions\(bootstrapRequest\.Role, roleCapabilities\)|BuildQuickActions\(request\.Role, roleCapabilities\)' src/Chummer.Play.Web >/dev/null
rg -n 'role == PlaySurfaceRole.Player && roleCapabilities.Contains\("play.session.sync"' src/Chummer.Play.Web >/dev/null
rg -n 'role == PlaySurfaceRole.GameMaster && roleCapabilities.Contains\("play.gm.actions"' src/Chummer.Play.Web >/dev/null
rg -n 'role == PlaySurfaceRole.GameMaster && roleCapabilities.Contains\("play.spider.cards"' src/Chummer.Play.Web >/dev/null
rg -n 'BrowserSessionOfflineQueueService' src/Chummer.Play.Web >/dev/null
rg -n 'IPlayEventLogStore' src/Chummer.Play.Web >/dev/null
rg -n 'IPlayOfflineCacheService' src/Chummer.Play.Web >/dev/null
rg -n 'IPlayOfflineQueueService' src/Chummer.Play.Web >/dev/null
rg -n 'AddSingleton<IPlayEventLogStore>' src/Chummer.Play.Web/PlayWebApplication.cs >/dev/null
rg -n 'AddSingleton<IPlayOfflineCacheService>' src/Chummer.Play.Web/PlayWebApplication.cs >/dev/null
rg -n 'AddSingleton<IPlayOfflineQueueService>' src/Chummer.Play.Web/PlayWebApplication.cs >/dev/null
rg -n 'offlineQueueService.EnqueueAsync\(' src/Chummer.Play.Web >/dev/null
rg -n 'offlineQueueService.SyncReplayAsync\(' src/Chummer.Play.Web >/dev/null
rg -n 'TryValidateCursor\(request\.Cursor, out var cursorError\)' src/Chummer.Play.Web >/dev/null
rg -n 'pending events cannot contain blank values' src/Chummer.Play.Web >/dev/null
rg -n 'SessionLineage\.IsStoredLineageAligned\(' src/Chummer.Play.Web >/dev/null
rg -n 'VerifySyncPrefixAcknowledgementAsync\(' src/Chummer.Play.RegressionChecks/Program.cs >/dev/null
rg -n 'VerifyEventLogRejectsMalformedAppendAsync\(' src/Chummer.Play.RegressionChecks/Program.cs >/dev/null
rg -n 'VerifyEventLogRejectsSequenceRegressionAsync\(' src/Chummer.Play.RegressionChecks/Program.cs >/dev/null
rg -n 'VerifyStoredLineageStaleResponsesAsync\(' src/Chummer.Play.RegressionChecks/Program.cs >/dev/null
rg -n 'VerifyOfflineQueueRejectsMalformedSessionEnvelopeAsync\(' src/Chummer.Play.RegressionChecks/Program.cs >/dev/null
rg -n 'VerifyOfflineQueueRejectsStaleLineageAsync\(' src/Chummer.Play.RegressionChecks/Program.cs >/dev/null
rg -n 'VerifyStoredLineageAlignment\(' src/Chummer.Play.RegressionChecks/Program.cs >/dev/null
rg -n 'IsLedgerAligned\(' src/Chummer.Play.Web/SessionLineage.cs >/dev/null
rg -n 'Math\.Max\(ledgerBeforeAppend\.LastKnownSequence, cursor\.AppliedThroughSequence\) \+ 1' src/Chummer.Play.Web/BrowserSessionOfflineQueueService.cs >/dev/null
rg -n 'EnsureStoredLineageAlignedAsync\(' src/Chummer.Play.Web/BrowserSessionOfflineQueueService.cs >/dev/null
rg -n 'Session id is required\.|Scene id is required\.|Scene revision is required\.|Runtime fingerprint is required\.' src/Chummer.Play.Web/BrowserSessionOfflineQueueService.cs >/dev/null
rg -n 'UseDefaultFiles\(\);' src/Chummer.Play.Web/PlayWebApplication.cs >/dev/null
rg -n 'UseStaticFiles\(\);' src/Chummer.Play.Web/PlayWebApplication.cs >/dev/null
rg -n 'AcknowledgePendingEventsAsync\(' src/Chummer.Play.Web/BrowserSessionOfflineQueueService.cs >/dev/null
rg -n 'CountAcceptedEventPrefix\(' src/Chummer.Play.Web/BrowserSessionOfflineQueueService.cs >/dev/null
rg -n 'LastAcceptedEventCount' src/Chummer.Play.Web/BrowserSessionEventLogStore.cs >/dev/null
rg -n 'RuntimeBundleQuota' src/Chummer.Play.Web/BrowserSessionOfflineCacheService.cs >/dev/null
rg -n 'ListKeysAsync\(' src/Chummer.Play.Web/BrowserSessionOfflineCacheService.cs >/dev/null
rg -n 'RemoveAsync\(' src/Chummer.Play.Web/BrowserSessionOfflineCacheService.cs >/dev/null
rg -n 'PlayCachePressureSnapshot' src/Chummer.Play.Core/PlayApi/PlaySessionModels.cs >/dev/null
rg -n 'PlayResumeResponse' src/Chummer.Play.Core/PlayApi/PlaySessionModels.cs >/dev/null
rg -n '"start_url": "/index.html"' src/Chummer.Play.Web/wwwroot/manifest.webmanifest >/dev/null
rg -n 'navigator\.serviceWorker\.register' src/Chummer.Play.Web/wwwroot/index.html >/dev/null
rg -n 'CACHE_VERSION' src/Chummer.Play.Web/wwwroot/service-worker.js >/dev/null
rg -n 'MEDIA_CACHE' src/Chummer.Play.Web/wwwroot/service-worker.js >/dev/null
rg -n 'MEDIA_MAX_ENTRIES' src/Chummer.Play.Web/wwwroot/service-worker.js >/dev/null
rg -n 'pruneMediaCache' src/Chummer.Play.Web/wwwroot/service-worker.js >/dev/null
rg -n 'QuotaExceededError|NS_ERROR_DOM_QUOTA_REACHED' src/Chummer.Play.Web/wwwroot/service-worker.js >/dev/null
rg -n 'public sealed record EngineSessionEnvelope' src/Chummer.Play.Core/PlayApi/PlaySessionModels.cs >/dev/null
rg -n 'public sealed record EngineSessionCursor' src/Chummer.Play.Core/PlayApi/PlaySessionModels.cs >/dev/null
rg -n 'EngineSessionEnvelope Session' src/Chummer.Play.Core/PlayApi/PlaySessionModels.cs >/dev/null
rg -n 'EngineSessionCursor Cursor' src/Chummer.Play.Core/PlayApi/PlaySessionModels.cs >/dev/null
rg -n 'request\.Cursor\.Session' src/Chummer.Play.Web >/dev/null
rg -n 'IBrowserKeyValueStore' src/Chummer.Play.Web/BrowserSessionEventLogStore.cs >/dev/null
rg -n 'GetFromJsonAsync<PlaySessionProjection>' src/Chummer.Play.Web/BrowserSessionApiClient.cs >/dev/null
rg -n 'request\.Session\.SessionId' src/Chummer.Play.Web/BrowserSessionApiClient.cs >/dev/null
rg -n 'PostAsJsonAsync\(PlayApiRoutes\.Reconnect' src/Chummer.Play.Web/BrowserSessionApiClient.cs >/dev/null
rg -n 'PostAsJsonAsync\(PlayApiRoutes\.Sync' src/Chummer.Play.Web/BrowserSessionApiClient.cs >/dev/null

if rg -n 'scaffold|placeholder|TODO' \
  src/Chummer.Play.Web/PlayWebApplication.cs \
  src/Chummer.Play.Web/PlayRouteHandlers.cs \
  src/Chummer.Play.Web/BrowserSessionApiClient.cs \
  src/Chummer.Play.Web/BrowserSessionEventLogStore.cs >/dev/null 2>&1; then
  echo "bootstrap/session/event-log seams must stay executable and free of scaffold placeholders" >&2
  exit 1
fi

bash "${package_plane_runner}" build src/Chummer.Play.Core/Chummer.Play.Core.csproj --nologo
bash "${package_plane_runner}" build src/Chummer.Play.Components/Chummer.Play.Components.csproj --nologo
bash "${package_plane_runner}" build src/Chummer.Play.Player/Chummer.Play.Player.csproj --nologo
bash "${package_plane_runner}" build src/Chummer.Play.Gm/Chummer.Play.Gm.csproj --nologo
bash "${package_plane_runner}" build src/Chummer.Play.Web/Chummer.Play.Web.csproj --nologo
bash "${package_plane_runner}" build src/Chummer.Play.RegressionChecks/Chummer.Play.RegressionChecks.csproj --nologo >/dev/null
bash "${package_plane_runner}" run --project src/Chummer.Play.RegressionChecks/Chummer.Play.RegressionChecks.csproj --nologo --no-build >/dev/null

if [[ -n "${published_feed_sources}" ]]; then
  echo "running published-feed compatibility restore/build checks"
  bash "${package_plane_runner}" restore src/Chummer.Play.Core/Chummer.Play.Core.csproj --nologo >/dev/null
  bash "${package_plane_runner}" restore src/Chummer.Play.Components/Chummer.Play.Components.csproj --nologo >/dev/null
  bash "${package_plane_runner}" restore src/Chummer.Play.Player/Chummer.Play.Player.csproj --nologo >/dev/null
  bash "${package_plane_runner}" restore src/Chummer.Play.Gm/Chummer.Play.Gm.csproj --nologo >/dev/null
  bash "${package_plane_runner}" restore src/Chummer.Play.Web/Chummer.Play.Web.csproj --nologo >/dev/null
  bash "${package_plane_runner}" build src/Chummer.Play.Core/Chummer.Play.Core.csproj --nologo --no-restore >/dev/null
  bash "${package_plane_runner}" build src/Chummer.Play.Components/Chummer.Play.Components.csproj --nologo --no-restore >/dev/null
  bash "${package_plane_runner}" build src/Chummer.Play.Player/Chummer.Play.Player.csproj --nologo --no-restore >/dev/null
  bash "${package_plane_runner}" build src/Chummer.Play.Gm/Chummer.Play.Gm.csproj --nologo --no-restore >/dev/null
  bash "${package_plane_runner}" build src/Chummer.Play.Web/Chummer.Play.Web.csproj --nologo --no-restore >/dev/null
  bash "${package_plane_runner}" build src/Chummer.Play.RegressionChecks/Chummer.Play.RegressionChecks.csproj --nologo --no-restore >/dev/null
  bash "${package_plane_runner}" run --project src/Chummer.Play.RegressionChecks/Chummer.Play.RegressionChecks.csproj --nologo --no-build >/dev/null
else
  echo "published-feed compatibility restore/build checks skipped (set CHUMMER_PUBLISHED_FEED_SOURCES to enable)"
  echo "running published-feed compatibility smoke check with repo-local feed"
  local_published_smoke_feed="${repo_root}/.artifacts/nuget-published-smoke"
  mkdir -p "${local_published_smoke_feed}"
  dotnet pack "${repo_root}/eng/package-stubs/EngineContractsStub/EngineContractsStub.csproj" --nologo -c Release -o "${local_published_smoke_feed}" >/dev/null
  dotnet pack "${repo_root}/eng/package-stubs/PlayContractsStub/PlayContractsStub.csproj" --nologo -c Release -o "${local_published_smoke_feed}" >/dev/null
  dotnet pack "${repo_root}/eng/package-stubs/UiKitStub/UiKitStub.csproj" --nologo -c Release -o "${local_published_smoke_feed}" >/dev/null
  CHUMMER_PUBLISHED_FEED_SOURCES="${local_published_smoke_feed}" \
    bash "${package_plane_runner}" restore src/Chummer.Play.Core/Chummer.Play.Core.csproj --nologo >/dev/null
  CHUMMER_PUBLISHED_FEED_SOURCES="${local_published_smoke_feed}" \
    bash "${package_plane_runner}" restore src/Chummer.Play.Components/Chummer.Play.Components.csproj --nologo >/dev/null
  CHUMMER_PUBLISHED_FEED_SOURCES="${local_published_smoke_feed}" \
    bash "${package_plane_runner}" restore src/Chummer.Play.Player/Chummer.Play.Player.csproj --nologo >/dev/null
  CHUMMER_PUBLISHED_FEED_SOURCES="${local_published_smoke_feed}" \
    bash "${package_plane_runner}" restore src/Chummer.Play.Gm/Chummer.Play.Gm.csproj --nologo >/dev/null
  CHUMMER_PUBLISHED_FEED_SOURCES="${local_published_smoke_feed}" \
    bash "${package_plane_runner}" restore src/Chummer.Play.Web/Chummer.Play.Web.csproj --nologo >/dev/null
  CHUMMER_PUBLISHED_FEED_SOURCES="${local_published_smoke_feed}" \
    bash "${package_plane_runner}" build src/Chummer.Play.Core/Chummer.Play.Core.csproj --nologo --no-restore >/dev/null
  CHUMMER_PUBLISHED_FEED_SOURCES="${local_published_smoke_feed}" \
    bash "${package_plane_runner}" build src/Chummer.Play.Components/Chummer.Play.Components.csproj --nologo --no-restore >/dev/null
  CHUMMER_PUBLISHED_FEED_SOURCES="${local_published_smoke_feed}" \
    bash "${package_plane_runner}" build src/Chummer.Play.Player/Chummer.Play.Player.csproj --nologo --no-restore >/dev/null
  CHUMMER_PUBLISHED_FEED_SOURCES="${local_published_smoke_feed}" \
    bash "${package_plane_runner}" build src/Chummer.Play.Gm/Chummer.Play.Gm.csproj --nologo --no-restore >/dev/null
  CHUMMER_PUBLISHED_FEED_SOURCES="${local_published_smoke_feed}" \
    bash "${package_plane_runner}" build src/Chummer.Play.Web/Chummer.Play.Web.csproj --nologo --no-restore >/dev/null
  CHUMMER_PUBLISHED_FEED_SOURCES="${local_published_smoke_feed}" \
    bash "${package_plane_runner}" build src/Chummer.Play.RegressionChecks/Chummer.Play.RegressionChecks.csproj --nologo --no-restore >/dev/null
  CHUMMER_PUBLISHED_FEED_SOURCES="${local_published_smoke_feed}" \
    bash "${package_plane_runner}" run --project src/Chummer.Play.RegressionChecks/Chummer.Play.RegressionChecks.csproj --nologo --no-build >/dev/null
fi

echo "chummer-play verify ok"
