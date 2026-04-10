# Worklist Queue

Purpose: keep the live mobile queue readable. Historical queue churn and audit replay notes now live in `AUDIT_LOG.md`.

## Status Keys
- `queued`
- `in_progress`
- `blocked`
- `done`

## Fleet execution sequence (cross-shard)

- P0 foundation: complete `core/WL-200` first, then run `DR-120` and launch trust/recovery surface parity slices in parallel (`ui/WL-240`, `hub/WL-240`, `mobile/WL-027`, `hub-registry/WL-260`, `media/MF-014`).
- P1 hardening: after `WL-200` is stable and `design/DR-124` is in review, execute `core/WL-201`, then the follow-on clarity/visibility slices (`ui/WL-241`, `hub/WL-241`, `hub-registry/WL-261`, `hub-registry/WL-262`, `mobile/WL-028`, `media/MF-015`).
- P2 quality finish: gate on `design/DR-121` through `design/DR-127` before closure on `design/DR-123`; complete ui-kit flagship reliability slice in the same cycle after cross-shard copy/state/known-issue coherence settles.

## Milestone Registry (mobile)
| Milestone | Status | Completion | ETA (UTC) | Confidence | Backlog truth |
|---|---|---|---|---|---|
| M4 Package-only cutover | done | 100% | 2026-03-10 | high | Package-only consumption and published-feed compatibility checks are in place. |
| M6 Local-first runtime seam | done | 100% | 2026-03-13 | high | Ledger, cache, offline queue, and replay ownership are real and regression-guarded. |
| M10 Hardening | done | 100% | 2026-03-13 | high | Accessibility, replay resilience, and performance-budget truth gates are closed. |
| M11 Finished mobile shell gate | done | 100% | 2026-03-13 | high | Role-shell completion, cross-device continuity, and release closure gates are closed. |
| M12 Play-shell completion depth (post-closure) | done | 100% | 2026-04-10 | high | Browser transport, event-log persistence, offline resume, observer read-mostly posture, release-proof cadence, and critical-action clarity all remain verify-enforced, and restore/onboarding routes now reject untrusted `deviceId` input with explicit 400 guidance while live route checks prove role-concrete restore/onboarding behavior plus spoofed-device rejection for player, GM, and observer lanes. |

## Queue
| ID | Status | Priority | Task | Owner | Notes |
|---|---|---|---|---|---|
| WL-005 | done | P1 | Make the local-first runtime seam real and boringly trustworthy. | agent | Closed 2026-03-13: durable ledger/cache/queue seams, stale-lineage-safe sync/replay handling, and corruption/tamper tolerance are now enforced in code and regression checks. |
| WL-019 | done | P2 | Materialize observer and cross-device continuity ownership. | agent | Closed 2026-03-13: continuity claim and observe flows are now explicit route/client seams instead of implied wishes. |
| WL-020 | done | P2 | Turn release hardening and finished-shell closure into executable truth gates. | agent | Closed 2026-03-13: M10/M11 are now explicit, enforced release truth instead of hand-wavy “probably done.” |
| WL-023 | done | P2 | Retire stale `chummer-play` repo identity from the mobile front door. | agent | Completed 2026-03-14: live repo-facing docs now use the `chummer6-mobile` identity, the old design doc name is treated as compatibility-only, and a public rejoin/resume guarantee doc now explains the user-visible promise. |
| WL-024 | done | P1 | Prove `D1` at the transport seams: mobile APIs must consume session semantics from canonical core contracts and publish role-shell replay state through transport adapters only. | agent | Closed 2026-03-18: play/session transport DTOs and checkpoint envelopes now come from package-owned surfaces, `scripts/ai/verify.sh` rejects repo-local copies, and the package-plane runner restores against owner packages instead of empty contract stubs when local repos are available. |
| WL-025 | done | P1 | Close `E1` by proving the player and GM shells are release-complete across replay, reconnect, observe, offline, and installable-PWA flows with current evidence. | agent | Closed 2026-03-19: `scripts/ai/verify.sh` now keeps replay, reconnect, observe, offline queue/cache, role-shell bootstrap, and installable-PWA behavior executable and package-boundary-safe in one standard verification path. |
| WL-026 | done | P2 | Publish post-closure runnable backlog for full play-shell completion depth: extend verifier-backed evidence for browser transport/event-log/offline resume into role-specific completion criteria and ongoing release proof. | agent | Closed 2026-03-23: added explicit M12 truth gates for player shell completion, GM shell completion, and ongoing release proof cadence; wired those gates into `scripts/ai/verify.sh`; and published dated queue-publication evidence in `AUDIT_LOG.md` tied to `feedback/2026-03-21-204029-audit-task-2652.md` and `feedback/2026-03-21-204029-audit-task-48734.md`. |
| WL-027 | done | P0 | Improve mobile UX continuity by adding explicit reconnect-and-recover state copy for disconnect, role change, and observer transitions. | agent | Closed 2026-04-10: workspace-lite projection now emits explicit disconnect/role-change/observer-transition recovery copy (`DisconnectRecoveryCopy`, `RoleChangeRecoveryCopy`, `ObserverTransitionRecoveryCopy`), the shell binds those fields into dedicated recovery regions (`recover-disconnect`, `recover-role-change`, `recover-observer-transition`), and regression checks enforce both projection semantics and DOM bindings via `VerifyWorkspaceLiteProjectionSummarizesRecoveryAndGuidance` plus `VerifyIndexShellAccessibilityContractAsync` and `VerifyIndexShellBindsContextualActionLabelsAsync`. |
| WL-028 | done | P1 | Improve mobile discoverability and efficiency for critical actions (rejoin, continue, support) with clearer command surfaces and low-noise guidance. | agent | Closed 2026-04-09: workspace-lite projection now emits explicit rejoin/continue/support command fields plus low-noise route guidance, the shell renders those commands in a dedicated critical-actions surface, and regression checks enforce both projection semantics and DOM bindings so critical completion paths are measurable and support-copy-aligned. |
| WL-029 | done | P2 | Add empty-state onboarding and one-tap recovery pathways for no-session, no-campaign, and post-failure mobile flows. | agent | Closed 2026-04-10: added `/api/play/onboarding-recovery/{sessionId}` entry projection, rendered one recommended one-tap action for no-session/no-campaign/post-failure states, wired retry/cancel/restore action copy through dedicated shell bindings, and enforced coverage through `VerifyEntryRecoveryProjectionCoversNoSessionNoCampaignAndPostFailure`, `VerifyIndexShellAccessibilityContractAsync`, and `VerifyIndexShellBindsContextualActionLabelsAsync`. |
| WL-030 | done | P2 | Add decision receipts for long-running shell actions (rejoin, quick actions, resume) so users can understand what was retried, skipped, or deferred. | agent | Closed 2026-04-10: workspace-lite projection now publishes explicit decision-receipt summary plus rejoin/quick-action/resume receipt lines with retried/skipped/deferred wording, the shell renders those receipts in a dedicated critical-actions region, and regression checks enforce both projection semantics and DOM bindings including one canonical support escalation route. |

