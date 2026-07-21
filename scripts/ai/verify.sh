#!/usr/bin/env bash
set -euo pipefail

export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-/tmp/dotnet-chummer6-mobile}"
export HOME="${HOME:-/tmp}"
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
package_plane_runner="${repo_root}/scripts/ai/with-package-plane.sh"
published_feed_sources="${CHUMMER_PUBLISHED_FEED_SOURCES:-}"
verification_mode="${CHUMMER_VERIFY_MODE:-slice}"
allow_stub_packages="${CHUMMER_ALLOW_STUB_PACKAGES:-}"
verification_run_id="${CHUMMER_VERIFY_RUN_ID:-}"

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
if [[ -z "${verification_run_id}" ]]; then
  verification_run_id="$(python3 -c 'import secrets; print(secrets.token_hex(16))')"
fi
export CHUMMER_VERIFY_RUN_ID="${verification_run_id}"

verification_skips=()
verification_receipt_status="in_progress"
verification_receipt_finalized=false
write_verification_mode_receipt() {
  local args=(
    scripts/ai/write_verification_mode_receipt.py
    --mode "${verification_mode}"
    --status "${verification_receipt_status}"
    --stub-packages-allowed "${allow_stub_packages}"
    --verification-run-id "${verification_run_id}"
  )
  local skip_reason
  for skip_reason in "${verification_skips[@]}"; do
    args+=(--skip "${skip_reason}")
  done
  python3 "${args[@]}" >/dev/null
}
finish_verification_receipt() {
  local exit_code=$?
  trap - EXIT
  if [[ "${exit_code}" -eq 0 ]]; then
    if [[ "${verification_receipt_finalized}" != true ]]; then
      verification_receipt_status="pass"
      write_verification_mode_receipt || true
    fi
  else
    verification_receipt_status="fail"
    write_verification_mode_receipt || true
  fi
  exit "${exit_code}"
}
skip_or_fail() {
  local reason="$1"
  verification_skips+=("${reason}")
  if [[ "${verification_mode}" == "release" ]]; then
    echo "release verification cannot skip: ${reason}" >&2
    exit 1
  fi
  echo "SKIP: ${reason}" >&2
}

cd "${repo_root}"
trap finish_verification_receipt EXIT
write_verification_mode_receipt
if [[ "${verification_mode}" == "release" && -z "${published_feed_sources}" ]]; then
  skip_or_fail "published-feed compatibility restore/build checks (set CHUMMER_PUBLISHED_FEED_SOURCES)"
fi

mobile_cross_surface_refreshed=0
materialize_mobile_release_proof() {
  if [[ "${mobile_cross_surface_refreshed}" -eq 0 ]]; then
    python3 scripts/materialize_mobile_cross_surface_readiness.py >/dev/null
    mobile_cross_surface_refreshed=1
  fi
  python3 scripts/materialize_mobile_release_boundary.py >/dev/null
  python3 scripts/materialize_mobile_local_release_proof.py >/dev/null
}

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
test -f docs/next90-m112-mobile-campaign-continuity.proof.md
test -f docs/next90-m119-mobile-onboarding-continuity.proof.md
test -f docs/next90-m121-mobile-live-combat-confidence.proof.md
test -f docs/next90-m122-mobile-runner-goal-updates-and-consequence-feed.proof.md
test -f docs/next90-m145-mobile-quick-explain-and-follow-up.proof.md
test -f feedback/2026-03-10-public-repo-graph-audit.md
test -f src/Chummer.Play.Web/Program.cs
test -f src/Chummer.Play.Web/PlayWebApplication.cs
test -f src/Chummer.Play.Core/Application/PlayTurnCompanionProjector.cs
test -f src/Chummer.Play.Web/Components/Pages/MobileTurnCompanionPage.razor
test -f src/Chummer.Play.Web/Components/Pages/MobileCampaignCollaborationPage.razor
test -f src/Chummer.Play.Web/wwwroot/mobile-campaign.js
test -f src/Chummer.Play.Web/Components/App.razor
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
test -f src/Chummer.Play.Web/wwwroot/manifest.player.webmanifest
test -f src/Chummer.Play.Web/wwwroot/manifest.gm.webmanifest
test -f src/Chummer.Play.Web/wwwroot/service-worker.js
test -f src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js
test -f src/Chummer.Play.Web/wwwroot/mobile.css
test -f src/Chummer.Play.Web/wwwroot/icons/apple-touch-icon.png
test -f src/Chummer.Play.Web/wwwroot/icons/icon-192.png
test -f src/Chummer.Play.Web/wwwroot/icons/icon-512.png
test -f src/Chummer.Play.Web/wwwroot/icons/icon-192.svg
test -f src/Chummer.Play.Web/wwwroot/icons/icon-512.svg
test -f scripts/cleanup_mobile_disposable_artifacts.py
test -f scripts/materialize_mobile_local_release_proof.py
test -f scripts/materialize_mobile_cross_surface_readiness.py
test -f scripts/materialize_mobile_release_boundary.py
test -f scripts/release/verify_mobile_release_proof.sh
test -f scripts/run_mobile_strict_public_edge_follow_through.py
test -f scripts/verify_mobile_pwa_runtime_smoke.py
test -f scripts/verify_mobile_pwa_viewport_smoke.py
test -f scripts/verify_mobile_pwa_analytics_smoke.py
test -f scripts/verify_mobile_pwa_performance_budget.py
test -f scripts/verify_next90_m112_mobile_campaign_continuity.py
test -f scripts/verify_next90_m119_mobile_onboarding_continuity.py
test -f scripts/verify_next90_m121_mobile_live_combat_confidence.py
test -f scripts/verify_next90_m122_mobile_runner_goal_updates.py
test -f scripts/verify_next90_m145_mobile_quick_explain_and_follow_up.py
test -f scripts/ai/repair_design_mirror.sh
test -f scripts/ai/with-package-plane.sh
test -f scripts/ai/write_verification_mode_receipt.py
test -f scripts/ai/verify_design_mirror.py
test -f tests/test_with_package_plane_locking.py
python3 -m unittest discover -s tests -p 'test_with_package_plane_locking.py' >/dev/null
test -f tests/test_verification_modes.py
python3 -m unittest discover -s tests -p 'test_verification_modes.py' >/dev/null
test -f tests/test_cleanup_mobile_disposable_artifacts.py
python3 -m unittest discover -s tests -p 'test_cleanup_mobile_disposable_artifacts.py' >/dev/null
test -f tests/test_mobile_strict_public_edge_follow_through.py
python3 -m unittest discover -s tests -p 'test_mobile_strict_public_edge_follow_through.py' >/dev/null
test -f tests/test_mobile_release_boundary.py
python3 -m unittest discover -s tests -p 'test_mobile_release_boundary.py' >/dev/null
test -f tests/test_mobile_pwa_performance_budget.py
test -f tests/test_mobile_campaign_collaboration_runtime.py
python3 -m unittest discover -s tests -p 'test_mobile_pwa_performance_budget.py' >/dev/null
python3 scripts/verify_mobile_pwa_performance_budget.py >/dev/null
test -f .codex-studio/published/MOBILE_PWA_PERFORMANCE_BUDGET.generated.json
rg -n '"contract_name": "chummer_play.mobile_pwa_performance_budget.v1"' .codex-studio/published/MOBILE_PWA_PERFORMANCE_BUDGET.generated.json >/dev/null
rg -n '"status": "pass"' .codex-studio/published/MOBILE_PWA_PERFORMANCE_BUDGET.generated.json >/dev/null
test -f src/Chummer.Play.RegressionChecks/Chummer.Play.RegressionChecks.csproj
test -f src/Chummer.Play.RegressionChecks/Program.cs
test -f src/Chummer.Play.Core/Chummer.Play.Core.csproj
test -f src/Chummer.Play.Core/Roaming/RoamingWorkspaceSyncPlanner.cs
test -f src/Chummer.Play.Components/Chummer.Play.Components.csproj
test -f src/Chummer.Play.Player/Chummer.Play.Player.csproj
test -f src/Chummer.Play.Gm/Chummer.Play.Gm.csproj
test -f eng/package-stubs/EngineContractsStub/EngineContractsStub.csproj
test -f eng/package-stubs/CampaignContractsStub/CampaignContractsStub.csproj
test -f eng/package-stubs/ControlContractsStub/ControlContractsStub.csproj
test -f eng/package-stubs/PlayContractsStub/PlayContractsStub.csproj
test -f eng/package-stubs/UiKitStub/UiKitStub.csproj

