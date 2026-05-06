# M117 Mobile Artifact Shelf Proof

Package: `next90-m117-mobile-artifact-shelf`  
Successor frontier: `3440617449`  
Active flagship frontier: `3371889980`  
Work task: `117.4`  
Milestone: `117`  
Concrete checkout root: `/docker/chummercomplete/chummer-play`  
Canonical queue/registry repo label: `chummer6-mobile`

This repo-local closure receipt covers the `artifact_shelf:mobile` and `artifact_recap_view:mobile` slice, closes the canonical successor queue and registry rows for `chummer6-mobile`, and preserves the historical active-flagship frontier anchor without reopening that shipped wave.

## What landed

- `src/Chummer.Play.Core/Application/PlayCampaignWorkspaceLiteProjector.cs` now keeps `SelectedArtifactView`, `ArtifactShelfSelectionSummary`, `SelectedRecapArtifactSummary`, `SelectedRecapArtifactHref`, and four explicit artifact shelf browse lanes for `personal`, `campaign`, `travel`, and `creator`.
- Artifact shelf browse targets are now session-aware and role-aware, using `/artifacts/{sessionId}?role=...&view=...` instead of anonymous generic shelf links.
- Repo-local regression proof now also pins plain shelf browse redirects so campaign/travel shelf entry links preserve the selected shelf lane before any recap artifact is chosen.
- Recap-artifact deep links now stay installable-shell-owned through `/artifacts/{sessionId}/{artifactId}` redirects that preserve the selected shelf lane and artifact identity.
- `src/Chummer.Play.Web/PlayWebApplication.cs` now round-trips both `artifactView` and `artifactId` through `/api/play/workspace-lite/{sessionId}` and redirects `/artifacts/{sessionId}` back into the installable shell.
- `src/Chummer.Play.Web/wwwroot/index.html` now renders a dedicated selected artifact shelf summary, a selected recap artifact summary, a direct recap-artifact deep link, and a direct follow-through link for the active mobile shelf view.
- The owned shell now keeps the selected recap-artifact deep link separate from the shelf-browse follow-through, so browsing the campaign or travel shelf does not silently collapse into reopening a pinned recap artifact.
- `src/Chummer.Play.RegressionChecks/Program.cs` now proves campaign, travel, and recap shelf visibility, role-concrete shelf links, travel-view reopening, selected-view shell bindings, and recap-artifact identity surviving into the server-backed workspace projection.
- Selected recap artifact identity now stays visible inside the owned mobile shell as projection-backed shelf state, and the summary now names the selected travel shelf or campaign lane instead of leaving that follow-through implicit in a redirect query parameter.

## Proof posture

- The active flagship frontier remains `3371889980` for `Add mobile artifact shelf views for campaign, travel, and recap artifacts`.
- Queue-closure guard: the repo-local verifier now fails closed unless the canonical registry row, design queue row, fleet queue row, proof receipt, and generated package receipt all agree that `next90-m117-mobile-artifact-shelf` is complete and verify-only.
- The canonical closeout rows now use `verify_closed_package_only`, so future shards must verify the closure anchors instead of reopening the slice.
- Canonical closeout anchors:
  - `/docker/chummercomplete/chummer-design/products/chummer/NEXT_90_DAY_PRODUCT_ADVANCE_REGISTRY.yaml`
  - `/docker/chummercomplete/chummer-design/products/chummer/NEXT_90_DAY_QUEUE_STAGING.generated.yaml`
  - `/docker/fleet/.codex-studio/published/NEXT_90_DAY_QUEUE_STAGING.generated.yaml`
- The generated local-release receipt in `MOBILE_LOCAL_RELEASE_PROOF.generated.json` now carries the closed package receipt for this slice, so regenerated proof cannot silently fall back to implementation-only posture.
- The repo-local verifier rejects blocked helper evidence case-insensitively in both the proof note and the generated receipt, so stale active-run telemetry or handoff citations cannot close M117.
- The package is materially complete for the `chummer6-mobile` slice in this checkout.
- Future shards should verify these anchors instead of reopening the package.
