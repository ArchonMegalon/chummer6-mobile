#!/usr/bin/env bash
set -euo pipefail

export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-/tmp/dotnet-chummer6-mobile}"
export HOME="${HOME:-/tmp}"
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
package_plane_runner="${repo_root}/scripts/ai/with-package-plane.sh"
published_feed_sources="${CHUMMER_PUBLISHED_FEED_SOURCES:-}"

cd "${repo_root}"

test -f README.md
test -f AGENTS.md
test -f WORKLIST.md
test -f Chummer.Play.slnx
test -f Directory.Build.props
test -f Directory.Packages.props
test -f global.json
test -f docs/chummer6-mobile.design.v1.md
test -f docs/chummer-play.design.v1.md
test -f docs/rejoin-resume-guarantees.md
test -f docs/sync-model.md
test -f docs/offline-storage.md
test -f docs/migration-map.md
test -f docs/PLAY_RELEASE_SIGNOFF.md
test -f feedback/2026-03-10-public-repo-graph-audit.md
test -f src/Chummer.Play.Web/Program.cs
test -f src/Chummer.Play.Web/PlayWebApplication.cs
test -f src/Chummer.Play.Web/PlayRouteHandlers.cs
test -f src/Chummer.Play.Web/BrowserSessionApiClient.cs
test -f src/Chummer.Play.Web/BrowserSessionCoachApiClient.cs
test -f src/Chummer.Play.Web/BrowserSessionEventLogStore.cs
test -f src/Chummer.Play.Web/BrowserSessionOfflineCacheService.cs
test -f src/Chummer.Play.Web/BrowserSessionOfflineQueueService.cs
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
test -f scripts/materialize_mobile_local_release_proof.py
test -f scripts/ai/with-package-plane.sh
test -f src/Chummer.Play.RegressionChecks/Chummer.Play.RegressionChecks.csproj
test -f src/Chummer.Play.RegressionChecks/Program.cs
test -f src/Chummer.Play.Core/Chummer.Play.Core.csproj
test -f src/Chummer.Play.Core/Roaming/RoamingWorkspaceSyncPlanner.cs
test -f src/Chummer.Play.Components/Chummer.Play.Components.csproj
test -f src/Chummer.Play.Player/Chummer.Play.Player.csproj
test -f src/Chummer.Play.Gm/Chummer.Play.Gm.csproj
test -f eng/package-stubs/EngineContractsStub/EngineContractsStub.csproj
test -f eng/package-stubs/CampaignContractsStub/CampaignContractsStub.csproj
test -f eng/package-stubs/PlayContractsStub/PlayContractsStub.csproj
test -f eng/package-stubs/UiKitStub/UiKitStub.csproj

if rg -n "Chummer\\.Contracts/" src README.md AGENTS.md WORKLIST.md docs >/dev/null 2>&1; then
  echo "copied contract source paths are not allowed in chummer6-mobile" >&2
  exit 1
fi

if rg -n "<ProjectReference Include=\"\\.\\.\\\\\\.\\.\\\\|<ProjectReference Include=\"\\.\\./\\.\\./" src >/dev/null 2>&1; then
  echo "cross-repo project references are not allowed in chummer6-mobile" >&2
  exit 1
fi

if find . -type d -name "Chummer.Contracts" | grep -q .; then
  echo "duplicated shared contract source is not allowed in chummer6-mobile" >&2
  exit 1
fi

if find . -type d \( -name "Chummer.Engine.Contracts" -o -name "Chummer.Play.Contracts" -o -name "Chummer.Ui.Kit" \) | grep -q .; then
  echo "shared package source trees are not allowed in chummer6-mobile" >&2
  exit 1
fi

if find src/Chummer.Play.Core -type f \( \
  -path "*/Application/PlaySurfaceRole.cs" -o \
  -path "*/PlayApi/BrowserSessionShellProbe.cs" -o \
  -path "*/PlayApi/PlaySessionModels.cs" -o \
  -path "*/Sync/SyncCheckpoint.cs" -o \
  -path "*/Offline/OfflineLedgerEnvelope.cs" \
  \) | grep -q .; then
  echo "repo-local play contract source copies are not allowed in chummer6-mobile" >&2
  exit 1
fi

