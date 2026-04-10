# GitHub Codex Review

PR: https://github.com/ArchonMegalon/chummer6-mobile/pull/6

Findings:
- [high] src/Chummer.Play.Core/Application/PlayRoamingRestoreService.cs [state] restore-travel-device-lineage-duplication
Trusted restore input explicitly allows `<primary>:travel` via `TryResolveTrustedRestoreDeviceId` (`allowedDeviceIds = [primary, $"{primary}:travel"]`).; `CreatePlan` forwards the accepted `targetDeviceId` unchanged into `BuildRestoreProjection`.; `BuildRestoreProjection` always adds sibling `InstallationId: $"{targetDeviceId}:travel"`, so a trusted travel target like `install-play_tablet:travel` produces `install-play_tablet:travel:travel`, which breaks stable claimed-device identity semantics for restore/offline continuity.
Expected fix: Normalize to canonical primary device id before companion generation (or branch when target is already travel) so IDs remain stable and never expand to `:travel:travel`.
- [high] src/Chummer.Play.RegressionChecks/Program.cs [tests] missing-travel-target-route-regression-test
`VerifyResumeAndWorkspaceLiteRoutesStayRoleConcreteAsync` asserts default restore/onboarding behavior and untrusted-device rejection.; The test does not issue successful requests with `deviceId=<primary>:travel` for `/api/play/restore-plan/{sessionId}` or `/api/play/onboarding-recovery/{sessionId}`.; Current assertions therefore cannot detect malformed travel-target expansion or sibling-id drift in those live route payloads.
Expected fix: Add explicit trusted-travel success tests for both routes, asserting stable target/sibling IDs and role-concrete follow-through hrefs.
- [high] src/Chummer.Play.Core/Chummer.Play.Core.csproj [contracts] mobile-control-contract-consumer-drift
Mobile core now references `$(ChummerControlContractsPackageId)` in `Chummer.Play.Core.csproj`.; Mobile code directly imports `Chummer.Control.Contracts.Support` in workspace projectors.; Mirrored contract set lists `Chummer.Control.Contracts` consumers as `chummer6-ui`, `fleet`, `executive-assistant`, and `internal service clients`, but not `chummer6-mobile`.
Expected fix: Either remove the control-contract dependency from mobile surfaces or update approved design contract-consumer canon before using it in this repo.