python3 scripts/ai/verify_design_mirror.py >/dev/null

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

if rg -n '\\b(class|record)\\s+(TokenCanon|ThemeCompiler|ShellChrome|AccessibilityState|Banner|StaleStateBadge|ApprovalChip|OfflineBanner|DenseTableHeader|DenseRowMetadata|ExplainChip|SpiderStatusCard|ArtifactStatusCard|GuidanceState|LongRunningActionControls)\\b|\\b(static\\s+)?UiAdapterPayload\\s+Adapt(ShellChrome|AccessibilityState|Banner|StaleStateBadge|ApprovalChip|OfflineBanner|DenseTableHeader|DenseRowMetadata|ExplainChip|SpiderStatusCard|ArtifactStatusCard|GuidanceState|LongRunningActionControls)\\s*\\(' src -g '*.cs' >/dev/null 2>&1; then
  echo "source-copied ui-kit token/theme/shell/accessibility/guidance/action-control primitives are not allowed in chummer6-mobile" >&2
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

if rg -n "Chummer\\.Control\\.Contracts(\\.Support)?|ChummerControlContractsPackage(Id|Version)" src docs README.md AGENTS.md WORKLIST.md src/Chummer.Play.Core/Chummer.Play.Core.csproj >/dev/null 2>&1; then
  echo "mobile must not consume Chummer.Control.Contracts directly; keep support/control truth projected through play-safe summaries instead" >&2
  exit 1
fi

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
require_worklist_or_audit_pattern 'TG-M12-PL .*VerifySyncPrefixAcknowledgementAsync.*VerifyStoredLineageAlignment.*VerifyStoredLineageStaleResponsesAsync.*VerifyBootstrapRoleShellEntryPointsAsync.*VerifyQuickActionRejectsCrossRoleAuthorizationAsync.*VerifyDeniedQuickActionsPreserveStoredReplayStateAsync'
require_worklist_or_audit_pattern 'TG-M12-GM .* done '
require_worklist_or_audit_pattern 'TG-M12-GM .*VerifyBootstrapRoleShellEntryPointsAsync.*VerifyQuickActionRejectsCrossRoleAuthorizationAsync.*VerifyDeniedQuickActionsPreserveStoredReplayStateAsync.*VerifyContinuityClaimRejectsStaleLineageWithoutMutationAsync.*VerifyObserveReturnsLineageSafeContinuityAsync'
require_worklist_or_audit_pattern 'TG-M12-OB .* done '
require_worklist_or_audit_pattern 'TG-M12-OB .*VerifyObserverBootstrapAndResumeStayReadMostlyAsync.*VerifyRoleBoundarySurvivesCapabilityLeakageAsync.*VerifyDeniedQuickActionsPreserveStoredReplayStateAsync'
require_worklist_or_audit_pattern 'TG-M12-RP .* done '
require_worklist_or_audit_pattern 'TG-M12-RP .*docs/PLAY_RELEASE_SIGNOFF.md.*scripts/ai/verify.sh'
require_worklist_or_audit_pattern 'WL-031 .* done .*post-M12 local-first sync/installable hardening depth'
require_worklist_or_audit_pattern 'M13 .*done .*TG-M13-.*gates.*scripts/ai/verify.sh'
require_worklist_or_audit_pattern 'TG-M13-OQ .* done '
require_worklist_or_audit_pattern 'TG-M13-OQ .*VerifyOfflineQueueRejectsMalformedSessionEnvelopeAsync.*VerifyOfflineQueueRejectsStaleLineageAsync.*VerifySyncPrefixAcknowledgementAsync'
require_worklist_or_audit_pattern 'TG-M13-IP .* done '
require_worklist_or_audit_pattern 'TG-M13-IP .*VerifyCachePressureBudgetContractAsync.*VerifyCachePressureDecisionNoticeUsesSupportNextActionCopy.*VerifyCampaignWorkspaceLiteProjectionPromotesContinuitySummary.*VerifyIndexShellBindsContextualActionLabelsAsync'
require_worklist_or_audit_pattern 'TG-M13-RF .* done '
require_worklist_or_audit_pattern 'TG-M13-RF .*VerifyResumePreservesLedgerWhenRuntimeBundleDriftsAsync.*VerifyResumeNormalizesCheckpointToLedgerLineageAsync.*VerifyReconnectLineageTransitionContinuityAsync.*VerifyResumeAndWorkspaceLiteRoutesStayRoleConcreteAsync'
require_worklist_or_audit_pattern 'TG-M13-RP .* done '
require_worklist_or_audit_pattern 'TG-M13-RP .*docs/PLAY_RELEASE_SIGNOFF.md.*scripts/ai/verify.sh.*scripts/materialize_mobile_local_release_proof.py'
require_worklist_or_audit_pattern 'WL-032 .* done .*deeper player-vs-GM product completion'
require_worklist_or_audit_pattern 'M14 .*done .*TG-M14-.*gates.*scripts/ai/verify.sh'
require_worklist_or_audit_pattern 'TG-M14-PL .* done '
require_worklist_or_audit_pattern 'TG-M14-PL .*VerifyRoleBoundarySurvivesCapabilityLeakageAsync.*VerifyQuickActionRejectsCrossRoleAuthorizationAsync.*VerifyDeniedQuickActionsPreserveStoredReplayStateAsync.*VerifyCampaignWorkspaceLiteProjectionPromotesContinuitySummary'
require_worklist_or_audit_pattern 'TG-M14-GM .* done '
require_worklist_or_audit_pattern 'TG-M14-GM .*VerifyBootstrapRoleShellEntryPointsAsync.*VerifyRoleBoundarySurvivesCapabilityLeakageAsync.*VerifyResumeAndWorkspaceLiteRoutesStayRoleConcreteAsync.*VerifyCampaignWorkspaceLiteProjectionPreservesObserverAndGmRoleDepth'
require_worklist_or_audit_pattern 'TG-M14-OB .* done '
require_worklist_or_audit_pattern 'TG-M14-OB .*VerifyObserverBootstrapAndResumeStayReadMostlyAsync.*VerifyResumeAndWorkspaceLiteRoutesStayRoleConcreteAsync.*VerifyCampaignWorkspaceLiteProjectionPreservesObserverAndGmRoleDepth.*VerifyIndexShellBindsContextualActionLabelsAsync'
require_worklist_or_audit_pattern 'TG-M14-RP .* done '
require_worklist_or_audit_pattern 'TG-M14-RP .*docs/PLAY_RELEASE_SIGNOFF.md.*scripts/ai/verify.sh.*WL-032'
require_worklist_or_audit_pattern 'WL-033 .* done .*artifact/publication projection completion work'
require_worklist_or_audit_pattern 'M15 .*done .*publication state, trust ranking, discoverability, lineage'
require_worklist_or_audit_pattern 'TG-M15-AP .* done '
require_worklist_or_audit_pattern 'TG-M15-AP .*VerifyCampaignWorkspaceLiteProjectionPromotesContinuitySummary'
require_worklist_or_audit_pattern 'TG-M15-AP .*publication state, trust ranking, discoverability, lineage, and direct creator-status hrefs'
require_worklist_or_audit_pattern 'TG-M15-MB .* done '
require_worklist_or_audit_pattern 'TG-M15-MB .*publish/admin/moderation ownership out of mobile.*creator-status deep links'
require_worklist_or_audit_pattern 'TG-M15-RP .* done '
require_worklist_or_audit_pattern 'TG-M15-RP .*docs/PLAY_RELEASE_SIGNOFF.md.*scripts/ai/verify.sh.*WL-033'
require_worklist_or_audit_pattern '2026-03-23: closed `WL-026`.*feedback/2026-03-21-204029-audit-task-2652.md.*feedback/2026-03-21-204029-audit-task-48734.md'
require_worklist_or_audit_pattern 'WL-009 .* done .*bootstrap'
require_worklist_or_audit_pattern 'WL-010 .* done .*BrowserSessionApiClient'
require_worklist_or_audit_pattern 'WL-011 .* done .*BrowserSessionEventLogStore'
rg -n 'Publication-safe projection boundary' docs/chummer6-mobile.design.v1.md >/dev/null
rg -n 'show recap-safe and replay-safe artifact shelf summaries' docs/chummer6-mobile.design.v1.md >/dev/null
rg -n 'deep-link into Hub-owned creator publication status or support follow-through' docs/chummer6-mobile.design.v1.md >/dev/null
rg -n 'own publication review state transitions|own moderation decisions or admin tooling|second creator-publication truth' docs/chummer6-mobile.design.v1.md >/dev/null
rg -n 'next90-m117-mobile-artifact-shelf' docs/next90-m117-mobile-artifact-shelf.proof.md >/dev/null
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
rg -n '"/artifacts/\{sessionId\}"' src/Chummer.Play.Web/PlayWebApplication.cs >/dev/null
rg -n 'AddSingleton<IRoamingWorkspaceSyncPlanner, RoamingWorkspaceSyncPlanner>' src/Chummer.Play.Web/PlayWebApplication.cs >/dev/null
rg -n 'RoamingWorkspaceRestorePlan|CreatePlan\(WorkspaceRestoreProjection restore, string targetDeviceId\)|ResumeSummary|SafeNextAction|RuleEnvironmentSummary|ReturnTargetCampaignName|AttentionItems' src/Chummer.Play.Core/Roaming/RoamingWorkspaceSyncPlanner.cs >/dev/null
rg -n 'ResumeSummary|SafeNextAction|RuleEnvironmentSummary|ReturnTargetCampaignName|AttentionItems' src/Chummer.Play.RegressionChecks/Program.cs >/dev/null
rg -n 'BuildOwnerRoute\(sessionId, role\)' src/Chummer.Play.Web/PlayWebApplication.cs >/dev/null
rg -n 'BuildOwnerRoute\(requestSession\.SessionId, request\.Role\)|BuildOwnerRoute\(effectiveSession\.SessionId, request\.Role\)' src/Chummer.Play.Web/PlayRouteHandlers.cs >/dev/null
if rg -n 'DeepLinkOwnerRoute:\s*"/play/\{sessionId\}"|PlayContinuityClaimResponse\([^)]*"/play/\{sessionId\}"' src/Chummer.Play.Web/PlayWebApplication.cs src/Chummer.Play.Web/PlayRouteHandlers.cs >/dev/null 2>&1; then
  echo "templated owner routes are not allowed in live resume/workspace responses" >&2
  exit 1
