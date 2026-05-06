# M112 Mobile Campaign Continuity Proof

Package: `next90-m112-mobile-campaign-continuity`
Milestone: `112`
Owner: `chummer6-mobile`
Concrete checkout root: `/docker/chummercomplete/chummer-play`
Canonical queue/registry repo label: `chummer6-mobile`
Allowed package paths: `src`, `tests`, `docs`, `scripts`
Active flagship frontier: `1033794907`

## Scope

This receipt covers the assigned successor slice only:

- `campaign_memory:travel`
- `campaign_state:mobile`

The implementation keeps continuity state package-owned inside the mobile play shell and travel restore lane. It does not widen mobile into a second campaign authority, bypass stale protection, or soften install-local cache boundaries.

## Landed Surface

The mobile shell and travel restore lane now expose current posture plus cached, stale, and action-required campaign continuity as first-class fields instead of burying that meaning inside a single summary paragraph.

- `src/Chummer.Play.Core/Application/PlayCampaignWorkspaceLiteProjector.cs` now emits `MobileCampaignCurrentState`, `MobileCampaignCachedState`, `MobileCampaignStaleState`, and `MobileCampaignActionRequired` alongside the existing `MobileCampaignStateSummary` and label list so the current mobile campaign posture is explicit in the payload, and the action-required field now repeats the claimed mobile lane plus session context instead of relying on a generic workspace action rail.
- `src/Chummer.Play.Core/Roaming/RoamingWorkspaceSyncPlanner.cs` now emits `TravelCampaignCurrentState`, `TravelCampaignCachedState`, `TravelCampaignStaleState`, and `TravelCampaignActionRequired` alongside the existing travel continuity summary and labels so claimed-device travel restore posture is explicit before reopening campaign work, and the action-required field now repeats the target travel lane and installation id.
- `src/Chummer.Play.Core/Application/PlayEntryRecoveryProjector.cs` now repeats the travel continuity cached, stale, and action-required breakdown inside the recovery action list and upgrades the post-failure entry summary so continuity state stays explicit even before the user resumes or opens the broader restore card.
- `src/Chummer.Play.Web/wwwroot/index.html` renders dedicated mobile and travel continuity state blocks for current posture plus cached, stale, and action-required posture instead of only a combined sentence plus list items, keeps tone-aware continuity state summaries in the shell, and now lets the continuity cards tone from explicit stale posture as well as the action-required field before falling back to summary copy so warning-only restore or cache-pressure posture cannot still look green at a glance. The workspace and restore rails also keep separate `ActionRequiredSummary` and `ActionRequiredLabels` bindings visible so the user never has to infer the next safe move from stale-state prose alone.
- `src/Chummer.Play.RegressionChecks/Program.cs` proves the new projection fields, restore-plan fields, shell regions, and shell bindings through the existing regression suite.
- `scripts/materialize_mobile_local_release_proof.py` now fails closed if the generated local release receipt stops proving the cached, stale, action-required, restore-action-required, hero continuity-tone, stale-tone handoff, restore/workspace continuity-breakdown bindings, and travel-companion detail bindings for this package, and preserves the existing generated proof timestamp when the payload is semantically unchanged so unrelated proof refreshes do not reopen the M112 slice. This document is the closed-package proof posture for the package-owned mobile continuity slice.

## Verification

Regression checks:

```bash
scripts/ai/with-package-plane.sh dotnet run --project src/Chummer.Play.RegressionChecks/Chummer.Play.RegressionChecks.csproj
```

Result:

```text
chummer6-mobile regression checks ok
```

Implementation-only verifier:

```bash
python3 scripts/verify_next90_m112_mobile_campaign_continuity.py
```

Result:

```text
m112_mobile_campaign_continuity_verify_ok
```

Executable anchors:

- `docs/PLAY_RELEASE_SIGNOFF.md`
- `scripts/materialize_mobile_local_release_proof.py`
- `.codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json`
- `scripts/verify_next90_m112_mobile_campaign_continuity.py`

## Closure Posture

The package is materially complete for the `chummer6-mobile` slice in this checkout. The canonical queue and registry rows now record this package as `complete` under `verify_closed_package_only`, while the repo-local `implemented` receipt in `MOBILE_LOCAL_RELEASE_PROOF.generated.json` stays package-scoped evidence for the mobile repo rather than a second queue authority.

The same repo-local receipt also pins the active flagship frontier id so this shipped mobile continuity slice stays attached to the current full-product closeout pass instead of reading like an orphaned successor-only proof note.

The implementation and proof remain intentionally scoped to the package-owned paths above so future shards cannot widen this receipt into sibling milestone work.

Canonical anchors:

- `NEXT_90_DAY_PRODUCT_ADVANCE_REGISTRY.yaml`
- `NEXT_90_DAY_QUEUE_STAGING.generated.yaml`

`scripts/verify_next90_m112_mobile_campaign_continuity.py` now fail-closes if the Fleet queue mirror, the mirrored design queue row, or the canonical registry row drift away from the closed-package posture, if the shell drops either explicit workspace or restore continuity-breakdown wiring while keeping only the helper definitions, or if the generated mobile release proof adds duplicate M112 receipts, reuses this proof marker, work-task identity, title, or either owned surface on another package row.
