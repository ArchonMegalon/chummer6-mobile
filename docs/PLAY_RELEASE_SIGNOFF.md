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

## Successor-wave explain and follow-up criteria (M145)

The successor mobile explain/follow-up lane is considered executable when the following remain true and regression-backed:

- Quick explain criteria: workspace-lite payloads keep packet-backed quick explain copy for visible scene, timeline, checkpoint, and bundle values through `VerifyCampaignWorkspaceLiteProjectionPromotesContinuitySummary` and `VerifyIndexShellBindsContextualActionLabelsAsync`.
- Source-anchor criteria: mobile shells keep source-anchor context explicit for scene packet, replay sequence, runtime fingerprint, owner route, and grounded bundle/checkpoint posture through `VerifyCampaignWorkspaceLiteProjectionPromotesContinuitySummary` and `VerifyIndexShellBindsContextualActionLabelsAsync`.
- Stale-state and bounded follow-up criteria: mobile shells keep stale-state posture explicit and keep grounded text-first follow-up bounded to the claimed live-play shell, update posture, and support posture through `VerifyCampaignWorkspaceLiteProjectionPromotesContinuitySummary` and `VerifyIndexShellBindsContextualActionLabelsAsync`.

## Mobile campaign continuity criteria (M112)

The successor mobile campaign continuity lane is considered executable when the following remain true and regression-backed:

- Mobile shell criteria: `PlayCampaignWorkspaceLiteProjection` keeps `MobileCampaignCurrentState`, `MobileCampaignStateSummary`, `MobileCampaignCachedState`, `MobileCampaignStaleState`, and `MobileCampaignActionRequired` explicit so stale, cached, and action-required campaign truth is visible without widening mobile into a second campaign authority, and the action-required field repeats the claimed mobile lane plus session context instead of relying on generic workspace copy.
- Cross-device continuity criteria: the installable shell keeps a stable claimed-device id, surfaces continuity-token plus owner-route posture from `/api/play/observe/{sessionId}`, and lets the user refresh the claim on the current device through `/api/play/continuity/claim` without detouring into a browser-only recovery ritual.
- Travel restore criteria: `RoamingWorkspaceRestorePlan` keeps `TravelCampaignCurrentState`, `TravelCampaignStateSummary`, `TravelCampaignCachedState`, `TravelCampaignStaleState`, and `TravelCampaignActionRequired` explicit so travel continuity stays claimed-device, bounded, and stale-aware before reopen or resume, and the action-required field repeats the target travel lane and installation id.
- Recovery proof criteria: `PlayEntryRecoveryProjection` keeps post-failure recovery copy explicit about cached, stale, and action-required travel continuity before one-tap resume so the calmer recovery lane does not collapse this state back into generic failure prose.
- Shell proof criteria: the installable shell renders dedicated mobile and travel continuity cards with tone-aware state breakdown, explicit `MobileCampaignCurrentState` and `TravelCampaignCurrentState` bindings, separate workspace/restore `ActionRequiredSummary` plus `ActionRequiredLabels` rails, and recovery-list continuity detail, with repo-local proof coverage through `docs/next90-m112-mobile-campaign-continuity.proof.md`, `scripts/verify_next90_m112_mobile_campaign_continuity.py`, `VerifyCampaignWorkspaceLiteProjectionPromotesContinuitySummary`, `VerifyRoamingWorkspaceRestorePlanRestoresPackageOwnedCampaignState`, `VerifyEntryRecoveryProjectionCoversNoSessionNoCampaignAndPostFailure`, and `VerifyIndexShellBindsContextualActionLabelsAsync`.
- Package-proof criteria: `next90-m112-mobile-campaign-continuity` is the repo-local closed-package proof anchor for `campaign_memory:travel` and `campaign_state:mobile`; the canonical queue and registry rows stay pinned to `verify_closed_package_only`, the local generated receipt remains package-scoped evidence, and `scripts/materialize_mobile_local_release_proof.py` preserves the existing generated proof timestamp when the proof payload is semantically unchanged.