fi
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
rg -n 'VerifyServiceWorkerKeepsPrivatePlayApiNetworkOnlyAsync\(' src/Chummer.Play.RegressionChecks/Program.cs >/dev/null
rg -n 'VerifyBootstrapRoleShellEntryPointsAsync\(' src/Chummer.Play.RegressionChecks/Program.cs >/dev/null
rg -n 'VerifyQuickActionRejectsCrossRoleAuthorizationAsync\(' src/Chummer.Play.RegressionChecks/Program.cs >/dev/null
rg -n 'VerifyDeniedQuickActionsPreserveStoredReplayStateAsync\(' src/Chummer.Play.RegressionChecks/Program.cs >/dev/null
rg -n 'VerifyCachePressureBudgetContractAsync\(' src/Chummer.Play.RegressionChecks/Program.cs >/dev/null
rg -n 'ClaimContinuityAsync\(' src/Chummer.Play.Web/BrowserSessionApiClient.cs >/dev/null
rg -n 'ObserveAsync\(' src/Chummer.Play.Web/BrowserSessionApiClient.cs >/dev/null
rg -n 'VerifyStoredLineageAlignment\(' src/Chummer.Play.RegressionChecks/Program.cs >/dev/null
rg -n 'VerifyCampaignWorkspaceLiteProjectionPromotesContinuitySummary\(' src/Chummer.Play.RegressionChecks/Program.cs >/dev/null
rg -n 'VerifyRoamingWorkspaceRestorePlanRestoresPackageOwnedCampaignState\(' src/Chummer.Play.RegressionChecks/Program.cs >/dev/null
rg -n 'VerifyRoamingWorkspaceRestorePlanPreservesConflictAndInstallLocalGuardrails\(' src/Chummer.Play.RegressionChecks/Program.cs >/dev/null
rg -n 'IsLedgerAligned\(' src/Chummer.Play.Web/SessionLineage.cs >/dev/null
rg -n 'VerifyIndexShellAccessibilityContractAsync|VerifyBootstrapRoleShellEntryPointsAsync|VerifyCachePressureBudgetContractAsync|RuntimeBundleQuota == 8|<html lang="en">' docs/PLAY_RELEASE_SIGNOFF.md >/dev/null
rg -n 'Mobile turn companion criteria|Bounded turn-state criteria:|Replay-safe continuity criteria:|RUNSITE anchor criteria:' docs/PLAY_RELEASE_SIGNOFF.md >/dev/null
rg -n 'Quick-glance criteria:.*scripts/verify_mobile_pwa_viewport_smoke.py' docs/PLAY_RELEASE_SIGNOFF.md >/dev/null
rg -n 'Installability criteria:.*Page.getInstallabilityErrors.*scripts/verify_mobile_pwa_viewport_smoke.py' docs/PLAY_RELEASE_SIGNOFF.md >/dev/null
rg -n 'Post-closure completion criteria \(M12\)' docs/PLAY_RELEASE_SIGNOFF.md >/dev/null
rg -n 'Player shell criteria: browser transport \+ event-log \+ offline resume stay lineage-safe.*authorization denials preserve stored replay context' docs/PLAY_RELEASE_SIGNOFF.md >/dev/null
rg -n 'GM shell criteria: GM-only action and Spider-card capability gates remain enforced.*preserve stored replay context' docs/PLAY_RELEASE_SIGNOFF.md >/dev/null
rg -n 'Observer shell criteria: bootstrap and resume keep the lane read-mostly.*denied quick-action attempts replay-safe' docs/PLAY_RELEASE_SIGNOFF.md >/dev/null
rg -n 'Release-proof cadence criteria: each closure slice must keep these criteria represented in `WORKLIST.md` \(`TG-M12-PL`, `TG-M12-GM`, `TG-M12-OB`, `TG-M12-RP`\)' docs/PLAY_RELEASE_SIGNOFF.md >/dev/null
rg -n 'Post-closure hardening criteria \(M13\)' docs/PLAY_RELEASE_SIGNOFF.md >/dev/null
rg -n 'Offline queue recovery criteria: stale-lineage and malformed-envelope recovery paths reject unsafe mutations.*VerifyOfflineQueueRejectsMalformedSessionEnvelopeAsync.*VerifyOfflineQueueRejectsStaleLineageAsync.*VerifySyncPrefixAcknowledgementAsync' docs/PLAY_RELEASE_SIGNOFF.md >/dev/null
rg -n 'Installable cache-pressure clarity criteria: cache-pressure budget, caution copy, and shell follow-through bindings remain explicit.*VerifyCachePressureBudgetContractAsync.*VerifyCachePressureDecisionNoticeUsesSupportNextActionCopy.*VerifyCampaignWorkspaceLiteProjectionPromotesContinuitySummary.*VerifyIndexShellBindsContextualActionLabelsAsync' docs/PLAY_RELEASE_SIGNOFF.md >/dev/null
rg -n 'Replay-safe resume fallback criteria: resume/reconnect fallback behavior preserves stored lineage and role-concrete continuity routes.*VerifyResumePreservesLedgerWhenRuntimeBundleDriftsAsync.*VerifyResumeNormalizesCheckpointToLedgerLineageAsync.*VerifyReconnectLineageTransitionContinuityAsync.*VerifyResumeAndWorkspaceLiteRoutesStayRoleConcreteAsync' docs/PLAY_RELEASE_SIGNOFF.md >/dev/null
rg -n 'Release-proof cadence criteria: each hardening slice must keep these criteria represented in `WORKLIST.md` \(`TG-M13-OQ`, `TG-M13-IP`, `TG-M13-RF`, `TG-M13-RP`\)' docs/PLAY_RELEASE_SIGNOFF.md >/dev/null
rg -n 'Post-closure role-depth criteria \(M14\)' docs/PLAY_RELEASE_SIGNOFF.md >/dev/null
rg -n 'Player lane criteria: player guidance and action posture remain role-specific.*VerifyRoleBoundarySurvivesCapabilityLeakageAsync.*VerifyQuickActionRejectsCrossRoleAuthorizationAsync.*VerifyDeniedQuickActionsPreserveStoredReplayStateAsync.*VerifyCampaignWorkspaceLiteProjectionPromotesContinuitySummary' docs/PLAY_RELEASE_SIGNOFF.md >/dev/null
rg -n 'GM lane criteria: GM guidance and operations posture remain separated from player quick-action posture.*VerifyBootstrapRoleShellEntryPointsAsync.*VerifyRoleBoundarySurvivesCapabilityLeakageAsync.*VerifyResumeAndWorkspaceLiteRoutesStayRoleConcreteAsync.*VerifyCampaignWorkspaceLiteProjectionPreservesObserverAndGmRoleDepth' docs/PLAY_RELEASE_SIGNOFF.md >/dev/null
rg -n 'Observer handoff criteria: observer bootstrap/resume remain read-mostly.*VerifyObserverBootstrapAndResumeStayReadMostlyAsync.*VerifyResumeAndWorkspaceLiteRoutesStayRoleConcreteAsync.*VerifyCampaignWorkspaceLiteProjectionPreservesObserverAndGmRoleDepth.*VerifyIndexShellBindsContextualActionLabelsAsync' docs/PLAY_RELEASE_SIGNOFF.md >/dev/null
rg -n 'Release-proof cadence criteria: each role-depth slice must keep these criteria represented in `WORKLIST.md` \(`TG-M14-PL`, `TG-M14-GM`, `TG-M14-OB`, `TG-M14-RP`\)' docs/PLAY_RELEASE_SIGNOFF.md >/dev/null
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
: <<'LEGACY_QUERY_AUTHORITY_MOBILE_CONTRACT'
rg -n '"id": "/mobile"' src/Chummer.Play.Web/wwwroot/manifest.webmanifest >/dev/null
rg -n '"start_url": "/mobile/player"' src/Chummer.Play.Web/wwwroot/manifest.webmanifest >/dev/null
rg -n '"scope": "/mobile/"' src/Chummer.Play.Web/wwwroot/manifest.webmanifest >/dev/null
rg -n '"url": "/mobile/player\?role=Player"' src/Chummer.Play.Web/wwwroot/manifest.webmanifest >/dev/null
rg -n '"url": "/mobile/gm\?role=GameMaster"' src/Chummer.Play.Web/wwwroot/manifest.webmanifest >/dev/null
rg -n '"id": "/mobile/player"' src/Chummer.Play.Web/wwwroot/manifest.player.webmanifest >/dev/null
rg -n '"start_url": "/mobile/player\?role=Player"' src/Chummer.Play.Web/wwwroot/manifest.player.webmanifest >/dev/null
rg -n '"scope": "/mobile/"' src/Chummer.Play.Web/wwwroot/manifest.player.webmanifest >/dev/null
rg -n '"purpose": "any maskable"' src/Chummer.Play.Web/wwwroot/manifest.player.webmanifest >/dev/null
rg -n '"id": "/mobile/gm"' src/Chummer.Play.Web/wwwroot/manifest.gm.webmanifest >/dev/null
rg -n '"start_url": "/mobile/gm\?role=GameMaster"' src/Chummer.Play.Web/wwwroot/manifest.gm.webmanifest >/dev/null
rg -n '"scope": "/mobile/"' src/Chummer.Play.Web/wwwroot/manifest.gm.webmanifest >/dev/null
rg -n '"purpose": "any maskable"' src/Chummer.Play.Web/wwwroot/manifest.gm.webmanifest >/dev/null
rg -n 'apple-mobile-web-app-capable|apple-mobile-web-app-title|rel="apple-touch-icon"' src/Chummer.Play.Web/Components/App.razor >/dev/null
rg -n 'rel="manifest" href="@RoleManifestHref"' src/Chummer.Play.Web/Components/Pages/MobileTurnCompanionPage.razor >/dev/null
rg -n 'manifest\.player\.webmanifest' src/Chummer.Play.Web/Components/Pages/MobileTurnCompanionPage.razor >/dev/null
rg -n 'manifest\.gm\.webmanifest' src/Chummer.Play.Web/Components/Pages/MobileTurnCompanionPage.razor >/dev/null
rg -n 'turn-share-owner-route-button' src/Chummer.Play.Web/Components/Pages/MobileTurnCompanionPage.razor >/dev/null
rg -n 'turn-owner-route-share-status' src/Chummer.Play.Web/Components/Pages/MobileTurnCompanionPage.razor >/dev/null
rg -n 'chummer-play-analytics-config' src/Chummer.Play.Web/Components/Pages/MobileTurnCompanionPage.razor >/dev/null
rg -n '@if \(RybbitAnalyticsEnabled\)' src/Chummer.Play.Web/Components/Pages/MobileTurnCompanionPage.razor >/dev/null
rg -n '=> !string.IsNullOrWhiteSpace\(RybbitAnalyticsSiteId\)' src/Chummer.Play.Web/Components/Pages/MobileTurnCompanionPage.razor >/dev/null
rg -n 'RYBBIT_CHUMMER_PLAY_SITE_ID' src/Chummer.Play.Web/Components/Pages/MobileTurnCompanionPage.razor >/dev/null
rg -n 'RYBBIT_CHUMMER_PLAY_SCRIPT_URL' src/Chummer.Play.Web/Components/Pages/MobileTurnCompanionPage.razor >/dev/null
rg -n 'RYBBIT_CHUMMER_PLAY_ALLOW_SAME_HOST_PROXY' src/Chummer.Play.Web/Components/Pages/MobileTurnCompanionPage.razor >/dev/null
rg -n 'IsAllowedRybbitEndpoint\(parsedScriptUrl\)' src/Chummer.Play.Web/Components/Pages/MobileTurnCompanionPage.razor >/dev/null
rg -n 'endpoint.Scheme == Uri.UriSchemeHttps' src/Chummer.Play.Web/Components/Pages/MobileTurnCompanionPage.razor >/dev/null
rg -n 'endpoint.IsLoopback \|\| string.Equals\(endpoint.Host, "localhost"' src/Chummer.Play.Web/Components/Pages/MobileTurnCompanionPage.razor >/dev/null
rg -n 'SkipPatterns:' src/Chummer.Play.Web/Components/Pages/MobileTurnCompanionPage.razor >/dev/null
rg -n 'MaskPatterns:' src/Chummer.Play.Web/Components/Pages/MobileTurnCompanionPage.razor >/dev/null
rg -n '/mobile/\*\*' src/Chummer.Play.Web/Components/Pages/MobileTurnCompanionPage.razor >/dev/null
rg -n '/api/play/\*\*' src/Chummer.Play.Web/Components/Pages/MobileTurnCompanionPage.razor >/dev/null
rg -n 'var analyticsQueueName = "ChummerPlayAnalyticsQueue";' src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js >/dev/null
rg -n 'function initializeMobileAnalytics\(client, resumeRoute\)' src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js >/dev/null
rg -n 'rybbit\.dataset\.skipPatterns = config\.skipPatterns' src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js >/dev/null
rg -n 'rybbit\.dataset\.maskPatterns = config\.maskPatterns' src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js >/dev/null
rg -n 'rybbit\.dataset\.replayBlockSelector = config\.replayBlockSelector' src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js >/dev/null
rg -n 'function isAnalyticsBlocked\(\)' src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js >/dev/null
rg -n 'window\.doNotTrack === "1"' src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js >/dev/null
rg -n 'navigator\.doNotTrack === "1"' src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js >/dev/null
rg -n 'navigator\.globalPrivacyControl === true' src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js >/dev/null
rg -n 'mobile_shell_open' src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js >/dev/null
rg -n 'mobile_install_prompt_available' src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js >/dev/null
rg -n 'mobile_install_prompt_open' src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js >/dev/null
rg -n 'mobile_install_prompt_choice' src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js >/dev/null
rg -n 'mobile_install_prompt_unavailable' src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js >/dev/null
rg -n 'mobile_role_switch' src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js >/dev/null
rg -n 'var resumeRoute = resolveResumeRoute\(params, requestedRoleName\);' src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js >/dev/null
rg -n 'function resolveResumeRoute\(params, requestedRoleName\)' src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js >/dev/null
rg -n 'var scopedRoleName = String\(requestedRoleName \|\| params\.get\("role"\) \|\| ""\)\.trim\(\);' src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js >/dev/null
rg -n 'function persistClientState\(client\)' src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js >/dev/null
rg -n 'document\.visibilityState === "hidden"' src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js >/dev/null
rg -n 'window\.addEventListener\("pagehide"' src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js >/dev/null
rg -n 'window\.addEventListener\("beforeunload"' src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js >/dev/null
rg -n 'var targetDeviceId = roleSegmentForAnalytics\(roleName\) === roleSegmentForAnalytics\(client\.roleName\)' src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js >/dev/null
rg -n 'if \(deviceId\) \{' src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js >/dev/null
rg -n 'if \(role == Role && !string.IsNullOrWhiteSpace\(ActiveDeviceId\)\)' src/Chummer.Play.Web/Components/Pages/MobileTurnCompanionPage.razor >/dev/null
rg -n 'case "share-owner-route":' src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js >/dev/null
rg -n 'function shareOwnerRoute\(client\)' src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js >/dev/null
rg -n 'navigator\.clipboard\.writeText\(shareUrl\)' src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js >/dev/null
rg -n 'function writeHandoffLink\(shareUrl\)' src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js >/dev/null
rg -n 'function sessionHandoffHref\(ownerRoute, client\)' src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js >/dev/null
rg -n ': readStoredValue\(deviceIdStorageKey\(roleName\)\);' src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js >/dev/null
rg -n 'window\.location\.assign\(href\);' src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js >/dev/null
rg -n '__chummerPlaySuppressRoleNavigation' src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js scripts/verify_mobile_pwa_analytics_smoke.py >/dev/null
rg -n 'window\.ChummerPlayInstallPromptForTest' src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js scripts/verify_mobile_pwa_analytics_smoke.py >/dev/null
rg -n 'window\.ChummerPlayInstallShellForTest' src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js scripts/verify_mobile_pwa_analytics_smoke.py >/dev/null
rg -n 'handoffParams\.set\("sessionId", sessionId\)' src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js >/dev/null
rg -n 'handoffParams\.set\("role", roleName\)' src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js >/dev/null
if rg -n 'handoffParams\.set\("deviceId"' src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js >/dev/null 2>&1; then
  echo "shared session handoff routes must not copy sender deviceId" >&2
  exit 1
