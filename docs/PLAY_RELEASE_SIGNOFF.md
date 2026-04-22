# Play Release Signoff

Purpose: keep the mobile/play share of `F0` explicit now that `E1` is already materially closed.

## Current hardening contract

`chummer6-mobile` stays release-complete for the current player/GM shell when `scripts/ai/verify.sh` keeps the following evidence executable:

- accessibility proof via `VerifyIndexShellAccessibilityContractAsync` and `VerifyBootstrapRoleShellEntryPointsAsync`
- performance-budget proof via `VerifyCachePressureBudgetContractAsync`
- replay/rejoin safety via the regression checks already tied into `M10` and `M11`
- package-only shared boundary consumption for `Chummer.Engine.Contracts`, `Chummer.Play.Contracts`, and `Chummer.Ui.Kit`

## Release budgets

- Accessibility: the installable shell must keep `<html lang="en">` and the polite live-status region in `src/Chummer.Play.Web/wwwroot/index.html`.
- Localization: the shell must keep explicit document-language and role-entry semantics instead of relying on implicit browser defaults.
- Performance: runtime-bundle cache pressure must preserve the current bounded quota budget (`RuntimeBundleQuota == 8`) and continue to report backpressure when the budget is saturated.

## Exit statement

The mobile/play head no longer blocks the product on shell reality, replay safety, accessibility, or bounded performance behavior. Remaining work is future product depth, not missing release proof for the current play shell.

## Post-closure completion criteria (M12)

The post-closure depth lane is considered executable when the following remain true and regression-backed:

- Player shell criteria: browser transport + event-log + offline resume stay lineage-safe, player role actions stay limited to player-safe capabilities even when capability descriptors are over-provisioned, and authorization denials preserve stored replay context instead of blanking the shell.
- GM shell criteria: GM-only action and Spider-card capability gates remain enforced even when player-safe capabilities are present, continuity/observe routes keep stale-lineage-safe behavior, and denied player-safe requests preserve stored replay context.
- Observer shell criteria: bootstrap and resume keep the lane read-mostly, surface observer-owned shell metadata, never inherit player quick actions, player write posture, or GM tactical cards, and keep denied quick-action attempts replay-safe.
- Release-proof cadence criteria: each closure slice must keep these criteria represented in `WORKLIST.md` (`TG-M12-PL`, `TG-M12-GM`, `TG-M12-OB`, `TG-M12-RP`) and preserved by `scripts/ai/verify.sh`.

## Post-closure hardening criteria (M13)

The local-first sync/installable hardening lane is considered executable when the following remain true and regression-backed:

- Offline queue recovery criteria: stale-lineage and malformed-envelope recovery paths reject unsafe mutations while preserving bounded replay continuity through `VerifyOfflineQueueRejectsMalformedSessionEnvelopeAsync`, `VerifyOfflineQueueRejectsStaleLineageAsync`, and `VerifySyncPrefixAcknowledgementAsync`.
- Installable cache-pressure clarity criteria: cache-pressure budget, caution copy, and shell follow-through bindings remain explicit for mobile/tablet installable posture through `VerifyCachePressureBudgetContractAsync`, `VerifyCachePressureDecisionNoticeUsesSupportNextActionCopy`, `VerifyCampaignWorkspaceLiteProjectionPromotesContinuitySummary`, and `VerifyIndexShellBindsContextualActionLabelsAsync`.
- Replay-safe resume fallback criteria: resume/reconnect fallback behavior preserves stored lineage and role-concrete continuity routes when runtime bundle posture drifts through `VerifyResumePreservesLedgerWhenRuntimeBundleDriftsAsync`, `VerifyResumeNormalizesCheckpointToLedgerLineageAsync`, `VerifyReconnectLineageTransitionContinuityAsync`, and `VerifyResumeAndWorkspaceLiteRoutesStayRoleConcreteAsync`.
- Release-proof cadence criteria: each hardening slice must keep these criteria represented in `WORKLIST.md` (`TG-M13-OQ`, `TG-M13-IP`, `TG-M13-RF`, `TG-M13-RP`) and preserved by `scripts/ai/verify.sh`.

## Post-closure role-depth criteria (M14)

The deeper player-vs-GM completion lane is considered executable when the following remain true and regression-backed:

- Player lane criteria: player guidance and action posture remain role-specific under capability leakage, denied cross-role requests preserve replay context, and workspace-lite follow-through remains player-concrete through `VerifyRoleBoundarySurvivesCapabilityLeakageAsync`, `VerifyQuickActionRejectsCrossRoleAuthorizationAsync`, `VerifyDeniedQuickActionsPreserveStoredReplayStateAsync`, and `VerifyCampaignWorkspaceLiteProjectionPromotesContinuitySummary`.
- GM lane criteria: GM guidance and operations posture remain separated from player quick-action posture, role routes stay concrete across resume/workspace/restore/onboarding seams, and GM operations summaries remain explicit through `VerifyBootstrapRoleShellEntryPointsAsync`, `VerifyRoleBoundarySurvivesCapabilityLeakageAsync`, `VerifyResumeAndWorkspaceLiteRoutesStayRoleConcreteAsync`, and `VerifyCampaignWorkspaceLiteProjectionPreservesObserverAndGmRoleDepth`.
- Observer handoff criteria: observer bootstrap/resume remain read-mostly, observer transition and follow-through guidance remain explicit, and observer lane action isolation stays intact through `VerifyObserverBootstrapAndResumeStayReadMostlyAsync`, `VerifyResumeAndWorkspaceLiteRoutesStayRoleConcreteAsync`, `VerifyCampaignWorkspaceLiteProjectionPreservesObserverAndGmRoleDepth`, and `VerifyIndexShellBindsContextualActionLabelsAsync`.
- Release-proof cadence criteria: each role-depth slice must keep these criteria represented in `WORKLIST.md` (`TG-M14-PL`, `TG-M14-GM`, `TG-M14-OB`, `TG-M14-RP`) and preserved by `scripts/ai/verify.sh`.

## Post-closure artifact/publication projection criteria (M15)

The remaining creator-publication and projection finish lane is considered executable when the following remain true and regression-backed:

- Artifact shelf projection criteria: workspace-lite recap and replay summaries keep publication state, trust ranking, discoverability, lineage, and direct creator-publication follow-through explicit through `VerifyCampaignWorkspaceLiteProjectionPromotesContinuitySummary`.
- Moderation boundary criteria: mobile surfaces expose publication-safe next actions and Hub-owned status follow-through without claiming moderation ownership, admin review authority, or a second publication truth; the repo boundary remains documented in `docs/chummer6-mobile.design.v1.md` and `docs/chummer6-mobile.design.v1.md` continues to reject publish/admin/moderation ownership.
- Release-proof cadence criteria: this synthesized completion slice must stay represented in `WORKLIST.md` (`TG-M15-AP`, `TG-M15-MB`, `TG-M15-RP`) and preserved by `scripts/ai/verify.sh`.
