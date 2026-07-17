#!/usr/bin/env python3
from __future__ import annotations

import datetime as dt
import hashlib
import json
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
CHECKOUT_ROOT = str(ROOT)
REPO_LABEL = "chummer6-mobile"
LOCAL_ORIGIN_PLACEHOLDER = "http://127.0.0.1:<port>"
REGRESSION_SOURCE = ROOT / "src" / "Chummer.Play.RegressionChecks" / "Program.cs"
PLAY_WEB_APPLICATION = ROOT / "src" / "Chummer.Play.Web" / "PlayWebApplication.cs"
PLAY_ROUTE_HANDLERS = ROOT / "src" / "Chummer.Play.Web" / "PlayRouteHandlers.cs"
TURN_COMPANION_SERVICE = ROOT / "src" / "Chummer.Play.Web" / "PlayTurnCompanionService.cs"
TURN_COMPANION_PROJECTOR = ROOT / "src" / "Chummer.Play.Core" / "Application" / "PlayTurnCompanionProjector.cs"
TURN_COMPANION_IMPORTS = ROOT / "src" / "Chummer.Play.Web" / "Components" / "_Imports.razor"
TURN_COMPANION_PAGE = ROOT / "src" / "Chummer.Play.Web" / "Components" / "Pages" / "MobileTurnCompanionPage.razor"
TURN_COMPANION_LIVE_PAGE = ROOT / "src" / "Chummer.Play.Web" / "Components" / "Pages" / "MobileLiveTurnCompanionPage.razor"
PLAY_SESSION_GRANT = ROOT / "src" / "Chummer.Play.Web" / "PlaySessionGrant.cs"
TURN_COMPANION_RUNTIME = ROOT / "src" / "Chummer.Play.Web" / "wwwroot" / "mobile-turn-companion.js"
MOBILE_INSTALL_RUNTIME = ROOT / "src" / "Chummer.Play.Web" / "wwwroot" / "mobile-install-shell.js"
MOBILE_CSS = ROOT / "src" / "Chummer.Play.Web" / "wwwroot" / "mobile.css"
WEB_SOURCE = ROOT / "src" / "Chummer.Play.Web" / "wwwroot" / "index.html"
APP_SHELL = ROOT / "src" / "Chummer.Play.Web" / "Components" / "App.razor"
PLAY_WEB_DOCKERFILE = ROOT / "src" / "Chummer.Play.Web" / "Dockerfile"
SERVICE_WORKER = ROOT / "src" / "Chummer.Play.Web" / "wwwroot" / "service-worker.js"
GENERIC_MANIFEST = ROOT / "src" / "Chummer.Play.Web" / "wwwroot" / "manifest.webmanifest"
PLAYER_MANIFEST = ROOT / "src" / "Chummer.Play.Web" / "wwwroot" / "manifest.player.webmanifest"
GM_MANIFEST = ROOT / "src" / "Chummer.Play.Web" / "wwwroot" / "manifest.gm.webmanifest"
OBSERVER_MANIFEST = ROOT / "src" / "Chummer.Play.Web" / "wwwroot" / "manifest.observer.webmanifest"
MIGRATION_MAP = ROOT / "docs" / "migration-map.md"
PLAY_SIGNOFF = ROOT / "docs" / "PLAY_RELEASE_SIGNOFF.md"
VERIFY_SCRIPT = ROOT / "scripts" / "ai" / "verify.sh"
PACKAGE_PLANE_HELPER = ROOT / "scripts" / "ai" / "with-package-plane.sh"
MOBILE_RELEASE_PROOF_VERIFIER = ROOT / "scripts" / "release" / "verify_mobile_release_proof.sh"
RUNTIME_SMOKE = ROOT / "scripts" / "verify_mobile_pwa_runtime_smoke.py"
VIEWPORT_SMOKE = ROOT / "scripts" / "verify_mobile_pwa_viewport_smoke.py"
ANALYTICS_SMOKE = ROOT / "scripts" / "verify_mobile_pwa_analytics_smoke.py"
PERFORMANCE_BUDGET_VERIFIER = ROOT / "scripts" / "verify_mobile_pwa_performance_budget.py"
M112_PROOF_DOC = ROOT / "docs" / "next90-m112-mobile-campaign-continuity.proof.md"
M112_VERIFIER = ROOT / "scripts" / "verify_next90_m112_mobile_campaign_continuity.py"
M119_PROOF_DOC = ROOT / "docs" / "next90-m119-mobile-onboarding-continuity.proof.md"
M119_VERIFIER = ROOT / "scripts" / "verify_next90_m119_mobile_onboarding_continuity.py"
M117_PROOF_DOC = ROOT / "docs" / "next90-m117-mobile-artifact-shelf.proof.md"
M117_VERIFIER = ROOT / "scripts" / "verify_next90_m117_mobile_artifact_shelf.py"
M117_SUCCESSOR_FRONTIER_ID = "3440617449"
M117_ACTIVE_FLAGSHIP_FRONTIER_ID = "3371889980"
M117_FRONTIER_IDS = [M117_SUCCESSOR_FRONTIER_ID, M117_ACTIVE_FLAGSHIP_FRONTIER_ID]
M121_PROOF_DOC = ROOT / "docs" / "next90-m121-mobile-live-combat-confidence.proof.md"
M121_VERIFIER = ROOT / "scripts" / "verify_next90_m121_mobile_live_combat_confidence.py"
M122_PROOF_DOC = ROOT / "docs" / "next90-m122-mobile-runner-goal-updates-and-consequence-feed.proof.md"
M122_VERIFIER = ROOT / "scripts" / "verify_next90_m122_mobile_runner_goal_updates.py"
M145_PROOF_DOC = ROOT / "docs" / "next90-m145-mobile-quick-explain-and-follow-up.proof.md"
M145_VERIFIER = ROOT / "scripts" / "verify_next90_m145_mobile_quick_explain_and_follow_up.py"
MOBILE_CROSS_SURFACE_REFRESH_SCRIPT = ROOT / "scripts" / "materialize_mobile_cross_surface_readiness.py"
MOBILE_CROSS_SURFACE_REFRESH_RECEIPT = ROOT / ".codex-studio" / "published" / "MOBILE_CROSS_SURFACE_READINESS.generated.json"
MOBILE_RELEASE_BOUNDARY_SCRIPT = ROOT / "scripts" / "materialize_mobile_release_boundary.py"
MOBILE_RELEASE_BOUNDARY_RECEIPT = ROOT / ".codex-studio" / "published" / "MOBILE_RELEASE_BOUNDARY.generated.json"
FLEET_FLAGSHIP_READINESS = Path("/docker/fleet/.codex-studio/published/FLAGSHIP_PRODUCT_READINESS.generated.json")
OUT = ROOT / ".codex-studio" / "published" / "MOBILE_LOCAL_RELEASE_PROOF.generated.json"
M112_ACTIVE_FLAGSHIP_FRONTIER_ID = "1033794907"
VERIFICATION_COMMANDS = [
    {
        "id": "mobile_pwa_analytics_smoke",
        "command": "python3 scripts/verify_mobile_pwa_analytics_smoke.py",
        "receipt_path": ".codex-studio/published/MOBILE_PWA_ANALYTICS_SMOKE.generated.json",
        "required_before_materialize": True,
        "proves": [
            "Rybbit default-disabled posture",
            "DNT/GPC collection block",
            "Player and GM shell-open/install/share analytics",
            "session/device/secret leak-free payloads",
        ],
    },
    {
        "id": "pwa_runtime_smoke",
        "command": "python3 scripts/verify_mobile_pwa_runtime_smoke.py",
        "receipt_path": ".codex-studio/published/MOBILE_PWA_RUNTIME_SMOKE.generated.json",
        "required_before_materialize": True,
        "proves": [
            "interactive Blazor mobile shell",
            "hero dropdown Play launch to Player and GM PWA routes",
            "Player and GM offline local queue replay after reconnect",
            "device-neutral session handoff receivers",
            "private play API fail-closed offline boundary",
        ],
    },
    {
        "id": "mobile_pwa_viewport_smoke",
        "command": "python3 scripts/verify_mobile_pwa_viewport_smoke.py",
        "receipt_path": ".codex-studio/published/MOBILE_PWA_VIEWPORT_SMOKE.generated.json",
        "required_before_materialize": True,
        "proves": [
            "Player and GM installability",
            "role-specific manifest URLs and scopes",
            "mobile viewport overflow and touch target bounds",
            "standalone installed-shell UI state",
        ],
    },
]

SMOKE_RECEIPT_CONTRACTS = {
    "mobile_pwa_analytics_smoke": "chummer_play.mobile_pwa_analytics_smoke.v2",
    "pwa_runtime_smoke": "chummer_play.mobile_pwa_runtime_smoke.v2",
    "mobile_pwa_viewport_smoke": "chummer_play.mobile_pwa_viewport_smoke.v2",
}

