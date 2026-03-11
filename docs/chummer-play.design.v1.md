# Chummer Play Design v1

Date: 2026-03-09

## Mission

`chummer-play` is the mobile and play-mode frontend for Chummer:
- player and GM play shells
- local-first session ledger handling
- runtime bundle consumption
- play-scoped coach, Spider, and delivery surfaces
- aggressive offline and media caching

It is the session OS client, not the full builder or workbench.

## Ownership

This repo owns:
- play-mode routes, shells, and components
- local event-log storage
- offline cache and sync/replay seams
- runtime bundle consumption
- play-safe role gating for player, GM, and observer modes

This repo must not own:
- rules math
- XML parsing
- runtime fingerprint generation
- profile or rulepack compilation
- direct provider calls
- publication workflows
- moderation backoffice
- hidden canon writes

## Dependencies

`chummer-play` may depend only on:
- `Chummer.Engine.Contracts`
- `Chummer.Play.Contracts`
- `Chummer.Ui.Kit`
- general framework libraries

`chummer-play` must not depend on:
- copied source from `chummer-presentation`
- raw project references into `chummer-core-engine`
- raw project references into `chummer.run-services`
- `executive-assistant` code

## Initial Milestone Spine

1. Contract canon and package-only dependencies.
2. Extract and stabilize the session-web/mobile host seam.
3. Replace scaffold bootstrap, browser session client, and browser event-log seams with executable play runtime code.
4. Materialize the dedicated `/api/play/*` surface for projection, reconnect, and sync ownership.
5. Define player and GM role shells.
6. Harden local-first sync, replay, stale protection, and offline cache ownership.
7. Integrate play-safe Spider and coach surfaces.
8. Ship installable PWA hardening for mobile/tablet play.

## Current play seam

The repo now owns an executable play host seam:
- `Chummer.Play.Web`
- browser session API and coach client implementations
- browser-backed event-ledger and offline cache services
- explicit player and GM shell descriptors with role-aware gating
- dedicated `/api/play/*` bootstrap, projection, reconnect, sync, quick-action, and resume ownership
- installable PWA hardening for service-worker cache policy, quota pressure, and deep-link resume

The remaining step is not scaffold replacement. It is package-plane cutover and continued local-first hardening while preserving package-only shared dependencies.

## Remaining materialization targets

The implemented play seam now shifts the active queue to:
- published-feed cutover for `Chummer.Play.Contracts` and `Chummer.Ui.Kit`
- continued local-first replay, lineage, and stale-protection hardening
- resume/reconnect continuity and runtime-bundle lineage coherence

Those seams map to the active milestone spine as follows:
- package-plane readiness: keep `Chummer.Play.Contracts` and `Chummer.Ui.Kit` consumption package-only, but treat published-feed cutover as explicit queue-owned work while those packages are still maturing
- local-first runtime seam: continue hardening browser-backed ledger, queue, replay, and lineage behavior behind dedicated abstractions
- resume/reconnect continuity: keep stale recovery, runtime cache ownership, and replay-safe lineage coherent across reconnect and resume

## Executable milestone mapping

To keep the uncovered scope materialized as queue-owned work instead of aspirational design text, the next milestones break down as follows:

- Milestone 4 dedicated play API ownership: `WL-004` aligns the contract family and `WL-012` owns the executable `/api/play/projection`, `/api/play/reconnect`, and `/api/play/sync` route surface in `Chummer.Play.Web`.
- Milestone 4 package-plane readiness: `WL-017` owns cutover from local stubs to published `Chummer.Play.Contracts` and `Chummer.Ui.Kit` feeds with restore-time compatibility checks and fallback semantics while package publication is incomplete.
- Milestone 6 offline cache and local-first replay ownership: `WL-005` remains the sync/storage umbrella, `WL-011` owns browser-backed event-ledger persistence, and `WL-013` owns runtime bundle lineage, replay checkpoints, and resume metadata in browser storage.
- Milestone 8 installable PWA hardening: `WL-007` now closes the installability umbrella with media-cache lifecycle hardening, while `WL-014` owns the concrete manifest, baseline service-worker cache policy, quota/backpressure handling, and deep-link resume work for mobile and tablet play.
- Resume/reconnect continuity hardening: regression coverage and lineage-fix follow-up now live under the existing local-first runtime seam and regression harness instead of reopening scaffold replacement work.

Each mapped milestone has an executable ownership boundary:

- dedicated play API work must stay package-only and role-aware, with no workbench DTO bleed-through or provider calls
- offline cache work must preserve provenance, stale protection, replay-safe resume, and bounded cache integrity metadata
- installable PWA work must keep player and GM shells role-gated while supporting install, offline boot, and deep-link recovery
