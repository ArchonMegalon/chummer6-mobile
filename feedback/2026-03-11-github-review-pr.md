# GitHub Codex Review

PR: https://github.com/ArchonMegalon/chummer-play/pull/1

Findings:
- [high] src/Chummer.Play.Web/BrowserSessionApiClient.cs : line 46 Reconnect stale conflicts are returned by the server as HTTP 409 with recovery payload (`projection` + `checkpoint`), but `ReconnectAsync` immediately calls `EnsureSuccessStatusCode()`. This turns expected stale-state flow into a transport exception and drops recovery data, creating an offline/reconnect stale-state hazard. Handle 409 explicitly and surface typed stale metadata to callers.
- [high] scripts/ai/verify.sh : line 49 Verification requires `src/Chummer.Play.RegressionChecks/Chummer.Play.RegressionChecks.csproj`, but that project file is not present in branch `HEAD` (only the regression `Program.cs` is committed). In a clean checkout, regression checks are not runnable, so stale/offline sync protections are effectively unverified. Commit the regression csproj (and keep it wired into normal build/test paths) to restore test coverage.
