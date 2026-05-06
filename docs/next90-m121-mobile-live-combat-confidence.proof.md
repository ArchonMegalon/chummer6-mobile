# M121 Mobile Live Combat Confidence Proof

Package: `next90-m121-mobile-add-player-table-cards-between-turn-affordances-and-gm-l`  
Work task: `121.4`  
Milestone: `121`  
Concrete checkout root: `/docker/chummercomplete/chummer-play`  
Canonical queue/registry repo label: `chummer6-mobile`

This implementation receipt covers the `add_player_table_cards_between:mobile` slice for player table cards, between-turn affordances, and GM-lite continuity views.

## What landed

- `src/Chummer.Play.Core/Application/PlayCampaignWorkspaceLiteProjector.cs` now emits `PlayerTableCardsSummary`, `PlayerTableCardLabels`, `BetweenTurnAffordancesSummary`, `BetweenTurnAffordanceLabels`, `GmLiteContinuitySummary`, and `GmLiteContinuityLabels`.
- The new projection stays package-owned and mobile-safe: it reuses session projection, quick actions, runboard posture, replay-safe continuity, and support follow-through without introducing combat math, VTT state, or a second GM authority.
- `src/Chummer.Play.Player/PlayerShell/PlayerShellModule.cs` and `src/Chummer.Play.Gm/TacticalShell/GmTacticalShellModule.cs` keep the role-shell summaries explicit so the player shell stays table-card and between-turn focused while the GM shell stays bounded to tactical cards and GM-lite continuity.
- `src/Chummer.Play.Web/wwwroot/index.html` now renders dedicated player table cards, between-turn affordance, and GM-lite continuity cards inside the installable shell.
- `src/Chummer.Play.RegressionChecks/Program.cs` now proves the new live-combat confidence summaries for player, observer, and GM roles, fail-closes the shell bindings for the new cards, and keeps the role-shell descriptor copy pinned to the same live-combat confidence scope.

## Proof posture

- The repo-local `.codex-design/product/` mirror is the first design anchor for this receipt:
  - `.codex-design/product/NEXT_90_DAY_PRODUCT_ADVANCE_REGISTRY.yaml`
  - `.codex-design/product/NEXT_90_DAY_QUEUE_STAGING.generated.yaml`
- The Fleet-published queue mirror remains:
  - `/docker/fleet/.codex-studio/published/NEXT_90_DAY_QUEUE_STAGING.generated.yaml`
- This proof does not claim queue closure.
- The canonical successor queue row remains `not_started` while sibling milestone 121 packages are still open.
- `scripts/verify_next90_m121_mobile_live_combat_confidence.py` fail-closes queue-row drift, registry drift, shell-binding drift, generated-proof drift, proof-doc drift, and worker-unsafe telemetry-marker drift for this implementation receipt.
- `scripts/materialize_mobile_local_release_proof.py` and `.codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json` carry the package receipt and the `mobile_live_combat_confidence` journey marker, including the dedicated summary bindings plus the list-node bindings for player table cards, between-turn affordances, and GM-lite continuity labels.
- Future shards should extend or close the package with canonical evidence instead of reopening these shipped mobile live-combat confidence mechanics.

## Verification

- `python3 scripts/verify_next90_m121_mobile_live_combat_confidence.py`
