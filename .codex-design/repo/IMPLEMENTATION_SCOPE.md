# Play implementation scope

## Mission

`chummer6-mobile` owns the player and GM play-mode shell, offline ledger/cache, sync client behavior, installable play/mobile UX, and play-safe live-session surfaces.

## Owns

* player shell
* GM shell
* offline ledger/cache
* sync client and reconnect behavior
* installable PWA/mobile UX
* play-safe Coach/Spider surfaces
* device-appropriate live-session interactions

## Must not own

* builder/workbench UX
* rules math or runtime fingerprint generation
* provider secrets
* publication or moderation workflows
* registry persistence
* render execution

## Current focus

* replace scaffolded bootstrap/session clients with real play API seams
* consume only `Chummer.Engine.Contracts`, `Chummer.Play.Contracts`, and `Chummer.Ui.Kit`
* receive mirrored `.codex-design/*` guidance like every other active repo
* turn offline ledger and event log into real durable client substrate

## Milestone spine

* L0 package canon
* L1 local ledger and sync
* L2 player shell
* L3 GM shell
* L4 relay/runtime convergence
* L5 Coach/Spider surfaces
* L6 mobile/PWA polish
* L7 observer/cross-device continuity
* L8 hardening
* L9 finished play shell

## Worker rule

If the feature exists to make a player or GM run a live session from a dedicated play shell, it belongs here.
If it looks like a builder, publisher, moderator, or rules engine job, it does not.


## External integration note

`chummer6-mobile` may render upstream projections, previews, docs/help links, and provider-assisted artifact references.

It must not own:

* vendor credentials
* direct provider SDK integrations
* direct third-party API orchestration
