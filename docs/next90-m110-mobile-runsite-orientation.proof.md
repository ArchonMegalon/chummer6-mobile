# M110 Mobile Runsite Orientation Proof

Package: `next90-m110-mobile-runsite-orientation`
Frontier: `3664656855`
Milestone: `110`
Work task: `110.5`
Owner: `chummer6-mobile`
Concrete checkout root: `/docker/chummercomplete/chummer-play`
Canonical queue/registry repo label: `chummer6-mobile`

## Scope

This receipt covers the assigned mobile successor package only:

- `runsite_host_mode:mobile`
- `campaign_orientation:mobile`

The implementation is grounded in the existing M110 implementation row `110.3` and its narrower entry-point closure. This queue-closure guard does not claim hosted runsite composition, media rendering, route preview generation, tactical truth, or any design-owned policy lane. Runsite host mode remains pre-session orientation only.

## Landed Surface

The mobile play shell now keeps runsite host mode launchable from campaign and travel shells before live play while route preview, map, and tour artifacts remain inspectable truth instead of being replaced by host-mode chrome.

- `src/Chummer.Play.Core/Application/PlayCampaignWorkspaceServerPlane.cs` keeps runsite host mode attached to the `runsite_host_mode:mobile`, `runsite_host_mode:entry`, and `campaign_orientation:mobile` surfaces with `pre-session-orientation-only-not-tactical-truth` provenance.
- `src/Chummer.Play.Core/Application/PlayCampaignWorkspaceLiteProjector.cs` keeps `RunsiteOrientationSummary`, `RunsiteOrientationHref`, `RunsiteOrientationProvenanceSummary`, `RunsiteOrientationTruthPosture`, and the `runsite_host_mode` artifact shelf view visible on the campaign shell without losing role-preserving launch context.
- `src/Chummer.Play.Web/PlayWebApplication.cs` redirects `/artifacts` and `/artifacts/{artifactId}` back into the installable shell with canonical `artifactSurface=runsite_host_mode`, inferred session context, and preserved `orientationSurface=campaign_orientation`.
- `src/Chummer.Play.Web/wwwroot/index.html` keeps runsite host mode surfaced on onboarding, campaign, and travel launch rails, repeats the active runsite host truth posture, and preserves active launched-artifact links instead of dropping users to a generic browser shelf.
- `src/Chummer.Play.RegressionChecks/Program.cs` proves the campaign-shell projection, runsite artifact redirect, role preservation, travel-shell redirect, and inspectable-truth boundary for Player, GameMaster, and Observer roles.

## Verification

Package verifier:

```bash
python3 scripts/verify_next90_m110_mobile_runsite_orientation.py
```

Result:

```text
m110 mobile runsite orientation proof ok
```

Generated proof:

- `.codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json` includes a `package_receipts` entry for `next90-m110-mobile-runsite-orientation`.
- `docs/PLAY_RELEASE_SIGNOFF.md` includes `Runsite host and orientation launch criteria (M110)`.

## Closure Posture

The package is materially complete for the `chummer6-mobile` slice in this checkout. This proof closes the queue-owned successor package while explicitly relying on the existing M110 implementation row `110.3` rather than pretending a second independent source implementation exists.

Canonical closeout anchors:

- `/docker/chummercomplete/chummer-design/products/chummer/NEXT_90_DAY_PRODUCT_ADVANCE_REGISTRY.yaml` keeps the existing M110 mobile implementation evidence in `110.3` and the completed successor-package closure row in `110.5`.
- `/docker/chummercomplete/chummer-design/products/chummer/NEXT_90_DAY_QUEUE_STAGING.generated.yaml` closes `next90-m110-mobile-runsite-orientation` on `verify_closed_package_only`.
- `/docker/fleet/.codex-studio/published/NEXT_90_DAY_QUEUE_STAGING.generated.yaml` mirrors the same completed queue row for worker-safe verification.
- `scripts/verify_next90_m110_mobile_runsite_orientation.py`
- `.codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json`

Future shards should verify these anchors instead of reopening the package.
