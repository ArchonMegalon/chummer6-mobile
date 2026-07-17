# Play Release Signoff

Purpose: keep the mobile/play share of `F0` explicit now that `E1` is already materially closed.

## Current hardening contract

`chummer6-mobile` stays release-complete for the current player/GM shell when `scripts/ai/verify.sh` keeps the following evidence executable:

- accessibility proof via `VerifyIndexShellAccessibilityContractAsync` and `VerifyBootstrapRoleShellEntryPointsAsync`
- performance-budget proof via `VerifyCachePressureBudgetContractAsync` and the fail-closed source-owned install-payload gate in `scripts/verify_mobile_pwa_performance_budget.py`
- replay/rejoin safety via the regression checks already tied into `M10` and `M11`
- real-host/browser PWA proof via `scripts/verify_mobile_pwa_runtime_smoke.py`, `scripts/verify_mobile_pwa_viewport_smoke.py`, plus `VerifyTurnCompanionRealHostPipelineUsesAntiforgeryAsync`
- package-only shared boundary consumption for `Chummer.Engine.Contracts`, `Chummer.Play.Contracts`, and `Chummer.Ui.Kit`

## Integration handoff contract

The Player/GM Blazor PWA handoff is integration-ready only when the mobile release proof is backed by source-visible artifacts, not loose local files:

- New source/proof artifacts that must be carried with the change: `scripts/materialize_mobile_cross_surface_readiness.py`, `scripts/verify_mobile_pwa_analytics_smoke.py`, `src/Chummer.Play.Web/wwwroot/manifest.player.webmanifest`, `src/Chummer.Play.Web/wwwroot/manifest.gm.webmanifest`, `src/Chummer.Play.Web/Dockerfile`, and the generated receipts under `.codex-studio/published/MOBILE_*_SMOKE.generated.json` plus `.codex-studio/published/MOBILE_CROSS_SURFACE_READINESS.generated.json`.
- The review boundary itself must stay materialized via `scripts/materialize_mobile_release_boundary.py` into `.codex-studio/published/MOBILE_RELEASE_BOUNDARY.generated.json`, listing the owned mobile/PWA source files, owned proof receipts, disposable local smoke artifacts, and any current external preflight/postdeploy blockers without sweeping unrelated repo work into the release handoff.
- Disposable browser screenshots produced under `_tmp/` are local smoke artifacts only and must not be treated as release evidence.
- Repo-tracked local receipts must scrub ephemeral localhost ports to `http://127.0.0.1:<port>` and minted receiver device ids to `<minted-device>` so reruns refresh proof without machine-local noise.
- Cross-surface readiness must fail closed on the public-edge postdeploy gate. It must not accept `pass_with_nonmobile_release_marker_drift`, release-marker drift exceptions, or any other degraded public-edge status.
- Public deployment evidence remains separate from local proof: the local proof can certify the mobile behavior, but final public-edge promotion still needs a clean public-edge preflight/postdeploy receipt when deployment locks and release-version truth are available.
- When the strict public-edge gate is blocked by live deploy state outside `chummer6-mobile`, `.codex-studio/published/MOBILE_CROSS_SURFACE_READINESS.generated.json` should still materialize with `status: "fail"` and explicit `public_edge.failures[]` details so the blocker is recorded instead of masquerading as a local shell regression.
- Strict public-edge promotion must not waive foreign active build-lane blockers. If `check_public_edge_deploy_preflight.py` reports active foreign `build-chummer6-linux` lanes, the mobile handoff remains externally blocked until those lanes exit and the strict preflight is rerun cleanly.
- Full-repo verification still depends on `.codex-design` mirror freshness. If `scripts/ai/verify.sh` stops at `verify_design_mirror.py` for stale `WEEKLY_PRODUCT_PULSE.generated.json` or `GOLDEN_JOURNEY_RELEASE_GATES.yaml`, that is a design-sync blocker outside the owned mobile/PWA change boundary, not evidence of a mobile shell regression.

## Release budgets

- Accessibility: the installable shell must keep `<html lang="en">` and the polite live-status region in `src/Chummer.Play.Web/wwwroot/index.html`.
- Localization: the shell must keep explicit document-language and role-entry semantics instead of relying on implicit browser defaults.
- Performance: runtime-bundle cache pressure must preserve the current bounded quota budget (`RuntimeBundleQuota == 8`) and continue to report backpressure when the budget is saturated. The service-worker's source-owned install assets must also remain within the fixed 180 KiB raw / 72 KiB deterministic-gzip aggregate budget; any newly precached asset must be explicitly budgeted. The SDK-owned `/_framework/blazor.web.js` loader is disclosed as an unmeasured exception in the generated receipt rather than being counted as source-owned proof.

## Real-Host PWA Runtime Criteria

