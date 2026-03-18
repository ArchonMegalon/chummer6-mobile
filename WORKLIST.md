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

## Queue
| ID | Status | Priority | Task | Owner | Notes |
|---|---|---|---|---|---|
| WL-005 | done | P1 | Make the local-first runtime seam real and boringly trustworthy. | agent | Closed 2026-03-13: durable ledger/cache/queue seams, stale-lineage-safe sync/replay handling, and corruption/tamper tolerance are now enforced in code and regression checks. |
| WL-019 | done | P2 | Materialize observer and cross-device continuity ownership. | agent | Closed 2026-03-13: continuity claim and observe flows are now explicit route/client seams instead of implied wishes. |
| WL-020 | done | P2 | Turn release hardening and finished-shell closure into executable truth gates. | agent | Closed 2026-03-13: M10/M11 are now explicit, enforced release truth instead of hand-wavy “probably done.” |
| WL-023 | done | P2 | Retire stale `chummer-play` repo identity from the mobile front door. | agent | Completed 2026-03-14: live repo-facing docs now use the `chummer6-mobile` identity, the old design doc name is treated as compatibility-only, and a public rejoin/resume guarantee doc now explains the user-visible promise. |
| WL-024 | done | P1 | Prove `D1` at the transport seams: mobile APIs must consume session semantics from canonical core contracts and publish role-shell replay state through transport adapters only. | agent | Closed 2026-03-18: play/session transport DTOs and checkpoint envelopes now come from package-owned surfaces, `scripts/ai/verify.sh` rejects repo-local copies, and the package-plane runner restores against owner packages instead of empty contract stubs when local repos are available. |

## Current repo truth

- Repo-local live queue: empty
- Remaining work is product polish, not boundary confusion: keep the local-first resume/rejoin/replay guarantee trustworthy and keep workbench/publisher concerns out
- Historical feedback references still mention `chummer-play`; those are retained as audit history, not current repo identity

## Historical log

- Full queue history, stale overlay cleanup, and repeated audit/re-entry notes now live in `AUDIT_LOG.md`.
