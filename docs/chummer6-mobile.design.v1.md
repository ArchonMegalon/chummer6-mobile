# chummer6-mobile design summary

`chummer6-mobile` owns the dedicated player and GM play shell for Chummer6.

## Boundary

This repo owns:

- role-aware play-shell UX
- local-first ledger, cache, and replay behavior
- reconnect, observe, and resume client seams
- installable mobile/tablet shell behavior

This repo does not own:

- workbench or builder UX
- canonical rules math
- provider secrets or direct third-party orchestration
- publish/admin/moderation surfaces

## Package-only rule

This repo consumes:

- `Chummer.Engine.Contracts`
- `Chummer.Play.Contracts`
- `Chummer.Ui.Kit`

It does not source-copy those boundaries.

## Current trust promise

The mobile shell should survive device drops, reconnect cleanly, and explain what state it believes is current without inventing a second semantic session truth.