REQUIRED_MARKERS = {
    "install_claim_restore_continue": [
        "VerifyRoamingWorkspaceRestorePlanRestoresPackageOwnedCampaignState",
        'Assert(plan.ResumeFollowThrough.Contains("Resume Redmond Patrol"',
        'Assert(plan.ResumeFollowThroughHref.Contains("/play/session-redmond"',
        'Assert(plan.SupportFollowThroughHref.Contains("/contact"',
        'Assert(plan.SupportFollowThroughHref.Contains("campaignId=campaign-redmond"',
        'Assert(projection.SupportFollowThroughHref.Contains("kind=install_help"',
        'Assert(projection.SupportFollowThroughHref.Contains("sessionId=session-redmond"',
    ],
    "campaign_session_recover_recap": [
        "VerifyCampaignWorkspaceLiteProjectionPromotesContinuitySummary",
        "VerifyCachePressureDecisionNoticeUsesSupportNextActionCopy",
        "VerifyIndexShellBindsContextualActionLabelsAsync",
        "VerifyPlayRoamingRestoreServiceProjectsClaimedDeviceRecovery",
        'Assert(projection.ContinuityRailSummary.Contains("Downtime:", StringComparison.Ordinal)',
        'Assert(projection.ContinuityRailSummary.Contains("Diary:", StringComparison.Ordinal)',
        'Assert(projection.ContinuityRailSummary.Contains("Contacts:", StringComparison.Ordinal)',
        'Assert(projection.ContinuityRailSummary.Contains("Heat:", StringComparison.Ordinal)',
        'Assert(projection.ContinuityRailSummary.Contains("Aftermath:", StringComparison.Ordinal)',
        'Assert(projection.ContinuityRailSummary.Contains("Return:", StringComparison.Ordinal)',
        'Assert(projection.GmOperationsSummary.Contains("Opposition:", StringComparison.Ordinal)',
        'Assert(projection.GmOperationsSummary.Contains("Roster movement:", StringComparison.Ordinal)',
        'Assert(projection.GmOperationsSummary.Contains("Prep library:", StringComparison.Ordinal)',
        'Assert(projection.GmOperationsSummary.Contains("Event controls:", StringComparison.Ordinal)',
        'Assert(projection.GmOperationsSummary.Contains("audit-visible", StringComparison.Ordinal)',
        'Assert(projection.GmOperationsSummary.Contains("support-linked", StringComparison.Ordinal)',
        'Assert(projection.GmOperationsLabels.Any(item => item.Contains("Opposition lane:", StringComparison.Ordinal))',
        'Assert(projection.GmOperationsLabels.Any(item => item.Contains("Roster movement lane:", StringComparison.Ordinal))',
        'Assert(projection.GmOperationsLabels.Any(item => item.Contains("Prep library lane:", StringComparison.Ordinal))',
        'Assert(projection.GmOperationsLabels.Any(item => item.Contains("Event controls lane:", StringComparison.Ordinal))',
        'Assert(projection.GmOperationsLabels.Any(item => item.Contains("Governance lane:", StringComparison.Ordinal))',
        'Assert(projection.OfflineTruthSummary.Contains("Stale:", StringComparison.Ordinal)',
        'Assert(projection.OfflineTruthSummary.Contains("Can do now:", StringComparison.Ordinal)',
        'Assert(projection.OfflineTruthSummary.Contains("safehouse", StringComparison.OrdinalIgnoreCase)',
        'Assert(projection.OfflineTruthSummary.Contains("Needs online:", StringComparison.Ordinal)',
        'Assert(projection.OfflineTruthLabels.Any(item => item.Contains("Stale lane:", StringComparison.Ordinal))',
        'Assert(projection.OfflineTruthLabels.Any(item => item.Contains("Can-do-now lane:", StringComparison.Ordinal))',
        'Assert(projection.OfflineTruthLabels.Any(item => item.Contains("Needs-online lane:", StringComparison.Ordinal))',
        'Assert(projection.OfflineTruthLabels.Any(item => item.Contains("safehouse", StringComparison.OrdinalIgnoreCase))',
        'Assert(plan.TravelCompanionSummary.Contains("Cached:", StringComparison.Ordinal)',
        'Assert(plan.TravelCompanionSummary.Contains("Stale:", StringComparison.Ordinal)',
        'Assert(plan.TravelCompanionSummary.Contains("Offline actions:", StringComparison.Ordinal)',
        'Assert(plan.TravelCompanionLabels.Any(item => item.Contains("Cached lane:", StringComparison.Ordinal))',
        'Assert(plan.TravelCompanionLabels.Any(item => item.Contains("Stale lane:", StringComparison.Ordinal))',
        'Assert(plan.TravelCompanionLabels.Any(item => item.Contains("Offline action lane:", StringComparison.Ordinal))',
        'Assert(plan.SafeNextAction.Contains("Open scene-redmond mobile return"',
        'Assert(projection.ChangePacketSummary.Contains("Return anchor stays on checkpoint 12"',
        'Assert(projection.ChangePacketLabels.Any(item => item.Contains("Scene packet: scene-redmond"',
        'Assert(projection.RolePosture.Contains("player lane"',
        'Assert(projection.RoleFollowThroughHref.Contains("/play/session-redmond?role=Player"',
        'Assert(projection.DecisionNotice.Contains("Use the current bundle proof for scene-redmond"',
        'setLink("workspace-decision-notice-link", payload.decisionNoticeHref, payload.decisionNotice, "/", "Decision notice follow-through");',
        'setLink("follow-through-update-link", payload.updateFollowThroughHref, payload.updateFollowThrough, "/downloads", "Update follow-through");',
        'setLink("follow-through-support-link", payload.supportFollowThroughHref, payload.supportFollowThrough, "/contact", "Support follow-through");',
        'setLink("follow-through-role-link", payload.roleFollowThroughHref, payload.roleFollowThrough, "/", "Role follow-through");',
        'setLink("restore-follow-through-link", payload.resumeFollowThroughHref, payload.resumeFollowThrough, "/play", "Claimed-device follow-through");',
        'setLink("restore-support-follow-through-link", payload.supportFollowThroughHref, payload.supportFollowThrough, "/contact", "Restore support follow-through");',
        'const explicitDeviceId = params.get("deviceId") || "";',
        'const resolvedDeviceId = resolveStableDeviceId(role, explicitDeviceId);',
        'const observerId = resolveObserverId();',
        'const observeResponse = await fetch(`/api/play/observe/${encodeURIComponent(sessionId)}`);',
        'fetch("/api/play/continuity/claim", {',
        'renderContinuityClaimStatus({',
        'document.getElementById("shell-continuity-claim-button").addEventListener("click", claimContinuityOnThisDevice);',
        'id="shell-continuity-claim-status"',
        'id="shell-owner-route-link"',
        'id="shell-continuity-claim-button"',
        'id="change-packet-summary"',
        'id="change-packet-list"',
        'id="workspace-continuity-rail"',
        'id="workspace-continuity-rail-list"',
        'id="workspace-gm-ops"',
        'id="workspace-gm-ops-list"',
        'id="workspace-offline-truth"',
        'id="workspace-offline-truth-list"',
        'id="workspace-role"',
        'id="workspace-update"',
        'id="workspace-support"',
        'id="workspace-support-status"',
        'id="workspace-known-issue"',
        'id="workspace-fix-state"',
        'id="restore-follow-through"',
        'id="restore-offline-truth"',
        'id="restore-offline-truth-labels"',
        'id="restore-travel-companion"',
        'id="restore-travel-companion-labels"',
        'id="restore-prefetch-labels"',
    ],
    "recover_from_sync_conflict": [
        "VerifyRoamingWorkspaceRestorePlanPreservesConflictAndInstallLocalGuardrails",
        "Assert(plan.RequiresConflictReview",
        'Assert(plan.ResumeFollowThrough.Contains("restore review"',
        'Assert(plan.SupportFollowThroughHref.Contains("different%20channels"',
        'Assert(plan.AttentionItems.Any(item => item.Contains("different channels"',
        'id="restore-support-follow-through"',
    ],
    "quality_release_hardening": [
        "VerifyIndexShellAccessibilityContractAsync",
        "VerifyCachePressureBudgetContractAsync",
        "VerifyCachePressureDecisionNoticeUsesSupportNextActionCopy",
        "VerifyOfflineQueueRejectsMalformedSessionEnvelopeAsync",
        "VerifyOfflineQueueRejectsStaleLineageAsync",
        "VerifySyncPrefixAcknowledgementAsync",
        "VerifyResumePreservesLedgerWhenRuntimeBundleDriftsAsync",
        "VerifyResumeNormalizesCheckpointToLedgerLineageAsync",
        "VerifyReconnectLineageTransitionContinuityAsync",
        "VerifyResumeAndWorkspaceLiteRoutesStayRoleConcreteAsync",
        "VerifyRuntimeBundleSessionLockReleasesOnCanceledAcquireAsync",
        '<html lang="en">',
        'role="status" aria-live="polite" aria-atomic="true"',
        "navigator.serviceWorker.register",
        'id="shell-play-action-link"',
        'id="shell-hero-action-menu"',
        "function normalizePlayRouteForMobileShell(href, roleFallback, sessionIdFallback, deviceFallback)",
        "function syncHeroActionMenu()",
        "function navigateHeroAction(action)",
        'document.getElementById("shell-hero-action-menu").addEventListener("change"',
        'const deviceId = playParams.get("deviceId") || deviceFallback || activeShellIdentity.deviceId || "";',
        'playParams.set("deviceId", deviceId);',
        'setLink("shell-play-action-link", "/play", "Play", "/play", "Play");',
        "Post-closure completion criteria (M12)",
        "Post-closure hardening criteria (M13)",
        "Post-closure role-depth criteria (M14)",
        "Release-proof cadence criteria:",
    ],
    "pwa_runtime_smoke": [
        "Real-Host PWA Runtime Criteria",
        "Hero launch criteria:",
        "scripts/verify_mobile_pwa_runtime_smoke.py",
        "mobile_pwa_runtime_smoke ok",
        "service_worker_controlled: true",
        "blazor_shell: interactive-server",
        "blazor_boot_script: /_framework/blazor.web.js",
        "service_worker_cache: chummer-shell-play-shell-v16",
        "normalized_role_fallbacks_cached: player / gm",
        "cached_manifest_start_urls: player / gm",
        "cached_manifest_shortcuts: player / gm",
        "cached_manifest_icon_purpose: any maskable",
        "stale_cache_cleanup: chummer-shell-play-shell-v14 -> removed",
        "legacy_cache_cleanup: chummer-shell-play-shell-v10 -> removed",
        "foreign_cache_preserved: foreign-origin-cache-smoke",
        "const CACHE_VERSION = \"play-shell-v16\";",
        "\"/_framework/blazor.web.js\"",
        "data-blazor-shell=\"interactive-server\"",
        "data-enhance-nav=\"false\"",
        "AddInteractiveServerComponents",
        "AddInteractiveServerRenderMode",
        "StaticWebAssetsLoader.UseStaticWebAssets",
        "app.MapStaticAssets();",
        "const MANAGED_CACHE_PREFIXES = [",
        "function isManagedPlayCache(cacheName)",
        ".filter((key) => isManagedPlayCache(key) && ![SHELL_CACHE, MEDIA_CACHE, MEDIA_META_CACHE].includes(key))",
        "const NON_CACHEABLE_PATHS = new Set([",
        "\"/mobile/pwa/ledger.json\"",
        "const NON_CACHEABLE_PATH_PREFIXES = [",
        "function isNonCacheableRequest(url)",
        "play_public_route_network_unavailable",
        "page.wait_for_selector(\"#workspace-shell:not([hidden])\", timeout=NAVIGATION_TIMEOUT_MS)",
        "page.wait_for_selector(\"[data-turn-root][data-blazor-shell='interactive-server']\", timeout=NAVIGATION_TIMEOUT_MS)",
        "page.wait_for_selector(\"script[src='/_framework/blazor.web.js']\", state=\"attached\", timeout=NAVIGATION_TIMEOUT_MS)",
        "FOREIGN_CACHE = \"foreign-origin-cache-smoke\"",
        "def describe_control_state(page: Page, selector: str) -> str",
        "control was not ready for click:",
        "localQueue",
        "serverQueue",
        "hero_player_launch:",
        "hero_gm_launch:",
        "hero_menu_player_launch:",
        "hero_menu_gm_launch:",
        "player_interactions:",
        "lifecycle_persisted:",
        "replay_ack: local 3->0 / server 0->3->0",
        "player_resume_snapshot:",
        "path_gm_resume:",
        "role_switch_device_isolated:",
        "reverse_role_switch_device_isolated:",
        "gm_interactions: fire-stairs / reveal-threat / local 3->0 / server 0->3->0",
        "gm_resume_snapshot:",
        "generic_resume:",
        "gm_resume:",
        "offline_reopen:",
        "offline_fresh_launch:",
        "offline Player and GM local replay/ack",
        "offline_player_queue_replay: local 1->0 / server 0->1->0 / ammo 8->7",
        "offline_gm_queue_replay: local 1->0 / server 0->1->0 / gm-advance-initiative",
        "offline_handoff_receiver:",
        "device-neutral session handoff receivers",
        "private_api_boundary:",
        "play_api_network_unavailable",
        "VerifyTurnCompanionRealHostPipelineUsesAntiforgeryAsync",
        "app.UseAntiforgery();",
        "python3 scripts/verify_mobile_pwa_runtime_smoke.py >/dev/null",
    ],
    "mobile_pwa_viewport_smoke": [
        "scripts/verify_mobile_pwa_viewport_smoke.py",
        "mobile_pwa_viewport_smoke ok",
        "Role-specific manifest criteria:",
        "Quick-glance criteria:",
        "turn-jump-nav",
        "turn-glance-grid",
        "turn-now-card",
        "viewport: 390x844 player lane",
        "gm_viewport: 390x844 gm lane",
        "narrow_viewport: 360x740 player lane",
        "overflow_free:",
        "gm_overflow_free:",
        "gm_key_bounds:",
        "narrow_overflow_free:",
        "narrow_key_bounds:",
        "quick_lane_priority:",
        "gm_quick_lane_priority:",
        "compact_layout:",
        "gm_compact_layout:",
        "narrow_compact_layout:",
        "gm_touch_target_min:",
        "narrow_touch_target_min:",
        "status_pill_style:",
        "quick_glance: ammo",
        "gm_quick_glance: ammo",
        "Page.getInstallabilityErrors",
        "player-specific PWA manifest",
        "GM-specific PWA manifest",
        "manifest_url:",
        "manifest_scope: /mobile/",
        "gm_manifest_url:",
        "gm_manifest_scope: /mobile/",
        "manifest_icon_purpose: any maskable",
        "gm_manifest_icon_purpose: any maskable",
        "installability_errors:",
        "gm_installability_errors:",
        "query_role_manifest: player / gm",
        "gm_query_manifest_url:",
        "gm_query_installability_errors:",
        "standalone_install_ui: player / gm",
        "standalone_install_button: player Installed / gm Installed",
        "safe_area_padding:",
        "gm_screenshot:",
        "narrow_screenshot:",
        "python3 scripts/verify_mobile_pwa_viewport_smoke.py >/dev/null",
        'viewport={"width": 360, "height": 740}',
        "incoherentOverflow",
        "minTouchTarget",
        "min-height: 2.75rem;",
        "min-width: 2.75rem;",
        'function renderQuickGlance(client)',
        'setText("turn-glance-ammo", String(statValue(projection, "ammo")));',
    ],
    "mobile_pwa_analytics_smoke": [
        "scripts/verify_mobile_pwa_analytics_smoke.py",
        "mobile_pwa_analytics_smoke ok",
        "Rybbit analytics criteria:",
        "secret_leak_free: true",
        "provider_script:",
        "pageview_skip:",
        "route_mask:",
        "shell_open_role_analytics: player / gm",
        "shell_open_display_mode: browser / standalone",
        "standalone_shell_open_analytics: player / gm",
        "install_prompt_analytics: available / open / accepted",
        "install_prompt_role_analytics: player / gm",
        "role_switch_analytics: player->gm / gm->player",
        "copied_session_handoff:",
        "receiver_device: <minted-device>",
        "gm_copied_session_handoff:",
        "gm_receiver_device: <minted-device>",
        "native_session_handoff:",
        "native_receiver_device: <minted-device>",
        "native_share_method: native",
        "gm_native_session_handoff:",
        "gm_native_receiver_device: <minted-device>",
        "gm_native_share_method: native",
        "link_session_handoff:",
        "link_receiver_device: <minted-device>",
        "link_share_method: link",
        "gm_link_session_handoff:",
        "gm_link_receiver_device: <minted-device>",
        "gm_link_share_method: link",
        "privacy_blocked: dnt_gpc",
        "privacy_provider_requests: 0",
        "privacy_event_count: 0",
        "analytics_default_disabled: true",
        "default_provider_requests: 0",
        "default_event_count: 0",
        "RYBBIT_CHUMMER_PLAY_SITE_ID",
        "RYBBIT_CHUMMER_PLAY_SCRIPT_URL",
        "RYBBIT_CHUMMER_PLAY_ALLOW_SAME_HOST_PROXY",
        "start_server(configure_analytics: bool = True)",
        'env.pop(key, None)',
        "def click_mobile_control(page: Page, selector: str, context: str) -> None",
        "control was not ready:",
        "eventCount",
        "hasConfigNode",
        "hasProviderScript",
        "assert_no_secret_payload",
        "mobile_shell_open",
        "mobile_install_prompt_available",
        "mobile_install_prompt_open",
        "mobile_install_prompt_choice",
        "mobile_install_prompt_unavailable",
        "mobile_privacy_probe",
        "mobile_session_handoff_share",
        "function isAnalyticsBlocked()",
        'window.doNotTrack === "1"',
        'navigator.doNotTrack === "1"',
        "navigator.globalPrivacyControl === true",
        "turn-share-owner-route-button",
        "turn-owner-route-share-status",
        "publicNote",
        "publicOwnerLabel",
        "session-analytics-secret",
        "device-analytics-secret",
        "token-analytics-secret",
        "python3 scripts/verify_mobile_pwa_analytics_smoke.py >/dev/null",
    ],
    "turn_companion_live_session": [
        "Mobile turn companion criteria",
        "Bounded turn-state criteria:",
        "Replay-safe continuity criteria:",
        "RUNSITE anchor criteria:",
        "VerifyTurnCompanionProjectionStaysBoundedAndComputesOddsAsync",
        "VerifyTurnCompanionPlayerProjectionCoversRequestedLiveTrackersAsync",
        "VerifyTurnCompanionGmProjectionStaysBoundedAndRoleSpecificAsync",
        "VerifyTurnCompanionDigitalResolveProducesBoundedReceiptAsync",
        "VerifyTurnCompanionManualResolveUpdatesHistoryAndAmmoAsync",
        "VerifyTurnCompanionObserverStaysReadOnlyAsync",
        "VerifyTurnCompanionClaimedDeviceStateIsolationAsync",
        "VerifyTurnCompanionRunsiteAnchorSelectionStaysDeviceScopedAsync",
        "VerifyTurnCompanionReplayQueueRoundTripsAsync",
        "VerifyTurnCompanionRouteRendersBlazorShellAsync",
        "VerifyTurnCompanionClientRuntimeKeepsClaimedDeviceContinuityContractAsync",
        "VerifyTurnCompanionManifestTargetsDirectMobilePwaAsync",
        "VerifyTurnCompanionAppShellDeclaresMobileInstallMetadataAsync",
        "BuildMobileOwnerRoute",
        "PlayRouteHandlers.BuildMobileOwnerRoute(sessionId, role, deviceId)",
        "/mobile/{mode}?{string.Join(\"&\", queryParts)}",
        "/mobile/player?sessionId=session-turn-projection",
        "/mobile/gm?sessionId=session-turn-gm-projection",
        "deviceId=player-shell-main",
        "deviceId=gm-shell-main",
        "turn companion sync surface must expose the player PWA handoff route instead of only the legacy play alias",
        "gm turn companion trust posture must not leak the legacy play alias as its visible PWA handoff",
        "chummer-play-analytics-config",
        "RybbitAnalyticsEnabled",
        "=> !string.IsNullOrWhiteSpace(RybbitAnalyticsSiteId)",
        "RYBBIT_CHUMMER_PLAY_SITE_ID",
        "RYBBIT_CHUMMER_PLAY_SCRIPT_URL",
        "RYBBIT_CHUMMER_PLAY_ALLOW_SAME_HOST_PROXY",
        "IsAllowedRybbitEndpoint(parsedScriptUrl)",
        "endpoint.Scheme == Uri.UriSchemeHttps",
        'endpoint.IsLoopback || string.Equals(endpoint.Host, "localhost"',
        "mobile_shell_open",
        "mobile_role_switch",
        "var resumeRoute = resolveResumeRoute(params, requestedRoleName);",
        "function resolveResumeRoute(params, requestedRoleName)",
        "var scopedRoleName = String(requestedRoleName || params.get(\"role\") || \"\").trim();",
        "function persistClientState(client)",
        "document.visibilityState === \"hidden\"",
        "window.addEventListener(\"pagehide\"",
        "window.addEventListener(\"beforeunload\"",
        "var targetDeviceId = roleSegmentForAnalytics(roleName) === roleSegmentForAnalytics(client.roleName)",
        ": readStoredValue(deviceIdStorageKey(roleName));",
        "window.location.assign(href);",
        "__chummerPlaySuppressRoleNavigation",
        "window.ChummerPlayInstallPromptForTest",
        "window.ChummerPlayInstallShellForTest",
        "if (deviceId) {",
        "if (role == Role && !string.IsNullOrWhiteSpace(ActiveDeviceId))",
        "case \"share-owner-route\":",
        "function shareOwnerRoute(client)",
        "navigator.clipboard.writeText(shareUrl)",
        "function writeHandoffLink(shareUrl)",
        "function sessionHandoffHref(ownerRoute, client)",
        "handoffParams.set(\"sessionId\", sessionId)",
        "handoffParams.set(\"role\", roleName)",
        "mobile_session_handoff_share",
        "rybbit.dataset.skipPatterns = config.skipPatterns",
        "rybbit.dataset.maskPatterns = config.maskPatterns",
        "rybbit.dataset.replayBlockSelector = config.replayBlockSelector",
        "return /session|device|token|continuity|owner|secret|key|href|url/i.test(key);",
        "function isSensitiveAnalyticsValue(value)",
        "isSensitiveAnalyticsValue(safeValue)",
        "(?:session|device|token|secret|key|continuity|owner)[_-]",
        '"/manifest.player.webmanifest"',
        '"/manifest.gm.webmanifest"',
        "function cacheMobileNavigationPath(pathname)",
        "chummer-play-cache-current-route",
        '"id": "/mobile/player"',
        '"id": "/mobile/gm"',
        '"scope": "/mobile/"',
        '"purpose": "any maskable"',
        'serverRoomProjection.Runsite.SelectedAnchorId == "server-room"',
        'deviceBProjection.Runsite.SelectedAnchorId == "front-door"',
        'observerProjection.Runsite.SelectedAnchorId == "front-door"',
        'projection.Now.ActorLabel.Contains("GM focus actor", StringComparison.Ordinal)',
        'projection.Act.Actions.Any(item => item.ActionId == "advance-initiative")',
        'projection.Sync.QuickActions.Any(item => item.ActionId == "gm-advance-initiative")',
        'projection.Now.InventoryCards.Any(item => item.ItemId == "stim-patch")',
        'projection.Now.InventoryCards.Any(item => item.ItemId == "flashbang")',
        'projection.Resolve.ManualEntryHint.Contains("hit count", StringComparison.OrdinalIgnoreCase)',
        'projection.Resolve.ManualEntryHint.Contains("digital resolver", StringComparison.OrdinalIgnoreCase)',
        'afterResolve.Resolve.LastOutcomeSummary.Contains("via digital entry", StringComparison.Ordinal)',
        'new PlayTurnStatCard("physical", "Physical", state.PhysicalDamage, "Damage marked locally on this shell.", "critical")',
        'new PlayTurnStatCard("stun", "Stun", state.StunDamage, "Stun carry-forward for the next exchange.", "warning")',
        'new PlayTurnStatCard("ammo", "Magazine", state.AmmoInMagazine, "Active firing posture.", "cool")',
        'state.Inventory.Select(item => new PlayTurnInventoryCard(item.ItemId, item.Label, item.Quantity, "Mission-critical inventory only.")).ToArray())',
        "RUNSITE stays orientation-only here: room, zone, and hotspot anchors are inspectable context, not token authority.",
        'id="turn-action-grid"',
        'id="turn-odds-summary"',
        'id="turn-history-list"',
        'id="turn-runsite-summary"',
        'id="runsite-anchor"',
        'id="turn-claim-device-button"',
        'queueLocalEvent(client, "turn:metric:" + metricId + ":" + signedToken(delta));',
        'queueLocalEvent(client, "turn:anchor:" + anchorId);',
        'queueLocalEvent(client, "turn:resolve:" + action.actionId + ":" + result.mode + ":" + result.hits + ":" + (result.glitch ? "glitch1" : "glitch0"));',
    ],
    "migration_boundary_evidence": [
        "`Chummer.Session.Web/Program.cs` -> `src/Chummer.Play.Web/Program.cs`",
        "`Chummer.Session.Web/BrowserSessionApiClient.cs` -> `src/Chummer.Play.Web/BrowserSessionApiClient.cs`",
        "`Chummer.Session.Web` bootstrap/sync/session DTOs -> `Chummer.Play.Contracts` package-owned play transport surface",
        "replace old `Chummer.Presentation` project references with package-only dependencies",
        "preserve local-first event log, runtime bundle, and offline cache ownership here",
        "keep DTO canon in shared packages instead of introducing repo-local copies",
    ],
    "mobile_campaign_continuity": [
        "Mobile campaign continuity criteria (M112)",
        "next90-m112-mobile-campaign-continuity",
        'Assert(projection.MobileCampaignCurrentState.Contains("Current continuity posture:", StringComparison.Ordinal)',
        'Assert(projection.MobileCampaignStateSummary.Contains("Cached state:", StringComparison.Ordinal)',
        'Assert(projection.MobileCampaignCachedState.Contains("Cached state:", StringComparison.Ordinal)',
        'Assert(projection.MobileCampaignStaleState.Contains("Stale state:", StringComparison.Ordinal)',
        'Assert(projection.MobileCampaignActionRequired.Contains("Action required:", StringComparison.Ordinal)',
        'Assert(projection.MobileCampaignActionRequired.Contains("Mobile shell owner: player lane. Session: scene-redmond.", StringComparison.Ordinal)',
        'Assert(projection.MobileCampaignStateLabels.Any(item => item.Contains("Cached lane:", StringComparison.Ordinal))',
        'Assert(projection.MobileCampaignStateLabels.Any(item => item.Contains("Action-required lane:", StringComparison.Ordinal))',
        'Assert(plan.TravelCampaignCurrentState.Contains("Current continuity posture:", StringComparison.Ordinal)',
        'Assert(plan.TravelCampaignStateSummary.Contains("Cached state:", StringComparison.Ordinal)',
        'Assert(plan.TravelCampaignCachedState.Contains("Cached state:", StringComparison.Ordinal)',
        'Assert(plan.TravelCampaignStaleState.Contains("Stale state:", StringComparison.Ordinal)',
        'Assert(plan.TravelCampaignActionRequired.Contains("Action required:", StringComparison.Ordinal)',
        'Assert(plan.TravelCampaignActionRequired.Contains("Travel lane: play_tablet on install-tablet.", StringComparison.Ordinal)',
        'Assert(postFailureProjection.EntryStateSummary.Contains("cached, stale, and action-required travel continuity", StringComparison.Ordinal)',
        'Assert(postFailureProjection.RecoveryActions.Any(item => item.Contains("Travel continuity cached state:", StringComparison.Ordinal))',
        'Assert(postFailureProjection.RecoveryActions.Any(item => item.Contains("Travel continuity stale state:", StringComparison.Ordinal))',
        'Assert(postFailureProjection.RecoveryActions.Any(item => item.Contains("Travel continuity action required:", StringComparison.Ordinal))',
        'Assert(plan.TravelCampaignStateLabels.Any(item => item.Contains("Cached lane:", StringComparison.Ordinal))',
        'Assert(plan.TravelCampaignStateLabels.Any(item => item.Contains("Travel companion lane:", StringComparison.Ordinal))',
        'setText("workspace-mobile-campaign-current-state", payload.mobileCampaignCurrentState, "No mobile campaign continuity posture is available yet.");',
        'setText("workspace-mobile-campaign-state", payload.mobileCampaignStateSummary, "No mobile campaign-state summary is available yet.");',
        'setList("workspace-mobile-campaign-state-list", payload.mobileCampaignStateLabels);',
        'setText("workspace-action-required", payload.actionRequiredSummary, "No action-required summary is available yet.");',
        'setList("workspace-action-required-list", payload.actionRequiredLabels);',
        'setText("workspace-mobile-campaign-cached-state", payload.mobileCampaignCachedState, "No mobile cached campaign state is available yet.");',
        'setText("workspace-mobile-campaign-stale-state", payload.mobileCampaignStaleState, "No mobile stale campaign state is available yet.");',
        'setText("workspace-mobile-campaign-action-required", payload.mobileCampaignActionRequired, "No mobile campaign action-required posture is available yet.");',
        'setText("restore-travel-campaign-current-state", payload.travelCampaignCurrentState, "No restore travel campaign continuity posture is available yet.");',
        'setText("restore-travel-campaign-state", payload.travelCampaignStateSummary, "No restore travel campaign-state summary is available yet.");',
        'setList("restore-travel-campaign-state-labels", payload.travelCampaignStateLabels);',
        'setText("restore-action-required", payload.actionRequiredSummary, "No restore action-required summary is available yet.");',
        'setList("restore-action-required-labels", payload.actionRequiredLabels);',
        'setText("restore-travel-campaign-cached-state", payload.travelCampaignCachedState, "No restore travel cached campaign state is available yet.");',
        'setText("restore-travel-campaign-stale-state", payload.travelCampaignStaleState, "No restore travel stale campaign state is available yet.");',
        'setText("restore-travel-campaign-action-required", payload.travelCampaignActionRequired, "No restore travel action-required posture is available yet.");',
        'if (lowered.includes("action required") || lowered.includes("action-required")) {',
        'function syncContinuityCardTone(cardId, currentStateId, summaryId, currentStateText, actionId)',
        'const actionState = actionId ? (document.getElementById(actionId).textContent || "") : "";',
        'card.dataset.tone = inferContinuityTone(actionState || currentStateText || currentState || summary);',
        'const continuityTone = inferContinuityTone(`${payload.mobileCampaignActionRequired || ""} ${payload.mobileCampaignStaleState || ""}`);',
        'document.getElementById("shell-continuity-status").dataset.tone = continuityTone;',
        '`${payload.mobileCampaignStaleState || ""} ${payload.mobileCampaignCurrentState || ""}`',
        '`${payload.travelCampaignStaleState || ""} ${payload.travelCampaignCurrentState || ""}`',
        'syncContinuityStateBreakdown(\n      "workspace-mobile-campaign-cached-state",',
        'syncContinuityStateBreakdown(\n      "restore-travel-campaign-cached-state",',
        'id="workspace-mobile-campaign-card"',
        'id="workspace-mobile-campaign-current-state"',
        'id="workspace-mobile-campaign-state"',
        'id="restore-travel-campaign-card"',
        'id="restore-travel-campaign-current-state"',
        'id="restore-travel-campaign-state"',
        'id="workspace-mobile-campaign-cached-state"',
        'id="workspace-mobile-campaign-stale-state"',
        'id="workspace-mobile-campaign-action-required"',
        'id="workspace-mobile-campaign-state-list"',
        'id="restore-travel-campaign-cached-state"',
        'id="restore-travel-campaign-stale-state"',
        'id="restore-travel-campaign-action-required"',
        'id="restore-travel-campaign-state-labels"',
        'campaign_memory:travel',
        'campaign_state:mobile',
        "scripts/verify_next90_m112_mobile_campaign_continuity.py",
    ],
    "mobile_onboarding_continuity": [
        "Mobile onboarding continuity criteria (M119)",
        "next90-m119-mobile-onboarding-continuity",
        'Assert(projection.LaunchPrimerSummary.Contains("Starter primer:", StringComparison.Ordinal)',
        'Assert(projection.LaunchPrimerHref == "/artifacts/session-redmond/artifact%3Asession-redmond%3Astarter-primer?role=Player&view=personal"',
        'Assert(projection.FirstSessionBriefingSummary.Contains("First-session briefing:", StringComparison.Ordinal)',
        'Assert(projection.FirstSessionBriefingHref == "/artifacts/session-redmond/artifact%3Asession-redmond%3Afirst-session-briefing?role=Player&view=travel"',
        'Assert(projection.StarterArtifactContinuitySummary.Contains("Starter continuity:", StringComparison.Ordinal)',
        'Assert(projection.StarterArtifactContinuityLabels.Any(item => item.Contains("Starter primer lane:", StringComparison.Ordinal))',
        'Assert(plan.StarterPrimerFollowThrough.Contains("starter primer", StringComparison.OrdinalIgnoreCase)',
        'Assert(plan.StarterPrimerFollowThroughHref.Contains("artifact%3Asession-redmond%3Astarter-primer", StringComparison.Ordinal)',
        'Assert(plan.StarterPrimerFollowThroughHref.Contains("deviceId=install-tablet", StringComparison.Ordinal)',
        'Assert(plan.FirstSessionBriefingFollowThrough.Contains("first-session briefing", StringComparison.OrdinalIgnoreCase)',
        'Assert(plan.FirstSessionBriefingFollowThroughHref.Contains("artifact%3Asession-redmond%3Afirst-session-briefing", StringComparison.Ordinal)',
        'Assert(plan.FirstSessionBriefingFollowThroughHref.Contains("view=travel", StringComparison.Ordinal)',
        'Assert(noSessionProjection.RecommendedActionLabel.Contains("starter primer", StringComparison.OrdinalIgnoreCase)',
        'Assert(noCampaignProjection.RecommendedActionHref.Contains("starter-primer", StringComparison.OrdinalIgnoreCase)',
        'Assert(restorePlanTravel.StarterPrimerFollowThroughHref.Contains($"deviceId={Uri.EscapeDataString(expectedDeviceId)}", StringComparison.Ordinal)',
        'Assert(restorePlanTravel.FirstSessionBriefingFollowThroughHref.Contains("view=travel", StringComparison.Ordinal)',
        'setText("workspace-launch-primer", payload.launchPrimerSummary, "No starter primer summary is available yet.");',
        'setLink("workspace-launch-primer-link", payload.launchPrimerHref, "Open starter primer", "/artifacts", "Open starter primer");',
        'setText("workspace-first-session-briefing", payload.firstSessionBriefingSummary, "No first-session briefing summary is available yet.");',
        'setLink("workspace-first-session-briefing-link", payload.firstSessionBriefingHref, "Open first-session briefing", "/artifacts", "Open first-session briefing");',
        'setText("workspace-starter-artifact-continuity", payload.starterArtifactContinuitySummary, "No starter artifact continuity summary is available yet.");',
        'setText("restore-starter-primer-follow-through", payload.starterPrimerFollowThrough, "No travel starter-primer follow-through is available yet.");',
        'setText("restore-first-session-briefing-follow-through", payload.firstSessionBriefingFollowThrough, "No travel first-session briefing follow-through is available yet.");',
        "starter_onboarding:mobile",
        "first_session_briefing:mobile",
        "scripts/verify_next90_m119_mobile_onboarding_continuity.py",
    ],
    "mobile_artifact_shelf": [
        "Mobile artifact shelf criteria (M117)",
        "next90-m117-mobile-artifact-shelf",
        'Assert(projection.ArtifactShelfSelectionSummary.Contains("My stuff shelf:", StringComparison.Ordinal)',
        'Assert(projection.ArtifactShelfViews.Count == 4',
        'Assert(projection.ArtifactShelfViews.Any(item => item.ViewId == "travel" && item.Label == "Travel cache" && item.Href == "/artifacts/session-redmond?role=Player&view=travel")',
        'Assert(travelProjection.SelectedArtifactView == "travel"',
        'Assert(travelProjection.ArtifactShelfSelectionSummary.Contains("Travel shelf:", StringComparison.Ordinal)',
        'Assert(observerProjection.ArtifactShelfViews.Any(item => item.ViewId == "travel" && item.Href.Contains("role=Observer", StringComparison.Ordinal))',
        'Assert(gmProjection.ArtifactShelfViews.Any(item => item.ViewId == "travel" && item.Href.Contains("role=GameMaster", StringComparison.Ordinal))',
        'Assert(recapProjection.SelectedRecapArtifactSummary.Contains("travel shelf", StringComparison.OrdinalIgnoreCase)',
        'setText("workspace-artifact-shelf-summary", payload.artifactShelfSelectionSummary, "No mobile artifact shelf summary is available yet.");',
        'setText("workspace-artifact-selection", payload.selectedRecapArtifactSummary, "Selected recap artifact: no recap artifact is pinned yet.");',
        'setLink("workspace-artifact-selection-link", payload.selectedRecapArtifactHref || selectedArtifactView?.href, artifactId',
        'document.getElementById("workspace-artifact-shelf-link").href = selectedArtifactView?.href || "/artifacts";',
        '? `Browse ${selectedArtifactView.label}`',
        'const artifactView = params.get("artifactView") || "";',
        'const artifactId = params.get("artifactId") || "";',
        'workspaceQuery.set("artifactView", artifactView);',
        'workspaceQuery.set("artifactId", artifactId);',
        'renderWorkspace(payload, artifactId);',
        'Assert(artifactShelfBrowseRedirect.Location == $"/index.html?sessionId={Uri.EscapeDataString(sessionId)}&role={Uri.EscapeDataString(role.ToString())}&artifactView=campaign"',
        'Assert(artifactShelfRedirect.Location == $"/index.html?sessionId={Uri.EscapeDataString(sessionId)}&role={Uri.EscapeDataString(role.ToString())}&artifactView=travel&artifactId=artifact-recap"',
        "artifact_shelf:mobile",
        "artifact_recap_view:mobile",
        "scripts/verify_next90_m117_mobile_artifact_shelf.py",
    ],
    "mobile_live_combat_confidence": [
        "Mobile live combat confidence criteria (M121)",
        "next90-m121-mobile-add-player-table-cards-between-turn-affordances-and-gm-l",
        'Assert(playerDescriptor.Summary.Contains("player table cards", StringComparison.OrdinalIgnoreCase)',
        'Assert(playerDescriptor.Summary.Contains("between-turn", StringComparison.OrdinalIgnoreCase)',
        'Assert(gmDescriptor.Summary.Contains("GM-lite continuity", StringComparison.Ordinal)',
        'Assert(projection.PlayerTableCardsSummary.Contains("Player table cards:", StringComparison.Ordinal)',
        'Assert(projection.PlayerTableCardLabels.Any(item => item.Contains("Initiative lane:", StringComparison.Ordinal))',
        'Assert(projection.BetweenTurnAffordancesSummary.Contains("Between-turn affordances:", StringComparison.Ordinal)',
        'Assert(projection.BetweenTurnAffordanceLabels.Any(item => item.Contains("Ready lane:", StringComparison.Ordinal))',
        'Assert(projection.GmLiteContinuitySummary.Contains("GM-lite continuity:", StringComparison.Ordinal)',
        'Assert(projection.GmLiteContinuityLabels.Any(item => item.Contains("Objective lane:", StringComparison.Ordinal))',
        'Assert(observerProjection.GmLiteContinuitySummary.Contains("observer lane", StringComparison.OrdinalIgnoreCase)',
        'Assert(gmProjection.PlayerTableCardsSummary.Contains("Advance Initiative", StringComparison.Ordinal)',
        'Assert(gmProjection.GmLiteContinuitySummary.Contains("GM runboard", StringComparison.Ordinal)',
        'setText("workspace-player-table-cards", payload.playerTableCardsSummary, "No player table-card summary is available yet.");',
        'setList("workspace-player-table-cards-list", payload.playerTableCardLabels);',
        'setText("workspace-between-turn-affordances", payload.betweenTurnAffordancesSummary, "No between-turn affordance summary is available yet.");',
        'setList("workspace-between-turn-affordances-list", payload.betweenTurnAffordanceLabels);',
        'setText("workspace-gm-lite-continuity", payload.gmLiteContinuitySummary, "No GM-lite continuity summary is available yet.");',
        'setList("workspace-gm-lite-continuity-list", payload.gmLiteContinuityLabels);',
        'id="workspace-player-table-cards"',
        'id="workspace-player-table-cards-list"',
        'id="workspace-between-turn-affordances"',
        'id="workspace-between-turn-affordances-list"',
        'id="workspace-gm-lite-continuity"',
        'id="workspace-gm-lite-continuity-list"',
        "player table cards",
        "between-turn affordances",
        "GM-lite continuity views",
        "add_player_table_cards_between:mobile",
        "scripts/verify_next90_m121_mobile_live_combat_confidence.py",
    ],
    "mobile_runner_goal_updates": [
        "Mobile runner-goal updates and consequence-feed criteria (M122)",
        "next90-m122-mobile-add-mobile-runner-goal-updates-and-player-safe-consequen",
        'Assert(projection.RunnerGoalUpdatesSummary.Contains("Runner goal updates:", StringComparison.Ordinal)',
        'Assert(projection.RunnerGoalUpdatesSummary.Contains("Return moments stay player-safe", StringComparison.Ordinal)',
        'Assert(projection.RunnerGoalUpdateLabels.Any(item => item.Contains("Goal checkpoint lane:", StringComparison.Ordinal))',
        'Assert(projection.RunnerGoalUpdateLabels.Any(item => item.Contains("Goal signal lane:", StringComparison.Ordinal))',
        'Assert(projection.RunnerGoalUpdateLabels.Any(item => item.Contains("Goal route lane:", StringComparison.Ordinal))',
        'Assert(projection.RunnerGoalUpdateLabels.Any(item => item.Contains("Goal boundary lane:", StringComparison.Ordinal))',
        'Assert(projection.PlayerSafeConsequenceFeedSummary.Contains("Player-safe consequence feed:", StringComparison.Ordinal)',
        'Assert(projection.PlayerSafeConsequenceFeedSummary.Contains("BLACK LEDGER world truth", StringComparison.Ordinal)',
        'Assert(projection.PlayerSafeConsequenceFeedLabels.Any(item => item.Contains("Consequence lane:", StringComparison.Ordinal))',
        'Assert(projection.PlayerSafeConsequenceFeedLabels.Any(item => item.Contains("Spoiler lane:", StringComparison.Ordinal))',
        'Assert(projection.PlayerSafeConsequenceFeedLabels.Any(item => item.Contains("Return lane:", StringComparison.Ordinal))',
        'Assert(projection.PlayerSafeConsequenceFeedLabels.Any(item => item.Contains("Trust lane:", StringComparison.Ordinal))',
        'Assert(observerProjection.RunnerGoalUpdatesSummary.Contains("Runner goal updates:", StringComparison.Ordinal)',
        'Assert(observerProjection.PlayerSafeConsequenceFeedSummary.Contains("Player-safe consequence feed:", StringComparison.Ordinal)',
        'Assert(gmProjection.RunnerGoalUpdatesSummary.Contains("GM runboard", StringComparison.Ordinal)',
        'Assert(gmProjection.PlayerSafeConsequenceFeedLabels.Any(item => item.Contains("Trust lane:", StringComparison.Ordinal))',
        'setText("workspace-runner-goal-updates", payload.runnerGoalUpdatesSummary, "No runner-goal update summary is available yet.");',
        'setList("workspace-runner-goal-update-list", payload.runnerGoalUpdateLabels);',
        'setText("workspace-player-safe-consequence-feed", payload.playerSafeConsequenceFeedSummary, "No player-safe consequence feed summary is available yet.");',
        'setList("workspace-player-safe-consequence-feed-list", payload.playerSafeConsequenceFeedLabels);',
        'id="workspace-runner-goal-updates"',
        'id="workspace-runner-goal-update-list"',
        'id="workspace-player-safe-consequence-feed"',
        'id="workspace-player-safe-consequence-feed-list"',
        "runner-goal return updates",
        "player-safe consequence feed views",
        "add_mobile_runner_goal_updates:mobile",
        "scripts/verify_next90_m122_mobile_runner_goal_updates.py",
    ],
    "quick_explain_follow_up": [
        "Successor-wave explain and follow-up criteria (M145)",
        'Assert(projection.QuickExplainSummary.Contains("Quick explain:", StringComparison.Ordinal)',
        'Assert(projection.SourceAnchorSummary.Contains("Source anchors:", StringComparison.Ordinal)',
        'Assert(projection.StaleStatePosture.Contains("Stale-state posture: green", StringComparison.Ordinal)',
        'Assert(projection.GroundedFollowUpSummary.Contains("Grounded follow-up:", StringComparison.Ordinal)',
        'Assert(projection.GroundedFollowUpLabels.Any(item => item.Contains("Boundary lane:", StringComparison.Ordinal))',
        'setText("workspace-quick-explain", payload.quickExplainSummary, "No quick explain summary is available yet.");',
        'setList("workspace-quick-explain-list", payload.quickExplainLabels);',
        'setText("workspace-source-anchor", payload.sourceAnchorSummary, "No source-anchor context is available yet.");',
        'setList("workspace-source-anchor-list", payload.sourceAnchorLabels);',
        'setText("workspace-stale-posture", payload.staleStatePosture, "No stale-state posture is available yet.");',
        'setText("workspace-grounded-follow-up", payload.groundedFollowUpSummary, "No grounded follow-up summary is available yet.");',
        'setList("workspace-grounded-follow-up-list", payload.groundedFollowUpLabels);',
        'id="workspace-quick-explain"',
        'id="workspace-source-anchor"',
        'id="workspace-stale-posture"',
        'id="workspace-grounded-follow-up"',
        "packet-backed quick explain",
        "source-anchor context",
        "grounded text-first follow-up bounded to the claimed live-play shell",
    ],
}

