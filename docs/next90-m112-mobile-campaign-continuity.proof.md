# M112 Mobile Campaign Continuity Proof

Package: `next90-m112-mobile-campaign-continuity`
Milestone: `112`
Owner: `chummer6-mobile`
Concrete checkout root: `/docker/chummercomplete/chummer-play`
Canonical queue/registry repo label: `chummer6-mobile`

## Scope

This receipt covers the assigned successor slice only:

- `campaign_memory:travel`
- `campaign_state:mobile`

The implementation keeps continuity state package-owned inside the mobile play shell and travel restore lane. It does not widen mobile into a second campaign authority, bypass stale protection, or soften install-local cache boundaries.

## Landed Surface

The mobile shell and travel restore lane now expose current posture plus cached, stale, and action-required campaign continuity as first-class fields instead of burying that meaning inside a single summary paragraph.

- `src/Chummer.Play.Core/Application/PlayCampaignWorkspaceLiteProjector.cs` now emits `MobileCampaignCurrentState`, `MobileCampaignCachedState`, `MobileCampaignStaleState`, and `MobileCampaignActionRequired` alongside the existing `MobileCampaignStateSummary` and label list so the current mobile campaign posture is explicit in the payload.
- `src/Chummer.Play.Core/Roaming/RoamingWorkspaceSyncPlanner.cs` now emits `TravelCampaignCurrentState`, `TravelCampaignCachedState`, `TravelCampaignStaleState`, and `TravelCampaignActionRequired` alongside the existing travel continuity summary and labels so claimed-device travel restore posture is explicit before reopening campaign work.
- `src/Chummer.Play.Web/wwwroot/index.html` renders dedicated mobile and travel continuity state blocks for current posture plus cached, stale, and action-required posture instead of only a combined sentence plus list items, and now marks the shell cards with tone-aware continuity state summaries so stale versus action-required posture is visible at a glance.
- `src/Chummer.Play.RegressionChecks/Program.cs` proves the new projection fields, restore-plan fields, shell regions, and shell bindings through the existing regression suite.

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

The package is materially complete for the `chummer6-mobile` slice in this checkout. This proof is intentionally an implementation-only receipt for the local successor slice, with a repo-local `implemented` receipt in `MOBILE_LOCAL_RELEASE_PROOF.generated.json`; it does not claim queue closure outside the current mobile repo.
