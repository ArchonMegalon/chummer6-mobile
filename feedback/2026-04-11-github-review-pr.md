# GitHub Codex Review

PR: https://github.com/ArchonMegalon/chummer6-mobile/pull/7

Findings:
- [high] src/Chummer.Play.Core/Application/PlayRoamingRestoreService.cs [state] restore-travel-device-lineage-duplication
`TryResolveTrustedRestoreDeviceId` explicitly accepts `<primary>:travel` as trusted input.; `CreatePlan` forwards the trusted `targetDeviceId` unchanged into `BuildRestoreProjection`.; `BuildRestoreProjection` always adds a sibling with `InstallationId: $"{targetDeviceId}:travel"`, so trusted travel input (for example `install-play_tablet:travel`) produces `install-play_tablet:travel:travel`, breaking stable claimed-device lineage.
Expected fix: Normalize trusted travel input to canonical primary before sibling generation (or branch when target already has `:travel`) so restore IDs never expand to `:travel:travel`.
- [high] src/Chummer.Play.RegressionChecks/Program.cs [tests] missing-travel-target-route-regression-test
`VerifyResumeAndWorkspaceLiteRoutesStayRoleConcreteAsync` validates default restore/onboarding behavior and untrusted `deviceId` rejection.; The same test does not execute successful requests with `deviceId=<primary>:travel` for `/api/play/restore-plan/{sessionId}` or `/api/play/onboarding-recovery/{sessionId}`.; Without trusted-travel success assertions, current checks cannot detect malformed travel-target expansion or sibling-id drift.
Expected fix: Add trusted-travel success tests for both routes, asserting stable target/sibling IDs (no `:travel:travel`) and role-concrete follow-through hrefs.
- [high] src/Chummer.Play.Core/Chummer.Play.Core.csproj [contracts] mobile-control-contract-consumer-drift
`Chummer.Play.Core.csproj` adds `$(ChummerControlContractsPackageId)`.; Mobile core projectors import `Chummer.Control.Contracts.Support` directly.; Mirrored `CONTRACT_SETS.yaml` lists `Chummer.Control.Contracts` consumers as `chummer6-ui`, `fleet`, `executive-assistant`, and internal service clients, but not `chummer6-mobile`.
Expected fix: Either remove control-contract usage from mobile surfaces, or update approved design contract-consumer canon/scope to explicitly authorize mobile as a consumer before shipping this dependency.