## Mobile artifact shelf criteria (M117)

The successor mobile artifact shelf lane is considered executable when the following remain true and regression-backed:

- Mobile shelf projection criteria: `PlayCampaignWorkspaceLiteProjection` keeps `SelectedArtifactView`, `ArtifactShelfSelectionSummary`, `SelectedRecapArtifactSummary`, `SelectedRecapArtifactHref`, and `ArtifactShelfViews` explicit so campaign, travel, recap, and published artifact lanes stay visible without widening mobile into a second shelf authority.
- Selected recap artifact criteria: the active shelf lane remains explicit inside `SelectedRecapArtifactSummary` so travel/campaign recap follow-through stays inspectable in-shell instead of surviving only as route state.
- Role-concrete shelf routing criteria: artifact shelf browse targets stay session-aware and role-aware through `/artifacts/{sessionId}` redirects, `artifactView` and `artifactId` shell round-trips, and `VerifyCampaignWorkspaceLiteProjectionPromotesContinuitySummary`.
- Shell proof criteria: the installable shell renders a dedicated selected artifact shelf summary, a selected recap artifact summary, a direct recap-artifact deep link, and a separate shelf-browse follow-through while preserving `workspace-recap-views` browse targets through `docs/next90-m117-mobile-artifact-shelf.proof.md`, `scripts/verify_next90_m117_mobile_artifact_shelf.py`, `VerifyCampaignWorkspaceLiteProjectionPromotesContinuitySummary`, and `VerifyIndexShellBindsContextualActionLabelsAsync`.
- Package-proof criteria: `next90-m117-mobile-artifact-shelf` is the repo-local closed-package receipt for `artifact_shelf:mobile` and `artifact_recap_view:mobile`; the receipt stays pinned to successor frontier `3440617449` while also naming historical flagship frontier `3371889980`, and `scripts/verify_next90_m117_mobile_artifact_shelf.py` now fail-closes registry, design-queue, fleet-queue, and generated-proof drift against the canonical `verify_closed_package_only` posture.

## Mobile onboarding continuity criteria (M119)

The successor mobile starter-lane continuity is considered executable when the following remain true and regression-backed:

- Starter artifact projection criteria: `PlayCampaignWorkspaceLiteProjection` keeps `LaunchPrimerSummary`, `LaunchPrimerProvenanceSummary`, `LaunchPrimerHref`, `FirstSessionBriefingSummary`, `FirstSessionBriefingProvenanceSummary`, `FirstSessionBriefingHref`, `StarterArtifactContinuitySummary`, and `StarterArtifactContinuityLabels` explicit so primer and first-session briefing artifacts stay inspectable on the claimed mobile shell instead of collapsing into generic recap copy.
- Travel restore criteria: `RoamingWorkspaceRestorePlan` keeps `StarterPrimerFollowThrough`, `StarterPrimerFollowThroughHref`, `FirstSessionBriefingFollowThrough`, and `FirstSessionBriefingFollowThroughHref` explicit so travel-shell reopen routes preserve claimed-device, session, role, and travel-shelf context without a browser-only handoff, and fail closed if the direct artifact hrefs lose the trusted `deviceId` or `view=travel` lane.
- Entry recovery criteria: `PlayEntryRecoveryProjection` must route `no_session` and `no_campaign` onboarding back through the starter-primer lane, and its recovery labels must keep starter-primer plus first-session-briefing continuity visible when the user needs a calmer first playable session path.
- Shell proof criteria: the installable shell renders dedicated primer and first-session briefing summaries, provenance, direct artifact links, starter continuity labels, and restore follow-through links through `docs/next90-m119-mobile-onboarding-continuity.proof.md`, `scripts/verify_next90_m119_mobile_onboarding_continuity.py`, `VerifyCampaignWorkspaceLiteProjectionPromotesContinuitySummary`, `VerifyRoamingWorkspaceRestorePlanRestoresPackageOwnedCampaignState`, `VerifyEntryRecoveryProjectionCoversNoSessionNoCampaignAndPostFailure`, and `VerifyIndexShellBindsContextualActionLabelsAsync`, including one-tap starter recovery and exact route preservation for role-aware travel reopen paths.
- Package-proof criteria: `next90-m119-mobile-onboarding-continuity` remains the repo-local implementation receipt for `starter_onboarding:mobile` and `first_session_briefing:mobile`; the canonical successor queue row is complete while this repo retains the mobile implementation proof.

