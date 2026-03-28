# Play Release Signoff

Purpose: keep the mobile/play share of `F0` explicit now that `E1` is already materially closed.

## Current hardening contract

`chummer6-mobile` stays release-complete for the current player/GM shell when `scripts/ai/verify.sh` keeps the following evidence executable:

- accessibility proof via `VerifyIndexShellAccessibilityContractAsync` and `VerifyBootstrapRoleShellEntryPointsAsync`
- performance-budget proof via `VerifyCachePressureBudgetContractAsync`
- replay/rejoin safety via the regression checks already tied into `M10` and `M11`
- package-only shared boundary consumption for `Chummer.Engine.Contracts`, `Chummer.Play.Contracts`, and `Chummer.Ui.Kit`

## Release budgets

- Accessibility: the installable shell must keep `<html lang="en">` and the polite live-status region in `src/Chummer.Play.Web/wwwroot/index.html`.
- Localization: the shell must keep explicit document-language and role-entry semantics instead of relying on implicit browser defaults.
- Performance: runtime-bundle cache pressure must preserve the current bounded quota budget (`RuntimeBundleQuota == 8`) and continue to report backpressure when the budget is saturated.

## Exit statement

The mobile/play head no longer blocks the product on shell reality, replay safety, accessibility, or bounded performance behavior. Remaining work is future product depth, not missing release proof for the current play shell.

## Post-closure completion criteria (M12)

The post-closure depth lane is considered executable when the following remain true and regression-backed:

- Player shell criteria: browser transport + event-log + offline resume stay lineage-safe, and player role actions stay limited to player-safe capabilities even when capability descriptors are over-provisioned.
- GM shell criteria: GM-only action and Spider-card capability gates remain enforced even when player-safe capabilities are present, and continuity/observe routes keep stale-lineage-safe behavior.
- Release-proof cadence criteria: each closure slice must keep these criteria represented in `WORKLIST.md` (`TG-M12-PL`, `TG-M12-GM`, `TG-M12-RP`) and preserved by `scripts/ai/verify.sh`.