- Real-host pipeline criteria: the Kestrel-hosted `/mobile` shell must render without a 500 and keep antiforgery middleware enabled through `app.UseAntiforgery();` and `VerifyTurnCompanionRealHostPipelineUsesAntiforgeryAsync`.
- Runtime smoke criteria: `scripts/verify_mobile_pwa_runtime_smoke.py` must keep service-worker control, claimed-device local tracker edits, RUNSITE anchor selection, manual resolve/history, replay-plus-ack queue behavior, generic `/mobile` resume, role-aware Player/GM shortcut resume, offline reopen, offline Player and GM local replay/ack, and device-neutral session handoff receivers executable against a real host instead of only through in-memory regression routing.
- Hero launch criteria: selecting `Play` from the hero action dropdown must immediately navigate to the role-specific mobile PWA target, with Player landing on `/mobile/player` and GM landing on `/mobile/gm`; this remains covered by the `hero_menu_player_launch:` and `hero_menu_gm_launch:` receipts in `scripts/verify_mobile_pwa_runtime_smoke.py`.
- Role-specific manifest criteria: the generic manifest must launch Player mode by default while `manifest.player.webmanifest` and `manifest.gm.webmanifest` keep distinct app ids, start URLs, shortcuts, adaptive icons, and `/mobile/` scope; `scripts/verify_mobile_pwa_viewport_smoke.py` and `VerifyTurnCompanionManifestTargetsDirectMobilePwaAsync` must keep browser installability and source-level manifest selection green for both modes.
- Installability criteria: the same Chromium-hosted shell must keep app-manifest parse errors and `Page.getInstallabilityErrors` empty through `scripts/verify_mobile_pwa_viewport_smoke.py`, so installability is proven by the browser’s PWA checks rather than only by metadata presence.
- Quick-glance criteria: `scripts/verify_mobile_pwa_viewport_smoke.py` must keep the 390px-wide player and GM shells overflow-free, show the quick-jump rail plus six-card glance strip, keep the live tracker card above lower-context trust/RUNSITE detail, and collapse the action and odds rails to a single handheld column.
- Rybbit analytics criteria: `scripts/verify_mobile_pwa_analytics_smoke.py` must keep Rybbit default-disabled unless explicitly configured, reject DNT/GPC collection, avoid session/device/secret leakage, carry bounded role/display/install posture, and prove copy/native/link session handoff events for both Player and GM lanes.

## Mobile turn companion criteria

- Bounded turn-state criteria: the `/mobile` companion keeps health, stun, edge, ammo, reserve, charges, mission-critical inventory, a bounded action rail, source-backed modifiers, quick odds, and digital/manual roll resolution explicit through `VerifyTurnCompanionProjectionStaysBoundedAndComputesOddsAsync`, `VerifyTurnCompanionPlayerProjectionCoversRequestedLiveTrackersAsync`, `VerifyTurnCompanionDigitalResolveProducesBoundedReceiptAsync`, `VerifyTurnCompanionManualResolveUpdatesHistoryAndAmmoAsync`, and `VerifyTurnCompanionRouteRendersBlazorShellAsync`.
- GM lane criteria: the same `/mobile` companion keeps a GM-specific actor posture, bounded initiative/threat actions, GM-only replay-safe quick actions, and role-concrete owner-route posture explicit through `VerifyTurnCompanionGmProjectionStaysBoundedAndRoleSpecificAsync` plus the real-browser GM interaction lane in `scripts/verify_mobile_pwa_runtime_smoke.py`.
- Replay-safe continuity criteria: the same shell keeps history, claimed-device continuity, replay/ack posture, install metadata, and device-scoped local state explicit through `VerifyTurnCompanionClaimedDeviceStateIsolationAsync`, `VerifyTurnCompanionReplayQueueRoundTripsAsync`, `VerifyTurnCompanionClientRuntimeKeepsClaimedDeviceContinuityContractAsync`, `VerifyTurnCompanionManifestTargetsDirectMobilePwaAsync`, and `VerifyTurnCompanionAppShellDeclaresMobileInstallMetadataAsync`.
- RUNSITE anchor criteria: optional RUNSITE room/zone/hotspot anchors stay orientation-only, device-scoped, and observer-safe instead of becoming tactical token authority through `VerifyTurnCompanionProjectionStaysBoundedAndComputesOddsAsync`, `VerifyTurnCompanionObserverStaysReadOnlyAsync`, and `VerifyTurnCompanionRunsiteAnchorSelectionStaysDeviceScopedAsync`.

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
- Package-proof criteria: `next90-m112-mobile-campaign-continuity` is the repo-local closed-package proof anchor for `campaign_memory:travel` and `campaign_state:mobile`; the canonical queue and registry rows stay pinned to `verify_closed_package_only`, the local generated receipt remains package-scoped evidence, and `scripts/materialize_mobile_local_release_proof.py` must renew the generated proof timestamp on every rerun so fleet freshness gates can trust the receipt.

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
