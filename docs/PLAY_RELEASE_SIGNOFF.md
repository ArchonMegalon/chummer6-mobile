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
