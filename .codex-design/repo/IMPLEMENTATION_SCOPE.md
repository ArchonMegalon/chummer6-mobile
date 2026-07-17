# Mobile implementation scope

## Mission

`chummer6-mobile` owns the dedicated player/GM/session shell for Chummer6:
local-first play, reconnect/replay behavior, mobile/tablet UX, installable PWA hardening, and the live-session turn companion.

The mobile lane is not a downsized builder.
It is the table-side companion that helps a player resolve the next useful action quickly during live play.

## Owns

* player shell and GM shell for live play
* the phone-first turn-companion surface for current state, next action, resolution, and recent action history
* local-first session ledger handling on the client side
* reconnect, replay, resume, and observer continuity on the play shell side
* offline/media caching for play use
* dedicated `/api/play/*` route consumption and play-shell integration
* installable PWA hardening for mobile/tablet play
* bounded RUNSITE anchor consumption for orientation-safe room, zone, or hotspot context

## Must not own

* workbench/browser/desktop builder UX
* engine/rules evaluation truth
* full character creation or broad advancement flow
* deep stash or shopping management
* registry or publication moderation UX
* hosted orchestration ownership
* exact tactical-map authority or token-movement ownership
* copied shared contracts or copied shared UI primitives

## Package boundary

`chummer6-mobile` must consume canonical shared packages only:

* `Chummer.Engine.Contracts`
* `Chummer.Play.Contracts`
* `Chummer.Campaign.Contracts` for campaign continuity projections
* `Chummer.World.Contracts` for world-state and mission-market projections
* `Chummer.Ui.Kit`

## Boundary truth

The mobile boundary is healthy when the live shell can trust replay/resume without re-owning engine or workbench concerns.
The shell must read as a short-session turn companion rather than a compressed workbench.

Current exit criteria remain practical, not decorative:

* WL-005 class local-first seams must be boringly trustworthy
* observer and cross-device continuity must stay in the play-shell boundary
* package-only discipline must remain strict
* old `chummer6-mobile` naming must disappear from the live repo identity
* current state, modifiers, action resolution, and stale/sync posture must stay obvious under phone-size pressure

## Current reality

This split is materially healthy enough to close `B0`, `A2`, `D1`, and `E1`.
Remaining work is future capability depth and cross-head polish, not whether the play shell, its replay/resume guarantees, or its package seams are real.

The next meaningful refinement is not "more builder on mobile."
It is a better live-session companion:

* current-state HUD
* quick action rail
* bounded modifier stack
* digital roll or manual physical-roll entry
* explicit local, synced, stale, and queued trust posture

See `products/chummer/LIVE_SESSION_TURN_COMPANION.md`.

## Flagship-grade bar

`chummer6-mobile` is not flagship grade until:

* reconnect, replay, and resume feel boring under real session stress
* player and GM flows read as authored live-play experiences rather than downsized workbench screens
* offline and degraded-network posture is visible enough that the table knows what is safe to trust
* the common turn loop works in seconds, one-handed, without forcing the user through workbench-shaped navigation