# These five proof sets intentionally replace the pre-install-boundary marker lists
# above.  The old source text remains in this file only as historical migration
# context; release materialization evaluates the current two-boundary contract.
REQUIRED_MARKERS["quality_release_hardening"] = [
    "function normalizePlayRouteForMobileShell(href, roleFallback, sessionIdFallback, deviceFallback)",
    "void sessionIdFallback;",
    "void deviceFallback;",
    "return `/mobile/${mode}`;",
    "Post-closure completion criteria (M12)",
    "Post-closure hardening criteria (M13)",
    "Post-closure role-depth criteria (M14)",
    "Release-proof cadence criteria:",
]
REQUIRED_MARKERS["pwa_runtime_smoke"] = [
    "mobile_pwa_runtime_smoke ok",
    "public_install_boundary: /mobile /mobile/player /mobile/gm /mobile/observer",
    "public_authority: none",
    "query_parameters_grant_access: false",
    "live_session_boundary: /mobile/live",
    "live_grant_source: trusted_server_headers",
    "live_owner_route: /mobile/live",
    "private_api_boundary: online 200 private,no-store",
    "PlaySessionGrantPolicy.TryResolve",
    "RequireTrustedMobileLiveGrantBoundaryAsync",
    "data-session-grant-backed=\"true\"",
    "python3 scripts/verify_mobile_pwa_runtime_smoke.py",
]
REQUIRED_MARKERS["mobile_pwa_viewport_smoke"] = [
    "mobile_pwa_viewport_smoke ok",
    "public_install_phone_layouts: player / gm / observer",
    "public_install_desktop_layout: 3 columns",
    "manifest_start_urls: clean player / gm / observer",
    "live_session_viewport: 390x844 /mobile/live",
    "live_touch_target_min:",
    "Page.getInstallabilityErrors",
    "python3 scripts/verify_mobile_pwa_viewport_smoke.py",
]
REQUIRED_MARKERS["mobile_pwa_analytics_smoke"] = [
    "mobile_pwa_analytics_smoke ok",
    "public_install_analytics: disabled",
    "live_analytics_route: /mobile/live",
    "live_analytics_role_source: trusted_server_headers",
    "privacy_blocked: dnt_gpc",
    "analytics_default_disabled: true",
    "secret_leak_free: true",
    "function isAnalyticsBlocked()",
    "return /session|device|token|continuity|owner|secret|key|href|url/i.test(key);",
    "python3 scripts/verify_mobile_pwa_analytics_smoke.py",
]
REQUIRED_MARKERS["turn_companion_live_session"] = [
    '@page "/mobile/live"',
    "PlaySessionGrantPolicy.ResolveCurrent",
    "The live companion requires a validated server session grant.",
    "PlayRouteHandlers.BuildMobileOwnerRoute()",
    'return "/mobile/live";',
    "query parameters must not grant live companion access",
    "chummer-play-analytics-config",
    "RybbitAnalyticsEnabled",
    "RYBBIT_CHUMMER_PLAY_SITE_ID",
    "RYBBIT_CHUMMER_PLAY_SCRIPT_URL",
    "RYBBIT_CHUMMER_PLAY_ALLOW_SAME_HOST_PROXY",
    "endpoint.Scheme == Uri.UriSchemeHttps",
    "mobile_shell_open",
    "mobile_role_switch",
    '"/manifest.player.webmanifest"',
    '"/manifest.gm.webmanifest"',
    '"/manifest.observer.webmanifest"',
    '"id": "/mobile/player"',
    '"id": "/mobile/gm"',
    '"id": "/mobile/observer"',
    '"scope": "/mobile/"',
    '"purpose": "any maskable"',
    'id="turn-action-grid"',
    'id="turn-history-list"',
    'id="turn-runsite-summary"',
    'id="runsite-anchor"',
]