if rg -n 'namespace Chummer\.Contracts\.Session;|public (sealed )?record (EffectAppliedEvent|TrackerIncrementedEvent)\b|public interface ISessionEvent\b' src -g '*.cs' >/dev/null 2>&1; then
  echo "semantic session contracts must remain engine-owned and must not be redefined in chummer6-mobile" >&2
  exit 1
fi

if rg -n '\\b(class|record)\\s+(TokenCanon|ThemeCompiler|ShellChrome|AccessibilityState|Banner|StaleStateBadge|ApprovalChip|OfflineBanner)\\b|\\b(static\\s+)?UiAdapterPayload\\s+Adapt(ShellChrome|AccessibilityState|Banner|StaleStateBadge|ApprovalChip|OfflineBanner)\\s*\\(' src -g '*.cs' >/dev/null 2>&1; then
  echo "source-copied ui-kit token/theme/shell/accessibility primitives are not allowed in chummer6-mobile" >&2
  exit 1
fi

if rg -n "EnableChummerPackageReferences" Directory.Build.props src README.md AGENTS.md WORKLIST.md docs >/dev/null 2>&1; then
  echo "package references must be unconditional across the play package plane" >&2
  exit 1
fi

rg -n "<PackageVersion Include=\"Chummer\\.Engine\\.Contracts\"" Directory.Packages.props >/dev/null
rg -n "<PackageVersion Include=\"Chummer\\.Campaign\\.Contracts\"" Directory.Packages.props >/dev/null
rg -n "<PackageVersion Include=\"Chummer\\.Play\\.Contracts\"" Directory.Packages.props >/dev/null
rg -n "<PackageVersion Include=\"Chummer\\.Ui\\.Kit\"" Directory.Packages.props >/dev/null
rg -n "<ChummerEngineContractsPackageId" Directory.Build.props >/dev/null
rg -n "<ChummerCampaignContractsPackageId" Directory.Build.props >/dev/null
rg -n "<ChummerEngineContractsPackageVersion>" Directory.Packages.props >/dev/null
rg -n "<ChummerCampaignContractsPackageVersion>" Directory.Packages.props >/dev/null
rg -n "<ChummerPlayContractsPackageVersion>" Directory.Packages.props >/dev/null
rg -n "<ChummerUiKitPackageVersion>" Directory.Packages.props >/dev/null
rg -n "<PackageReference Include=\"\\$\\(ChummerEngineContractsPackageId\\)\"" src/Chummer.Play.Core/Chummer.Play.Core.csproj >/dev/null
rg -n "<PackageReference Include=\"\\$\\(ChummerCampaignContractsPackageId\\)\"" src/Chummer.Play.Core/Chummer.Play.Core.csproj >/dev/null
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
    '$(ChummerEngineContractsPackageId)'|'$(ChummerCampaignContractsPackageId)'|'$(ChummerPlayContractsPackageId)'|'$(ChummerUiKitPackageId)')
      ;;
    Chummer.Engine.Contracts|Chummer.Campaign.Contracts|Chummer.Play.Contracts|Chummer.Ui.Kit)
      echo "shared Chummer package references must use Directory.Build.props package id properties: ${package_reference}" >&2
      exit 1
      ;;
    Chummer.*)
      echo "unsupported Chummer package reference in chummer6-mobile: ${package_reference}" >&2
      exit 1
      ;;
  esac
done

require_worklist_or_audit_pattern() {
  local pattern="$1"
  if rg -n "${pattern}" WORKLIST.md AUDIT_LOG.md >/dev/null 2>&1; then
    return 0
  fi

  echo "missing queue/audit traceability pattern: ${pattern}" >&2
  exit 1
}

