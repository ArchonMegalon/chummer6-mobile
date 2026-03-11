# Chummer Play

Chummer Play is the mobile and play-mode frontend for Chummer.

Current scope:
- player and GM play-mode shells
- local-first session ledger handling
- runtime bundle consumption
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
CHUMMER_PUBLISHED_FEED_SOURCES="https://api.nuget.org/v3/index.json;https://<internal-feed>/v3/index.json" \
  bash scripts/ai/with-package-plane.sh build Chummer.Play.slnx --nologo
```

The same published-feed inputs also drive `verify.sh`:

```bash
CHUMMER_PUBLISHED_FEED_SOURCES="https://api.nuget.org/v3/index.json;https://<internal-feed>/v3/index.json" \
  bash scripts/ai/verify.sh
```

When published feeds use package versions different from local preview defaults, optionally pin them for compatibility checks:

```bash
CHUMMER_PUBLISHED_FEED_SOURCES="https://api.nuget.org/v3/index.json;https://<internal-feed>/v3/index.json" \
CHUMMER_PUBLISHED_ENGINE_CONTRACTS_VERSION="0.1.0-preview.42" \
CHUMMER_PUBLISHED_PLAY_CONTRACTS_VERSION="0.1.0-preview.42" \
CHUMMER_PUBLISHED_UI_KIT_VERSION="0.1.0-preview.42" \
  bash scripts/ai/verify.sh
```

If `CHUMMER_PUBLISHED_FEED_SOURCES` is unset, verification falls back to repo-local package stubs so package-plane checks stay executable in offline environments.
