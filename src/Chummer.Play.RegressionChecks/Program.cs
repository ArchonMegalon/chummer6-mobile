using Chummer.Campaign.Contracts;
using Chummer.Play.Core.Application;
using Chummer.Play.Core.Offline;
using Chummer.Play.Core.PlayApi;
using Chummer.Play.Core.Roaming;
using Chummer.Play.Core.Sync;
using Chummer.Play.Gm.TacticalShell;
using Chummer.Play.Player.PlayerShell;
using Chummer.Play.Web;
using Chummer.Play.Web.BrowserState;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;

Environment.SetEnvironmentVariable(
    "CHUMMER_PLAY_BROWSER_STATE_DIR",
    Path.Combine(Path.GetTempPath(), "chummer-play-regression-browser-state", Guid.NewGuid().ToString("N")));
Environment.SetEnvironmentVariable("CHUMMER_PLAY_BROWSER_STATE_ISOLATE_PER_APP", "true");

await RunCheckAsync(nameof(VerifyLedgerLineageResetAsync), VerifyLedgerLineageResetAsync);
await RunCheckAsync(nameof(VerifyBootstrapProjectionPreservesReplayStateAsync), VerifyBootstrapProjectionPreservesReplayStateAsync);
await RunCheckAsync(nameof(VerifyMonotonicSequenceOwnershipAsync), VerifyMonotonicSequenceOwnershipAsync);
await RunCheckAsync(nameof(VerifyConcurrentEnqueueSequenceOwnershipAsync), VerifyConcurrentEnqueueSequenceOwnershipAsync);
await RunCheckAsync(nameof(VerifySyncPrefixAcknowledgementAsync), VerifySyncPrefixAcknowledgementAsync);
await RunCheckAsync(nameof(VerifySyncPreservesNewerLedgerSequenceAsync), VerifySyncPreservesNewerLedgerSequenceAsync);
RunCheck(nameof(VerifyCursorValidationRejectsNegativeSequence), VerifyCursorValidationRejectsNegativeSequence);
await RunCheckAsync(nameof(VerifyEventLogRejectsMalformedAppendAsync), VerifyEventLogRejectsMalformedAppendAsync);
await RunCheckAsync(nameof(VerifyEventLogRejectsSequenceRegressionAsync), VerifyEventLogRejectsSequenceRegressionAsync);
await RunCheckAsync(nameof(VerifyEventLogPersistsAcrossServiceInstancesAsync), VerifyEventLogPersistsAcrossServiceInstancesAsync);
await RunCheckAsync(nameof(VerifyOfflineQueueRejectsMalformedPendingEventsAsync), VerifyOfflineQueueRejectsMalformedPendingEventsAsync);
await RunCheckAsync(nameof(VerifyOfflineQueueRejectsNegativeSequenceAsync), VerifyOfflineQueueRejectsNegativeSequenceAsync);
await RunCheckAsync(nameof(VerifyOfflineQueueRejectsMalformedSessionEnvelopeAsync), VerifyOfflineQueueRejectsMalformedSessionEnvelopeAsync);
await RunCheckAsync(nameof(VerifyOfflineCacheRejectsMalformedCheckpointAndRuntimeEntryAsync), VerifyOfflineCacheRejectsMalformedCheckpointAndRuntimeEntryAsync);
await RunCheckAsync(nameof(VerifyOfflineCacheDropsMalformedStoredEntriesAsync), VerifyOfflineCacheDropsMalformedStoredEntriesAsync);
await RunCheckAsync(nameof(VerifyOfflineCacheDropsUnparseableStoredEntriesAsync), VerifyOfflineCacheDropsUnparseableStoredEntriesAsync);
await RunCheckAsync(nameof(VerifyOfflineCacheRuntimeBundleQuotaEvictionAsync), VerifyOfflineCacheRuntimeBundleQuotaEvictionAsync);
await RunCheckAsync(nameof(VerifyOfflineCacheReadsDoNotMutateQuotaEvictionAsync), VerifyOfflineCacheReadsDoNotMutateQuotaEvictionAsync);
await RunCheckAsync(nameof(VerifyOfflineCacheReadDoesNotRewriteStoredMetadataAsync), VerifyOfflineCacheReadDoesNotRewriteStoredMetadataAsync);
await RunCheckAsync(nameof(VerifyOfflineCacheConcurrentReadAndWritePreserveNewestSameSessionMetadataAsync), VerifyOfflineCacheConcurrentReadAndWritePreserveNewestSameSessionMetadataAsync);
await RunCheckAsync(nameof(VerifyOfflineCacheQuotaIgnoresUnparseableRuntimeBundleKeysAsync), VerifyOfflineCacheQuotaIgnoresUnparseableRuntimeBundleKeysAsync);
await RunCheckAsync(nameof(VerifyOfflineCacheConcurrentCrossSessionQuotaWritesStayBoundedAsync), VerifyOfflineCacheConcurrentCrossSessionQuotaWritesStayBoundedAsync);
await RunCheckAsync(nameof(VerifyAppBrowserStatePersistsAcrossRebuildAsync), VerifyAppBrowserStatePersistsAcrossRebuildAsync);
await RunCheckAsync(nameof(VerifyIndexShellAccessibilityContractAsync), VerifyIndexShellAccessibilityContractAsync);
await RunCheckAsync(nameof(VerifyServiceWorkerKeepsPrivatePlayApiNetworkOnlyAsync), VerifyServiceWorkerKeepsPrivatePlayApiNetworkOnlyAsync);
await RunCheckAsync(nameof(VerifyPlayApiBoundaryRequiresTrustedContextAsync), VerifyPlayApiBoundaryRequiresTrustedContextAsync);
await RunCheckAsync(nameof(VerifyIndexShellBindsContextualActionLabelsAsync), VerifyIndexShellBindsContextualActionLabelsAsync);
await RunCheckAsync(nameof(VerifyTurnCompanionProjectionStaysBoundedAndComputesOddsAsync), VerifyTurnCompanionProjectionStaysBoundedAndComputesOddsAsync);
await RunCheckAsync(nameof(VerifyTurnCompanionPlayerProjectionCoversRequestedLiveTrackersAsync), VerifyTurnCompanionPlayerProjectionCoversRequestedLiveTrackersAsync);
await RunCheckAsync(nameof(VerifyTurnCompanionGmProjectionStaysBoundedAndRoleSpecificAsync), VerifyTurnCompanionGmProjectionStaysBoundedAndRoleSpecificAsync);
await RunCheckAsync(nameof(VerifyTurnCompanionDigitalResolveProducesBoundedReceiptAsync), VerifyTurnCompanionDigitalResolveProducesBoundedReceiptAsync);
await RunCheckAsync(nameof(VerifyTurnCompanionManualResolveUpdatesHistoryAndAmmoAsync), VerifyTurnCompanionManualResolveUpdatesHistoryAndAmmoAsync);
await RunCheckAsync(nameof(VerifyTurnCompanionObserverStaysReadOnlyAsync), VerifyTurnCompanionObserverStaysReadOnlyAsync);
await RunCheckAsync(nameof(VerifyTurnCompanionClaimedDeviceStateIsolationAsync), VerifyTurnCompanionClaimedDeviceStateIsolationAsync);
await RunCheckAsync(nameof(VerifyTurnCompanionRunsiteAnchorSelectionStaysDeviceScopedAsync), VerifyTurnCompanionRunsiteAnchorSelectionStaysDeviceScopedAsync);
await RunCheckAsync(nameof(VerifyTurnCompanionReplayQueueRoundTripsAsync), VerifyTurnCompanionReplayQueueRoundTripsAsync);
await RunCheckAsync(nameof(VerifyTurnCompanionRouteRendersBlazorShellAsync), VerifyTurnCompanionRouteRendersBlazorShellAsync);
await RunCheckAsync(nameof(VerifyTurnCompanionClientRuntimeKeepsClaimedDeviceContinuityContractAsync), VerifyTurnCompanionClientRuntimeKeepsClaimedDeviceContinuityContractAsync);
await RunCheckAsync(nameof(VerifyTurnCompanionManifestTargetsDirectMobilePwaAsync), VerifyTurnCompanionManifestTargetsDirectMobilePwaAsync);
await RunCheckAsync(nameof(VerifyTurnCompanionAppShellDeclaresMobileInstallMetadataAsync), VerifyTurnCompanionAppShellDeclaresMobileInstallMetadataAsync);
await RunCheckAsync(nameof(VerifyTurnCompanionRealHostPipelineUsesAntiforgeryAsync), VerifyTurnCompanionRealHostPipelineUsesAntiforgeryAsync);
await RunCheckAsync(nameof(VerifyBootstrapRoleShellEntryPointsAsync), VerifyBootstrapRoleShellEntryPointsAsync);
await RunCheckAsync(nameof(VerifyRoleBoundarySurvivesCapabilityLeakageAsync), VerifyRoleBoundarySurvivesCapabilityLeakageAsync);
await RunCheckAsync(nameof(VerifyQuickActionRejectsCrossRoleAuthorizationAsync), VerifyQuickActionRejectsCrossRoleAuthorizationAsync);
await RunCheckAsync(nameof(VerifyDeniedQuickActionsPreserveStoredReplayStateAsync), VerifyDeniedQuickActionsPreserveStoredReplayStateAsync);
await RunCheckAsync(nameof(VerifyObserverBootstrapAndResumeStayReadMostlyAsync), VerifyObserverBootstrapAndResumeStayReadMostlyAsync);
await RunCheckAsync(nameof(VerifyResumeAndWorkspaceLiteRoutesStayRoleConcreteAsync), VerifyResumeAndWorkspaceLiteRoutesStayRoleConcreteAsync);
await RunCheckAsync(nameof(VerifyNoSessionRestoreHrefUsesRealPlayEntryRouteAsync), VerifyNoSessionRestoreHrefUsesRealPlayEntryRouteAsync);
await RunCheckAsync(nameof(VerifyCachePressureBudgetContractAsync), VerifyCachePressureBudgetContractAsync);
await RunCheckAsync(nameof(VerifyEventLogDropsMalformedStoredLedgerAsync), VerifyEventLogDropsMalformedStoredLedgerAsync);
await RunCheckAsync(nameof(VerifyEventLogDropsUnparseableStoredLedgerKeysAsync), VerifyEventLogDropsUnparseableStoredLedgerKeysAsync);
await RunCheckAsync(nameof(VerifyOfflineQueueRejectsStaleLineageAsync), VerifyOfflineQueueRejectsStaleLineageAsync);
await RunCheckAsync(nameof(VerifyReconnectLineageTransitionContinuityAsync), VerifyReconnectLineageTransitionContinuityAsync);
await RunCheckAsync(nameof(VerifyStoredLineageStaleResponsesAsync), VerifyStoredLineageStaleResponsesAsync);
await RunCheckAsync(nameof(VerifyReconnectRejectsStaleLineageWithoutMutationAsync), VerifyReconnectRejectsStaleLineageWithoutMutationAsync);
await RunCheckAsync(nameof(VerifyReconnectClientThrowsTypedStaleAsync), VerifyReconnectClientThrowsTypedStaleAsync);
await RunCheckAsync(nameof(VerifyContinuityClaimRejectsStaleLineageWithoutMutationAsync), VerifyContinuityClaimRejectsStaleLineageWithoutMutationAsync);
await RunCheckAsync(nameof(VerifyContinuityClaimRejectsUnknownSessionWithoutMutationAsync), VerifyContinuityClaimRejectsUnknownSessionWithoutMutationAsync);
await RunCheckAsync(nameof(VerifyObserveDoesNotSeedStateForEmptySessionAsync), VerifyObserveDoesNotSeedStateForEmptySessionAsync);
await RunCheckAsync(nameof(VerifyObserveDoesNotMutateStoredStateOrReturnStaleRuntimeBundleAsync), VerifyObserveDoesNotMutateStoredStateOrReturnStaleRuntimeBundleAsync);
await RunCheckAsync(nameof(VerifyObserveKeepsRequestedSessionIdWhenStoredCheckpointDriftsAsync), VerifyObserveKeepsRequestedSessionIdWhenStoredCheckpointDriftsAsync);
await RunCheckAsync(nameof(VerifyObserveReturnsLineageSafeContinuityAsync), VerifyObserveReturnsLineageSafeContinuityAsync);
await RunCheckAsync(nameof(VerifyObservePreservesContinuityWhenClaimCursorLeadsLedgerAsync), VerifyObservePreservesContinuityWhenClaimCursorLeadsLedgerAsync);
await RunCheckAsync(nameof(VerifyObserveRouteRoundTripAsync), VerifyObserveRouteRoundTripAsync);
await RunCheckAsync(nameof(VerifyQueueMutationLineageExceptionReturnsStaleAsync), VerifyQueueMutationLineageExceptionReturnsStaleAsync);
await RunCheckAsync(nameof(VerifyBootstrapRejectsStaleLineageWithoutLedgerResetAsync), VerifyBootstrapRejectsStaleLineageWithoutLedgerResetAsync);
await RunCheckAsync(nameof(VerifyProjectionPrefersStoredLedgerWithoutCheckpointAsync), VerifyProjectionPrefersStoredLedgerWithoutCheckpointAsync);
RunCheck(nameof(VerifyStoredStaleStatePrefersLedgerOverOlderCheckpoint), VerifyStoredStaleStatePrefersLedgerOverOlderCheckpoint);
await RunCheckAsync(nameof(VerifyResumePreservesLedgerWhenRuntimeBundleDriftsAsync), VerifyResumePreservesLedgerWhenRuntimeBundleDriftsAsync);
await RunCheckAsync(nameof(VerifyResumeNormalizesCheckpointToLedgerLineageAsync), VerifyResumeNormalizesCheckpointToLedgerLineageAsync);
await RunCheckAsync(nameof(VerifyRuntimeBundleSessionLockReleasesOnCanceledAcquireAsync), VerifyRuntimeBundleSessionLockReleasesOnCanceledAcquireAsync);
RunCheck(nameof(VerifyCheckpointLineageAlignment), VerifyCheckpointLineageAlignment);
RunCheck(nameof(VerifyStoredLineageAlignment), VerifyStoredLineageAlignment);
RunCheck(nameof(VerifyCampaignWorkspaceLiteProjectionPromotesContinuitySummary), VerifyCampaignWorkspaceLiteProjectionPromotesContinuitySummary);
RunCheck(nameof(VerifyDisconnectRecoveryCopyUsesConcreteRouteWithoutCheckpoint), VerifyDisconnectRecoveryCopyUsesConcreteRouteWithoutCheckpoint);
RunCheck(nameof(VerifyEntryRecoveryProjectionCoversNoSessionNoCampaignAndPostFailure), VerifyEntryRecoveryProjectionCoversNoSessionNoCampaignAndPostFailure);
RunCheck(nameof(VerifyCachePressureDecisionNoticeUsesSupportNextActionCopy), VerifyCachePressureDecisionNoticeUsesSupportNextActionCopy);
RunCheck(nameof(VerifyCampaignWorkspaceLiteProjectionPreservesObserverAndGmRoleDepth), VerifyCampaignWorkspaceLiteProjectionPreservesObserverAndGmRoleDepth);
RunCheck(nameof(VerifyRoamingWorkspaceRestorePlanRestoresPackageOwnedCampaignState), VerifyRoamingWorkspaceRestorePlanRestoresPackageOwnedCampaignState);
RunCheck(nameof(VerifyRoamingWorkspaceRestorePlanPreservesConflictAndInstallLocalGuardrails), VerifyRoamingWorkspaceRestorePlanPreservesConflictAndInstallLocalGuardrails);
RunCheck(nameof(VerifyPlayRoamingRestoreServiceProjectsClaimedDeviceRecovery), VerifyPlayRoamingRestoreServiceProjectsClaimedDeviceRecovery);

Console.WriteLine("chummer6-mobile regression checks ok");

static void RunCheck(string name, Action action)
{
    TraceCheckBoundary("START", name);
    action();
    TraceCheckBoundary("OK", name);
}

static async Task RunCheckAsync(string name, Func<Task> action)
{
    TraceCheckBoundary("START", name);
    await action();
    TraceCheckBoundary("OK", name);
}

static void TraceCheckBoundary(string phase, string name)
{
    if (!string.Equals(Environment.GetEnvironmentVariable("CHUMMER_REGRESSION_TRACE"), "1", StringComparison.Ordinal))
    {
        return;
    }

    Console.Error.WriteLine($"{phase} {name}");
}

static void VerifyRoamingWorkspaceRestorePlanRestoresPackageOwnedCampaignState()
{
    IRoamingWorkspaceSyncPlanner planner = new RoamingWorkspaceSyncPlanner();
    WorkspaceRestoreProjection restore = CreateWorkspaceRestoreProjection(conflicts: Array.Empty<string>());

    RoamingWorkspaceRestorePlan plan = planner.CreatePlan(restore, "install-tablet");

    Assert(plan.TargetDeviceId == "install-tablet", "roaming restore must target the requested claimed device");
    Assert(plan.DeviceRole == "play_tablet", "roaming restore must preserve the claimed device role");
    Assert(plan.Dossiers.Count == 1, "roaming restore must carry living dossiers onto the claimed second device");
    Assert(plan.Campaigns.Count == 1, "roaming restore must carry campaign summaries onto the claimed second device");
    Assert(plan.RuleEnvironments.Count == 1, "roaming restore must carry rule environments onto the claimed second device");
    Assert(plan.Artifacts.Count == 1, "roaming restore must carry reconnectable artifact truth onto the claimed second device");
    Assert(plan.Entitlements.Count == 1, "roaming restore must carry entitlements onto the claimed second device");
    Assert(plan.ResumeSummary.Contains("Redmond Patrol", StringComparison.Ordinal), "roaming restore must surface the primary campaign in the resume summary");
    Assert(plan.ResumeSummary.Contains("sr6.preview.v1", StringComparison.Ordinal), "roaming restore must surface the active rule fingerprint in the resume summary");
    Assert(plan.SafeNextAction.Contains("Open Redmond Patrol", StringComparison.Ordinal), "roaming restore must point the claimed device at the next safe campaign action");
    Assert(plan.ResumeFollowThrough.Contains("Resume Redmond Patrol", StringComparison.Ordinal), "roaming restore must surface an explicit claimed-device resume follow-through.");
    Assert(plan.ResumeFollowThroughHref.Contains("/play/session-redmond", StringComparison.Ordinal), "roaming restore must expose a direct claimed-device resume href.");
    Assert(plan.ResumeFollowThroughHref.Contains("deviceId=install-tablet", StringComparison.Ordinal), "roaming restore resume href must preserve the claimed device id.");
    Assert(plan.StarterPrimerFollowThrough.Contains("starter primer", StringComparison.OrdinalIgnoreCase), "roaming restore must surface a starter-primer follow-through on the claimed device.");
    Assert(plan.StarterPrimerFollowThroughHref.Contains("/artifacts/session-redmond/", StringComparison.Ordinal), "roaming restore must expose a direct claimed-device starter-primer href.");
    Assert(plan.StarterPrimerFollowThroughHref.Contains("artifact%3Asession-redmond%3Astarter-primer", StringComparison.Ordinal), "roaming restore starter-primer href must preserve the canonical starter-primer artifact id.");
    Assert(plan.StarterPrimerFollowThroughHref.Contains("deviceId=install-tablet", StringComparison.Ordinal), "roaming restore starter-primer href must preserve the claimed device id.");
    Assert(plan.FirstSessionBriefingFollowThrough.Contains("first-session briefing", StringComparison.OrdinalIgnoreCase), "roaming restore must surface a first-session briefing follow-through on the claimed device.");
    Assert(plan.FirstSessionBriefingFollowThroughHref.Contains("/artifacts/session-redmond/", StringComparison.Ordinal), "roaming restore must expose a direct claimed-device first-session briefing href.");
    Assert(plan.FirstSessionBriefingFollowThroughHref.Contains("artifact%3Asession-redmond%3Afirst-session-briefing", StringComparison.Ordinal), "roaming restore first-session briefing href must preserve the canonical briefing artifact id.");
    Assert(plan.FirstSessionBriefingFollowThroughHref.Contains("view=travel", StringComparison.Ordinal), "roaming restore first-session briefing href must preserve the travel artifact shelf.");
    Assert(plan.SupportFollowThrough.Contains("Redmond Patrol", StringComparison.Ordinal), "roaming restore must surface a support follow-through tied to the target campaign.");
    Assert(plan.SupportFollowThroughHref.Contains("/contact", StringComparison.Ordinal), "roaming restore must expose a direct support follow-through href.");
    Assert(plan.SupportFollowThroughHref.Contains("campaignId=campaign-redmond", StringComparison.Ordinal), "roaming restore support href must preserve the target campaign id.");
    Assert(plan.RuleEnvironmentSummary == "sr6.preview.v1 · approved · campaign", "roaming restore must surface concise rule-environment posture");
    Assert(plan.PrefetchReadinessSummary.Contains("green", StringComparison.OrdinalIgnoreCase), "roaming restore must expose a green prefetch readiness summary when the packet is aligned");
    Assert(plan.PrefetchReadinessSummary.Contains("1 dossier", StringComparison.Ordinal), "roaming restore must keep the prefetch inventory visible inside the readiness summary");
    Assert(plan.LocalCacheBoundarySummary.Contains("install-local", StringComparison.Ordinal), "roaming restore must expose an install-local cache boundary summary");
    Assert(plan.OfflineTruthSummary.Contains("Cached:", StringComparison.Ordinal), "roaming restore must expose explicit cached-state truth for the target device.");
    Assert(plan.OfflineTruthSummary.Contains("Stale:", StringComparison.Ordinal), "roaming restore must expose explicit stale-state truth for the target device.");
    Assert(plan.OfflineTruthSummary.Contains("Offline actions:", StringComparison.Ordinal), "roaming restore must expose explicit offline-action boundaries for the target device.");
    Assert(plan.OfflineTruthLabels.Any(item => item.Contains("Cached lane:", StringComparison.Ordinal)), "roaming restore must expose a cached-state label for the target device.");
    Assert(plan.OfflineTruthLabels.Any(item => item.Contains("Stale lane:", StringComparison.Ordinal)), "roaming restore must expose a stale-state label for the target device.");
    Assert(plan.OfflineTruthLabels.Any(item => item.Contains("Offline action lane:", StringComparison.Ordinal)), "roaming restore must expose an offline-action label for the target device.");
    Assert(plan.TravelCampaignCurrentState.Contains("Current continuity posture:", StringComparison.Ordinal), "roaming restore must expose explicit travel campaign current posture.");
    Assert(plan.TravelCampaignStateSummary.Contains("Cached state:", StringComparison.Ordinal), "roaming restore must expose explicit travel campaign cached-state summary.");
    Assert(plan.TravelCampaignCachedState.Contains("Cached state:", StringComparison.Ordinal), "roaming restore must expose explicit travel campaign cached-state detail.");
    Assert(plan.TravelCampaignStaleState.Contains("Stale state:", StringComparison.Ordinal), "roaming restore must expose explicit travel campaign stale-state detail.");
    Assert(plan.TravelCampaignActionRequired.Contains("Action required:", StringComparison.Ordinal), "roaming restore must expose explicit travel campaign action-required posture.");
    Assert(plan.TravelCampaignActionRequired.Contains("Travel lane: play_tablet on install-tablet.", StringComparison.Ordinal), "roaming restore must keep the target travel lane explicit inside the travel campaign action-required posture.");
    Assert(plan.TravelCampaignStateLabels.Any(item => item.Contains("Cached lane:", StringComparison.Ordinal)), "roaming restore must expose cached travel campaign labels.");
    Assert(plan.TravelCampaignStateLabels.Any(item => item.Contains("Travel companion lane:", StringComparison.Ordinal)), "roaming restore must expose travel companion lane labels inside the travel campaign state list.");
    Assert(plan.TravelCompanionSummary.Contains("Cached:", StringComparison.Ordinal), "roaming restore must expose explicit cached-state truth for the travel companion lane.");
    Assert(plan.TravelCompanionSummary.Contains("Stale:", StringComparison.Ordinal), "roaming restore must expose explicit stale-state truth for the travel companion lane.");
    Assert(plan.TravelCompanionSummary.Contains("Offline actions:", StringComparison.Ordinal), "roaming restore must expose explicit offline-action boundaries for the travel companion lane.");
    Assert(plan.TravelCompanionLabels.Any(item => item.Contains("Cached lane:", StringComparison.Ordinal)), "roaming restore must expose a cached-state label for the travel companion lane.");
    Assert(plan.TravelCompanionLabels.Any(item => item.Contains("Stale lane:", StringComparison.Ordinal)), "roaming restore must expose a stale-state label for the travel companion lane.");
    Assert(plan.TravelCompanionLabels.Any(item => item.Contains("Offline action lane:", StringComparison.Ordinal)), "roaming restore must expose an offline-action label for the travel companion lane.");
    Assert(plan.TravelCompanionLabels.Any(item => item.Contains("Travel companion lane:", StringComparison.Ordinal)), "roaming restore must expose the sibling travel companion lane details.");
    Assert(plan.PrefetchLabels.Any(item => item.Contains("Prefetch inventory", StringComparison.Ordinal)), "roaming restore must expose explicit prefetch inventory labels");
    Assert(plan.PrefetchLabels.Any(item => item.Contains("Kestrel (dossier-kestrel)", StringComparison.Ordinal)), "roaming restore must expose the exact dossier set staged for offline restore");
    Assert(plan.PrefetchLabels.Any(item => item.Contains("Redmond Patrol (campaign-redmond)", StringComparison.Ordinal)), "roaming restore must expose the exact campaign set staged for offline restore");
    Assert(plan.PrefetchLabels.Any(item => item.Contains("sr6.preview.v1 [approved]", StringComparison.Ordinal)), "roaming restore must expose the exact rule posture staged for offline restore");
    Assert(plan.PrefetchLabels.Any(item => item.Contains("Linux preview installer (artifact-linux-preview)", StringComparison.Ordinal)), "roaming restore must expose the exact artifact set staged for offline restore");
    Assert(plan.PrefetchLabels.Any(item => item.Contains("Companion device", StringComparison.Ordinal)), "roaming restore must keep alternate claimed-device lanes visible when planning offline prefetch");
    Assert(plan.ReturnTargetCampaignName == "Redmond Patrol", "roaming restore must expose the primary campaign return target");
    Assert(plan.AttentionItems.Count == 1, "roaming restore should keep install-local guardrails visible even when restore state is conflict-free");
    Assert(plan.AttentionItems[0].Contains("install-local", StringComparison.Ordinal), "roaming restore attention items must preserve the install-local guardrail");
    Assert(plan.CanResume, "roaming restore must remain resumable when package-owned campaign state exists");
    Assert(!plan.RequiresConflictReview, "roaming restore should stay conflict-free for aligned package-owned state");
}

static async Task VerifyNoSessionRestoreHrefUsesRealPlayEntryRouteAsync()
{
    IRoamingWorkspaceSyncPlanner planner = new RoamingWorkspaceSyncPlanner();
    WorkspaceRestoreProjection baseline = CreateWorkspaceRestoreProjection(conflicts: Array.Empty<string>());
    WorkspaceRestoreProjection restore = baseline with
    {
        RecentDossiers =
        [
            baseline.RecentDossiers[0] with
            {
                LatestContinuity = null,
                CurrentRunId = null,
                CurrentSceneId = null,
            }
        ],
        RecentCampaigns =
        [
            baseline.RecentCampaigns[0] with
            {
                LatestContinuity = null,
                ActiveRunId = null,
                RunIds = Array.Empty<string>(),
            }
        ],
    };

    RoamingWorkspaceRestorePlan plan = planner.CreatePlan(restore, "install-tablet");

    Assert(plan.ResumeFollowThroughHref.Contains("/play?", StringComparison.Ordinal), "no-session restore must route through the concrete /play entry path");
    Assert(plan.ResumeFollowThroughHref.Contains("deviceId=install-tablet", StringComparison.Ordinal), "no-session restore href must preserve the claimed device id");
    Assert(plan.ResumeFollowThroughHref.Contains("role=Player", StringComparison.Ordinal), "no-session restore href must preserve the mapped shell role");

    var app = PlayWebApplication.Build([]);
    try
    {
        var (route, query) = SplitHref(plan.ResumeFollowThroughHref);
        var response = await ExecuteRouteResponseAsync(app, HttpMethod.Get, route, query);
        Assert(response.StatusCode == StatusCodes.Status302Found, "sessionless /play entry must redirect instead of returning a dead route");
        Assert(string.Equals(response.Location, "/index.html?role=Player&deviceId=install-tablet", StringComparison.Ordinal), "sessionless /play entry must land on the index shell with role and claimed-device context");
    }
    finally
    {
        await app.DisposeAsync();
    }
}

static void VerifyCampaignWorkspaceLiteProjectionPromotesContinuitySummary()
{
    var response = new PlayResumeResponse(
        SessionId: "session-redmond",
        Role: PlaySurfaceRole.Player,
        DeepLinkOwnerRoute: "/play/session-redmond?role=Player",
        Bootstrap: new PlayBootstrapResponse(
            "chummer6-mobile",
            new PlaySessionProjection(
                new EngineSessionCursor(
                    new EngineSessionEnvelope("session-redmond", "scene-redmond", "scene-r9", "sr6.preview.v1"),
                    12),
                Timeline: ["Reconnect complete", "Objective board refreshed"],
                GeneratedAtUtc: DateTimeOffset.Parse("2026-03-27T21:00:00+00:00")),
            new PlayShellSnapshot(PlaySurfaceRole.Player, "Player shell", "Table-safe shell", ["play.session.sync"]),
            [new PlayShellSnapshot(PlaySurfaceRole.Player, "Player shell", "Table-safe shell", ["play.session.sync"])],
            new BrowserSessionShellProbe(true, true, true),
            ["play.session.sync"],
            [],
            [new PlayCoachHint("coach-player-sync", "Sync before submitting quick actions after reconnect.")],
            [new PlayQuickAction("player-mark-ready", "Mark Ready", "play.session.sync", true)]),
        Checkpoint: new SyncCheckpoint("session-redmond", "scene-redmond", "scene-r9", "sr6.preview.v1", 12, DateTimeOffset.Parse("2026-03-27T21:00:00+00:00")),
        RuntimeBundle: new PlayRuntimeBundleMetadata("sr6.preview.v1", "scene-r9", "bundle-redmond", DateTimeOffset.Parse("2026-03-27T20:55:00+00:00"), DateTimeOffset.Parse("2026-03-27T20:56:00+00:00")),
        CachePressure: new PlayCachePressureSnapshot(2, 8, false, 0, [], DateTimeOffset.Parse("2026-03-27T21:00:00+00:00")),
        SupportNotice: new PlaySupportClosureNotice(
            StatusLabel: "Ready to verify",
            KnownIssueSummary: "If session-redmond/scene-redmond still reproduces the same problem, report it against bundle-redmond so support can ground the case against this mobile shell.",
            FixAvailabilitySummary: "Bundle bundle-redmond is the grounded local fix and update target for sr6.preview.v1.",
            NextSafeAction: "Use the current bundle proof for scene-redmond if you verify a fix or reopen support on this device.",
            FollowThroughHref: "/contact?kind=install_help&sessionId=session-redmond&sceneId=scene-redmond&bundle=bundle-redmond"));

    var projection = PlayCampaignWorkspaceLiteProjector.Create(response);
    var travelProjection = PlayCampaignWorkspaceLiteProjector.Create(response, artifactView: "travel");
    var recapProjection = PlayCampaignWorkspaceLiteProjector.Create(response, artifactView: "travel", artifactId: "artifact-recap-1");

    Assert(projection.Summary.Contains("session-redmond", StringComparison.Ordinal), "workspace-lite summary must keep the session identity visible");
    Assert(projection.Summary.Contains("Objective board refreshed", StringComparison.Ordinal), "workspace-lite summary must surface the latest timeline clue");
    Assert(projection.CurrentSceneSummary.Contains("scene-redmond", StringComparison.Ordinal), "workspace-lite summary must surface the current scene");
    Assert(projection.ChangePacketSummary.Contains("Return anchor stays on checkpoint 12", StringComparison.Ordinal), "workspace-lite summary must surface the claimed-device return anchor in the change packet.");
    Assert(projection.PlayerTableCardsSummary.Contains("Player table cards:", StringComparison.Ordinal), "workspace-lite summary must expose a dedicated player table-card summary.");
    Assert(projection.PlayerTableCardsSummary.Contains("Action budget cue:", StringComparison.Ordinal), "workspace-lite table-card summary must expose an action-budget cue.");
    Assert(projection.PlayerTableCardLabels.Any(item => item.Contains("Initiative lane:", StringComparison.Ordinal)), "workspace-lite summary must expose an initiative lane label for table cards.");
    Assert(projection.PlayerTableCardLabels.Any(item => item.Contains("Action-budget lane:", StringComparison.Ordinal)), "workspace-lite summary must expose an action-budget lane label for table cards.");
    Assert(projection.PlayerTableCardLabels.Any(item => item.Contains("Condition/effect lane:", StringComparison.Ordinal)), "workspace-lite summary must expose a condition/effect lane label for table cards.");
    Assert(projection.PlayerTableCardLabels.Any(item => item.Contains("Receipt lane:", StringComparison.Ordinal)), "workspace-lite summary must expose a receipt lane label for table cards.");
    Assert(projection.BetweenTurnAffordancesSummary.Contains("Between-turn affordances:", StringComparison.Ordinal), "workspace-lite summary must expose between-turn affordance posture.");
    Assert(projection.BetweenTurnAffordancesSummary.Contains("Support cue:", StringComparison.Ordinal), "workspace-lite between-turn summary must keep the support cue explicit.");
    Assert(projection.BetweenTurnAffordanceLabels.Any(item => item.Contains("Ready lane:", StringComparison.Ordinal)), "workspace-lite summary must expose a ready lane label for between-turn affordances.");
    Assert(projection.BetweenTurnAffordanceLabels.Any(item => item.Contains("Recap lane:", StringComparison.Ordinal)), "workspace-lite summary must expose a recap lane label for between-turn affordances.");
    Assert(projection.BetweenTurnAffordanceLabels.Any(item => item.Contains("Travel lane:", StringComparison.Ordinal)), "workspace-lite summary must expose a travel lane label for between-turn affordances.");
    Assert(projection.BetweenTurnAffordanceLabels.Any(item => item.Contains("Support lane:", StringComparison.Ordinal)), "workspace-lite summary must expose a support lane label for between-turn affordances.");
    Assert(projection.GmLiteContinuitySummary.Contains("GM-lite continuity:", StringComparison.Ordinal), "workspace-lite summary must expose a GM-lite continuity summary.");
    Assert(projection.GmLiteContinuitySummary.Contains("runboard", StringComparison.OrdinalIgnoreCase), "workspace-lite GM-lite continuity summary must keep runboard posture explicit.");
    Assert(projection.GmLiteContinuityLabels.Any(item => item.Contains("Initiative lane:", StringComparison.Ordinal)), "workspace-lite summary must expose an initiative lane label for GM-lite continuity.");
    Assert(projection.GmLiteContinuityLabels.Any(item => item.Contains("Roster lane:", StringComparison.Ordinal)), "workspace-lite summary must expose a roster lane label for GM-lite continuity.");
    Assert(projection.GmLiteContinuityLabels.Any(item => item.Contains("Objective lane:", StringComparison.Ordinal)), "workspace-lite summary must expose an objective lane label for GM-lite continuity.");
    Assert(projection.GmLiteContinuityLabels.Any(item => item.Contains("GM-lite lane:", StringComparison.Ordinal)), "workspace-lite summary must expose a bounded GM-lite lane label.");
    Assert(projection.ChangePacketLabels.Any(item => item.Contains("Scene packet: scene-redmond", StringComparison.Ordinal)), "workspace-lite summary must surface a scene packet label.");
    Assert(projection.ChangePacketLabels.Any(item => item.Contains("Latest signal: Objective board refreshed", StringComparison.Ordinal)), "workspace-lite summary must surface the latest timeline signal inside the change packet.");
    Assert(projection.ChangePacketLabels.Any(item => item.Contains("Bundle proof: bundle-redmond", StringComparison.Ordinal)), "workspace-lite summary must keep the grounded bundle proof inside the change packet.");
    Assert(projection.ChangePacketLabels.Any(item => item.Contains("Replay-safe packet: scene-redmond replay timeline", StringComparison.Ordinal)), "workspace-lite summary must surface the replay-safe packet inside the change packet.");
    Assert(projection.QuickExplainSummary.Contains("Quick explain:", StringComparison.Ordinal), "workspace-lite summary must expose a packet-backed quick explain summary.");
    Assert(projection.QuickExplainSummary.Contains("scene-redmond", StringComparison.Ordinal), "workspace-lite quick explain must keep the current scene visible.");
    Assert(projection.QuickExplainSummary.Contains("Objective board refreshed", StringComparison.Ordinal), "workspace-lite quick explain must keep the latest timeline signal visible.");
    Assert(projection.QuickExplainLabels.Any(item => item.Contains("Scene value:", StringComparison.Ordinal)), "workspace-lite quick explain must expose a scene-value anchor.");
    Assert(projection.QuickExplainLabels.Any(item => item.Contains("Return anchor value:", StringComparison.Ordinal)), "workspace-lite quick explain must expose the local return-anchor value.");
    Assert(projection.QuickExplainLabels.Any(item => item.Contains("bundle-redmond", StringComparison.Ordinal)), "workspace-lite quick explain must keep grounded bundle proof visible.");
    Assert(projection.SourceAnchorSummary.Contains("Source anchors:", StringComparison.Ordinal), "workspace-lite summary must expose source-anchor context.");
    Assert(projection.SourceAnchorSummary.Contains("/play/session-redmond?role=Player", StringComparison.Ordinal), "workspace-lite source-anchor context must keep the owner route explicit.");
    Assert(projection.SourceAnchorLabels.Any(item => item.Contains("Scene packet anchor:", StringComparison.Ordinal)), "workspace-lite source-anchor context must expose the scene packet anchor.");
    Assert(projection.SourceAnchorLabels.Any(item => item.Contains("Runtime anchor: sr6.preview.v1.", StringComparison.Ordinal)), "workspace-lite source-anchor context must expose the runtime anchor.");
    Assert(projection.StaleStatePosture.Contains("Stale-state posture: green", StringComparison.Ordinal), "workspace-lite summary must expose explicit stale-state posture.");
    Assert(projection.StaleStatePosture.Contains("bundle-redmond", StringComparison.Ordinal), "workspace-lite stale-state posture must keep grounded bundle proof explicit.");
    Assert(projection.GroundedFollowUpSummary.Contains("Grounded follow-up:", StringComparison.Ordinal), "workspace-lite summary must expose a bounded grounded follow-up summary.");
    Assert(projection.GroundedFollowUpSummary.Contains("Text-first fallback stays bounded", StringComparison.Ordinal), "workspace-lite grounded follow-up must stay text-first and bounded.");
    Assert(projection.GroundedFollowUpLabels.Any(item => item.Contains("Continue lane:", StringComparison.Ordinal)), "workspace-lite grounded follow-up must expose a continue lane.");
    Assert(projection.GroundedFollowUpLabels.Any(item => item.Contains("Support lane:", StringComparison.Ordinal)), "workspace-lite grounded follow-up must expose a support lane.");
    Assert(projection.GroundedFollowUpLabels.Any(item => item.Contains("Boundary lane:", StringComparison.Ordinal)), "workspace-lite grounded follow-up must expose a boundary lane.");
    Assert(projection.ServerPlaneSummary.Contains("Session readiness is green", StringComparison.Ordinal), "workspace-lite summary must expose campaign server-plane readiness for the current shell.");
    Assert(projection.ServerPlaneSummary.Contains("checkpoint 12", StringComparison.OrdinalIgnoreCase), "workspace-lite summary must expose the restore summary inside the server-plane summary.");
    Assert(projection.RunboardSummary.Contains("scene-redmond live runboard", StringComparison.Ordinal), "workspace-lite summary must expose the current runboard summary.");
    Assert(projection.RosterSummary.Contains("Roster readiness", StringComparison.Ordinal), "workspace-lite summary must expose roster readiness alongside the server plane.");
    Assert(projection.RecapSummary.Contains("recap-safe packet", StringComparison.Ordinal), "workspace-lite summary must expose the recap-safe packet summary.");
    Assert(projection.RecapAudienceSummary.Contains("My stuff", StringComparison.Ordinal), "workspace-lite summary must expose the personal artifact shelf view.");
    Assert(projection.RecapAudienceSummary.Contains("Campaign stuff", StringComparison.Ordinal), "workspace-lite summary must expose the campaign artifact shelf view.");
    Assert(projection.RecapAudienceSummary.Contains("Published stuff", StringComparison.Ordinal), "workspace-lite summary must expose the published artifact shelf view.");
    Assert(projection.RecapOwnershipSummary.Contains("owned mobile return lane", StringComparison.Ordinal), "workspace-lite summary must expose artifact ownership posture for the mobile return lane.");
    Assert(projection.RecapPublicationSummary.Contains("Preview Ready", StringComparison.Ordinal), "workspace-lite summary must expose artifact publication state for the recap shelf.");
    Assert(projection.RecapPublicationSummary.Contains("Review Pending", StringComparison.Ordinal), "workspace-lite summary must expose creator-publication trust ranking for the recap shelf.");
    Assert(projection.RecapPublicationSummary.Contains("Still bounded", StringComparison.Ordinal), "workspace-lite summary must expose recap discoverability posture until publication actually goes live.");
    Assert(projection.RecapPublicationSummary.Contains("published stuff", StringComparison.OrdinalIgnoreCase), "workspace-lite summary must explain that the same recap-safe packet feeds published artifact posture.");
    Assert(projection.RecapProvenanceSummary.Contains("sr6.preview.v1", StringComparison.Ordinal), "workspace-lite summary must expose recap provenance for the grounded runtime fingerprint.");
    Assert(projection.RecapProvenanceSummary.Contains("checkpoint 12", StringComparison.OrdinalIgnoreCase), "workspace-lite summary must keep the grounded checkpoint inside the recap provenance summary.");
    Assert(projection.RecapAuditSummary.Contains("artifact:session-redmond:recap", StringComparison.Ordinal), "workspace-lite summary must expose the governed recap artifact id inside the audit summary.");
    Assert(projection.RecapLineageSummary.Contains("publication:session-redmond", StringComparison.Ordinal), "workspace-lite summary must expose creator-publication lineage for the recap shelf.");
    Assert(projection.RecapLineageSummary.Contains("governed successor publication", StringComparison.Ordinal), "workspace-lite summary must keep recap lineage anchored to governed successor promotion.");
    Assert(projection.RecapNextAction.Contains("creator publication status", StringComparison.Ordinal), "workspace-lite summary must expose the next artifact-shelf step directly from the server-plane recap projection.");
    Assert(projection.RecapPublicationHref.Contains("/account/work/publications/", StringComparison.Ordinal), "workspace-lite summary must expose a direct follow-through href into creator publication status.");
    Assert(projection.ReplaySummary.Contains("replay timeline", StringComparison.Ordinal), "workspace-lite summary must expose a replay-safe package summary alongside the recap-safe packet.");
    Assert(projection.ReplaySummary.Contains("contested turns", StringComparison.OrdinalIgnoreCase), "workspace-lite summary must explain replay-safe contested-turn review.");
    Assert(projection.ReplayAudienceSummary.Contains("Campaign stuff", StringComparison.Ordinal), "workspace-lite summary must expose the campaign artifact shelf view for replay-safe packages.");
    Assert(projection.ReplayAudienceSummary.Contains("Published stuff", StringComparison.Ordinal), "workspace-lite summary must expose the published artifact shelf view for replay-safe packages.");
    Assert(projection.ReplayOwnershipSummary.Contains("replay-safe artifact", StringComparison.Ordinal), "workspace-lite summary must expose replay artifact ownership posture.");
    Assert(projection.ReplayPublicationSummary.Contains("Preview Ready", StringComparison.Ordinal), "workspace-lite summary must expose replay artifact publication state.");
    Assert(projection.ReplayPublicationSummary.Contains("Review Pending", StringComparison.Ordinal), "workspace-lite summary must expose replay creator-publication trust ranking.");
    Assert(projection.ReplayProvenanceSummary.Contains("contested-turn review lane", StringComparison.OrdinalIgnoreCase), "workspace-lite summary must keep replay provenance attached to the contested-turn review lane.");
    Assert(projection.ReplayAuditSummary.Contains("artifact:session-redmond:replay", StringComparison.Ordinal), "workspace-lite summary must expose the governed replay artifact id inside the audit summary.");
    Assert(projection.ReplayLineageSummary.Contains("publication:session-redmond:replay", StringComparison.Ordinal), "workspace-lite summary must expose creator-publication lineage for the replay shelf.");
    Assert(projection.ReplayNextAction.Contains("replay timeline", StringComparison.Ordinal), "workspace-lite summary must expose the replay-specific next artifact step.");
    Assert(projection.ReplayPublicationHref.Contains("/account/work/publications/", StringComparison.Ordinal), "workspace-lite summary must expose a direct follow-through href into replay publication status.");
    Assert(projection.SelectedArtifactView == "personal", "workspace-lite summary must default the player lane to the personal artifact shelf view.");
    Assert(projection.ArtifactShelfSelectionSummary.Contains("My stuff shelf:", StringComparison.Ordinal), "workspace-lite summary must expose a dedicated selected artifact shelf summary.");
    Assert(projection.ArtifactShelfViews.Count == 4, "workspace-lite summary must expose first-class artifact shelf views for personal, campaign, travel, and published browsing.");
    Assert(projection.ArtifactShelfViews.Any(item => item.ViewId == "personal" && item.Label == "My stuff" && item.IsSelected && item.Href == "/artifacts/session-redmond?role=Player&view=personal"), "workspace-lite summary must expose a selected personal artifact shelf view.");
    Assert(projection.ArtifactShelfViews.Any(item => item.ViewId == "campaign" && item.Label == "Campaign stuff" && item.Href == "/artifacts/session-redmond?role=Player&view=campaign"), "workspace-lite summary must expose the campaign artifact shelf view as a direct browse target.");
    Assert(projection.ArtifactShelfViews.Any(item => item.ViewId == "travel" && item.Label == "Travel cache" && item.Href == "/artifacts/session-redmond?role=Player&view=travel"), "workspace-lite summary must expose the travel artifact shelf view as a direct browse target.");
    Assert(projection.ArtifactShelfViews.Any(item => item.ViewId == "creator" && item.Label == "Published stuff" && item.Href == "/artifacts/session-redmond?role=Player&view=creator"), "workspace-lite summary must expose the published artifact shelf view as a direct browse target.");
    Assert(travelProjection.SelectedArtifactView == "travel", "workspace-lite summary must let the mobile shell reopen the travel artifact shelf view directly.");
    Assert(travelProjection.ArtifactShelfSelectionSummary.Contains("Travel shelf:", StringComparison.Ordinal), "workspace-lite summary must expose dedicated travel artifact shelf copy.");
    Assert(travelProjection.ArtifactShelfSelectionSummary.Contains("scene-redmond recap-safe packet", StringComparison.Ordinal), "workspace-lite travel shelf copy must keep recap artifacts visible inside the bounded travel lane.");
    Assert(projection.SelectedRecapArtifactSummary.Contains("no recap artifact is pinned yet", StringComparison.Ordinal), "workspace-lite summary must keep unselected recap artifact posture explicit.");
    Assert(projection.SelectedRecapArtifactHref == "/artifacts/session-redmond?role=Player&view=personal", "workspace-lite summary must fall back to the selected artifact shelf when no recap artifact is pinned.");
    Assert(recapProjection.SelectedRecapArtifactSummary.Contains("artifact-recap-1", StringComparison.Ordinal), "workspace-lite summary must expose the selected recap artifact identity.");
    Assert(recapProjection.SelectedRecapArtifactSummary.Contains("travel shelf", StringComparison.OrdinalIgnoreCase), "workspace-lite summary must keep the selected recap artifact anchored to the selected travel shelf.");
    Assert(recapProjection.SelectedRecapArtifactHref == "/artifacts/session-redmond/artifact-recap-1?role=Player&view=travel", "workspace-lite summary must expose a direct recap artifact deep link that preserves the selected travel shelf.");
    Assert(projection.LaunchPrimerSummary.Contains("Starter primer:", StringComparison.Ordinal), "workspace-lite summary must expose a starter-primer continuity summary.");
    Assert(projection.LaunchPrimerSummary.Contains("bundle-redmond", StringComparison.Ordinal), "workspace-lite starter-primer summary must keep the grounded bundle visible.");
    Assert(projection.LaunchPrimerProvenanceSummary.Contains("Starter primer provenance:", StringComparison.Ordinal), "workspace-lite summary must expose starter-primer provenance.");
    Assert(projection.LaunchPrimerHref == "/artifacts/session-redmond/artifact%3Asession-redmond%3Astarter-primer?role=Player&view=personal", "workspace-lite summary must expose a direct starter-primer artifact href.");
    Assert(projection.FirstSessionBriefingSummary.Contains("First-session briefing:", StringComparison.Ordinal), "workspace-lite summary must expose a first-session briefing continuity summary.");
    Assert(projection.FirstSessionBriefingSummary.Contains("browser detour", StringComparison.OrdinalIgnoreCase), "workspace-lite first-session briefing summary must keep the no-detour continuity claim explicit.");
    Assert(projection.FirstSessionBriefingProvenanceSummary.Contains("First-session briefing provenance:", StringComparison.Ordinal), "workspace-lite summary must expose first-session briefing provenance.");
    Assert(projection.FirstSessionBriefingHref == "/artifacts/session-redmond/artifact%3Asession-redmond%3Afirst-session-briefing?role=Player&view=travel", "workspace-lite summary must expose a direct first-session briefing artifact href.");
    Assert(projection.StarterArtifactContinuitySummary.Contains("Starter continuity:", StringComparison.Ordinal), "workspace-lite summary must expose a starter continuity summary.");
    Assert(projection.StarterArtifactContinuitySummary.Contains("Primer lane:", StringComparison.Ordinal), "workspace-lite starter continuity summary must keep the primer lane explicit.");
    Assert(projection.StarterArtifactContinuitySummary.Contains("Briefing lane:", StringComparison.Ordinal), "workspace-lite starter continuity summary must keep the briefing lane explicit.");
    Assert(projection.StarterArtifactContinuityLabels.Any(item => item.Contains("Starter primer lane:", StringComparison.Ordinal)), "workspace-lite summary must expose a starter-primer continuity label.");
    Assert(projection.StarterArtifactContinuityLabels.Any(item => item.Contains("First-session briefing lane:", StringComparison.Ordinal)), "workspace-lite summary must expose a first-session briefing continuity label.");
    Assert(projection.StarterArtifactContinuityLabels.Any(item => item.Contains("Starter artifact shelf:", StringComparison.Ordinal)), "workspace-lite summary must expose a claimed-device starter artifact shelf label.");
    Assert(projection.CampaignMemorySummary.Contains("Campaign memory:", StringComparison.Ordinal), "workspace-lite summary must expose a first-class campaign-memory summary.");
    Assert(projection.CampaignMemorySummary.Contains("governed memory lane", StringComparison.OrdinalIgnoreCase), "workspace-lite summary must keep the governed memory-lane wording explicit.");
    Assert(projection.CampaignMemorySummary.Contains("player lane", StringComparison.OrdinalIgnoreCase), "workspace-lite summary must keep the current role inside the campaign-memory summary.");
    Assert(projection.CampaignMemorySummary.Contains("replay timeline", StringComparison.Ordinal), "workspace-lite summary must keep replay-safe carry-forward inside the campaign-memory summary.");
    Assert(projection.CampaignMemorySummary.Contains("recap-safe packet", StringComparison.Ordinal), "workspace-lite summary must keep recap-safe carry-forward inside the campaign-memory summary.");
    Assert(projection.RunnerGoalUpdatesSummary.Contains("Runner goal updates:", StringComparison.Ordinal), "workspace-lite summary must expose a dedicated runner-goal update summary.");
    Assert(projection.RunnerGoalUpdatesSummary.Contains("Return moments stay player-safe", StringComparison.Ordinal), "workspace-lite runner-goal summary must keep player-safe return language explicit.");
    Assert(projection.RunnerGoalUpdateLabels.Any(item => item.Contains("Goal checkpoint lane:", StringComparison.Ordinal)), "workspace-lite summary must expose a checkpoint lane for runner-goal updates.");
    Assert(projection.RunnerGoalUpdateLabels.Any(item => item.Contains("Goal signal lane:", StringComparison.Ordinal)), "workspace-lite summary must expose a replay-safe signal lane for runner-goal updates.");
    Assert(projection.RunnerGoalUpdateLabels.Any(item => item.Contains("Goal route lane:", StringComparison.Ordinal)), "workspace-lite summary must expose a return-route lane for runner-goal updates.");
    Assert(projection.RunnerGoalUpdateLabels.Any(item => item.Contains("Goal boundary lane:", StringComparison.Ordinal)), "workspace-lite summary must expose a boundary lane for runner-goal updates.");
    Assert(projection.PlayerSafeConsequenceFeedSummary.Contains("Player-safe consequence feed:", StringComparison.Ordinal), "workspace-lite summary must expose a dedicated player-safe consequence feed summary.");
    Assert(projection.PlayerSafeConsequenceFeedSummary.Contains("BLACK LEDGER world truth", StringComparison.Ordinal), "workspace-lite consequence-feed summary must keep BLACK LEDGER truth outside mobile.");
    Assert(projection.PlayerSafeConsequenceFeedLabels.Any(item => item.Contains("Consequence lane:", StringComparison.Ordinal)), "workspace-lite summary must expose a consequence lane label.");
    Assert(projection.PlayerSafeConsequenceFeedLabels.Any(item => item.Contains("Spoiler lane:", StringComparison.Ordinal)), "workspace-lite summary must expose a spoiler-boundary lane label.");
    Assert(projection.PlayerSafeConsequenceFeedLabels.Any(item => item.Contains("Return lane:", StringComparison.Ordinal)), "workspace-lite summary must expose a return lane label for consequence feed views.");
    Assert(projection.PlayerSafeConsequenceFeedLabels.Any(item => item.Contains("Trust lane:", StringComparison.Ordinal)), "workspace-lite summary must expose a trust lane label for consequence feed views.");
    Assert(projection.CampaignMemoryReturnSummary.Contains("Memory return:", StringComparison.Ordinal), "workspace-lite summary must expose a dedicated campaign-memory return cue.");
    Assert(projection.CampaignMemoryReturnSummary.Contains("Next:", StringComparison.Ordinal), "workspace-lite summary must keep the next safe action attached to the memory return cue.");
    Assert(projection.ContinuityRailSummary.Contains("Downtime:", StringComparison.Ordinal), "workspace-lite summary must expose downtime posture on the continuity rail.");
    Assert(projection.ContinuityRailSummary.Contains("Diary:", StringComparison.Ordinal), "workspace-lite summary must expose diary posture on the continuity rail.");
    Assert(projection.ContinuityRailSummary.Contains("Contacts:", StringComparison.Ordinal), "workspace-lite summary must expose contacts posture on the continuity rail.");
    Assert(projection.ContinuityRailSummary.Contains("Heat:", StringComparison.Ordinal), "workspace-lite summary must expose heat posture on the continuity rail.");
    Assert(projection.ContinuityRailSummary.Contains("Aftermath:", StringComparison.Ordinal), "workspace-lite summary must expose aftermath posture on the continuity rail.");
    Assert(projection.ContinuityRailSummary.Contains("Return:", StringComparison.Ordinal), "workspace-lite summary must expose return posture on the continuity rail.");
    Assert(projection.ContinuityRailLabels.Any(item => item.Contains("Downtime lane:", StringComparison.Ordinal)), "workspace-lite summary must expose a downtime continuity label.");
    Assert(projection.ContinuityRailLabels.Any(item => item.Contains("Diary lane:", StringComparison.Ordinal)), "workspace-lite summary must expose a diary continuity label.");
    Assert(projection.ContinuityRailLabels.Any(item => item.Contains("Contacts lane:", StringComparison.Ordinal)), "workspace-lite summary must expose a contacts continuity label.");
    Assert(projection.ContinuityRailLabels.Any(item => item.Contains("Heat lane:", StringComparison.Ordinal)), "workspace-lite summary must expose a heat continuity label.");
    Assert(projection.ContinuityRailLabels.Any(item => item.Contains("Aftermath lane:", StringComparison.Ordinal)), "workspace-lite summary must expose an aftermath continuity label.");
    Assert(projection.ContinuityRailLabels.Any(item => item.Contains("Return lane:", StringComparison.Ordinal)), "workspace-lite summary must expose a return continuity label.");
    Assert(projection.GmOperationsSummary.Contains("Opposition:", StringComparison.Ordinal), "workspace-lite summary must expose opposition packet posture on the same campaign lane.");
    Assert(projection.GmOperationsSummary.Contains("Roster movement:", StringComparison.Ordinal), "workspace-lite summary must expose roster movement posture on the same campaign lane.");
    Assert(projection.GmOperationsSummary.Contains("Prep library:", StringComparison.Ordinal), "workspace-lite summary must expose prep library posture on the same campaign lane.");
    Assert(projection.GmOperationsSummary.Contains("Event controls:", StringComparison.Ordinal), "workspace-lite summary must expose event-control posture on the same campaign lane.");
    Assert(projection.GmOperationsSummary.Contains("audit-visible", StringComparison.Ordinal), "workspace-lite summary must keep GM and organizer operations explicitly audit-visible.");
    Assert(projection.GmOperationsSummary.Contains("support-linked", StringComparison.Ordinal), "workspace-lite summary must keep GM and organizer operations explicitly support-linked.");
    Assert(projection.GmOperationsLabels.Any(item => item.Contains("Opposition lane:", StringComparison.Ordinal)), "workspace-lite summary must expose an opposition lane label for GM operations.");
    Assert(projection.GmOperationsLabels.Any(item => item.Contains("Roster movement lane:", StringComparison.Ordinal)), "workspace-lite summary must expose a roster movement lane label for GM operations.");
    Assert(projection.GmOperationsLabels.Any(item => item.Contains("Prep library lane:", StringComparison.Ordinal)), "workspace-lite summary must expose a prep library lane label for GM operations.");
    Assert(projection.GmOperationsLabels.Any(item => item.Contains("Event controls lane:", StringComparison.Ordinal)), "workspace-lite summary must expose an event-controls lane label for GM operations.");
    Assert(projection.GmOperationsLabels.Any(item => item.Contains("Governance lane:", StringComparison.Ordinal)), "workspace-lite summary must expose a governance lane label for GM operations.");
    Assert(projection.GmOperationsLabels.Any(item => item.Contains("audit-visible", StringComparison.Ordinal)), "workspace-lite summary must keep governance lane text explicitly audit-visible.");
    Assert(projection.GmOperationsLabels.Any(item => item.Contains("support-linked", StringComparison.Ordinal)), "workspace-lite summary must keep governance lane text explicitly support-linked.");
    Assert(projection.OfflineTruthSummary.Contains("Cached:", StringComparison.Ordinal), "workspace-lite summary must expose explicit cached-state posture for offline continuity.");
    Assert(projection.OfflineTruthSummary.Contains("Stale:", StringComparison.Ordinal), "workspace-lite summary must expose explicit stale-state posture for offline continuity.");
    Assert(projection.OfflineTruthSummary.Contains("Offline actions:", StringComparison.Ordinal), "workspace-lite summary must expose explicit offline action posture for bounded local truth.");
    Assert(projection.OfflineTruthSummary.Contains("Can do now:", StringComparison.Ordinal), "workspace-lite summary must expose explicit install-local actions that are currently allowed offline.");
    Assert(projection.OfflineTruthSummary.Contains("safehouse", StringComparison.OrdinalIgnoreCase), "workspace-lite summary must keep safehouse continuity explicit in offline truth.");
    Assert(projection.OfflineTruthSummary.Contains("Needs online:", StringComparison.Ordinal), "workspace-lite summary must expose explicit actions that still require reconnect or online sync.");
    Assert(projection.OfflineTruthLabels.Any(item => item.Contains("Cached lane:", StringComparison.Ordinal)), "workspace-lite summary must expose a cached-state label for offline continuity.");
    Assert(projection.OfflineTruthLabels.Any(item => item.Contains("Stale lane:", StringComparison.Ordinal)), "workspace-lite summary must expose a stale-state label for offline continuity.");
    Assert(projection.OfflineTruthLabels.Any(item => item.Contains("Offline action lane:", StringComparison.Ordinal)), "workspace-lite summary must expose an offline action label for bounded local truth.");
    Assert(projection.OfflineTruthLabels.Any(item => item.Contains("Can-do-now lane:", StringComparison.Ordinal)), "workspace-lite summary must expose a lane label for actions allowed offline right now.");
    Assert(projection.OfflineTruthLabels.Any(item => item.Contains("Needs-online lane:", StringComparison.Ordinal)), "workspace-lite summary must expose a lane label for actions that remain online-only.");
    Assert(projection.OfflineTruthLabels.Any(item => item.Contains("safehouse", StringComparison.OrdinalIgnoreCase)), "workspace-lite summary must keep safehouse continuity explicit in offline-truth lane labels.");
    Assert(projection.MobileCampaignCurrentState.Contains("Current continuity posture:", StringComparison.Ordinal), "workspace-lite summary must expose explicit mobile campaign current posture.");
    Assert(projection.MobileCampaignStateSummary.Contains("Cached state:", StringComparison.Ordinal), "workspace-lite summary must expose explicit mobile campaign cached-state summary.");
    Assert(projection.MobileCampaignCachedState.Contains("Cached state:", StringComparison.Ordinal), "workspace-lite summary must expose explicit mobile campaign cached-state detail.");
    Assert(projection.MobileCampaignStaleState.Contains("Stale state:", StringComparison.Ordinal), "workspace-lite summary must expose explicit mobile campaign stale-state detail.");
    Assert(projection.MobileCampaignActionRequired.Contains("Action required:", StringComparison.Ordinal), "workspace-lite summary must expose explicit mobile campaign action-required posture.");
    Assert(projection.MobileCampaignActionRequired.Contains("Mobile shell owner: player lane. Session: scene-redmond.", StringComparison.Ordinal), "workspace-lite summary must keep the claimed mobile lane explicit inside the campaign action-required posture.");
    Assert(projection.MobileCampaignStateLabels.Any(item => item.Contains("Cached lane:", StringComparison.Ordinal)), "workspace-lite summary must expose cached mobile campaign labels.");
    Assert(projection.MobileCampaignStateLabels.Any(item => item.Contains("Action-required lane:", StringComparison.Ordinal)), "workspace-lite summary must expose action-required mobile campaign labels.");
    Assert(projection.DecisionNotice.Contains("Continue scene-redmond", StringComparison.Ordinal), "workspace-lite summary must expose the active campaign decision notice.");
    Assert(projection.DecisionNoticeHref.Contains("/play/session-redmond?role=Player", StringComparison.Ordinal), "workspace-lite summary must expose a direct decision-notice follow-through href.");
    Assert(projection.RolePosture.Contains("/play/session-redmond?role=Player", StringComparison.Ordinal), "workspace-lite summary must expose the role route posture");
    Assert(projection.RolePosture.Contains("player lane", StringComparison.OrdinalIgnoreCase), "workspace-lite summary must expose the current device role posture");
    Assert(projection.RulePosture.Contains("sr6.preview.v1", StringComparison.Ordinal), "workspace-lite summary must surface the runtime fingerprint");
    Assert(projection.LegalRunnerSummary.Contains("bundle-redmond", StringComparison.Ordinal), "workspace-lite summary must surface explicit legal-runner proof for the grounded bundle.");
    Assert(projection.LegalRunnerSummary.Contains("sr6.preview.v1", StringComparison.Ordinal), "workspace-lite summary must keep the grounded runtime fingerprint inside the legal-runner proof.");
    Assert(projection.UnderstandableReturnSummary.Contains("Checkpoint 12", StringComparison.Ordinal), "workspace-lite summary must surface the checkpoint inside the understandable-return proof.");
    Assert(projection.UnderstandableReturnSummary.Contains("Restore summary:", StringComparison.Ordinal), "workspace-lite summary must keep the restore lane explicit inside the understandable-return proof.");
    Assert(projection.CampaignReadySummary.Contains("Session readiness is green", StringComparison.Ordinal), "workspace-lite summary must surface explicit campaign-ready proof for the grounded shell.");
    Assert(projection.CampaignReadySummary.Contains("Roster readiness", StringComparison.Ordinal), "workspace-lite summary must keep roster posture attached to the campaign-ready proof.");
    Assert(projection.SafeNextAction.Contains("Sync before taking the next quick action", StringComparison.Ordinal), "workspace-lite summary must point the player lane at the next safe action");
    Assert(projection.RejoinCommand.Contains("Rejoin scene-redmond", StringComparison.Ordinal), "workspace-lite summary must expose a dedicated rejoin command for the current scene.");
    Assert(projection.RejoinCommandHref.Contains("/play/session-redmond?role=Player", StringComparison.Ordinal), "workspace-lite summary must expose a direct rejoin command href.");
    Assert(projection.DisconnectRecoveryCopy.Contains("Disconnect recovery:", StringComparison.Ordinal), "workspace-lite summary must expose explicit disconnect recovery copy.");
    Assert(projection.DisconnectRecoveryCopy.Contains("no-loss", StringComparison.OrdinalIgnoreCase), "workspace-lite summary disconnect copy must keep no-loss reconnect intent explicit.");
    Assert(projection.RoleChangeRecoveryCopy.Contains("Role-change recovery:", StringComparison.Ordinal), "workspace-lite summary must expose explicit role-change recovery copy.");
    Assert(projection.RoleChangeRecoveryCopy.Contains("session-redmond/scene-redmond", StringComparison.Ordinal), "workspace-lite summary role-change copy must keep session and scene continuity explicit.");
    Assert(projection.ObserverTransitionRecoveryCopy.Contains("Observer transition recovery:", StringComparison.Ordinal), "workspace-lite summary must expose explicit observer-transition recovery copy.");
    Assert(projection.ObserverTransitionRecoveryCopy.Contains("observe mode", StringComparison.OrdinalIgnoreCase), "workspace-lite summary observer-transition copy must explain switching into observer posture.");
    Assert(projection.ContinueCommand.Contains("Sync before taking the next quick action", StringComparison.Ordinal), "workspace-lite summary must expose a dedicated continue command tied to next-safe-action guidance.");
    Assert(projection.ContinueCommandHref.Contains("/play/session-redmond?role=Player", StringComparison.Ordinal), "workspace-lite summary must expose a direct continue command href.");
    Assert(projection.SupportCommand.Contains("bundle proof", StringComparison.Ordinal), "workspace-lite summary must expose a dedicated support command tied to grounded fix verification.");
    Assert(projection.SupportCommandHref.Contains("/contact", StringComparison.Ordinal), "workspace-lite summary must expose a direct support command href.");
    Assert(projection.LongRunningDecisionReceiptSummary.Contains("Decision receipts are active", StringComparison.Ordinal), "workspace-lite summary must expose a decision-receipt summary for long-running shell actions.");
    Assert(projection.LongRunningDecisionReceiptSummary.Contains("/contact", StringComparison.Ordinal), "workspace-lite summary decision-receipt summary must point to one canonical support escalation route.");
    Assert(projection.LongRunningDecisionReceipts.Count == 3, "workspace-lite summary must expose one decision receipt each for rejoin, quick actions, and resume.");
    Assert(projection.LongRunningDecisionReceipts.Any(item => item.Contains("Rejoin receipt:", StringComparison.Ordinal) && item.Contains("skipped", StringComparison.OrdinalIgnoreCase)), "workspace-lite summary must publish a rejoin decision receipt with retried/skipped/deferred outcome copy.");
    Assert(projection.LongRunningDecisionReceipts.Any(item => item.Contains("Quick-action receipt:", StringComparison.Ordinal) && item.Contains("deferred", StringComparison.OrdinalIgnoreCase)), "workspace-lite summary must publish a quick-action decision receipt with retried/skipped/deferred outcome copy.");
    Assert(projection.LongRunningDecisionReceipts.Any(item => item.Contains("Resume receipt:", StringComparison.Ordinal) && item.Contains("resumed", StringComparison.OrdinalIgnoreCase)), "workspace-lite summary must publish a resume decision receipt with explicit progress/resume state.");
    Assert(projection.LowNoiseGuidance.Count == 3, "workspace-lite summary must expose low-noise guidance for rejoin, continue, and support routes.");
    Assert(projection.LowNoiseGuidance.All(item => item.Contains("route:", StringComparison.OrdinalIgnoreCase)), "workspace-lite summary low-noise guidance must stay route-oriented and concise.");
    Assert(projection.ContinuityPosture.Contains("Checkpoint 12", StringComparison.Ordinal), "workspace-lite summary must expose the aligned continuity checkpoint");
    Assert(projection.CachePosture.Contains("2/8", StringComparison.Ordinal), "workspace-lite summary must expose cache posture");
    Assert(projection.TravelPosture.Contains("bounded offline use", StringComparison.Ordinal), "workspace-lite summary must make travel readiness deliberate on the claimed device");
    Assert(projection.TravelPosture.Contains("checkpoint 12", StringComparison.OrdinalIgnoreCase), "workspace-lite summary must keep the travel posture tied to the pinned local checkpoint");
    Assert(projection.TravelPosture.Contains("replay timeline", StringComparison.Ordinal), "workspace-lite summary must keep replay-safe posture visible in the travel handoff.");
    Assert(projection.OfflinePrefetchSummary.Contains("bundle-redmond", StringComparison.Ordinal), "workspace-lite summary must expose the grounded runtime bundle inside the offline prefetch summary");
    Assert(projection.OfflinePrefetchSummary.Contains("scene-redmond return dossier", StringComparison.Ordinal), "workspace-lite summary must name the grounded dossier staged for offline travel");
    Assert(projection.OfflinePrefetchSummary.Contains("scene-redmond mobile return", StringComparison.Ordinal), "workspace-lite summary must name the grounded campaign staged for offline travel");
    Assert(projection.OfflinePrefetchSummary.Contains("sr6.preview.v1", StringComparison.Ordinal), "workspace-lite summary must name the grounded rule environment staged for offline travel");
    Assert(projection.OfflinePrefetchSummary.Contains("scene-redmond replay timeline", StringComparison.Ordinal), "workspace-lite summary must include the replay-safe package inside offline prefetch posture.");
    Assert(projection.OfflinePrefetchSummary.Contains("scene-redmond recap-safe packet", StringComparison.Ordinal), "workspace-lite summary must include the recap-safe packet inside offline prefetch posture.");
    Assert(projection.OfflinePrefetchSummary.Contains("install-local", StringComparison.Ordinal), "workspace-lite summary must keep the offline prefetch summary explicit about install-local boundaries");
    Assert(projection.UpdatePosture.Contains("bundle-redmond", StringComparison.Ordinal), "workspace-lite summary must expose the current update posture for the validated runtime bundle");
    Assert(projection.SupportPosture.Contains("session-redmond/scene-redmond", StringComparison.Ordinal), "workspace-lite summary must expose install-safe support posture for the current session");
    Assert(projection.SupportStatus.Contains("Ready to verify", StringComparison.Ordinal), "workspace-lite summary must expose support case status for the current mobile shell.");
    Assert(projection.KnownIssueSummary.Contains("session-redmond/scene-redmond", StringComparison.Ordinal), "workspace-lite summary must expose the known issue summary for the current grounded shell.");
    Assert(projection.FixAvailabilitySummary.Contains("bundle-redmond", StringComparison.Ordinal), "workspace-lite summary must expose channel-aware fix availability for the grounded runtime bundle.");
    Assert(projection.CurrentCautionSummary.Contains("no extra caution is published", StringComparison.OrdinalIgnoreCase), "workspace-lite summary must expose an explicit lowered caution lane when the grounded bundle is already available.");
    Assert(projection.UpdateFollowThrough.Contains("bundle-redmond", StringComparison.Ordinal), "workspace-lite summary must surface an explicit update follow-through route for the validated runtime bundle.");
    Assert(projection.UpdateFollowThroughHref.Contains("/downloads", StringComparison.Ordinal), "workspace-lite summary must provide a direct update follow-through href.");
    Assert(projection.SupportFollowThrough.Contains("bundle proof", StringComparison.Ordinal), "workspace-lite summary must surface an explicit support follow-through route tied to grounded fix verification.");
    Assert(projection.SupportFollowThroughHref.Contains("/contact", StringComparison.Ordinal), "workspace-lite summary must provide a direct support follow-through href.");
    Assert(projection.SupportFollowThroughHref.Contains("kind=install_help", StringComparison.Ordinal), "workspace-lite summary must preselect the install-help support intake for direct follow-through.");
    Assert(projection.SupportFollowThroughHref.Contains("sessionId=session-redmond", StringComparison.Ordinal), "workspace-lite summary must keep the session id in the support follow-through href.");
    Assert(projection.SupportFollowThroughHref.Contains("sceneId=scene-redmond", StringComparison.Ordinal), "workspace-lite summary must keep the scene id in the support follow-through href.");
    Assert(projection.SupportFollowThroughHref.Contains("bundle=bundle-redmond", StringComparison.Ordinal), "workspace-lite summary must keep the grounded runtime bundle in the support follow-through href.");
    Assert(projection.RoleFollowThrough.Contains("player lane", StringComparison.OrdinalIgnoreCase), "workspace-lite summary must surface an explicit role follow-through route for the current device posture.");
    Assert(projection.RoleFollowThroughHref.Contains("/play/session-redmond?role=Player", StringComparison.Ordinal), "workspace-lite summary must provide a direct role follow-through href.");
    Assert(projection.QuickActionLabels.SequenceEqual(["Mark Ready"]), "workspace-lite summary must surface quick action labels");
    Assert(projection.FollowThroughLabels.Count >= 3, "workspace-lite summary must surface explicit follow-through labels for update, support, and role posture.");
    Assert(projection.FollowThroughLabels.Any(item => item.Contains("Review Pending", StringComparison.Ordinal)), "workspace-lite summary must carry artifact publication trust ranking into follow-through labels.");
    Assert(projection.FollowThroughLabels.Any(item => item.Contains("Artifact lineage:", StringComparison.Ordinal)), "workspace-lite summary must carry artifact publication lineage into follow-through labels.");
    Assert(projection.FollowThroughLabels.Any(item => item.Contains("creator publication status", StringComparison.Ordinal)), "workspace-lite summary must carry the artifact publication next step into follow-through labels.");
    Assert(projection.FollowThroughLabels.Any(item => item.Contains("replay timeline", StringComparison.Ordinal)), "workspace-lite summary must carry replay-safe follow-through labels alongside recap-safe posture.");
    Assert(projection.FollowThroughLabels.Any(item => item.Contains("Current caution:", StringComparison.Ordinal) && item.Contains("bundle-redmond", StringComparison.Ordinal)), "workspace-lite summary must keep the explicit caution lane inside follow-through labels.");
    Assert(projection.ChangePacketLabels.Any(item => item.Contains("Travel-safe packet: checkpoint 12 + bundle-redmond", StringComparison.Ordinal)), "workspace-lite summary must expose the bounded offline travel packet inside the change packet labels.");
    Assert(projection.FollowThroughLabels.Any(item => item.Contains("bundle-redmond", StringComparison.Ordinal)), "workspace-lite summary must keep the update follow-through tied to the validated runtime bundle.");
    Assert(projection.FollowThroughLabels.Any(item => item.Contains("session-redmond/scene-redmond", StringComparison.Ordinal)), "workspace-lite summary must keep support follow-through tied to the grounded session context.");
    Assert(projection.CoachHints.SequenceEqual(["Sync before submitting quick actions after reconnect."]), "workspace-lite summary must surface coach hints");
}

static void VerifyCachePressureDecisionNoticeUsesSupportNextActionCopy()
{
    var response = CreateWorkspaceLiteProjectionResponse(
        sessionId: "session-cache-support",
        sceneId: "scene-redmond",
        sceneRevision: "scene-r9",
        runtimeFingerprint: "sr6.preview.v1",
        role: PlaySurfaceRole.Player,
        route: "/play/session-cache-support",
        timeline: ["Reconnect complete", "Objective board refreshed"],
        capabilities: ["play.session.sync"],
        coachHints:
        [
            new PlayCoachHint("coach-player-sync", "Sync before submitting quick actions after reconnect.")
        ],
        quickActions:
        [
            new PlayQuickAction("player-mark-ready", "Mark Ready", "play.session.sync", true)
        ],
        bundleTag: "bundle-redmond",
        sequence: 12) with
    {
        CachePressure = new PlayCachePressureSnapshot(8, 8, true, 2, [], DateTimeOffset.Parse("2026-03-29T18:40:00+00:00")),
        SupportNotice = new PlaySupportClosureNotice(
            StatusLabel: "Ready to verify",
            KnownIssueSummary: "Cache pressure can still evict bundle-redmond before the next safe return.",
            FixAvailabilitySummary: "Bundle bundle-redmond is the grounded local fix target for sr6.preview.v1.",
            NextSafeAction: "Use the current bundle proof for scene-redmond if you verify a fix or reopen support on this device.",
            FollowThroughHref: "/contact?kind=install_help&sessionId=session-cache-support&sceneId=scene-redmond&bundle=bundle-redmond")
    };

    var projection = PlayCampaignWorkspaceLiteProjector.Create(response);

    Assert(projection.DecisionNotice.Contains("Use the current bundle proof for scene-redmond", StringComparison.Ordinal), "cache-pressure decision notices must reuse the live support next-safe action instead of a generic support-follow-through label.");
    Assert(!projection.DecisionNotice.Contains("Open support follow-through", StringComparison.Ordinal), "cache-pressure decision notices must not fall back to the old generic support-follow-through label.");
    Assert(projection.DecisionNoticeHref.Contains("/contact", StringComparison.Ordinal), "cache-pressure decision notices must keep the direct support follow-through href.");
    Assert(projection.TravelPosture.Contains("degraded", StringComparison.OrdinalIgnoreCase), "cache-pressure workspace-lite projection must mark travel posture as degraded when stale pressure is active.");
    Assert(projection.ContinuityRailSummary.Contains("degraded", StringComparison.OrdinalIgnoreCase), "cache-pressure workspace-lite projection must keep degraded continuity wording visible on the heat rail.");
    Assert(projection.ContinuityRailLabels.Any(item => item.Contains("degraded", StringComparison.OrdinalIgnoreCase)), "cache-pressure workspace-lite projection must keep degraded continuity labels on the heat rail.");
    Assert(projection.OfflineTruthSummary.Contains("degraded", StringComparison.OrdinalIgnoreCase), "cache-pressure workspace-lite projection must mark stale offline truth as degraded.");
    Assert(projection.OfflineTruthLabels.Any(item => item.Contains("degraded", StringComparison.OrdinalIgnoreCase)), "cache-pressure workspace-lite projection must keep degraded stale labels for offline truth.");
    Assert(projection.CurrentCautionSummary.Contains("Clear cache pressure", StringComparison.Ordinal), "cache-pressure workspace-lite projection must elevate the cache-pressure caution lane explicitly.");
}

static void VerifyDisconnectRecoveryCopyUsesConcreteRouteWithoutCheckpoint()
{
    var response = new PlayResumeResponse(
        SessionId: "session-disconnect-null",
        Role: PlaySurfaceRole.Observer,
        DeepLinkOwnerRoute: PlayRouteHandlers.BuildOwnerRoute("session-disconnect-null", PlaySurfaceRole.Observer),
        Bootstrap: new PlayBootstrapResponse(
            "chummer6-mobile",
            new PlaySessionProjection(
                new EngineSessionCursor(
                    new EngineSessionEnvelope("session-disconnect-null", "scene-null", "scene-r1", "runtime-null"),
                    0),
                Timeline: ["Reconnect pending"],
                GeneratedAtUtc: DateTimeOffset.Parse("2026-04-10T08:00:00+00:00")),
            new PlayShellSnapshot(PlaySurfaceRole.Observer, "Observer Shell", "Read-mostly shell.", ["play.session.read"]),
            [new PlayShellSnapshot(PlaySurfaceRole.Observer, "Observer Shell", "Read-mostly shell.", ["play.session.read"])],
            new BrowserSessionShellProbe(true, false, true),
            ["play.session.read"],
            [],
            [new PlayCoachHint("coach-observer-continuity", "Stay read-mostly until owner confirmation arrives.")],
            []),
        Checkpoint: null,
        RuntimeBundle: null,
        CachePressure: new PlayCachePressureSnapshot(0, 8, false, 0, [], DateTimeOffset.Parse("2026-04-10T08:00:00+00:00")));

    var projection = PlayCampaignWorkspaceLiteProjector.Create(response);
    string expectedRoute = PlayRouteHandlers.BuildOwnerRoute("session-disconnect-null", PlaySurfaceRole.Observer);
    Assert(projection.DisconnectRecoveryCopy.Contains(expectedRoute, StringComparison.Ordinal), "disconnect recovery copy must use a concrete role-safe route when checkpoint state is missing.");
    Assert(projection.DisconnectRecoveryCopy.Contains("seed a fresh checkpoint", StringComparison.OrdinalIgnoreCase), "disconnect recovery copy must keep the null-checkpoint recovery branch explicit.");
}

static void VerifyEntryRecoveryProjectionCoversNoSessionNoCampaignAndPostFailure()
{
    var noSessionResume = CreateWorkspaceLiteProjectionResponse(
            sessionId: "session-empty",
            sceneId: "scene-main",
            sceneRevision: "scene-r0",
            runtimeFingerprint: "runtime-local",
            role: PlaySurfaceRole.Player,
            route: "/play/session-empty",
            timeline: ["No timeline events are cached yet."],
            capabilities: ["play.session.sync"],
            coachHints:
            [
                new PlayCoachHint("coach-player-sync", "Sync before submitting quick actions after reconnect.")
            ],
            quickActions:
            [
                new PlayQuickAction("player-mark-ready", "Mark Ready", "play.session.sync", true)
            ],
            bundleTag: "bundle-local",
            sequence: 0) with
    {
        Checkpoint = null,
        RuntimeBundle = null
    };
    var noSessionRestore = new RoamingWorkspaceRestorePlan(
        RestoreId: "restore-empty",
        TargetDeviceId: "install-empty",
        DeviceRole: "play_tablet",
        ResumeSummary: "No claimed-device packet is cached yet.",
        SafeNextAction: "Start role-safe session bootstrap",
        ResumeFollowThrough: "Start role-safe session bootstrap",
        ResumeFollowThroughHref: "/play/session-empty",
        SupportFollowThrough: "Open support",
        SupportFollowThroughHref: "/contact",
        RuleEnvironmentSummary: "None",
        PrefetchReadinessSummary: "None",
        LocalCacheBoundarySummary: "install-local",
        OfflineTruthSummary: "None",
        OfflineTruthLabels: [],
        ActionRequiredSummary: "Claim or reconnect this device before continuity can start.",
        ActionRequiredLabels: ["Action-required lane: seed a claimed restore packet before offline continuity is allowed."],
        TravelCampaignCurrentState: "Current continuity posture: no bounded travel continuity packet is staged for play_tablet yet.",
        TravelCampaignStateSummary: "Cached state: no cached travel continuity packet is staged for play_tablet.",
        TravelCampaignCachedState: "Cached state: no cached travel campaign state is attached to install-empty yet.",
        TravelCampaignStaleState: "Stale state: pending until play_tablet seeds a bounded travel continuity packet.",
        TravelCampaignActionRequired: "Action required: claim or reconnect play_tablet before this device can carry campaign continuity at all.",
        TravelCampaignStateLabels: ["Cached lane: no travel continuity packet is staged yet."],
        TravelCompanionSummary: "None",
        TravelCompanionLabels: [],
        PrefetchLabels: [],
        StarterPrimerFollowThrough: "Open starter primer",
        StarterPrimerFollowThroughHref: "/artifacts/session-empty/artifact%3Asession-empty%3Astarter-primer?deviceId=install-empty&role=Player&view=travel",
        FirstSessionBriefingFollowThrough: "Open first-session briefing",
        FirstSessionBriefingFollowThroughHref: "/artifacts/session-empty/artifact%3Asession-empty%3Afirst-session-briefing?deviceId=install-empty&role=Player&view=travel",
        ReturnTargetCampaignName: null,
        AttentionItems: [],
        ConflictSummaries: [],
        LocalOnlyNotes: [],
        CanResume: false,
        RequiresConflictReview: false,
        Dossiers: [],
        Campaigns: [],
        RuleEnvironments: [],
        Artifacts: [],
        Entitlements: []);
    var noSessionProjection = PlayEntryRecoveryProjector.Create(noSessionResume, noSessionRestore);
    Assert(noSessionProjection.EntryState == "no_session", "entry recovery must classify empty cache state as no_session");
    Assert(noSessionProjection.RecommendedActionLabel.Contains("starter primer", StringComparison.OrdinalIgnoreCase), "entry recovery no_session state must recommend the starter-primer lane.");
    Assert(noSessionProjection.RecommendedActionHref.Contains("starter-primer", StringComparison.OrdinalIgnoreCase), "entry recovery no_session state must route directly to the starter-primer artifact lane.");
    Assert(noSessionProjection.RecoveryActions.Any(item => item.Contains("Starter primer lane:", StringComparison.Ordinal)), "entry recovery no_session state must expose starter-primer continuity guidance.");
    Assert(noSessionProjection.RecoveryActions.Any(item => item.Contains("First-session briefing lane:", StringComparison.Ordinal)), "entry recovery no_session state must expose first-session briefing continuity guidance.");

    var noCampaignResume = CreateWorkspaceLiteProjectionResponse(
        sessionId: "session-no-campaign",
        sceneId: "scene-no-campaign",
        sceneRevision: "scene-r2",
        runtimeFingerprint: "runtime-no-campaign",
        role: PlaySurfaceRole.Player,
        route: "/play/session-no-campaign",
        timeline: ["Reconnect complete"],
        capabilities: ["play.session.sync"],
        coachHints: [new PlayCoachHint("coach-player-sync", "Sync before submitting quick actions after reconnect.")],
        quickActions: [new PlayQuickAction("player-mark-ready", "Mark Ready", "play.session.sync", true)],
        bundleTag: "bundle-no-campaign",
        sequence: 4);
    var noCampaignRestore = new RoamingWorkspaceRestorePlan(
        RestoreId: "restore-no-campaign",
        TargetDeviceId: "install-no-campaign",
        DeviceRole: "play_tablet",
        ResumeSummary: "No campaign is attached yet.",
        SafeNextAction: "Create campaign return target",
        ResumeFollowThrough: "Create campaign return target",
        ResumeFollowThroughHref: "/campaigns/new?source=mobile-play",
        SupportFollowThrough: "Open support",
        SupportFollowThroughHref: "/contact",
        RuleEnvironmentSummary: "None",
        PrefetchReadinessSummary: "None",
        LocalCacheBoundarySummary: "install-local",
        OfflineTruthSummary: "None",
        OfflineTruthLabels: [],
        ActionRequiredSummary: "Create a campaign return target before continuity can start.",
        ActionRequiredLabels: ["Action-required lane: seed a claimed restore packet before offline continuity is allowed."],
        TravelCampaignCurrentState: "Current continuity posture: no bounded travel continuity packet is staged for play_tablet yet.",
        TravelCampaignStateSummary: "Cached state: no cached travel continuity packet is staged for play_tablet.",
        TravelCampaignCachedState: "Cached state: no cached travel campaign state is attached to install-no-campaign yet.",
        TravelCampaignStaleState: "Stale state: pending until play_tablet seeds a bounded travel continuity packet.",
        TravelCampaignActionRequired: "Action required: claim or reconnect play_tablet before this device can carry campaign continuity at all.",
        TravelCampaignStateLabels: ["Cached lane: no travel continuity packet is staged yet."],
        TravelCompanionSummary: "None",
        TravelCompanionLabels: [],
        PrefetchLabels: [],
        StarterPrimerFollowThrough: "Open starter primer",
        StarterPrimerFollowThroughHref: "/artifacts/session-no-campaign/artifact%3Asession-no-campaign%3Astarter-primer?deviceId=install-no-campaign&role=Player&view=travel",
        FirstSessionBriefingFollowThrough: "Open first-session briefing",
        FirstSessionBriefingFollowThroughHref: "/artifacts/session-no-campaign/artifact%3Asession-no-campaign%3Afirst-session-briefing?deviceId=install-no-campaign&role=Player&view=travel",
        ReturnTargetCampaignName: null,
        AttentionItems: [],
        ConflictSummaries: [],
        LocalOnlyNotes: [],
        CanResume: false,
        RequiresConflictReview: false,
        Dossiers: [],
        Campaigns: [],
        RuleEnvironments: [],
        Artifacts: [],
        Entitlements: []);
    var noCampaignProjection = PlayEntryRecoveryProjector.Create(noCampaignResume, noCampaignRestore);
    Assert(noCampaignProjection.EntryState == "no_campaign", "entry recovery must classify missing campaign target as no_campaign");
    Assert(noCampaignProjection.RecommendedActionLabel.Contains("starter primer", StringComparison.OrdinalIgnoreCase), "entry recovery no_campaign state must stay on the starter-primer continuity lane.");
    Assert(noCampaignProjection.RecommendedActionHref.Contains("starter-primer", StringComparison.OrdinalIgnoreCase), "entry recovery no_campaign state must recommend one-tap starter-primer follow-through");

    var postFailureResume = CreateWorkspaceLiteProjectionResponse(
            sessionId: "session-post-failure",
            sceneId: "scene-post-failure",
            sceneRevision: "scene-r4",
            runtimeFingerprint: "runtime-post-failure",
            role: PlaySurfaceRole.Player,
            route: "/play/session-post-failure",
            timeline: ["Reconnect complete"],
            capabilities: ["play.session.sync"],
            coachHints: [new PlayCoachHint("coach-player-sync", "Sync before submitting quick actions after reconnect.")],
            quickActions: [new PlayQuickAction("player-mark-ready", "Mark Ready", "play.session.sync", true)],
            bundleTag: "bundle-post-failure",
            sequence: 6) with
    {
        CachePressure = new PlayCachePressureSnapshot(8, 8, true, 2, [], DateTimeOffset.UtcNow),
        SupportNotice = new PlaySupportClosureNotice(
            StatusLabel: "Runtime proof missing",
            KnownIssueSummary: "Runtime proof missing",
            FixAvailabilitySummary: "Fix unavailable",
            NextSafeAction: "Reconnect",
            FollowThroughHref: "/contact")
    };
    var postFailureRestore = new RoamingWorkspaceRestorePlan(
        RestoreId: "restore-post-failure",
        TargetDeviceId: "install-post-failure",
        DeviceRole: "play_tablet",
        ResumeSummary: "Conflict review required",
        SafeNextAction: "Review restore conflicts on play_tablet before resume",
        ResumeFollowThrough: "Open restore review",
        ResumeFollowThroughHref: "/play/session-post-failure?restoreReview=1",
        SupportFollowThrough: "Open support",
        SupportFollowThroughHref: "/contact",
        RuleEnvironmentSummary: "sr6.preview.v1 · candidate · campaign",
        PrefetchReadinessSummary: "warning-only",
        LocalCacheBoundarySummary: "install-local",
        OfflineTruthSummary: "warning posture",
        OfflineTruthLabels: ["Stale lane: warning-only"],
        ActionRequiredSummary: "Review restore conflicts before you trust stale or travel campaign state on play_tablet.",
        ActionRequiredLabels: ["Action-required lane: clear restore conflict review before mutating campaign continuity on install-post-failure."],
        TravelCampaignCurrentState: "Current continuity posture: warning posture is active until restore conflict review closes.",
        TravelCampaignStateSummary: "Cached state: warning-only until restore conflict review closes.",
        TravelCampaignCachedState: "Cached state: staged packet remains bounded during restore conflict review.",
        TravelCampaignStaleState: "Stale state: warning posture is active until restore conflict review closes.",
        TravelCampaignActionRequired: "Action required: review restore conflicts before you trust stale or travel campaign state on play_tablet.",
        TravelCampaignStateLabels: ["Cached lane: staged packet remains bounded during restore conflict review."],
        TravelCompanionSummary: "warning posture",
        TravelCompanionLabels: ["Stale lane: warning-only"],
        PrefetchLabels: [],
        StarterPrimerFollowThrough: "Review primer follow-through",
        StarterPrimerFollowThroughHref: "/artifacts/session-post-failure/artifact%3Asession-post-failure%3Astarter-primer?deviceId=install-post-failure&role=Player&view=travel",
        FirstSessionBriefingFollowThrough: "Review first-session briefing follow-through",
        FirstSessionBriefingFollowThroughHref: "/artifacts/session-post-failure/artifact%3Asession-post-failure%3Afirst-session-briefing?deviceId=install-post-failure&role=Player&view=travel",
        ReturnTargetCampaignName: "Redmond Patrol",
        AttentionItems: [],
        ConflictSummaries: ["conflict"],
        LocalOnlyNotes: [],
        CanResume: true,
        RequiresConflictReview: true,
        Dossiers: [],
        Campaigns: [],
        RuleEnvironments: [],
        Artifacts: [],
        Entitlements: []);
    var postFailureProjection = PlayEntryRecoveryProjector.Create(postFailureResume, postFailureRestore);
    Assert(postFailureProjection.EntryState == "post_failure", "entry recovery must classify conflict/cache/runtime proof failures as post_failure");
    Assert(postFailureProjection.RecommendedActionLabel.Contains("Review", StringComparison.OrdinalIgnoreCase), "entry recovery post_failure state must recommend one-tap restore review");
    Assert(postFailureProjection.EntryStateSummary.Contains("cached, stale, and action-required travel continuity", StringComparison.Ordinal), "entry recovery post_failure state must keep continuity breakdown explicit before resume");
    Assert(postFailureProjection.RecoveryActions.Count == 8, "entry recovery must expose retry/cancel/restore, explicit travel continuity breakdown, and starter continuity guidance labels for every entry state");
    Assert(postFailureProjection.RecoveryActions.Any(item => item.Contains("Travel continuity cached state:", StringComparison.Ordinal)), "entry recovery post_failure state must repeat cached travel continuity in recovery actions");
    Assert(postFailureProjection.RecoveryActions.Any(item => item.Contains("Travel continuity stale state:", StringComparison.Ordinal)), "entry recovery post_failure state must repeat stale travel continuity in recovery actions");
    Assert(postFailureProjection.RecoveryActions.Any(item => item.Contains("Travel continuity action required:", StringComparison.Ordinal)), "entry recovery post_failure state must repeat action-required travel continuity in recovery actions");
}

static void VerifyRoamingWorkspaceRestorePlanPreservesConflictAndInstallLocalGuardrails()
{
    IRoamingWorkspaceSyncPlanner planner = new RoamingWorkspaceSyncPlanner();
    WorkspaceRestoreProjection restore = CreateWorkspaceRestoreProjection(
        conflicts:
        [
            "Claimed installs are on different channels; restore should confirm which campaign posture is current."
        ],
        approvalState: "candidate");

    RoamingWorkspaceRestorePlan plan = planner.CreatePlan(restore, "install-workstation");

    Assert(plan.RequiresConflictReview, "roaming restore must keep explicit conflict review when channels drift");
    Assert(plan.ConflictSummaries.Count == 1, "roaming restore must preserve explicit conflict summaries");
    Assert(plan.ConflictSummaries[0].Contains("different channels", StringComparison.Ordinal), "roaming restore must keep the original conflict language");
    Assert(plan.SafeNextAction.Contains("Review restore conflicts", StringComparison.Ordinal), "roaming restore must force conflict review before resume");
    Assert(plan.ResumeFollowThrough.Contains("restore review", StringComparison.Ordinal), "roaming restore must switch the follow-through label into explicit conflict review mode.");
    Assert(plan.SupportFollowThroughHref.Contains("different%20channels", StringComparison.OrdinalIgnoreCase), "roaming restore support href must preserve the conflict summary for support follow-through.");
    Assert(plan.RuleEnvironmentSummary == "sr6.preview.v1 · candidate · campaign", "roaming restore must preserve non-approved rule posture for the shell");
    Assert(plan.PrefetchReadinessSummary.Contains("warning-only", StringComparison.OrdinalIgnoreCase), "roaming restore must downgrade prefetch readiness when conflict review is still required");
    Assert(plan.OfflineTruthSummary.Contains("warning posture", StringComparison.OrdinalIgnoreCase), "roaming restore must mark stale warning posture when conflict review is required.");
    Assert(plan.OfflineTruthLabels.Any(item => item.Contains("warning-only", StringComparison.OrdinalIgnoreCase)), "roaming restore must label stale warning posture when conflict review is required.");
    Assert(plan.TravelCompanionSummary.Contains("warning posture", StringComparison.OrdinalIgnoreCase), "roaming restore must mark travel-companion stale warning posture when conflict review is required.");
    Assert(plan.TravelCompanionLabels.Any(item => item.Contains("warning-only", StringComparison.OrdinalIgnoreCase)), "roaming restore must label travel-companion stale warning posture when conflict review is required.");
    Assert(plan.PrefetchLabels.Any(item => item.Contains("Target device", StringComparison.Ordinal)), "roaming restore must expose the targeted prefetch lane");
    Assert(plan.AttentionItems.Count == 3, "roaming restore attention items must include conflict, approval, and install-local guardrails");
    Assert(plan.AttentionItems.Any(item => item.Contains("different channels", StringComparison.Ordinal)), "roaming restore attention items must carry the channel drift conflict");
    Assert(plan.AttentionItems.Any(item => item.Contains("not approved", StringComparison.Ordinal)), "roaming restore attention items must flag non-approved rule posture");
    Assert(plan.AttentionItems.Any(item => item.Contains("install-local", StringComparison.Ordinal)), "roaming restore attention items must keep the install-local guardrail visible");
    Assert(plan.LocalOnlyNotes.Count == 2, "roaming restore must preserve install-local guardrail notes");
    Assert(plan.LocalOnlyNotes.All(note => !note.Contains("secret=", StringComparison.OrdinalIgnoreCase)), "roaming restore must not leak install-local secrets into the roaming packet");
}

static void VerifyCampaignWorkspaceLiteProjectionPreservesObserverAndGmRoleDepth()
{
    var observerProjection = PlayCampaignWorkspaceLiteProjector.Create(
        CreateWorkspaceLiteProjectionResponse(
            sessionId: "session-observer-lite",
            sceneId: "scene-observer",
            sceneRevision: "scene-r5",
            runtimeFingerprint: "runtime-observer",
            role: PlaySurfaceRole.Observer,
            route: "/observe/session-observer-lite",
            timeline: ["Read-only watch resumed", "Lead player confirmed route"],
            capabilities: ["play.session.read"],
            coachHints:
            [
                new PlayCoachHint("coach-observer-continuity", "Stay read-mostly until the owner lane confirms the next revision."),
                new PlayCoachHint("coach-observer-shadow", "Mirror only grounded tactical changes after continuity is confirmed.")
            ],
            quickActions: [],
            bundleTag: "bundle-observer",
            sequence: 6));

    Assert(observerProjection.Role == PlaySurfaceRole.Observer, "observer workspace-lite projection must preserve the observer role");
    Assert(observerProjection.RolePosture.Contains("observer lane", StringComparison.OrdinalIgnoreCase), "observer workspace-lite projection must keep the observer lane posture explicit");
    Assert(observerProjection.RolePosture.Contains("/observe/session-observer-lite", StringComparison.Ordinal), "observer workspace-lite projection must keep the observer owner route visible");
    Assert(observerProjection.SafeNextAction.Contains("Resume the observer lane for scene-observer", StringComparison.Ordinal), "observer workspace-lite projection must keep the observer-specific next safe action");
    Assert(observerProjection.SafeNextAction.Contains("confirm continuity", StringComparison.Ordinal), "observer workspace-lite projection must require continuity confirmation before mirrored updates");
    Assert(observerProjection.RosterSummary.Contains("1 blocker", StringComparison.Ordinal), "observer workspace-lite projection must preserve the read-mostly blocker count");
    Assert(observerProjection.AttentionItems.Any(item => item.Contains("No quick actions", StringComparison.Ordinal)), "observer workspace-lite projection must explain the review-only posture when quick actions are unavailable");
    Assert(observerProjection.AttentionItems.Any(item => item.Contains("read-mostly", StringComparison.OrdinalIgnoreCase)), "observer workspace-lite projection must keep the read-mostly attention item visible");
    Assert(observerProjection.DecisionNoticeHref.Contains("/observe/session-observer-lite", StringComparison.Ordinal), "observer workspace-lite projection must keep the observer lane decision follow-through route");
    Assert(observerProjection.RejoinCommandHref.Contains("/observe/session-observer-lite", StringComparison.Ordinal), "observer workspace-lite projection must keep the observer rejoin command on the observer route.");
    Assert(observerProjection.ContinueCommandHref.Contains("/observe/session-observer-lite", StringComparison.Ordinal), "observer workspace-lite projection must keep the observer continue command on the observer route.");
    Assert(observerProjection.DisconnectRecoveryCopy.Contains("Disconnect recovery:", StringComparison.Ordinal), "observer workspace-lite projection must expose explicit disconnect recovery copy.");
    Assert(observerProjection.RoleChangeRecoveryCopy.Contains("observer lane", StringComparison.OrdinalIgnoreCase), "observer workspace-lite projection must keep role-change recovery copy aligned to observer posture.");
    Assert(observerProjection.ObserverTransitionRecoveryCopy.Contains("remain read-mostly", StringComparison.OrdinalIgnoreCase), "observer workspace-lite projection must keep observer transition recovery copy read-mostly.");
    Assert(observerProjection.RoleFollowThrough.Contains("observer lane", StringComparison.OrdinalIgnoreCase), "observer workspace-lite projection must keep observer-specific role follow-through text");
    Assert(observerProjection.RoleFollowThrough.Contains("read-mostly", StringComparison.OrdinalIgnoreCase), "observer workspace-lite projection must keep the read-mostly follow-through posture");
    Assert(observerProjection.RoleFollowThroughHref.Contains("/observe/session-observer-lite", StringComparison.Ordinal), "observer workspace-lite projection must keep the observer role follow-through href");
    Assert(observerProjection.CampaignReadySummary.Contains("observer lane", StringComparison.OrdinalIgnoreCase), "observer workspace-lite projection must keep observer posture explicit inside campaign-ready proof");
    Assert(observerProjection.QuickActionLabels.Count == 0, "observer workspace-lite projection must not expose quick actions");
    Assert(observerProjection.CampaignMemorySummary.Contains("observer lane", StringComparison.OrdinalIgnoreCase), "observer workspace-lite projection must keep the observer lane explicit inside campaign memory.");
    Assert(observerProjection.CampaignMemoryReturnSummary.Contains("install-local continuity lane", StringComparison.OrdinalIgnoreCase), "observer workspace-lite projection must keep campaign-memory return bounded to the same install-local lane.");
    Assert(observerProjection.ContinuityRailSummary.Contains("observer lane", StringComparison.OrdinalIgnoreCase), "observer workspace-lite projection must keep the observer lane explicit in continuity-rail posture.");
    Assert(observerProjection.PlayerTableCardsSummary.Contains("Player table cards:", StringComparison.Ordinal), "observer workspace-lite projection must expose player table cards as a bounded live-play receipt.");
    Assert(observerProjection.BetweenTurnAffordancesSummary.Contains("Between-turn affordances:", StringComparison.Ordinal), "observer workspace-lite projection must expose a between-turn affordance summary.");
    Assert(observerProjection.GmLiteContinuitySummary.Contains("GM-lite continuity:", StringComparison.Ordinal), "observer workspace-lite projection must expose a GM-lite continuity summary.");
    Assert(observerProjection.GmLiteContinuitySummary.Contains("observer lane", StringComparison.OrdinalIgnoreCase), "observer workspace-lite projection must keep the observer lane explicit inside the GM-lite continuity view.");
    Assert(observerProjection.RunnerGoalUpdatesSummary.Contains("Runner goal updates:", StringComparison.Ordinal), "observer workspace-lite projection must expose a runner-goal update summary.");
    Assert(observerProjection.PlayerSafeConsequenceFeedSummary.Contains("Player-safe consequence feed:", StringComparison.Ordinal), "observer workspace-lite projection must expose a player-safe consequence feed summary.");
    Assert(observerProjection.PlayerSafeConsequenceFeedSummary.Contains("observer lane", StringComparison.OrdinalIgnoreCase), "observer workspace-lite projection must keep the observer lane explicit inside the consequence feed summary.");
    Assert(observerProjection.OfflineTruthSummary.Contains("observer", StringComparison.OrdinalIgnoreCase), "observer workspace-lite projection must keep observer posture explicit in offline-truth summary.");
    Assert(observerProjection.OfflinePrefetchSummary.Contains("observer lane", StringComparison.OrdinalIgnoreCase), "observer workspace-lite projection must keep the observer return lane explicit in offline prefetch");
    Assert(observerProjection.FollowThroughLabels.Any(item => item.Contains("observer lane", StringComparison.OrdinalIgnoreCase)), "observer workspace-lite projection must surface observer-specific follow-through labels");
    Assert(observerProjection.SelectedArtifactView == "campaign", "observer workspace-lite projection must fall back to the campaign artifact shelf view while creator discovery stays bounded.");
    Assert(observerProjection.ArtifactShelfViews.Any(item => item.ViewId == "campaign" && item.IsSelected), "observer workspace-lite projection must keep the campaign artifact shelf view selected while creator discovery stays bounded.");
    Assert(observerProjection.ArtifactShelfViews.Any(item => item.ViewId == "travel" && item.Href.Contains("role=Observer", StringComparison.Ordinal)), "observer workspace-lite projection must keep travel artifact shelf links role-concrete.");
    Assert(observerProjection.CoachHints.SequenceEqual(
        [
            "Stay read-mostly until the owner lane confirms the next revision.",
            "Mirror only grounded tactical changes after continuity is confirmed."
        ]),
        "observer workspace-lite projection must preserve observer-specific coach hints");

    var gmProjection = PlayCampaignWorkspaceLiteProjector.Create(
        CreateWorkspaceLiteProjectionResponse(
            sessionId: "session-gm-lite",
            sceneId: "scene-rigel",
            sceneRevision: "scene-r8",
            runtimeFingerprint: "runtime-gm",
            role: PlaySurfaceRole.GameMaster,
            route: "/gm/session-gm-lite",
            timeline: ["Initiative tracker grounded", "Opposition packet refreshed"],
            capabilities: ["play.gm.actions", "play.spider.cards"],
            coachHints:
            [
                new PlayCoachHint("coach-gm-runboard", "Confirm the grounded scene before advancing the runboard."),
                new PlayCoachHint("coach-gm-spider", "Publish spider cards only after the next safe initiative step is pinned.")
            ],
            quickActions:
            [
                new PlayQuickAction("gm-advance-initiative", "Advance Initiative", "play.gm.actions", true),
                new PlayQuickAction("gm-publish-spider-card", "Publish Spider Card", "play.spider.cards", true)
            ],
            bundleTag: "bundle-gm",
            sequence: 9));

    Assert(gmProjection.Role == PlaySurfaceRole.GameMaster, "gm workspace-lite projection must preserve the gm role");
    Assert(gmProjection.RolePosture.Contains("GM runboard", StringComparison.Ordinal), "gm workspace-lite projection must keep the gm posture explicit");
    Assert(gmProjection.RolePosture.Contains("/gm/session-gm-lite", StringComparison.Ordinal), "gm workspace-lite projection must keep the gm owner route visible");
    Assert(gmProjection.SafeNextAction.Contains("Open the GM shell, confirm scene scene-rigel", StringComparison.Ordinal), "gm workspace-lite projection must keep the gm-specific next safe action");
    Assert(gmProjection.RosterSummary.Contains("GM runboard is clear to continue scene-rigel", StringComparison.Ordinal), "gm workspace-lite projection must keep the gm roster lane unblocked when continuity is aligned");
    Assert(gmProjection.DecisionNoticeHref.Contains("/gm/session-gm-lite", StringComparison.Ordinal), "gm workspace-lite projection must keep the gm decision follow-through route");
    Assert(gmProjection.RejoinCommandHref.Contains("/gm/session-gm-lite", StringComparison.Ordinal), "gm workspace-lite projection must keep the gm rejoin command on the gm route.");
    Assert(gmProjection.ContinueCommandHref.Contains("/gm/session-gm-lite", StringComparison.Ordinal), "gm workspace-lite projection must keep the gm continue command on the gm route.");
    Assert(gmProjection.DisconnectRecoveryCopy.Contains("Disconnect recovery:", StringComparison.Ordinal), "gm workspace-lite projection must expose explicit disconnect recovery copy.");
    Assert(gmProjection.RoleChangeRecoveryCopy.Contains("GM runboard", StringComparison.Ordinal), "gm workspace-lite projection must keep role-change recovery copy aligned to gm posture.");
    Assert(gmProjection.ObserverTransitionRecoveryCopy.Contains("switching into observe mode", StringComparison.OrdinalIgnoreCase), "gm workspace-lite projection must preserve observer transition handoff guidance.");
    Assert(gmProjection.RoleFollowThrough.Contains("GM changes anchored on scene-rigel", StringComparison.Ordinal), "gm workspace-lite projection must keep gm changes anchored on the current scene");
    Assert(gmProjection.RoleFollowThroughHref.Contains("/gm/session-gm-lite", StringComparison.Ordinal), "gm workspace-lite projection must keep the gm role follow-through href");
    Assert(gmProjection.CampaignReadySummary.Contains("GM runboard", StringComparison.Ordinal), "gm workspace-lite projection must keep gm posture explicit inside campaign-ready proof");
    Assert(gmProjection.QuickActionLabels.SequenceEqual(["Advance Initiative", "Publish Spider Card"]), "gm workspace-lite projection must preserve gm quick actions");
    Assert(gmProjection.AttentionItems.SequenceEqual(["No blocking continuity issues are active on this device."]), "gm workspace-lite projection must stay clear when gm continuity is fully aligned");
    Assert(gmProjection.CampaignMemorySummary.Contains("GM runboard", StringComparison.Ordinal), "gm workspace-lite projection must keep the gm lane explicit inside campaign memory.");
    Assert(gmProjection.CampaignMemoryReturnSummary.Contains("Next:", StringComparison.Ordinal), "gm workspace-lite projection must keep the next safe action attached to the campaign-memory return cue.");
    Assert(gmProjection.ContinuityRailSummary.Contains("GM runboard", StringComparison.Ordinal), "gm workspace-lite projection must keep the gm lane explicit in continuity-rail posture.");
    Assert(gmProjection.PlayerTableCardsSummary.Contains("Player table cards:", StringComparison.Ordinal), "gm workspace-lite projection must expose player table-card confidence copy.");
    Assert(gmProjection.PlayerTableCardsSummary.Contains("Advance Initiative", StringComparison.Ordinal), "gm workspace-lite projection must surface the initiative quick action inside table-card confidence copy.");
    Assert(gmProjection.BetweenTurnAffordancesSummary.Contains("Between-turn affordances:", StringComparison.Ordinal), "gm workspace-lite projection must expose between-turn affordances.");
    Assert(gmProjection.GmLiteContinuitySummary.Contains("GM-lite continuity:", StringComparison.Ordinal), "gm workspace-lite projection must expose GM-lite continuity copy.");
    Assert(gmProjection.GmLiteContinuitySummary.Contains("GM runboard", StringComparison.Ordinal), "gm workspace-lite projection must keep the GM runboard explicit inside the GM-lite continuity view.");
    Assert(gmProjection.RunnerGoalUpdatesSummary.Contains("Runner goal updates:", StringComparison.Ordinal), "gm workspace-lite projection must expose runner-goal update copy.");
    Assert(gmProjection.RunnerGoalUpdatesSummary.Contains("GM runboard", StringComparison.Ordinal), "gm workspace-lite projection must keep GM posture explicit inside runner-goal updates.");
    Assert(gmProjection.PlayerSafeConsequenceFeedSummary.Contains("Player-safe consequence feed:", StringComparison.Ordinal), "gm workspace-lite projection must expose player-safe consequence feed copy.");
    Assert(gmProjection.PlayerSafeConsequenceFeedLabels.Any(item => item.Contains("Trust lane:", StringComparison.Ordinal)), "gm workspace-lite projection must keep a trust lane label for the consequence feed view.");
    Assert(gmProjection.OfflineTruthSummary.Contains("GM", StringComparison.Ordinal), "gm workspace-lite projection must keep GM posture explicit in offline-truth summary.");
    Assert(gmProjection.OfflinePrefetchSummary.Contains("GM runboard return lane", StringComparison.Ordinal), "gm workspace-lite projection must keep the gm return lane explicit in offline prefetch");
    Assert(gmProjection.FollowThroughLabels.Any(item => item.Contains("GM changes anchored", StringComparison.Ordinal)), "gm workspace-lite projection must surface gm-specific follow-through labels");
    Assert(gmProjection.SelectedArtifactView == "campaign", "gm workspace-lite projection must default to the campaign artifact shelf view.");
    Assert(gmProjection.ArtifactShelfViews.Any(item => item.ViewId == "campaign" && item.IsSelected), "gm workspace-lite projection must keep the campaign artifact shelf view selected.");
    Assert(gmProjection.ArtifactShelfViews.Any(item => item.ViewId == "travel" && item.Href.Contains("role=GameMaster", StringComparison.Ordinal)), "gm workspace-lite projection must keep travel artifact shelf links role-concrete.");
    Assert(gmProjection.CoachHints.SequenceEqual(
        [
            "Confirm the grounded scene before advancing the runboard.",
            "Publish spider cards only after the next safe initiative step is pinned."
        ]),
        "gm workspace-lite projection must preserve gm-specific coach hints");
}

static void VerifyPlayRoamingRestoreServiceProjectsClaimedDeviceRecovery()
{
    IPlayRoamingRestoreService service = new PlayRoamingRestoreService(new RoamingWorkspaceSyncPlanner());
    PlayResumeResponse response = new(
        SessionId: "session-redmond",
        Role: PlaySurfaceRole.Player,
        DeepLinkOwnerRoute: "/play/session-redmond",
        Bootstrap: new PlayBootstrapResponse(
            "chummer6-mobile",
            new PlaySessionProjection(
                new EngineSessionCursor(
                    new EngineSessionEnvelope("session-redmond", "scene-redmond", "scene-r9", "sr6.preview.v1"),
                    12),
                Timeline: ["Reconnect complete", "Objective board refreshed"],
                GeneratedAtUtc: DateTimeOffset.Parse("2026-03-27T21:00:00+00:00")),
            new PlayShellSnapshot(PlaySurfaceRole.Player, "Player shell", "Table-safe shell", ["play.session.sync"]),
            [new PlayShellSnapshot(PlaySurfaceRole.Player, "Player shell", "Table-safe shell", ["play.session.sync"])],
            new BrowserSessionShellProbe(true, true, true),
            ["play.session.sync"],
            [],
            [new PlayCoachHint("coach-player-sync", "Sync before submitting quick actions after reconnect.")],
            [new PlayQuickAction("player-mark-ready", "Mark Ready", "play.session.sync", true)]),
        Checkpoint: new SyncCheckpoint("session-redmond", "scene-redmond", "scene-r9", "sr6.preview.v1", 12, DateTimeOffset.Parse("2026-03-27T21:00:00+00:00")),
        RuntimeBundle: new PlayRuntimeBundleMetadata("sr6.preview.v1", "scene-r9", "bundle-redmond", DateTimeOffset.Parse("2026-03-27T20:55:00+00:00"), DateTimeOffset.Parse("2026-03-27T20:56:00+00:00")),
        CachePressure: new PlayCachePressureSnapshot(2, 8, false, 0, [], DateTimeOffset.Parse("2026-03-27T21:00:00+00:00")));

    RoamingWorkspaceRestorePlan plan = service.CreatePlan(response, "install-tablet");

    Assert(plan.TargetDeviceId == "install-tablet", "play restore service must preserve the requested claimed device");
    Assert(plan.DeviceRole == "play_tablet", "play restore service must map player role onto the play-tablet restore lane");
    Assert(plan.ResumeSummary.Contains("scene-redmond mobile return", StringComparison.Ordinal), "play restore service must keep the campaign return target visible");
    Assert(plan.RuleEnvironmentSummary == "sr6.preview.v1 · approved · campaign", "play restore service must keep the approved runtime fingerprint visible");
    Assert(plan.PrefetchReadinessSummary.Contains("bounded offline use", StringComparison.Ordinal), "play restore service must make bounded offline prefetch deliberate on the claimed device");
    Assert(plan.LocalCacheBoundarySummary.Contains("install-local", StringComparison.Ordinal), "play restore service must keep install-local cache boundaries explicit");
    Assert(plan.OfflineTruthSummary.Contains("Offline actions:", StringComparison.Ordinal), "play restore service must expose explicit offline-action truth for claimed-device restore.");
    Assert(plan.OfflineTruthLabels.Any(item => item.Contains("Offline action lane:", StringComparison.Ordinal)), "play restore service must expose offline-action labels for claimed-device restore.");
    Assert(plan.TravelCompanionSummary.Contains("Offline actions:", StringComparison.Ordinal), "play restore service must expose explicit offline-action truth for travel companion continuity.");
    Assert(plan.TravelCompanionLabels.Any(item => item.Contains("Offline action lane:", StringComparison.Ordinal)), "play restore service must expose travel companion offline-action labels for claimed-device restore.");
    Assert(plan.TravelCompanionLabels.Any(item => item.Contains("travel_cache", StringComparison.Ordinal)), "play restore service must expose the travel companion lane role inside restore planning.");
    Assert(plan.PrefetchLabels.Any(item => item.Contains("scene-redmond return dossier", StringComparison.Ordinal)), "play restore service must expose the exact dossier staged for offline restore");
    Assert(plan.PrefetchLabels.Any(item => item.Contains("scene-redmond mobile return", StringComparison.Ordinal)), "play restore service must expose the exact campaign staged for offline restore");
    Assert(plan.PrefetchLabels.Any(item => item.Contains("sr6.preview.v1 [approved]", StringComparison.Ordinal)), "play restore service must expose the exact rule posture staged for offline restore");
    Assert(plan.PrefetchLabels.Any(item => item.Contains("bundle-redmond runtime bundle (artifact:session-redmond:bundle)", StringComparison.Ordinal)), "play restore service must expose the exact artifact set staged for offline restore");
    Assert(plan.PrefetchLabels.Any(item => item.Contains("Travel cache", StringComparison.Ordinal)), "play restore service must keep the sibling travel cache visible in restore planning");
    Assert(plan.SafeNextAction.Contains("Open scene-redmond mobile return", StringComparison.Ordinal), "play restore service must point the claimed device at the next safe campaign action");
    Assert(plan.ResumeFollowThroughHref.Contains("/play/session-redmond", StringComparison.Ordinal), "play restore service must expose the direct claimed-device resume href.");
    Assert(plan.ResumeFollowThroughHref.Contains("role=Player", StringComparison.Ordinal), "play restore service resume href must preserve the mapped mobile role.");
    Assert(plan.SupportFollowThroughHref.Contains("sessionId=session-redmond", StringComparison.Ordinal), "play restore service support href must keep the grounded session id.");
    Assert(plan.AttentionItems.Any(item => item.Contains("install-local", StringComparison.Ordinal)), "play restore service must preserve install-local guardrails");
    Assert(plan.LocalOnlyNotes.Any(item => item.Contains("install-local", StringComparison.Ordinal)), "play restore service must expose install-local notes on the shell projection");
    Assert(plan.Campaigns.Count == 1, "play restore service must project at least one campaign return target");
    Assert(plan.Dossiers.Count == 1, "play restore service must project at least one grounded dossier return target");

    RoamingWorkspaceRestorePlan travelPlan = service.CreatePlan(response, "install-play_tablet:travel");

    Assert(travelPlan.TargetDeviceId == "install-play_tablet", "play restore service must normalize travel targets to the canonical primary claimed device");
    Assert(travelPlan.TargetDeviceId.Contains(":travel:travel", StringComparison.Ordinal) is false, "play restore service target ids must never expand travel lineage");
    Assert(travelPlan.TravelCompanionLabels.Any(item => item.Contains("install-play_tablet:travel", StringComparison.Ordinal)), "play restore service must keep the trusted travel companion visible after normalization");
    Assert(travelPlan.TravelCompanionLabels.All(item => !item.Contains(":travel:travel", StringComparison.Ordinal)), "play restore service travel companion labels must never expand travel lineage");
}

static PlayResumeResponse CreateWorkspaceLiteProjectionResponse(
    string sessionId,
    string sceneId,
    string sceneRevision,
    string runtimeFingerprint,
    PlaySurfaceRole role,
    string route,
    IReadOnlyList<string> timeline,
    IReadOnlyList<string> capabilities,
    IReadOnlyList<PlayCoachHint> coachHints,
    IReadOnlyList<PlayQuickAction> quickActions,
    string bundleTag,
    long sequence)
{
    DateTimeOffset generatedAtUtc = DateTimeOffset.Parse("2026-03-28T09:00:00+00:00");
    string shellName = role switch
    {
        PlaySurfaceRole.GameMaster => "GM Shell",
        PlaySurfaceRole.Observer => "Observer Shell",
        _ => "Player shell"
    };
    string shellSummary = role switch
    {
        PlaySurfaceRole.GameMaster => "Runboard-first shell for grounded scene coordination.",
        PlaySurfaceRole.Observer => "Read-mostly shell that mirrors grounded tactical changes.",
        _ => "Table-safe shell"
    };

    return new PlayResumeResponse(
        SessionId: sessionId,
        Role: role,
        DeepLinkOwnerRoute: route,
        Bootstrap: new PlayBootstrapResponse(
            "chummer6-mobile",
            new PlaySessionProjection(
                new EngineSessionCursor(
                    new EngineSessionEnvelope(sessionId, sceneId, sceneRevision, runtimeFingerprint),
                    sequence),
                Timeline: timeline,
                GeneratedAtUtc: generatedAtUtc),
            new PlayShellSnapshot(role, shellName, shellSummary, capabilities),
            [new PlayShellSnapshot(role, shellName, shellSummary, capabilities)],
            new BrowserSessionShellProbe(true, true, true),
            capabilities,
            [],
            coachHints,
            quickActions),
        Checkpoint: new SyncCheckpoint(sessionId, sceneId, sceneRevision, runtimeFingerprint, sequence, generatedAtUtc),
        RuntimeBundle: new PlayRuntimeBundleMetadata(runtimeFingerprint, sceneRevision, bundleTag, generatedAtUtc.AddMinutes(-5), generatedAtUtc.AddMinutes(-4)),
        CachePressure: new PlayCachePressureSnapshot(1, 8, false, 0, [], generatedAtUtc));
}

static WorkspaceRestoreProjection CreateWorkspaceRestoreProjection(IReadOnlyList<string> conflicts, string approvalState = "approved")
{
    RuleEnvironmentRef environment = new(
        EnvironmentId: "ruleenv-preview",
        OwnerScope: "campaign",
        CompatibilityFingerprint: "sr6.preview.v1",
        ApprovalState: approvalState,
        SourcePacks: ["shadowrun-6e-core@current"],
        HouseRulePacks: [],
        OptionToggles: ["campaign_continuity"]);
    ContinuitySnapshotRef continuity = new(
        SnapshotId: "snapshot-1",
        CapturedAtUtc: DateTimeOffset.UtcNow,
        Summary: "Campaign continuity is ready for a second-device restore.",
        RestoreState: "synced",
        SessionId: "session-redmond",
        SceneId: "scene-r7",
        RecapArtifactId: "artifact-recap-1");
    RunnerDossierProjection dossier = new(
        DossierId: "dossier-kestrel",
        RunnerHandle: "kestrel",
        DisplayName: "Kestrel",
        Status: DossierStatuses.Active,
        OwnerUserId: "usr_runner",
        CrewId: "crew-redmond",
        CampaignId: "campaign-redmond",
        CurrentRunId: "run-redmond",
        CurrentSceneId: "scene-r7",
        RuleEnvironment: environment,
        LatestContinuity: continuity,
        BuildReceiptIds: ["receipt-build-1"],
        SnapshotIds: ["snapshot-1"],
        Projections:
        [
            new PublicationSafeProjection(
                ProjectionId: "projection-dossier",
                Kind: "dossier_card",
                Label: "Living dossier",
                Summary: "Runner continuity projection",
                ArtifactId: "artifact-dossier-1")
        ],
        CreatedAtUtc: DateTimeOffset.UtcNow.AddDays(-7),
        UpdatedAtUtc: DateTimeOffset.UtcNow.AddHours(-2));
    CampaignProjection campaign = new(
        CampaignId: "campaign-redmond",
        GroupId: "group-redmond",
        Name: "Redmond Patrol",
        Status: CampaignStatuses.Active,
        Visibility: "shared",
        Summary: "Campaign continuity and roster posture.",
        RuleEnvironment: environment,
        ActiveRunId: "run-redmond",
        CrewIds: ["crew-redmond"],
        DossierIds: ["dossier-kestrel"],
        RunIds: ["run-redmond"],
        LatestContinuity: continuity,
        CreatedAtUtc: DateTimeOffset.UtcNow.AddDays(-7),
        UpdatedAtUtc: DateTimeOffset.UtcNow.AddHours(-2));

    return new WorkspaceRestoreProjection(
        RestoreId: "restore-runner",
        UserId: "usr_runner",
        RecentDossiers: [dossier],
        RecentCampaigns: [campaign],
        RecentRuleEnvironments: [environment],
        RecentArtifacts:
        [
            new RestoreArtifactProjection(
                ArtifactId: "artifact-linux-preview",
                Label: "Linux preview installer",
                Kind: "installer",
                Summary: "Preview installer remains reconnectable on the second device.",
                Channel: "preview",
                Version: "0.7.0-preview")
        ],
        Entitlements:
        [
            new RestoreEntitlementProjection(
                EntitlementId: "grant-preview",
                Label: "Preview workstation access",
                Scope: "desktop",
                Status: "active",
                Summary: "Access token can be refreshed after claim on the second device.")
        ],
        ClaimedDevices:
        [
            new ClaimedDeviceRestoreProjection(
                InstallationId: "install-workstation",
                DeviceRole: "workstation",
                Platform: "linux",
                HeadId: "desktop",
                Channel: "preview",
                HostLabel: "Shadowworkstation",
                RestoreSummary: "linux · desktop · 0.7.0-preview"),
            new ClaimedDeviceRestoreProjection(
                InstallationId: "install-tablet",
                DeviceRole: "play_tablet",
                Platform: "android",
                HeadId: "offline",
                Channel: "preview",
                HostLabel: "Travel tablet",
                RestoreSummary: "android · offline · 0.7.0-preview")
        ],
        ConflictSummaries: conflicts,
        LocalOnlyNotes:
        [
            "Secrets, grant tokens, and runtime caches stay install-local and are never mirrored into the roaming restore packet.",
            "The target device must mint its own local cache and observer continuity token after restore."
        ],
        GeneratedAtUtc: DateTimeOffset.UtcNow);
}

static async Task VerifyLedgerLineageResetAsync()
{
    var store = new BrowserSessionEventLogStore(new InMemoryBrowserKeyValueStore());

    await store.AppendPendingEventsAsync("session-a", "scene-a", "scene-r1", "runtime-a", ["evt-1"], 7);
    var reset = await store.GetOrCreateAsync("session-a", "scene-b", "scene-r2", "runtime-b");

    Assert(reset.PendingEvents.Count == 0, "lineage reset must clear pending events");
    Assert(reset.LastKnownSequence == 0, "lineage reset must restart sequence ownership");
    Assert(reset.SceneId == "scene-b", "lineage reset must adopt request scene id");
    Assert(reset.SceneRevision == "scene-r2", "lineage reset must adopt request scene revision");
    Assert(reset.RuntimeFingerprint == "runtime-b", "lineage reset must adopt request runtime fingerprint");
}

static async Task VerifyBootstrapProjectionPreservesReplayStateAsync()
{
    var store = new BrowserSessionEventLogStore(new InMemoryBrowserKeyValueStore());
    var session = new EngineSessionEnvelope("session-bootstrap", "scene-a", "scene-r1", "runtime-a");
    await store.AppendPendingEventsAsync(
        session.SessionId,
        session.SceneId,
        session.SceneRevision,
        session.RuntimeFingerprint,
        ["evt-1", "evt-2"],
        7
    );

    var ledger = await store.GetOrCreateAsync(
        session.SessionId,
        session.SceneId,
        session.SceneRevision,
        session.RuntimeFingerprint
    );
    var projection = new PlaySessionProjection(
        new EngineSessionCursor(session, ledger.LastKnownSequence),
        ledger.PendingEvents.Count == 0
            ? ["projection ready", "local replay idle"]
            : ["projection ready", .. ledger.PendingEvents.Select(evt => $"pending:{evt}")],
        DateTimeOffset.UtcNow
    );

    Assert(projection.Cursor.AppliedThroughSequence == 7, "bootstrap projection must preserve persisted sequence ownership");
    Assert(projection.Timeline[1] == "pending:evt-1", "bootstrap projection must preserve first pending event");
    Assert(projection.Timeline[2] == "pending:evt-2", "bootstrap projection must preserve second pending event");
}

static async Task VerifyMonotonicSequenceOwnershipAsync()
{
    var store = new BrowserSessionEventLogStore(new InMemoryBrowserKeyValueStore());
    var cache = new BrowserSessionOfflineCacheService(new InMemoryBrowserKeyValueStore());
    var queue = new BrowserSessionOfflineQueueService(store, cache);

    await store.AppendPendingEventsAsync("session-b", "scene-a", "scene-r1", "runtime-a", ["evt-seed"], 10);
    var result = await queue.EnqueueAsync(
        new EngineSessionCursor(new EngineSessionEnvelope("session-b", "scene-a", "scene-r1", "runtime-a"), 2),
        "evt-next"
    );

    Assert(result.AppliedThroughSequence == 11, "enqueue sequence must be monotonic from ledger ownership");
    Assert(result.Ledger.LastKnownSequence == 11, "ledger sequence must persist monotonic result");

    var checkpoint = await cache.GetCheckpointAsync("session-b");
    Assert(checkpoint is not null && checkpoint.AppliedThroughSequence == 11, "checkpoint must track monotonic sequence");
}

static async Task VerifyConcurrentEnqueueSequenceOwnershipAsync()
{
    var store = new BrowserSessionEventLogStore(new InMemoryBrowserKeyValueStore());
    var cache = new BrowserSessionOfflineCacheService(new InMemoryBrowserKeyValueStore());
    var queue = new BrowserSessionOfflineQueueService(store, cache);
    var session = new EngineSessionEnvelope("session-concurrency", "scene-a", "scene-r1", "runtime-a");
    var cursor = new EngineSessionCursor(session, 0);
    var tasks = Enumerable.Range(1, 10)
        .Select(index => queue.EnqueueAsync(cursor, $"evt-{index}"))
        .ToArray();

    var results = await Task.WhenAll(tasks);
    var orderedSequences = results.Select(result => result.AppliedThroughSequence).OrderBy(static sequence => sequence).ToArray();
    for (var i = 0; i < orderedSequences.Length; i++)
    {
        Assert(orderedSequences[i] == i + 1, "concurrent enqueue must assign contiguous unique sequence ownership");
    }

    var ledger = await store.GetExistingAsync(session.SessionId);
    Assert(ledger is not null, "concurrent enqueue must persist a ledger");
    var persistedLedger = ledger!;
    Assert(persistedLedger.PendingEvents.Count == 10, "concurrent enqueue must persist all pending events");
    Assert(persistedLedger.LastKnownSequence == 10, "concurrent enqueue must preserve highest assigned sequence");
}

static async Task VerifySyncPrefixAcknowledgementAsync()
{
    var store = new BrowserSessionEventLogStore(new InMemoryBrowserKeyValueStore());
    var cache = new BrowserSessionOfflineCacheService(new InMemoryBrowserKeyValueStore());
    var queue = new BrowserSessionOfflineQueueService(store, cache);
    var session = new EngineSessionEnvelope("session-d", "scene-a", "scene-r1", "runtime-a");

    await store.AppendPendingEventsAsync(session.SessionId, session.SceneId, session.SceneRevision, session.RuntimeFingerprint, ["evt-1", "evt-2", "evt-3"], 3);
    var result = await queue.SyncReplayAsync(
        new PlaySyncRequest(
            new EngineSessionCursor(session, 3),
            ["evt-1", "evt-x", "evt-3"]
        )
    );

    Assert(result.AcceptedEventCount == 1, "sync acknowledgement must trim only contiguous accepted prefixes");
    Assert(result.Ledger.PendingEvents.Count == 2, "sync acknowledgement must keep non-prefix pending events");
    Assert(result.Ledger.PendingEvents[0] == "evt-2", "sync acknowledgement must preserve first unmatched event");
    Assert(result.Ledger.LastAcceptedEventCount == 1, "sync acknowledgement provenance must persist the exact trimmed prefix count");
    Assert(result.Ledger.LastSyncedAtUtc is not null, "sync acknowledgement provenance must record a sync timestamp");
}

static async Task VerifySyncPreservesNewerLedgerSequenceAsync()
{
    var store = new BrowserSessionEventLogStore(new InMemoryBrowserKeyValueStore());
    var cache = new BrowserSessionOfflineCacheService(new InMemoryBrowserKeyValueStore());
    var queue = new BrowserSessionOfflineQueueService(store, cache);
    var session = new EngineSessionEnvelope("session-sync-sequence", "scene-a", "scene-r1", "runtime-a");

    await store.AppendPendingEventsAsync(session.SessionId, session.SceneId, session.SceneRevision, session.RuntimeFingerprint, ["evt-1"], 5);
    var syncResult = await queue.SyncReplayAsync(
        new PlaySyncRequest(
            new EngineSessionCursor(session, 2),
            ["evt-1"]
        )
    );

    Assert(syncResult.AcceptedEventCount == 1, "sync must acknowledge the accepted pending event");
    Assert(syncResult.AppliedThroughSequence == 5, "sync must preserve the newer stored ledger sequence");

    var checkpoint = await cache.GetCheckpointAsync(session.SessionId);
    Assert(checkpoint is not null && checkpoint.AppliedThroughSequence == 5, "sync checkpoint must not regress below stored ledger sequence");
}

static async Task VerifyOfflineQueueRejectsMalformedPendingEventsAsync()
{
    var store = new BrowserSessionEventLogStore(new InMemoryBrowserKeyValueStore());
    var cache = new BrowserSessionOfflineCacheService(new InMemoryBrowserKeyValueStore());
    var queue = new BrowserSessionOfflineQueueService(store, cache);
    var session = new EngineSessionEnvelope("session-malformed-sync", "scene-a", "scene-r1", "runtime-a");
    var cursor = new EngineSessionCursor(session, 0);

    await AssertThrowsAsync<ArgumentException>(
        () => queue.SyncReplayAsync(new PlaySyncRequest(cursor, null!)),
        "offline queue sync must reject null pending events payloads"
    );

    await AssertThrowsAsync<ArgumentException>(
        () => queue.SyncReplayAsync(new PlaySyncRequest(cursor, ["evt-1", " "])),
        "offline queue sync must reject blank pending events"
    );
}

static async Task VerifyEventLogRejectsMalformedAppendAsync()
{
    var store = new BrowserSessionEventLogStore(new InMemoryBrowserKeyValueStore());

    await AssertThrowsAsync<ArgumentException>(
        () => store.GetOrCreateAsync("session-eventlog-invalid", "", "scene-r1", "runtime-a"),
        "event-log get/create must reject blank scene id"
    );

    await AssertThrowsAsync<ArgumentException>(
        () => store.AppendPendingEventsAsync("session-eventlog-invalid", "scene-a", "scene-r1", "runtime-a", Array.Empty<string>(), 0),
        "event-log append must reject empty pending event payloads"
    );

    await AssertThrowsAsync<ArgumentException>(
        () => store.AppendPendingEventsAsync("session-eventlog-invalid", "scene-a", "scene-r1", "runtime-a", ["evt-1", " "], 0),
        "event-log append must reject blank pending events"
    );

    await AssertThrowsAsync<ArgumentOutOfRangeException>(
        () => store.AppendPendingEventsAsync("session-eventlog-invalid", "scene-a", "scene-r1", "runtime-a", ["evt-1"], -1),
        "event-log append must reject negative sequence ownership"
    );
}

static async Task VerifyEventLogRejectsSequenceRegressionAsync()
{
    var store = new BrowserSessionEventLogStore(new InMemoryBrowserKeyValueStore());

    await store.AppendPendingEventsAsync(
        "session-eventlog-sequence-regression",
        "scene-a",
        "scene-r1",
        "runtime-a",
        ["evt-1"],
        4
    );

    await AssertThrowsAsync<InvalidOperationException>(
        () => store.AppendPendingEventsAsync(
            "session-eventlog-sequence-regression",
            "scene-a",
            "scene-r1",
            "runtime-a",
            ["evt-2"],
            3
        ),
        "event-log append must reject regressing sequence ownership for direct callers"
    );
}

static async Task VerifyEventLogPersistsAcrossServiceInstancesAsync()
{
    var browserStore = new InMemoryBrowserKeyValueStore();
    var writer = new BrowserSessionEventLogStore(browserStore);
    var reader = new BrowserSessionEventLogStore(browserStore);
    const string sessionId = "session-eventlog-persist";

    await writer.AppendPendingEventsAsync(
        sessionId,
        "scene-a",
        "scene-r1",
        "runtime-a",
        ["evt-1", "evt-2"],
        2
    );

    var persisted = await reader.GetExistingAsync(sessionId);
    Assert(persisted is not null, "event-log must persist ledger entries in browser storage");
    Assert(persisted!.PendingEvents.Count == 2, "event-log persistence must keep pending events across service instances");
    Assert(persisted.LastKnownSequence == 2, "event-log persistence must keep sequence ownership across service instances");
}

static async Task VerifyAppBrowserStatePersistsAcrossRebuildAsync()
{
    const string sessionId = "session-app-rebuild-persist";
    string? previousRoot = Environment.GetEnvironmentVariable("CHUMMER_PLAY_BROWSER_STATE_DIR");
    string? previousIsolation = Environment.GetEnvironmentVariable("CHUMMER_PLAY_BROWSER_STATE_ISOLATE_PER_APP");
    string browserStateRoot = Path.Combine(Path.GetTempPath(), "chummer-play-app-browser-state", Guid.NewGuid().ToString("N"));
    Environment.SetEnvironmentVariable("CHUMMER_PLAY_BROWSER_STATE_DIR", browserStateRoot);
    Environment.SetEnvironmentVariable("CHUMMER_PLAY_BROWSER_STATE_ISOLATE_PER_APP", "false");

    try
    {
        var firstApp = PlayWebApplication.Build([]);
        try
        {
            var firstService = firstApp.Services.GetRequiredService<PlayTurnCompanionService>();
            var firstProjection = await firstService.AdjustMetricAsync(sessionId, PlaySurfaceRole.Player, "ammo", -2);
            Assert(
                firstProjection.Now.StatCards.First(item => item.MetricId == "ammo").Value == 10,
                "first app instance must persist the updated turn companion ammo counter");

            var replayResponse = await firstService.ReplayClientQueueAsync(
                sessionId,
                PlaySurfaceRole.Player,
                ["quick-action:player-mark-ready"]);
            Assert(replayResponse.Accepted, "first app instance must accept replaying a bounded quick action");
            Assert(replayResponse.PendingQueueCount == 1, "first app instance must persist one queued replay-safe event");
        }
        finally
        {
            await firstApp.DisposeAsync();
        }

        var secondApp = PlayWebApplication.Build([]);
        try
        {
            var secondService = secondApp.Services.GetRequiredService<PlayTurnCompanionService>();
            var secondProjection = await secondService.GetProjectionAsync(sessionId, PlaySurfaceRole.Player);
            Assert(
                secondProjection.Now.StatCards.First(item => item.MetricId == "ammo").Value == 10,
                "rebuilt app instance must keep persisted turn companion counters from browser-state storage");

            var queueStatus = await secondService.GetQueueStatusAsync(sessionId, PlaySurfaceRole.Player);
            Assert(queueStatus.PendingQueueCount == 1, "rebuilt app instance must keep the queued replay-safe event count");
            Assert(queueStatus.Sync.CanAcknowledgeServerQueue, "rebuilt app instance must still expose queue acknowledgement for persisted pending events");
        }
        finally
        {
            await secondApp.DisposeAsync();
        }
    }
    finally
    {
        Environment.SetEnvironmentVariable("CHUMMER_PLAY_BROWSER_STATE_DIR", previousRoot);
        Environment.SetEnvironmentVariable("CHUMMER_PLAY_BROWSER_STATE_ISOLATE_PER_APP", previousIsolation);
        if (Directory.Exists(browserStateRoot))
        {
            Directory.Delete(browserStateRoot, recursive: true);
        }
    }
}

static async Task VerifyEventLogDropsMalformedStoredLedgerAsync()
{
    var browserStore = new InMemoryBrowserKeyValueStore();
    var store = new BrowserSessionEventLogStore(browserStore);
    const string sessionId = "session-eventlog-malformed-read";
    var key = PlayBrowserStateKeys.Ledger(sessionId);

    await browserStore.SetAsync(
        key,
        new OfflineLedgerEnvelope(
            sessionId,
            "scene-a",
            "scene-r1",
            "runtime-a",
            ["evt-1"],
            -1,
            DateTimeOffset.UtcNow,
            null,
            0
        )
    );

    var existing = await store.GetExistingAsync(sessionId);
    Assert(existing is null, "event-log should drop malformed stored ledger snapshots on read");

    var keys = await browserStore.ListKeysAsync("play:ledger:");
    Assert(!keys.Contains(key, StringComparer.Ordinal), "event-log should remove malformed ledger keys from browser storage");
}

static async Task VerifyEventLogDropsUnparseableStoredLedgerKeysAsync()
{
    var browserStore = new InMemoryBrowserKeyValueStore();
    var store = new BrowserSessionEventLogStore(browserStore);
    const string sessionId = "session-eventlog-unparseable-read";
    var key = PlayBrowserStateKeys.Ledger(sessionId);

    await browserStore.SetAsync<object>(key, "tampered-ledger-value");

    var existing = await store.GetExistingAsync(sessionId);
    Assert(existing is null, "event-log should treat unparseable stored ledger snapshots as missing");

    var keys = await browserStore.ListKeysAsync("play:ledger:");
    Assert(!keys.Contains(key, StringComparer.Ordinal), "event-log should remove unparseable ledger keys from browser storage");
}

static async Task VerifyOfflineQueueRejectsNegativeSequenceAsync()
{
    var store = new BrowserSessionEventLogStore(new InMemoryBrowserKeyValueStore());
    var cache = new BrowserSessionOfflineCacheService(new InMemoryBrowserKeyValueStore());
    var queue = new BrowserSessionOfflineQueueService(store, cache);
    var session = new EngineSessionEnvelope("session-negative-sequence", "scene-a", "scene-r1", "runtime-a");
    var negativeCursor = new EngineSessionCursor(session, -1);

    await AssertThrowsAsync<ArgumentOutOfRangeException>(
        () => queue.EnqueueAsync(negativeCursor, "evt-1"),
        "offline queue enqueue must reject negative applied-through sequence values"
    );

    await AssertThrowsAsync<ArgumentOutOfRangeException>(
        () => queue.SyncReplayAsync(new PlaySyncRequest(negativeCursor, ["evt-1"])),
        "offline queue sync must reject negative applied-through sequence values"
    );
}

static async Task VerifyOfflineQueueRejectsMalformedSessionEnvelopeAsync()
{
    var store = new BrowserSessionEventLogStore(new InMemoryBrowserKeyValueStore());
    var cache = new BrowserSessionOfflineCacheService(new InMemoryBrowserKeyValueStore());
    var queue = new BrowserSessionOfflineQueueService(store, cache);

    var missingSceneId = new EngineSessionCursor(
        new EngineSessionEnvelope("session-envelope-invalid", "", "scene-r1", "runtime-a"),
        0
    );
    await AssertThrowsAsync<ArgumentException>(
        () => queue.EnqueueAsync(missingSceneId, "evt-1"),
        "offline queue enqueue must reject blank scene id in direct callers"
    );

    var missingRuntime = new EngineSessionCursor(
        new EngineSessionEnvelope("session-envelope-invalid", "scene-a", "scene-r1", ""),
        0
    );
    await AssertThrowsAsync<ArgumentException>(
        () => queue.SyncReplayAsync(new PlaySyncRequest(missingRuntime, ["evt-1"])),
        "offline queue sync must reject blank runtime fingerprint in direct callers"
    );
}

static async Task VerifyOfflineCacheRejectsMalformedCheckpointAndRuntimeEntryAsync()
{
    var cache = new BrowserSessionOfflineCacheService(new InMemoryBrowserKeyValueStore());

    await AssertThrowsAsync<ArgumentException>(
        () => cache.SetCheckpointAsync(
            new SyncCheckpoint("session-cache-invalid", "scene-a", "scene-r1", "", 0, DateTimeOffset.UtcNow)
        ),
        "offline cache must reject checkpoints with blank runtime fingerprint in direct callers"
    );

    await AssertThrowsAsync<ArgumentOutOfRangeException>(
        () => cache.SetCheckpointAsync(
            new SyncCheckpoint("session-cache-invalid", "scene-a", "scene-r1", "runtime-a", -1, DateTimeOffset.UtcNow)
        ),
        "offline cache must reject checkpoints with negative sequence ownership in direct callers"
    );

    await AssertThrowsAsync<ArgumentException>(
        () => cache.CacheRuntimeBundleAsync(
            new RuntimeBundleCacheEntry(
                "session-cache-invalid",
                "runtime-a",
                "scene-r1",
                "",
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow
            )
        ),
        "offline cache must reject runtime bundle metadata with blank bundle tags in direct callers"
    );
}

static async Task VerifyOfflineCacheDropsMalformedStoredEntriesAsync()
{
    var browserStore = new InMemoryBrowserKeyValueStore();
    var cache = new BrowserSessionOfflineCacheService(browserStore);
    const string sessionId = "session-cache-malformed-read";
    var checkpointKey = PlayBrowserStateKeys.Checkpoint(sessionId);
    var runtimeBundleKey = PlayBrowserStateKeys.RuntimeBundle(sessionId);

    await browserStore.SetAsync(
        checkpointKey,
        new SyncCheckpoint(sessionId, "scene-a", "scene-r1", "runtime-a", -1, DateTimeOffset.UtcNow)
    );

    await browserStore.SetAsync(
        runtimeBundleKey,
        new RuntimeBundleCacheEntry(
            sessionId,
            "runtime-a",
            "scene-r1",
            "",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow
        )
    );

    var checkpoint = await cache.GetCheckpointAsync(sessionId);
    Assert(checkpoint is null, "offline cache should drop malformed checkpoints on read");
    var runtimeBundle = await cache.GetRuntimeBundleAsync(sessionId);
    Assert(runtimeBundle is null, "offline cache should drop malformed runtime bundle metadata on read");

    var keys = await browserStore.ListKeysAsync("play:");
    Assert(!keys.Contains(checkpointKey, StringComparer.Ordinal), "offline cache should remove malformed checkpoint keys");
    Assert(!keys.Contains(runtimeBundleKey, StringComparer.Ordinal), "offline cache should remove malformed runtime bundle keys");
}

static async Task VerifyOfflineCacheDropsUnparseableStoredEntriesAsync()
{
    var browserStore = new InMemoryBrowserKeyValueStore();
    var cache = new BrowserSessionOfflineCacheService(browserStore);
    const string sessionId = "session-cache-unparseable-read";
    var checkpointKey = PlayBrowserStateKeys.Checkpoint(sessionId);
    var runtimeBundleKey = PlayBrowserStateKeys.RuntimeBundle(sessionId);

    await browserStore.SetAsync<object>(checkpointKey, "tampered-checkpoint-value");
    await browserStore.SetAsync<object>(runtimeBundleKey, 42);

    var checkpoint = await cache.GetCheckpointAsync(sessionId);
    Assert(checkpoint is null, "offline cache should treat unparseable checkpoints as missing");

    var runtimeBundle = await cache.GetRuntimeBundleAsync(sessionId);
    Assert(runtimeBundle is null, "offline cache should treat unparseable runtime bundle metadata as missing");

    var keys = await browserStore.ListKeysAsync("play:");
    Assert(!keys.Contains(checkpointKey, StringComparer.Ordinal), "offline cache should remove unparseable checkpoint keys");
    Assert(!keys.Contains(runtimeBundleKey, StringComparer.Ordinal), "offline cache should remove unparseable runtime bundle keys");
}

static async Task VerifyOfflineCacheRuntimeBundleQuotaEvictionAsync()
{
    var cache = new BrowserSessionOfflineCacheService(new InMemoryBrowserKeyValueStore());
    var baseTime = DateTimeOffset.UtcNow.AddMinutes(-30);

    for (var i = 1; i <= 9; i++)
    {
        await cache.CacheRuntimeBundleAsync(
            new RuntimeBundleCacheEntry(
                $"session-cache-{i}",
                $"runtime-{i}",
                $"scene-r{i}",
                $"bundle-{i}",
                baseTime.AddMinutes(i),
                baseTime.AddMinutes(i)
            )
        );
    }

    var evicted = await cache.GetRuntimeBundleAsync("session-cache-1");
    Assert(evicted is null, "offline cache must evict the oldest runtime bundle entry once quota is exceeded");

    var retained = await cache.GetRuntimeBundleAsync("session-cache-9");
    Assert(retained is not null, "offline cache must retain the newest runtime bundle entry after eviction");

    var pressure = await cache.GetCachePressureAsync();
    Assert(pressure.RuntimeBundleCount == 8, "offline cache pressure must report bounded runtime bundle count");
    Assert(pressure.BackpressureActive, "offline cache pressure must report near-quota state at runtime bundle limit");
}

static async Task VerifyOfflineCacheReadsDoNotMutateQuotaEvictionAsync()
{
    var cache = new BrowserSessionOfflineCacheService(new InMemoryBrowserKeyValueStore());
    var baseTime = DateTimeOffset.UtcNow.AddMinutes(-30);

    for (var i = 1; i <= 8; i++)
    {
        await cache.CacheRuntimeBundleAsync(
            new RuntimeBundleCacheEntry(
                $"session-cache-touch-{i}",
                $"runtime-touch-{i}",
                $"scene-touch-r{i}",
                $"bundle-touch-{i}",
                baseTime.AddMinutes(i),
                baseTime.AddMinutes(i)
            )
        );
    }

    var touched = await cache.GetRuntimeBundleAsync("session-cache-touch-1");
    Assert(touched is not null, "offline cache read-touch test requires a readable runtime bundle");

    await cache.CacheRuntimeBundleAsync(
        new RuntimeBundleCacheEntry(
            "session-cache-touch-9",
            "runtime-touch-9",
            "scene-touch-r9",
            "bundle-touch-9",
            baseTime.AddMinutes(9),
            baseTime.AddMinutes(9)
        )
    );

    var touchedAfterEviction = await cache.GetRuntimeBundleAsync("session-cache-touch-1");
    Assert(touchedAfterEviction is null, "runtime-bundle reads must not rewrite cache order or shield the oldest entry from quota eviction");

    var nextOldest = await cache.GetRuntimeBundleAsync("session-cache-touch-2");
    Assert(nextOldest is not null, "quota eviction must preserve newer entries when reads do not mutate stored runtime metadata");
}

static async Task VerifyOfflineCacheReadDoesNotRewriteStoredMetadataAsync()
{
    var browserStore = new InMemoryBrowserKeyValueStore();
    var cache = new BrowserSessionOfflineCacheService(browserStore);
    const string sessionId = "session-cache-touch-no-write";
    var baseTime = DateTimeOffset.UtcNow.AddMinutes(-5);
    var key = PlayBrowserStateKeys.RuntimeBundle(sessionId);
    var initialTimestamp = baseTime;

    await cache.CacheRuntimeBundleAsync(
        new RuntimeBundleCacheEntry(
            sessionId,
            "runtime-initial",
            "scene-r1",
            "bundle-initial",
            initialTimestamp,
            initialTimestamp
        )
    );

    var readResult = await cache.GetRuntimeBundleAsync(sessionId);
    Assert(readResult is not null, "runtime bundle read regression requires a readable entry");
    Assert(readResult.LastValidatedAtUtc >= initialTimestamp, "runtime bundle reads should still surface a fresh validation timestamp to callers");

    var storedEntry = await browserStore.GetAsync<RuntimeBundleCacheEntry>(key);
    Assert(storedEntry is not null, "runtime bundle entry must remain stored after read");
    Assert(storedEntry.RuntimeFingerprint == "runtime-initial", "runtime bundle reads must not rewrite stored runtime fingerprint metadata");
    Assert(storedEntry.SceneRevision == "scene-r1", "runtime bundle reads must not rewrite stored scene revision metadata");
    Assert(storedEntry.BundleTag == "bundle-initial", "runtime bundle reads must not rewrite stored bundle tag metadata");
    Assert(storedEntry.LastValidatedAtUtc == initialTimestamp, "runtime bundle reads must not persist a write-on-read timestamp update");
}

static async Task VerifyOfflineCacheConcurrentReadAndWritePreserveNewestSameSessionMetadataAsync()
{
    const string sessionId = "session-cache-touch-same-session";
    var key = PlayBrowserStateKeys.RuntimeBundle(sessionId);
    var gate = new GateableGetBrowserStore(key);
    var cache = new BrowserSessionOfflineCacheService(gate);
    var initialTimestamp = DateTimeOffset.UtcNow.AddMinutes(-10);

    await cache.CacheRuntimeBundleAsync(
        new RuntimeBundleCacheEntry(
            sessionId,
            "runtime-initial",
            "scene-r1",
            "bundle-initial",
            initialTimestamp,
            initialTimestamp
        )
    );
    gate.EnableGate();
    TraceCheckBoundary("STEP", $"{nameof(VerifyOfflineCacheConcurrentReadAndWritePreserveNewestSameSessionMetadataAsync)} seeded initial entry");

    var readTask = Task.Run(() => cache.GetRuntimeBundleAsync(sessionId));
    TraceCheckBoundary("STEP", $"{nameof(VerifyOfflineCacheConcurrentReadAndWritePreserveNewestSameSessionMetadataAsync)} waiting for gated read");
    await gate.WaitForGetAsync();
    TraceCheckBoundary("STEP", $"{nameof(VerifyOfflineCacheConcurrentReadAndWritePreserveNewestSameSessionMetadataAsync)} gated read started");

    var updatedTimestamp = initialTimestamp.AddMinutes(5);
    var writeTask = Task.Run(() => cache.CacheRuntimeBundleAsync(
        new RuntimeBundleCacheEntry(
            sessionId,
            "runtime-updated",
            "scene-r2",
            "bundle-updated",
            updatedTimestamp,
            updatedTimestamp
        )
    ));
    TraceCheckBoundary("STEP", $"{nameof(VerifyOfflineCacheConcurrentReadAndWritePreserveNewestSameSessionMetadataAsync)} queued concurrent write");

    gate.ReleaseGet();
    TraceCheckBoundary("STEP", $"{nameof(VerifyOfflineCacheConcurrentReadAndWritePreserveNewestSameSessionMetadataAsync)} released gated read");
    var readResult = await readTask;
    TraceCheckBoundary("STEP", $"{nameof(VerifyOfflineCacheConcurrentReadAndWritePreserveNewestSameSessionMetadataAsync)} read completed");
    await writeTask;
    TraceCheckBoundary("STEP", $"{nameof(VerifyOfflineCacheConcurrentReadAndWritePreserveNewestSameSessionMetadataAsync)} write completed");

    Assert(readResult is not null, "same-session interleaving regression requires the read side to complete");
    var storedEntry = await gate.GetAsync<RuntimeBundleCacheEntry>(key);
    Assert(storedEntry is not null, "same-session interleaving regression requires the runtime bundle entry to remain stored");
    Assert(storedEntry.RuntimeFingerprint == "runtime-updated", "same-session read/write interleaving must preserve the newest runtime fingerprint");
    Assert(storedEntry.SceneRevision == "scene-r2", "same-session read/write interleaving must preserve the newest scene revision");
    Assert(storedEntry.BundleTag == "bundle-updated", "same-session read/write interleaving must preserve the newest bundle tag");
    Assert(storedEntry.CachedAtUtc == updatedTimestamp, "same-session read/write interleaving must preserve the newest cache timestamp");
    Assert(storedEntry.LastValidatedAtUtc == updatedTimestamp, "same-session read/write interleaving must not let the read path clobber stored validation time");
}

static async Task VerifyOfflineCacheQuotaIgnoresUnparseableRuntimeBundleKeysAsync()
{
    var browserStore = new InMemoryBrowserKeyValueStore();
    var cache = new BrowserSessionOfflineCacheService(browserStore);
    var baseTime = DateTimeOffset.UtcNow.AddMinutes(-60);

    var malformedKeyA = PlayBrowserStateKeys.RuntimeBundle("session-cache-malformed-a");
    var malformedKeyB = PlayBrowserStateKeys.RuntimeBundle("session-cache-malformed-b");
    await browserStore.SetAsync<object>(malformedKeyA, "tampered-runtime-a");
    await browserStore.SetAsync<object>(malformedKeyB, "tampered-runtime-b");

    for (var i = 1; i <= 8; i++)
    {
        await cache.CacheRuntimeBundleAsync(
            new RuntimeBundleCacheEntry(
                $"session-cache-clean-{i}",
                $"runtime-clean-{i}",
                $"scene-clean-r{i}",
                $"bundle-clean-{i}",
                baseTime.AddMinutes(i),
                baseTime.AddMinutes(i)
            )
        );
    }

    var oldestValid = await cache.GetRuntimeBundleAsync("session-cache-clean-1");
    Assert(oldestValid is not null, "quota should not evict valid runtime bundles because of unparseable key residue");

    var keys = await browserStore.ListKeysAsync(PlayBrowserStateKeys.RuntimeBundlePrefix);
    Assert(keys.Count == 8, "runtime-bundle keyspace should contain only bounded valid entries after pruning unparseable keys");
    Assert(!keys.Contains(malformedKeyA, StringComparer.Ordinal), "runtime-bundle cache should prune unparseable keys before quota accounting");
    Assert(!keys.Contains(malformedKeyB, StringComparer.Ordinal), "runtime-bundle cache should prune all unparseable keys before quota accounting");
}

static async Task VerifyOfflineCacheConcurrentCrossSessionQuotaWritesStayBoundedAsync()
{
    var browserStore = new DelayedMutationBrowserStore(TimeSpan.FromMilliseconds(25));
    var cache = new BrowserSessionOfflineCacheService(browserStore);
    var baseTime = DateTimeOffset.UtcNow.AddMinutes(-90);
    var releaseWriters = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var readyCount = 0;

    for (var i = 1; i <= 8; i++)
    {
        await cache.CacheRuntimeBundleAsync(
            new RuntimeBundleCacheEntry(
                $"session-cache-concurrent-{i}",
                $"runtime-concurrent-{i}",
                $"scene-concurrent-r{i}",
                $"bundle-concurrent-{i}",
                baseTime.AddMinutes(i),
                baseTime.AddMinutes(i)
            )
        );
    }

    Task writeNine = Task.Run(async () =>
    {
        if (Interlocked.Increment(ref readyCount) == 2)
        {
            releaseWriters.TrySetResult();
        }

        await releaseWriters.Task;
        await cache.CacheRuntimeBundleAsync(
            new RuntimeBundleCacheEntry(
                "session-cache-concurrent-9",
                "runtime-concurrent-9",
                "scene-concurrent-r9",
                "bundle-concurrent-9",
                baseTime.AddMinutes(9),
                baseTime.AddMinutes(9)
            )
        );
    });

    Task writeTen = Task.Run(async () =>
    {
        if (Interlocked.Increment(ref readyCount) == 2)
        {
            releaseWriters.TrySetResult();
        }

        await releaseWriters.Task;
        await cache.CacheRuntimeBundleAsync(
            new RuntimeBundleCacheEntry(
                "session-cache-concurrent-10",
                "runtime-concurrent-10",
                "scene-concurrent-r10",
                "bundle-concurrent-10",
                baseTime.AddMinutes(10),
                baseTime.AddMinutes(10)
            )
        );
    });

    await Task.WhenAll(writeNine, writeTen);

    var keys = await browserStore.ListKeysAsync(PlayBrowserStateKeys.RuntimeBundlePrefix);
    Assert(keys.Count == 8, "concurrent cross-session quota writes must preserve the global runtime-bundle quota");
    Assert(await cache.GetRuntimeBundleAsync("session-cache-concurrent-1") is null, "first oldest runtime bundle must be evicted under concurrent quota pressure");
    Assert(await cache.GetRuntimeBundleAsync("session-cache-concurrent-2") is null, "second oldest runtime bundle must be evicted under concurrent quota pressure");
    Assert(await cache.GetRuntimeBundleAsync("session-cache-concurrent-9") is not null, "first concurrent write must survive quota reconciliation");
    Assert(await cache.GetRuntimeBundleAsync("session-cache-concurrent-10") is not null, "second concurrent write must survive quota reconciliation");
}

static async Task VerifyIndexShellAccessibilityContractAsync()
{
    var indexHtmlPath = Path.Combine(GetRepoRoot(), "src", "Chummer.Play.Web", "wwwroot", "index.html");
    var html = await File.ReadAllTextAsync(indexHtmlPath);

    Assert(html.Contains("<html lang=\"en\">", StringComparison.Ordinal), "play shell must declare an explicit document language");
    Assert(html.Contains("<main>", StringComparison.Ordinal), "play shell must expose a main landmark");
    Assert(html.Contains("<h1>Chummer Play</h1>", StringComparison.Ordinal), "play shell must expose a top-level heading");
    Assert(html.Contains("id=\"shell-headline\"", StringComparison.Ordinal), "play shell must expose an authored hero headline.");
    Assert(html.Contains("id=\"shell-subhead\"", StringComparison.Ordinal), "play shell must expose an authored hero subhead.");
    Assert(html.Contains("id=\"shell-role-chip\"", StringComparison.Ordinal), "play shell must expose a live role chip.");
    Assert(html.Contains("id=\"shell-session-chip\"", StringComparison.Ordinal), "play shell must expose a live session chip.");
    Assert(html.Contains("id=\"shell-network-chip\"", StringComparison.Ordinal), "play shell must expose a live network chip.");
    Assert(html.Contains("id=\"shell-install-chip\"", StringComparison.Ordinal), "play shell must expose a live install-status chip.");
    Assert(html.Contains("id=\"shell-primary-action-link\"", StringComparison.Ordinal), "play shell must expose a hero primary action.");
    Assert(html.Contains("id=\"shell-secondary-action-link\"", StringComparison.Ordinal), "play shell must expose a hero secondary action.");
    Assert(html.Contains("id=\"shell-install-button\"", StringComparison.Ordinal), "play shell must expose an install action.");
    Assert(html.Contains("id=\"shell-continuity-status\"", StringComparison.Ordinal), "play shell must expose continuity confidence in the hero shell.");
    Assert(html.Contains("id=\"shell-restore-status\"", StringComparison.Ordinal), "play shell must expose restore readiness in the hero shell.");
    Assert(html.Contains("id=\"shell-install-status\"", StringComparison.Ordinal), "play shell must expose installable posture in the hero shell.");
    Assert(html.Contains("id=\"shell-service-worker-detail\"", StringComparison.Ordinal), "play shell must expose service-worker cache-control posture in the hero shell.");
    Assert(html.Contains("id=\"shell-role-focus-heading\"", StringComparison.Ordinal), "play shell must expose authored role-focus copy.");
    Assert(html.Contains("id=\"shell-reconnect-copy\"", StringComparison.Ordinal), "play shell must expose reconnect confidence copy.");
    Assert(html.Contains("id=\"shell-continuity-safety\"", StringComparison.Ordinal), "play shell must expose claimed-device continuity safety copy in the hero shell.");
    Assert(html.Contains("id=\"shell-network-state-list\"", StringComparison.Ordinal), "play shell must expose a live network-state list for install and reconnect trust.");
    Assert(html.Contains("id=\"shell-offline-copy\"", StringComparison.Ordinal), "play shell must expose offline boundary copy.");
    Assert(html.Contains("id=\"output\" role=\"status\" aria-live=\"polite\" aria-atomic=\"true\"", StringComparison.Ordinal), "play shell resume status region must expose polite live updates");
    Assert(html.Contains("id=\"workspace-summary\"", StringComparison.Ordinal), "play shell must expose a workspace-lite summary region");
    Assert(html.Contains("id=\"entry-state-summary\"", StringComparison.Ordinal), "play shell must expose an onboarding/recovery entry-state summary.");
    Assert(html.Contains("id=\"entry-recommended-link\"", StringComparison.Ordinal), "play shell must expose a one-tap recommended onboarding/recovery action.");
    Assert(html.Contains("id=\"entry-retry-link\"", StringComparison.Ordinal), "play shell must expose a retry action for onboarding/recovery.");
    Assert(html.Contains("id=\"entry-cancel-link\"", StringComparison.Ordinal), "play shell must expose a cancel action for onboarding/recovery.");
    Assert(html.Contains("id=\"entry-restore-link\"", StringComparison.Ordinal), "play shell must expose a restore action for onboarding/recovery.");
    Assert(html.Contains("id=\"entry-recovery-actions\"", StringComparison.Ordinal), "play shell must expose onboarding/recovery guidance labels.");
    Assert(html.Contains("id=\"critical-rejoin\"", StringComparison.Ordinal), "play shell must expose a dedicated rejoin command surface.");
    Assert(html.Contains("id=\"critical-rejoin-link\"", StringComparison.Ordinal), "play shell must expose a direct rejoin command link.");
    Assert(html.Contains("id=\"critical-continue\"", StringComparison.Ordinal), "play shell must expose a dedicated continue command surface.");
    Assert(html.Contains("id=\"critical-continue-link\"", StringComparison.Ordinal), "play shell must expose a direct continue command link.");
    Assert(html.Contains("id=\"critical-support\"", StringComparison.Ordinal), "play shell must expose a dedicated support command surface.");
    Assert(html.Contains("id=\"critical-support-link\"", StringComparison.Ordinal), "play shell must expose a direct support command link.");
    Assert(html.Contains("id=\"critical-decision-receipt-summary\"", StringComparison.Ordinal), "play shell must expose a dedicated decision-receipt summary region for long-running shell actions.");
    Assert(html.Contains("id=\"critical-decision-receipts\"", StringComparison.Ordinal), "play shell must expose dedicated decision receipts for rejoin, quick actions, and resume.");
    Assert(html.Contains("id=\"critical-low-noise-guidance\"", StringComparison.Ordinal), "play shell must expose low-noise guidance for critical command routes.");
    Assert(html.Contains("id=\"recover-disconnect\"", StringComparison.Ordinal), "play shell must expose explicit disconnect recovery copy.");
    Assert(html.Contains("id=\"recover-role-change\"", StringComparison.Ordinal), "play shell must expose explicit role-change recovery copy.");
    Assert(html.Contains("id=\"recover-observer-transition\"", StringComparison.Ordinal), "play shell must expose explicit observer-transition recovery copy.");
    Assert(html.Contains("id=\"workspace-player-table-cards\"", StringComparison.Ordinal), "play shell must expose a player table-card summary region.");
    Assert(html.Contains("id=\"workspace-player-table-cards-list\"", StringComparison.Ordinal), "play shell must expose player table-card labels.");
    Assert(html.Contains("id=\"workspace-between-turn-affordances\"", StringComparison.Ordinal), "play shell must expose a between-turn affordance summary region.");
    Assert(html.Contains("id=\"workspace-between-turn-affordances-list\"", StringComparison.Ordinal), "play shell must expose between-turn affordance labels.");
    Assert(html.Contains("id=\"workspace-gm-lite-continuity\"", StringComparison.Ordinal), "play shell must expose a GM-lite continuity summary region.");
    Assert(html.Contains("id=\"workspace-gm-lite-continuity-list\"", StringComparison.Ordinal), "play shell must expose GM-lite continuity labels.");
    Assert(html.Contains("id=\"workspace-role\"", StringComparison.Ordinal), "play shell must expose role posture alongside current state");
    Assert(html.Contains("id=\"change-packet-summary\"", StringComparison.Ordinal), "play shell must expose a change-packet summary alongside current state");
    Assert(html.Contains("id=\"workspace-quick-explain\"", StringComparison.Ordinal), "play shell must expose a quick-explain summary region.");
    Assert(html.Contains("id=\"workspace-quick-explain-list\"", StringComparison.Ordinal), "play shell must expose quick-explain labels.");
    Assert(html.Contains("id=\"workspace-source-anchor\"", StringComparison.Ordinal), "play shell must expose source-anchor context.");
    Assert(html.Contains("id=\"workspace-source-anchor-list\"", StringComparison.Ordinal), "play shell must expose source-anchor labels.");
    Assert(html.Contains("id=\"workspace-stale-posture\"", StringComparison.Ordinal), "play shell must expose explicit stale-state posture for the current shell.");
    Assert(html.Contains("id=\"workspace-grounded-follow-up\"", StringComparison.Ordinal), "play shell must expose a grounded follow-up summary.");
    Assert(html.Contains("id=\"workspace-grounded-follow-up-list\"", StringComparison.Ordinal), "play shell must expose grounded follow-up labels.");
    Assert(html.Contains("id=\"workspace-mobile-campaign-card\"", StringComparison.Ordinal), "play shell must expose a dedicated mobile campaign continuity card.");
    Assert(html.Contains("id=\"workspace-mobile-campaign-current-state\"", StringComparison.Ordinal), "play shell must expose explicit mobile campaign current posture.");
    Assert(html.Contains("id=\"workspace-mobile-campaign-state\"", StringComparison.Ordinal), "play shell must expose explicit mobile campaign state summary.");
    Assert(html.Contains("id=\"workspace-mobile-campaign-cached-state\"", StringComparison.Ordinal), "play shell must expose explicit mobile campaign cached-state detail.");
    Assert(html.Contains("id=\"workspace-mobile-campaign-stale-state\"", StringComparison.Ordinal), "play shell must expose explicit mobile campaign stale-state detail.");
    Assert(html.Contains("id=\"workspace-mobile-campaign-action-required\"", StringComparison.Ordinal), "play shell must expose explicit mobile campaign action-required detail.");
    Assert(html.Contains("id=\"workspace-mobile-campaign-state-list\"", StringComparison.Ordinal), "play shell must expose explicit mobile campaign state labels.");
    Assert(html.Contains("id=\"workspace-legal-runner\"", StringComparison.Ordinal), "play shell must expose legal-runner proof alongside current state");
    Assert(html.Contains("id=\"workspace-understandable-return\"", StringComparison.Ordinal), "play shell must expose understandable-return proof alongside current state");
    Assert(html.Contains("id=\"workspace-campaign-ready\"", StringComparison.Ordinal), "play shell must expose campaign-ready proof alongside current state");
    Assert(html.Contains("id=\"change-packet-list\"", StringComparison.Ordinal), "play shell must expose change-packet labels for the current return anchor");
    Assert(html.Contains("id=\"workspace-server-plane\"", StringComparison.Ordinal), "play shell must expose a campaign server-plane summary alongside current state");
    Assert(html.Contains("id=\"workspace-roster\"", StringComparison.Ordinal), "play shell must expose roster readiness alongside current state");
    Assert(html.Contains("id=\"workspace-runboard\"", StringComparison.Ordinal), "play shell must expose the current runboard summary alongside current state");
    Assert(html.Contains("id=\"workspace-recap\"", StringComparison.Ordinal), "play shell must expose a recap-safe packet summary alongside current state");
    Assert(html.Contains("id=\"workspace-recap-audience\"", StringComparison.Ordinal), "play shell must expose artifact audience posture alongside the recap-safe packet.");
    Assert(html.Contains("id=\"workspace-recap-ownership\"", StringComparison.Ordinal), "play shell must expose artifact ownership posture alongside the recap-safe packet.");
    Assert(html.Contains("id=\"workspace-recap-publication\"", StringComparison.Ordinal), "play shell must expose artifact publication posture alongside the recap-safe packet.");
    Assert(html.Contains("id=\"workspace-recap-provenance\"", StringComparison.Ordinal), "play shell must expose artifact provenance alongside the recap-safe packet.");
    Assert(html.Contains("id=\"workspace-recap-audit\"", StringComparison.Ordinal), "play shell must expose artifact audit posture alongside the recap-safe packet.");
    Assert(html.Contains("id=\"workspace-recap-next\"", StringComparison.Ordinal), "play shell must expose the next artifact-shelf step alongside the recap-safe packet.");
    Assert(html.Contains("id=\"workspace-recap-view\"", StringComparison.Ordinal), "play shell must expose the selected artifact shelf view alongside the recap-safe packet.");
    Assert(html.Contains("id=\"workspace-recap-views\"", StringComparison.Ordinal), "play shell must expose first-class artifact shelf browse targets alongside the recap-safe packet.");
    Assert(html.Contains("id=\"workspace-recap-publication-link\"", StringComparison.Ordinal), "play shell must expose a direct artifact-shelf follow-through link.");
    Assert(html.Contains("id=\"workspace-artifact-shelf-summary\"", StringComparison.Ordinal), "play shell must expose a dedicated selected artifact shelf summary.");
    Assert(html.Contains("id=\"workspace-artifact-selection\"", StringComparison.Ordinal), "play shell must expose a dedicated selected recap artifact summary.");
    Assert(html.Contains("id=\"workspace-artifact-selection-link\"", StringComparison.Ordinal), "play shell must expose a direct selected recap artifact link.");
    Assert(html.Contains("id=\"workspace-artifact-shelf-link\"", StringComparison.Ordinal), "play shell must expose a direct selected artifact shelf link.");
    Assert(html.Contains("id=\"workspace-runner-goal-updates\"", StringComparison.Ordinal), "play shell must expose a dedicated runner-goal update summary.");
    Assert(html.Contains("id=\"workspace-runner-goal-update-list\"", StringComparison.Ordinal), "play shell must expose runner-goal update labels.");
    Assert(html.Contains("id=\"workspace-player-safe-consequence-feed\"", StringComparison.Ordinal), "play shell must expose a dedicated player-safe consequence feed summary.");
    Assert(html.Contains("id=\"workspace-player-safe-consequence-feed-list\"", StringComparison.Ordinal), "play shell must expose player-safe consequence feed labels.");
    Assert(html.Contains("id=\"workspace-replay\"", StringComparison.Ordinal), "play shell must expose a replay-safe package summary alongside the recap-safe packet.");
    Assert(html.Contains("id=\"workspace-replay-audience\"", StringComparison.Ordinal), "play shell must expose replay artifact audience posture alongside the replay-safe package.");
    Assert(html.Contains("id=\"workspace-replay-ownership\"", StringComparison.Ordinal), "play shell must expose replay artifact ownership posture alongside the replay-safe package.");
    Assert(html.Contains("id=\"workspace-replay-publication\"", StringComparison.Ordinal), "play shell must expose replay artifact publication posture alongside the replay-safe package.");
    Assert(html.Contains("id=\"workspace-replay-provenance\"", StringComparison.Ordinal), "play shell must expose replay artifact provenance alongside the replay-safe package.");
    Assert(html.Contains("id=\"workspace-replay-audit\"", StringComparison.Ordinal), "play shell must expose replay artifact audit posture alongside the replay-safe package.");
    Assert(html.Contains("id=\"workspace-replay-lineage\"", StringComparison.Ordinal), "play shell must expose replay artifact lineage alongside the replay-safe package.");
    Assert(html.Contains("id=\"workspace-replay-next\"", StringComparison.Ordinal), "play shell must expose the replay-safe next step alongside the replay-safe package.");
    Assert(html.Contains("id=\"workspace-replay-publication-link\"", StringComparison.Ordinal), "play shell must expose a direct replay artifact follow-through link.");
    Assert(html.Contains("id=\"workspace-memory\"", StringComparison.Ordinal), "play shell must expose the campaign-memory summary alongside current state");
    Assert(html.Contains("id=\"workspace-memory-return\"", StringComparison.Ordinal), "play shell must expose the campaign-memory return cue alongside current state");
    Assert(html.Contains("id=\"workspace-continuity-rail\"", StringComparison.Ordinal), "play shell must expose a continuity rail summary for downtime/diary/contacts/heat/aftermath/return.");
    Assert(html.Contains("id=\"workspace-continuity-rail-list\"", StringComparison.Ordinal), "play shell must expose continuity rail labels for downtime/diary/contacts/heat/aftermath/return.");
    Assert(html.Contains("id=\"workspace-gm-ops\"", StringComparison.Ordinal), "play shell must expose GM operations summary for opposition/prep/roster/event controls.");
    Assert(html.Contains("id=\"workspace-gm-ops-list\"", StringComparison.Ordinal), "play shell must expose GM operations lane labels for opposition/prep/roster/event controls.");
    Assert(html.Contains("id=\"workspace-offline-truth\"", StringComparison.Ordinal), "play shell must expose explicit cached/stale/offline-action truth alongside the continuity rail.");
    Assert(html.Contains("id=\"workspace-offline-truth-list\"", StringComparison.Ordinal), "play shell must expose explicit cached/stale/offline-action labels alongside the continuity rail.");
    Assert(html.Contains("id=\"workspace-action-required\"", StringComparison.Ordinal), "play shell must expose a dedicated action-required summary alongside the continuity rail.");
    Assert(html.Contains("id=\"workspace-action-required-list\"", StringComparison.Ordinal), "play shell must expose dedicated action-required labels alongside the continuity rail.");
    Assert(html.Contains("id=\"workspace-decision-notice\"", StringComparison.Ordinal), "play shell must expose the current decision notice alongside current state");
    Assert(html.Contains("id=\"workspace-decision-notice-link\"", StringComparison.Ordinal), "play shell must expose a direct decision-notice follow-through link.");
    Assert(html.Contains("id=\"workspace-travel\"", StringComparison.Ordinal), "play shell must expose deliberate travel readiness alongside current state");
    Assert(html.Contains("id=\"workspace-prefetch\"", StringComparison.Ordinal), "play shell must expose the bounded offline prefetch summary alongside current state");
    Assert(html.Contains("id=\"workspace-update\"", StringComparison.Ordinal), "play shell must expose update posture alongside current state");
    Assert(html.Contains("id=\"workspace-support\"", StringComparison.Ordinal), "play shell must expose support posture alongside current state");
    Assert(html.Contains("id=\"workspace-support-status\"", StringComparison.Ordinal), "play shell must expose support status alongside current state");
    Assert(html.Contains("id=\"workspace-known-issue\"", StringComparison.Ordinal), "play shell must expose the current known-issue summary alongside current state");
    Assert(html.Contains("id=\"workspace-fix-state\"", StringComparison.Ordinal), "play shell must expose fix-availability truth alongside current state");
    Assert(html.Contains("id=\"follow-through-update\"", StringComparison.Ordinal), "play shell must expose the explicit update follow-through route.");
    Assert(html.Contains("id=\"follow-through-update-link\"", StringComparison.Ordinal), "play shell must expose a direct update follow-through link.");
    Assert(html.Contains("id=\"follow-through-support\"", StringComparison.Ordinal), "play shell must expose the explicit support follow-through route.");
    Assert(html.Contains("id=\"follow-through-support-link\"", StringComparison.Ordinal), "play shell must expose a direct support follow-through link.");
    Assert(html.Contains("id=\"follow-through-role\"", StringComparison.Ordinal), "play shell must expose the explicit role follow-through route.");
    Assert(html.Contains("id=\"follow-through-role-link\"", StringComparison.Ordinal), "play shell must expose a direct role follow-through link.");
    Assert(html.Contains("id=\"follow-through\"", StringComparison.Ordinal), "play shell must expose explicit follow-through labels for update and support posture");
    Assert(html.Contains("id=\"attention-list\"", StringComparison.Ordinal), "play shell must expose an attention list for continuity risks");
    Assert(html.Contains("id=\"restore-summary\"", StringComparison.Ordinal), "play shell must expose a claimed-device recovery summary region");
    Assert(html.Contains("id=\"restore-rule-environment\"", StringComparison.Ordinal), "play shell must expose rule-environment recovery posture");
    Assert(html.Contains("id=\"restore-prefetch\"", StringComparison.Ordinal), "play shell must expose claimed-device offline prefetch readiness");
    Assert(html.Contains("id=\"restore-local-boundary\"", StringComparison.Ordinal), "play shell must expose install-local cache boundaries for restore planning");
    Assert(html.Contains("id=\"restore-offline-truth\"", StringComparison.Ordinal), "play shell must expose restore cached/stale/offline-action truth.");
    Assert(html.Contains("id=\"restore-offline-truth-labels\"", StringComparison.Ordinal), "play shell must expose restore cached/stale/offline-action labels.");
    Assert(html.Contains("id=\"restore-action-required\"", StringComparison.Ordinal), "play shell must expose restore action-required summary.");
    Assert(html.Contains("id=\"restore-action-required-labels\"", StringComparison.Ordinal), "play shell must expose restore action-required labels.");
    Assert(html.Contains("id=\"restore-travel-campaign-card\"", StringComparison.Ordinal), "play shell must expose a dedicated travel campaign continuity card.");
    Assert(html.Contains("id=\"restore-travel-campaign-current-state\"", StringComparison.Ordinal), "play shell must expose explicit restore travel campaign current posture.");
    Assert(html.Contains("id=\"restore-travel-campaign-state\"", StringComparison.Ordinal), "play shell must expose explicit restore travel campaign summary.");
    Assert(html.Contains("id=\"restore-travel-campaign-cached-state\"", StringComparison.Ordinal), "play shell must expose explicit restore travel campaign cached-state detail.");
    Assert(html.Contains("id=\"restore-travel-campaign-stale-state\"", StringComparison.Ordinal), "play shell must expose explicit restore travel campaign stale-state detail.");
    Assert(html.Contains("id=\"restore-travel-campaign-action-required\"", StringComparison.Ordinal), "play shell must expose explicit restore travel campaign action-required detail.");
    Assert(html.Contains("id=\"restore-travel-campaign-state-labels\"", StringComparison.Ordinal), "play shell must expose explicit restore travel campaign state labels.");
    Assert(html.Contains("id=\"restore-travel-companion\"", StringComparison.Ordinal), "play shell must expose restore travel companion continuity truth.");
    Assert(html.Contains("id=\"restore-travel-companion-labels\"", StringComparison.Ordinal), "play shell must expose restore travel companion continuity labels.");
    Assert(html.Contains("id=\"restore-prefetch-labels\"", StringComparison.Ordinal), "play shell must expose explicit prefetch labels for alternate claimed-device lanes");
    Assert(html.Contains("id=\"restore-attention\"", StringComparison.Ordinal), "play shell must expose restore attention items");
    Assert(html.Contains("id=\"restore-local-notes\"", StringComparison.Ordinal), "play shell must expose install-local restore notes");
    Assert(html.Contains("/api/play/workspace-lite/", StringComparison.Ordinal), "play shell must fetch the workspace-lite projection instead of dumping only the raw resume payload");
    Assert(html.Contains("/api/play/restore-plan/", StringComparison.Ordinal), "play shell must fetch the claimed-device restore projection alongside the workspace-lite payload");
    Assert(html.Contains("/api/play/onboarding-recovery/", StringComparison.Ordinal), "play shell must fetch onboarding/recovery entry projection alongside workspace and restore payloads");
    Assert(html.Contains("beforeinstallprompt", StringComparison.Ordinal), "play shell must listen for install-prompt availability.");
    Assert(html.Contains("window.addEventListener(\"online\", updateNetworkStatus);", StringComparison.Ordinal), "play shell must react to live online transitions.");
    Assert(html.Contains("window.addEventListener(\"offline\", updateNetworkStatus);", StringComparison.Ordinal), "play shell must react to live offline transitions.");
}

static async Task VerifyServiceWorkerKeepsPrivatePlayApiNetworkOnlyAsync()
{
    var serviceWorkerPath = Path.Combine(GetRepoRoot(), "src", "Chummer.Play.Web", "wwwroot", "service-worker.js");
    var script = await File.ReadAllTextAsync(serviceWorkerPath);

    Assert(script.Contains("url.pathname.startsWith(\"/api/play/\")", StringComparison.Ordinal), "service worker must explicitly route private play API reads.");
    Assert(script.Contains("Private play state is network-only", StringComparison.Ordinal), "service worker must document the private API cache boundary.");
    Assert(script.Contains("play_api_network_unavailable", StringComparison.Ordinal), "service worker must return a typed offline failure for private play API reads.");
    Assert(script.Contains("\"cache-control\": \"no-store\"", StringComparison.Ordinal), "service worker private API fallback must be non-cacheable.");
    Assert(script.Contains("\"/mobile\"", StringComparison.Ordinal), "service worker shell cache must include the mobile turn companion route.");
    Assert(script.Contains("\"/mobile.css\"", StringComparison.Ordinal), "service worker shell cache must include the mobile turn companion stylesheet.");
    Assert(script.Contains("\"/mobile-turn-companion.js\"", StringComparison.Ordinal), "service worker shell cache must include the mobile turn companion client runtime.");
    Assert(script.Contains("\"/icons/apple-touch-icon.png\"", StringComparison.Ordinal), "service worker shell cache must include the apple touch icon for install-local relaunch.");
    Assert(script.Contains("\"/icons/icon-192.png\"", StringComparison.Ordinal), "service worker shell cache must include the 192px raster icon.");
    Assert(script.Contains("\"/icons/icon-512.png\"", StringComparison.Ordinal), "service worker shell cache must include the 512px raster icon.");
    Assert(script.Contains("const MOBILE_NAV_FALLBACK = \"/mobile\";", StringComparison.Ordinal), "service worker must keep a dedicated mobile navigation fallback.");
    Assert(!script.Contains("\"/_framework/blazor.web.js\"", StringComparison.Ordinal), "mobile turn companion shell cache must not depend on the Blazor interactive framework script.");
    Assert(!script.Contains("API_CACHE", StringComparison.Ordinal), "service worker must not keep a Cache API bucket for private play API responses.");
    Assert(!script.Contains("cacheWithQuotaHandling(API_CACHE", StringComparison.Ordinal), "service worker must not persist private play API responses.");
    Assert(!script.Contains("caches.open(API_CACHE)", StringComparison.Ordinal), "service worker must not replay cached private play API responses.");
}

static async Task VerifyPlayApiBoundaryRequiresTrustedContextAsync()
{
    var applicationPath = Path.Combine(GetRepoRoot(), "src", "Chummer.Play.Web", "PlayWebApplication.cs");
    var source = await File.ReadAllTextAsync(applicationPath);

    Assert(source.Contains("app.Use(RequireTrustedPlayApiBoundaryAsync);", StringComparison.Ordinal), "mobile app must register the play API trust boundary before route handlers.");
    Assert(source.Contains("context.Request.Path.StartsWithSegments(\"/api/play\")", StringComparison.Ordinal), "play API boundary must scope itself to private /api/play routes.");
    Assert(source.Contains("ApplyPrivateNoStoreHeaders(context.Response)", StringComparison.Ordinal), "play API responses must be marked private and non-cacheable.");
    Assert(source.Contains("context.Response.OnStarting(() =>", StringComparison.Ordinal), "play API no-store headers must be applied to successful and denied responses.");
    Assert(source.Contains("context.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment()", StringComparison.Ordinal), "development mode must remain explicit for local test/dev play API access.");
    Assert(source.Contains("IPAddress.IsLoopback(remoteAddress)", StringComparison.Ordinal), "loopback requests must remain the only implicit production trust case.");
    Assert(source.Contains("CHUMMER_PLAY_API_KEY", StringComparison.Ordinal), "remote production play API calls must require an explicit configured key.");
    Assert(source.Contains("X-Chummer-Play-Api-Key", StringComparison.Ordinal), "remote production play API calls must use the documented play API key header.");
    Assert(source.Contains("CryptographicOperations.FixedTimeEquals", StringComparison.Ordinal), "play API key comparison must be constant-time.");
    Assert(source.Contains("play_api_forbidden", StringComparison.Ordinal), "blocked play API calls must return a typed denial.");

    var remoteProductionNoKey = BuildPlayApiBoundaryContext(
        environmentName: Environments.Production,
        configuredApiKey: null,
        suppliedApiKey: null,
        remoteAddress: IPAddress.Parse("203.0.113.24"));
    Assert(!PlayWebApplication.IsTrustedPlayApiRequest(remoteProductionNoKey), "remote production play API calls must be denied when no API key is configured.");

    var remoteProductionWrongKey = BuildPlayApiBoundaryContext(
        environmentName: Environments.Production,
        configuredApiKey: "expected-play-key",
        suppliedApiKey: "wrong-play-key",
        remoteAddress: IPAddress.Parse("203.0.113.24"));
    Assert(!PlayWebApplication.IsTrustedPlayApiRequest(remoteProductionWrongKey), "remote production play API calls must reject mismatched API keys.");

    var remoteProductionValidKey = BuildPlayApiBoundaryContext(
        environmentName: Environments.Production,
        configuredApiKey: "expected-play-key",
        suppliedApiKey: "expected-play-key",
        remoteAddress: IPAddress.Parse("203.0.113.24"));
    Assert(PlayWebApplication.IsTrustedPlayApiRequest(remoteProductionValidKey), "remote production play API calls must pass only with the configured API key.");

    var loopbackProduction = BuildPlayApiBoundaryContext(
        environmentName: Environments.Production,
        configuredApiKey: null,
        suppliedApiKey: null,
        remoteAddress: IPAddress.Loopback);
    Assert(PlayWebApplication.IsTrustedPlayApiRequest(loopbackProduction), "loopback production play API calls must remain trusted for local installs.");

    var developmentRemote = BuildPlayApiBoundaryContext(
        environmentName: Environments.Development,
        configuredApiKey: null,
        suppliedApiKey: null,
        remoteAddress: IPAddress.Parse("203.0.113.24"));
    Assert(PlayWebApplication.IsTrustedPlayApiRequest(developmentRemote), "development play API calls must remain trusted for local developer smoke tests.");

    var deniedApiResponse = await InvokePlayApiBoundaryAsync(
        environmentName: Environments.Production,
        configuredApiKey: null,
        suppliedApiKey: null,
        remoteAddress: IPAddress.Parse("203.0.113.24"));
    Assert(!deniedApiResponse.NextCalled, "remote production play API calls without trust must not reach route handlers.");
    Assert(deniedApiResponse.StatusCode == StatusCodes.Status403Forbidden, "remote production play API calls without trust must receive a forbidden response.");
    Assert(deniedApiResponse.Body.Contains("play_api_forbidden", StringComparison.Ordinal), "remote production play API denial must keep the typed error contract.");
    Assert(string.Equals(deniedApiResponse.CacheControl, "private, no-store", StringComparison.Ordinal), "remote production play API denial must remain private and non-cacheable.");
    Assert(string.Equals(deniedApiResponse.Pragma, "no-cache", StringComparison.Ordinal), "remote production play API denial must carry legacy no-cache headers.");
    Assert(string.Equals(deniedApiResponse.Expires, "0", StringComparison.Ordinal), "remote production play API denial must expire immediately.");

    var allowedApiResponse = await InvokePlayApiBoundaryAsync(
        environmentName: Environments.Production,
        configuredApiKey: "expected-play-key",
        suppliedApiKey: "expected-play-key",
        remoteAddress: IPAddress.Parse("203.0.113.24"));
    Assert(allowedApiResponse.NextCalled, "remote production play API calls with the configured key must reach route handlers.");
    Assert(allowedApiResponse.StatusCode == StatusCodes.Status204NoContent, "trusted play API middleware pass-through must preserve the route handler response.");
    Assert(string.Equals(allowedApiResponse.CacheControl, "private, no-store", StringComparison.Ordinal), "trusted play API responses must remain private and non-cacheable.");
}

static async Task VerifyIndexShellBindsContextualActionLabelsAsync()
{
    var indexHtmlPath = Path.Combine(GetRepoRoot(), "src", "Chummer.Play.Web", "wwwroot", "index.html");
    var html = await File.ReadAllTextAsync(indexHtmlPath);

    Assert(html.Contains("setLink(\"workspace-decision-notice-link\", payload.decisionNoticeHref, payload.decisionNotice, \"/\", \"Decision notice follow-through\");", StringComparison.Ordinal), "play shell must bind decision-notice follow-through from the workspace projection instead of hiding it behind generic copy.");
    Assert(html.Contains("setText(\"entry-state-summary\", payload.entryStateSummary, \"Entry onboarding and recovery state is not available yet.\");", StringComparison.Ordinal), "play shell must bind onboarding/recovery entry state summary.");
    Assert(html.Contains("setLink(\"entry-recommended-link\", payload.recommendedActionHref, payload.recommendedActionLabel, \"/play\", \"Recommended next step\");", StringComparison.Ordinal), "play shell must bind one-tap recommended onboarding/recovery route.");
    Assert(html.Contains("setLink(\"entry-retry-link\", payload.retryActionHref, payload.retryActionLabel, \"/play\", \"Retry recovery\");", StringComparison.Ordinal), "play shell must bind retry route from onboarding/recovery projection.");
    Assert(html.Contains("setLink(\"entry-cancel-link\", payload.cancelActionHref, payload.cancelActionLabel, \"/\", \"Cancel and stay read-only\");", StringComparison.Ordinal), "play shell must bind cancel route from onboarding/recovery projection.");
    Assert(html.Contains("setLink(\"entry-restore-link\", payload.restoreActionHref, payload.restoreActionLabel, \"/play\", \"Restore claimed-device plan\");", StringComparison.Ordinal), "play shell must bind restore route from onboarding/recovery projection.");
    Assert(html.Contains("setList(\"entry-recovery-actions\", payload.recoveryActions);", StringComparison.Ordinal), "play shell must bind onboarding/recovery retry-cancel-restore guidance labels.");
    Assert(html.Contains("setText(\"critical-rejoin\", payload.rejoinCommand, \"No rejoin command is available yet.\");", StringComparison.Ordinal), "play shell must bind the dedicated rejoin command label from the workspace-lite projection.");
    Assert(html.Contains("setLink(\"critical-rejoin-link\", payload.rejoinCommandHref, payload.rejoinCommand, \"/play\", \"Rejoin\");", StringComparison.Ordinal), "play shell must bind the dedicated rejoin command route from the workspace-lite projection.");
    Assert(html.Contains("setText(\"critical-continue\", payload.continueCommand, \"No continue command is available yet.\");", StringComparison.Ordinal), "play shell must bind the dedicated continue command label from the workspace-lite projection.");
    Assert(html.Contains("setLink(\"critical-continue-link\", payload.continueCommandHref, payload.continueCommand, \"/play\", \"Continue\");", StringComparison.Ordinal), "play shell must bind the dedicated continue command route from the workspace-lite projection.");
    Assert(html.Contains("setText(\"critical-support\", payload.supportCommand, \"No support command is available yet.\");", StringComparison.Ordinal), "play shell must bind the dedicated support command label from the workspace-lite projection.");
    Assert(html.Contains("setLink(\"critical-support-link\", payload.supportCommandHref, payload.supportCommand, \"/contact\", \"Support\");", StringComparison.Ordinal), "play shell must bind the dedicated support command route from the workspace-lite projection.");
    Assert(html.Contains("setText(\"critical-decision-receipt-summary\", payload.longRunningDecisionReceiptSummary, \"Decision receipts are not available yet.\");", StringComparison.Ordinal), "play shell must bind decision-receipt summary copy from the workspace-lite projection.");
    Assert(html.Contains("setList(\"critical-decision-receipts\", payload.longRunningDecisionReceipts);", StringComparison.Ordinal), "play shell must bind decision receipts for rejoin, quick actions, and resume from the workspace-lite projection.");
    Assert(html.Contains("setList(\"critical-low-noise-guidance\", payload.lowNoiseGuidance);", StringComparison.Ordinal), "play shell must bind low-noise guidance from the workspace-lite projection.");
    Assert(html.Contains("setText(\"workspace-player-table-cards\", payload.playerTableCardsSummary, \"No player table-card summary is available yet.\");", StringComparison.Ordinal), "play shell must bind the player table-card summary from the workspace-lite projection.");
    Assert(html.Contains("setList(\"workspace-player-table-cards-list\", payload.playerTableCardLabels);", StringComparison.Ordinal), "play shell must bind the player table-card labels from the workspace-lite projection.");
    Assert(html.Contains("setText(\"workspace-between-turn-affordances\", payload.betweenTurnAffordancesSummary, \"No between-turn affordance summary is available yet.\");", StringComparison.Ordinal), "play shell must bind the between-turn affordance summary from the workspace-lite projection.");
    Assert(html.Contains("setList(\"workspace-between-turn-affordances-list\", payload.betweenTurnAffordanceLabels);", StringComparison.Ordinal), "play shell must bind the between-turn affordance labels from the workspace-lite projection.");
    Assert(html.Contains("setText(\"workspace-gm-lite-continuity\", payload.gmLiteContinuitySummary, \"No GM-lite continuity summary is available yet.\");", StringComparison.Ordinal), "play shell must bind the GM-lite continuity summary from the workspace-lite projection.");
    Assert(html.Contains("setList(\"workspace-gm-lite-continuity-list\", payload.gmLiteContinuityLabels);", StringComparison.Ordinal), "play shell must bind the GM-lite continuity labels from the workspace-lite projection.");
    Assert(html.Contains("setText(\"recover-disconnect\", payload.disconnectRecoveryCopy, \"Disconnect recovery copy is not available yet.\");", StringComparison.Ordinal), "play shell must bind explicit disconnect recovery copy from the workspace-lite projection.");
    Assert(html.Contains("setText(\"recover-role-change\", payload.roleChangeRecoveryCopy, \"Role-change recovery copy is not available yet.\");", StringComparison.Ordinal), "play shell must bind explicit role-change recovery copy from the workspace-lite projection.");
    Assert(html.Contains("setText(\"recover-observer-transition\", payload.observerTransitionRecoveryCopy, \"Observer-transition recovery copy is not available yet.\");", StringComparison.Ordinal), "play shell must bind explicit observer-transition recovery copy from the workspace-lite projection.");
    Assert(html.Contains("const explicitDeviceId = params.get(\"deviceId\") || \"\";", StringComparison.Ordinal), "play shell must read the explicit claimed-device id from the shell query.");
    Assert(html.Contains("const resolvedDeviceId = resolveStableDeviceId(role, explicitDeviceId);", StringComparison.Ordinal), "play shell must stabilize a claimed-device id when the query omits one.");
    Assert(html.Contains("const observerId = resolveObserverId();", StringComparison.Ordinal), "play shell must stabilize an observer continuity id for cross-device handoff.");
    Assert(html.Contains("const observeResponse = await fetch(`/api/play/observe/${encodeURIComponent(sessionId)}`);", StringComparison.Ordinal), "play shell must load stored continuity state from the observe route.");
    Assert(html.Contains("renderContinuityClaimStatus({", StringComparison.Ordinal), "play shell must surface continuity claim posture before rendering the deeper workspace surfaces.");
    Assert(html.Contains("fetch(\"/api/play/continuity/claim\", {", StringComparison.Ordinal), "play shell must let the user refresh claimed-device continuity from the mobile shell.");
    Assert(html.Contains("document.getElementById(\"shell-continuity-claim-button\").addEventListener(\"click\", claimContinuityOnThisDevice);", StringComparison.Ordinal), "play shell must wire the continuity-claim action button.");
    Assert(html.Contains("id=\"shell-continuity-claim-status\"", StringComparison.Ordinal), "play shell must render a dedicated continuity-claim status surface.");
    Assert(html.Contains("id=\"shell-owner-route-link\"", StringComparison.Ordinal), "play shell must render a dedicated owner-route follow-through for cross-device handoff.");
    Assert(html.Contains("id=\"shell-continuity-claim-button\"", StringComparison.Ordinal), "play shell must render a one-tap claimed-device continuity action.");
    Assert(html.Contains("setText(\"workspace-quick-explain\", payload.quickExplainSummary, \"No quick explain summary is available yet.\");", StringComparison.Ordinal), "play shell must bind packet-backed quick explain summary from the workspace-lite projection.");
    Assert(html.Contains("setList(\"workspace-quick-explain-list\", payload.quickExplainLabels);", StringComparison.Ordinal), "play shell must bind quick-explain labels from the workspace-lite projection.");
    Assert(html.Contains("setText(\"workspace-source-anchor\", payload.sourceAnchorSummary, \"No source-anchor context is available yet.\");", StringComparison.Ordinal), "play shell must bind source-anchor context from the workspace-lite projection.");
    Assert(html.Contains("setList(\"workspace-source-anchor-list\", payload.sourceAnchorLabels);", StringComparison.Ordinal), "play shell must bind source-anchor labels from the workspace-lite projection.");
    Assert(html.Contains("setText(\"workspace-stale-posture\", payload.staleStatePosture, \"No stale-state posture is available yet.\");", StringComparison.Ordinal), "play shell must bind stale-state posture from the workspace-lite projection.");
    Assert(html.Contains("setText(\"workspace-grounded-follow-up\", payload.groundedFollowUpSummary, \"No grounded follow-up summary is available yet.\");", StringComparison.Ordinal), "play shell must bind grounded follow-up summary from the workspace-lite projection.");
    Assert(html.Contains("setList(\"workspace-grounded-follow-up-list\", payload.groundedFollowUpLabels);", StringComparison.Ordinal), "play shell must bind grounded follow-up labels from the workspace-lite projection.");
    Assert(html.Contains("setText(\"workspace-mobile-campaign-current-state\", payload.mobileCampaignCurrentState, \"No mobile campaign continuity posture is available yet.\");", StringComparison.Ordinal), "play shell must bind explicit mobile campaign current posture from the workspace-lite projection.");
    Assert(html.Contains("setText(\"workspace-mobile-campaign-state\", payload.mobileCampaignStateSummary, \"No mobile campaign-state summary is available yet.\");", StringComparison.Ordinal), "play shell must bind explicit mobile campaign summary from the workspace-lite projection.");
    Assert(html.Contains("setList(\"workspace-mobile-campaign-state-list\", payload.mobileCampaignStateLabels);", StringComparison.Ordinal), "play shell must bind explicit mobile campaign state labels from the workspace-lite projection.");
    Assert(html.Contains("setText(\"workspace-action-required\", payload.actionRequiredSummary, \"No action-required summary is available yet.\");", StringComparison.Ordinal), "play shell must bind the explicit workspace action-required summary from the workspace-lite projection.");
    Assert(html.Contains("setList(\"workspace-action-required-list\", payload.actionRequiredLabels);", StringComparison.Ordinal), "play shell must bind the explicit workspace action-required labels from the workspace-lite projection.");
    Assert(html.Contains("setText(\"restore-travel-campaign-current-state\", payload.travelCampaignCurrentState, \"No restore travel campaign continuity posture is available yet.\");", StringComparison.Ordinal), "play shell must bind explicit restore travel campaign current posture.");
    Assert(html.Contains("setText(\"restore-travel-campaign-state\", payload.travelCampaignStateSummary, \"No restore travel campaign-state summary is available yet.\");", StringComparison.Ordinal), "play shell must bind explicit restore travel campaign summary.");
    Assert(html.Contains("setList(\"restore-travel-campaign-state-labels\", payload.travelCampaignStateLabels);", StringComparison.Ordinal), "play shell must bind explicit restore travel campaign state labels from the restore projection.");
    Assert(html.Contains("setText(\"restore-action-required\", payload.actionRequiredSummary, \"No restore action-required summary is available yet.\");", StringComparison.Ordinal), "play shell must bind the explicit restore action-required summary from the restore projection.");
    Assert(html.Contains("setList(\"restore-action-required-labels\", payload.actionRequiredLabels);", StringComparison.Ordinal), "play shell must bind the explicit restore action-required labels from the restore projection.");
    Assert(html.Contains("function renderShellHero(payload, restorePayload)", StringComparison.Ordinal), "play shell must derive a dedicated flagship hero from workspace and restore payloads.");
    Assert(html.Contains("document.getElementById(\"shell-headline\").textContent = roleFocus.heading;", StringComparison.Ordinal), "play shell must bind the hero headline to authored role-specific copy.");
    Assert(html.Contains("document.getElementById(\"shell-subhead\").textContent = payload.summary || \"No workspace-lite summary is available yet.\";", StringComparison.Ordinal), "play shell must bind the hero subhead to the workspace-lite summary.");
    Assert(html.Contains("document.getElementById(\"shell-status\").textContent = payload.currentSceneSummary || \"No current scene summary is available yet.\";", StringComparison.Ordinal), "play shell must bind the hero status line to the current scene summary.");
    Assert(html.Contains("document.getElementById(\"shell-continuity-status\").textContent = payload.mobileCampaignCurrentState || \"No mobile continuity posture is available yet.\";", StringComparison.Ordinal), "play shell must bind the hero continuity status.");
    Assert(html.Contains("document.getElementById(\"shell-restore-status\").textContent = restorePayload.resumeSummary || \"No claimed-device recovery summary is available yet.\";", StringComparison.Ordinal), "play shell must bind the hero restore status.");
    Assert(html.Contains("setLink(\"shell-primary-action-link\", primaryActionHref, primaryActionText, \"/play\", \"Open safe next step\");", StringComparison.Ordinal), "play shell must bind the hero primary action to the safe-next-step route.");
    Assert(html.Contains("setLink(\"shell-secondary-action-link\", secondaryActionHref, secondaryActionText, \"/contact\", \"Open support route\");", StringComparison.Ordinal), "play shell must bind the hero secondary action to the support route.");
    Assert(html.Contains("document.getElementById(\"shell-install-button\").addEventListener(\"click\", async () => {", StringComparison.Ordinal), "play shell must bind the install action to the deferred PWA prompt.");
    Assert(html.Contains("document.getElementById(\"shell-install-chip-value\").textContent = state.label;", StringComparison.Ordinal), "play shell must bind install-state chip copy from installability status.");
    Assert(html.Contains("document.getElementById(\"shell-network-chip-value\").textContent = label;", StringComparison.Ordinal), "play shell must bind network-state chip copy from online/offline status.");
    Assert(html.Contains("let lastWorkspacePayload = null;", StringComparison.Ordinal), "play shell must retain the latest workspace payload so install and network trust cues can stay current.");
    Assert(html.Contains("let lastRestorePayload = null;", StringComparison.Ordinal), "play shell must retain the latest restore payload so install and network trust cues can stay current.");
    Assert(html.Contains("let serviceWorkerDetail = \"Service worker state: verification is still pending.\";", StringComparison.Ordinal), "play shell must keep explicit service-worker trust copy.");
    Assert(html.Contains("document.getElementById(\"shell-service-worker-detail\").textContent = serviceWorkerDetail;", StringComparison.Ordinal), "play shell must bind service-worker trust copy into the hero shell.");
    Assert(html.Contains("document.getElementById(\"shell-continuity-safety\").textContent = lastWorkspacePayload?.actionRequiredSummary", StringComparison.Ordinal), "play shell must bind claimed-device continuity safety from the latest workspace or restore payload.");
    Assert(html.Contains("setList(\"shell-network-state-list\", buildNetworkStateLabels());", StringComparison.Ordinal), "play shell must bind a live network-state list for install and reconnect trust.");
    Assert(html.Contains("function buildNetworkStateLabels()", StringComparison.Ordinal), "play shell must derive explicit install, network, continuity, and support trust labels.");
    Assert(html.Contains("lastWorkspacePayload = payload;", StringComparison.Ordinal), "play shell must capture the latest workspace payload when rendering the hero shell.");
    Assert(html.Contains("lastRestorePayload = restorePayload;", StringComparison.Ordinal), "play shell must capture the latest restore payload when rendering the hero shell.");
    Assert(html.Contains("serviceWorkerDetail = navigator.serviceWorker.controller", StringComparison.Ordinal), "play shell must distinguish an active service-worker controller from first-load registration.");
    Assert(html.Contains("serviceWorkerDetail = \"Service worker state: registration failed, so install-local cache trust is reduced until this shell reloads cleanly.\";", StringComparison.Ordinal), "play shell must expose a degraded cache-trust posture when service-worker registration fails.");
    Assert(html.Contains("window.addEventListener(\"beforeinstallprompt\", (event) => {", StringComparison.Ordinal), "play shell must react to the deferred install prompt event.");
    Assert(html.Contains("updateNetworkStatus();", StringComparison.Ordinal), "play shell must refresh network and continuity trust cues after installability changes.");
    Assert(html.Contains("function inferContinuityTone(text)", StringComparison.Ordinal), "play shell must derive tone-aware continuity cues from explicit stale/cached/action-required posture.");
    Assert(html.Contains("if (lowered.includes(\"action required\") || lowered.includes(\"action-required\")) {", StringComparison.Ordinal), "play shell tone inference must treat action-required continuity as a first-class state.");
    Assert(html.Contains("function syncContinuityCardTone(cardId, currentStateId, summaryId, currentStateText, actionId)", StringComparison.Ordinal), "play shell must let continuity card tone inspect explicit action-required posture.");
    Assert(html.Contains("const actionState = actionId ? (document.getElementById(actionId).textContent || \"\") : \"\";", StringComparison.Ordinal), "play shell continuity card tone must read the action-required field.");
    Assert(html.Contains("card.dataset.tone = inferContinuityTone(actionState || currentStateText || currentState || summary);", StringComparison.Ordinal), "play shell continuity card tone must prioritize action-required posture over summary-only copy.");
    Assert(html.Contains("syncContinuityCardTone(\n      \"workspace-mobile-campaign-card\",", StringComparison.Ordinal), "play shell must apply tone-aware mobile campaign continuity card cues.");
    Assert(html.Contains("`${payload.mobileCampaignStaleState || \"\"} ${payload.mobileCampaignCurrentState || \"\"}`", StringComparison.Ordinal), "play shell must let stale mobile campaign posture influence the continuity card tone before the summary can look green.");
    Assert(html.Contains("\"workspace-mobile-campaign-action-required\");", StringComparison.Ordinal), "play shell must route workspace action-required posture into the mobile campaign continuity tone.");
    Assert(html.Contains("syncContinuityStateBreakdown(\n      \"workspace-mobile-campaign-cached-state\",", StringComparison.Ordinal), "play shell must keep the workspace cached/stale/action-required continuity breakdown wired.");
    Assert(html.Contains("syncContinuityCardTone(\n      \"restore-travel-campaign-card\",", StringComparison.Ordinal), "play shell must apply tone-aware restore travel continuity card cues.");
    Assert(html.Contains("`${payload.travelCampaignStaleState || \"\"} ${payload.travelCampaignCurrentState || \"\"}`", StringComparison.Ordinal), "play shell must let stale restore travel posture influence the continuity card tone before the summary can look green.");
    Assert(html.Contains("\"restore-travel-campaign-action-required\");", StringComparison.Ordinal), "play shell must route restore action-required posture into the travel campaign continuity tone.");
    Assert(html.Contains("syncContinuityStateBreakdown(\n      \"restore-travel-campaign-cached-state\",", StringComparison.Ordinal), "play shell must keep the restore cached/stale/action-required continuity breakdown wired.");
    Assert(html.Contains("setText(\"workspace-recap-audience\", payload.recapAudienceSummary, \"No artifact audience summary is available yet.\");", StringComparison.Ordinal), "play shell must bind artifact audience posture from the workspace-lite projection.");
    Assert(html.Contains("setText(\"workspace-recap-ownership\", payload.recapOwnershipSummary, \"No artifact ownership summary is available yet.\");", StringComparison.Ordinal), "play shell must bind artifact ownership posture from the workspace-lite projection.");
    Assert(html.Contains("setText(\"workspace-recap-publication\", payload.recapPublicationSummary, \"No artifact publication summary is available yet.\");", StringComparison.Ordinal), "play shell must bind artifact publication posture from the workspace-lite projection.");
    Assert(html.Contains("setText(\"workspace-recap-provenance\", payload.recapProvenanceSummary, \"No artifact provenance summary is available yet.\");", StringComparison.Ordinal), "play shell must bind artifact provenance posture from the workspace-lite projection.");
    Assert(html.Contains("setText(\"workspace-recap-audit\", payload.recapAuditSummary, \"No artifact audit summary is available yet.\");", StringComparison.Ordinal), "play shell must bind artifact audit posture from the workspace-lite projection.");
    Assert(html.Contains("setText(\"workspace-recap-lineage\", payload.recapLineageSummary, \"No artifact lineage summary is available yet.\");", StringComparison.Ordinal), "play shell must bind artifact lineage posture from the workspace-lite projection.");
    Assert(html.Contains("setText(\"workspace-recap-next\", payload.recapNextAction, \"No artifact next step is available yet.\");", StringComparison.Ordinal), "play shell must bind the next artifact-shelf step from the workspace-lite projection.");
    Assert(html.Contains("const selectedArtifactView = (payload.artifactShelfViews || []).find((item) => item.isSelected);", StringComparison.Ordinal), "play shell must derive the selected artifact shelf view from the workspace-lite projection.");
    Assert(html.Contains("document.getElementById(\"workspace-recap-view\").textContent = selectedArtifactView", StringComparison.Ordinal), "play shell must bind the selected artifact shelf view from the workspace-lite projection.");
    Assert(html.Contains("setLinkList(\"workspace-recap-views\", payload.artifactShelfViews);", StringComparison.Ordinal), "play shell must bind explicit artifact shelf browse links from the workspace-lite projection.");
    Assert(html.Contains("setText(\"workspace-artifact-shelf-summary\", payload.artifactShelfSelectionSummary, \"No mobile artifact shelf summary is available yet.\");", StringComparison.Ordinal), "play shell must bind the selected artifact shelf summary from the workspace-lite projection.");
    Assert(html.Contains("setText(\"workspace-artifact-selection\", payload.selectedRecapArtifactSummary, \"Selected recap artifact: no recap artifact is pinned yet.\");", StringComparison.Ordinal), "play shell must bind the selected recap artifact summary from the workspace-lite projection.");
    Assert(html.Contains("setLink(\"workspace-artifact-selection-link\", payload.selectedRecapArtifactHref || selectedArtifactView?.href, artifactId", StringComparison.Ordinal), "play shell must bind a direct selected recap artifact link from the workspace-lite projection.");
    Assert(html.Contains("? `Open recap artifact ${artifactId}`", StringComparison.Ordinal), "play shell must keep the selected recap artifact link label specific to the requested artifact.");
    Assert(html.Contains("document.getElementById(\"workspace-artifact-shelf-link\").href = selectedArtifactView?.href || \"/artifacts\";", StringComparison.Ordinal), "play shell must keep the selected artifact shelf link bound to the shelf browse target instead of reusing the recap deep link.");
    Assert(html.Contains("? `Browse ${selectedArtifactView.label}`", StringComparison.Ordinal), "play shell must keep the selected artifact shelf link label scoped to shelf browsing instead of the recap artifact identity.");
    Assert(html.Contains("setText(\"workspace-launch-primer\", payload.launchPrimerSummary, \"No starter primer summary is available yet.\");", StringComparison.Ordinal), "play shell must bind the starter-primer continuity summary from the workspace-lite projection.");
    Assert(html.Contains("setText(\"workspace-launch-primer-provenance\", payload.launchPrimerProvenanceSummary, \"No starter primer provenance is available yet.\");", StringComparison.Ordinal), "play shell must bind the starter-primer provenance from the workspace-lite projection.");
    Assert(html.Contains("setLink(\"workspace-launch-primer-link\", payload.launchPrimerHref, \"Open starter primer\", \"/artifacts\", \"Open starter primer\");", StringComparison.Ordinal), "play shell must bind a direct starter-primer artifact link from the workspace-lite projection.");
    Assert(html.Contains("setText(\"workspace-first-session-briefing\", payload.firstSessionBriefingSummary, \"No first-session briefing summary is available yet.\");", StringComparison.Ordinal), "play shell must bind the first-session briefing continuity summary from the workspace-lite projection.");
    Assert(html.Contains("setText(\"workspace-first-session-briefing-provenance\", payload.firstSessionBriefingProvenanceSummary, \"No first-session briefing provenance is available yet.\");", StringComparison.Ordinal), "play shell must bind the first-session briefing provenance from the workspace-lite projection.");
    Assert(html.Contains("setLink(\"workspace-first-session-briefing-link\", payload.firstSessionBriefingHref, \"Open first-session briefing\", \"/artifacts\", \"Open first-session briefing\");", StringComparison.Ordinal), "play shell must bind a direct first-session briefing artifact link from the workspace-lite projection.");
    Assert(html.Contains("setText(\"workspace-starter-artifact-continuity\", payload.starterArtifactContinuitySummary, \"No starter artifact continuity summary is available yet.\");", StringComparison.Ordinal), "play shell must bind the starter artifact continuity summary from the workspace-lite projection.");
    Assert(html.Contains("setList(\"workspace-starter-artifact-continuity-list\", payload.starterArtifactContinuityLabels);", StringComparison.Ordinal), "play shell must bind starter artifact continuity labels from the workspace-lite projection.");
    Assert(html.Contains("setText(\"workspace-runner-goal-updates\", payload.runnerGoalUpdatesSummary, \"No runner-goal update summary is available yet.\");", StringComparison.Ordinal), "play shell must bind the runner-goal update summary from the workspace-lite projection.");
    Assert(html.Contains("setList(\"workspace-runner-goal-update-list\", payload.runnerGoalUpdateLabels);", StringComparison.Ordinal), "play shell must bind runner-goal update labels from the workspace-lite projection.");
    Assert(html.Contains("setText(\"workspace-player-safe-consequence-feed\", payload.playerSafeConsequenceFeedSummary, \"No player-safe consequence feed summary is available yet.\");", StringComparison.Ordinal), "play shell must bind the player-safe consequence feed summary from the workspace-lite projection.");
    Assert(html.Contains("setList(\"workspace-player-safe-consequence-feed-list\", payload.playerSafeConsequenceFeedLabels);", StringComparison.Ordinal), "play shell must bind player-safe consequence feed labels from the workspace-lite projection.");
    Assert(html.Contains("setText(\"workspace-replay\", payload.replaySummary, \"No replay-safe package summary is available yet.\");", StringComparison.Ordinal), "play shell must bind replay-safe package summary from the workspace-lite projection.");
    Assert(html.Contains("setText(\"workspace-replay-audience\", payload.replayAudienceSummary, \"No replay artifact audience summary is available yet.\");", StringComparison.Ordinal), "play shell must bind replay artifact audience posture from the workspace-lite projection.");
    Assert(html.Contains("setText(\"workspace-replay-ownership\", payload.replayOwnershipSummary, \"No replay artifact ownership summary is available yet.\");", StringComparison.Ordinal), "play shell must bind replay artifact ownership posture from the workspace-lite projection.");
    Assert(html.Contains("setText(\"workspace-replay-publication\", payload.replayPublicationSummary, \"No replay artifact publication summary is available yet.\");", StringComparison.Ordinal), "play shell must bind replay artifact publication posture from the workspace-lite projection.");
    Assert(html.Contains("setText(\"workspace-replay-provenance\", payload.replayProvenanceSummary, \"No replay artifact provenance summary is available yet.\");", StringComparison.Ordinal), "play shell must bind replay artifact provenance posture from the workspace-lite projection.");
    Assert(html.Contains("setText(\"workspace-replay-audit\", payload.replayAuditSummary, \"No replay artifact audit summary is available yet.\");", StringComparison.Ordinal), "play shell must bind replay artifact audit posture from the workspace-lite projection.");
    Assert(html.Contains("setText(\"workspace-replay-lineage\", payload.replayLineageSummary, \"No replay artifact lineage summary is available yet.\");", StringComparison.Ordinal), "play shell must bind replay artifact lineage posture from the workspace-lite projection.");
    Assert(html.Contains("setText(\"workspace-replay-next\", payload.replayNextAction, \"No replay artifact next step is available yet.\");", StringComparison.Ordinal), "play shell must bind the replay artifact next step from the workspace-lite projection.");
    Assert(html.Contains("setText(\"workspace-legal-runner\", payload.legalRunnerSummary, \"No legal-runner summary is available yet.\");", StringComparison.Ordinal), "play shell must bind legal-runner proof from the workspace-lite projection.");
    Assert(html.Contains("setText(\"workspace-understandable-return\", payload.understandableReturnSummary, \"No understandable-return summary is available yet.\");", StringComparison.Ordinal), "play shell must bind understandable-return proof from the workspace-lite projection.");
    Assert(html.Contains("setText(\"workspace-campaign-ready\", payload.campaignReadySummary, \"No campaign-ready summary is available yet.\");", StringComparison.Ordinal), "play shell must bind campaign-ready proof from the workspace-lite projection.");
    Assert(html.Contains("setLink(\"workspace-recap-publication-link\", payload.recapPublicationHref, payload.recapNextAction, \"/account/work\", \"Artifact shelf follow-through\");", StringComparison.Ordinal), "play shell must bind the artifact-shelf follow-through route from the workspace-lite projection.");
    Assert(html.Contains("setLink(\"workspace-replay-publication-link\", payload.replayPublicationHref, payload.replayNextAction, \"/account/work\", \"Replay artifact follow-through\");", StringComparison.Ordinal), "play shell must bind the replay artifact follow-through route from the workspace-lite projection.");
    Assert(html.Contains("setText(\"workspace-continuity-rail\", payload.continuityRailSummary, \"No continuity rail summary is available yet.\");", StringComparison.Ordinal), "play shell must bind continuity rail summary from the workspace-lite projection.");
    Assert(html.Contains("setList(\"workspace-continuity-rail-list\", payload.continuityRailLabels);", StringComparison.Ordinal), "play shell must bind continuity rail labels from the workspace-lite projection.");
    Assert(html.Contains("setText(\"workspace-gm-ops\", payload.gmOperationsSummary, \"No GM operations summary is available yet.\");", StringComparison.Ordinal), "play shell must bind GM operations summary from the workspace-lite projection.");
    Assert(html.Contains("setList(\"workspace-gm-ops-list\", payload.gmOperationsLabels);", StringComparison.Ordinal), "play shell must bind GM operations lane labels from the workspace-lite projection.");
    Assert(html.Contains("setText(\"workspace-offline-truth\", payload.offlineTruthSummary, \"No offline truth summary is available yet.\");", StringComparison.Ordinal), "play shell must bind cached/stale/offline-action summary from the workspace-lite projection.");
    Assert(html.Contains("setList(\"workspace-offline-truth-list\", payload.offlineTruthLabels);", StringComparison.Ordinal), "play shell must bind cached/stale/offline-action labels from the workspace-lite projection.");
    Assert(html.Contains("setLink(\"follow-through-update-link\", payload.updateFollowThroughHref, payload.updateFollowThrough, \"/downloads\", \"Update follow-through\");", StringComparison.Ordinal), "play shell must bind update follow-through route to the workspace projection.");
    Assert(html.Contains("setLink(\"follow-through-support-link\", payload.supportFollowThroughHref, payload.supportFollowThrough, \"/contact\", \"Support follow-through\");", StringComparison.Ordinal), "play shell must bind support follow-through route to the workspace projection.");
    Assert(html.Contains("setLink(\"follow-through-role-link\", payload.roleFollowThroughHref, payload.roleFollowThrough, \"/\", \"Role follow-through\");", StringComparison.Ordinal), "play shell must bind role follow-through route to the workspace projection.");
    Assert(html.Contains("setLink(\"restore-follow-through-link\", payload.resumeFollowThroughHref, payload.resumeFollowThrough, \"/play\", \"Claimed-device follow-through\");", StringComparison.Ordinal), "play shell must bind claimed-device follow-through route to the restore projection.");
    Assert(html.Contains("setLink(\"restore-support-follow-through-link\", payload.supportFollowThroughHref, payload.supportFollowThrough, \"/contact\", \"Restore support follow-through\");", StringComparison.Ordinal), "play shell must bind restore support follow-through route to the restore projection.");
    Assert(html.Contains("setText(\"restore-starter-primer-follow-through\", payload.starterPrimerFollowThrough, \"No travel starter-primer follow-through is available yet.\");", StringComparison.Ordinal), "play shell must bind travel starter-primer follow-through from the restore projection.");
    Assert(html.Contains("setLink(\"restore-starter-primer-follow-through-link\", payload.starterPrimerFollowThroughHref, payload.starterPrimerFollowThrough, \"/artifacts\", \"Open travel starter primer\");", StringComparison.Ordinal), "play shell must bind the travel starter-primer route from the restore projection.");
    Assert(html.Contains("setText(\"restore-first-session-briefing-follow-through\", payload.firstSessionBriefingFollowThrough, \"No travel first-session briefing follow-through is available yet.\");", StringComparison.Ordinal), "play shell must bind travel first-session briefing follow-through from the restore projection.");
    Assert(html.Contains("setLink(\"restore-first-session-briefing-follow-through-link\", payload.firstSessionBriefingFollowThroughHref, payload.firstSessionBriefingFollowThrough, \"/artifacts\", \"Open travel first-session briefing\");", StringComparison.Ordinal), "play shell must bind the travel first-session briefing route from the restore projection.");
    Assert(html.Contains("setText(\"restore-offline-truth\", payload.offlineTruthSummary, \"No restore offline truth summary is available yet.\");", StringComparison.Ordinal), "play shell must bind restore cached/stale/offline-action summary from the restore projection.");
    Assert(html.Contains("setList(\"restore-offline-truth-labels\", payload.offlineTruthLabels);", StringComparison.Ordinal), "play shell must bind restore cached/stale/offline-action labels from the restore projection.");
    Assert(html.Contains("setText(\"restore-travel-companion\", payload.travelCompanionSummary, \"No restore travel companion summary is available yet.\");", StringComparison.Ordinal), "play shell must bind restore travel-companion continuity summary from the restore projection.");
    Assert(html.Contains("setList(\"restore-travel-companion-labels\", payload.travelCompanionLabels);", StringComparison.Ordinal), "play shell must bind restore travel-companion continuity labels from the restore projection.");
    Assert(html.Contains("const artifactView = params.get(\"artifactView\") || \"\";", StringComparison.Ordinal), "play shell must preserve the requested artifact shelf view from the shell query.");
    Assert(html.Contains("const artifactId = params.get(\"artifactId\") || \"\";", StringComparison.Ordinal), "play shell must preserve the requested recap artifact id from the shell query.");
    Assert(html.Contains("workspaceQuery.set(\"artifactView\", artifactView);", StringComparison.Ordinal), "play shell must round-trip the requested artifact shelf view into the workspace-lite query.");
    Assert(html.Contains("workspaceQuery.set(\"artifactId\", artifactId);", StringComparison.Ordinal), "play shell must round-trip the requested recap artifact id into the workspace-lite query.");
    Assert(html.Contains("renderShellHero(payload, restorePayload);", StringComparison.Ordinal), "play shell must render the hero shell from workspace and restore payloads before the dense detail surfaces.");
    Assert(html.Contains("renderWorkspace(payload, artifactId);", StringComparison.Ordinal), "play shell must render the selected recap artifact identity into the owned mobile shell.");
}

static async Task VerifyTurnCompanionProjectionStaysBoundedAndComputesOddsAsync()
{
    const string sessionId = "session-turn-projection";
    var app = PlayWebApplication.Build([]);

    try
    {
        var service = app.Services.GetRequiredService<PlayTurnCompanionService>();
        var projection = await service.GetProjectionAsync(sessionId, PlaySurfaceRole.Player);

        Assert(projection.CanMutate, "player turn companion projection must stay editable on the claimed player lane");
        Assert(projection.ShellSummary.Contains("Claimed player actor", StringComparison.Ordinal), "turn companion must keep the claimed player actor explicit");
        Assert(projection.LocalBoundarySummary.Contains("Install-local turn tracker", StringComparison.Ordinal), "turn companion must keep the local tracker boundary explicit");
        Assert(projection.Trust.StatusLabel.Length > 0, "turn companion must expose a named trust posture");
        Assert(projection.Trust.Labels.Any(item => item.Contains("Owner route:", StringComparison.Ordinal)), "turn companion trust posture must keep the owner route visible");
        Assert(projection.Now.StatCards.Any(item => item.MetricId == "physical"), "turn companion now surface must expose physical tracking");
        Assert(projection.Now.StatCards.Any(item => item.MetricId == "ammo"), "turn companion now surface must expose ammo tracking");
        Assert(projection.Act.Actions.Any(item => item.ActionId == "attack"), "turn companion act surface must expose a bounded attack rail");
        Assert(projection.Adjust.Options.Any(item => item.ModifierId == "cover"), "turn companion adjust surface must expose source-backed modifier toggles");
        Assert(projection.Resolve.Odds.Count == 4, "turn companion resolve surface must expose fast odds badges");
        Assert(projection.Resolve.OddsSummary.Contains("1+ hit", StringComparison.Ordinal), "turn companion resolve surface must expose 1+ hit odds");
        Assert(projection.Runsite.TruthPosture.Contains("orientation-only", StringComparison.OrdinalIgnoreCase), "turn companion runsite surface must preserve orientation-only truth");
        Assert(projection.Sync.QuickActions.Any(item => item.ActionId == "player-mark-ready"), "turn companion sync surface must expose replay-safe player quick actions");
        Assert(projection.Sync.PendingSummary.Contains("Server replay queue", StringComparison.Ordinal), "turn companion sync surface must explain the replay queue posture");
    }
    finally
    {
        await app.DisposeAsync();
    }
}

static async Task VerifyTurnCompanionPlayerProjectionCoversRequestedLiveTrackersAsync()
{
    const string sessionId = "session-turn-player-trackers";
    var app = PlayWebApplication.Build([]);

    try
    {
        var service = app.Services.GetRequiredService<PlayTurnCompanionService>();
        var projection = await service.GetProjectionAsync(sessionId, PlaySurfaceRole.Player, "player-shell-trackers");

        string[] expectedStatCards = ["physical", "stun", "edge", "ammo", "reserve", "charges"];
        foreach (string metricId in expectedStatCards)
        {
            Assert(
                projection.Now.StatCards.Any(item => item.MetricId == metricId),
                $"player turn companion must expose the {metricId} live-session tracker");
        }

        Assert(
            projection.Now.InventoryCards.Any(item => item.ItemId == "stim-patch"),
            "player turn companion must expose mission-critical consumables in the inventory rail");
        Assert(
            projection.Now.InventoryCards.Any(item => item.ItemId == "medkit"),
            "player turn companion must expose the medkit in the inventory rail");
        Assert(
            projection.Now.InventoryCards.Any(item => item.ItemId == "flashbang"),
            "player turn companion must expose bounded tactical inventory instead of a full stash manager");
        Assert(
            projection.Now.InventoryCards.All(item => item.Detail.Contains("Mission-critical inventory only.", StringComparison.Ordinal)),
            "player turn companion inventory rail must stay bounded to in-session essentials");

        string[] expectedActions = ["attack", "reload", "use-consumable", "cast-or-sustain"];
        foreach (string actionId in expectedActions)
        {
            Assert(
                projection.Act.Actions.Any(item => item.ActionId == actionId),
                $"player turn companion must expose the bounded {actionId} action");
        }

        string[] expectedModifiers = ["cover", "wound", "recoil", "visibility", "aim", "sustained"];
        foreach (string modifierId in expectedModifiers)
        {
            Assert(
                projection.Adjust.Options.Any(item => item.ModifierId == modifierId),
                $"player turn companion must expose the {modifierId} modifier toggle");
        }

        Assert(
            projection.Resolve.ManualEntryHint.Contains("hit count", StringComparison.OrdinalIgnoreCase)
            && projection.Resolve.ManualEntryHint.Contains("digital resolver", StringComparison.OrdinalIgnoreCase),
            "player turn companion must support manual roll entry on the resolve surface");
        Assert(
            projection.Resolve.OddsSummary.Contains("1+ hit", StringComparison.Ordinal),
            "player turn companion must show fast odds before rolling");
        Assert(
            projection.History.Entries.First().Detail.Contains("do not replace engine or GM authority", StringComparison.Ordinal),
            "player turn companion history seed must keep the shell explicitly bounded away from full character-builder authority");
        Assert(
            projection.Runsite.Anchors.Any(item => item.AnchorId == "server-room"),
            "player turn companion must expose optional RUNSITE room or zone anchors");
        Assert(
            projection.Sync.ClaimedDeviceSummary.Contains("bounded turn companion", StringComparison.OrdinalIgnoreCase),
            "player turn companion sync surface must keep the claimed-device shell boundary explicit");
    }
    finally
    {
        await app.DisposeAsync();
    }
}

static async Task VerifyTurnCompanionGmProjectionStaysBoundedAndRoleSpecificAsync()
{
    const string sessionId = "session-turn-gm-projection";
    var app = PlayWebApplication.Build([]);

    try
    {
        var service = app.Services.GetRequiredService<PlayTurnCompanionService>();
        var projection = await service.GetProjectionAsync(sessionId, PlaySurfaceRole.GameMaster, "gm-shell-main");

        Assert(projection.Role == PlaySurfaceRole.GameMaster, "gm turn companion projection must preserve the gm role");
        Assert(projection.CanMutate, "gm turn companion projection must stay editable on the claimed GM lane");
        Assert(projection.Now.ActorLabel.Contains("GM focus actor", StringComparison.Ordinal), "gm turn companion must keep the GM focus actor explicit");
        Assert(projection.Trust.Labels.Any(item =>
            item.Contains("/play/", StringComparison.Ordinal)
            && item.Contains("role=GameMaster", StringComparison.Ordinal)), "gm turn companion trust posture must keep the role-concrete GM owner route visible");
        Assert(projection.Act.Actions.Any(item => item.ActionId == "advance-initiative"), "gm turn companion action rail must expose advance-initiative");
        Assert(projection.Act.Actions.Any(item => item.ActionId == "reveal-threat"), "gm turn companion action rail must expose reveal-threat");
        Assert(projection.Act.Actions.All(item => item.ActionId != "use-consumable"), "gm turn companion action rail must not expose player-only consumable actions");
        Assert(projection.Resolve.SelectedActionLabel.Contains("Advance Initiative", StringComparison.Ordinal), "gm turn companion must default to a bounded GM action");
        Assert(projection.Sync.QuickActions.Any(item => item.ActionId == "gm-advance-initiative"), "gm turn companion sync surface must expose the GM initiative quick action");
        Assert(projection.Sync.QuickActions.Any(item => item.ActionId == "gm-publish-spider-card"), "gm turn companion sync surface must expose the bounded spider-card quick action");
        Assert(projection.Runsite.TruthPosture.Contains("orientation-only", StringComparison.OrdinalIgnoreCase), "gm turn companion RUNSITE posture must stay orientation-only");
    }
    finally
    {
        await app.DisposeAsync();
    }
}

static async Task VerifyTurnCompanionDigitalResolveProducesBoundedReceiptAsync()
{
    const string sessionId = "session-turn-digital";
    var app = PlayWebApplication.Build([]);

    try
    {
        var service = app.Services.GetRequiredService<PlayTurnCompanionService>();
        var afterResolve = await service.ResolveActionAsync(
            sessionId,
            PlaySurfaceRole.Player,
            new PlayTurnResolveRequest(UseManualEntry: false, ManualHits: null, ManualGlitch: false));

        PlayTurnStatCard ammoCard = afterResolve.Now.StatCards.First(item => item.MetricId == "ammo");
        Assert(ammoCard.Value == 9, "digital attack resolution must spend the bounded attack ammo cost");
        Assert(afterResolve.Resolve.LastOutcomeSummary.Contains("via digital entry", StringComparison.Ordinal), "digital resolution receipt must record the digital dice path");
        Assert(afterResolve.Resolve.LastOutcomeSummary.Contains("Magazine 12 -> 9", StringComparison.Ordinal), "digital resolution receipt must keep the ammo delta explicit");
        Assert(afterResolve.History.Entries.First().Title.Contains("resolved", StringComparison.OrdinalIgnoreCase), "digital resolution must create a latest history receipt");
        Assert(!afterResolve.History.Entries.First().Manual, "digital resolution receipt must stay distinct from manual entry");
    }
    finally
    {
        await app.DisposeAsync();
    }
}

static async Task VerifyTurnCompanionManualResolveUpdatesHistoryAndAmmoAsync()
{
    const string sessionId = "session-turn-manual";
    var app = PlayWebApplication.Build([]);

    try
    {
        var service = app.Services.GetRequiredService<PlayTurnCompanionService>();

        await service.ToggleModifierAsync(sessionId, PlaySurfaceRole.Player, "cover", enabled: true);
        var beforeResolve = await service.GetProjectionAsync(sessionId, PlaySurfaceRole.Player);
        Assert(beforeResolve.Resolve.DicePool == 14, "turn companion must raise the attack pool when cover is enabled");

        var afterResolve = await service.ResolveActionAsync(
            sessionId,
            PlaySurfaceRole.Player,
            new PlayTurnResolveRequest(UseManualEntry: true, ManualHits: 3, ManualGlitch: false));

        PlayTurnStatCard ammoCard = afterResolve.Now.StatCards.First(item => item.MetricId == "ammo");
        Assert(ammoCard.Value == 9, "manual attack resolution must spend the bounded attack ammo cost");
        Assert(afterResolve.Resolve.LastOutcomeSummary.Contains("3 hit(s)", StringComparison.Ordinal), "manual resolution receipt must preserve the entered hit count");
        Assert(afterResolve.History.Entries.First().Title.Contains("resolved", StringComparison.OrdinalIgnoreCase), "manual resolution must create a latest history receipt");
        Assert(afterResolve.History.Entries.First().Detail.Contains("Magazine 12 -> 9", StringComparison.Ordinal), "manual resolution receipt must keep the local ammo delta explicit");
    }
    finally
    {
        await app.DisposeAsync();
    }
}

static async Task VerifyTurnCompanionObserverStaysReadOnlyAsync()
{
    const string sessionId = "session-turn-observer";
    var app = PlayWebApplication.Build([]);

    try
    {
        var service = app.Services.GetRequiredService<PlayTurnCompanionService>();
        var observerProjection = await service.GetProjectionAsync(sessionId, PlaySurfaceRole.Observer);
        Assert(!observerProjection.CanMutate, "observer turn companion projection must stay read-only");

        var unchangedProjection = await service.AdjustMetricAsync(sessionId, PlaySurfaceRole.Observer, "ammo", -3);
        PlayTurnStatCard ammoCard = unchangedProjection.Now.StatCards.First(item => item.MetricId == "ammo");
        Assert(ammoCard.Value == 12, "observer turn companion must not mutate local ammo tracking");
        Assert(unchangedProjection.Runsite.Summary.Contains("RUNSITE anchor", StringComparison.Ordinal), "observer turn companion must still expose bounded runsite context");
    }
    finally
    {
        await app.DisposeAsync();
    }
}

static async Task VerifyTurnCompanionClaimedDeviceStateIsolationAsync()
{
    const string sessionId = "session-turn-device-isolation";
    var app = PlayWebApplication.Build([]);

    try
    {
        var service = app.Services.GetRequiredService<PlayTurnCompanionService>();
        const string deviceA = "player-shell-alpha";
        const string deviceB = "player-shell-bravo";

        var deviceAProjection = await service.AdjustMetricAsync(
            sessionId,
            PlaySurfaceRole.Player,
            "ammo",
            -2,
            deviceA);
        var deviceBProjection = await service.GetProjectionAsync(
            sessionId,
            PlaySurfaceRole.Player,
            deviceB);
        var deviceAReloaded = await service.GetProjectionAsync(
            sessionId,
            PlaySurfaceRole.Player,
            deviceA);

        Assert(
            deviceAProjection.Now.StatCards.First(item => item.MetricId == "ammo").Value == 10,
            "claimed device A must keep its own local ammo counter");
        Assert(
            deviceBProjection.Now.StatCards.First(item => item.MetricId == "ammo").Value == 12,
            "claimed device B must not inherit device A local ammo changes");
        Assert(
            deviceAReloaded.Now.StatCards.First(item => item.MetricId == "ammo").Value == 10,
            "claimed device A must reload its own persisted local ammo changes");
    }
    finally
    {
        await app.DisposeAsync();
    }
}

static async Task VerifyTurnCompanionRunsiteAnchorSelectionStaysDeviceScopedAsync()
{
    const string sessionId = "session-turn-anchor";
    var app = PlayWebApplication.Build([]);

    try
    {
        var service = app.Services.GetRequiredService<PlayTurnCompanionService>();
        const string deviceA = "player-anchor-alpha";
        const string deviceB = "player-anchor-bravo";

        var baselineProjection = await service.GetProjectionAsync(
            sessionId,
            PlaySurfaceRole.Player,
            deviceA);
        var serverRoomProjection = await service.SelectAnchorAsync(
            sessionId,
            PlaySurfaceRole.Player,
            "server-room",
            deviceA);
        var deviceAReloaded = await service.GetProjectionAsync(
            sessionId,
            PlaySurfaceRole.Player,
            deviceA);
        var deviceBProjection = await service.GetProjectionAsync(
            sessionId,
            PlaySurfaceRole.Player,
            deviceB);
        var observerProjection = await service.SelectAnchorAsync(
            sessionId,
            PlaySurfaceRole.Observer,
            "fire-stairs",
            "observer-anchor");

        Assert(
            baselineProjection.Runsite.SelectedAnchorId == "front-door",
            "turn companion must seed a default RUNSITE anchor on a fresh claimed-device lane");
        Assert(
            serverRoomProjection.Runsite.SelectedAnchorId == "server-room",
            "turn companion must keep the selected RUNSITE anchor on the claimed device");
        Assert(
            serverRoomProjection.Runsite.Summary.Contains("Server Room", StringComparison.Ordinal),
            "turn companion runsite summary must name the selected room/zone anchor");
        Assert(
            serverRoomProjection.Runsite.TruthPosture.Contains("orientation-only", StringComparison.OrdinalIgnoreCase),
            "turn companion RUNSITE selection must stay bounded to orientation-only truth");
        Assert(
            serverRoomProjection.LocalRevision == baselineProjection.LocalRevision + 1,
            "turn companion RUNSITE anchor selection must advance the local revision on the claimed device");
        Assert(
            deviceAReloaded.Runsite.SelectedAnchorId == "server-room",
            "turn companion must reload the selected RUNSITE anchor for the same claimed device");
        Assert(
            deviceBProjection.Runsite.SelectedAnchorId == "front-door",
            "turn companion must not leak one device's RUNSITE anchor selection into a different claimed device");
        Assert(
            observerProjection.Runsite.SelectedAnchorId == "front-door",
            "observer turn companion must stay read-only even when a RUNSITE anchor mutation is requested");
    }
    finally
    {
        await app.DisposeAsync();
    }
}

static async Task VerifyTurnCompanionReplayQueueRoundTripsAsync()
{
    const string sessionId = "session-turn-replay";
    var app = PlayWebApplication.Build([]);

    try
    {
        var service = app.Services.GetRequiredService<PlayTurnCompanionService>();
        var replayResponse = await service.ReplayClientQueueAsync(
            sessionId,
            PlaySurfaceRole.Player,
            ["turn:metric:ammo:-1", "quick-action:player-mark-ready"]);

        Assert(replayResponse.Accepted, "turn companion replay route must accept bounded local receipts");
        Assert(replayResponse.AcceptedEventCount == 2, "turn companion replay route must count accepted local receipts");
        Assert(replayResponse.PendingQueueCount == 2, "turn companion replay route must increase the server queue count");
        Assert(replayResponse.Sync.CanAcknowledgeServerQueue, "turn companion replay route must expose queue acknowledgement once server events are pending");

        var acknowledgeResponse = await service.AcknowledgePendingQueueAsync(
            sessionId,
            PlaySurfaceRole.Player);

        Assert(acknowledgeResponse.Accepted, "turn companion acknowledgement route must accept current queued events");
        Assert(acknowledgeResponse.AcceptedEventCount == 2, "turn companion acknowledgement route must report the acknowledged event count");
        Assert(acknowledgeResponse.PendingQueueCount == 0, "turn companion acknowledgement route must clear the server queue count");
        Assert(!acknowledgeResponse.Sync.CanAcknowledgeServerQueue, "turn companion acknowledgement route must disable server queue acknowledgement once empty");
    }
    finally
    {
        await app.DisposeAsync();
    }
}

static async Task VerifyTurnCompanionRouteRendersBlazorShellAsync()
{
    var app = PlayWebApplication.Build([]);

    try
    {
        var response = await ExecuteRouteBodyResponseAsync(
            app,
            HttpMethod.Get,
            "/mobile",
            "?sessionId=session-turn-route&role=Player&deviceId=player-shell-route");

        Assert(response.StatusCode == StatusCodes.Status200OK, "mobile turn companion route must return a normal page response");
        Assert(response.Body.Contains("Live-session turn companion", StringComparison.Ordinal), "mobile turn companion route must render the bounded shell headline");
        Assert(response.Body.Contains("Choose one bounded action", StringComparison.Ordinal), "mobile turn companion route must render the action rail");
        Assert(response.Body.Contains("Source-backed modifier stack", StringComparison.Ordinal), "mobile turn companion route must render the modifier surface");
        Assert(response.Body.Contains("Replay and acknowledge with intent", StringComparison.Ordinal), "mobile turn companion route must render the reconnect and replay surface");
        Assert(response.Body.Contains("Recent deltas and queued receipts", StringComparison.Ordinal), "mobile turn companion route must render the history surface");
        Assert(response.Body.Contains("Claimed Device", StringComparison.Ordinal), "mobile turn companion route must render the claimed-device continuity card");
        Assert(response.Body.Contains("turn-claim-device-button", StringComparison.Ordinal), "mobile turn companion route must render the continuity claim action");
        Assert(response.Body.Contains("turn-owner-route-link", StringComparison.Ordinal), "mobile turn companion route must render the owner-route follow-through");
        Assert(response.Body.Contains("turn-install-status", StringComparison.Ordinal), "mobile turn companion route must render the install-boundary status surface");
        Assert(response.Body.Contains("turn-install-button", StringComparison.Ordinal), "mobile turn companion route must render the direct install action");
        Assert(response.Body.Contains("turn-install-detail", StringComparison.Ordinal), "mobile turn companion route must render install guidance beside the action");
        Assert(response.Body.Contains("turn-jump-nav", StringComparison.Ordinal), "mobile turn companion route must render the quick jump rail for handheld play.");
        Assert(response.Body.Contains("turn-glance-grid", StringComparison.Ordinal), "mobile turn companion route must render the quick-glance tracker strip for handheld play.");
        Assert(response.Body.Contains("turn-now-card", StringComparison.Ordinal), "mobile turn companion route must keep the live tracker card as a direct handheld anchor target.");
        Assert(response.Body.Contains("data-device-id=\"player-shell-route\"", StringComparison.Ordinal), "mobile turn companion route must preserve the claimed-device id on the shell root");
        Assert(response.Body.Contains("/mobile.css", StringComparison.Ordinal), "mobile turn companion route must bind the dedicated mobile stylesheet");
        Assert(response.Body.Contains("/mobile-turn-companion.js", StringComparison.Ordinal), "mobile turn companion route must load the dedicated client runtime.");
        Assert(response.Body.Contains("turn-companion-bootstrap", StringComparison.Ordinal), "mobile turn companion route must emit a client bootstrap payload.");
        Assert(!response.Body.Contains("_framework/blazor.web.js", StringComparison.Ordinal), "mobile turn companion route must render without the Blazor interactive framework script.");
    }
    finally
    {
        await app.DisposeAsync();
    }
}

static async Task VerifyTurnCompanionClientRuntimeKeepsClaimedDeviceContinuityContractAsync()
{
    var scriptPath = Path.Combine(GetRepoRoot(), "src", "Chummer.Play.Web", "wwwroot", "mobile-turn-companion.js");
    var script = await File.ReadAllTextAsync(scriptPath);

    Assert(script.Contains("function claimedTurnRoute(sessionId, roleName, deviceId)", StringComparison.Ordinal), "mobile turn companion runtime must keep a claimed-device owner-route helper.");
    Assert(script.Contains("function observeRoute(sessionId)", StringComparison.Ordinal), "mobile turn companion runtime must keep a continuity observe-route helper.");
    Assert(script.Contains("function setButtonText(id, text)", StringComparison.Ordinal), "mobile turn companion runtime must keep a continuity button-text helper.");
    Assert(script.Contains("function setLink(id, href, text)", StringComparison.Ordinal), "mobile turn companion runtime must keep an owner-route link helper.");
    Assert(script.Contains("continuityPayload: client.continuityPayload", StringComparison.Ordinal), "mobile turn companion runtime must persist claimed-device continuity inside the local snapshot.");
    Assert(script.Contains("serviceWorkerStatus: client.serviceWorkerStatus", StringComparison.Ordinal), "mobile turn companion runtime must persist install-boundary status inside the local snapshot.");
    Assert(script.Contains("case \"install-shell\":", StringComparison.Ordinal), "mobile turn companion runtime must expose a direct install-shell action.");
    Assert(script.Contains("function renderInstallSurface(client)", StringComparison.Ordinal), "mobile turn companion runtime must render a dedicated install surface.");
    Assert(script.Contains("function installShell(client)", StringComparison.Ordinal), "mobile turn companion runtime must handle direct install requests from the mobile shell.");
    Assert(script.Contains("await promptEvent.prompt();", StringComparison.Ordinal), "mobile turn companion runtime must drive the deferred browser install prompt when it is available.");
    Assert(script.Contains("window.matchMedia(\"(display-mode: standalone)\")", StringComparison.Ordinal), "mobile turn companion runtime must detect installed standalone display mode.");
    Assert(script.Contains("var lastRouteKey = storagePrefix + \"last-route\";", StringComparison.Ordinal), "mobile turn companion runtime must keep a dedicated last-route storage key for installed relaunch.");
    Assert(script.Contains("var resumeRoute = resolveResumeRoute(params);", StringComparison.Ordinal), "mobile turn companion runtime must derive resume posture from the current launch parameters.");
    Assert(script.Contains("function resolveResumeRoute(params)", StringComparison.Ordinal), "mobile turn companion runtime must resolve generic and role-aware resume behavior.");
    Assert(script.Contains("function lastRouteKeyForRole(roleName)", StringComparison.Ordinal), "mobile turn companion runtime must keep per-role last-route keys.");
    Assert(script.Contains("writeStoredValue(lastRouteKeyForRole(client.roleName), payload);", StringComparison.Ordinal), "mobile turn companion runtime must persist a role-specific last route.");
    Assert(script.Contains("function shouldPersistGlobalLastRoute()", StringComparison.Ordinal), "mobile turn companion runtime must keep a visibility-aware guard for the install-wide resume route.");
    Assert(script.Contains("document.visibilityState === \"visible\" || document.hasFocus()", StringComparison.Ordinal), "mobile turn companion runtime must keep background tabs from stealing the global last-route resume lane.");
    Assert(script.Contains("resumeSource: \"session-only\"", StringComparison.Ordinal), "mobile turn companion runtime must support role-shortcut session restore even before that role has a local device lane.");
    Assert(script.Contains("function saveLastRoute(client)", StringComparison.Ordinal), "mobile turn companion runtime must persist the last live-session lane for relaunch.");
    Assert(script.Contains("Resumed the last claimed-device route for this install.", StringComparison.Ordinal), "mobile turn companion runtime must surface when an installed launch resumed the last lane.");
    Assert(script.Contains("Resumed the last \" + resumeRoute.roleName + \" claimed-device route for this install.", StringComparison.Ordinal), "mobile turn companion runtime must surface role-aware relaunch status.");
    Assert(script.Contains("Resuming \" + resumeRoute.sessionId + \" in the \" + resumeRoute.roleName + \" lane on this install.", StringComparison.Ordinal), "mobile turn companion runtime must explain when a role shortcut resumes the session on a fresh role-specific lane.");
    Assert(script.Contains("function renderQuickGlance(client)", StringComparison.Ordinal), "mobile turn companion runtime must keep the quick-glance tracker strip synchronized with live local state.");
    Assert(script.Contains("setText(\"turn-glance-ammo\", String(statValue(projection, \"ammo\")));", StringComparison.Ordinal), "mobile turn companion runtime must surface the current magazine value in the quick-glance strip.");
    Assert(script.Contains("Refreshing trust, queue, and claimed-device posture for this shell.", StringComparison.Ordinal), "mobile turn companion runtime must refresh claimed-device posture on a normal online load.");
    Assert(script.Contains("setButtonDisabled(\"turn-claim-device-button\", client.networkBusy || !navigator.onLine || !hasContinuityCursor);", StringComparison.Ordinal), "mobile turn companion runtime must keep the claim action disabled until continuity posture is ready.");
    Assert(script.Contains("navigator.serviceWorker.register(\"/service-worker.js\", { scope: \"/\" })", StringComparison.Ordinal), "mobile turn companion runtime must register the shared service worker when the mobile shell opens directly.");
    Assert(script.Contains("window.addEventListener(\"beforeinstallprompt\"", StringComparison.Ordinal), "mobile turn companion runtime must listen for install-prompt availability on the direct mobile shell.");
}

static async Task VerifyTurnCompanionManifestTargetsDirectMobilePwaAsync()
{
    var manifestPath = Path.Combine(GetRepoRoot(), "src", "Chummer.Play.Web", "wwwroot", "manifest.webmanifest");
    var manifestText = await File.ReadAllTextAsync(manifestPath);
    using JsonDocument manifest = JsonDocument.Parse(manifestText);
    JsonElement root = manifest.RootElement;

    Assert(root.TryGetProperty("id", out JsonElement idElement), "mobile manifest must declare a stable app id.");
    Assert(string.Equals(idElement.GetString(), "/mobile", StringComparison.Ordinal), "mobile manifest id must target the direct turn companion shell.");
    Assert(root.TryGetProperty("start_url", out JsonElement startUrlElement), "mobile manifest must declare a start_url.");
    Assert(string.Equals(startUrlElement.GetString(), "/mobile", StringComparison.Ordinal), "mobile manifest start_url must launch the generic mobile shell so installed relaunch can resume the last claimed-device lane.");
    Assert(root.TryGetProperty("shortcuts", out JsonElement shortcutsElement) && shortcutsElement.ValueKind == JsonValueKind.Array, "mobile manifest must expose direct launch shortcuts.");
    Assert(shortcutsElement.GetArrayLength() >= 2, "mobile manifest must expose both player and GM launch shortcuts.");
    Assert(shortcutsElement.EnumerateArray().Any(item => string.Equals(item.GetProperty("url").GetString(), "/mobile?role=Player", StringComparison.Ordinal)), "mobile manifest must keep a direct player companion shortcut.");
    Assert(shortcutsElement.EnumerateArray().Any(item => string.Equals(item.GetProperty("url").GetString(), "/mobile?role=GameMaster", StringComparison.Ordinal)), "mobile manifest must keep a direct GM companion shortcut.");
    Assert(root.TryGetProperty("icons", out JsonElement iconsElement) && iconsElement.ValueKind == JsonValueKind.Array, "mobile manifest must declare install icons.");
    Assert(iconsElement.EnumerateArray().Any(item =>
        string.Equals(item.GetProperty("src").GetString(), "/icons/icon-192.png", StringComparison.Ordinal)
        && string.Equals(item.GetProperty("type").GetString(), "image/png", StringComparison.Ordinal)),
        "mobile manifest must expose a 192px PNG install icon.");
    Assert(iconsElement.EnumerateArray().Any(item =>
        string.Equals(item.GetProperty("src").GetString(), "/icons/icon-512.png", StringComparison.Ordinal)
        && string.Equals(item.GetProperty("type").GetString(), "image/png", StringComparison.Ordinal)),
        "mobile manifest must expose a 512px PNG install icon.");
}

static async Task VerifyTurnCompanionAppShellDeclaresMobileInstallMetadataAsync()
{
    var appPath = Path.Combine(GetRepoRoot(), "src", "Chummer.Play.Web", "Components", "App.razor");
    var appShell = await File.ReadAllTextAsync(appPath);

    Assert(appShell.Contains("<meta name=\"theme-color\" content=\"#0f1b26\" />", StringComparison.Ordinal), "mobile app shell must declare the PWA theme color.");
    Assert(appShell.Contains("<meta name=\"apple-mobile-web-app-capable\" content=\"yes\" />", StringComparison.Ordinal), "mobile app shell must declare Apple standalone capability.");
    Assert(appShell.Contains("<meta name=\"apple-mobile-web-app-status-bar-style\" content=\"black-translucent\" />", StringComparison.Ordinal), "mobile app shell must declare Apple status-bar posture.");
    Assert(appShell.Contains("<meta name=\"apple-mobile-web-app-title\" content=\"Chummer Play\" />", StringComparison.Ordinal), "mobile app shell must declare an Apple install title.");
    Assert(appShell.Contains("<link rel=\"apple-touch-icon\" href=\"/icons/apple-touch-icon.png\" />", StringComparison.Ordinal), "mobile app shell must declare the Apple touch icon.");
}

static async Task VerifyTurnCompanionRealHostPipelineUsesAntiforgeryAsync()
{
    var applicationPath = Path.Combine(GetRepoRoot(), "src", "Chummer.Play.Web", "PlayWebApplication.cs");
    var source = await File.ReadAllTextAsync(applicationPath);

    Assert(source.Contains("app.UseAntiforgery();", StringComparison.Ordinal), "mobile real-host pipeline must enable antiforgery middleware before the Razor Components endpoint.");
}

static Task VerifyBootstrapRoleShellEntryPointsAsync()
{
    var playerDescriptor = PlayerShellModule.CreateDescriptor();
    var gmDescriptor = GmTacticalShellModule.CreateDescriptor();
    var playerCapabilities = PlayRouteHandlers.ResolveRoleCapabilities(
        PlayRouteHandlers.ToSnapshot(playerDescriptor)
    );
    var gmCapabilities = PlayRouteHandlers.ResolveRoleCapabilities(
        PlayRouteHandlers.ToSnapshot(gmDescriptor)
    );
    var playerActions = PlayRouteHandlers.BuildQuickActions(PlaySurfaceRole.Player, playerCapabilities);
    var gmActions = PlayRouteHandlers.BuildQuickActions(PlaySurfaceRole.GameMaster, gmCapabilities);

    Assert(playerDescriptor.Summary.Contains("player table cards", StringComparison.OrdinalIgnoreCase), "player bootstrap entry points must describe the player table-card lane explicitly");
    Assert(playerDescriptor.Summary.Contains("between-turn", StringComparison.OrdinalIgnoreCase), "player bootstrap entry points must describe between-turn affordances explicitly");
    Assert(gmDescriptor.Summary.Contains("GM-lite continuity", StringComparison.Ordinal), "gm bootstrap entry points must describe GM-lite continuity explicitly");
    Assert(playerActions.Count > 0, "player bootstrap entry points must expose quick actions");
    Assert(gmActions.Count > 0, "gm bootstrap entry points must expose quick actions");
    Assert(playerActions.All(action => !action.ActionId.StartsWith("gm-", StringComparison.Ordinal)), "player role entry points must not expose gm quick actions");
    Assert(gmActions.All(action => !action.ActionId.StartsWith("player-", StringComparison.Ordinal)), "gm role entry points must not expose player quick actions");
    Assert(playerActions.All(action => !string.IsNullOrWhiteSpace(action.Label)), "player role entry points must expose non-empty action labels");
    Assert(gmActions.All(action => !string.IsNullOrWhiteSpace(action.Label)), "gm role entry points must expose non-empty action labels");
    return Task.CompletedTask;
}

static async Task VerifyObserverBootstrapAndResumeStayReadMostlyAsync()
{
    const string sessionId = "session-observer-bootstrap";
    var app = PlayWebApplication.Build([]);

    try
    {
        var store = app.Services.GetRequiredService<BrowserSessionEventLogStore>();
        var cache = app.Services.GetRequiredService<BrowserSessionOfflineCacheService>();
        await store.AppendPendingEventsAsync(sessionId, "scene-observer", "scene-r5", "runtime-observer", ["evt-observer"], 6);
        await cache.SetCheckpointAsync(
            new SyncCheckpoint(sessionId, "scene-observer", "scene-r5", "runtime-observer", 6, DateTimeOffset.UtcNow)
        );
        await cache.CacheRuntimeBundleAsync(sessionId, "runtime-observer", "scene-r5", "bundle:scene-observer:runtime-observer");

        var bootstrapQuery = $"?sessionId={Uri.EscapeDataString(sessionId)}&role={Uri.EscapeDataString(PlaySurfaceRole.Observer.ToString())}&sceneId=scene-observer&sceneRevision=scene-r5&runtimeFingerprint=runtime-observer";
        var bootstrap = await ExecuteRouteRequestAsync<PlayBootstrapResponse>(
            app,
            HttpMethod.Get,
            PlayApiRoutes.Bootstrap,
            bootstrapQuery,
            expectedStatusCode: StatusCodes.Status200OK
        );

        Assert(bootstrap.ActiveShell.Role == PlaySurfaceRole.Observer, "observer bootstrap must project an observer shell role");
        Assert(bootstrap.ActiveShell.ShellName == "Observer Shell", "observer bootstrap must name the observer shell explicitly");
        Assert(bootstrap.ActiveShell.Summary.Contains("Read-mostly", StringComparison.Ordinal), "observer bootstrap must describe the read-mostly posture");
        Assert(bootstrap.RoleCapabilities.SequenceEqual(["play.session.read"]), "observer bootstrap must expose only read-mostly capabilities");
        Assert(bootstrap.ActiveShell.RequiredCapabilities.SequenceEqual(["play.session.read"]), "observer shell metadata must stay read-only");
        Assert(bootstrap.AvailableShells.Count == 1 && bootstrap.AvailableShells[0].Role == PlaySurfaceRole.Observer, "observer bootstrap must keep the available shell list observer-scoped");
        Assert(bootstrap.QuickActions.Count == 0, "observer bootstrap must not expose quick actions");
        Assert(bootstrap.TacticalSpiderCards.Count == 0, "observer bootstrap must not expose GM spider cards");
        Assert(bootstrap.CoachHints.Select(hint => hint.HintId).SequenceEqual(["coach-observer-continuity", "coach-observer-readonly"]), "observer bootstrap must expose observer-specific coach hints");

        var resume = await ExecuteRouteRequestAsync<PlayResumeResponse>(
            app,
            HttpMethod.Get,
            PlayApiRoutes.Resume,
            $"?role={Uri.EscapeDataString(PlaySurfaceRole.Observer.ToString())}",
            routeValues: new Dictionary<string, string> { ["sessionId"] = sessionId },
            expectedStatusCode: StatusCodes.Status200OK
        );

        Assert(resume.Role == PlaySurfaceRole.Observer, "observer resume must preserve the observer role");
        Assert(resume.Bootstrap.ActiveShell.Role == PlaySurfaceRole.Observer, "observer resume must keep observer shell metadata");
        Assert(resume.Bootstrap.RoleCapabilities.SequenceEqual(["play.session.read"]), "observer resume must keep read-only capabilities");
        Assert(resume.Bootstrap.QuickActions.Count == 0, "observer resume must not surface player or gm quick actions");
        Assert(resume.Bootstrap.CoachHints.Select(hint => hint.HintId).SequenceEqual(["coach-observer-continuity", "coach-observer-readonly"]), "observer resume must keep observer-specific coach hints");
        Assert(resume.Bootstrap.Projection.Timeline.Contains("pending:evt-observer", StringComparer.Ordinal), "observer resume must preserve pending replay state");
        Assert(resume.DeepLinkOwnerRoute == PlayRouteHandlers.BuildOwnerRoute(sessionId, PlaySurfaceRole.Observer), "observer resume must return a concrete observer owner route");
    }
    finally
    {
        await app.DisposeAsync();
    }
}

static async Task VerifyResumeAndWorkspaceLiteRoutesStayRoleConcreteAsync()
{
    const string sessionId = "session-role-routes";
    var app = PlayWebApplication.Build([]);

    try
    {
        var store = app.Services.GetRequiredService<BrowserSessionEventLogStore>();
        var cache = app.Services.GetRequiredService<BrowserSessionOfflineCacheService>();
        await store.AppendPendingEventsAsync(sessionId, "scene-routes", "scene-r4", "runtime-routes", ["evt-routes"], 4);
        await cache.SetCheckpointAsync(
            new SyncCheckpoint(sessionId, "scene-routes", "scene-r4", "runtime-routes", 4, DateTimeOffset.UtcNow)
        );
        await cache.CacheRuntimeBundleAsync(sessionId, "runtime-routes", "scene-r4", "bundle:scene-routes:runtime-routes");

        foreach (PlaySurfaceRole role in Enum.GetValues<PlaySurfaceRole>())
        {
            string expectedRoleQuery = $"role={Uri.EscapeDataString(role.ToString())}";
            string expectedDeviceId = role switch
            {
                PlaySurfaceRole.GameMaster => "install-workstation",
                PlaySurfaceRole.Observer => "install-observer_screen",
                _ => "install-play_tablet"
            };

            var resume = await ExecuteRouteRequestAsync<PlayResumeResponse>(
                app,
                HttpMethod.Get,
                PlayApiRoutes.Resume,
                $"?role={Uri.EscapeDataString(role.ToString())}",
                routeValues: new Dictionary<string, string> { ["sessionId"] = sessionId },
                expectedStatusCode: StatusCodes.Status200OK
            );

            string expectedRoute = PlayRouteHandlers.BuildOwnerRoute(sessionId, role);
            Assert(resume.DeepLinkOwnerRoute == expectedRoute, $"resume route must stay role-concrete for {role}");
            Assert(!resume.DeepLinkOwnerRoute.Contains("{sessionId}", StringComparison.Ordinal), $"resume route must never expose templated placeholders for {role}");

            var workspace = await ExecuteRouteRequestAsync<PlayCampaignWorkspaceLiteProjection>(
                app,
                HttpMethod.Get,
                "/api/play/workspace-lite/{sessionId}",
                $"?role={Uri.EscapeDataString(role.ToString())}",
                routeValues: new Dictionary<string, string> { ["sessionId"] = sessionId },
                expectedStatusCode: StatusCodes.Status200OK
            );

            Assert(workspace.RejoinCommandHref == expectedRoute, $"workspace-lite rejoin route must stay role-concrete for {role}");
            Assert(workspace.ContinueCommandHref == expectedRoute, $"workspace-lite continue route must stay role-concrete for {role}");
            Assert(workspace.RoleFollowThroughHref == expectedRoute, $"workspace-lite role follow-through route must stay role-concrete for {role}");
            Assert(workspace.LowNoiseGuidance.Any(item => item.Contains(expectedRoute, StringComparison.Ordinal)), $"workspace-lite guidance must include the concrete role route for {role}");
            Assert(!workspace.DisconnectRecoveryCopy.Contains("{sessionId}", StringComparison.Ordinal), $"workspace-lite disconnect copy must not expose templated placeholders for {role}");

            var artifactShelfBrowseRedirect = await ExecuteRouteResponseAsync(
                app,
                HttpMethod.Get,
                "/artifacts/{sessionId}",
                $"?role={Uri.EscapeDataString(role.ToString())}&view=campaign",
                routeValues: new Dictionary<string, string> { ["sessionId"] = sessionId }
            );

            Assert(artifactShelfBrowseRedirect.StatusCode == StatusCodes.Status302Found, $"artifact shelf browse links must redirect back into the installable shell for {role}");
            Assert(artifactShelfBrowseRedirect.Location == $"/index.html?sessionId={Uri.EscapeDataString(sessionId)}&role={Uri.EscapeDataString(role.ToString())}&artifactView=campaign", $"artifact shelf browse links must preserve role and shelf selection for {role}");

            var artifactShelfRedirect = await ExecuteRouteResponseAsync(
                app,
                HttpMethod.Get,
                "/artifacts/{sessionId}/{artifactId}",
                $"?role={Uri.EscapeDataString(role.ToString())}&view=travel",
                routeValues: new Dictionary<string, string>
                {
                    ["sessionId"] = sessionId,
                    ["artifactId"] = "artifact-recap"
                }
            );

            Assert(artifactShelfRedirect.StatusCode == StatusCodes.Status302Found, $"artifact shelf deep links must redirect back into the installable shell for {role}");
            Assert(artifactShelfRedirect.Location == $"/index.html?sessionId={Uri.EscapeDataString(sessionId)}&role={Uri.EscapeDataString(role.ToString())}&artifactView=travel&artifactId=artifact-recap", $"artifact shelf deep links must preserve role, travel shelf selection, and artifact identity for {role}");

            var restorePlan = await ExecuteRouteRequestAsync<RoamingWorkspaceRestorePlan>(
                app,
                HttpMethod.Get,
                "/api/play/restore-plan/{sessionId}",
                $"?role={Uri.EscapeDataString(role.ToString())}",
                routeValues: new Dictionary<string, string> { ["sessionId"] = sessionId },
                expectedStatusCode: StatusCodes.Status200OK
            );

            Assert(restorePlan.TargetDeviceId == expectedDeviceId, $"restore-plan must default to the trusted claimed device for {role}");
            Assert(restorePlan.ResumeFollowThroughHref.Contains($"/play/{Uri.EscapeDataString(sessionId)}", StringComparison.Ordinal), $"restore-plan must return a concrete owner route for {role}");
            Assert(restorePlan.ResumeFollowThroughHref.Contains(expectedRoleQuery, StringComparison.Ordinal), $"restore-plan resume href must preserve the explicit role query for {role}");
            Assert(!restorePlan.ResumeFollowThroughHref.Contains("{sessionId}", StringComparison.Ordinal), $"restore-plan href must never expose templated placeholders for {role}");
            Assert(restorePlan.StarterPrimerFollowThroughHref.Contains($"/artifacts/{Uri.EscapeDataString(sessionId)}/", StringComparison.Ordinal), $"restore-plan must expose a concrete starter-primer artifact route for {role}");
            Assert(restorePlan.StarterPrimerFollowThroughHref.Contains(expectedRoleQuery, StringComparison.Ordinal), $"restore-plan starter-primer href must preserve the explicit role query for {role}");
            Assert(restorePlan.StarterPrimerFollowThroughHref.Contains($"deviceId={Uri.EscapeDataString(expectedDeviceId)}", StringComparison.Ordinal), $"restore-plan starter-primer href must preserve the trusted claimed-device id for {role}");
            Assert(restorePlan.FirstSessionBriefingFollowThroughHref.Contains($"/artifacts/{Uri.EscapeDataString(sessionId)}/", StringComparison.Ordinal), $"restore-plan must expose a concrete first-session briefing artifact route for {role}");
            Assert(restorePlan.FirstSessionBriefingFollowThroughHref.Contains(expectedRoleQuery, StringComparison.Ordinal), $"restore-plan first-session briefing href must preserve the explicit role query for {role}");
            Assert(restorePlan.FirstSessionBriefingFollowThroughHref.Contains("view=travel", StringComparison.Ordinal), $"restore-plan first-session briefing href must preserve the travel shelf for {role}");

            var onboardingRecovery = await ExecuteRouteRequestAsync<PlayEntryRecoveryProjection>(
                app,
                HttpMethod.Get,
                "/api/play/onboarding-recovery/{sessionId}",
                $"?role={Uri.EscapeDataString(role.ToString())}",
                routeValues: new Dictionary<string, string> { ["sessionId"] = sessionId },
                expectedStatusCode: StatusCodes.Status200OK
            );

            Assert(onboardingRecovery.RestoreActionHref.Contains($"/play/{Uri.EscapeDataString(sessionId)}", StringComparison.Ordinal), $"onboarding-recovery must keep restore href role-concrete for {role}");
            Assert(onboardingRecovery.RestoreActionHref.Contains(expectedRoleQuery, StringComparison.Ordinal), $"onboarding-recovery restore href must preserve the explicit role query for {role}");
            Assert(!onboardingRecovery.RestoreActionHref.Contains("{sessionId}", StringComparison.Ordinal), $"onboarding-recovery restore href must never expose templated placeholders for {role}");

            string trustedTravelDeviceId = $"{expectedDeviceId}:travel";
            var restorePlanTravel = await ExecuteRouteRequestAsync<RoamingWorkspaceRestorePlan>(
                app,
                HttpMethod.Get,
                "/api/play/restore-plan/{sessionId}",
                $"?role={Uri.EscapeDataString(role.ToString())}&deviceId={Uri.EscapeDataString(trustedTravelDeviceId)}",
                routeValues: new Dictionary<string, string> { ["sessionId"] = sessionId },
                expectedStatusCode: StatusCodes.Status200OK
            );

            Assert(restorePlanTravel.TargetDeviceId == expectedDeviceId, $"restore-plan must normalize trusted travel device ids to the role primary target for {role}");
            Assert(restorePlanTravel.TargetDeviceId.Contains(":travel:travel", StringComparison.Ordinal) is false, $"restore-plan target device id must never expand travel lineage for {role}");
            Assert(restorePlanTravel.TravelCompanionLabels.Any(item => item.Contains(trustedTravelDeviceId, StringComparison.OrdinalIgnoreCase)), $"restore-plan must preserve a travel sibling claimed device for {role}");
            Assert(restorePlanTravel.TravelCompanionLabels.All(item => !item.Contains(":travel:travel", StringComparison.Ordinal)), $"restore-plan claimed devices must never expand travel lineage for {role}");
            Assert(restorePlanTravel.ResumeFollowThroughHref.Contains(expectedRoleQuery, StringComparison.Ordinal), $"restore-plan trusted travel route must preserve the explicit role query for {role}");
            Assert(restorePlanTravel.StarterPrimerFollowThroughHref.Contains($"deviceId={Uri.EscapeDataString(expectedDeviceId)}", StringComparison.Ordinal), $"restore-plan trusted travel route must keep starter-primer follow-through on the normalized claimed device for {role}");
            Assert(restorePlanTravel.FirstSessionBriefingFollowThroughHref.Contains("view=travel", StringComparison.Ordinal), $"restore-plan trusted travel route must keep first-session briefing follow-through on the travel shelf for {role}");

            var onboardingRecoveryTravel = await ExecuteRouteRequestAsync<PlayEntryRecoveryProjection>(
                app,
                HttpMethod.Get,
                "/api/play/onboarding-recovery/{sessionId}",
                $"?role={Uri.EscapeDataString(role.ToString())}&deviceId={Uri.EscapeDataString(trustedTravelDeviceId)}",
                routeValues: new Dictionary<string, string> { ["sessionId"] = sessionId },
                expectedStatusCode: StatusCodes.Status200OK
            );

            Assert(onboardingRecoveryTravel.RestoreActionHref.Contains(expectedRoleQuery, StringComparison.Ordinal), $"onboarding-recovery trusted travel route must preserve the explicit role query for {role}");
            Assert(!onboardingRecoveryTravel.RestoreActionHref.Contains("{sessionId}", StringComparison.Ordinal), $"onboarding-recovery trusted travel route must never expose templated placeholders for {role}");

            JsonElement restoreBadRequest = await ExecuteRouteRequestAsync<JsonElement>(
                app,
                HttpMethod.Get,
                "/api/play/restore-plan/{sessionId}",
                $"?role={Uri.EscapeDataString(role.ToString())}&deviceId={Uri.EscapeDataString("install-attacker")}",
                routeValues: new Dictionary<string, string> { ["sessionId"] = sessionId },
                expectedStatusCode: StatusCodes.Status400BadRequest
            );

            Assert(restoreBadRequest.TryGetProperty("error", out var restoreError) && string.Equals(restoreError.GetString(), "invalid_device_id", StringComparison.Ordinal), $"restore-plan must reject untrusted device ids for {role}");
            Assert(restoreBadRequest.TryGetProperty("allowedDeviceIds", out var restoreAllowed), $"restore-plan must return allowed trusted device ids for {role}");
            Assert(restoreAllowed.ValueKind == JsonValueKind.Array, $"restore-plan allowed trusted device ids must be serialized as an array for {role}");
            Assert(restoreAllowed.EnumerateArray().Any(item => string.Equals(item.GetString(), expectedDeviceId, StringComparison.OrdinalIgnoreCase)), $"restore-plan allowed trusted device ids must include the role primary target for {role}");

            JsonElement onboardingBadRequest = await ExecuteRouteRequestAsync<JsonElement>(
                app,
                HttpMethod.Get,
                "/api/play/onboarding-recovery/{sessionId}",
                $"?role={Uri.EscapeDataString(role.ToString())}&deviceId={Uri.EscapeDataString("install-attacker")}",
                routeValues: new Dictionary<string, string> { ["sessionId"] = sessionId },
                expectedStatusCode: StatusCodes.Status400BadRequest
            );

            Assert(onboardingBadRequest.TryGetProperty("error", out var onboardingError) && string.Equals(onboardingError.GetString(), "invalid_device_id", StringComparison.Ordinal), $"onboarding-recovery must reject untrusted device ids for {role}");
            Assert(onboardingBadRequest.TryGetProperty("allowedDeviceIds", out var onboardingAllowed), $"onboarding-recovery must return allowed trusted device ids for {role}");
            Assert(onboardingAllowed.ValueKind == JsonValueKind.Array, $"onboarding-recovery allowed trusted device ids must be serialized as an array for {role}");
            Assert(onboardingAllowed.EnumerateArray().Any(item => string.Equals(item.GetString(), expectedDeviceId, StringComparison.OrdinalIgnoreCase)), $"onboarding-recovery allowed trusted device ids must include the role primary target for {role}");
        }
    }
    finally
    {
        await app.DisposeAsync();
    }
}

static Task VerifyRoleBoundarySurvivesCapabilityLeakageAsync()
{
    IReadOnlyList<string> leakedCapabilities =
    [
        "play.session.sync",
        "play.gm.actions",
        "play.spider.cards",
        "play.notes.write",
    ];

    var playerActions = PlayRouteHandlers.BuildQuickActions(PlaySurfaceRole.Player, leakedCapabilities);
    var gmActions = PlayRouteHandlers.BuildQuickActions(PlaySurfaceRole.GameMaster, leakedCapabilities);
    var observerActions = PlayRouteHandlers.BuildQuickActions(PlaySurfaceRole.Observer, leakedCapabilities);

    Assert(
        playerActions.Select(action => action.ActionId).SequenceEqual(["player-mark-ready", "player-request-cover"]),
        "player role quick actions must stay player-only even when gm capabilities leak into the capability list"
    );
    Assert(
        playerActions.All(action => action.RequiredCapability == "play.session.sync"),
        "player role quick actions must continue to require only player-safe capabilities when capability lists are over-provisioned"
    );
    Assert(
        gmActions.Select(action => action.ActionId).SequenceEqual(["gm-advance-initiative", "gm-publish-spider-card"]),
        "gm role quick actions must stay gm-only even when player-safe capabilities leak into the capability list"
    );
    Assert(
        gmActions.All(action => action.RequiredCapability is "play.gm.actions" or "play.spider.cards"),
        "gm role quick actions must continue to require gm-only capabilities when capability lists are over-provisioned"
    );
    Assert(observerActions.Count == 0, "observer lane must remain read-mostly even when player and gm capabilities leak into the capability list");
    return Task.CompletedTask;
}

static async Task VerifyQuickActionRejectsCrossRoleAuthorizationAsync()
{
    const string sessionId = "session-role-gate";
    var store = new BrowserSessionEventLogStore(new InMemoryBrowserKeyValueStore());
    var cache = new BrowserSessionOfflineCacheService(new InMemoryBrowserKeyValueStore());
    var queue = new BrowserSessionOfflineQueueService(store, cache);
    var session = new EngineSessionEnvelope(sessionId, "scene-role", "scene-r2", "runtime-role");
    await store.AppendPendingEventsAsync(
        session.SessionId,
        session.SceneId,
        session.SceneRevision,
        session.RuntimeFingerprint,
        ["evt-existing"],
        5
    );
    await cache.SetCheckpointAsync(
        new SyncCheckpoint(
            session.SessionId,
            session.SceneId,
            session.SceneRevision,
            session.RuntimeFingerprint,
            5,
            DateTimeOffset.UtcNow
        )
    );

    var response = await ExecuteResultAsync<PlayQuickActionResponse>(
        await PlayRouteHandlers.HandleQuickActionAsync(
            new PlayQuickActionRequest(new EngineSessionCursor(session, 5), PlaySurfaceRole.Player, "gm-advance-initiative"),
            cache,
            store,
            queue,
            PlayerShellModule.CreateDescriptor(),
            GmTacticalShellModule.CreateDescriptor(),
            CancellationToken.None
        )
    );

    Assert(!response.Accepted, "cross-role quick action must be denied");
    Assert(!response.Stale, "cross-role quick action denial must be authorization-based, not stale-lineage");
    Assert(response.Reason == "action not permitted for role capabilities", "cross-role quick action denial must return deterministic reason");
    Assert(response.Projection.Cursor.AppliedThroughSequence == 5, "cross-role quick action denial must preserve sequence ownership");
    Assert(response.Projection.Cursor.Session.SceneId == "scene-role", "cross-role quick action denial must preserve lineage");

    var ledger = await store.GetExistingAsync(sessionId);
    Assert(ledger is not null, "cross-role quick action denial must preserve existing ledger");
    Assert(ledger!.LastKnownSequence == 5, "cross-role quick action denial must not mutate sequence ownership");
    Assert(ledger.PendingEvents.SequenceEqual(["evt-existing"]), "cross-role quick action denial must not mutate pending events");
}

static async Task VerifyDeniedQuickActionsPreserveStoredReplayStateAsync()
{
    const string sessionId = "session-role-denial-replay";
    var store = new BrowserSessionEventLogStore(new InMemoryBrowserKeyValueStore());
    var cache = new BrowserSessionOfflineCacheService(new InMemoryBrowserKeyValueStore());
    var queue = new BrowserSessionOfflineQueueService(store, cache);
    var session = new EngineSessionEnvelope(sessionId, "scene-role", "scene-r6", "runtime-role");
    await store.AppendPendingEventsAsync(
        session.SessionId,
        session.SceneId,
        session.SceneRevision,
        session.RuntimeFingerprint,
        ["evt-existing"],
        7
    );
    await cache.SetCheckpointAsync(
        new SyncCheckpoint(
            session.SessionId,
            session.SceneId,
            session.SceneRevision,
            session.RuntimeFingerprint,
            7,
            DateTimeOffset.UtcNow
        )
    );

    async Task AssertDeniedStateAsync(PlaySurfaceRole role, string actionId)
    {
        var response = await ExecuteResultAsync<PlayQuickActionResponse>(
            await PlayRouteHandlers.HandleQuickActionAsync(
                new PlayQuickActionRequest(new EngineSessionCursor(session, 7), role, actionId),
                cache,
                store,
                queue,
                PlayerShellModule.CreateDescriptor(),
                GmTacticalShellModule.CreateDescriptor(),
                CancellationToken.None
            )
        );

        Assert(!response.Accepted, $"{role} denial must not be accepted");
        Assert(!response.Stale, $"{role} denial must stay authorization-based instead of stale");
        Assert(response.Reason == "action not permitted for role capabilities", $"{role} denial must return deterministic authorization reason");
        Assert(response.Projection.Cursor.Session.SceneId == session.SceneId, $"{role} denial must preserve stored scene lineage");
        Assert(response.Projection.Cursor.Session.RuntimeFingerprint == session.RuntimeFingerprint, $"{role} denial must preserve stored runtime lineage");
        Assert(response.Projection.Cursor.AppliedThroughSequence == 7, $"{role} denial must preserve stored sequence ownership");
        Assert(
            response.Projection.Timeline.Any(item => string.Equals(item, "pending:evt-existing", StringComparison.Ordinal)),
            $"{role} denial must preserve stored pending replay state"
        );
        Assert(response.Checkpoint is not null, $"{role} denial must return an aligned checkpoint");
        Assert(response.Checkpoint!.SceneRevision == session.SceneRevision, $"{role} denial checkpoint must preserve stored scene revision");
        Assert(response.Checkpoint.AppliedThroughSequence == 7, $"{role} denial checkpoint must preserve stored sequence ownership");
    }

    await AssertDeniedStateAsync(PlaySurfaceRole.Player, "gm-advance-initiative");
    await AssertDeniedStateAsync(PlaySurfaceRole.GameMaster, "player-mark-ready");
    await AssertDeniedStateAsync(PlaySurfaceRole.Observer, "player-mark-ready");

    var ledger = await store.GetExistingAsync(sessionId);
    Assert(ledger is not null, "authorization denials must preserve the existing ledger");
    Assert(ledger!.LastKnownSequence == 7, "authorization denials must not mutate stored sequence ownership");
    Assert(ledger.PendingEvents.SequenceEqual(["evt-existing"]), "authorization denials must not mutate stored pending events");
}

static async Task VerifyCachePressureBudgetContractAsync()
{
    var cache = new BrowserSessionOfflineCacheService(new InMemoryBrowserKeyValueStore());
    var baseTime = DateTimeOffset.UtcNow.AddMinutes(-30);

    for (var i = 1; i <= 9; i++)
    {
        await cache.CacheRuntimeBundleAsync(
            new RuntimeBundleCacheEntry(
                $"session-budget-{i}",
                $"runtime-budget-{i}",
                $"scene-budget-r{i}",
                $"bundle-budget-{i}",
                baseTime.AddMinutes(i),
                baseTime.AddMinutes(i)
            )
        );
    }

    var pressure = await cache.GetCachePressureAsync();
    Assert(pressure.RuntimeBundleCount == pressure.RuntimeBundleQuota, "runtime-bundle cache pressure must stay bounded at quota");
    Assert(pressure.RuntimeBundleQuota == 8, "runtime-bundle cache pressure must preserve the M10 quota budget");
    Assert(pressure.BackpressureActive, "runtime-bundle cache pressure must report backpressure when at budget");
}

static async Task VerifyOfflineQueueRejectsStaleLineageAsync()
{
    var storeState = new InMemoryBrowserKeyValueStore();
    var store = new BrowserSessionEventLogStore(storeState);
    var cache = new BrowserSessionOfflineCacheService(storeState);
    var queue = new BrowserSessionOfflineQueueService(store, cache);
    const string sessionId = "session-stale-queue";

    await store.AppendPendingEventsAsync(sessionId, "scene-stored", "scene-r2", "runtime-stored", ["evt-1"], 4);

    var staleCursor = new EngineSessionCursor(
        new EngineSessionEnvelope(sessionId, "scene-request", "scene-r1", "runtime-request"),
        4
    );

    await AssertThrowsAsync<InvalidOperationException>(
        () => queue.EnqueueAsync(staleCursor, "evt-new"),
        "offline queue enqueue must reject stale lineage before mutating stored replay state"
    );

    await AssertThrowsAsync<InvalidOperationException>(
        () => queue.SyncReplayAsync(new PlaySyncRequest(staleCursor, ["evt-1"])),
        "offline queue sync must reject stale lineage before acknowledging stored replay state"
    );
}

static void VerifyCursorValidationRejectsNegativeSequence()
{
    var valid = PlayRouteHandlers.TryValidateCursor(
        new EngineSessionCursor(new EngineSessionEnvelope("session-valid", "scene-a", "scene-r1", "runtime-a"), 0),
        out var validError
    );
    Assert(valid, $"cursor validation should accept non-negative sequence: {validError}");

    var invalid = PlayRouteHandlers.TryValidateCursor(
        new EngineSessionCursor(new EngineSessionEnvelope("session-negative", "scene-a", "scene-r1", "runtime-a"), -1),
        out var invalidError
    );
    Assert(!invalid, "cursor validation must reject negative sequence values");
    Assert(
        string.Equals(invalidError, "applied through sequence cannot be negative.", StringComparison.Ordinal),
        "cursor validation must return a deterministic negative-sequence error"
    );
}

static async Task VerifyReconnectLineageTransitionContinuityAsync()
{
    var store = new BrowserSessionEventLogStore(new InMemoryBrowserKeyValueStore());
    var cache = new BrowserSessionOfflineCacheService(new InMemoryBrowserKeyValueStore());
    var queue = new BrowserSessionOfflineQueueService(store, cache);
    const string sessionId = "session-reconnect";

    await store.AppendPendingEventsAsync(sessionId, "scene-a", "scene-r1", "runtime-a", ["evt-old"], 4);
    await cache.SetCheckpointAsync(new SyncCheckpoint(sessionId, "scene-a", "scene-r1", "runtime-a", 4, DateTimeOffset.UtcNow));

    var reconnectCursor = new EngineSessionCursor(
        new EngineSessionEnvelope(sessionId, "scene-b", "scene-r2", "runtime-b"),
        0
    );
    var ledger = await store.GetOrCreateAsync(
        reconnectCursor.Session.SessionId,
        reconnectCursor.Session.SceneId,
        reconnectCursor.Session.SceneRevision,
        reconnectCursor.Session.RuntimeFingerprint
    );
    var effectiveSession = new EngineSessionEnvelope(
        ledger.SessionId,
        ledger.SceneId,
        ledger.SceneRevision,
        ledger.RuntimeFingerprint
    );
    var existingCheckpoint = await cache.GetCheckpointAsync(sessionId);
    var appliedThroughSequence = Math.Max(reconnectCursor.AppliedThroughSequence, ledger.LastKnownSequence);
    var reconnectCheckpoint = existingCheckpoint is not null
        && SessionLineage.IsCheckpointAligned(existingCheckpoint, effectiveSession)
            ? existingCheckpoint with
            {
                AppliedThroughSequence = appliedThroughSequence,
                CapturedAtUtc = DateTimeOffset.UtcNow,
            }
            : new SyncCheckpoint(
                effectiveSession.SessionId,
                effectiveSession.SceneId,
                effectiveSession.SceneRevision,
                effectiveSession.RuntimeFingerprint,
                appliedThroughSequence,
                DateTimeOffset.UtcNow
            );
    await cache.SetCheckpointAsync(reconnectCheckpoint);

    var storedLedger = await store.GetExistingAsync(sessionId);
    Assert(
        SessionLineage.IsStoredLineageAligned(effectiveSession, reconnectCheckpoint, storedLedger),
        "reconnect must realign checkpoint and ledger lineage before sync/quick-action stale checks"
    );

    var enqueueResult = await queue.EnqueueAsync(new EngineSessionCursor(effectiveSession, reconnectCheckpoint.AppliedThroughSequence), "evt-new");
    var syncResult = await queue.SyncReplayAsync(
        new PlaySyncRequest(new EngineSessionCursor(effectiveSession, enqueueResult.AppliedThroughSequence), ["evt-new"])
    );
    Assert(syncResult.AcceptedEventCount == 1, "sync should continue on the new lineage after reconnect realignment");
}

static async Task VerifyStoredLineageStaleResponsesAsync()
{
    const string sessionId = "session-stale";
    var storedSession = new EngineSessionEnvelope(sessionId, "scene-stored", "scene-r2", "runtime-stored");
    var requestSession = new EngineSessionEnvelope(sessionId, "scene-request", "scene-r1", "runtime-request");
    var app = PlayWebApplication.Build([]);

    try
    {
        var store = app.Services.GetRequiredService<BrowserSessionEventLogStore>();
        await store.AppendPendingEventsAsync(
            storedSession.SessionId,
            storedSession.SceneId,
            storedSession.SceneRevision,
            storedSession.RuntimeFingerprint,
            ["evt-stored"],
            4
        );

        var existingLedger = await store.GetExistingAsync(sessionId);
        Assert(existingLedger is not null, "stored-ledger stale regression must have an existing ledger");
        Assert(
            !SessionLineage.IsStoredLineageAligned(requestSession, checkpoint: null, existingLedger),
            "mismatched request lineage must be rejected when only stored ledger lineage exists"
        );
        var offlineCacheService = app.Services.GetRequiredService<BrowserSessionOfflineCacheService>();
        var offlineQueueService = app.Services.GetRequiredService<BrowserSessionOfflineQueueService>();
        var cursor = new EngineSessionCursor(requestSession, 4);
        var quickActionResponse = await ExecuteResultAsync<PlayQuickActionResponse>(
            await PlayRouteHandlers.HandleQuickActionAsync(
                new PlayQuickActionRequest(cursor, PlaySurfaceRole.Player, "player-mark-ready"),
                offlineCacheService,
                store,
                offlineQueueService,
                PlayerShellModule.CreateDescriptor(),
                GmTacticalShellModule.CreateDescriptor(),
                CancellationToken.None
            )
        );
        Assert(!quickActionResponse.Accepted, "stale quick action must be rejected");
        Assert(quickActionResponse.Stale, "stale quick action must be marked stale");
        Assert(
            quickActionResponse.Projection.Cursor.Session.SceneId == storedSession.SceneId,
            "stale quick action projection must prefer stored ledger scene id"
        );
        Assert(
            quickActionResponse.Checkpoint?.ProjectionFingerprint == storedSession.RuntimeFingerprint,
            "stale quick action checkpoint must prefer stored ledger runtime fingerprint"
        );
        Assert(
            quickActionResponse.Projection.Cursor.AppliedThroughSequence == 4,
            "stale quick action projection must prefer stored ledger sequence ownership when checkpoint is missing"
        );
        Assert(
            quickActionResponse.Projection.Timeline.Contains("pending:evt-stored", StringComparer.Ordinal),
            "stale quick action projection must preserve stored pending events when checkpoint is missing"
        );

        var syncResponse = await ExecuteResultAsync<PlaySyncResponse>(
            await PlayRouteHandlers.HandleSyncAsync(
                new PlaySyncRequest(cursor, ["evt-client"]),
                offlineCacheService,
                store,
                offlineQueueService,
                CancellationToken.None
            )
        );
        Assert(!syncResponse.Accepted, "stale sync must be rejected");
        Assert(syncResponse.Stale, "stale sync must be marked stale");
        Assert(
            syncResponse.Projection.Cursor.Session.SceneRevision == storedSession.SceneRevision,
            "stale sync projection must prefer stored ledger scene revision"
        );
        Assert(
            syncResponse.Checkpoint?.SceneId == storedSession.SceneId,
            "stale sync checkpoint must prefer stored ledger scene id"
        );
        Assert(
            syncResponse.Projection.Cursor.AppliedThroughSequence == 4,
            "stale sync projection must prefer stored ledger sequence ownership when checkpoint is missing"
        );
        Assert(
            syncResponse.Projection.Timeline.Contains("pending:evt-stored", StringComparer.Ordinal),
            "stale sync projection must preserve stored pending events when checkpoint is missing"
        );

        var recoveredResult = await PlayRouteHandlers.HandleSyncAsync(
            new PlaySyncRequest(syncResponse.Projection.Cursor, ["evt-stored"]),
            offlineCacheService,
            store,
            offlineQueueService,
            CancellationToken.None
        );
        var recoveredSync = await ExecuteResultAsync<PlaySyncResponse>(recoveredResult);
        Assert(recoveredSync.Accepted, "retrying sync with the stored stale checkpoint lineage must recover");
        Assert(!recoveredSync.Stale, "retrying sync with the stored stale checkpoint lineage must not stay stale");
    }
    finally
    {
        await app.DisposeAsync();
    }
}

static async Task VerifyReconnectRejectsStaleLineageWithoutMutationAsync()
{
    const string sessionId = "session-reconnect-stale";
    var app = PlayWebApplication.Build([]);

    try
    {
        var store = app.Services.GetRequiredService<BrowserSessionEventLogStore>();
        var cache = app.Services.GetRequiredService<BrowserSessionOfflineCacheService>();
        await store.AppendPendingEventsAsync(sessionId, "scene-stored", "scene-r4", "runtime-stored", ["evt-stored"], 8);
        await cache.SetCheckpointAsync(
            new SyncCheckpoint(sessionId, "scene-stored", "scene-r4", "runtime-stored", 8, DateTimeOffset.UtcNow)
        );

        var staleResult = await PlayRouteHandlers.HandleReconnectAsync(
            new PlayReconnectRequest(
                new EngineSessionCursor(
                    new EngineSessionEnvelope(sessionId, "scene-request", "scene-r1", "runtime-request"),
                    3
                )
            ),
            store,
            cache,
            CancellationToken.None
        );
        var staleResponse = await ExecuteResultWithStatusAsync<ReconnectConflictPayload>(
            staleResult,
            StatusCodes.Status409Conflict
        );

        Assert(staleResponse.Stale, "stale reconnect must return stale conflict metadata");
        Assert(staleResponse.Error == "session lineage changed", "stale reconnect must return lineage conflict reason");
        Assert(staleResponse.Projection.Cursor.Session.SceneId == "scene-stored", "stale reconnect conflict must preserve stored scene lineage");
        Assert(staleResponse.Projection.Cursor.AppliedThroughSequence == 8, "stale reconnect conflict must preserve stored sequence ownership");
        Assert(staleResponse.Projection.Timeline.Contains("pending:evt-stored", StringComparer.Ordinal), "stale reconnect conflict must preserve stored pending events");

        var ledgerAfterConflict = await store.GetExistingAsync(sessionId);
        Assert(ledgerAfterConflict is not null, "stale reconnect must keep existing ledger");
        Assert(ledgerAfterConflict!.SceneId == "scene-stored", "stale reconnect must not reset ledger scene");
        Assert(ledgerAfterConflict.LastKnownSequence == 8, "stale reconnect must not reset ledger sequence ownership");
        Assert(ledgerAfterConflict.PendingEvents.Count == 1 && ledgerAfterConflict.PendingEvents[0] == "evt-stored", "stale reconnect must not drop pending events");

        var checkpointAfterConflict = await cache.GetCheckpointAsync(sessionId);
        Assert(checkpointAfterConflict is not null, "stale reconnect must preserve existing checkpoint");
        Assert(checkpointAfterConflict!.SceneId == "scene-stored", "stale reconnect must not mutate checkpoint lineage");
        Assert(checkpointAfterConflict.AppliedThroughSequence == 8, "stale reconnect must not mutate checkpoint sequence ownership");
    }
    finally
    {
        await app.DisposeAsync();
    }
}

static async Task VerifyReconnectClientThrowsTypedStaleAsync()
{
    var projection = new PlaySessionProjection(
        new EngineSessionCursor(
            new EngineSessionEnvelope("session-reconnect-client-stale", "scene-stored", "scene-r7", "runtime-stored"),
            12
        ),
        ["projection ready", "pending:evt-stored"],
        DateTimeOffset.UtcNow
    );
    var checkpoint = new SyncCheckpoint(
        "session-reconnect-client-stale",
        "scene-stored",
        "scene-r7",
        "runtime-stored",
        12,
        DateTimeOffset.UtcNow
    );
    var payload = new ReconnectConflictPayload("session lineage changed", true, projection, checkpoint);
    var apiClient = CreateApiClient(
        new StubHttpMessageHandler((request, _) =>
        {
            Assert(request.Method == HttpMethod.Post, "reconnect client must issue POST requests");
            Assert(
                string.Equals(request.RequestUri?.AbsolutePath, PlayApiRoutes.Reconnect, StringComparison.Ordinal),
                "reconnect client must call the reconnect route"
            );

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            var response = new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
            return Task.FromResult(response);
        })
    );

    PlayReconnectStaleException? staleException = null;
    try
    {
        await apiClient.ReconnectAsync(
            new PlayReconnectRequest(
                new EngineSessionCursor(
                    new EngineSessionEnvelope("session-reconnect-client-stale", "scene-request", "scene-r1", "runtime-request"),
                    1
                )
            )
        );
    }
    catch (PlayReconnectStaleException ex)
    {
        staleException = ex;
    }

    Assert(staleException is not null, "reconnect client must throw typed stale exception on 409 conflict");
    Assert(staleException!.Projection.Cursor.Session.SceneId == "scene-stored", "typed stale reconnect exception must include stored projection lineage");
    Assert(staleException.Projection.Cursor.AppliedThroughSequence == 12, "typed stale reconnect exception must include stored projection sequence ownership");
    Assert(staleException.Checkpoint.SceneRevision == "scene-r7", "typed stale reconnect exception must include stored checkpoint lineage");
    Assert(staleException.Checkpoint.AppliedThroughSequence == 12, "typed stale reconnect exception must include stored checkpoint sequence ownership");
}

static async Task VerifyContinuityClaimRejectsStaleLineageWithoutMutationAsync()
{
    const string sessionId = "session-continuity-stale";
    var app = PlayWebApplication.Build([]);

    try
    {
        var store = app.Services.GetRequiredService<BrowserSessionEventLogStore>();
        var cache = app.Services.GetRequiredService<BrowserSessionOfflineCacheService>();
        await store.AppendPendingEventsAsync(sessionId, "scene-stored", "scene-r7", "runtime-stored", ["evt-stored"], 6);
        await cache.SetCheckpointAsync(
            new SyncCheckpoint(sessionId, "scene-stored", "scene-r7", "runtime-stored", 6, DateTimeOffset.UtcNow)
        );

        var staleResultResponse = await PlayRouteHandlers.HandleContinuityClaimAsync(
            new PlayContinuityClaimRequest(
                new EngineSessionCursor(
                    new EngineSessionEnvelope(sessionId, "scene-request", "scene-r1", "runtime-request"),
                    2
                ),
                PlaySurfaceRole.Player,
                "device-request",
                "observer-request"
            ),
            store,
            cache,
            app.Services.GetRequiredService<IBrowserKeyValueStore>(),
            CancellationToken.None
        );
        var staleResult = await ExecuteResultAsync<PlayContinuityClaimResponse>(staleResultResponse);

        Assert(!staleResult.Accepted, "stale continuity claim must not be accepted");
        Assert(staleResult.Stale, "stale continuity claim must mark stale lineage");
        Assert(staleResult.Reason == "session lineage changed", "stale continuity claim must return stale reason");
        Assert(staleResult.Projection.Cursor.Session.SceneId == "scene-stored", "stale continuity claim must preserve stored scene lineage");
        Assert(staleResult.Checkpoint.SceneRevision == "scene-r7", "stale continuity claim must preserve stored checkpoint lineage");

        var ledgerAfterClaim = await store.GetExistingAsync(sessionId);
        Assert(ledgerAfterClaim is not null, "stale continuity claim must keep existing ledger");
        Assert(ledgerAfterClaim!.SceneId == "scene-stored", "stale continuity claim must not mutate stored ledger scene");
        Assert(ledgerAfterClaim.LastKnownSequence == 6, "stale continuity claim must not mutate stored sequence ownership");
    }
    finally
    {
        await app.DisposeAsync();
    }
}

static async Task VerifyContinuityClaimRejectsUnknownSessionWithoutMutationAsync()
{
    const string sessionId = "session-continuity-unknown";
    var app = PlayWebApplication.Build([]);

    try
    {
        var store = app.Services.GetRequiredService<BrowserSessionEventLogStore>();
        var cache = app.Services.GetRequiredService<BrowserSessionOfflineCacheService>();
        var browserStore = app.Services.GetRequiredService<IBrowserKeyValueStore>();

        var resultResponse = await PlayRouteHandlers.HandleContinuityClaimAsync(
            new PlayContinuityClaimRequest(
                new EngineSessionCursor(
                    new EngineSessionEnvelope(sessionId, "scene-unknown", "scene-r1", "runtime-unknown"),
                    3
                ),
                PlaySurfaceRole.Player,
                "device-unknown",
                "observer-unknown"
            ),
            store,
            cache,
            browserStore,
            CancellationToken.None
        );
        var result = await ExecuteResultAsync<PlayContinuityClaimResponse>(resultResponse);

        Assert(!result.Accepted, "unknown-session continuity claim must not be accepted");
        Assert(!result.Stale, "unknown-session continuity claim must not report stale lineage");
        Assert(result.Reason == "session not bootstrapped", "unknown-session continuity claim must require trusted bootstrap lineage");

        var ledgerAfterClaim = await store.GetExistingAsync(sessionId);
        Assert(ledgerAfterClaim is null, "unknown-session continuity claim must not create ledger state");

        var checkpointAfterClaim = await cache.GetCheckpointAsync(sessionId);
        Assert(checkpointAfterClaim is null, "unknown-session continuity claim must not create checkpoint state");

        var continuityAfterClaim = await browserStore.GetAsync<ObserverContinuityEntry>(PlayBrowserStateKeys.Continuity(sessionId));
        Assert(continuityAfterClaim is null, "unknown-session continuity claim must not create continuity state");
    }
    finally
    {
        await app.DisposeAsync();
    }
}

static async Task VerifyObserveReturnsLineageSafeContinuityAsync()
{
    const string sessionId = "session-observe-continuity";
    var app = PlayWebApplication.Build([]);

    try
    {
        var store = app.Services.GetRequiredService<BrowserSessionEventLogStore>();
        var cache = app.Services.GetRequiredService<BrowserSessionOfflineCacheService>();
        var browserStore = app.Services.GetRequiredService<IBrowserKeyValueStore>();
        await store.AppendPendingEventsAsync(sessionId, "scene-observe", "scene-r9", "runtime-observe", ["evt-pending"], 9);
        await cache.SetCheckpointAsync(
            new SyncCheckpoint(sessionId, "scene-observe", "scene-r9", "runtime-observe", 9, DateTimeOffset.UtcNow)
        );
        await cache.CacheRuntimeBundleAsync(sessionId, "runtime-observe", "scene-r9", "bundle:scene-observe:runtime-observe");

        var claimResult = await PlayRouteHandlers.HandleContinuityClaimAsync(
            new PlayContinuityClaimRequest(
                new EngineSessionCursor(
                    new EngineSessionEnvelope(sessionId, "scene-observe", "scene-r9", "runtime-observe"),
                    9
                ),
                PlaySurfaceRole.GameMaster,
                "device-gm-a",
                "observer-gm-a"
            ),
            store,
            cache,
            browserStore,
            CancellationToken.None
        );
        var continuityClaim = await ExecuteResultAsync<PlayContinuityClaimResponse>(claimResult);
        Assert(continuityClaim.Accepted, "continuity claim must be accepted for aligned lineage");
        Assert(!continuityClaim.Stale, "continuity claim must not be stale for aligned lineage");

        var observeResult = await PlayRouteHandlers.HandleObserveAsync(
            sessionId,
            store,
            cache,
            browserStore,
            CancellationToken.None
        );
        var observe = await ExecuteResultAsync<PlayObserveResponse>(observeResult);
        Assert(observe.Continuity is not null, "observe must return continuity metadata after accepted claim");
        Assert(observe.Continuity!.ObserverId == "observer-gm-a", "observe must return stored observer id");
        Assert(observe.Continuity.DeviceId == "device-gm-a", "observe must return stored device id");
        Assert(observe.Continuity.ObservedThroughSequence == 9, "observe must preserve continuity sequence ownership");
        Assert(observe.RuntimeBundle is not null, "observe must return runtime bundle metadata for cross-device resume");
        Assert(observe.Projection.Timeline.Contains("pending:evt-pending", StringComparer.Ordinal), "observe must preserve stored pending replay events");

        await browserStore.SetAsync(
            PlayBrowserStateKeys.Continuity(sessionId),
            new ObserverContinuityEntry(
                sessionId,
                "scene-old",
                "scene-r1",
                "runtime-old",
                "observer-old",
                "device-old",
                PlaySurfaceRole.Player,
                12,
                DateTimeOffset.UtcNow,
                "old-token"
            )
        );

        var observeAfterTamperResult = await PlayRouteHandlers.HandleObserveAsync(
            sessionId,
            store,
            cache,
            browserStore,
            CancellationToken.None
        );
        var observeAfterTamper = await ExecuteResultAsync<PlayObserveResponse>(observeAfterTamperResult);
        Assert(observeAfterTamper.Continuity is null, "observe must drop stale/mismatched continuity metadata");
    }
    finally
    {
        await app.DisposeAsync();
    }
}

static async Task VerifyObserveDoesNotSeedStateForEmptySessionAsync()
{
    const string sessionId = "session-observe-empty";
    var app = PlayWebApplication.Build([]);

    try
    {
        var store = app.Services.GetRequiredService<BrowserSessionEventLogStore>();
        var cache = app.Services.GetRequiredService<BrowserSessionOfflineCacheService>();
        var browserStore = app.Services.GetRequiredService<IBrowserKeyValueStore>();

        var observeResult = await PlayRouteHandlers.HandleObserveAsync(
            sessionId,
            store,
            cache,
            browserStore,
            CancellationToken.None
        );
        var observe = await ExecuteResultAsync<PlayObserveResponse>(observeResult);

        Assert(observe.SessionId == sessionId, "observe must preserve requested session id on empty state");
        Assert(observe.Checkpoint.SceneId == "scene-main", "observe must return fallback scene metadata on empty state");
        Assert(observe.Checkpoint.ProjectionFingerprint == "runtime-local", "observe must return fallback runtime metadata on empty state");
        Assert(observe.Projection.Cursor.AppliedThroughSequence == 0, "observe must start empty-state projections at sequence zero");
        Assert(observe.Continuity is null, "observe must not synthesize continuity metadata on empty state");

        var ledgerAfterObserve = await store.GetExistingAsync(sessionId);
        var checkpointAfterObserve = await cache.GetCheckpointAsync(sessionId);
        Assert(ledgerAfterObserve is null, "observe must not create a ledger for an empty session");
        Assert(checkpointAfterObserve is null, "observe must not persist a checkpoint for an empty session");
    }
    finally
    {
        await app.DisposeAsync();
    }
}

static async Task VerifyObserveDoesNotMutateStoredStateOrReturnStaleRuntimeBundleAsync()
{
    const string sessionId = "session-observe-stored";
    var app = PlayWebApplication.Build([]);

    try
    {
        var store = app.Services.GetRequiredService<BrowserSessionEventLogStore>();
        var cache = app.Services.GetRequiredService<BrowserSessionOfflineCacheService>();
        var browserStore = app.Services.GetRequiredService<IBrowserKeyValueStore>();
        await store.AppendPendingEventsAsync(sessionId, "scene-observe", "scene-r9", "runtime-observe", ["evt-pending"], 9);
        await cache.SetCheckpointAsync(
            new SyncCheckpoint(sessionId, "scene-observe", "scene-r9", "runtime-observe", 9, DateTimeOffset.UtcNow.AddMinutes(-5))
        );
        await cache.CacheRuntimeBundleAsync(sessionId, "runtime-stale", "scene-r1", "bundle:scene-observe:runtime-stale");

        var ledgerBeforeObserve = await store.GetExistingAsync(sessionId);
        var checkpointBeforeObserve = await cache.GetCheckpointAsync(sessionId);

        var observeResult = await PlayRouteHandlers.HandleObserveAsync(
            sessionId,
            store,
            cache,
            browserStore,
            CancellationToken.None
        );
        var observe = await ExecuteResultAsync<PlayObserveResponse>(observeResult);

        var ledgerAfterObserve = await store.GetExistingAsync(sessionId);
        var checkpointAfterObserve = await cache.GetCheckpointAsync(sessionId);

        Assert(observe.RuntimeBundle is null, "observe must drop runtime bundle metadata when its lineage no longer matches stored state");
        Assert(observe.Checkpoint.SceneRevision == "scene-r9", "observe must keep stored checkpoint scene revision");
        Assert(observe.Checkpoint.ProjectionFingerprint == "runtime-observe", "observe must keep stored checkpoint runtime fingerprint");
        Assert(observe.Projection.Timeline.Contains("pending:evt-pending", StringComparer.Ordinal), "observe must preserve stored pending replay events");
        Assert(LedgerValueEquals(ledgerBeforeObserve, ledgerAfterObserve), "observe must not mutate the stored ledger during a read path");
        Assert(Equals(checkpointBeforeObserve, checkpointAfterObserve), "observe must not rewrite the stored checkpoint during a read path");
    }
    finally
    {
        await app.DisposeAsync();
    }
}

static async Task VerifyObserveKeepsRequestedSessionIdWhenStoredCheckpointDriftsAsync()
{
    const string sessionId = "session-observe-route";
    const string driftedSessionId = "session-observe-drift";
    var app = PlayWebApplication.Build([]);

    try
    {
        var browserStore = app.Services.GetRequiredService<IBrowserKeyValueStore>();
        await browserStore.SetAsync(
            PlayBrowserStateKeys.Checkpoint(sessionId),
            new SyncCheckpoint(driftedSessionId, "scene-observe", "scene-r11", "runtime-observe", 11, DateTimeOffset.UtcNow)
        );
        await browserStore.SetAsync(
            PlayBrowserStateKeys.Continuity(driftedSessionId),
            new ObserverContinuityEntry(
                driftedSessionId,
                "scene-observe",
                "scene-r11",
                "runtime-observe",
                "observer-drift",
                "device-drift",
                PlaySurfaceRole.Player,
                11,
                DateTimeOffset.UtcNow,
                "token-drift"
            )
        );
        await browserStore.SetAsync(
            PlayBrowserStateKeys.Continuity(sessionId),
            new ObserverContinuityEntry(
                sessionId,
                "scene-observe",
                "scene-r11",
                "runtime-observe",
                "observer-route",
                "device-route",
                PlaySurfaceRole.GameMaster,
                11,
                DateTimeOffset.UtcNow,
                "token-route"
            )
        );

        var observe = await ExecuteRouteRequestAsync<PlayObserveResponse>(
            app,
            HttpMethod.Get,
            PlayApiRoutes.Observe,
            routeValues: new Dictionary<string, string> { ["sessionId"] = sessionId }
        );

        Assert(observe.SessionId == sessionId, "observe route must preserve the requested session id");
        Assert(observe.Checkpoint.SessionId == sessionId, "observe checkpoint must normalize corrupted stored session ids back to the route session id");
        Assert(observe.Projection.Cursor.Session.SessionId == sessionId, "observe projection must keep the route session id authoritative");
        Assert(observe.Continuity is not null, "observe should return continuity stored under the requested session id when lineage matches");
        Assert(observe.Continuity!.ObserverId == "observer-route", "observe must not read continuity data from a drifted stored session id");
    }
    finally
    {
        await app.DisposeAsync();
    }
}

static async Task VerifyObservePreservesContinuityWhenClaimCursorLeadsLedgerAsync()
{
    const string sessionId = "session-observe-claim-ahead";
    var app = PlayWebApplication.Build([]);

    try
    {
        var store = app.Services.GetRequiredService<BrowserSessionEventLogStore>();
        var cache = app.Services.GetRequiredService<BrowserSessionOfflineCacheService>();
        var browserStore = app.Services.GetRequiredService<IBrowserKeyValueStore>();
        await store.AppendPendingEventsAsync(sessionId, "scene-observe", "scene-r12", "runtime-observe", ["evt-pending"], 9);
        await cache.SetCheckpointAsync(
            new SyncCheckpoint(sessionId, "scene-observe", "scene-r12", "runtime-observe", 9, DateTimeOffset.UtcNow)
        );

        var claimResult = await PlayRouteHandlers.HandleContinuityClaimAsync(
            new PlayContinuityClaimRequest(
                new EngineSessionCursor(
                    new EngineSessionEnvelope(sessionId, "scene-observe", "scene-r12", "runtime-observe"),
                    12
                ),
                PlaySurfaceRole.Player,
                "device-player-a",
                "observer-player-a"
            ),
            store,
            cache,
            browserStore,
            CancellationToken.None
        );
        var continuityClaim = await ExecuteResultAsync<PlayContinuityClaimResponse>(claimResult);
        Assert(continuityClaim.Accepted, "continuity claim should be accepted when cursor leads ledger but lineage matches");
        Assert(continuityClaim.Checkpoint.AppliedThroughSequence == 12, "continuity claim must align checkpoint to the leading cursor sequence");

        var observeResult = await PlayRouteHandlers.HandleObserveAsync(
            sessionId,
            store,
            cache,
            browserStore,
            CancellationToken.None
        );
        var observe = await ExecuteResultAsync<PlayObserveResponse>(observeResult);
        Assert(observe.Continuity is not null, "observe must preserve continuity metadata after a leading-cursor claim");
        Assert(observe.Continuity!.ObservedThroughSequence == 12, "observe continuity sequence must match checkpoint-aligned claim sequence");
        Assert(observe.Checkpoint.AppliedThroughSequence == 12, "observe checkpoint sequence must stay checkpoint-aligned");
        Assert(observe.Projection.Cursor.AppliedThroughSequence == 12, "observe projection cursor sequence must align with checkpoint sequence");
    }
    finally
    {
        await app.DisposeAsync();
    }
}

static async Task VerifyObserveRouteRoundTripAsync()
{
    const string sessionId = "session-observe-routes";
    var app = PlayWebApplication.Build([]);

    try
    {
        var store = app.Services.GetRequiredService<BrowserSessionEventLogStore>();
        var cache = app.Services.GetRequiredService<BrowserSessionOfflineCacheService>();
        var browserStore = app.Services.GetRequiredService<IBrowserKeyValueStore>();
        await store.AppendPendingEventsAsync(sessionId, "scene-observe", "scene-r13", "runtime-observe", ["evt-pending"], 13);
        await cache.SetCheckpointAsync(
            new SyncCheckpoint(sessionId, "scene-observe", "scene-r13", "runtime-observe", 13, DateTimeOffset.UtcNow)
        );

        var claim = await PlayRouteHandlers.HandleContinuityClaimAsync(
            new PlayContinuityClaimRequest(
                new EngineSessionCursor(
                    new EngineSessionEnvelope(sessionId, "scene-observe", "scene-r13", "runtime-observe"),
                    13
                ),
                PlaySurfaceRole.GameMaster,
                "device-routes",
                "observer-routes"
            ),
            store,
            cache,
            browserStore,
            CancellationToken.None
        );
        var continuityClaim = await ExecuteResultAsync<PlayContinuityClaimResponse>(claim);
        Assert(continuityClaim.Accepted, "continuity claim setup must accept aligned lineage before route observe");

        var observe = await ExecuteRouteRequestAsync<PlayObserveResponse>(
            app,
            HttpMethod.Get,
            PlayApiRoutes.Observe,
            routeValues: new Dictionary<string, string> { ["sessionId"] = sessionId }
        );
        Assert(observe.Continuity is not null, "observe route must round-trip continuity metadata");
        Assert(observe.Continuity!.ObserverId == "observer-routes", "observe route must serialize continuity payloads correctly");
        Assert(observe.Projection.Cursor.AppliedThroughSequence == 13, "observe route must preserve checkpoint-aligned sequence state");
    }
    finally
    {
        await app.DisposeAsync();
    }
}

static async Task VerifyQueueMutationLineageExceptionReturnsStaleAsync()
{
    const string sessionId = "session-race";
    var storeState = new InMemoryBrowserKeyValueStore();
    var store = new BrowserSessionEventLogStore(storeState);
    var cache = new BrowserSessionOfflineCacheService(storeState);
    var requestSession = new EngineSessionEnvelope(sessionId, "scene-a", "scene-r1", "runtime-a");
    await store.AppendPendingEventsAsync(sessionId, requestSession.SceneId, requestSession.SceneRevision, requestSession.RuntimeFingerprint, ["evt-0"], 1);
    await cache.SetCheckpointAsync(
        new SyncCheckpoint(sessionId, requestSession.SceneId, requestSession.SceneRevision, requestSession.RuntimeFingerprint, 1, DateTimeOffset.UtcNow)
    );

    var driftedSession = new EngineSessionEnvelope(sessionId, "scene-drift", "scene-r2", "runtime-drift");
    var quickActionResult = await PlayRouteHandlers.HandleQuickActionAsync(
        new PlayQuickActionRequest(new EngineSessionCursor(requestSession, 1), PlaySurfaceRole.Player, "player-mark-ready"),
        cache,
        store,
        new ThrowingLineageDriftQueueService(store, cache, driftedSession, throwOnEnqueue: true, throwOnSync: false),
        PlayerShellModule.CreateDescriptor(),
        GmTacticalShellModule.CreateDescriptor(),
        CancellationToken.None
    );
    var quickActionResponse = await ExecuteResultAsync<PlayQuickActionResponse>(quickActionResult);
    Assert(!quickActionResponse.Accepted, "quick action must reject race-condition lineage drift as stale instead of 500");
    Assert(quickActionResponse.Stale, "quick action must report stale after queue-level lineage exception");
    Assert(quickActionResponse.Projection.Cursor.Session.SceneId == driftedSession.SceneId, "quick action stale response must use refreshed stored lineage");
    Assert(quickActionResponse.Projection.Timeline.Contains("pending:evt-drift", StringComparer.Ordinal), "quick action stale response must preserve refreshed pending events");

    var syncResult = await PlayRouteHandlers.HandleSyncAsync(
        new PlaySyncRequest(new EngineSessionCursor(requestSession, 1), ["evt-client"]),
        cache,
        store,
        new ThrowingLineageDriftQueueService(store, cache, driftedSession, throwOnEnqueue: false, throwOnSync: true),
        CancellationToken.None
    );
    var syncResponse = await ExecuteResultAsync<PlaySyncResponse>(syncResult);
    Assert(!syncResponse.Accepted, "sync must reject race-condition lineage drift as stale instead of 500");
    Assert(syncResponse.Stale, "sync must report stale after queue-level lineage exception");
    Assert(syncResponse.Projection.Cursor.Session.SceneRevision == driftedSession.SceneRevision, "sync stale response must use refreshed stored lineage");
    Assert(syncResponse.Projection.Timeline.Contains("pending:evt-drift", StringComparer.Ordinal), "sync stale response must preserve refreshed pending events");
}

static async Task VerifyBootstrapRejectsStaleLineageWithoutLedgerResetAsync()
{
    const string sessionId = "session-bootstrap-stale";
    var app = PlayWebApplication.Build([]);

    try
    {
        var store = app.Services.GetRequiredService<BrowserSessionEventLogStore>();
        var cache = app.Services.GetRequiredService<BrowserSessionOfflineCacheService>();
        await store.AppendPendingEventsAsync(sessionId, "scene-stored", "scene-r4", "runtime-stored", ["evt-stored"], 9);
        await cache.SetCheckpointAsync(
            new SyncCheckpoint(sessionId, "scene-stored", "scene-r4", "runtime-stored", 9, DateTimeOffset.UtcNow)
        );

        var query = $"?sessionId={Uri.EscapeDataString(sessionId)}&role={Uri.EscapeDataString(PlaySurfaceRole.Player.ToString())}&sceneId=scene-request&sceneRevision=scene-r1&runtimeFingerprint=runtime-request";
        var bootstrap = await ExecuteRouteRequestAsync<PlayBootstrapResponse>(
            app,
            HttpMethod.Get,
            PlayApiRoutes.Bootstrap,
            query,
            expectedStatusCode: StatusCodes.Status200OK
        );

        Assert(bootstrap.Projection.Cursor.Session.SceneId == "scene-stored", "bootstrap endpoint must normalize stale request lineage to stored scene id");
        Assert(bootstrap.Projection.Cursor.Session.SceneRevision == "scene-r4", "bootstrap endpoint must normalize stale request lineage to stored scene revision");
        Assert(bootstrap.Projection.Cursor.Session.RuntimeFingerprint == "runtime-stored", "bootstrap endpoint must normalize stale request lineage to stored runtime fingerprint");
        Assert(bootstrap.Projection.Cursor.AppliedThroughSequence == 9, "bootstrap endpoint must preserve stored sequence ownership");
        Assert(bootstrap.Projection.Timeline.Contains("pending:evt-stored", StringComparer.Ordinal), "bootstrap endpoint must preserve pending replay events during stale request recovery");

        var ledgerAfterBootstrap = await store.GetExistingAsync(sessionId);
        Assert(ledgerAfterBootstrap is not null, "bootstrap endpoint must keep stored ledger");
        Assert(ledgerAfterBootstrap!.SceneId == "scene-stored", "bootstrap endpoint must not reset stored ledger scene");
        Assert(ledgerAfterBootstrap.LastKnownSequence == 9, "bootstrap endpoint must not reset stored ledger sequence ownership");
        Assert(ledgerAfterBootstrap.PendingEvents.Count == 1 && ledgerAfterBootstrap.PendingEvents[0] == "evt-stored", "bootstrap endpoint must not drop stored pending events");

        var checkpointAfterBootstrap = await cache.GetCheckpointAsync(sessionId);
        Assert(checkpointAfterBootstrap is not null, "bootstrap endpoint must preserve checkpoint");
        Assert(checkpointAfterBootstrap!.SceneId == "scene-stored", "bootstrap endpoint must preserve checkpoint scene lineage");
        Assert(checkpointAfterBootstrap.SceneRevision == "scene-r4", "bootstrap endpoint must preserve checkpoint revision lineage");
        Assert(checkpointAfterBootstrap.ProjectionFingerprint == "runtime-stored", "bootstrap endpoint must preserve checkpoint runtime lineage");
        Assert(checkpointAfterBootstrap.AppliedThroughSequence == 9, "bootstrap endpoint must preserve checkpoint sequence ownership");
    }
    finally
    {
        await app.DisposeAsync();
    }
}

static async Task VerifyProjectionPrefersStoredLedgerWithoutCheckpointAsync()
{
    const string sessionId = "session-projection-ledger";
    var storeState = new InMemoryBrowserKeyValueStore();
    var store = new BrowserSessionEventLogStore(storeState);
    var cache = new BrowserSessionOfflineCacheService(storeState);

    await store.AppendPendingEventsAsync(sessionId, "scene-ledger", "scene-r7", "runtime-ledger", ["evt-ledger"], 12);

    var (session, ledger) = await PlayWebApplication.ResolveProjectionSessionAsync(
        sessionId,
        store,
        cache,
        CancellationToken.None
    );

    Assert(session.SceneId == "scene-ledger", "projection must prefer stored ledger scene id when checkpoint is missing");
    Assert(session.SceneRevision == "scene-r7", "projection must prefer stored ledger scene revision when checkpoint is missing");
    Assert(session.RuntimeFingerprint == "runtime-ledger", "projection must prefer stored ledger runtime when checkpoint is missing");
    Assert(ledger.PendingEvents.Count == 1 && ledger.PendingEvents[0] == "evt-ledger", "projection must preserve stored pending events");
    Assert(ledger.LastKnownSequence == 12, "projection must preserve stored sequence ownership");
}

static async Task VerifyResumePreservesLedgerWhenRuntimeBundleDriftsAsync()
{
    const string sessionId = "session-resume-lineage";
    var storeState = new InMemoryBrowserKeyValueStore();
    var store = new BrowserSessionEventLogStore(storeState);
    var cache = new BrowserSessionOfflineCacheService(storeState);

    await store.AppendPendingEventsAsync(sessionId, "scene-ledger", "scene-r9", "runtime-ledger", ["evt-pending"], 6);
    await cache.SetCheckpointAsync(
        new SyncCheckpoint(sessionId, "scene-ledger", "scene-r9", "runtime-ledger", 6, DateTimeOffset.UtcNow)
    );
    await cache.CacheRuntimeBundleAsync(
        new RuntimeBundleCacheEntry(
            sessionId,
            "runtime-stale",
            "scene-r3",
            "bundle:scene-ledger:runtime-stale",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow
        )
    );

    var resumeState = await PlayWebApplication.ResolveResumeStateAsync(
        sessionId,
        store,
        cache,
        CancellationToken.None
    );

    Assert(resumeState.Session.SceneRevision == "scene-r9", "resume must keep checkpoint scene revision when runtime metadata drifts");
    Assert(resumeState.Session.RuntimeFingerprint == "runtime-ledger", "resume must keep checkpoint runtime when runtime metadata drifts");
    Assert(resumeState.Ledger.PendingEvents.Count == 1 && resumeState.Ledger.PendingEvents[0] == "evt-pending", "resume must preserve stored pending events");
    Assert(resumeState.Ledger.LastKnownSequence == 6, "resume must preserve stored sequence ownership");
}

static async Task VerifyResumeNormalizesCheckpointToLedgerLineageAsync()
{
    const string sessionId = "session-resume-normalize";
    var storeState = new InMemoryBrowserKeyValueStore();
    var store = new BrowserSessionEventLogStore(storeState);
    var cache = new BrowserSessionOfflineCacheService(storeState);

    await store.AppendPendingEventsAsync(sessionId, "scene-ledger", "scene-r8", "runtime-ledger", ["evt-a"], 7);
    await cache.SetCheckpointAsync(
        new SyncCheckpoint(sessionId, "scene-old", "scene-r1", "runtime-old", 3, DateTimeOffset.UtcNow)
    );

    var resumeState = await PlayWebApplication.ResolveResumeStateAsync(
        sessionId,
        store,
        cache,
        CancellationToken.None
    );

    Assert(resumeState.Checkpoint is not null, "resume must emit a checkpoint");
    var checkpoint = resumeState.Checkpoint!;
    Assert(checkpoint.SceneId == "scene-ledger", "resume must normalize checkpoint scene id to ledger lineage");
    Assert(checkpoint.SceneRevision == "scene-r8", "resume must normalize checkpoint revision to ledger lineage");
    Assert(checkpoint.ProjectionFingerprint == "runtime-ledger", "resume must normalize checkpoint runtime to ledger lineage");
    Assert(checkpoint.AppliedThroughSequence == 7, "resume must advance checkpoint sequence to ledger ownership");
}

static async Task VerifyRuntimeBundleSessionLockReleasesOnCanceledAcquireAsync()
{
    const string sessionId = "session-runtime-lock-cancel";
    var browserStore = new CoordinatedRuntimeWriteBrowserStore(sessionId);
    var cache = new BrowserSessionOfflineCacheService(browserStore);
    browserStore.EnableReadTouchWriteBarrier();

    var holdTask = cache.CacheRuntimeBundleAsync(
        new RuntimeBundleCacheEntry(
            sessionId,
            "runtime-initial",
            "scene-r1",
            "bundle-lock-initial",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow
        ),
        CancellationToken.None
    );
    await browserStore.WaitForBlockedReadTouchWriteAsync();

    using var cancelSource = new CancellationTokenSource();
    cancelSource.Cancel();
    await AssertThrowsAsync<OperationCanceledException>(
        () => cache.GetRuntimeBundleAsync(sessionId, cancelSource.Token),
        "runtime-bundle lock acquire cancellation must propagate operation canceled"
    );

    browserStore.ReleaseReadTouchWriteBarrier();
    await holdTask;
    await cache.GetRuntimeBundleAsync(sessionId, CancellationToken.None);

    var lockCount = GetRuntimeBundleSessionLockCount();
    Assert(lockCount == 0, "runtime-bundle session locks must be released after canceled acquire attempts");
}

static int GetRuntimeBundleSessionLockCount()
{
    var field = typeof(BrowserSessionOfflineCacheService).GetField(
        "RuntimeBundleSessionLocks",
        BindingFlags.NonPublic | BindingFlags.Static
    ) ?? throw new InvalidOperationException("Could not access runtime bundle lock state for regression validation.");

    var dictionary = field.GetValue(null) as System.Collections.IDictionary
        ?? throw new InvalidOperationException("Runtime bundle lock state has an unexpected type.");
    return dictionary.Count;
}

static void VerifyStoredStaleStatePrefersLedgerOverOlderCheckpoint()
{
    var requestSession = new EngineSessionEnvelope("session-stale-priority", "scene-request", "scene-r9", "runtime-request");
    var requestCursor = new EngineSessionCursor(requestSession, 12);
    var checkpoint = new SyncCheckpoint(
        "session-stale-priority",
        "scene-checkpoint",
        "scene-r3",
        "runtime-checkpoint",
        4,
        DateTimeOffset.UtcNow
    );
    var ledger = new Chummer.Play.Core.Offline.OfflineLedgerEnvelope(
        "session-stale-priority",
        "scene-ledger",
        "scene-r7",
        "runtime-ledger",
        ["evt-ledger"],
        8,
        DateTimeOffset.UtcNow,
        null,
        0
    );

    var staleState = PlayRouteHandlers.BuildStoredStaleState(requestSession, requestCursor, checkpoint, ledger);

    Assert(staleState.Projection.Cursor.Session.SceneId == "scene-ledger", "stale projection must prefer ledger scene over older checkpoint");
    Assert(staleState.Projection.Cursor.Session.SceneRevision == "scene-r7", "stale projection must prefer ledger revision over older checkpoint");
    Assert(staleState.Projection.Cursor.Session.RuntimeFingerprint == "runtime-ledger", "stale projection must prefer ledger runtime over older checkpoint");
    Assert(staleState.Checkpoint.SceneId == "scene-ledger", "stale checkpoint must normalize to the stored ledger scene when checkpoint is older");
    Assert(staleState.Checkpoint.SceneRevision == "scene-r7", "stale checkpoint must normalize to the stored ledger revision when checkpoint is older");
    Assert(staleState.Checkpoint.ProjectionFingerprint == "runtime-ledger", "stale checkpoint must normalize to the stored ledger runtime when checkpoint is older");
    Assert(staleState.Checkpoint.AppliedThroughSequence == 8, "stale checkpoint sequence must advance to the newer ledger sequence");
}

static async Task<TResponse> ExecuteResultAsync<TResponse>(IResult result)
{
    var context = new DefaultHttpContext
    {
        RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider(),
    };
    context.Response.Body = new MemoryStream();

    await result.ExecuteAsync(context);

    if (context.Response.StatusCode is < 200 or >= 300)
    {
        throw new InvalidOperationException($"Expected success response, got {context.Response.StatusCode}.");
    }

    context.Response.Body.Position = 0;
    using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
    var json = await reader.ReadToEndAsync();
    return JsonSerializer.Deserialize<TResponse>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))
        ?? throw new InvalidOperationException("Expected JSON response payload.");
}

static async Task<TResponse> ExecuteResultWithStatusAsync<TResponse>(IResult result, int expectedStatusCode)
{
    var context = new DefaultHttpContext
    {
        RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider(),
    };
    context.Response.Body = new MemoryStream();

    await result.ExecuteAsync(context);

    if (context.Response.StatusCode != expectedStatusCode)
    {
        throw new InvalidOperationException($"Expected status {expectedStatusCode}, got {context.Response.StatusCode}.");
    }

    context.Response.Body.Position = 0;
    using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
    var json = await reader.ReadToEndAsync();
    return JsonSerializer.Deserialize<TResponse>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))
        ?? throw new InvalidOperationException("Expected JSON response payload.");
}

static void VerifyCheckpointLineageAlignment()
{
    var checkpoint = new SyncCheckpoint("session-c", "scene-a", "scene-r1", "runtime-a", 5, DateTimeOffset.UtcNow);
    var aligned = new EngineSessionEnvelope("session-c", "scene-a", "scene-r1", "runtime-a");
    var mismatchedRuntime = new EngineSessionEnvelope("session-c", "scene-a", "scene-r1", "runtime-b");

    Assert(SessionLineage.IsCheckpointAligned(checkpoint, aligned), "aligned checkpoint lineage must pass");
    Assert(!SessionLineage.IsCheckpointAligned(checkpoint, mismatchedRuntime), "runtime mismatch must fail stale protection");
}

static void VerifyStoredLineageAlignment()
{
    var session = new EngineSessionEnvelope("session-e", "scene-a", "scene-r1", "runtime-a");
    var checkpoint = new SyncCheckpoint("session-e", "scene-a", "scene-r1", "runtime-a", 1, DateTimeOffset.UtcNow);
    var ledger = new Chummer.Play.Core.Offline.OfflineLedgerEnvelope(
        "session-e",
        "scene-a",
        "scene-r1",
        "runtime-a",
        [],
        1,
        DateTimeOffset.UtcNow,
        null,
        0
    );
    var mismatchedLedger = ledger with { RuntimeFingerprint = "runtime-b" };

    Assert(SessionLineage.IsStoredLineageAligned(session, checkpoint, ledger), "stored lineage must pass when checkpoint and ledger match");
    Assert(!SessionLineage.IsStoredLineageAligned(session, checkpoint, mismatchedLedger), "stored lineage must fail when ledger lineage changes");
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static bool LedgerValueEquals(OfflineLedgerEnvelope? left, OfflineLedgerEnvelope? right)
{
    if (ReferenceEquals(left, right))
    {
        return true;
    }

    if (left is null || right is null)
    {
        return false;
    }

    return left.SessionId == right.SessionId
        && left.SceneId == right.SceneId
        && left.SceneRevision == right.SceneRevision
        && left.RuntimeFingerprint == right.RuntimeFingerprint
        && left.LastKnownSequence == right.LastKnownSequence
        && left.LastAcceptedEventCount == right.LastAcceptedEventCount
        && left.PendingEvents.SequenceEqual(right.PendingEvents, StringComparer.Ordinal)
        && left.UpdatedAtUtc == right.UpdatedAtUtc
        && left.LastSyncedAtUtc == right.LastSyncedAtUtc;
}

static async Task AssertThrowsAsync<TException>(Func<Task> action, string message)
    where TException : Exception
{
    try
    {
        await action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException(message);
}

static BrowserSessionApiClient CreateApiClient(HttpMessageHandler handler) =>
    new(
        new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost"),
        }
    );

static string GetRepoRoot()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "WORKLIST.md")))
        {
            return directory.FullName;
        }

        directory = directory.Parent;
    }

    throw new InvalidOperationException("Could not locate repository root from regression checks runtime directory.");
}

static async Task<TResponse> ExecuteRouteRequestAsync<TResponse>(
    WebApplication app,
    HttpMethod method,
    string route,
    string query = "",
    string? jsonBody = null,
    IReadOnlyDictionary<string, string>? routeValues = null,
    int expectedStatusCode = StatusCodes.Status200OK
)
{
    var endpointRouteBuilder = (IEndpointRouteBuilder)app;
    var endpoint = endpointRouteBuilder
        .DataSources
        .SelectMany(static source => source.Endpoints)
        .OfType<RouteEndpoint>()
        .FirstOrDefault(candidate =>
            candidate.RequestDelegate is not null
            && MethodMatches(candidate, method)
            && RouteMatches(candidate, route)
        );
    if (endpoint is null)
    {
        throw new InvalidOperationException($"Could not resolve endpoint for route '{route}' and method '{method.Method}'.");
    }

    var context = new DefaultHttpContext
    {
        RequestServices = app.Services,
    };
    context.Request.Method = method.Method;
    context.Request.Scheme = "http";
    context.Request.Host = new HostString("localhost");
    context.Request.Path = NormalizePath(route);
    context.Request.QueryString = new QueryString(query);
    context.Response.Body = new MemoryStream();
    context.SetEndpoint(endpoint);
    if (routeValues is not null)
    {
        foreach (var routeValue in routeValues)
        {
            context.Request.RouteValues[routeValue.Key] = routeValue.Value;
        }
    }

    if (!string.IsNullOrWhiteSpace(jsonBody))
    {
        var bytes = Encoding.UTF8.GetBytes(jsonBody);
        context.Request.Body = new MemoryStream(bytes);
        context.Request.ContentLength = bytes.Length;
        context.Request.ContentType = "application/json";
    }

    await endpoint.RequestDelegate!(context);
    Assert(
        context.Response.StatusCode == expectedStatusCode,
        $"Expected status code {expectedStatusCode} for '{route}' but received {context.Response.StatusCode}."
    );

    context.Response.Body.Position = 0;
    using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
    var payload = await reader.ReadToEndAsync();
    return JsonSerializer.Deserialize<TResponse>(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web))
        ?? throw new InvalidOperationException($"Route '{route}' returned an empty JSON payload.");
}

static async Task<(int StatusCode, string Location)> ExecuteRouteResponseAsync(
    WebApplication app,
    HttpMethod method,
    string route,
    string query = "",
    string? jsonBody = null,
    IReadOnlyDictionary<string, string>? routeValues = null
)
{
    var endpointRouteBuilder = (IEndpointRouteBuilder)app;
    var endpoint = endpointRouteBuilder
        .DataSources
        .SelectMany(static source => source.Endpoints)
        .OfType<RouteEndpoint>()
        .FirstOrDefault(candidate =>
            candidate.RequestDelegate is not null
            && MethodMatches(candidate, method)
            && RouteMatches(candidate, route)
        );
    if (endpoint is null)
    {
        throw new InvalidOperationException($"Could not resolve endpoint for route '{route}' and method '{method.Method}'.");
    }

    var context = new DefaultHttpContext
    {
        RequestServices = app.Services,
    };
    context.Request.Method = method.Method;
    context.Request.Scheme = "http";
    context.Request.Host = new HostString("localhost");
    context.Request.Path = NormalizePath(route);
    context.Request.QueryString = new QueryString(query);
    context.Response.Body = new MemoryStream();
    context.SetEndpoint(endpoint);
    if (routeValues is not null)
    {
        foreach (var routeValue in routeValues)
        {
            context.Request.RouteValues[routeValue.Key] = routeValue.Value;
        }
    }

    if (!string.IsNullOrWhiteSpace(jsonBody))
    {
        var bytes = Encoding.UTF8.GetBytes(jsonBody);
        context.Request.Body = new MemoryStream(bytes);
        context.Request.ContentLength = bytes.Length;
        context.Request.ContentType = "application/json";
    }

    await endpoint.RequestDelegate!(context);
    return (context.Response.StatusCode, context.Response.Headers.Location.ToString());
}

static async Task<(int StatusCode, string Body)> ExecuteRouteBodyResponseAsync(
    WebApplication app,
    HttpMethod method,
    string route,
    string query = "",
    string? jsonBody = null,
    IReadOnlyDictionary<string, string>? routeValues = null)
{
    var endpointRouteBuilder = (IEndpointRouteBuilder)app;
    var endpoint = endpointRouteBuilder
        .DataSources
        .SelectMany(static source => source.Endpoints)
        .OfType<RouteEndpoint>()
        .FirstOrDefault(candidate =>
            candidate.RequestDelegate is not null
            && MethodMatches(candidate, method)
            && RouteMatches(candidate, route));
    if (endpoint is null)
    {
        throw new InvalidOperationException($"Could not resolve endpoint for route '{route}' and method '{method.Method}'.");
    }

    var context = new DefaultHttpContext
    {
        RequestServices = app.Services,
    };
    context.Request.Method = method.Method;
    context.Request.Scheme = "http";
    context.Request.Host = new HostString("localhost");
    context.Request.Path = NormalizePath(route);
    context.Request.QueryString = new QueryString(query);
    context.Response.Body = new MemoryStream();
    context.SetEndpoint(endpoint);
    if (routeValues is not null)
    {
        foreach (var routeValue in routeValues)
        {
            context.Request.RouteValues[routeValue.Key] = routeValue.Value;
        }
    }

    if (!string.IsNullOrWhiteSpace(jsonBody))
    {
        var bytes = Encoding.UTF8.GetBytes(jsonBody);
        context.Request.Body = new MemoryStream(bytes);
        context.Request.ContentLength = bytes.Length;
        context.Request.ContentType = "application/json";
    }

    await endpoint.RequestDelegate!(context);
    context.Response.Body.Position = 0;
    using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
    string body = await reader.ReadToEndAsync();
    return (context.Response.StatusCode, body);
}

static (string Route, string Query) SplitHref(string href)
{
    int separator = href.IndexOf('?', StringComparison.Ordinal);
    if (separator < 0)
    {
        return (href, string.Empty);
    }

    return (href[..separator], href[separator..]);
}

static bool MethodMatches(RouteEndpoint endpoint, HttpMethod method)
{
    var methods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods;
    return methods is null || methods.Any(allowed => string.Equals(allowed, method.Method, StringComparison.OrdinalIgnoreCase));
}

static bool RouteMatches(RouteEndpoint endpoint, string route)
{
    var endpointRoute = endpoint.RoutePattern.RawText ?? endpoint.RoutePattern.ToString();
    var normalizedEndpointRoute = NormalizePath(endpointRoute);
    var normalizedRoute = NormalizePath(route);
    if (string.Equals(normalizedEndpointRoute, normalizedRoute, StringComparison.OrdinalIgnoreCase))
    {
        return true;
    }

    var endpointSegments = normalizedEndpointRoute.Split('/', StringSplitOptions.RemoveEmptyEntries);
    var routeSegments = normalizedRoute.Split('/', StringSplitOptions.RemoveEmptyEntries);
    if (endpointSegments.Length != routeSegments.Length)
    {
        return false;
    }

    for (var index = 0; index < endpointSegments.Length; index++)
    {
        var endpointSegment = endpointSegments[index];
        if (endpointSegment.StartsWith("{", StringComparison.Ordinal)
            && endpointSegment.EndsWith("}", StringComparison.Ordinal))
        {
            continue;
        }

        if (!string.Equals(endpointSegment, routeSegments[index], StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
    }

    return true;
}

static string NormalizePath(string? path)
{
    if (string.IsNullOrWhiteSpace(path))
    {
        return "/";
    }

    var normalized = path.Trim();
    if (!normalized.StartsWith("/", StringComparison.Ordinal))
    {
        normalized = "/" + normalized;
    }

    return normalized.Length > 1
        ? normalized.TrimEnd('/')
        : normalized;
}

static DefaultHttpContext BuildPlayApiBoundaryContext(
    string environmentName,
    string? configuredApiKey,
    string? suppliedApiKey,
    IPAddress remoteAddress)
{
    IConfiguration configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["CHUMMER_PLAY_API_KEY"] = configuredApiKey
        })
        .Build();
    var services = new ServiceCollection()
        .AddSingleton(configuration)
        .AddSingleton<IHostEnvironment>(new RegressionHostEnvironment(environmentName))
        .BuildServiceProvider();
    var context = new DefaultHttpContext
    {
        RequestServices = services
    };
    context.Request.Path = "/api/play/resume/session-boundary";
    context.Connection.RemoteIpAddress = remoteAddress;
    if (!string.IsNullOrWhiteSpace(suppliedApiKey))
    {
        context.Request.Headers["X-Chummer-Play-Api-Key"] = suppliedApiKey;
    }

    return context;
}

static async Task<(bool NextCalled, int StatusCode, string CacheControl, string Pragma, string Expires, string Body)> InvokePlayApiBoundaryAsync(
    string environmentName,
    string? configuredApiKey,
    string? suppliedApiKey,
    IPAddress remoteAddress)
{
    var context = BuildPlayApiBoundaryContext(
        environmentName,
        configuredApiKey,
        suppliedApiKey,
        remoteAddress);
    context.Response.Body = new MemoryStream();

    var nextCalled = false;
    await PlayWebApplication.RequireTrustedPlayApiBoundaryAsync(
        context,
        innerContext =>
        {
            nextCalled = true;
            innerContext.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        });
    await context.Response.StartAsync();

    context.Response.Body.Position = 0;
    using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
    string body = await reader.ReadToEndAsync();
    return (
        nextCalled,
        context.Response.StatusCode,
        context.Response.Headers.CacheControl.ToString(),
        context.Response.Headers.Pragma.ToString(),
        context.Response.Headers.Expires.ToString(),
        body);
}

file sealed record ReconnectConflictPayload(
    string Error,
    bool Stale,
    PlaySessionProjection Projection,
    SyncCheckpoint Checkpoint
);

file sealed class RegressionHostEnvironment : IHostEnvironment
{
    public RegressionHostEnvironment(string environmentName)
    {
        EnvironmentName = environmentName;
    }

    public string EnvironmentName { get; set; }

    public string ApplicationName { get; set; } = "Chummer.Play.RegressionChecks";

    public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();

    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}

file sealed class CoordinatedRuntimeWriteBrowserStore : IBrowserKeyValueStore
{
    private readonly InMemoryBrowserKeyValueStore _inner = new();
    private readonly string _runtimeBundleKey;
    private readonly TaskCompletionSource _readTouchWriteBlocked =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _readTouchWriteRelease =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private volatile bool _barrierEnabled;

    public CoordinatedRuntimeWriteBrowserStore(string sessionId)
    {
        _runtimeBundleKey = PlayBrowserStateKeys.RuntimeBundle(sessionId);
    }

    public void EnableReadTouchWriteBarrier() => _barrierEnabled = true;

    public async Task WaitForBlockedReadTouchWriteAsync()
    {
        await _readTouchWriteBlocked.Task;
    }

    public void ReleaseReadTouchWriteBarrier()
    {
        _readTouchWriteRelease.TrySetResult();
    }

    public Task<TValue?> GetAsync<TValue>(string key, CancellationToken cancellationToken = default)
    {
        return _inner.GetAsync<TValue>(key, cancellationToken);
    }

    public async Task SetAsync<TValue>(string key, TValue value, CancellationToken cancellationToken = default)
    {
        if (_barrierEnabled
            && string.Equals(key, _runtimeBundleKey, StringComparison.Ordinal)
            && value is RuntimeBundleCacheEntry runtimeBundleEntry
            && string.Equals(runtimeBundleEntry.RuntimeFingerprint, "runtime-initial", StringComparison.Ordinal))
        {
            _readTouchWriteBlocked.TrySetResult();
            await _readTouchWriteRelease.Task.WaitAsync(cancellationToken);
        }

        await _inner.SetAsync(key, value, cancellationToken);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        return _inner.RemoveAsync(key, cancellationToken);
    }

    public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
    {
        return _inner.ListKeysAsync(prefix, cancellationToken);
    }
}

file sealed class DelayedMutationBrowserStore : IBrowserKeyValueStore
{
    private readonly InMemoryBrowserKeyValueStore _inner = new();
    private readonly TimeSpan _mutationDelay;

    public DelayedMutationBrowserStore(TimeSpan mutationDelay)
    {
        _mutationDelay = mutationDelay;
    }

    public Task<TValue?> GetAsync<TValue>(string key, CancellationToken cancellationToken = default)
    {
        return _inner.GetAsync<TValue>(key, cancellationToken);
    }

    public async Task SetAsync<TValue>(string key, TValue value, CancellationToken cancellationToken = default)
    {
        await Task.Delay(_mutationDelay, cancellationToken);
        await _inner.SetAsync(key, value, cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await Task.Delay(_mutationDelay, cancellationToken);
        await _inner.RemoveAsync(key, cancellationToken);
    }

    public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
    {
        return _inner.ListKeysAsync(prefix, cancellationToken);
    }
}

file sealed class GateableGetBrowserStore : IBrowserKeyValueStore
{
    private readonly InMemoryBrowserKeyValueStore _inner = new();
    private readonly string _gatedKey;
    private readonly TaskCompletionSource _getStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _releaseGet = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private volatile bool _gateEnabled;

    public GateableGetBrowserStore(string gatedKey)
    {
        _gatedKey = gatedKey;
    }

    public void EnableGate()
    {
        _gateEnabled = true;
    }

    public async Task<TValue?> GetAsync<TValue>(string key, CancellationToken cancellationToken = default)
    {
        if (_gateEnabled && string.Equals(key, _gatedKey, StringComparison.Ordinal))
        {
            _getStarted.TrySetResult();
            using var registration = cancellationToken.Register(() => _releaseGet.TrySetCanceled(cancellationToken));
            await _releaseGet.Task.WaitAsync(cancellationToken);
        }

        return await _inner.GetAsync<TValue>(key, cancellationToken);
    }

    public Task SetAsync<TValue>(string key, TValue value, CancellationToken cancellationToken = default)
    {
        return _inner.SetAsync(key, value, cancellationToken);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        return _inner.RemoveAsync(key, cancellationToken);
    }

    public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
    {
        return _inner.ListKeysAsync(prefix, cancellationToken);
    }

    public Task WaitForGetAsync()
    {
        return _getStarted.Task;
    }

    public void ReleaseGet()
    {
        _releaseGet.TrySetResult();
    }
}

file sealed class ThrowingLineageDriftQueueService : IPlayOfflineQueueService
{
    private readonly BrowserSessionEventLogStore _store;
    private readonly BrowserSessionOfflineCacheService _cache;
    private readonly EngineSessionEnvelope _driftedSession;
    private readonly bool _throwOnEnqueue;
    private readonly bool _throwOnSync;

    public ThrowingLineageDriftQueueService(
        BrowserSessionEventLogStore store,
        BrowserSessionOfflineCacheService cache,
        EngineSessionEnvelope driftedSession,
        bool throwOnEnqueue,
        bool throwOnSync
    )
    {
        _store = store;
        _cache = cache;
        _driftedSession = driftedSession;
        _throwOnEnqueue = throwOnEnqueue;
        _throwOnSync = throwOnSync;
    }

    public async Task<OfflineQueueEnqueueResult> EnqueueAsync(
        EngineSessionCursor cursor,
        string queuedEvent,
        CancellationToken cancellationToken = default
    )
    {
        if (!_throwOnEnqueue)
        {
            throw new NotSupportedException("Test queue is configured only for sync mutation failure.");
        }

        await DriftLineageAsync(cancellationToken);
        throw new InvalidOperationException("Stored lineage drifted during queue mutation.");
    }

    public async Task<OfflineQueueSyncResult> SyncReplayAsync(
        PlaySyncRequest request,
        CancellationToken cancellationToken = default
    )
    {
        if (!_throwOnSync)
        {
            throw new NotSupportedException("Test queue is configured only for quick-action mutation failure.");
        }

        await DriftLineageAsync(cancellationToken);
        throw new InvalidOperationException("Stored lineage drifted during queue mutation.");
    }

    private async Task DriftLineageAsync(CancellationToken cancellationToken)
    {
        await _store.GetOrCreateAsync(
            _driftedSession.SessionId,
            _driftedSession.SceneId,
            _driftedSession.SceneRevision,
            _driftedSession.RuntimeFingerprint,
            cancellationToken
        );
        await _store.AppendPendingEventsAsync(
            _driftedSession.SessionId,
            _driftedSession.SceneId,
            _driftedSession.SceneRevision,
            _driftedSession.RuntimeFingerprint,
            ["evt-drift"],
            2,
            cancellationToken
        );
        await _cache.SetCheckpointAsync(
            new SyncCheckpoint(
                _driftedSession.SessionId,
                _driftedSession.SceneId,
                _driftedSession.SceneRevision,
                _driftedSession.RuntimeFingerprint,
                2,
                DateTimeOffset.UtcNow
            ),
            cancellationToken
        );
    }
}

file sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

    public StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) =>
        _handler = handler;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => await _handler(request, cancellationToken);
}
