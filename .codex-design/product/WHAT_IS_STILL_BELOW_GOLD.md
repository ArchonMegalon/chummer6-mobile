# What Is Still Below Gold

Last reviewed: 2026-08-06

This file exists to stop structural closure from masquerading as flagship replacement truth.

## Current whole-product blockers

Current whole-product release proof is not gold. The authoritative source is
`FINAL_GOLD_GRAPH.generated.json`; this file summarizes it without widening any claim.

- Current whole-product verdict: `review_required`.
- Current registry channel: `preview`.
- Current registry rollout state: `coverage_incomplete`.
- Current registry supportability state: `review_required`.
- Current public shelf platform ids: `linux`, `windows`.
- Current public shelf head ids: `avalonia`.
- The preview shelf carries Avalonia Linux x64 and Windows x64 installers; public availability does not by itself prove public-stable supportability.
- macOS is not on the current public shelf.
- Native Windows startup/visual proof, the macOS artifact/proof lane, Google OAuth operator evidence, current scanner-backed supply-chain evidence, and the downstream release-control receipts remain outside repo-local proof authority or depend on those inputs.
- Campaign operability, Fleet readiness, the operator dashboard, the final janitor, and the flagship readiness gate must remain red until their current receipts independently pass.

## Families below gold

No in-scope flagship parity family is currently below `gold_ready`, but family parity is only one input to whole-product release truth.

`FLAGSHIP_PARITY_REGISTRY.yaml` may mark an in-scope family `gold_ready` only from that family's proof. It must not be read as a public-stable or whole-product verdict.

## Regression watch

Keep this file as the fail-closed place to record future regressions if the published readiness proof reopens a coverage lane or if any family in `FLAGSHIP_PARITY_REGISTRY.yaml` falls below its currently justified state.

Do not reintroduce a public or operator flagship claim if the promoted Avalonia route loses workbench-first startup or restore continuation, the real `File` menu, first-class master index, first-class character roster, in-app claim/restore/recovery, dense-workbench budget proof, or honest support/crash routing.
