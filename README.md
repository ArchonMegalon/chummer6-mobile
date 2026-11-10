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
- `Chummer.Campaign.Contracts`
- `Chummer.Play.Contracts`
- `Chummer.Ui.Kit`

It must not copy shared contracts from other Chummer repos.
It must also treat semantic session events as engine-owned truth, not redefine them locally.

## What rejoin and resume guarantee

- rejoin never invents a second truth about session state
- resume is lineage-safe, not "best effort and pray"
- replay catches a device back up without mutating the canonical ledger out of order

Public guarantee note: [docs/rejoin-resume-guarantees.md](docs/rejoin-resume-guarantees.md)

Release signoff note: [docs/PLAY_RELEASE_SIGNOFF.md](docs/PLAY_RELEASE_SIGNOFF.md)

## Design Mirror

Repo-local Chummer design mirror files live under `.codex-design/`:
- `.codex-design/product/README.md`
- `.codex-design/repo/IMPLEMENTATION_SCOPE.md`
- `.codex-design/review/REVIEW_CONTEXT.md`

Historical compatibility note:

- the older design draft name `docs/chummer-play.design.v1.md` is still present for compatibility, but `chummer6-mobile` is the live repo identity

## Verification

Run the local fast-path verification:

```bash
bash scripts/ai/verify.sh
```

The default `slice` mode checks repo-owned source, boundaries, Python contracts, the mirrored design files, M121 evidence and PWA asset budgets, then builds and runs the package-plane and local browser smoke checks. A standalone checkout bootstraps real owner packages from exact public source commits and project digests in `eng/package-plane.lock.json`; see [docs/PACKAGE_BOOTSTRAP.md](docs/PACKAGE_BOOTSTRAP.md). Stub fallback is disabled in `slice`. Prerequisites are SDK 10.0.103 for the source bootstrap, Git, public GitHub/NuGet access, Python 3 with PyYAML and Playwright (including its browser), and ripgrep. Missing tools, source verification failures or product failures fail verification. Recorded M121 `checkout_root` is generation provenance, not a requirement to verify from that host directory.

Focused repo-only checks do not need Fleet, parent Git metadata or managed builds:

```bash
python3 -m unittest discover -s tests -p 'test_next90_m121_mobile_live_combat_confidence.py'
python3 -m unittest discover -s tests -p 'test_mobile_pwa_performance_budget.py'
python3 scripts/verify_next90_m121_mobile_live_combat_confidence.py
python3 scripts/verify_mobile_pwa_performance_budget.py --check-only
```

Fleet parity and the external release-controller contract are separate commands with explicit inputs. Missing files fail; neither command discovers sibling layouts or skips an integration test:

```bash
python3 scripts/verify_next90_m121_mobile_live_combat_confidence.py \
  --fleet-queue /path/to/fleet/.codex-studio/published/NEXT_90_DAY_QUEUE_STAGING.generated.yaml
python3 tests/integration/test_mobile_pwa_release_chain.py \
  --release-root /path/to/release-checkout --run-services-root /path/to/run-services
python3 tests/integration/test_next90_m122_mobile_runner_goal_updates.py \
  --fleet-root /path/to/fleet --design-product-root /path/to/design/products/chummer
```

Root `tests/test_*.py` are repo tests (the browser tests still need Playwright). Tests under `tests/integration/` must be invoked directly with their required inputs; ordinary root discovery does not claim external contract coverage. The M122 worker-context proof hygiene check stays in repo tests, while both original M122 end-to-end assertions remain in integration.

The M122 integration entry point also requires a current `MOBILE_LOCAL_RELEASE_PROOF.generated.json` for this checkout. Full integration/release verification refreshes that proof before invoking the entry point; a direct invocation must follow the same refresh and retain its actual external evidence.

Full external verification runs both `tests/integration/` entry points and retains the Fleet milestone, canonical mirror comparison, cross-surface/public-edge and release-proof gates. It requires explicit existing roots before it starts product checks:

```bash
CHUMMER_VERIFY_MODE=integration CHUMMER_ALLOW_STUB_PACKAGES=0 \
  CHUMMER_FLEET_ROOT=/path/to/fleet CHUMMER_DESIGN_ROOT=/path/to/design \
  CHUMMER_RELEASE_ROOT=/path/to/release-checkout CHUMMER_RUN_SERVICES_ROOT=/path/to/run-services \
  CHUMMER_PUBLISHED_FEED_SOURCES="<pinned-feed>" bash scripts/ai/verify.sh
```

Use `CHUMMER_VERIFY_MODE=release` with the same inputs for release certification. Only explicitly selected `scaffold` mode may use the legacy empty package scaffolds; this exercises bootstrap wiring and cannot establish a product build. `slice` always disables stub fallback. `integration` and `release` reject stubs, and their `--no-restore`/`--no-build` steps require matching same-run package-plane provenance. The existing public-edge environment and current strict receipts are also required by their gates. Release verification rejects skipped feed or public-edge proof. Every full default run writes `.codex-studio/published/MOBILE_VERIFICATION_MODE.generated.json` with its mode, run ID, omitted external gates and release-evidence eligibility. Repo success never asserts Fleet or release success. There is no checked-in GitHub Actions workflow in this checkout; automation should invoke the default and explicit integration commands with the same separation.

If the local design mirror drifts from canon, repair only this repo mirror bundle with:

```bash
bash scripts/ai/repair_design_mirror.sh
```

Run ad hoc `dotnet` restore/build/run commands through the repo package-plane helper so the shared package feed resolves the same way as verification:

```bash
bash scripts/ai/with-package-plane.sh build Chummer.Play.slnx --nologo
```

The helper now serializes access to the shared `.artifacts/nuget-packages` cache with an internal `flock`, so concurrent package-plane runs do not delete each other's extracted contract packages mid-build. Override the defaults only when you need a separate lock scope or a shorter wait:

```bash
CHUMMER_PACKAGE_PLANE_LOCK_FILE=/tmp/chummer-play-alt.lock \
CHUMMER_PACKAGE_PLANE_LOCK_TIMEOUT_SECONDS=120 \
  bash scripts/ai/with-package-plane.sh build Chummer.Play.slnx --nologo
```

To run the published-feed package-plane cutover path for `Chummer.Play.Contracts` and `Chummer.Ui.Kit`, provide semicolon-delimited restore sources:

```bash
CHUMMER_PUBLISHED_FEED_SOURCES="https://api.nuget.org/v3/index.json;https://packages.example.invalid/v3/index.json" \
  bash scripts/ai/with-package-plane.sh build Chummer.Play.slnx --nologo
```
