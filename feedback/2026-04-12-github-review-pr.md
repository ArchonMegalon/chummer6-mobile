# GitHub Codex Review

PR: https://github.com/ArchonMegalon/chummer6-mobile/pull/9

Findings:
- [high] src/Chummer.Play.Core/Chummer.Play.Core.csproj [contracts] contracts-mobile-control-consumer-drift
`src/Chummer.Play.Core/Chummer.Play.Core.csproj` adds `$(ChummerControlContractsPackageId)` (line 9).; `src/Chummer.Play.Core/Application/PlayCampaignWorkspaceLiteProjector.cs` imports `Chummer.Control.Contracts.Support` (line 5).; Mirrored `CONTRACT_SETS.yaml` lists `Chummer.Control.Contracts` consumers as `chummer6-ui`, `fleet`, `executive-assistant`, and `internal service clients` only (lines 69-73), excluding `chummer6-mobile`.; Mirrored `IMPLEMENTATION_SCOPE.md` package boundary still limits mobile to `Chummer.Engine.Contracts`, `Chummer.Play.Contracts`, and `Chummer.Ui.Kit` (lines 27-31).
Expected fix: Either remove `Chummer.Control.Contracts` consumption from mobile codepaths, or land an approved design update that explicitly authorizes `chummer6-mobile` as a `Chummer.Control.Contracts` consumer and updates local mirror scope accordingly before shipping.
