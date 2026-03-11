# chummer6-mobile

Player, GM, and session-shell frontend for Chummer6.

Current scope:
- player and GM play shells
- local-first session ledger handling
- runtime stack consumption
- play-scoped coach and Spider surfaces
- offline and media caching
- dedicated `/api/play/*` route ownership
- installable PWA hardening for mobile/tablet play

This repo must consume canonical shared packages only:
- `Chummer.Engine.Contracts`
- `Chummer.Play.Contracts`
- `Chummer.Ui.Kit`

It must not copy shared contracts from other Chummer repos.

## Design Mirror

Repo-local Chummer design mirror files live under `.codex-design/`:
- `.codex-design/product/README.md`
- `.codex-design/repo/IMPLEMENTATION_SCOPE.md`
- `.codex-design/review/REVIEW_CONTEXT.md`

## Verification

Run the local fast-path verification:

```bash
bash scripts/ai/verify.sh
```

Run ad hoc `dotnet` restore/build/run commands through the repo package-plane helper so the shared package feed resolves the same way as verification:

```bash
bash scripts/ai/with-package-plane.sh build Chummer.Play.slnx --nologo
```

To run the published-feed package-plane cutover path for `Chummer.Play.Contracts` and `Chummer.Ui.Kit`, provide semicolon-delimited restore sources:

```bash
CHUMMER_PUBLISHED_FEED_SOURCES="https://api.nuget.org/v3/index.json;https://packages.example.invalid/v3/index.json" \
  bash scripts/ai/with-package-plane.sh build Chummer.Play.slnx --nologo
```