fi
rg -n 'mobile_session_handoff_share' src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js >/dev/null
rg -n 'return /session\|device\|token\|continuity\|owner\|secret\|key\|href\|url/i\.test\(key\);' src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js >/dev/null
rg -n 'function isSensitiveAnalyticsValue\(value\)' src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js >/dev/null
rg -n 'isSensitiveAnalyticsValue\(safeValue\)' src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js >/dev/null
rg -n '\(\?:session\|device\|token\|secret\|key\|continuity\|owner\)\[_-\]' src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js >/dev/null
rg -n 'navigator\.serviceWorker\.register' src/Chummer.Play.Web/wwwroot/index.html >/dev/null
rg -n 'role="status" aria-live="polite" aria-atomic="true"' src/Chummer.Play.Web/wwwroot/index.html >/dev/null
rg -n 'min-height: 2.75rem;' src/Chummer.Play.Web/wwwroot/mobile.css >/dev/null
rg -n 'min-width: 2.75rem;' src/Chummer.Play.Web/wwwroot/mobile.css >/dev/null
rg -n 'stepper button' src/Chummer.Play.Web/wwwroot/mobile.css >/dev/null
rg -n 'id="shell-play-action-link"' src/Chummer.Play.Web/wwwroot/index.html >/dev/null
rg -n 'id="shell-hero-action-menu"' src/Chummer.Play.Web/wwwroot/index.html >/dev/null
rg -n 'function normalizePlayRouteForMobileShell\(href, roleFallback, sessionIdFallback, deviceFallback\)' src/Chummer.Play.Web/wwwroot/index.html >/dev/null
rg -n 'function syncHeroActionMenu\(\)' src/Chummer.Play.Web/wwwroot/index.html >/dev/null
rg -n 'function navigateHeroAction\(action\)' src/Chummer.Play.Web/wwwroot/index.html >/dev/null
rg -n 'document\.getElementById\("shell-hero-action-menu"\)\.addEventListener\("change"' src/Chummer.Play.Web/wwwroot/index.html >/dev/null
rg -n 'const deviceId = playParams\.get\("deviceId"\) \|\| deviceFallback \|\| activeShellIdentity\.deviceId \|\| "";' src/Chummer.Play.Web/wwwroot/index.html >/dev/null
rg -n 'playParams\.set\("deviceId", deviceId\);' src/Chummer.Play.Web/wwwroot/index.html >/dev/null
rg -n 'setLink\("shell-play-action-link", "/play", "Play", "/play", "Play"\);' src/Chummer.Play.Web/wwwroot/index.html >/dev/null
rg -n 'const CACHE_VERSION = "play-shell-v16";' src/Chummer.Play.Web/wwwroot/service-worker.js >/dev/null
rg -n 'function cacheMobileNavigationPath\(pathname\)' src/Chummer.Play.Web/wwwroot/service-worker.js >/dev/null
rg -n 'chummer-play-cache-current-route' src/Chummer.Play.Web/wwwroot/service-worker.js >/dev/null
rg -n 'if \(url\.search\) \{' src/Chummer.Play.Web/wwwroot/service-worker.js >/dev/null
rg -n 'event\.waitUntil\(cacheMobileNavigationPath\(url\.pathname\)\.catch\(\(\) => undefined\)\);' src/Chummer.Play.Web/wwwroot/service-worker.js >/dev/null
rg -n 'event\.waitUntil\(cacheMobileNavigationResponse\(url\.pathname, response\.clone\(\)\)\.catch\(\(\) => undefined\)\);' src/Chummer.Play.Web/wwwroot/service-worker.js >/dev/null
! rg -n '  "/index\.html",' src/Chummer.Play.Web/wwwroot/service-worker.js >/dev/null
rg -n '"/_framework/blazor\.web\.js"' src/Chummer.Play.Web/wwwroot/service-worker.js >/dev/null
rg -n 'AddInteractiveServerComponents' src/Chummer.Play.Web/PlayWebApplication.cs >/dev/null
rg -n 'AddInteractiveServerRenderMode' src/Chummer.Play.Web/PlayWebApplication.cs >/dev/null
rg -n 'StaticWebAssetsLoader\.UseStaticWebAssets' src/Chummer.Play.Web/PlayWebApplication.cs >/dev/null
rg -n 'app\.MapStaticAssets\(\);' src/Chummer.Play.Web/PlayWebApplication.cs >/dev/null
rg -n 'script src="/_framework/blazor\.web\.js"' src/Chummer.Play.Web/Components/App.razor >/dev/null
rg -n 'data-blazor-shell="interactive-server"' src/Chummer.Play.Web/Components/Pages/MobileTurnCompanionPage.razor >/dev/null
rg -n 'data-enhance-nav="false"' src/Chummer.Play.Web/Components/Pages/MobileTurnCompanionPage.razor >/dev/null
rg -n 'page\.wait_for_selector\("#workspace-shell:not\(\[hidden\]\)", timeout=NAVIGATION_TIMEOUT_MS\)' scripts/verify_mobile_pwa_runtime_smoke.py >/dev/null
rg -n 'page\.wait_for_selector\("\[data-turn-root\]\[data-blazor-shell='\''interactive-server'\''\]", timeout=NAVIGATION_TIMEOUT_MS\)' scripts/verify_mobile_pwa_runtime_smoke.py >/dev/null
rg -n 'page\.wait_for_selector\("script\[src='\''/_framework/blazor\.web\.js'\''\]", state="attached", timeout=NAVIGATION_TIMEOUT_MS\)' scripts/verify_mobile_pwa_runtime_smoke.py >/dev/null
rg -n 'const MANAGED_CACHE_PREFIXES = \[' src/Chummer.Play.Web/wwwroot/service-worker.js >/dev/null
rg -n 'function isManagedPlayCache\(cacheName\)' src/Chummer.Play.Web/wwwroot/service-worker.js >/dev/null
rg -n '\.filter\(\(key\) => isManagedPlayCache\(key\) && !\[SHELL_CACHE, MEDIA_CACHE, MEDIA_META_CACHE\]\.includes\(key\)\)' src/Chummer.Play.Web/wwwroot/service-worker.js >/dev/null
rg -n 'MEDIA_CACHE' src/Chummer.Play.Web/wwwroot/service-worker.js >/dev/null
rg -n 'MEDIA_MAX_ENTRIES' src/Chummer.Play.Web/wwwroot/service-worker.js >/dev/null
rg -n 'pruneMediaCache' src/Chummer.Play.Web/wwwroot/service-worker.js >/dev/null
rg -n 'QuotaExceededError|NS_ERROR_DOM_QUOTA_REACHED' src/Chummer.Play.Web/wwwroot/service-worker.js >/dev/null
rg -n 'play_api_network_unavailable' src/Chummer.Play.Web/wwwroot/service-worker.js >/dev/null
rg -n 'const NON_CACHEABLE_PATHS = new Set\(\[' src/Chummer.Play.Web/wwwroot/service-worker.js >/dev/null
rg -n '"/mobile/pwa/ledger\.json"' src/Chummer.Play.Web/wwwroot/service-worker.js >/dev/null
rg -n 'const NON_CACHEABLE_PATH_PREFIXES = \[' src/Chummer.Play.Web/wwwroot/service-worker.js >/dev/null
rg -n '"/account"' src/Chummer.Play.Web/wwwroot/service-worker.js >/dev/null
rg -n '"/api"' src/Chummer.Play.Web/wwwroot/service-worker.js >/dev/null
rg -n 'function isNonCacheableRequest\(url\)' src/Chummer.Play.Web/wwwroot/service-worker.js >/dev/null
rg -n 'play_public_route_network_unavailable' src/Chummer.Play.Web/wwwroot/service-worker.js >/dev/null
if rg -n 'API_CACHE|cacheWithQuotaHandling\(API_CACHE|caches\.open\(API_CACHE\)' src/Chummer.Play.Web/wwwroot/service-worker.js >/dev/null 2>&1; then
  echo "private /api/play responses must not use Cache API storage" >&2
  exit 1