PACKAGE_RECEIPTS = [
    {
        "package_id": "next90-m112-mobile-campaign-continuity",
        "title": "Make travel and mobile campaign continuity explicit for stale, cached, and action-required campaign state.",
        "milestone_id": "112",
        "work_task_id": "112.4",
        "frontier_id": "3720982159",
        "active_flagship_frontier_id": M112_ACTIVE_FLAGSHIP_FRONTIER_ID,
        "repo": REPO_LABEL,
        "checkout_root": CHECKOUT_ROOT,
        "allowed_paths": [
            "src",
            "tests",
            "docs",
            "scripts",
        ],
        "status": "implemented",
        "proof_marker_set": "mobile_campaign_continuity",
        "owned_surfaces": [
            "campaign_memory:travel",
            "campaign_state:mobile",
        ],
        "proof_receipt": "docs/next90-m112-mobile-campaign-continuity.proof.md",
    },
    {
        "package_id": "next90-m119-mobile-onboarding-continuity",
        "title": "Add travel and mobile starter continuity for primer and briefing artifacts.",
        "milestone_id": "119",
        "work_task_id": "119.3",
        "frontier_id": "2766704797",
        "repo": REPO_LABEL,
        "checkout_root": CHECKOUT_ROOT,
        "allowed_paths": [
            "src",
            "tests",
            "docs",
            "scripts",
        ],
        "status": "implemented",
        "proof_marker_set": "mobile_onboarding_continuity",
        "owned_surfaces": [
            "starter_onboarding:mobile",
            "first_session_briefing:mobile",
        ],
        "proof_receipt": "docs/next90-m119-mobile-onboarding-continuity.proof.md",
    },
    {
        "package_id": "next90-m121-mobile-add-player-table-cards-between-turn-affordances-and-gm-l",
        "title": "Add player table cards, between-turn affordances, and GM-lite continuity views for live combat confidence.",
        "milestone_id": "121",
        "work_task_id": "121.4",
        "frontier_id": "6121780841",
        "repo": REPO_LABEL,
        "checkout_root": CHECKOUT_ROOT,
        "allowed_paths": [
            "src",
            "tests",
            "docs",
            "scripts",
        ],
        "status": "implemented",
        "proof_marker_set": "mobile_live_combat_confidence",
        "owned_surfaces": [
            "add_player_table_cards_between:mobile",
        ],
        "proof_receipt": "docs/next90-m121-mobile-live-combat-confidence.proof.md",
    },
    {
        "package_id": "next90-m145-mobile-quick-explain-and-follow-up",
        "milestone_id": "145",
        "work_task_id": "145.3",
        "frontier_id": "1453045303",
        "status": "closed",
        "proof_marker_set": "quick_explain_follow_up",
        "owned_surfaces": [
            "quick_explain:mobile",
            "grounded_follow_up:mobile",
        ],
        "proof_receipt": "docs/next90-m145-mobile-quick-explain-and-follow-up.proof.md",
    },
    {
        "package_id": "next90-m122-mobile-add-mobile-runner-goal-updates-and-player-safe-consequen",
        "title": "Add mobile runner-goal updates and player-safe consequence feed views for campaign return moments.",
        "milestone_id": "122",
        "work_task_id": "122.4",
        "frontier_id": "8138838792",
        "repo": REPO_LABEL,
        "checkout_root": CHECKOUT_ROOT,
        "allowed_paths": [
            "src",
            "tests",
            "docs",
            "scripts",
        ],
        "status": "implemented",
        "proof_marker_set": "mobile_runner_goal_updates",
        "owned_surfaces": [
            "add_mobile_runner_goal_updates:mobile",
        ],
        "proof_receipt": "docs/next90-m122-mobile-runner-goal-updates-and-consequence-feed.proof.md",
    },
    {
        "package_id": "next90-m117-mobile-artifact-shelf",
        "milestone_id": "117",
        "work_task_id": "117.4",
        "title": "Add mobile artifact shelf views for campaign, travel, and recap artifacts.",
        "frontier_id": M117_SUCCESSOR_FRONTIER_ID,
        "frontier_ids": M117_FRONTIER_IDS,
        "active_flagship_frontier_id": M117_ACTIVE_FLAGSHIP_FRONTIER_ID,
        "repo": REPO_LABEL,
        "checkout_root": CHECKOUT_ROOT,
        "allowed_paths": [
            "src",
            "tests",
            "docs",
            "scripts",
        ],
        "status": "closed",
        "proof_marker_set": "mobile_artifact_shelf",
        "owned_surfaces": [
            "artifact_shelf:mobile",
            "artifact_recap_view:mobile",
        ],
        "proof_receipt": "docs/next90-m117-mobile-artifact-shelf.proof.md",
    },
]