require_worklist_or_audit_pattern 'WL-012 .*dedicated `/api/play/\*` surface'
require_worklist_or_audit_pattern 'WL-013 .*browser offline cache ownership'
require_worklist_or_audit_pattern 'WL-014 .*installable play shell for PWA usage'
require_worklist_or_audit_pattern 'WL-020 .* done .*executable mobile backlog slices'
require_worklist_or_audit_pattern 'WL-021 .* done .*Close M10 hardening evidence gates'
require_worklist_or_audit_pattern 'WL-022 .* done .*Close M11 finished-play-shell release gate'
require_worklist_or_audit_pattern 'M10 .*WL-005, WL-020, WL-021'
require_worklist_or_audit_pattern 'M11 .*WL-020, WL-022'
require_worklist_or_audit_pattern 'TG-M10-AX .* done .*executable regression coverage'
require_worklist_or_audit_pattern 'TG-M10-PF .* done .*verify.sh'
require_worklist_or_audit_pattern 'TG-M10-RR .* done .*RegressionChecks/Program.cs'
require_worklist_or_audit_pattern 'TG-M11-RG .* done '
require_worklist_or_audit_pattern 'TG-M11-RG .*VerifyBootstrapRoleShellEntryPointsAsync.*VerifyQuickActionRejectsCrossRoleAuthorizationAsync'
require_worklist_or_audit_pattern 'TG-M11-OC .* done '
require_worklist_or_audit_pattern 'TG-M11-OC .*PlayApiRoutes.ContinuityClaim.*PlayApiRoutes.Observe'
require_worklist_or_audit_pattern 'TG-M11-OC .*VerifyContinuityClaimRejectsStaleLineageWithoutMutationAsync.*VerifyObserveReturnsLineageSafeContinuityAsync'
require_worklist_or_audit_pattern 'TG-M11-RC .* done '
require_worklist_or_audit_pattern 'TG-M11-RC .*WL-021.*WL-022.*scripts/ai/verify.sh'
require_worklist_or_audit_pattern 'WL-026 .* done .*post-closure runnable backlog'
require_worklist_or_audit_pattern 'WL-026 .*Closed 2026-03-23.*M12 truth gates'
require_worklist_or_audit_pattern 'M12 .*WL-024, WL-025, WL-026'
require_worklist_or_audit_pattern 'TG-M12-PL .* done '
require_worklist_or_audit_pattern 'TG-M12-PL .*VerifySyncPrefixAcknowledgementAsync.*VerifyStoredLineageAlignment.*VerifyStoredLineageStaleResponsesAsync.*VerifyBootstrapRoleShellEntryPointsAsync.*VerifyQuickActionRejectsCrossRoleAuthorizationAsync'
require_worklist_or_audit_pattern 'TG-M12-GM .* done '
require_worklist_or_audit_pattern 'TG-M12-GM .*VerifyBootstrapRoleShellEntryPointsAsync.*VerifyQuickActionRejectsCrossRoleAuthorizationAsync.*VerifyContinuityClaimRejectsStaleLineageWithoutMutationAsync.*VerifyObserveReturnsLineageSafeContinuityAsync'
require_worklist_or_audit_pattern 'TG-M12-RP .* done '
require_worklist_or_audit_pattern 'TG-M12-RP .*docs/PLAY_RELEASE_SIGNOFF.md.*scripts/ai/verify.sh'
require_worklist_or_audit_pattern '2026-03-23: closed `WL-026`.*feedback/2026-03-21-204029-audit-task-2652.md.*feedback/2026-03-21-204029-audit-task-48734.md'
require_worklist_or_audit_pattern 'WL-009 .* done .*bootstrap'
require_worklist_or_audit_pattern 'WL-010 .* done .*BrowserSessionApiClient'
require_worklist_or_audit_pattern 'WL-011 .* done .*BrowserSessionEventLogStore'
rg -n 'published-feed cutover for `Chummer.Play.Contracts` and `Chummer.Ui.Kit`' docs/chummer-play.design.v1.md >/dev/null
rg -n 'Milestone 4 dedicated play API ownership: `WL-004` aligns the contract family and `WL-012` owns the executable `/api/play/projection`, `/api/play/reconnect`, and `/api/play/sync` route surface' docs/chummer-play.design.v1.md >/dev/null
rg -n 'Milestone 6 offline cache and local-first replay ownership: `WL-005` remains the sync/storage umbrella, `WL-011` owns browser-backed event-ledger persistence, and `WL-013` owns runtime bundle lineage, replay checkpoints, and resume metadata in browser storage' docs/chummer-play.design.v1.md >/dev/null
rg -n 'Milestone 8 installable PWA hardening: `WL-007` now closes the installability umbrella with media-cache lifecycle hardening, while `WL-014` owns the concrete manifest, baseline service-worker cache policy, quota/backpressure handling, and deep-link resume work' docs/chummer-play.design.v1.md >/dev/null
rg -n 'resume/reconnect continuity and runtime-bundle lineage coherence' docs/chummer-play.design.v1.md >/dev/null
rg -n 'BrowserSessionOfflineCacheService.*owns runtime bundle cache metadata' docs/offline-storage.md >/dev/null
rg -n '/api/play/projection.* /api/play/reconnect.* /api/play/sync|/api/play/bootstrap.*role-aware shell bootstrap' docs/sync-model.md >/dev/null
rg -n 'PlayApiRoutes\.Projection' src/Chummer.Play.Web >/dev/null
rg -n 'PlayApiRoutes\.Reconnect' src/Chummer.Play.Web >/dev/null
rg -n 'PlayApiRoutes\.ContinuityClaim' src/Chummer.Play.Web >/dev/null
rg -n 'PlayApiRoutes\.Observe' src/Chummer.Play.Web >/dev/null
rg -n 'PlayApiRoutes\.Sync' src/Chummer.Play.Web >/dev/null
rg -n 'PlayApiRoutes\.Resume' src/Chummer.Play.Web >/dev/null
rg -n 'PlayApiRoutes\.CachePressure' src/Chummer.Play.Web >/dev/null
rg -n 'PlayCampaignWorkspaceLiteProjector|PlayCampaignWorkspaceLiteProjection' src/Chummer.Play.Core src/Chummer.Play.Web >/dev/null
rg -n '/api/play/workspace-lite/\{sessionId\}' src/Chummer.Play.Web/PlayWebApplication.cs >/dev/null
rg -n 'AddSingleton<IRoamingWorkspaceSyncPlanner, RoamingWorkspaceSyncPlanner>' src/Chummer.Play.Web/PlayWebApplication.cs >/dev/null
rg -n 'RoamingWorkspaceRestorePlan|CreatePlan\(WorkspaceRestoreProjection restore, string targetDeviceId\)|ResumeSummary|SafeNextAction|RuleEnvironmentSummary|ReturnTargetCampaignName|AttentionItems' src/Chummer.Play.Core/Roaming/RoamingWorkspaceSyncPlanner.cs >/dev/null
rg -n 'ResumeSummary|SafeNextAction|RuleEnvironmentSummary|ReturnTargetCampaignName|AttentionItems' src/Chummer.Play.RegressionChecks/Program.cs >/dev/null
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
rg -n 'VerifyIndexShellAccessibilityContractAsync\(' src/Chummer.Play.RegressionChecks/Program.cs >/dev/null
rg -n 'VerifyBootstrapRoleShellEntryPointsAsync\(' src/Chummer.Play.RegressionChecks/Program.cs >/dev/null
rg -n 'VerifyQuickActionRejectsCrossRoleAuthorizationAsync\(' src/Chummer.Play.RegressionChecks/Program.cs >/dev/null
rg -n 'VerifyCachePressureBudgetContractAsync\(' src/Chummer.Play.RegressionChecks/Program.cs >/dev/null
rg -n 'ClaimContinuityAsync\(' src/Chummer.Play.Web/BrowserSessionApiClient.cs >/dev/null
rg -n 'ObserveAsync\(' src/Chummer.Play.Web/BrowserSessionApiClient.cs >/dev/null
rg -n 'VerifyStoredLineageAlignment\(' src/Chummer.Play.RegressionChecks/Program.cs >/dev/null
rg -n 'VerifyCampaignWorkspaceLiteProjectionPromotesContinuitySummary\(' src/Chummer.Play.RegressionChecks/Program.cs >/dev/null
rg -n 'VerifyRoamingWorkspaceRestorePlanRestoresPackageOwnedCampaignState\(' src/Chummer.Play.RegressionChecks/Program.cs >/dev/null
rg -n 'VerifyRoamingWorkspaceRestorePlanPreservesConflictAndInstallLocalGuardrails\(' src/Chummer.Play.RegressionChecks/Program.cs >/dev/null
rg -n 'IsLedgerAligned\(' src/Chummer.Play.Web/SessionLineage.cs >/dev/null
rg -n 'VerifyIndexShellAccessibilityContractAsync|VerifyBootstrapRoleShellEntryPointsAsync|VerifyCachePressureBudgetContractAsync|RuntimeBundleQuota == 8|<html lang="en">' docs/PLAY_RELEASE_SIGNOFF.md >/dev/null
rg -n 'Post-closure completion criteria \(M12\)' docs/PLAY_RELEASE_SIGNOFF.md >/dev/null
rg -n 'Player shell criteria: browser transport \+ event-log \+ offline resume stay lineage-safe' docs/PLAY_RELEASE_SIGNOFF.md >/dev/null
rg -n 'GM shell criteria: GM-only action and Spider-card capability gates remain enforced' docs/PLAY_RELEASE_SIGNOFF.md >/dev/null
rg -n 'Release-proof cadence criteria: each closure slice must keep these criteria represented in `WORKLIST.md` \(`TG-M12-PL`, `TG-M12-GM`, `TG-M12-RP`\)' docs/PLAY_RELEASE_SIGNOFF.md >/dev/null
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
rg -n 'PlayCachePressureSnapshot' src >/dev/null
rg -n 'PlayResumeResponse' src >/dev/null
rg -n 'Chummer\.Campaign\.Contracts' README.md >/dev/null
rg -n '"start_url": "/index.html"' src/Chummer.Play.Web/wwwroot/manifest.webmanifest >/dev/null
rg -n 'navigator\.serviceWorker\.register' src/Chummer.Play.Web/wwwroot/index.html >/dev/null
rg -n 'role="status" aria-live="polite" aria-atomic="true"' src/Chummer.Play.Web/wwwroot/index.html >/dev/null
rg -n 'CACHE_VERSION' src/Chummer.Play.Web/wwwroot/service-worker.js >/dev/null
rg -n 'MEDIA_CACHE' src/Chummer.Play.Web/wwwroot/service-worker.js >/dev/null
rg -n 'MEDIA_MAX_ENTRIES' src/Chummer.Play.Web/wwwroot/service-worker.js >/dev/null
rg -n 'pruneMediaCache' src/Chummer.Play.Web/wwwroot/service-worker.js >/dev/null
rg -n 'QuotaExceededError|NS_ERROR_DOM_QUOTA_REACHED' src/Chummer.Play.Web/wwwroot/service-worker.js >/dev/null
rg -n 'request\.Cursor\.Session' src/Chummer.Play.Web >/dev/null
rg -n 'IBrowserKeyValueStore' src/Chummer.Play.Web/BrowserSessionEventLogStore.cs >/dev/null
rg -n 'GetFromJsonAsync<PlaySessionProjection>' src/Chummer.Play.Web/BrowserSessionApiClient.cs >/dev/null
rg -n 'request\.Session\.SessionId' src/Chummer.Play.Web/BrowserSessionApiClient.cs >/dev/null
rg -n 'PostAsJsonAsync\(PlayApiRoutes\.Reconnect' src/Chummer.Play.Web/BrowserSessionApiClient.cs >/dev/null
rg -n 'PostAsJsonAsync\(PlayApiRoutes\.Sync' src/Chummer.Play.Web/BrowserSessionApiClient.cs >/dev/null

