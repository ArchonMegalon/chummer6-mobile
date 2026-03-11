# GitHub Codex Review

PR: https://github.com/ArchonMegalon/chummer-play/pull/1

Findings:
- [high] src/Chummer.Play.Web/PlayWebApplication.cs : line 162 The reconnect route calls `eventLogStore.GetOrCreateAsync(...)` before any stale-lineage guard. If a stale reconnect cursor arrives, `BrowserSessionEventLogStore` resets the existing ledger on lineage mismatch, dropping pending events and sequence ownership. Add a stored-lineage check (checkpoint+ledger vs request cursor) and return stale/conflict without mutating storage when misaligned.
- [medium] src/Chummer.Play.Web/PlayRouteHandlers.cs : line 76 `HandleQuickActionAsync` and `HandleSyncAsync` do a pre-check then call queue mutations, but `BrowserSessionOfflineQueueService` re-validates lineage and throws `InvalidOperationException` on mismatch. A concurrent lineage change between these steps will currently surface as a 500 instead of a stale-safe response. Catch and translate this to stale/409 behavior using stored lineage projection/checkpoint.
- [medium] src/Chummer.Play.RegressionChecks/Program.cs : line 363 `VerifyBootstrapRejectsStaleLineageWithoutLedgerResetAsync` does not test stale bootstrap input. It only calls `ResolveProjectionSessionAsync(sessionId, ...)`, which has no caller lineage parameters, so it cannot detect stale `/api/play/bootstrap` behavior. Add an endpoint-level regression that submits conflicting bootstrap lineage (`sceneId/sceneRevision/runtimeFingerprint`) and asserts ledger pending events, sequence, and checkpoint lineage are preserved.