## Mobile live combat confidence criteria (M121)

The successor mobile live-combat confidence lane is considered executable when the following remain true and regression-backed:

- Projection criteria: `PlayCampaignWorkspaceLiteProjection` exposes player table cards, between-turn affordances, and GM-lite continuity views using session projection, quick actions, runboard posture, replay-safe continuity, and support-linked follow-through instead of local combat math or a second GM truth source.
- Role-depth criteria: the same projection keeps player, observer, and GM lanes distinct so observer posture stays read-mostly, player posture stays table-safe, and GM posture stays runboard-oriented without widening into a VTT surface.
- Shell proof criteria: the installable shell renders dedicated player table-card, between-turn affordance, and GM-lite continuity cards through `docs/next90-m121-mobile-live-combat-confidence.proof.md`, `scripts/verify_next90_m121_mobile_live_combat_confidence.py`, `VerifyCampaignWorkspaceLiteProjectionPromotesContinuitySummary`, `VerifyCampaignWorkspaceLiteProjectionPreservesObserverAndGmRoleDepth`, and `VerifyIndexShellBindsContextualActionLabelsAsync`.
- Package-proof criteria: `next90-m121-mobile-add-player-table-cards-between-turn-affordances-and-gm-l` remains the repo-local implementation receipt for `add_player_table_cards_between:mobile`; the canonical successor queue row stays open while this repo records only the mobile implementation truth.

## Mobile runner-goal updates and consequence-feed criteria (M122)

The successor mobile campaign-return lane is considered executable when the following remain true and regression-backed:

- Projection criteria: `PlayCampaignWorkspaceLiteProjection` keeps `RunnerGoalUpdatesSummary`, `RunnerGoalUpdateLabels`, `PlayerSafeConsequenceFeedSummary`, and `PlayerSafeConsequenceFeedLabels` explicit so runner-goal return updates and player-safe consequence cues remain first-class mobile return surfaces instead of dissolving into generic continuity prose.
- Boundary criteria: the runner-goal lane stays checkpoint-backed, replay-safe, install-local, and support-linked, while the consequence-feed lane remains spoiler-bounded and explicitly keeps BLACK LEDGER world truth outside mobile.
- Role-depth criteria: the same campaign-return projection stays distinct across player, observer, and GM roles so the copy names the active lane without widening mobile into campaign authority or operator-only consequence detail.
- Shell proof criteria: the installable shell renders dedicated runner-goal update and player-safe consequence feed cards through `docs/next90-m122-mobile-runner-goal-updates-and-consequence-feed.proof.md`, `scripts/verify_next90_m122_mobile_runner_goal_updates.py`, `VerifyCampaignWorkspaceLiteProjectionPromotesContinuitySummary`, `VerifyCampaignWorkspaceLiteProjectionPreservesObserverAndGmRoleDepth`, and `VerifyIndexShellBindsContextualActionLabelsAsync`.
- Package-proof criteria: `next90-m122-mobile-add-mobile-runner-goal-updates-and-player-safe-consequen` remains the repo-local implementation receipt for `add_mobile_runner_goal_updates:mobile`; the canonical successor queue row stays open while this repo records only the mobile implementation truth.