if rg -n 'public (sealed )?record (EngineSessionEnvelope|EngineSessionCursor|PlayBootstrapRequest|PlayBootstrapResponse|PlaySessionProjection|PlayReconnectRequest|PlayReconnectResponse|PlayContinuityClaimRequest|PlayContinuityClaimResponse|PlayObserveResponse|PlaySyncRequest|PlaySyncResponse|PlayQuickActionRequest|PlayQuickActionResponse|PlayRuntimeBundleMetadata|PlayCachePressureSnapshot|PlayResumeResponse|BrowserSessionShellProbe|SyncCheckpoint|OfflineLedgerEnvelope)|public enum PlaySurfaceRole' src -g '*.cs' >/dev/null 2>&1; then
  echo "play transport and checkpoint DTOs must come from canonical packages, not repo-local source" >&2
  exit 1
fi

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
python3 scripts/materialize_mobile_local_release_proof.py >/dev/null
test -f .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json
rg -n '"contract_name": "chummer6-mobile.local_release_proof"' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n '"status": "passed"' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null

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
  python3 scripts/materialize_mobile_local_release_proof.py >/dev/null
else
  echo "published-feed compatibility restore/build checks skipped (set CHUMMER_PUBLISHED_FEED_SOURCES to enable)"
  echo "running local owner-package compatibility smoke check"
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
  python3 scripts/materialize_mobile_local_release_proof.py >/dev/null
fi

echo "chummer6-mobile verify ok"
