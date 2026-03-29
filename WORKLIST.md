# Worklist Queue

Purpose: keep the live mobile queue readable. Historical queue churn and audit replay notes now live in `AUDIT_LOG.md`.

## Status Keys
- `queued`
- `in_progress`
- `blocked`
- `done`

## Milestone Registry (mobile)
| Milestone | Status | Completion | ETA (UTC) | Confidence | Backlog truth |
|---|---|---|---|---|---|
| M4 Package-only cutover | done | 100% | 2026-03-10 | high | Package-only consumption and published-feed compatibility checks are in place. |
| M6 Local-first runtime seam | done | 100% | 2026-03-13 | high | Ledger, cache, offline queue, and replay ownership are real and regression-guarded. |
| M10 Hardening | done | 100% | 2026-03-13 | high | Accessibility, replay resilience, and performance-budget truth gates are closed. |
| M11 Finished mobile shell gate | done | 100% | 2026-03-13 | high | Role-shell completion, cross-device continuity, and release closure gates are closed. |
| M12 Play-shell completion depth (post-closure) | in_progress | 85% | 2026-04-10 | medium | Browser transport, event-log persistence, offline resume, and observer-lane read-mostly shell posture remain closed, and post-closure role-shell completion gates are now explicit and verify-enforced; the latest additive depth slices now prove player/GM capability leakage cannot bleed into observer bootstrap or resume payloads, authorization denials preserve stored replay context instead of blanking the shell, and workspace-lite continuity stays role-correct for both observer and GM follow-through, with remaining scope limited to sustained release proof refresh and any future role-depth regressions opened by new shell surfaces. |

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

## M12 Truth-Gate Registry

| Gate ID | Milestone | ETA Target (UTC) | Status | Completion Truth |
|---|---|---|---|---|
| TG-M12-PL | M12 | 2026-04-10 | done | Player-shell completion depth is post-closure executable: browser transport ownership, event-log persistence lineage, offline resume payload behavior, and role-safe quick-action scope stay regression-backed through `VerifySyncPrefixAcknowledgementAsync`, `VerifyStoredLineageAlignment`, `VerifyStoredLineageStaleResponsesAsync`, `VerifyBootstrapRoleShellEntryPointsAsync`, `VerifyRoleBoundarySurvivesCapabilityLeakageAsync`, `VerifyQuickActionRejectsCrossRoleAuthorizationAsync`, and `VerifyDeniedQuickActionsPreserveStoredReplayStateAsync`. |
| TG-M12-GM | M12 | 2026-04-10 | done | GM-shell completion depth is post-closure executable: GM-only capability gating (`play.gm.actions`, `play.spider.cards`), continuity/observe route ownership, non-mutating stale-lineage rejection, and role-boundary resilience under over-provisioned capability lists remain regression-backed through `VerifyBootstrapRoleShellEntryPointsAsync`, `VerifyRoleBoundarySurvivesCapabilityLeakageAsync`, `VerifyQuickActionRejectsCrossRoleAuthorizationAsync`, `VerifyDeniedQuickActionsPreserveStoredReplayStateAsync`, `VerifyContinuityClaimRejectsStaleLineageWithoutMutationAsync`, and `VerifyObserveReturnsLineageSafeContinuityAsync`. |
| TG-M12-OB | M12 | 2026-04-10 | done | Observer-shell completion depth is explicit: bootstrap and resume payloads keep the lane read-mostly, expose observer-owned shell metadata, reject inherited player/GM action posture, and preserve stored replay context on denied quick-action attempts through `VerifyObserverBootstrapAndResumeStayReadMostlyAsync`, `VerifyRoleBoundarySurvivesCapabilityLeakageAsync`, and `VerifyDeniedQuickActionsPreserveStoredReplayStateAsync`. |
| TG-M12-RP | M12 | 2026-04-10 | done | Ongoing release proof is explicit and enforceable: `docs/PLAY_RELEASE_SIGNOFF.md` defines post-closure player/GM completion criteria and release-proof cadence, and `scripts/ai/verify.sh` enforces those gate references plus `WL-026` closure traceability. |

## Current repo truth

- Repo-local live queue: none (all currently materialized worklist rows are done; `M12` remains in progress for additive depth evidence beyond this closure slice)
- The player and GM shells are materially release-complete on the current replay/reconnect/observe/offline/installable-PWA axis, the observer lane now has explicit read-mostly bootstrap/resume proof, and workspace-lite continuity now has explicit observer/GM regression coverage; remaining change pressure is explicitly tracked as post-closure release-proof refresh and any future additive role-depth slices under `M12`.
- Historical feedback references still mention `chummer-play`; those are retained as audit history, not current repo identity

## Historical log

- Full queue history, stale overlay cleanup, and repeated audit/re-entry notes now live in `AUDIT_LOG.md`.
