# M145 Mobile Quick Explain And Follow-Up Proof

Package: `next90-m145-mobile-quick-explain-and-follow-up`
Work task: `145.3`
Milestone: `145`
Owner: `chummer6-mobile`
Concrete checkout root: `/docker/chummercomplete/chummer-play`
Canonical queue/registry repo label: `chummer6-mobile`

## Scope

This receipt covers the assigned successor slice only:

- `quick_explain:mobile`
- `grounded_follow_up:mobile`

The implementation stays inside the package-owned mobile play shell. It does not invent rules truth, create a second campaign authority, or widen follow-up into a new control surface outside the claimed live-play shell.

## Landed Surface

The mobile workspace-lite shell now carries packet-backed quick explain, source-anchor context, stale-state posture, and grounded text-first follow-up directly in the projection payload instead of leaving the user to infer that meaning from scattered labels.

- `src/Chummer.Play.Core/Application/PlayCampaignWorkspaceLiteProjector.cs` now emits `QuickExplainSummary`, `QuickExplainLabels`, `SourceAnchorSummary`, `SourceAnchorLabels`, `StaleStatePosture`, `GroundedFollowUpSummary`, and `GroundedFollowUpLabels` from the existing session packet, replay sequence, checkpoint, runtime bundle, and role route.
- `src/Chummer.Play.Web/wwwroot/index.html` now renders a dedicated quick-explain card that keeps packet-backed visible-value explanation, source-anchor context, stale-state posture, and bounded text-first follow-up readable on the mobile/live-play shell without opening a second workbench-style surface.
- `src/Chummer.Play.RegressionChecks/Program.cs` now proves the new projection fields plus the shell regions and bindings through the existing regression harness.

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
python3 scripts/verify_next90_m145_mobile_quick_explain_and_follow_up.py
```

Result:

```text
m145_mobile_quick_explain_and_follow_up_verify_ok
```

Executable anchors:

- `docs/PLAY_RELEASE_SIGNOFF.md`
- `scripts/materialize_mobile_local_release_proof.py`
- `.codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json`
- `scripts/verify_next90_m145_mobile_quick_explain_and_follow_up.py`

## Closure Posture

The package is materially complete for the `chummer6-mobile` slice in this checkout. The queue-closure guard now lives in the canonical successor registry and both staged queue roots instead of relying on a repo-local implementation note alone.

Canonical closeout anchors:

- `/docker/chummercomplete/chummer-design/products/chummer/NEXT_90_DAY_PRODUCT_ADVANCE_REGISTRY.yaml`
- `/docker/chummercomplete/chummer-design/products/chummer/NEXT_90_DAY_QUEUE_STAGING.generated.yaml`
- `/docker/fleet/.codex-studio/published/NEXT_90_DAY_QUEUE_STAGING.generated.yaml`
- `scripts/verify_next90_m145_mobile_quick_explain_and_follow_up.py`
- `.codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json`

Future shards should verify these anchors instead of reopening the package. The generated mobile release proof now carries a package receipt for this slice, and the canonical queue rows close on `verify_closed_package_only`.