def iso_now() -> str:
    return dt.datetime.now(dt.timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z")


def sha256_file(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def load_required_smoke_receipts() -> tuple[list[dict[str, object]], list[str]]:
    summaries: list[dict[str, object]] = []
    errors: list[str] = []
    required_commands = [
        command
        for command in VERIFICATION_COMMANDS
        if command.get("required_before_materialize") is True
    ]
    required_ids = {
        str(command.get("id") or "")
        for command in required_commands
    }
    expected_ids = set(SMOKE_RECEIPT_CONTRACTS)
    if required_ids != expected_ids:
        errors.append(f"required smoke command set drifted: {sorted(required_ids ^ expected_ids)}")

    for command in required_commands:
        command_id = str(command.get("id") or "")
        if command_id not in SMOKE_RECEIPT_CONTRACTS:
            continue

        receipt_path = command.get("receipt_path")
        if not isinstance(receipt_path, str) or not receipt_path.strip():
            errors.append(f"{command_id} missing receipt_path")
            continue

        full_path = (ROOT / receipt_path).resolve()
        if not full_path.is_relative_to(ROOT):
            errors.append(f"{command_id} receipt_path escapes repo: {receipt_path}")
            continue
        if not full_path.is_file():
            errors.append(f"{command_id} receipt missing on disk: {receipt_path}")
            continue

        try:
            receipt_payload = json.loads(full_path.read_text(encoding="utf-8"))
        except json.JSONDecodeError as exc:
            errors.append(f"{command_id} receipt is invalid json: {receipt_path}: {exc}")
            continue

        expected_contract = SMOKE_RECEIPT_CONTRACTS[command_id]
        contract_name = receipt_payload.get("contract_name")
        status = receipt_payload.get("status")
        generated_at_utc = receipt_payload.get("generated_at_utc")
        if contract_name != expected_contract:
            errors.append(f"{command_id} receipt contract drifted: {contract_name!r}")
        if status != "pass":
            errors.append(f"{command_id} receipt status is not pass: {status!r}")
        if not isinstance(generated_at_utc, str) or not generated_at_utc.strip():
            errors.append(f"{command_id} receipt missing generated_at_utc")

        summaries.append(
            {
                "id": command_id,
                "command": command.get("command"),
                "receipt_path": receipt_path,
                "contract_name": contract_name,
                "status": status,
                "generated_at_utc": generated_at_utc,
                "required_before_materialize": True,
            }
        )

    return summaries, errors


def load_json_object(path: Path, label: str, errors: list[str]) -> dict[str, object]:
    if not path.is_file():
        errors.append(f"{label} missing on disk: {path.relative_to(ROOT)}")
        return {}
    try:
        loaded = json.loads(path.read_text(encoding="utf-8"))
    except json.JSONDecodeError as exc:
        errors.append(f"{label} invalid json: {exc}")
        return {}
    if not isinstance(loaded, dict):
        errors.append(f"{label} root must be an object")
        return {}
    return loaded


def shortcut_urls(manifest: dict[str, object]) -> list[str]:
    shortcuts = manifest.get("shortcuts")
    if not isinstance(shortcuts, list):
        return []
    urls: list[str] = []
    for shortcut in shortcuts:
        if isinstance(shortcut, dict) and isinstance(shortcut.get("url"), str):
            urls.append(str(shortcut["url"]))
    return urls


def icon_purposes(manifest: dict[str, object]) -> list[str]:
    icons = manifest.get("icons")
    if not isinstance(icons, list):
        return []
    purposes: list[str] = []
    for icon in icons:
        if isinstance(icon, dict) and isinstance(icon.get("purpose"), str):
            purposes.append(str(icon["purpose"]))
    return sorted(set(purposes))


def role_launch(runtime_payload: dict[str, object], launch_id: str) -> dict[str, object]:
    launches = runtime_payload.get("hero_launches")
    if not isinstance(launches, dict):
        return {}
    launch = launches.get(launch_id)
    return launch if isinstance(launch, dict) else {}


def viewport_manifest(viewport_payload: dict[str, object], role_id: str) -> dict[str, object]:
    manifests = viewport_payload.get("manifests")
    if not isinstance(manifests, dict):
        return {}
    manifest = manifests.get(role_id)
    return manifest if isinstance(manifest, dict) else {}


def analytics_handoff(analytics_payload: dict[str, object], handoff_id: str) -> dict[str, object]:
    handoffs = analytics_payload.get("handoff")
    if not isinstance(handoffs, dict):
        return {}
    handoff = handoffs.get(handoff_id)
    return handoff if isinstance(handoff, dict) else {}


def has_route_fragment(payload: dict[str, object], fragment: str) -> bool:
    return fragment in str(payload.get("route") or "")


def has_placeholder_local_origin(payload: dict[str, object], route_prefix: str) -> bool:
    return str(payload.get("route") or "").startswith(f"{LOCAL_ORIGIN_PLACEHOLDER}{route_prefix}")


def build_role_pwa_contract() -> tuple[dict[str, object], list[str]]:
    errors: list[str] = []
    runtime_payload = load_json_object(ROOT / ".codex-studio/published/MOBILE_PWA_RUNTIME_SMOKE.generated.json", "runtime smoke receipt", errors)
    viewport_payload = load_json_object(ROOT / ".codex-studio/published/MOBILE_PWA_VIEWPORT_SMOKE.generated.json", "viewport smoke receipt", errors)
    analytics_payload = load_json_object(ROOT / ".codex-studio/published/MOBILE_PWA_ANALYTICS_SMOKE.generated.json", "analytics smoke receipt", errors)
    generic_manifest = load_json_object(GENERIC_MANIFEST, "generic mobile manifest", errors)
    player_manifest = load_json_object(PLAYER_MANIFEST, "player mobile manifest", errors)
    gm_manifest = load_json_object(GM_MANIFEST, "GM mobile manifest", errors)

    offline = runtime_payload.get("offline") if isinstance(runtime_payload.get("offline"), dict) else {}
    private_api = runtime_payload.get("private_api_boundary") if isinstance(runtime_payload.get("private_api_boundary"), dict) else {}
    handoff_receivers = offline.get("handoff_receivers") if isinstance(offline.get("handoff_receivers"), dict) else {}
    provider_script = analytics_payload.get("provider_script") if isinstance(analytics_payload.get("provider_script"), dict) else {}
    privacy = analytics_payload.get("privacy") if isinstance(analytics_payload.get("privacy"), dict) else {}
    role_analytics = analytics_payload.get("role_analytics") if isinstance(analytics_payload.get("role_analytics"), dict) else {}
    standalone_install = viewport_payload.get("standalone_install_ui") if isinstance(viewport_payload.get("standalone_install_ui"), dict) else {}
    player_viewport_manifest = viewport_manifest(viewport_payload, "player")
    gm_viewport_manifest = viewport_manifest(viewport_payload, "gm")
    gm_query_viewport_manifest = viewport_manifest(viewport_payload, "query_role_gm")

    roles = [
        {
            "role": "Player",
            "mode": "player",
            "route": "/mobile/player",
            "manifest_path": "src/Chummer.Play.Web/wwwroot/manifest.player.webmanifest",
            "manifest_id": player_manifest.get("id"),
            "manifest_start_url": player_manifest.get("start_url"),
            "manifest_scope": player_manifest.get("scope"),
            "manifest_display": player_manifest.get("display"),
            "manifest_shortcut_urls": shortcut_urls(player_manifest),
            "manifest_icon_purposes": icon_purposes(player_manifest),
            "installability_error_count": viewport_manifest(viewport_payload, "player").get("installability_error_count"),
            "hero_launch": role_launch(runtime_payload, "player"),
            "hero_dropdown_launch": role_launch(runtime_payload, "menu_player"),
            "standalone_install_button": standalone_install.get("player_button"),
            "handoff_routes": {
                "clipboard": analytics_handoff(analytics_payload, "clipboard_player"),
                "native": analytics_handoff(analytics_payload, "native_player"),
                "link": analytics_handoff(analytics_payload, "link_player"),
            },
        },
        {
            "role": "GameMaster",
            "mode": "gm",
            "route": "/mobile/gm",
            "manifest_path": "src/Chummer.Play.Web/wwwroot/manifest.gm.webmanifest",
            "manifest_id": gm_manifest.get("id"),
            "manifest_start_url": gm_manifest.get("start_url"),
            "manifest_scope": gm_manifest.get("scope"),
            "manifest_display": gm_manifest.get("display"),
            "manifest_shortcut_urls": shortcut_urls(gm_manifest),
            "manifest_icon_purposes": icon_purposes(gm_manifest),
            "installability_error_count": viewport_manifest(viewport_payload, "gm").get("installability_error_count"),
            "hero_launch": role_launch(runtime_payload, "gm"),
            "hero_dropdown_launch": role_launch(runtime_payload, "menu_gm"),
            "standalone_install_button": standalone_install.get("gm_button"),
            "handoff_routes": {
                "clipboard": analytics_handoff(analytics_payload, "clipboard_gm"),
                "native": analytics_handoff(analytics_payload, "native_gm"),
                "link": analytics_handoff(analytics_payload, "link_gm"),
            },
        },
    ]

    checks = {
        "generic_manifest_exposes_role_shortcuts": set(shortcut_urls(generic_manifest)) >= {"/mobile/player?role=Player", "/mobile/gm?role=GameMaster"},
        "player_manifest_direct_launch": player_manifest.get("id") == "/mobile/player" and player_manifest.get("start_url") == "/mobile/player?role=Player",
        "gm_manifest_direct_launch": gm_manifest.get("id") == "/mobile/gm" and gm_manifest.get("start_url") == "/mobile/gm?role=GameMaster",
        "role_manifests_share_mobile_scope": player_manifest.get("scope") == "/mobile/" and gm_manifest.get("scope") == "/mobile/",
        "role_manifests_are_standalone": player_manifest.get("display") == "standalone" and gm_manifest.get("display") == "standalone",
        "hero_dropdown_play_opens_player_and_gm": role_launch(runtime_payload, "menu_player").get("mode") == "player" and role_launch(runtime_payload, "menu_gm").get("mode") == "gm",
        "direct_hero_play_opens_player_and_gm": role_launch(runtime_payload, "player").get("mode") == "player" and role_launch(runtime_payload, "gm").get("mode") == "gm",
        "interactive_blazor_shell_proven": runtime_payload.get("blazor_shell") == "interactive-server" and runtime_payload.get("blazor_boot_script") == "/_framework/blazor.web.js",
        "service_worker_offline_shell_proven": runtime_payload.get("service_worker_controlled") is True and runtime_payload.get("service_worker_cache") == "chummer-shell-play-shell-v16",
        "offline_player_queue_replay_proven": offline.get("player_queue_replay") == "local 1->0 / server 0->1->0 / ammo 8->7",
        "offline_gm_queue_replay_proven": offline.get("gm_queue_replay") == "local 1->0 / server 0->1->0 / gm-advance-initiative",
        "device_neutral_handoff_receivers_proven": handoff_receivers.get("device_neutral") is True,
        "private_play_api_fails_closed_offline": private_api.get("online_status") == 200 and private_api.get("offline_status") == 503 and private_api.get("offline_error") == "play_api_network_unavailable",
        "rybbit_default_disabled": privacy.get("default_disabled") is True and privacy.get("default_provider_requests") == 0 and privacy.get("default_event_count") == 0,
        "rybbit_dnt_gpc_blocked": privacy.get("dnt_gpc_blocked") is True and privacy.get("privacy_provider_requests") == 0 and privacy.get("privacy_event_count") == 0,
        "rybbit_secret_leak_free": privacy.get("secret_leak_free") is True,
        "rybbit_skips_and_masks_mobile_routes": provider_script.get("skip_patterns") == "[\"/mobile\",\"/mobile/**\"]" and provider_script.get("mask_patterns") == "[\"/mobile\",\"/mobile/**\",\"/api/play/**\"]",
        "role_analytics_cover_browser_and_standalone": role_analytics.get("shell_open_roles") == ["player", "gm"] and role_analytics.get("shell_open_display_modes") == ["browser", "standalone"],
        "role_switch_analytics_cover_both_directions": isinstance(role_analytics.get("role_switches"), dict) and role_analytics.get("role_switches", {}).get("player_to") == "gm" and role_analytics.get("role_switches", {}).get("gm_to") == "player",
        "viewport_manifest_urls_use_placeholder_origin": player_viewport_manifest.get("url") == f"{LOCAL_ORIGIN_PLACEHOLDER}/manifest.player.webmanifest"
        and gm_viewport_manifest.get("url") == f"{LOCAL_ORIGIN_PLACEHOLDER}/manifest.gm.webmanifest"
        and gm_query_viewport_manifest.get("url") == f"{LOCAL_ORIGIN_PLACEHOLDER}/manifest.gm.webmanifest",
        "handoff_routes_preserve_role_and_mint_receiver_device": all(
            [
                has_route_fragment(analytics_handoff(analytics_payload, "clipboard_player"), "/mobile/player?sessionId=<session>&role=Player"),
                has_route_fragment(analytics_handoff(analytics_payload, "native_player"), "/mobile/player?sessionId=<session>&role=Player"),
                has_route_fragment(analytics_handoff(analytics_payload, "link_player"), "/mobile/player?sessionId=<session>&role=Player"),
                has_route_fragment(analytics_handoff(analytics_payload, "clipboard_gm"), "/mobile/gm?sessionId=<session>&role=GameMaster"),
                has_route_fragment(analytics_handoff(analytics_payload, "native_gm"), "/mobile/gm?sessionId=<session>&role=GameMaster"),
                has_route_fragment(analytics_handoff(analytics_payload, "link_gm"), "/mobile/gm?sessionId=<session>&role=GameMaster"),
                analytics_handoff(analytics_payload, "clipboard_player").get("receiver_device") == "<minted-device>",
                analytics_handoff(analytics_payload, "native_player").get("receiver_device") == "<minted-device>",
                analytics_handoff(analytics_payload, "link_player").get("receiver_device") == "<minted-device>",
                analytics_handoff(analytics_payload, "clipboard_gm").get("receiver_device") == "<minted-device>",
                analytics_handoff(analytics_payload, "native_gm").get("receiver_device") == "<minted-device>",
                analytics_handoff(analytics_payload, "link_gm").get("receiver_device") == "<minted-device>",
            ]
        ),
        "handoff_routes_use_placeholder_origin": all(
            [
                has_placeholder_local_origin(analytics_handoff(analytics_payload, "clipboard_player"), "/mobile/player"),
                has_placeholder_local_origin(analytics_handoff(analytics_payload, "native_player"), "/mobile/player"),
                has_placeholder_local_origin(analytics_handoff(analytics_payload, "link_player"), "/mobile/player"),
                has_placeholder_local_origin(analytics_handoff(analytics_payload, "clipboard_gm"), "/mobile/gm"),
                has_placeholder_local_origin(analytics_handoff(analytics_payload, "native_gm"), "/mobile/gm"),
                has_placeholder_local_origin(analytics_handoff(analytics_payload, "link_gm"), "/mobile/gm"),
            ]
        ),
    }

    for key, value in checks.items():
        if value is not True:
            errors.append(f"role_pwa_contract check failed: {key}")

    contract = {
        "status": "pass" if not errors else "fail",
        "contract_name": "chummer6-mobile.role_pwa_contract.v1",
        "roles": roles,
        "checks": checks,
        "offline_online": {
            "service_worker_cache": runtime_payload.get("service_worker_cache"),
            "service_worker_controlled": runtime_payload.get("service_worker_controlled"),
            "player_queue_replay": offline.get("player_queue_replay"),
            "gm_queue_replay": offline.get("gm_queue_replay"),
            "private_api_boundary": private_api,
        },
        "session_handoff": {
            "device_neutral_receivers": handoff_receivers,
            "share_methods": ["clipboard", "native", "link"],
            "sender_device_id_stripped": True,
            "receiver_device_id_minted": checks["handoff_routes_preserve_role_and_mint_receiver_device"],
        },
        "rybbit": {
            "site_id": provider_script.get("site_id"),
            "script_path": provider_script.get("script_path"),
            "skip_patterns": provider_script.get("skip_patterns"),
            "mask_patterns": provider_script.get("mask_patterns"),
            "default_disabled": privacy.get("default_disabled"),
            "dnt_gpc_blocked": privacy.get("dnt_gpc_blocked"),
            "secret_leak_free": privacy.get("secret_leak_free"),
            "events": analytics_payload.get("events") if isinstance(analytics_payload.get("events"), list) else [],
        },
        "source_receipts": {
            "runtime": ".codex-studio/published/MOBILE_PWA_RUNTIME_SMOKE.generated.json",
            "viewport": ".codex-studio/published/MOBILE_PWA_VIEWPORT_SMOKE.generated.json",
            "analytics": ".codex-studio/published/MOBILE_PWA_ANALYTICS_SMOKE.generated.json",
        },
    }
    return contract, errors


def build_role_pwa_contract_v2() -> tuple[dict[str, object], list[str]]:
    errors: list[str] = []
    runtime = load_json_object(
        ROOT / ".codex-studio/published/MOBILE_PWA_RUNTIME_SMOKE.generated.json",
        "runtime smoke receipt",
        errors,
    )
    viewport = load_json_object(
        ROOT / ".codex-studio/published/MOBILE_PWA_VIEWPORT_SMOKE.generated.json",
        "viewport smoke receipt",
        errors,
    )
    analytics = load_json_object(
        ROOT / ".codex-studio/published/MOBILE_PWA_ANALYTICS_SMOKE.generated.json",
        "analytics smoke receipt",
        errors,
    )
    manifests = {
        "generic": load_json_object(GENERIC_MANIFEST, "generic mobile manifest", errors),
        "player": load_json_object(PLAYER_MANIFEST, "player mobile manifest", errors),
        "gm": load_json_object(GM_MANIFEST, "GM mobile manifest", errors),
        "observer": load_json_object(OBSERVER_MANIFEST, "observer mobile manifest", errors),
    }

    runtime_public = runtime.get("public_install_boundary") if isinstance(runtime.get("public_install_boundary"), dict) else {}
    runtime_live = runtime.get("live_session_boundary") if isinstance(runtime.get("live_session_boundary"), dict) else {}
    private_api = runtime.get("private_api_boundary") if isinstance(runtime.get("private_api_boundary"), dict) else {}
    viewport_public = viewport.get("public_install_boundary") if isinstance(viewport.get("public_install_boundary"), dict) else {}
    viewport_live = viewport.get("live_session_boundary") if isinstance(viewport.get("live_session_boundary"), dict) else {}
    viewport_manifests = viewport.get("manifests") if isinstance(viewport.get("manifests"), dict) else {}
    analytics_public = analytics.get("public_install_boundary") if isinstance(analytics.get("public_install_boundary"), dict) else {}
    analytics_live = analytics.get("live_session_boundary") if isinstance(analytics.get("live_session_boundary"), dict) else {}
    analytics_privacy = analytics.get("privacy") if isinstance(analytics.get("privacy"), dict) else {}
    phone_layouts = viewport_public.get("phone_layouts") if isinstance(viewport_public.get("phone_layouts"), dict) else {}
    desktop_layout = viewport_public.get("desktop_layout") if isinstance(viewport_public.get("desktop_layout"), dict) else {}

    expected_routes = ["/mobile", "/mobile/player", "/mobile/gm", "/mobile/observer"]
    manifest_expectations = {
        "player": "/mobile/player",
        "gm": "/mobile/gm",
        "observer": "/mobile/observer",
    }
    role_receipts: list[dict[str, object]] = []
    manifest_checks: list[bool] = []
    viewport_manifest_checks: list[bool] = []
    for mode, route in manifest_expectations.items():
        manifest = manifests[mode]
        viewport_manifest_receipt = viewport_manifests.get(mode) if isinstance(viewport_manifests.get(mode), dict) else {}
        manifest_ok = (
            manifest.get("id") == route
            and manifest.get("start_url") == route
            and manifest.get("scope") == "/mobile/"
            and manifest.get("display") == "standalone"
            and all("?" not in item for item in shortcut_urls(manifest))
        )
        viewport_manifest_ok = (
            viewport_manifest_receipt.get("url") == f"{LOCAL_ORIGIN_PLACEHOLDER}/manifest.{mode}.webmanifest"
            and viewport_manifest_receipt.get("id") == route
            and viewport_manifest_receipt.get("start_url") == route
            and viewport_manifest_receipt.get("scope") == "/mobile/"
            and viewport_manifest_receipt.get("installability_error_count") == 0
        )
        manifest_checks.append(manifest_ok)
        viewport_manifest_checks.append(viewport_manifest_ok)
        role_receipts.append(
            {
                "mode": mode,
                "route": route,
                "authority": "none",
                "manifest_id": manifest.get("id"),
                "manifest_start_url": manifest.get("start_url"),
                "manifest_scope": manifest.get("scope"),
                "manifest_shortcut_urls": shortcut_urls(manifest),
                "manifest_icon_purposes": icon_purposes(manifest),
                "installability_error_count": viewport_manifest_receipt.get("installability_error_count"),
            }
        )

    checks = {
        "public_install_routes_are_distinct_and_authority_free": runtime_public.get("routes") == expected_routes
        and runtime_public.get("authority") == "none"
        and runtime_public.get("live_state_loaded") is False
        and runtime_public.get("live_runtime_loaded") is False,
        "query_parameters_cannot_grant_live_access": runtime_public.get("query_parameters_grant_access") is False
        and runtime_live.get("query_parameters_grant_access") is False,
        "live_route_requires_trusted_server_grant": runtime_live.get("route") == "/mobile/live"
        and runtime_live.get("grant_source") == "trusted_server_headers"
        and runtime_live.get("owner_route") == "/mobile/live",
        "role_change_exits_live_authority": runtime_live.get("role_change_exit") == "/mobile/gm",
        "private_api_is_no_store": private_api.get("online_status") == 200
        and "private" in str(private_api.get("online_cache_control") or "").lower()
        and "no-store" in str(private_api.get("online_cache_control") or "").lower(),
        "manifests_are_clean_install_labels": all(manifest_checks),
        "manifest_shortcuts_are_authority_free": set(shortcut_urls(manifests["generic"]))
        == {"/mobile/player", "/mobile/gm", "/mobile/observer"},
        "viewport_manifests_are_installable_and_clean": all(viewport_manifest_checks),
        "public_phone_and_desktop_layouts_proven": viewport_public.get("authority") == "none"
        and viewport_public.get("query_parameters_grant_access") is False
        and set(phone_layouts) == {"player", "gm", "observer"}
        and all(isinstance(item, dict) and item.get("overflowFree") is True for item in phone_layouts.values())
        and desktop_layout.get("overflowFree") is True
        and int(desktop_layout.get("gridColumns") or 0) >= 3,
        "live_mobile_viewport_is_grant_backed": viewport_live.get("route") == "/mobile/live"
        and viewport_live.get("grant_source") == "trusted_server_headers",
        "public_install_analytics_are_disabled": analytics_public.get("analytics_enabled") is False
        and analytics_public.get("provider_requests") == 0
        and analytics_public.get("event_count") == 0,
        "live_analytics_are_sanitized_and_grant_backed": analytics_live.get("route") == "/mobile/live"
        and analytics_live.get("grant_source") == "trusted_server_headers"
        and analytics_live.get("secret_leak_free") is True,
        "analytics_privacy_controls_hold": analytics_privacy.get("dnt_gpc_blocked") is True
        and analytics_privacy.get("default_disabled") is True
        and analytics_privacy.get("secret_leak_free") is True,
    }
    for key, value in checks.items():
        if value is not True:
            errors.append(f"role_pwa_contract check failed: {key}")

    return (
        {
            "status": "pass" if not errors else "fail",
            "contract_name": "chummer6-mobile.role_pwa_contract.v2",
            "public_install_boundary": {
                "routes": expected_routes,
                "authority": "none",
                "roles": role_receipts,
            },
            "live_session_boundary": runtime_live,
            "private_api_boundary": private_api,
            "viewport_boundary": {
                "public": viewport_public,
                "live": viewport_live,
            },
            "analytics_boundary": {
                "public": analytics_public,
                "live": analytics_live,
                "privacy": analytics_privacy,
            },
            "checks": checks,
            "source_receipts": {
                "runtime": ".codex-studio/published/MOBILE_PWA_RUNTIME_SMOKE.generated.json",
                "viewport": ".codex-studio/published/MOBILE_PWA_VIEWPORT_SMOKE.generated.json",
                "analytics": ".codex-studio/published/MOBILE_PWA_ANALYTICS_SMOKE.generated.json",
            },
        },
        errors,
    )


def load_cross_surface_readiness() -> tuple[dict[str, object], list[str]]:
    errors: list[str] = []
    fleet_payload = load_json_object(FLEET_FLAGSHIP_READINESS, "fleet flagship readiness receipt", errors)
    coverage = fleet_payload.get("coverage") if isinstance(fleet_payload.get("coverage"), dict) else {}
    coverage_details = fleet_payload.get("coverage_details") if isinstance(fleet_payload.get("coverage_details"), dict) else {}
    readiness_planes = fleet_payload.get("readiness_planes") if isinstance(fleet_payload.get("readiness_planes"), dict) else {}
    warning_keys = fleet_payload.get("warning_keys") if isinstance(fleet_payload.get("warning_keys"), list) else []
    missing_keys = fleet_payload.get("missing_keys") if isinstance(fleet_payload.get("missing_keys"), list) else []
    ready_keys = fleet_payload.get("ready_keys") if isinstance(fleet_payload.get("ready_keys"), list) else []
    readiness_plane_gap_keys = fleet_payload.get("readiness_plane_gap_keys") if isinstance(fleet_payload.get("readiness_plane_gap_keys"), list) else []
    mobile_detail = coverage_details.get("mobile_play_shell") if isinstance(coverage_details.get("mobile_play_shell"), dict) else {}
    mobile_evidence = mobile_detail.get("evidence") if isinstance(mobile_detail.get("evidence"), dict) else {}
    flagship_ready = readiness_planes.get("flagship_ready") if isinstance(readiness_planes.get("flagship_ready"), dict) else {}
    blocking_keys = [
        str(item)
        for item in [*warning_keys, *missing_keys, *readiness_plane_gap_keys]
        if isinstance(item, str) and item.strip()
    ]
    non_mobile_blockers = sorted({item for item in blocking_keys if item != "mobile_play_shell"})

    checks = {
        "fleet_receipt_present": bool(fleet_payload),
        "fleet_mobile_play_shell_ready": coverage.get("mobile_play_shell") == "ready" and mobile_detail.get("status") == "ready",
        "fleet_mobile_local_release_passed": mobile_evidence.get("mobile_local_release_status") == "passed",
        "fleet_mobile_scope_not_listed_as_blocker": "mobile_play_shell" not in blocking_keys,
    }

    for key, value in checks.items():
        if value is not True:
            errors.append(f"cross_surface_readiness check failed: {key}")

    contract = {
        "status": "pass" if not errors else "fail",
        "contract_name": "chummer6-mobile.cross_surface_readiness.v1",
        "fleet_flagship_readiness_path": str(FLEET_FLAGSHIP_READINESS),
        "fleet_generated_at": fleet_payload.get("generated_at") or fleet_payload.get("generated_at_utc"),
        "fleet_status": fleet_payload.get("status"),
        "ready_keys": ready_keys,
        "warning_keys": warning_keys,
        "missing_keys": missing_keys,
        "readiness_plane_gap_keys": readiness_plane_gap_keys,
        "coverage": {
            "mobile_play_shell": coverage.get("mobile_play_shell"),
            "desktop_client": coverage.get("desktop_client"),
        },
        "mobile_play_shell": {
            "status": mobile_detail.get("status"),
            "summary": mobile_detail.get("summary"),
            "mobile_local_release_status": mobile_evidence.get("mobile_local_release_status"),
            "campaign_session_recover_recap_effective_state": mobile_evidence.get("campaign_session_recover_recap_effective_state"),
            "recover_from_sync_conflict_owner_scoped_effective_state": mobile_evidence.get("recover_from_sync_conflict_owner_scoped_effective_state"),
        },
        "flagship_ready": {
            "status": flagship_ready.get("status"),
            "summary": flagship_ready.get("summary"),
            "reasons": flagship_ready.get("reasons") if isinstance(flagship_ready.get("reasons"), list) else [],
        },
        "checks": checks,
        "non_mobile_blockers": non_mobile_blockers,
    }
    return contract, errors


def load_cross_surface_refresh() -> tuple[dict[str, object], list[str]]:
    errors: list[str] = []
    relative_script = str(MOBILE_CROSS_SURFACE_REFRESH_SCRIPT.relative_to(ROOT))
    relative_receipt = str(MOBILE_CROSS_SURFACE_REFRESH_RECEIPT.relative_to(ROOT))
    if not MOBILE_CROSS_SURFACE_REFRESH_RECEIPT.is_file():
        return (
            {
                "status": "not_materialized",
                "required": False,
                "script_path": relative_script,
                "receipt_path": relative_receipt,
            },
            errors,
        )

    receipt_payload = load_json_object(MOBILE_CROSS_SURFACE_REFRESH_RECEIPT, "mobile cross-surface readiness receipt", errors)
    checks = receipt_payload.get("checks") if isinstance(receipt_payload.get("checks"), dict) else {}
    public_edge = receipt_payload.get("public_edge") if isinstance(receipt_payload.get("public_edge"), dict) else {}
    fleet_readiness = receipt_payload.get("fleet_readiness") if isinstance(receipt_payload.get("fleet_readiness"), dict) else {}
    mobile_play_shell = fleet_readiness.get("mobile_play_shell") if isinstance(fleet_readiness.get("mobile_play_shell"), dict) else {}
    surface_source_fingerprint = (
        receipt_payload.get("surface_source_fingerprint")
        if isinstance(receipt_payload.get("surface_source_fingerprint"), dict)
        else {}
    )
    fingerprint_errors: list[str] = []
    fingerprint_paths: list[str] = []

    if receipt_payload.get("contract_name") != "chummer6-mobile.cross_surface_readiness_refresh.v1":
        errors.append("cross-surface refresh receipt contract drifted")
    refresh_status = receipt_payload.get("status")
    if refresh_status not in {"pass", "fail"}:
        errors.append(f"cross-surface refresh receipt status drifted: {refresh_status!r}")
    if not isinstance(receipt_payload.get("generated_at_utc"), str) or not str(receipt_payload.get("generated_at_utc")).strip():
        errors.append("cross-surface refresh receipt missing generated_at_utc")
    for key in [
        "fleet_mobile_play_shell_ready",
        "fleet_mobile_local_release_passed",
        "fleet_mobile_scope_not_blocking",
    ]:
        if checks.get(key) is not True:
            errors.append(f"cross-surface refresh check failed: {key}")

    if public_edge.get("skipped") is not True:
        if refresh_status == "pass":
            for key in [
                "public_edge_frontdoor_navigation_pass",
                "public_edge_frontdoor_route_is_player",
                "public_edge_handoff_launch_route_is_player",
                "public_edge_role_alias_routes_pass",
                "public_edge_public_targets_keep_play_only",
                "frontdoor_player_gm_blazor_shells_live",
                "frontdoor_player_gm_role_manifests_live",
                "frontdoor_player_gm_handoff_links_preserve_role_and_strip_device",
                "frontdoor_rybbit_roles_live",
            ]:
                if checks.get(key) is not True:
                    errors.append(f"cross-surface refresh check failed: {key}")
            for key in [
                "public_edge_pwa_static_pass",
                "public_edge_mobile_ledger_pass",
                "public_edge_gate_pass",
                "strict_public_edge_gate_pass",
            ]:
                if checks.get(key) is not True:
                    errors.append(f"cross-surface refresh strict public-edge check failed: {key}")
        elif refresh_status == "fail":
            failures = public_edge.get("failures") if isinstance(public_edge.get("failures"), list) else []
            if public_edge.get("status") != "fail":
                errors.append("cross-surface refresh public_edge status drifted for failed receipt")
            if checks.get("strict_public_edge_gate_pass") is True:
                errors.append("cross-surface refresh failed receipt still reports strict_public_edge_gate_pass")
            if not failures:
                errors.append("cross-surface refresh failed receipt is missing explicit public-edge blockers")
        for key in [
            "strict_postdeploy_strict_preflight",
            "strict_postdeploy_strict_invocation",
            "strict_postdeploy_strict_no_allowance_invocation",
        ]:
            if not isinstance(public_edge.get(key), bool):
                errors.append(f"cross-surface refresh public_edge {key} missing")
        live_build_lock_probe = public_edge.get("live_build_lock_probe") if isinstance(public_edge.get("live_build_lock_probe"), dict) else {}
        if not live_build_lock_probe:
            errors.append("cross-surface refresh live_build_lock_probe missing")
        else:
            if live_build_lock_probe.get("command") != "ps -eo pid=,args=":
                errors.append("cross-surface refresh live_build_lock_probe command drifted")
            if not isinstance(live_build_lock_probe.get("status"), str) or not str(live_build_lock_probe.get("status")).strip():
                errors.append("cross-surface refresh live_build_lock_probe status missing")
            if not isinstance(live_build_lock_probe.get("process_count"), int):
                errors.append("cross-surface refresh live_build_lock_probe process_count missing")
            if live_build_lock_probe.get("status") == "present":
                if not isinstance(live_build_lock_probe.get("entries"), list) or not live_build_lock_probe.get("entries"):
                    errors.append("cross-surface refresh live_build_lock_probe present state must list entries")

    if surface_source_fingerprint.get("kind") != "current_checkout_sha256":
        fingerprint_errors.append("cross-surface refresh receipt source fingerprint kind drifted")
    fingerprint_files = surface_source_fingerprint.get("files")
    if not isinstance(fingerprint_files, list) or not fingerprint_files:
        fingerprint_errors.append("cross-surface refresh receipt missing source fingerprint files")
        fingerprint_files = []
    expected_file_count = surface_source_fingerprint.get("file_count")
    if not isinstance(expected_file_count, int) or expected_file_count != len(fingerprint_files):
        fingerprint_errors.append("cross-surface refresh receipt source fingerprint file count drifted")
    for entry in fingerprint_files:
        if not isinstance(entry, dict):
            fingerprint_errors.append("cross-surface refresh receipt source fingerprint entry must be an object")
            continue
        relative_path = entry.get("path")
        recorded_sha = entry.get("sha256")
        if not isinstance(relative_path, str) or not relative_path.strip():
            fingerprint_errors.append("cross-surface refresh receipt source fingerprint entry missing path")
            continue
        fingerprint_paths.append(relative_path)
        path = ROOT / relative_path
        if not path.is_file():
            fingerprint_errors.append(f"cross-surface refresh source fingerprint file missing from checkout: {relative_path}")
            continue
        if not isinstance(recorded_sha, str) or len(recorded_sha) != 64:
            fingerprint_errors.append(f"cross-surface refresh source fingerprint sha256 invalid for {relative_path}")
            continue
        current_sha = sha256_file(path)
        if current_sha != recorded_sha:
            fingerprint_errors.append(f"cross-surface refresh source fingerprint drifted: {relative_path}")
    errors.extend(fingerprint_errors)

    summary = {
        "status": receipt_payload.get("status"),
        "required": False,
        "contract_name": receipt_payload.get("contract_name"),
        "generated_at_utc": receipt_payload.get("generated_at_utc"),
        "script_path": relative_script,
        "receipt_path": relative_receipt,
        "base_url": receipt_payload.get("base_url"),
        "fleet_status": fleet_readiness.get("status"),
        "fleet_warning_keys": fleet_readiness.get("warning_keys") if isinstance(fleet_readiness.get("warning_keys"), list) else [],
        "fleet_missing_keys": fleet_readiness.get("missing_keys") if isinstance(fleet_readiness.get("missing_keys"), list) else [],
        "mobile_play_shell": {
            "status": mobile_play_shell.get("status"),
            "summary": mobile_play_shell.get("summary"),
            "mobile_local_release_status": mobile_play_shell.get("mobile_local_release_status"),
        },
        "public_edge": {
            "skipped": public_edge.get("skipped"),
            "status": public_edge.get("status"),
            "live_gate_status": public_edge.get("live_gate_status"),
            "strict_status": public_edge.get("strict_status"),
            "gate_exit_code": public_edge.get("gate_exit_code"),
            "downloads_status": public_edge.get("downloads_status"),
            "frontdoor_navigation_status": public_edge.get("frontdoor_navigation_status"),
            "frontdoor_route": public_edge.get("frontdoor_route"),
            "handoff_launch_route": public_edge.get("handoff_launch_route"),
            "role_alias_route_status": public_edge.get("role_alias_route_status"),
            "pwa_static_status": public_edge.get("pwa_static_status"),
            "mobile_ledger_status": public_edge.get("mobile_ledger_status"),
            "ready_mobile_handoff_status": public_edge.get("ready_mobile_handoff_status"),
            "participate_iframe_shell_status": public_edge.get("participate_iframe_shell_status"),
            "player_manifest_path": public_edge.get("player_manifest_path"),
            "gm_manifest_path": public_edge.get("gm_manifest_path"),
            "player_handoff_strips_device": public_edge.get("player_handoff_strips_device"),
            "gm_handoff_strips_device": public_edge.get("gm_handoff_strips_device"),
            "rybbit_player": public_edge.get("rybbit_player"),
            "rybbit_gm": public_edge.get("rybbit_gm"),
            "strict_preflight_status": public_edge.get("strict_preflight_status"),
            "strict_postdeploy_status": public_edge.get("strict_postdeploy_status"),
            "strict_postdeploy_stale": public_edge.get("strict_postdeploy_stale"),
            "strict_postdeploy_skip_preflight": public_edge.get("strict_postdeploy_skip_preflight"),
            "strict_postdeploy_skip_release_version_match": public_edge.get("strict_postdeploy_skip_release_version_match"),
            "strict_postdeploy_strict_preflight": public_edge.get("strict_postdeploy_strict_preflight"),
            "strict_postdeploy_strict_invocation": public_edge.get("strict_postdeploy_strict_invocation"),
            "strict_postdeploy_strict_no_allowance_invocation": public_edge.get("strict_postdeploy_strict_no_allowance_invocation"),
            "live_build_lock_probe": live_build_lock_probe,
            "failures": public_edge.get("failures") if isinstance(public_edge.get("failures"), list) else [],
        },
        "surface_source_fingerprint": {
            "kind": surface_source_fingerprint.get("kind"),
            "file_count": surface_source_fingerprint.get("file_count"),
            "paths": fingerprint_paths,
            "matches_current_checkout": len(fingerprint_errors) == 0,
        },
        "checks": checks,
    }
    return summary, errors


def load_release_boundary() -> tuple[dict[str, object], list[str]]:
    errors: list[str] = []
    relative_script = str(MOBILE_RELEASE_BOUNDARY_SCRIPT.relative_to(ROOT))
    relative_receipt = str(MOBILE_RELEASE_BOUNDARY_RECEIPT.relative_to(ROOT))
    if not MOBILE_RELEASE_BOUNDARY_RECEIPT.is_file():
        errors.append("release boundary receipt is not materialized")
        return (
            {
                "status": "not_materialized",
                "required": True,
                "script_path": relative_script,
                "receipt_path": relative_receipt,
            },
            errors,
        )

    receipt_payload = load_json_object(MOBILE_RELEASE_BOUNDARY_RECEIPT, "mobile release boundary receipt", errors)
    ownership_checks = receipt_payload.get("ownership_checks") if isinstance(receipt_payload.get("ownership_checks"), dict) else {}
    owned_boundary = receipt_payload.get("owned_boundary") if isinstance(receipt_payload.get("owned_boundary"), dict) else {}
    release_receipts = owned_boundary.get("release_receipts") if isinstance(owned_boundary.get("release_receipts"), list) else []
    worktree = receipt_payload.get("worktree") if isinstance(receipt_payload.get("worktree"), dict) else {}
    play_worktree = worktree.get("play") if isinstance(worktree.get("play"), dict) else {}
    run_services_worktree = worktree.get("run_services") if isinstance(worktree.get("run_services"), dict) else {}
    owned_disposable_artifacts = (
        receipt_payload.get("owned_disposable_local_artifacts")
        if isinstance(receipt_payload.get("owned_disposable_local_artifacts"), list)
        else []
    )
    shared_external_temp_artifacts = (
        receipt_payload.get("shared_external_temp_artifacts")
        if isinstance(receipt_payload.get("shared_external_temp_artifacts"), list)
        else []
    )
    disposable_artifacts = (
        receipt_payload.get("disposable_local_artifacts")
        if isinstance(receipt_payload.get("disposable_local_artifacts"), list)
        else []
    )
    preflight_snapshot = receipt_payload.get("preflight_snapshot") if isinstance(receipt_payload.get("preflight_snapshot"), dict) else {}
    postdeploy_snapshot = receipt_payload.get("postdeploy_snapshot") if isinstance(receipt_payload.get("postdeploy_snapshot"), dict) else {}
    design_mirror_snapshot = receipt_payload.get("design_mirror_snapshot") if isinstance(receipt_payload.get("design_mirror_snapshot"), dict) else {}
    live_build_lock_probe = receipt_payload.get("live_build_lock_probe") if isinstance(receipt_payload.get("live_build_lock_probe"), dict) else {}
    canonical_release_blockers = receipt_payload.get("canonical_release_blockers") if isinstance(receipt_payload.get("canonical_release_blockers"), dict) else {}
    external_follow_through = receipt_payload.get("external_follow_through") if isinstance(receipt_payload.get("external_follow_through"), dict) else {}

    if receipt_payload.get("contract_name") != "chummer6-mobile.release_boundary.v1":
        errors.append("release boundary receipt contract drifted")
    if receipt_payload.get("status") != "pass":
        errors.append(f"release boundary receipt status is not pass: {receipt_payload.get('status')!r}")
    if not isinstance(receipt_payload.get("generated_at_utc"), str) or not str(receipt_payload.get("generated_at_utc")).strip():
        errors.append("release boundary receipt missing generated_at_utc")

    expected_checks = {
        "owned_play_source_files_present",
        "owned_play_test_files_present",
        "owned_run_services_source_files_present",
        "owned_run_services_test_files_present",
        "owned_release_receipts_present",
        "release_receipts_machine_local_noise_free",
    }
    actual_check_keys = {str(key) for key in ownership_checks}
    if actual_check_keys != expected_checks:
        errors.append(f"release boundary ownership_checks drifted: {sorted(actual_check_keys ^ expected_checks)}")
    for key in sorted(expected_checks):
        if ownership_checks.get(key) is not True:
            errors.append(f"release boundary ownership check failed: {key}")

    expected_release_receipt_paths = {
        ".codex-studio/published/MOBILE_RELEASE_BOUNDARY.generated.json",
        ".codex-studio/published/MOBILE_CROSS_SURFACE_READINESS.generated.json",
        ".codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json",
        ".codex-studio/published/MOBILE_STRICT_PUBLIC_EDGE_FOLLOW_THROUGH.generated.json",
        ".codex-studio/published/MOBILE_PWA_ANALYTICS_SMOKE.generated.json",
        ".codex-studio/published/MOBILE_PWA_PERFORMANCE_BUDGET.generated.json",
        ".codex-studio/published/MOBILE_PWA_RUNTIME_SMOKE.generated.json",
        ".codex-studio/published/MOBILE_PWA_VIEWPORT_SMOKE.generated.json",
    }
    actual_release_receipt_paths = {
        str(row.get("path"))
        for row in release_receipts
        if isinstance(row, dict) and isinstance(row.get("path"), str)
    }
    if actual_release_receipt_paths != expected_release_receipt_paths:
        errors.append(
            f"release boundary receipt path set drifted: {sorted(actual_release_receipt_paths ^ expected_release_receipt_paths)!r}"
        )
    if len(release_receipts) != len(expected_release_receipt_paths):
        errors.append(f"release boundary receipt count drifted: {len(release_receipts)!r}")
    for row in release_receipts:
        if not isinstance(row, dict):
            errors.append("release boundary release_receipts contains a non-object entry")
            continue
        if row.get("exists") is not True:
            errors.append(f"release boundary release receipt missing: {row.get('path')!r}")
        if row.get("pending_materialization") is True:
            errors.append(f"release boundary release receipt still pending materialization: {row.get('path')!r}")

    for label, container, key in [
        ("play worktree", play_worktree, "owned_entry_count"),
        ("play worktree", play_worktree, "foreign_entry_count"),
        ("play worktree", play_worktree, "disposable_entry_count"),
        ("play worktree", play_worktree, "external_blocker_entry_count"),
        ("play worktree", play_worktree, "ambient_entry_count"),
        ("run-services worktree", run_services_worktree, "owned_entry_count"),
        ("run-services worktree", run_services_worktree, "foreign_entry_count"),
        ("run-services worktree", run_services_worktree, "ambient_entry_count"),
    ]:
        if not isinstance(container.get(key), int):
            errors.append(f"release boundary {label} {key} missing")

    owned_disposable_count = receipt_payload.get("owned_disposable_local_artifact_count")
    if not isinstance(owned_disposable_count, int):
        errors.append("release boundary owned_disposable_local_artifact_count missing")
        owned_disposable_count = len(owned_disposable_artifacts)
    elif owned_disposable_count != len(owned_disposable_artifacts):
        errors.append("release boundary owned_disposable_local_artifact_count drifted from artifact list")

    shared_external_temp_count = receipt_payload.get("shared_external_temp_artifact_count")
    if not isinstance(shared_external_temp_count, int):
        errors.append("release boundary shared_external_temp_artifact_count missing")
        shared_external_temp_count = len(shared_external_temp_artifacts)
    elif shared_external_temp_count != len(shared_external_temp_artifacts):
        errors.append("release boundary shared_external_temp_artifact_count drifted from artifact list")

    disposable_artifact_count = receipt_payload.get("disposable_local_artifact_count")
    if not isinstance(disposable_artifact_count, int):
        errors.append("release boundary disposable_local_artifact_count missing")
        disposable_artifact_count = len(disposable_artifacts)
    elif disposable_artifact_count != len(disposable_artifacts):
        errors.append("release boundary disposable_local_artifact_count drifted from artifact list")

    if disposable_artifact_count != owned_disposable_count + shared_external_temp_count:
        errors.append("release boundary disposable artifact partition count drifted")

    if not design_mirror_snapshot:
        errors.append("release boundary design_mirror_snapshot missing")
    else:
        if not isinstance(design_mirror_snapshot.get("status"), str) or not str(design_mirror_snapshot.get("status")).strip():
            errors.append("release boundary design_mirror_snapshot status missing")
        if design_mirror_snapshot.get("script_path") != "scripts/ai/verify_design_mirror.py":
            errors.append("release boundary design_mirror_snapshot script_path drifted")
        if design_mirror_snapshot.get("command") != "python3 scripts/ai/verify_design_mirror.py":
            errors.append("release boundary design_mirror_snapshot command drifted")
        if design_mirror_snapshot.get("status") == "fail":
            if not isinstance(design_mirror_snapshot.get("blocking_findings"), list) or not design_mirror_snapshot.get("blocking_findings"):
                errors.append("release boundary failed design_mirror_snapshot must list blocking findings")
            if not isinstance(design_mirror_snapshot.get("repair_commands"), list) or not design_mirror_snapshot.get("repair_commands"):
                errors.append("release boundary failed design_mirror_snapshot must list repair commands")

    if not live_build_lock_probe:
        errors.append("release boundary live_build_lock_probe missing")
    else:
        if live_build_lock_probe.get("command") != "ps -eo pid=,args=":
            errors.append("release boundary live_build_lock_probe command drifted")
        if not isinstance(live_build_lock_probe.get("status"), str) or not str(live_build_lock_probe.get("status")).strip():
            errors.append("release boundary live_build_lock_probe status missing")
        if not isinstance(live_build_lock_probe.get("process_count"), int):
            errors.append("release boundary live_build_lock_probe process_count missing")
        if live_build_lock_probe.get("status") == "present":
            if not isinstance(live_build_lock_probe.get("entries"), list) or not live_build_lock_probe.get("entries"):
                errors.append("release boundary live_build_lock_probe present state must list entries")

    if not canonical_release_blockers:
        errors.append("release boundary canonical_release_blockers missing")
    else:
        if canonical_release_blockers.get("status") != "present":
            errors.append(f"release boundary canonical_release_blockers status drifted: {canonical_release_blockers.get('status')!r}")
        if not isinstance(canonical_release_blockers.get("path"), str) or not str(canonical_release_blockers.get("path")).strip():
            errors.append("release boundary canonical_release_blockers path missing")
        if not isinstance(canonical_release_blockers.get("generated_at"), str) or not str(canonical_release_blockers.get("generated_at")).strip():
            errors.append("release boundary canonical_release_blockers generated_at missing")
        root_blocker_ids = canonical_release_blockers.get("root_blocker_ids")
        if not isinstance(root_blocker_ids, list):
            errors.append("release boundary canonical_release_blockers root_blocker_ids missing")
        root_blockers = canonical_release_blockers.get("root_blockers")
        if not isinstance(root_blockers, list):
            errors.append("release boundary canonical_release_blockers root_blockers missing")
            root_blockers = []
        if not isinstance(canonical_release_blockers.get("root_blocker_count"), int):
            errors.append("release boundary canonical_release_blockers root_blocker_count missing")
        elif isinstance(root_blocker_ids, list) and canonical_release_blockers.get("root_blocker_count") != len(root_blocker_ids):
            errors.append("release boundary canonical_release_blockers root_blocker_count drifted")
        for row in root_blockers:
            if not isinstance(row, dict):
                errors.append("release boundary canonical_release_blockers contains a non-object row")
                continue
            if not isinstance(row.get("blocker_id") or row.get("id"), str) or not str(row.get("blocker_id") or row.get("id")).strip():
                errors.append("release boundary canonical_release_blockers row missing blocker id")
            if not isinstance(row.get("failing_gate"), str) or not str(row.get("failing_gate")).strip():
                errors.append("release boundary canonical_release_blockers row missing failing_gate")

    if not external_follow_through:
        errors.append("release boundary external_follow_through missing")
    else:
        design_follow_through = external_follow_through.get("design_mirror") if isinstance(external_follow_through.get("design_mirror"), dict) else {}
        strict_follow_through = external_follow_through.get("strict_public_edge") if isinstance(external_follow_through.get("strict_public_edge"), dict) else {}
        if not design_follow_through:
            errors.append("release boundary external_follow_through design_mirror missing")
        else:
            if not isinstance(design_follow_through.get("status"), str) or not str(design_follow_through.get("status")).strip():
                errors.append("release boundary external_follow_through design_mirror status missing")
            if not isinstance(design_follow_through.get("repair_commands"), list):
                errors.append("release boundary external_follow_through design_mirror repair_commands missing")
        if not strict_follow_through:
            errors.append("release boundary external_follow_through strict_public_edge missing")
        else:
            if not isinstance(strict_follow_through.get("status"), str) or not str(strict_follow_through.get("status")).strip():
                errors.append("release boundary external_follow_through strict_public_edge status missing")
            if not isinstance(strict_follow_through.get("follow_through_command"), str) or not str(strict_follow_through.get("follow_through_command")).strip():
                errors.append("release boundary external_follow_through strict_public_edge follow_through_command missing")
            if not isinstance(strict_follow_through.get("follow_through_receipt_path"), str) or not str(strict_follow_through.get("follow_through_receipt_path")).strip():
                errors.append("release boundary external_follow_through strict_public_edge follow_through_receipt_path missing")
            if not isinstance(strict_follow_through.get("rerun_commands"), list) or len(strict_follow_through.get("rerun_commands")) < 2:
                errors.append("release boundary external_follow_through strict_public_edge rerun_commands missing")

    return (
        {
            "status": receipt_payload.get("status"),
            "required": True,
            "script_path": relative_script,
            "receipt_path": relative_receipt,
            "contract_name": receipt_payload.get("contract_name"),
            "generated_at_utc": receipt_payload.get("generated_at_utc"),
            "ownership_checks": ownership_checks,
            "play_owned_entry_count": play_worktree.get("owned_entry_count"),
            "play_foreign_entry_count": play_worktree.get("foreign_entry_count"),
            "play_disposable_entry_count": play_worktree.get("disposable_entry_count"),
            "play_external_blocker_entry_count": play_worktree.get("external_blocker_entry_count"),
            "play_ambient_entry_count": play_worktree.get("ambient_entry_count"),
            "run_services_owned_entry_count": run_services_worktree.get("owned_entry_count"),
            "run_services_foreign_entry_count": run_services_worktree.get("foreign_entry_count"),
            "run_services_ambient_entry_count": run_services_worktree.get("ambient_entry_count"),
            "release_receipt_count": len(release_receipts),
            "owned_disposable_local_artifact_count": owned_disposable_count,
            "shared_external_temp_artifact_count": shared_external_temp_count,
            "disposable_local_artifact_count": disposable_artifact_count,
            "preflight_snapshot": preflight_snapshot,
            "postdeploy_snapshot": postdeploy_snapshot,
            "design_mirror_snapshot": design_mirror_snapshot,
            "live_build_lock_probe": live_build_lock_probe,
            "canonical_release_blockers": canonical_release_blockers,
            "external_follow_through": external_follow_through,
        },
        errors,
    )


def main() -> int:
    if not REGRESSION_SOURCE.is_file():
        print(f"missing regression source: {REGRESSION_SOURCE}", file=sys.stderr)
        return 1
    if not WEB_SOURCE.is_file():
        print(f"missing web source: {WEB_SOURCE}", file=sys.stderr)
        return 1
    if not PLAY_WEB_APPLICATION.is_file():
        print(f"missing play web application: {PLAY_WEB_APPLICATION}", file=sys.stderr)
        return 1
    if not PLAY_ROUTE_HANDLERS.is_file():
        print(f"missing play route handlers: {PLAY_ROUTE_HANDLERS}", file=sys.stderr)
        return 1
    if not TURN_COMPANION_SERVICE.is_file():
        print(f"missing turn companion service: {TURN_COMPANION_SERVICE}", file=sys.stderr)
        return 1
    if not TURN_COMPANION_PROJECTOR.is_file():
        print(f"missing turn companion projector: {TURN_COMPANION_PROJECTOR}", file=sys.stderr)
        return 1
    if not TURN_COMPANION_IMPORTS.is_file():
        print(f"missing turn companion imports: {TURN_COMPANION_IMPORTS}", file=sys.stderr)
        return 1
    if not TURN_COMPANION_PAGE.is_file():
        print(f"missing turn companion page: {TURN_COMPANION_PAGE}", file=sys.stderr)
        return 1
    if not TURN_COMPANION_LIVE_PAGE.is_file():
        print(f"missing live turn companion page: {TURN_COMPANION_LIVE_PAGE}", file=sys.stderr)
        return 1
    if not PLAY_SESSION_GRANT.is_file():
        print(f"missing play session grant policy: {PLAY_SESSION_GRANT}", file=sys.stderr)
        return 1
    if not TURN_COMPANION_RUNTIME.is_file():
        print(f"missing turn companion runtime: {TURN_COMPANION_RUNTIME}", file=sys.stderr)
        return 1
    if not MOBILE_INSTALL_RUNTIME.is_file():
        print(f"missing mobile install runtime: {MOBILE_INSTALL_RUNTIME}", file=sys.stderr)
        return 1
    if not MOBILE_CSS.is_file():
        print(f"missing mobile stylesheet: {MOBILE_CSS}", file=sys.stderr)
        return 1
    if not APP_SHELL.is_file():
        print(f"missing app shell: {APP_SHELL}", file=sys.stderr)
        return 1
    if not PLAY_WEB_DOCKERFILE.is_file():
        print(f"missing play web dockerfile: {PLAY_WEB_DOCKERFILE}", file=sys.stderr)
        return 1
    if not MIGRATION_MAP.is_file():
        print(f"missing migration map: {MIGRATION_MAP}", file=sys.stderr)
        return 1
    if not PLAY_SIGNOFF.is_file():
        print(f"missing play signoff: {PLAY_SIGNOFF}", file=sys.stderr)
        return 1
    if not VERIFY_SCRIPT.is_file():
        print(f"missing verify script: {VERIFY_SCRIPT}", file=sys.stderr)
        return 1
    if not PACKAGE_PLANE_HELPER.is_file():
        print(f"missing package-plane helper: {PACKAGE_PLANE_HELPER}", file=sys.stderr)
        return 1
    if not MOBILE_RELEASE_PROOF_VERIFIER.is_file():
        print(f"missing mobile release proof verifier: {MOBILE_RELEASE_PROOF_VERIFIER}", file=sys.stderr)
        return 1
    if not RUNTIME_SMOKE.is_file():
        print(f"missing runtime smoke script: {RUNTIME_SMOKE}", file=sys.stderr)
        return 1
    if not VIEWPORT_SMOKE.is_file():
        print(f"missing viewport smoke script: {VIEWPORT_SMOKE}", file=sys.stderr)
        return 1
    if not ANALYTICS_SMOKE.is_file():
        print(f"missing analytics smoke script: {ANALYTICS_SMOKE}", file=sys.stderr)
        return 1
    if not PERFORMANCE_BUDGET_VERIFIER.is_file():
        print(f"missing performance budget verifier: {PERFORMANCE_BUDGET_VERIFIER}", file=sys.stderr)
        return 1
    if not M112_PROOF_DOC.is_file():
        print(f"missing M112 proof doc: {M112_PROOF_DOC}", file=sys.stderr)
        return 1
    if not M112_VERIFIER.is_file():
        print(f"missing M112 verifier: {M112_VERIFIER}", file=sys.stderr)
        return 1
    if not M119_PROOF_DOC.is_file():
        print(f"missing M119 proof doc: {M119_PROOF_DOC}", file=sys.stderr)
        return 1
    if not M119_VERIFIER.is_file():
        print(f"missing M119 verifier: {M119_VERIFIER}", file=sys.stderr)
        return 1
    if not M117_PROOF_DOC.is_file():
        print(f"missing M117 proof doc: {M117_PROOF_DOC}", file=sys.stderr)
        return 1
    if not M117_VERIFIER.is_file():
        print(f"missing M117 verifier: {M117_VERIFIER}", file=sys.stderr)
        return 1
    if not M121_PROOF_DOC.is_file():
        print(f"missing M121 proof doc: {M121_PROOF_DOC}", file=sys.stderr)
        return 1
    if not M121_VERIFIER.is_file():
        print(f"missing M121 verifier: {M121_VERIFIER}", file=sys.stderr)
        return 1
    if not M122_PROOF_DOC.is_file():
        print(f"missing M122 proof doc: {M122_PROOF_DOC}", file=sys.stderr)
        return 1
    if not M122_VERIFIER.is_file():
        print(f"missing M122 verifier: {M122_VERIFIER}", file=sys.stderr)
        return 1
    if not M145_PROOF_DOC.is_file():
        print(f"missing M145 proof doc: {M145_PROOF_DOC}", file=sys.stderr)
        return 1
    if not M145_VERIFIER.is_file():
        print(f"missing M145 verifier: {M145_VERIFIER}", file=sys.stderr)
        return 1
    if not MOBILE_CROSS_SURFACE_REFRESH_SCRIPT.is_file():
        print(f"missing mobile cross-surface refresh script: {MOBILE_CROSS_SURFACE_REFRESH_SCRIPT}", file=sys.stderr)
        return 1
    if not MOBILE_RELEASE_BOUNDARY_SCRIPT.is_file():
        print(f"missing mobile release boundary script: {MOBILE_RELEASE_BOUNDARY_SCRIPT}", file=sys.stderr)
        return 1
    if not GENERIC_MANIFEST.is_file():
        print(f"missing generic manifest: {GENERIC_MANIFEST}", file=sys.stderr)
        return 1
    if not PLAYER_MANIFEST.is_file():
        print(f"missing player manifest: {PLAYER_MANIFEST}", file=sys.stderr)
        return 1
    if not GM_MANIFEST.is_file():
        print(f"missing GM manifest: {GM_MANIFEST}", file=sys.stderr)
        return 1
    if not OBSERVER_MANIFEST.is_file():
        print(f"missing observer manifest: {OBSERVER_MANIFEST}", file=sys.stderr)
        return 1

    regression_text = REGRESSION_SOURCE.read_text(encoding="utf-8")
    play_web_application_text = PLAY_WEB_APPLICATION.read_text(encoding="utf-8")
    play_route_handlers_text = PLAY_ROUTE_HANDLERS.read_text(encoding="utf-8")
    turn_companion_service_text = TURN_COMPANION_SERVICE.read_text(encoding="utf-8")
    turn_companion_projector_text = TURN_COMPANION_PROJECTOR.read_text(encoding="utf-8")
    turn_companion_page_text = TURN_COMPANION_PAGE.read_text(encoding="utf-8")
    turn_companion_live_page_text = TURN_COMPANION_LIVE_PAGE.read_text(encoding="utf-8")
    play_session_grant_text = PLAY_SESSION_GRANT.read_text(encoding="utf-8")
    turn_companion_runtime_text = TURN_COMPANION_RUNTIME.read_text(encoding="utf-8")
    mobile_install_runtime_text = MOBILE_INSTALL_RUNTIME.read_text(encoding="utf-8")
    mobile_css_text = MOBILE_CSS.read_text(encoding="utf-8")
    web_text = WEB_SOURCE.read_text(encoding="utf-8")
    app_shell_text = APP_SHELL.read_text(encoding="utf-8")
    service_worker_text = SERVICE_WORKER.read_text(encoding="utf-8")
    generic_manifest_text = GENERIC_MANIFEST.read_text(encoding="utf-8")
    player_manifest_text = PLAYER_MANIFEST.read_text(encoding="utf-8")
    gm_manifest_text = GM_MANIFEST.read_text(encoding="utf-8")
    observer_manifest_text = OBSERVER_MANIFEST.read_text(encoding="utf-8")
    migration_map_text = MIGRATION_MAP.read_text(encoding="utf-8")
    play_signoff_text = PLAY_SIGNOFF.read_text(encoding="utf-8")
    verify_script_text = VERIFY_SCRIPT.read_text(encoding="utf-8")
    mobile_release_proof_verifier_text = MOBILE_RELEASE_PROOF_VERIFIER.read_text(encoding="utf-8")
    runtime_smoke_text = RUNTIME_SMOKE.read_text(encoding="utf-8")
    viewport_smoke_text = VIEWPORT_SMOKE.read_text(encoding="utf-8")
    analytics_smoke_text = ANALYTICS_SMOKE.read_text(encoding="utf-8")
    performance_budget_verifier_text = PERFORMANCE_BUDGET_VERIFIER.read_text(encoding="utf-8")
    m112_proof_doc_text = M112_PROOF_DOC.read_text(encoding="utf-8")
    m112_verifier_text = M112_VERIFIER.read_text(encoding="utf-8")
    m119_proof_doc_text = M119_PROOF_DOC.read_text(encoding="utf-8")
    m119_verifier_text = M119_VERIFIER.read_text(encoding="utf-8")
    m117_proof_doc_text = M117_PROOF_DOC.read_text(encoding="utf-8")
    m117_verifier_text = M117_VERIFIER.read_text(encoding="utf-8")
    m121_proof_doc_text = M121_PROOF_DOC.read_text(encoding="utf-8")
    m121_verifier_text = M121_VERIFIER.read_text(encoding="utf-8")
    m122_proof_doc_text = M122_PROOF_DOC.read_text(encoding="utf-8")
    m122_verifier_text = M122_VERIFIER.read_text(encoding="utf-8")
    m145_proof_doc_text = M145_PROOF_DOC.read_text(encoding="utf-8")
    m145_verifier_text = M145_VERIFIER.read_text(encoding="utf-8")
    mobile_release_boundary_text = MOBILE_RELEASE_BOUNDARY_SCRIPT.read_text(encoding="utf-8")
    combined_text = "\n".join(
        [
            regression_text,
            play_web_application_text,
            play_route_handlers_text,
            turn_companion_service_text,
            turn_companion_projector_text,
            turn_companion_page_text,
            turn_companion_live_page_text,
            play_session_grant_text,
            turn_companion_runtime_text,
            mobile_install_runtime_text,
            mobile_css_text,
            web_text,
            app_shell_text,
            service_worker_text,
            generic_manifest_text,
            player_manifest_text,
            gm_manifest_text,
            observer_manifest_text,
            migration_map_text,
            play_signoff_text,
            verify_script_text,
            mobile_release_proof_verifier_text,
            runtime_smoke_text,
            viewport_smoke_text,
            analytics_smoke_text,
            performance_budget_verifier_text,
            m112_proof_doc_text,
            m112_verifier_text,
            m119_proof_doc_text,
            m119_verifier_text,
            m117_proof_doc_text,
            m117_verifier_text,
            m121_proof_doc_text,
            m121_verifier_text,
            m122_proof_doc_text,
            m122_verifier_text,
            m145_proof_doc_text,
            m145_verifier_text,
            mobile_release_boundary_text,
        ])

    missing: list[str] = []
    journeys_passed: list[str] = []
    for journey_id, markers in REQUIRED_MARKERS.items():
        journey_missing = [marker for marker in markers if marker not in combined_text]
        if journey_missing:
            for marker in journey_missing:
                missing.append(f"{journey_id}: {marker}")
            continue
        journeys_passed.append(journey_id)

    if missing:
        for item in missing:
            print(f"mobile_local_release_proof_missing: {item}", file=sys.stderr)
        return 1

    smoke_receipts, smoke_receipt_errors = load_required_smoke_receipts()
    if smoke_receipt_errors:
        for item in smoke_receipt_errors:
            print(f"mobile_local_release_proof_receipt_invalid: {item}", file=sys.stderr)
        return 1
    role_pwa_contract, role_pwa_errors = build_role_pwa_contract_v2()
    if role_pwa_errors:
        for item in role_pwa_errors:
            print(f"mobile_local_release_proof_role_pwa_invalid: {item}", file=sys.stderr)
        return 1
    cross_surface_readiness, cross_surface_errors = load_cross_surface_readiness()
    if cross_surface_errors:
        for item in cross_surface_errors:
            print(f"mobile_local_release_proof_cross_surface_invalid: {item}", file=sys.stderr)
        return 1
    cross_surface_refresh, cross_surface_refresh_errors = load_cross_surface_refresh()
    if cross_surface_refresh_errors:
        for item in cross_surface_refresh_errors:
            print(f"mobile_local_release_proof_cross_surface_refresh_invalid: {item}", file=sys.stderr)
        return 1
    release_boundary, release_boundary_errors = load_release_boundary()
    if release_boundary_errors:
        for item in release_boundary_errors:
            print(f"mobile_local_release_proof_release_boundary_invalid: {item}", file=sys.stderr)
        return 1

    generated_at = iso_now()
    source_files = [
        str(REGRESSION_SOURCE.relative_to(ROOT)),
        str(PLAY_WEB_APPLICATION.relative_to(ROOT)),
        str(PLAY_ROUTE_HANDLERS.relative_to(ROOT)),
        str(TURN_COMPANION_SERVICE.relative_to(ROOT)),
        str(TURN_COMPANION_PROJECTOR.relative_to(ROOT)),
        str(TURN_COMPANION_IMPORTS.relative_to(ROOT)),
        str(TURN_COMPANION_PAGE.relative_to(ROOT)),
        str(TURN_COMPANION_LIVE_PAGE.relative_to(ROOT)),
        str(PLAY_SESSION_GRANT.relative_to(ROOT)),
        str(TURN_COMPANION_RUNTIME.relative_to(ROOT)),
        str(MOBILE_INSTALL_RUNTIME.relative_to(ROOT)),
        str(MOBILE_CSS.relative_to(ROOT)),
        str(WEB_SOURCE.relative_to(ROOT)),
        str(APP_SHELL.relative_to(ROOT)),
        str(PLAY_WEB_DOCKERFILE.relative_to(ROOT)),
        str(SERVICE_WORKER.relative_to(ROOT)),
        str(GENERIC_MANIFEST.relative_to(ROOT)),
        str(PLAYER_MANIFEST.relative_to(ROOT)),
        str(GM_MANIFEST.relative_to(ROOT)),
        str(OBSERVER_MANIFEST.relative_to(ROOT)),
        str(MIGRATION_MAP.relative_to(ROOT)),
        str(PLAY_SIGNOFF.relative_to(ROOT)),
        str(VERIFY_SCRIPT.relative_to(ROOT)),
        str(PACKAGE_PLANE_HELPER.relative_to(ROOT)),
        str(MOBILE_RELEASE_PROOF_VERIFIER.relative_to(ROOT)),
        str(RUNTIME_SMOKE.relative_to(ROOT)),
        str(VIEWPORT_SMOKE.relative_to(ROOT)),
        str(ANALYTICS_SMOKE.relative_to(ROOT)),
        str(PERFORMANCE_BUDGET_VERIFIER.relative_to(ROOT)),
        str(M112_PROOF_DOC.relative_to(ROOT)),
        str(M112_VERIFIER.relative_to(ROOT)),
        str(M119_PROOF_DOC.relative_to(ROOT)),
        str(M119_VERIFIER.relative_to(ROOT)),
        str(M117_PROOF_DOC.relative_to(ROOT)),
        str(M117_VERIFIER.relative_to(ROOT)),
        str(M121_PROOF_DOC.relative_to(ROOT)),
        str(M121_VERIFIER.relative_to(ROOT)),
        str(M122_PROOF_DOC.relative_to(ROOT)),
        str(M122_VERIFIER.relative_to(ROOT)),
        str(M145_PROOF_DOC.relative_to(ROOT)),
        str(M145_VERIFIER.relative_to(ROOT)),
        str(MOBILE_CROSS_SURFACE_REFRESH_SCRIPT.relative_to(ROOT)),
        str(MOBILE_RELEASE_BOUNDARY_SCRIPT.relative_to(ROOT)),
    ]
    source_file_digests = [
        {
            "path": source_file,
            "sha256": sha256_file(ROOT / source_file),
        }
        for source_file in source_files
    ]
    payload = {
        "contract_name": "chummer6-mobile.local_release_proof",
        "status": "passed",
        "proof_kind": "source_backed_local_regression_contract",
        "source_files": source_files,
        "source_file_digests": source_file_digests,
        "journeys_passed": journeys_passed,
        "required_markers": REQUIRED_MARKERS,
        "package_receipts": PACKAGE_RECEIPTS,
        "verification_commands": VERIFICATION_COMMANDS,
        "smoke_receipts": smoke_receipts,
        "role_pwa_contract": role_pwa_contract,
        "cross_surface_readiness": cross_surface_readiness,
        "cross_surface_refresh": cross_surface_refresh,
        "release_boundary": release_boundary,
        "source_file_count": len(source_files),
        "source_file_digest_count": len(source_file_digests),
        # Fleet freshness gates key off this timestamp, so reruns must renew it
        # even when the proof payload is otherwise unchanged.
        "generated_at": generated_at,
        "generated_at_utc": generated_at,
    }

    OUT.parent.mkdir(parents=True, exist_ok=True)
    serialized = json.dumps(payload, indent=2) + "\n"
    OUT.write_text(serialized, encoding="utf-8")
    print(f"wrote mobile local release proof: {OUT}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