fi
LEGACY_QUERY_AUTHORITY_MOBILE_CONTRACT

# Public install routes are role labels only. The distinct live document is
# server-grant backed and neither query strings nor PWA shortcuts confer access.
rg -n '"id": "/mobile"' src/Chummer.Play.Web/wwwroot/manifest.webmanifest >/dev/null
rg -n '"start_url": "/mobile/player"' src/Chummer.Play.Web/wwwroot/manifest.webmanifest >/dev/null
rg -n '"url": "/mobile/player"' src/Chummer.Play.Web/wwwroot/manifest.webmanifest >/dev/null
rg -n '"url": "/mobile/gm"' src/Chummer.Play.Web/wwwroot/manifest.webmanifest >/dev/null
rg -n '"url": "/mobile/observer"' src/Chummer.Play.Web/wwwroot/manifest.webmanifest >/dev/null
for role in player gm observer; do
  manifest="src/Chummer.Play.Web/wwwroot/manifest.${role}.webmanifest"
  rg -n "\"id\": \"/mobile/${role}\"" "${manifest}" >/dev/null
  rg -n "\"start_url\": \"/mobile/${role}\"" "${manifest}" >/dev/null
  rg -n '"scope": "/mobile/"' "${manifest}" >/dev/null
  rg -n '"purpose": "any maskable"' "${manifest}" >/dev/null
  if rg -n 'sessionId=|deviceId=|\?role=' "${manifest}" >/dev/null 2>&1; then
    echo "public ${role} manifest must not encode live authority" >&2
    exit 1
  fi