## M12 Truth-Gate Registry

| Gate ID | Milestone | ETA Target (UTC) | Status | Completion Truth |
|---|---|---|---|---|
| TG-M12-PL | M12 | 2026-04-10 | done | Player-shell completion depth is post-closure executable: browser transport ownership, event-log persistence lineage, offline resume payload behavior, and role-safe quick-action scope stay regression-backed through `VerifySyncPrefixAcknowledgementAsync`, `VerifyStoredLineageAlignment`, `VerifyStoredLineageStaleResponsesAsync`, `VerifyBootstrapRoleShellEntryPointsAsync`, `VerifyRoleBoundarySurvivesCapabilityLeakageAsync`, `VerifyQuickActionRejectsCrossRoleAuthorizationAsync`, and `VerifyDeniedQuickActionsPreserveStoredReplayStateAsync`. |
| TG-M12-GM | M12 | 2026-04-10 | done | GM-shell completion depth is post-closure executable: GM-only capability gating (`play.gm.actions`, `play.spider.cards`), continuity/observe route ownership, non-mutating stale-lineage rejection, and role-boundary resilience under over-provisioned capability lists remain regression-backed through `VerifyBootstrapRoleShellEntryPointsAsync`, `VerifyRoleBoundarySurvivesCapabilityLeakageAsync`, `VerifyQuickActionRejectsCrossRoleAuthorizationAsync`, `VerifyDeniedQuickActionsPreserveStoredReplayStateAsync`, `VerifyContinuityClaimRejectsStaleLineageWithoutMutationAsync`, and `VerifyObserveReturnsLineageSafeContinuityAsync`. |
| TG-M12-OB | M12 | 2026-04-10 | done | Observer-shell completion depth is explicit: bootstrap and resume payloads keep the lane read-mostly, expose observer-owned shell metadata, reject inherited player/GM action posture, and preserve stored replay context on denied quick-action attempts through `VerifyObserverBootstrapAndResumeStayReadMostlyAsync`, `VerifyRoleBoundarySurvivesCapabilityLeakageAsync`, and `VerifyDeniedQuickActionsPreserveStoredReplayStateAsync`. |
| TG-M12-RP | M12 | 2026-04-10 | done | Ongoing release proof is explicit and enforceable: `docs/PLAY_RELEASE_SIGNOFF.md` defines post-closure player/GM completion criteria and release-proof cadence, and `scripts/ai/verify.sh` enforces those gate references plus `WL-026` closure traceability. |

## Current repo truth

- Repo-local live queue: none (all currently materialized worklist rows are done, including `M12` completion-depth closure and truth-gate evidence)
- The player and GM shells are materially release-complete on the current replay/reconnect/observe/offline/installable-PWA axis, the observer lane now has explicit read-mostly bootstrap/resume proof, workspace-lite continuity now has explicit observer/GM regression coverage, follow-through anchors now render projection-backed action text instead of generic link labels, cache-pressure decision notices now reuse the live support next-step instead of a generic support CTA, empty-state onboarding/recovery now exposes one-tap no-session/no-campaign/post-failure actions with retry/cancel/restore lanes, and restore-plan/onboarding routes now enforce trusted claimed-device targeting with live spoofed-device rejection coverage.
- Historical feedback references still mention `chummer-play`; those are retained as audit history, not current repo identity

## Historical log

- Full queue history, stale overlay cleanup, and repeated audit/re-entry notes now live in `AUDIT_LOG.md`.