done
rg -n '@page "/mobile"|@page "/mobile/player"|@page "/mobile/gm"|@page "/mobile/observer"' src/Chummer.Play.Web/Components/Pages/MobileTurnCompanionPage.razor >/dev/null
rg -n 'data-play-surface="install-only"' src/Chummer.Play.Web/Components/Pages/MobileTurnCompanionPage.razor >/dev/null
rg -n 'data-authority="none"' src/Chummer.Play.Web/Components/Pages/MobileTurnCompanionPage.razor >/dev/null
rg -n 'mobile-install-shell\.js' src/Chummer.Play.Web/Components/Pages/MobileTurnCompanionPage.razor >/dev/null
if rg -n 'data-turn-root|mobile-turn-companion\.js|sessionId|deviceId' src/Chummer.Play.Web/Components/Pages/MobileTurnCompanionPage.razor >/dev/null 2>&1; then
  echo "public mobile install pages must not render live-session state" >&2
  exit 1
fi
rg -n '@page "/mobile/live"' src/Chummer.Play.Web/Components/Pages/MobileLiveTurnCompanionPage.razor >/dev/null
rg -n 'data-session-grant-backed="true"' src/Chummer.Play.Web/Components/Pages/MobileLiveTurnCompanionPage.razor >/dev/null
rg -n 'PlaySessionGrantPolicy\.ResolveCurrent' src/Chummer.Play.Web/Components/Pages/MobileLiveTurnCompanionPage.razor >/dev/null
rg -n 'PlayRouteHandlers\.BuildMobileOwnerRoute\(\)' src/Chummer.Play.Web/Components/Pages/MobileLiveTurnCompanionPage.razor >/dev/null
rg -n 'chummer-play-analytics-config|RYBBIT_CHUMMER_PLAY_SITE_ID|RYBBIT_CHUMMER_PLAY_SCRIPT_URL' src/Chummer.Play.Web/Components/Pages/MobileLiveTurnCompanionPage.razor >/dev/null
rg -n 'RequireTrustedMobileLiveGrantBoundaryAsync|PlaySessionGrantPolicy\.TryResolve' src/Chummer.Play.Web/PlayWebApplication.cs >/dev/null
rg -n 'play_session_grant_required' src/Chummer.Play.Web/PlayWebApplication.cs >/dev/null
rg -n 'GrantIdHeader|SessionIdHeader|RoleHeader|DeviceIdHeader' src/Chummer.Play.Web/PlaySessionGrant.cs >/dev/null
rg -n '@page "/mobile/campaigns"|@page "/join/campaign/\{InviteId\}"' src/Chummer.Play.Web/Components/Pages/MobileCampaignCollaborationPage.razor >/dev/null
rg -n 'data-private-state="open-tab-only"' src/Chummer.Play.Web/Components/Pages/MobileCampaignCollaborationPage.razor >/dev/null
rg -n 'antiforgeryRoute = "/api/v1/antiforgery"' src/Chummer.Play.Web/wwwroot/mobile-campaign.js >/dev/null
rg -n 'headers\[antiforgery\.headerName\] = antiforgery\.requestToken' src/Chummer.Play.Web/wwwroot/mobile-campaign.js >/dev/null
rg -n 'credentials: "include"' src/Chummer.Play.Web/wwwroot/mobile-campaign.js >/dev/null
rg -n 'state\.inviteSecret = takeInviteSecret\(\)' src/Chummer.Play.Web/wwwroot/mobile-campaign.js >/dev/null
rg -n 'window\.history\.replaceState\(\{\}, "", window\.location\.pathname \+ window\.location\.search\)' src/Chummer.Play.Web/wwwroot/mobile-campaign.js >/dev/null
rg -n 'Canonical Core editing is temporarily unavailable\. Nothing was changed' src/Chummer.Play.Web/wwwroot/mobile-campaign.js >/dev/null
rg -n 'IsMobileCampaignDocumentPath|ApplyPrivateMobileCampaignHeaders' src/Chummer.Play.Web/PlayWebApplication.cs >/dev/null
rg -n 'return "/mobile/live";' src/Chummer.Play.Web/PlayRouteHandlers.cs src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js >/dev/null
rg -n 'void sessionIdFallback;|void deviceFallback;|return `/mobile/\$\{mode\}`;' src/Chummer.Play.Web/wwwroot/index.html >/dev/null
rg -n 'function analyticsRoute\(client, config\)|return "/mobile/live";' src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js >/dev/null
rg -n 'function isAnalyticsBlocked\(\)|navigator\.globalPrivacyControl === true' src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js >/dev/null
rg -n 'const CACHE_VERSION = "v21";' src/Chummer.Play.Web/wwwroot/service-worker.js >/dev/null
rg -n 'const CRITICAL_SHELL_ASSETS = \[' src/Chummer.Play.Web/wwwroot/service-worker.js >/dev/null
rg -n '"/manifest\.observer\.webmanifest"' src/Chummer.Play.Web/wwwroot/service-worker.js >/dev/null
rg -n 'const NON_CACHEABLE_PATH_PREFIXES = \[' src/Chummer.Play.Web/wwwroot/service-worker.js >/dev/null
rg -n '"/api"' src/Chummer.Play.Web/wwwroot/service-worker.js >/dev/null
if rg -n 'CRITICAL_SHELL_ASSETS[\s\S]*mobile-turn-companion\.js|API_CACHE|cacheWithQuotaHandling\(API_CACHE|caches\.open\(API_CACHE\)' src/Chummer.Play.Web/wwwroot/service-worker.js >/dev/null 2>&1; then
  echo "private live state and APIs must not enter the public Cache API contract" >&2
  exit 1
fi
rg -n '<RequiresAspNetWebAssets>false</RequiresAspNetWebAssets>' src/Chummer.Play.Web/Chummer.Play.Web.csproj >/dev/null
if rg -n 'StaticWebAssetsLoader\.UseStaticWebAssets|app\.MapStaticAssets\(\)' src/Chummer.Play.Web/PlayWebApplication.cs >/dev/null 2>&1; then
  echo "the hermetic web build must not require the unsupported static-web-assets pack" >&2
  exit 1
fi
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
python3 -m pytest -q tests/test_mobile_campaign_collaboration_runtime.py >/dev/null
python3 scripts/verify_mobile_pwa_analytics_smoke.py >/dev/null
python3 scripts/verify_mobile_pwa_runtime_smoke.py >/dev/null
python3 scripts/verify_mobile_pwa_viewport_smoke.py >/dev/null
python3 scripts/cleanup_mobile_disposable_artifacts.py >/dev/null
python3 scripts/run_mobile_strict_public_edge_follow_through.py >/dev/null
test -f .codex-studio/published/MOBILE_STRICT_PUBLIC_EDGE_FOLLOW_THROUGH.generated.json
rg -n '"contract_name": "chummer6-mobile.strict_public_edge_follow_through.v1"' .codex-studio/published/MOBILE_STRICT_PUBLIC_EDGE_FOLLOW_THROUGH.generated.json >/dev/null
rg -n '"generated_at_utc": "' .codex-studio/published/MOBILE_STRICT_PUBLIC_EDGE_FOLLOW_THROUGH.generated.json >/dev/null
rg -n '"strict_follow_through": \{' .codex-studio/published/MOBILE_STRICT_PUBLIC_EDGE_FOLLOW_THROUGH.generated.json >/dev/null
materialize_mobile_release_proof
python3 scripts/verify_next90_m112_mobile_campaign_continuity.py >/dev/null
python3 scripts/verify_next90_m119_mobile_onboarding_continuity.py >/dev/null
python3 scripts/verify_next90_m121_mobile_live_combat_confidence.py >/dev/null
python3 scripts/verify_next90_m122_mobile_runner_goal_updates.py >/dev/null
python3 scripts/verify_next90_m145_mobile_quick_explain_and_follow_up.py >/dev/null
test -f .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json
rg -n '"contract_name": "chummer6-mobile.local_release_proof"' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n '"status": "passed"' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n '"generated_at": "' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n '"generated_at_utc": "' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n '"source_file_digests": \[' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n '"sha256": "' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n '"journeys_passed": \[' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n '"install_claim_restore_continue"' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n '"campaign_session_recover_recap"' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n '"recover_from_sync_conflict"' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n '"quality_release_hardening"' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n '"pwa_runtime_smoke"' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n '"mobile_pwa_viewport_smoke"' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n '"mobile_pwa_analytics_smoke"' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n '"turn_companion_live_session"' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n '"migration_boundary_evidence"' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n '"mobile_campaign_continuity"' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n '"mobile_onboarding_continuity"' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n '"mobile_live_combat_confidence"' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n '"mobile_runner_goal_updates"' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n '"quick_explain_follow_up"' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n '"required_markers": \{' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'VerifyRoamingWorkspaceRestorePlanRestoresPackageOwnedCampaignState' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'VerifyRoamingWorkspaceRestorePlanPreservesConflictAndInstallLocalGuardrails' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'VerifyCachePressureDecisionNoticeUsesSupportNextActionCopy' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'VerifyIndexShellAccessibilityContractAsync' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'VerifyCachePressureBudgetContractAsync' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'VerifyTurnCompanionRealHostPipelineUsesAntiforgeryAsync' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'VerifyTurnCompanionRunsiteAnchorSelectionStaysDeviceScopedAsync' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'RUNSITE stays orientation-only here: room, zone, and hotspot anchors are inspectable context, not token authority.' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'mobile_pwa_runtime_smoke ok' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'mobile_pwa_viewport_smoke ok' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'mobile_pwa_analytics_smoke ok' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
: <<'LEGACY_QUERY_AUTHORITY_RECEIPT_CONTRACT'
rg -n 'normalized_role_fallbacks_cached: player / gm' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'shell_open_role_analytics: player / gm' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'shell_open_display_mode: browser / standalone' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'standalone_shell_open_analytics: player / gm' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'install_prompt_analytics: available / open / accepted' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'install_prompt_role_analytics: player / gm' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'role_switch_analytics: player->gm / gm->player' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'copied_session_handoff:' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'receiver_device: <minted-device>' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'gm_copied_session_handoff:' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'gm_receiver_device: <minted-device>' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'native_session_handoff:' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'native_receiver_device: <minted-device>' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'native_share_method: native' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'gm_native_session_handoff:' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'gm_native_receiver_device: <minted-device>' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'gm_native_share_method: native' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'link_session_handoff:' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'link_receiver_device: <minted-device>' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'link_share_method: link' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'gm_link_session_handoff:' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'gm_link_receiver_device: <minted-device>' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'gm_link_share_method: link' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'privacy_blocked: dnt_gpc' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'privacy_provider_requests: 0' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'privacy_event_count: 0' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'analytics_default_disabled: true' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'default_provider_requests: 0' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'default_event_count: 0' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'secret_leak_free: true' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'service_worker_controlled: true' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'blazor_shell: interactive-server' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'blazor_boot_script: /_framework/blazor.web.js' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'data-blazor-shell=\\"interactive-server\\"' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'AddInteractiveServerRenderMode' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'service_worker_cache: chummer-shell-play-shell-v16' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'cached_manifest_start_urls: player / gm' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'cached_manifest_shortcuts: player / gm' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'cached_manifest_icon_purpose: any maskable' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'stale_cache_cleanup: chummer-shell-play-shell-v14 -> removed' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'legacy_cache_cleanup: chummer-shell-play-shell-v10 -> removed' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'foreign_cache_preserved: foreign-origin-cache-smoke' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'hero_player_launch:' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'hero_gm_launch:' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'hero_menu_player_launch:' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'hero_menu_gm_launch:' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'compact_layout:' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'gm_overflow_free:' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'gm_key_bounds:' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'gm_compact_layout:' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'gm_touch_target_min:' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'narrow_viewport: 360x740 player lane' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'narrow_overflow_free:' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'narrow_key_bounds:' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'narrow_compact_layout:' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'narrow_touch_target_min:' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'status_pill_style:' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'installability_errors:' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'manifest_scope: /mobile/' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'manifest_icon_purpose: any maskable' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'gm_viewport: 390x844 gm lane' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'gm_manifest_url:' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'gm_manifest_scope: /mobile/' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'gm_manifest_icon_purpose: any maskable' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'gm_installability_errors:' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'query_role_manifest: player / gm' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'gm_query_manifest_url:' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'gm_query_installability_errors:' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'standalone_install_ui: player / gm' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'standalone_install_button: player Installed / gm Installed' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'player_interactions:' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'lifecycle_persisted:' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'replay_ack: local 3->0 / server 0->3->0' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'player_resume_snapshot:' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'path_gm_resume:' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'role_switch_device_isolated:' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'reverse_role_switch_device_isolated:' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'gm_interactions: fire-stairs / reveal-threat / local 3->0 / server 0->3->0' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'gm_resume_snapshot:' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'offline_fresh_launch:' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'offline_player_queue_replay: local 1->0 / server 0->1->0 / ammo 8->7' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'offline_gm_queue_replay: local 1->0 / server 0->1->0 / gm-advance-initiative' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'offline_handoff_receiver:' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'private_api_boundary:' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'play_api_network_unavailable' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
LEGACY_QUERY_AUTHORITY_RECEIPT_CONTRACT
rg -n 'public_install_boundary: /mobile /mobile/player /mobile/gm /mobile/observer' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'public_authority: none' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'query_parameters_grant_access: false' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'live_session_boundary: /mobile/live' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'live_grant_source: trusted_server_headers' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'live_owner_route: /mobile/live' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'public_install_phone_layouts: player / gm / observer' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'public_install_desktop_layout: 3 columns' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'manifest_start_urls: clean player / gm / observer' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'live_session_viewport: 390x844 /mobile/live' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'public_install_analytics: disabled' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'live_analytics_route: /mobile/live' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'live_analytics_role_source: trusted_server_headers' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'privacy_blocked: dnt_gpc' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'analytics_default_disabled: true' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'secret_leak_free: true' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n '"contract_name": "chummer6-mobile.role_pwa_contract.v2"' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n '"live_route_requires_trusted_server_grant": true' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n '"query_parameters_cannot_grant_live_access": true' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'VerifyRuntimeBundleSessionLockReleasesOnCanceledAcquireAsync' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'Post-closure completion criteria \(M12\)' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'Post-closure hardening criteria \(M13\)' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'Post-closure role-depth criteria \(M14\)' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'Release-proof cadence criteria:' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'VerifyOfflineQueueRejectsMalformedSessionEnvelopeAsync' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'VerifyOfflineQueueRejectsStaleLineageAsync' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'VerifyResumePreservesLedgerWhenRuntimeBundleDriftsAsync' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'VerifyResumeNormalizesCheckpointToLedgerLineageAsync' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'VerifyReconnectLineageTransitionContinuityAsync' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'VerifyResumeAndWorkspaceLiteRoutesStayRoleConcreteAsync' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'replace old `Chummer.Presentation` project references with package-only dependencies' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
rg -n 'preserve local-first event log, runtime bundle, and offline cache ownership here' .codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json >/dev/null
if [[ -n "${published_feed_sources}" ]]; then
  echo "running published-feed compatibility restore/build checks"
  bash "${package_plane_runner}" restore src/Chummer.Play.Core/Chummer.Play.Core.csproj --nologo >/dev/null
  bash "${package_plane_runner}" restore src/Chummer.Play.Components/Chummer.Play.Components.csproj --nologo >/dev/null
  bash "${package_plane_runner}" restore src/Chummer.Play.Player/Chummer.Play.Player.csproj --nologo >/dev/null
  bash "${package_plane_runner}" restore src/Chummer.Play.Gm/Chummer.Play.Gm.csproj --nologo >/dev/null
  bash "${package_plane_runner}" restore src/Chummer.Play.Web/Chummer.Play.Web.csproj --nologo >/dev/null
  bash "${package_plane_runner}" restore src/Chummer.Play.RegressionChecks/Chummer.Play.RegressionChecks.csproj --nologo >/dev/null
  bash "${package_plane_runner}" build src/Chummer.Play.Core/Chummer.Play.Core.csproj --nologo --no-restore >/dev/null
  bash "${package_plane_runner}" build src/Chummer.Play.Components/Chummer.Play.Components.csproj --nologo --no-restore >/dev/null
  bash "${package_plane_runner}" build src/Chummer.Play.Player/Chummer.Play.Player.csproj --nologo --no-restore >/dev/null
  bash "${package_plane_runner}" build src/Chummer.Play.Gm/Chummer.Play.Gm.csproj --nologo --no-restore >/dev/null
  bash "${package_plane_runner}" build src/Chummer.Play.Web/Chummer.Play.Web.csproj --nologo --no-restore >/dev/null
  bash "${package_plane_runner}" build src/Chummer.Play.RegressionChecks/Chummer.Play.RegressionChecks.csproj --nologo --no-restore >/dev/null
  bash "${package_plane_runner}" run --project src/Chummer.Play.RegressionChecks/Chummer.Play.RegressionChecks.csproj --nologo --no-build >/dev/null
  python3 scripts/verify_mobile_pwa_analytics_smoke.py >/dev/null
  python3 scripts/cleanup_mobile_disposable_artifacts.py >/dev/null
  python3 scripts/run_mobile_strict_public_edge_follow_through.py >/dev/null
  test -f .codex-studio/published/MOBILE_STRICT_PUBLIC_EDGE_FOLLOW_THROUGH.generated.json
  verification_receipt_status="pass"
  write_verification_mode_receipt
  materialize_mobile_release_proof
  python3 scripts/verify_next90_m117_mobile_artifact_shelf.py >/dev/null
  python3 scripts/verify_next90_m112_mobile_campaign_continuity.py >/dev/null
  python3 scripts/verify_next90_m119_mobile_onboarding_continuity.py >/dev/null
  python3 scripts/verify_next90_m121_mobile_live_combat_confidence.py >/dev/null
  python3 scripts/verify_next90_m122_mobile_runner_goal_updates.py >/dev/null
  python3 scripts/verify_next90_m145_mobile_quick_explain_and_follow_up.py >/dev/null
else
  if [[ "${verification_mode}" == "scaffold" || "${verification_mode}" == "slice" ]]; then
    skip_or_fail "published-feed compatibility restore/build checks (set CHUMMER_PUBLISHED_FEED_SOURCES)"
  fi
  echo "running local owner-package compatibility smoke check"
  bash "${package_plane_runner}" restore src/Chummer.Play.Core/Chummer.Play.Core.csproj --nologo >/dev/null
  bash "${package_plane_runner}" restore src/Chummer.Play.Components/Chummer.Play.Components.csproj --nologo >/dev/null
  bash "${package_plane_runner}" restore src/Chummer.Play.Player/Chummer.Play.Player.csproj --nologo >/dev/null
  bash "${package_plane_runner}" restore src/Chummer.Play.Gm/Chummer.Play.Gm.csproj --nologo >/dev/null
  bash "${package_plane_runner}" restore src/Chummer.Play.Web/Chummer.Play.Web.csproj --nologo >/dev/null
  bash "${package_plane_runner}" restore src/Chummer.Play.RegressionChecks/Chummer.Play.RegressionChecks.csproj --nologo >/dev/null
  bash "${package_plane_runner}" build src/Chummer.Play.Core/Chummer.Play.Core.csproj --nologo --no-restore >/dev/null
  bash "${package_plane_runner}" build src/Chummer.Play.Components/Chummer.Play.Components.csproj --nologo --no-restore >/dev/null
  bash "${package_plane_runner}" build src/Chummer.Play.Player/Chummer.Play.Player.csproj --nologo --no-restore >/dev/null
  bash "${package_plane_runner}" build src/Chummer.Play.Gm/Chummer.Play.Gm.csproj --nologo --no-restore >/dev/null
  bash "${package_plane_runner}" build src/Chummer.Play.Web/Chummer.Play.Web.csproj --nologo --no-restore >/dev/null
  bash "${package_plane_runner}" build src/Chummer.Play.RegressionChecks/Chummer.Play.RegressionChecks.csproj --nologo --no-restore >/dev/null
  bash "${package_plane_runner}" run --project src/Chummer.Play.RegressionChecks/Chummer.Play.RegressionChecks.csproj --nologo --no-build >/dev/null
  python3 scripts/verify_mobile_pwa_analytics_smoke.py >/dev/null
  python3 scripts/cleanup_mobile_disposable_artifacts.py >/dev/null
  python3 scripts/run_mobile_strict_public_edge_follow_through.py >/dev/null
  test -f .codex-studio/published/MOBILE_STRICT_PUBLIC_EDGE_FOLLOW_THROUGH.generated.json
  verification_receipt_status="pass"
  write_verification_mode_receipt
  materialize_mobile_release_proof
  python3 scripts/verify_next90_m117_mobile_artifact_shelf.py >/dev/null
  python3 scripts/verify_next90_m112_mobile_campaign_continuity.py >/dev/null
  python3 scripts/verify_next90_m119_mobile_onboarding_continuity.py >/dev/null
  python3 scripts/verify_next90_m121_mobile_live_combat_confidence.py >/dev/null
  python3 scripts/verify_next90_m122_mobile_runner_goal_updates.py >/dev/null
  python3 scripts/verify_next90_m145_mobile_quick_explain_and_follow_up.py >/dev/null
fi

bash scripts/release/verify_mobile_release_proof.sh >/dev/null

verification_receipt_finalized=true
echo "chummer6-mobile verify ok"
