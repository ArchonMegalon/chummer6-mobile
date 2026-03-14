using Chummer.Play.Core.Application;
using Chummer.Play.Core.Offline;
using Chummer.Play.Core.PlayApi;
using Chummer.Play.Core.Sync;
using Chummer.Play.Gm.TacticalShell;
using Chummer.Play.Player.PlayerShell;
using Chummer.Play.Web;
using Chummer.Play.Web.BrowserState;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;

await RunCheckAsync(nameof(VerifyLedgerLineageResetAsync), VerifyLedgerLineageResetAsync);
await RunCheckAsync(nameof(VerifyBootstrapProjectionPreservesReplayStateAsync), VerifyBootstrapProjectionPreservesReplayStateAsync);
await RunCheckAsync(nameof(VerifyMonotonicSequenceOwnershipAsync), VerifyMonotonicSequenceOwnershipAsync);
await RunCheckAsync(nameof(VerifyConcurrentEnqueueSequenceOwnershipAsync), VerifyConcurrentEnqueueSequenceOwnershipAsync);
await RunCheckAsync(nameof(VerifySyncPrefixAcknowledgementAsync), VerifySyncPrefixAcknowledgementAsync);
await RunCheckAsync(nameof(VerifySyncPreservesNewerLedgerSequenceAsync), VerifySyncPreservesNewerLedgerSequenceAsync);
RunCheck(nameof(VerifyCursorValidationRejectsNegativeSequence), VerifyCursorValidationRejectsNegativeSequence);
await RunCheckAsync(nameof(VerifyEventLogRejectsMalformedAppendAsync), VerifyEventLogRejectsMalformedAppendAsync);
await RunCheckAsync(nameof(VerifyEventLogRejectsSequenceRegressionAsync), VerifyEventLogRejectsSequenceRegressionAsync);
await RunCheckAsync(nameof(VerifyEventLogPersistsAcrossServiceInstancesAsync), VerifyEventLogPersistsAcrossServiceInstancesAsync);
await RunCheckAsync(nameof(VerifyOfflineQueueRejectsMalformedPendingEventsAsync), VerifyOfflineQueueRejectsMalformedPendingEventsAsync);
await RunCheckAsync(nameof(VerifyOfflineQueueRejectsNegativeSequenceAsync), VerifyOfflineQueueRejectsNegativeSequenceAsync);
await RunCheckAsync(nameof(VerifyOfflineQueueRejectsMalformedSessionEnvelopeAsync), VerifyOfflineQueueRejectsMalformedSessionEnvelopeAsync);
await RunCheckAsync(nameof(VerifyOfflineCacheRejectsMalformedCheckpointAndRuntimeEntryAsync), VerifyOfflineCacheRejectsMalformedCheckpointAndRuntimeEntryAsync);
await RunCheckAsync(nameof(VerifyOfflineCacheDropsMalformedStoredEntriesAsync), VerifyOfflineCacheDropsMalformedStoredEntriesAsync);
await RunCheckAsync(nameof(VerifyOfflineCacheDropsUnparseableStoredEntriesAsync), VerifyOfflineCacheDropsUnparseableStoredEntriesAsync);
await RunCheckAsync(nameof(VerifyOfflineCacheRuntimeBundleQuotaEvictionAsync), VerifyOfflineCacheRuntimeBundleQuotaEvictionAsync);
await RunCheckAsync(nameof(VerifyOfflineCacheReadsDoNotMutateQuotaEvictionAsync), VerifyOfflineCacheReadsDoNotMutateQuotaEvictionAsync);
await RunCheckAsync(nameof(VerifyOfflineCacheReadDoesNotRewriteStoredMetadataAsync), VerifyOfflineCacheReadDoesNotRewriteStoredMetadataAsync);
await RunCheckAsync(nameof(VerifyOfflineCacheConcurrentReadAndWritePreserveNewestSameSessionMetadataAsync), VerifyOfflineCacheConcurrentReadAndWritePreserveNewestSameSessionMetadataAsync);
await RunCheckAsync(nameof(VerifyOfflineCacheQuotaIgnoresUnparseableRuntimeBundleKeysAsync), VerifyOfflineCacheQuotaIgnoresUnparseableRuntimeBundleKeysAsync);
await RunCheckAsync(nameof(VerifyOfflineCacheConcurrentCrossSessionQuotaWritesStayBoundedAsync), VerifyOfflineCacheConcurrentCrossSessionQuotaWritesStayBoundedAsync);
await RunCheckAsync(nameof(VerifyIndexShellAccessibilityContractAsync), VerifyIndexShellAccessibilityContractAsync);
await RunCheckAsync(nameof(VerifyBootstrapRoleShellEntryPointsAsync), VerifyBootstrapRoleShellEntryPointsAsync);
await RunCheckAsync(nameof(VerifyQuickActionRejectsCrossRoleAuthorizationAsync), VerifyQuickActionRejectsCrossRoleAuthorizationAsync);
await RunCheckAsync(nameof(VerifyCachePressureBudgetContractAsync), VerifyCachePressureBudgetContractAsync);
await RunCheckAsync(nameof(VerifyEventLogDropsMalformedStoredLedgerAsync), VerifyEventLogDropsMalformedStoredLedgerAsync);
await RunCheckAsync(nameof(VerifyEventLogDropsUnparseableStoredLedgerKeysAsync), VerifyEventLogDropsUnparseableStoredLedgerKeysAsync);
await RunCheckAsync(nameof(VerifyOfflineQueueRejectsStaleLineageAsync), VerifyOfflineQueueRejectsStaleLineageAsync);
await RunCheckAsync(nameof(VerifyReconnectLineageTransitionContinuityAsync), VerifyReconnectLineageTransitionContinuityAsync);
await RunCheckAsync(nameof(VerifyStoredLineageStaleResponsesAsync), VerifyStoredLineageStaleResponsesAsync);
await RunCheckAsync(nameof(VerifyReconnectRejectsStaleLineageWithoutMutationAsync), VerifyReconnectRejectsStaleLineageWithoutMutationAsync);
await RunCheckAsync(nameof(VerifyReconnectClientThrowsTypedStaleAsync), VerifyReconnectClientThrowsTypedStaleAsync);
await RunCheckAsync(nameof(VerifyContinuityClaimRejectsStaleLineageWithoutMutationAsync), VerifyContinuityClaimRejectsStaleLineageWithoutMutationAsync);
await RunCheckAsync(nameof(VerifyContinuityClaimRejectsUnknownSessionWithoutMutationAsync), VerifyContinuityClaimRejectsUnknownSessionWithoutMutationAsync);
await RunCheckAsync(nameof(VerifyObserveDoesNotSeedStateForEmptySessionAsync), VerifyObserveDoesNotSeedStateForEmptySessionAsync);
await RunCheckAsync(nameof(VerifyObserveDoesNotMutateStoredStateOrReturnStaleRuntimeBundleAsync), VerifyObserveDoesNotMutateStoredStateOrReturnStaleRuntimeBundleAsync);
await RunCheckAsync(nameof(VerifyObserveKeepsRequestedSessionIdWhenStoredCheckpointDriftsAsync), VerifyObserveKeepsRequestedSessionIdWhenStoredCheckpointDriftsAsync);
await RunCheckAsync(nameof(VerifyObserveReturnsLineageSafeContinuityAsync), VerifyObserveReturnsLineageSafeContinuityAsync);
await RunCheckAsync(nameof(VerifyObservePreservesContinuityWhenClaimCursorLeadsLedgerAsync), VerifyObservePreservesContinuityWhenClaimCursorLeadsLedgerAsync);
await RunCheckAsync(nameof(VerifyObserveRouteRoundTripAsync), VerifyObserveRouteRoundTripAsync);
await RunCheckAsync(nameof(VerifyQueueMutationLineageExceptionReturnsStaleAsync), VerifyQueueMutationLineageExceptionReturnsStaleAsync);
await RunCheckAsync(nameof(VerifyBootstrapRejectsStaleLineageWithoutLedgerResetAsync), VerifyBootstrapRejectsStaleLineageWithoutLedgerResetAsync);
await RunCheckAsync(nameof(VerifyProjectionPrefersStoredLedgerWithoutCheckpointAsync), VerifyProjectionPrefersStoredLedgerWithoutCheckpointAsync);
RunCheck(nameof(VerifyStoredStaleStatePrefersLedgerOverOlderCheckpoint), VerifyStoredStaleStatePrefersLedgerOverOlderCheckpoint);
await RunCheckAsync(nameof(VerifyResumePreservesLedgerWhenRuntimeBundleDriftsAsync), VerifyResumePreservesLedgerWhenRuntimeBundleDriftsAsync);
await RunCheckAsync(nameof(VerifyResumeNormalizesCheckpointToLedgerLineageAsync), VerifyResumeNormalizesCheckpointToLedgerLineageAsync);
await RunCheckAsync(nameof(VerifyRuntimeBundleSessionLockReleasesOnCanceledAcquireAsync), VerifyRuntimeBundleSessionLockReleasesOnCanceledAcquireAsync);
RunCheck(nameof(VerifyCheckpointLineageAlignment), VerifyCheckpointLineageAlignment);
RunCheck(nameof(VerifyStoredLineageAlignment), VerifyStoredLineageAlignment);

Console.WriteLine("chummer6-mobile regression checks ok");

static void RunCheck(string name, Action action)
{
    TraceCheckBoundary("START", name);
    action();
    TraceCheckBoundary("OK", name);
}

static async Task RunCheckAsync(string name, Func<Task> action)
{
    TraceCheckBoundary("START", name);
    await action();
    TraceCheckBoundary("OK", name);
}

static void TraceCheckBoundary(string phase, string name)
{
    if (!string.Equals(Environment.GetEnvironmentVariable("CHUMMER_REGRESSION_TRACE"), "1", StringComparison.Ordinal))
    {
        return;
    }

    Console.Error.WriteLine($"{phase} {name}");
}

static async Task VerifyLedgerLineageResetAsync()
{
    var store = new BrowserSessionEventLogStore(new InMemoryBrowserKeyValueStore());

    await store.AppendPendingEventsAsync("session-a", "scene-a", "scene-r1", "runtime-a", ["evt-1"], 7);
    var reset = await store.GetOrCreateAsync("session-a", "scene-b", "scene-r2", "runtime-b");

    Assert(reset.PendingEvents.Count == 0, "lineage reset must clear pending events");
    Assert(reset.LastKnownSequence == 0, "lineage reset must restart sequence ownership");
    Assert(reset.SceneId == "scene-b", "lineage reset must adopt request scene id");
    Assert(reset.SceneRevision == "scene-r2", "lineage reset must adopt request scene revision");
    Assert(reset.RuntimeFingerprint == "runtime-b", "lineage reset must adopt request runtime fingerprint");
}

static async Task VerifyBootstrapProjectionPreservesReplayStateAsync()
{
    var store = new BrowserSessionEventLogStore(new InMemoryBrowserKeyValueStore());
    var session = new EngineSessionEnvelope("session-bootstrap", "scene-a", "scene-r1", "runtime-a");
    await store.AppendPendingEventsAsync(
        session.SessionId,
        session.SceneId,
        session.SceneRevision,
        session.RuntimeFingerprint,
        ["evt-1", "evt-2"],
        7
    );

    var ledger = await store.GetOrCreateAsync(
        session.SessionId,
        session.SceneId,
        session.SceneRevision,
        session.RuntimeFingerprint
    );
    var projection = new PlaySessionProjection(
        new EngineSessionCursor(session, ledger.LastKnownSequence),
        ledger.PendingEvents.Count == 0
            ? ["projection ready", "local replay idle"]
            : ["projection ready", .. ledger.PendingEvents.Select(evt => $"pending:{evt}")],
        DateTimeOffset.UtcNow
    );

    Assert(projection.Cursor.AppliedThroughSequence == 7, "bootstrap projection must preserve persisted sequence ownership");
    Assert(projection.Timeline[1] == "pending:evt-1", "bootstrap projection must preserve first pending event");
    Assert(projection.Timeline[2] == "pending:evt-2", "bootstrap projection must preserve second pending event");
}

static async Task VerifyMonotonicSequenceOwnershipAsync()
{
    var store = new BrowserSessionEventLogStore(new InMemoryBrowserKeyValueStore());
    var cache = new BrowserSessionOfflineCacheService(new InMemoryBrowserKeyValueStore());
    var queue = new BrowserSessionOfflineQueueService(store, cache);

    await store.AppendPendingEventsAsync("session-b", "scene-a", "scene-r1", "runtime-a", ["evt-seed"], 10);
    var result = await queue.EnqueueAsync(
        new EngineSessionCursor(new EngineSessionEnvelope("session-b", "scene-a", "scene-r1", "runtime-a"), 2),
        "evt-next"
    );

    Assert(result.AppliedThroughSequence == 11, "enqueue sequence must be monotonic from ledger ownership");
    Assert(result.Ledger.LastKnownSequence == 11, "ledger sequence must persist monotonic result");

    var checkpoint = await cache.GetCheckpointAsync("session-b");
    Assert(checkpoint is not null && checkpoint.AppliedThroughSequence == 11, "checkpoint must track monotonic sequence");
}

static async Task VerifyConcurrentEnqueueSequenceOwnershipAsync()
{
    var store = new BrowserSessionEventLogStore(new InMemoryBrowserKeyValueStore());
    var cache = new BrowserSessionOfflineCacheService(new InMemoryBrowserKeyValueStore());
    var queue = new BrowserSessionOfflineQueueService(store, cache);
    var session = new EngineSessionEnvelope("session-concurrency", "scene-a", "scene-r1", "runtime-a");
    var cursor = new EngineSessionCursor(session, 0);
    var tasks = Enumerable.Range(1, 10)
        .Select(index => queue.EnqueueAsync(cursor, $"evt-{index}"))
        .ToArray();

    var results = await Task.WhenAll(tasks);
    var orderedSequences = results.Select(result => result.AppliedThroughSequence).OrderBy(static sequence => sequence).ToArray();
    for (var i = 0; i < orderedSequences.Length; i++)
    {
        Assert(orderedSequences[i] == i + 1, "concurrent enqueue must assign contiguous unique sequence ownership");
    }

    var ledger = await store.GetExistingAsync(session.SessionId);
    Assert(ledger is not null, "concurrent enqueue must persist a ledger");
    var persistedLedger = ledger!;
    Assert(persistedLedger.PendingEvents.Count == 10, "concurrent enqueue must persist all pending events");
    Assert(persistedLedger.LastKnownSequence == 10, "concurrent enqueue must preserve highest assigned sequence");
}

static async Task VerifySyncPrefixAcknowledgementAsync()
{
    var store = new BrowserSessionEventLogStore(new InMemoryBrowserKeyValueStore());
    var cache = new BrowserSessionOfflineCacheService(new InMemoryBrowserKeyValueStore());
    var queue = new BrowserSessionOfflineQueueService(store, cache);
    var session = new EngineSessionEnvelope("session-d", "scene-a", "scene-r1", "runtime-a");

    await store.AppendPendingEventsAsync(session.SessionId, session.SceneId, session.SceneRevision, session.RuntimeFingerprint, ["evt-1", "evt-2", "evt-3"], 3);
    var result = await queue.SyncReplayAsync(
        new PlaySyncRequest(
            new EngineSessionCursor(session, 3),
            ["evt-1", "evt-x", "evt-3"]
        )
    );

    Assert(result.AcceptedEventCount == 1, "sync acknowledgement must trim only contiguous accepted prefixes");
    Assert(result.Ledger.PendingEvents.Count == 2, "sync acknowledgement must keep non-prefix pending events");
    Assert(result.Ledger.PendingEvents[0] == "evt-2", "sync acknowledgement must preserve first unmatched event");
    Assert(result.Ledger.LastAcceptedEventCount == 1, "sync acknowledgement provenance must persist the exact trimmed prefix count");
    Assert(result.Ledger.LastSyncedAtUtc is not null, "sync acknowledgement provenance must record a sync timestamp");
}

static async Task VerifySyncPreservesNewerLedgerSequenceAsync()
{
    var store = new BrowserSessionEventLogStore(new InMemoryBrowserKeyValueStore());
    var cache = new BrowserSessionOfflineCacheService(new InMemoryBrowserKeyValueStore());
    var queue = new BrowserSessionOfflineQueueService(store, cache);
    var session = new EngineSessionEnvelope("session-sync-sequence", "scene-a", "scene-r1", "runtime-a");

    await store.AppendPendingEventsAsync(session.SessionId, session.SceneId, session.SceneRevision, session.RuntimeFingerprint, ["evt-1"], 5);
    var syncResult = await queue.SyncReplayAsync(
        new PlaySyncRequest(
            new EngineSessionCursor(session, 2),
            ["evt-1"]
        )
    );

    Assert(syncResult.AcceptedEventCount == 1, "sync must acknowledge the accepted pending event");
    Assert(syncResult.AppliedThroughSequence == 5, "sync must preserve the newer stored ledger sequence");

    var checkpoint = await cache.GetCheckpointAsync(session.SessionId);
    Assert(checkpoint is not null && checkpoint.AppliedThroughSequence == 5, "sync checkpoint must not regress below stored ledger sequence");
}

static async Task VerifyOfflineQueueRejectsMalformedPendingEventsAsync()
{
    var store = new BrowserSessionEventLogStore(new InMemoryBrowserKeyValueStore());
    var cache = new BrowserSessionOfflineCacheService(new InMemoryBrowserKeyValueStore());
    var queue = new BrowserSessionOfflineQueueService(store, cache);
    var session = new EngineSessionEnvelope("session-malformed-sync", "scene-a", "scene-r1", "runtime-a");
    var cursor = new EngineSessionCursor(session, 0);

    await AssertThrowsAsync<ArgumentException>(
        () => queue.SyncReplayAsync(new PlaySyncRequest(cursor, null!)),
        "offline queue sync must reject null pending events payloads"
    );

    await AssertThrowsAsync<ArgumentException>(
        () => queue.SyncReplayAsync(new PlaySyncRequest(cursor, ["evt-1", " "])),
        "offline queue sync must reject blank pending events"
    );
}

static async Task VerifyEventLogRejectsMalformedAppendAsync()
{
    var store = new BrowserSessionEventLogStore(new InMemoryBrowserKeyValueStore());

    await AssertThrowsAsync<ArgumentException>(
        () => store.GetOrCreateAsync("session-eventlog-invalid", "", "scene-r1", "runtime-a"),
        "event-log get/create must reject blank scene id"
    );

    await AssertThrowsAsync<ArgumentException>(
        () => store.AppendPendingEventsAsync("session-eventlog-invalid", "scene-a", "scene-r1", "runtime-a", Array.Empty<string>(), 0),
        "event-log append must reject empty pending event payloads"
    );

    await AssertThrowsAsync<ArgumentException>(
        () => store.AppendPendingEventsAsync("session-eventlog-invalid", "scene-a", "scene-r1", "runtime-a", ["evt-1", " "], 0),
        "event-log append must reject blank pending events"
    );

    await AssertThrowsAsync<ArgumentOutOfRangeException>(
        () => store.AppendPendingEventsAsync("session-eventlog-invalid", "scene-a", "scene-r1", "runtime-a", ["evt-1"], -1),
        "event-log append must reject negative sequence ownership"
    );
}

static async Task VerifyEventLogRejectsSequenceRegressionAsync()
{
    var store = new BrowserSessionEventLogStore(new InMemoryBrowserKeyValueStore());

    await store.AppendPendingEventsAsync(
        "session-eventlog-sequence-regression",
        "scene-a",
        "scene-r1",
        "runtime-a",
        ["evt-1"],
        4
    );

    await AssertThrowsAsync<InvalidOperationException>(
        () => store.AppendPendingEventsAsync(
            "session-eventlog-sequence-regression",
            "scene-a",
            "scene-r1",
            "runtime-a",
            ["evt-2"],
            3
        ),
        "event-log append must reject regressing sequence ownership for direct callers"
    );
}

static async Task VerifyEventLogPersistsAcrossServiceInstancesAsync()
{
    var browserStore = new InMemoryBrowserKeyValueStore();
    var writer = new BrowserSessionEventLogStore(browserStore);
    var reader = new BrowserSessionEventLogStore(browserStore);
    const string sessionId = "session-eventlog-persist";

    await writer.AppendPendingEventsAsync(
        sessionId,
        "scene-a",
        "scene-r1",
        "runtime-a",
        ["evt-1", "evt-2"],
        2
    );

    var persisted = await reader.GetExistingAsync(sessionId);
    Assert(persisted is not null, "event-log must persist ledger entries in browser storage");
    Assert(persisted!.PendingEvents.Count == 2, "event-log persistence must keep pending events across service instances");
    Assert(persisted.LastKnownSequence == 2, "event-log persistence must keep sequence ownership across service instances");
}

static async Task VerifyEventLogDropsMalformedStoredLedgerAsync()
{
    var browserStore = new InMemoryBrowserKeyValueStore();
    var store = new BrowserSessionEventLogStore(browserStore);
    const string sessionId = "session-eventlog-malformed-read";
    var key = PlayBrowserStateKeys.Ledger(sessionId);

    await browserStore.SetAsync(
        key,
        new OfflineLedgerEnvelope(
            sessionId,
            "scene-a",
            "scene-r1",
            "runtime-a",
            ["evt-1"],
            -1,
            DateTimeOffset.UtcNow,
            null,
            0
        )
    );

    var existing = await store.GetExistingAsync(sessionId);
    Assert(existing is null, "event-log should drop malformed stored ledger snapshots on read");

    var keys = await browserStore.ListKeysAsync("play:ledger:");
    Assert(!keys.Contains(key, StringComparer.Ordinal), "event-log should remove malformed ledger keys from browser storage");
}

static async Task VerifyEventLogDropsUnparseableStoredLedgerKeysAsync()
{
    var browserStore = new InMemoryBrowserKeyValueStore();
    var store = new BrowserSessionEventLogStore(browserStore);
    const string sessionId = "session-eventlog-unparseable-read";
    var key = PlayBrowserStateKeys.Ledger(sessionId);

    await browserStore.SetAsync<object>(key, "tampered-ledger-value");

    var existing = await store.GetExistingAsync(sessionId);
    Assert(existing is null, "event-log should treat unparseable stored ledger snapshots as missing");

    var keys = await browserStore.ListKeysAsync("play:ledger:");
    Assert(!keys.Contains(key, StringComparer.Ordinal), "event-log should remove unparseable ledger keys from browser storage");
}

static async Task VerifyOfflineQueueRejectsNegativeSequenceAsync()
{
    var store = new BrowserSessionEventLogStore(new InMemoryBrowserKeyValueStore());
    var cache = new BrowserSessionOfflineCacheService(new InMemoryBrowserKeyValueStore());
    var queue = new BrowserSessionOfflineQueueService(store, cache);
    var session = new EngineSessionEnvelope("session-negative-sequence", "scene-a", "scene-r1", "runtime-a");
    var negativeCursor = new EngineSessionCursor(session, -1);

    await AssertThrowsAsync<ArgumentOutOfRangeException>(
        () => queue.EnqueueAsync(negativeCursor, "evt-1"),
        "offline queue enqueue must reject negative applied-through sequence values"
    );

    await AssertThrowsAsync<ArgumentOutOfRangeException>(
        () => queue.SyncReplayAsync(new PlaySyncRequest(negativeCursor, ["evt-1"])),
        "offline queue sync must reject negative applied-through sequence values"
    );
}

static async Task VerifyOfflineQueueRejectsMalformedSessionEnvelopeAsync()
{
    var store = new BrowserSessionEventLogStore(new InMemoryBrowserKeyValueStore());
    var cache = new BrowserSessionOfflineCacheService(new InMemoryBrowserKeyValueStore());
    var queue = new BrowserSessionOfflineQueueService(store, cache);

    var missingSceneId = new EngineSessionCursor(
        new EngineSessionEnvelope("session-envelope-invalid", "", "scene-r1", "runtime-a"),
        0
    );
    await AssertThrowsAsync<ArgumentException>(
        () => queue.EnqueueAsync(missingSceneId, "evt-1"),
        "offline queue enqueue must reject blank scene id in direct callers"
    );

    var missingRuntime = new EngineSessionCursor(
        new EngineSessionEnvelope("session-envelope-invalid", "scene-a", "scene-r1", ""),
        0
    );
    await AssertThrowsAsync<ArgumentException>(
        () => queue.SyncReplayAsync(new PlaySyncRequest(missingRuntime, ["evt-1"])),
        "offline queue sync must reject blank runtime fingerprint in direct callers"
    );
}

static async Task VerifyOfflineCacheRejectsMalformedCheckpointAndRuntimeEntryAsync()
{
    var cache = new BrowserSessionOfflineCacheService(new InMemoryBrowserKeyValueStore());

    await AssertThrowsAsync<ArgumentException>(
        () => cache.SetCheckpointAsync(
            new SyncCheckpoint("session-cache-invalid", "scene-a", "scene-r1", "", 0, DateTimeOffset.UtcNow)
        ),
        "offline cache must reject checkpoints with blank runtime fingerprint in direct callers"
    );

    await AssertThrowsAsync<ArgumentOutOfRangeException>(
        () => cache.SetCheckpointAsync(
            new SyncCheckpoint("session-cache-invalid", "scene-a", "scene-r1", "runtime-a", -1, DateTimeOffset.UtcNow)
        ),
        "offline cache must reject checkpoints with negative sequence ownership in direct callers"
    );

    await AssertThrowsAsync<ArgumentException>(
        () => cache.CacheRuntimeBundleAsync(
            new RuntimeBundleCacheEntry(
                "session-cache-invalid",
                "runtime-a",
                "scene-r1",
                "",
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow
            )
        ),
        "offline cache must reject runtime bundle metadata with blank bundle tags in direct callers"
    );
}

static async Task VerifyOfflineCacheDropsMalformedStoredEntriesAsync()
{
    var browserStore = new InMemoryBrowserKeyValueStore();
    var cache = new BrowserSessionOfflineCacheService(browserStore);
    const string sessionId = "session-cache-malformed-read";
    var checkpointKey = PlayBrowserStateKeys.Checkpoint(sessionId);
    var runtimeBundleKey = PlayBrowserStateKeys.RuntimeBundle(sessionId);

    await browserStore.SetAsync(
        checkpointKey,
        new SyncCheckpoint(sessionId, "scene-a", "scene-r1", "runtime-a", -1, DateTimeOffset.UtcNow)
    );

    await browserStore.SetAsync(
        runtimeBundleKey,
        new RuntimeBundleCacheEntry(
            sessionId,
            "runtime-a",
            "scene-r1",
            "",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow
        )
    );

    var checkpoint = await cache.GetCheckpointAsync(sessionId);
    Assert(checkpoint is null, "offline cache should drop malformed checkpoints on read");
    var runtimeBundle = await cache.GetRuntimeBundleAsync(sessionId);
    Assert(runtimeBundle is null, "offline cache should drop malformed runtime bundle metadata on read");

    var keys = await browserStore.ListKeysAsync("play:");
    Assert(!keys.Contains(checkpointKey, StringComparer.Ordinal), "offline cache should remove malformed checkpoint keys");
    Assert(!keys.Contains(runtimeBundleKey, StringComparer.Ordinal), "offline cache should remove malformed runtime bundle keys");
}

static async Task VerifyOfflineCacheDropsUnparseableStoredEntriesAsync()
{
    var browserStore = new InMemoryBrowserKeyValueStore();
    var cache = new BrowserSessionOfflineCacheService(browserStore);
    const string sessionId = "session-cache-unparseable-read";
    var checkpointKey = PlayBrowserStateKeys.Checkpoint(sessionId);
    var runtimeBundleKey = PlayBrowserStateKeys.RuntimeBundle(sessionId);

    await browserStore.SetAsync<object>(checkpointKey, "tampered-checkpoint-value");
    await browserStore.SetAsync<object>(runtimeBundleKey, 42);

    var checkpoint = await cache.GetCheckpointAsync(sessionId);
    Assert(checkpoint is null, "offline cache should treat unparseable checkpoints as missing");

    var runtimeBundle = await cache.GetRuntimeBundleAsync(sessionId);
    Assert(runtimeBundle is null, "offline cache should treat unparseable runtime bundle metadata as missing");

    var keys = await browserStore.ListKeysAsync("play:");
    Assert(!keys.Contains(checkpointKey, StringComparer.Ordinal), "offline cache should remove unparseable checkpoint keys");
    Assert(!keys.Contains(runtimeBundleKey, StringComparer.Ordinal), "offline cache should remove unparseable runtime bundle keys");
}

static async Task VerifyOfflineCacheRuntimeBundleQuotaEvictionAsync()
{
    var cache = new BrowserSessionOfflineCacheService(new InMemoryBrowserKeyValueStore());
    var baseTime = DateTimeOffset.UtcNow.AddMinutes(-30);

    for (var i = 1; i <= 9; i++)
    {
        await cache.CacheRuntimeBundleAsync(
            new RuntimeBundleCacheEntry(
                $"session-cache-{i}",
                $"runtime-{i}",
                $"scene-r{i}",
                $"bundle-{i}",
                baseTime.AddMinutes(i),
                baseTime.AddMinutes(i)
            )
        );
    }

    var evicted = await cache.GetRuntimeBundleAsync("session-cache-1");
    Assert(evicted is null, "offline cache must evict the oldest runtime bundle entry once quota is exceeded");

    var retained = await cache.GetRuntimeBundleAsync("session-cache-9");
    Assert(retained is not null, "offline cache must retain the newest runtime bundle entry after eviction");

    var pressure = await cache.GetCachePressureAsync();
    Assert(pressure.RuntimeBundleCount == 8, "offline cache pressure must report bounded runtime bundle count");
    Assert(pressure.BackpressureActive, "offline cache pressure must report near-quota state at runtime bundle limit");
}

static async Task VerifyOfflineCacheReadsDoNotMutateQuotaEvictionAsync()
{
    var cache = new BrowserSessionOfflineCacheService(new InMemoryBrowserKeyValueStore());
    var baseTime = DateTimeOffset.UtcNow.AddMinutes(-30);

    for (var i = 1; i <= 8; i++)
    {
        await cache.CacheRuntimeBundleAsync(
            new RuntimeBundleCacheEntry(
                $"session-cache-touch-{i}",
                $"runtime-touch-{i}",
                $"scene-touch-r{i}",
                $"bundle-touch-{i}",
                baseTime.AddMinutes(i),
                baseTime.AddMinutes(i)
            )
        );
    }

    var touched = await cache.GetRuntimeBundleAsync("session-cache-touch-1");
    Assert(touched is not null, "offline cache read-touch test requires a readable runtime bundle");

    await cache.CacheRuntimeBundleAsync(
        new RuntimeBundleCacheEntry(
            "session-cache-touch-9",
            "runtime-touch-9",
            "scene-touch-r9",
            "bundle-touch-9",
            baseTime.AddMinutes(9),
            baseTime.AddMinutes(9)
        )
    );

    var touchedAfterEviction = await cache.GetRuntimeBundleAsync("session-cache-touch-1");
    Assert(touchedAfterEviction is null, "runtime-bundle reads must not rewrite cache order or shield the oldest entry from quota eviction");

    var nextOldest = await cache.GetRuntimeBundleAsync("session-cache-touch-2");
    Assert(nextOldest is not null, "quota eviction must preserve newer entries when reads do not mutate stored runtime metadata");
}

static async Task VerifyOfflineCacheReadDoesNotRewriteStoredMetadataAsync()
{
    var browserStore = new InMemoryBrowserKeyValueStore();
    var cache = new BrowserSessionOfflineCacheService(browserStore);
    const string sessionId = "session-cache-touch-no-write";
    var baseTime = DateTimeOffset.UtcNow.AddMinutes(-5);
    var key = PlayBrowserStateKeys.RuntimeBundle(sessionId);
    var initialTimestamp = baseTime;

    await cache.CacheRuntimeBundleAsync(
        new RuntimeBundleCacheEntry(
            sessionId,
            "runtime-initial",
            "scene-r1",
            "bundle-initial",
            initialTimestamp,
            initialTimestamp
        )
    );

    var readResult = await cache.GetRuntimeBundleAsync(sessionId);
    Assert(readResult is not null, "runtime bundle read regression requires a readable entry");
    Assert(readResult.LastValidatedAtUtc >= initialTimestamp, "runtime bundle reads should still surface a fresh validation timestamp to callers");

    var storedEntry = await browserStore.GetAsync<RuntimeBundleCacheEntry>(key);
    Assert(storedEntry is not null, "runtime bundle entry must remain stored after read");
    Assert(storedEntry.RuntimeFingerprint == "runtime-initial", "runtime bundle reads must not rewrite stored runtime fingerprint metadata");
    Assert(storedEntry.SceneRevision == "scene-r1", "runtime bundle reads must not rewrite stored scene revision metadata");
    Assert(storedEntry.BundleTag == "bundle-initial", "runtime bundle reads must not rewrite stored bundle tag metadata");
    Assert(storedEntry.LastValidatedAtUtc == initialTimestamp, "runtime bundle reads must not persist a write-on-read timestamp update");
}

static async Task VerifyOfflineCacheConcurrentReadAndWritePreserveNewestSameSessionMetadataAsync()
{
    const string sessionId = "session-cache-touch-same-session";
    var key = PlayBrowserStateKeys.RuntimeBundle(sessionId);
    var gate = new GateableGetBrowserStore(key);
    var cache = new BrowserSessionOfflineCacheService(gate);
    var initialTimestamp = DateTimeOffset.UtcNow.AddMinutes(-10);

    await cache.CacheRuntimeBundleAsync(
        new RuntimeBundleCacheEntry(
            sessionId,
            "runtime-initial",
            "scene-r1",
            "bundle-initial",
            initialTimestamp,
            initialTimestamp
        )
    );
    gate.EnableGate();
    TraceCheckBoundary("STEP", $"{nameof(VerifyOfflineCacheConcurrentReadAndWritePreserveNewestSameSessionMetadataAsync)} seeded initial entry");

    var readTask = Task.Run(() => cache.GetRuntimeBundleAsync(sessionId));
    TraceCheckBoundary("STEP", $"{nameof(VerifyOfflineCacheConcurrentReadAndWritePreserveNewestSameSessionMetadataAsync)} waiting for gated read");
    await gate.WaitForGetAsync();
    TraceCheckBoundary("STEP", $"{nameof(VerifyOfflineCacheConcurrentReadAndWritePreserveNewestSameSessionMetadataAsync)} gated read started");

    var updatedTimestamp = initialTimestamp.AddMinutes(5);
    var writeTask = Task.Run(() => cache.CacheRuntimeBundleAsync(
        new RuntimeBundleCacheEntry(
            sessionId,
            "runtime-updated",
            "scene-r2",
            "bundle-updated",
            updatedTimestamp,
            updatedTimestamp
        )
    ));
    TraceCheckBoundary("STEP", $"{nameof(VerifyOfflineCacheConcurrentReadAndWritePreserveNewestSameSessionMetadataAsync)} queued concurrent write");

    gate.ReleaseGet();
    TraceCheckBoundary("STEP", $"{nameof(VerifyOfflineCacheConcurrentReadAndWritePreserveNewestSameSessionMetadataAsync)} released gated read");
    var readResult = await readTask;
    TraceCheckBoundary("STEP", $"{nameof(VerifyOfflineCacheConcurrentReadAndWritePreserveNewestSameSessionMetadataAsync)} read completed");
    await writeTask;
    TraceCheckBoundary("STEP", $"{nameof(VerifyOfflineCacheConcurrentReadAndWritePreserveNewestSameSessionMetadataAsync)} write completed");

    Assert(readResult is not null, "same-session interleaving regression requires the read side to complete");
    var storedEntry = await gate.GetAsync<RuntimeBundleCacheEntry>(key);
    Assert(storedEntry is not null, "same-session interleaving regression requires the runtime bundle entry to remain stored");
    Assert(storedEntry.RuntimeFingerprint == "runtime-updated", "same-session read/write interleaving must preserve the newest runtime fingerprint");
    Assert(storedEntry.SceneRevision == "scene-r2", "same-session read/write interleaving must preserve the newest scene revision");
    Assert(storedEntry.BundleTag == "bundle-updated", "same-session read/write interleaving must preserve the newest bundle tag");
    Assert(storedEntry.CachedAtUtc == updatedTimestamp, "same-session read/write interleaving must preserve the newest cache timestamp");
    Assert(storedEntry.LastValidatedAtUtc == updatedTimestamp, "same-session read/write interleaving must not let the read path clobber stored validation time");
}

static async Task VerifyOfflineCacheQuotaIgnoresUnparseableRuntimeBundleKeysAsync()
{
    var browserStore = new InMemoryBrowserKeyValueStore();
    var cache = new BrowserSessionOfflineCacheService(browserStore);
    var baseTime = DateTimeOffset.UtcNow.AddMinutes(-60);

    var malformedKeyA = PlayBrowserStateKeys.RuntimeBundle("session-cache-malformed-a");
    var malformedKeyB = PlayBrowserStateKeys.RuntimeBundle("session-cache-malformed-b");
    await browserStore.SetAsync<object>(malformedKeyA, "tampered-runtime-a");
    await browserStore.SetAsync<object>(malformedKeyB, "tampered-runtime-b");

    for (var i = 1; i <= 8; i++)
    {
        await cache.CacheRuntimeBundleAsync(
            new RuntimeBundleCacheEntry(
                $"session-cache-clean-{i}",
                $"runtime-clean-{i}",
                $"scene-clean-r{i}",
                $"bundle-clean-{i}",
                baseTime.AddMinutes(i),
                baseTime.AddMinutes(i)
            )
        );
    }

    var oldestValid = await cache.GetRuntimeBundleAsync("session-cache-clean-1");
    Assert(oldestValid is not null, "quota should not evict valid runtime bundles because of unparseable key residue");

    var keys = await browserStore.ListKeysAsync(PlayBrowserStateKeys.RuntimeBundlePrefix);
    Assert(keys.Count == 8, "runtime-bundle keyspace should contain only bounded valid entries after pruning unparseable keys");
    Assert(!keys.Contains(malformedKeyA, StringComparer.Ordinal), "runtime-bundle cache should prune unparseable keys before quota accounting");
    Assert(!keys.Contains(malformedKeyB, StringComparer.Ordinal), "runtime-bundle cache should prune all unparseable keys before quota accounting");
}

static async Task VerifyOfflineCacheConcurrentCrossSessionQuotaWritesStayBoundedAsync()
{
    var browserStore = new DelayedMutationBrowserStore(TimeSpan.FromMilliseconds(25));
    var cache = new BrowserSessionOfflineCacheService(browserStore);
    var baseTime = DateTimeOffset.UtcNow.AddMinutes(-90);
    var releaseWriters = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var readyCount = 0;

    for (var i = 1; i <= 8; i++)
    {
        await cache.CacheRuntimeBundleAsync(
            new RuntimeBundleCacheEntry(
                $"session-cache-concurrent-{i}",
                $"runtime-concurrent-{i}",
                $"scene-concurrent-r{i}",
                $"bundle-concurrent-{i}",
                baseTime.AddMinutes(i),
                baseTime.AddMinutes(i)
            )
        );
    }

    Task writeNine = Task.Run(async () =>
    {
        if (Interlocked.Increment(ref readyCount) == 2)
        {
            releaseWriters.TrySetResult();
        }

        await releaseWriters.Task;
        await cache.CacheRuntimeBundleAsync(
            new RuntimeBundleCacheEntry(
                "session-cache-concurrent-9",
                "runtime-concurrent-9",
                "scene-concurrent-r9",
                "bundle-concurrent-9",
                baseTime.AddMinutes(9),
                baseTime.AddMinutes(9)
            )
        );
    });

    Task writeTen = Task.Run(async () =>
    {
        if (Interlocked.Increment(ref readyCount) == 2)
        {
            releaseWriters.TrySetResult();
        }

        await releaseWriters.Task;
        await cache.CacheRuntimeBundleAsync(
            new RuntimeBundleCacheEntry(
                "session-cache-concurrent-10",
                "runtime-concurrent-10",
                "scene-concurrent-r10",
                "bundle-concurrent-10",
                baseTime.AddMinutes(10),
                baseTime.AddMinutes(10)
            )
        );
    });

    await Task.WhenAll(writeNine, writeTen);

    var keys = await browserStore.ListKeysAsync(PlayBrowserStateKeys.RuntimeBundlePrefix);
    Assert(keys.Count == 8, "concurrent cross-session quota writes must preserve the global runtime-bundle quota");
    Assert(await cache.GetRuntimeBundleAsync("session-cache-concurrent-1") is null, "first oldest runtime bundle must be evicted under concurrent quota pressure");
    Assert(await cache.GetRuntimeBundleAsync("session-cache-concurrent-2") is null, "second oldest runtime bundle must be evicted under concurrent quota pressure");
    Assert(await cache.GetRuntimeBundleAsync("session-cache-concurrent-9") is not null, "first concurrent write must survive quota reconciliation");
    Assert(await cache.GetRuntimeBundleAsync("session-cache-concurrent-10") is not null, "second concurrent write must survive quota reconciliation");
}

static async Task VerifyIndexShellAccessibilityContractAsync()
{
    var indexHtmlPath = Path.Combine(GetRepoRoot(), "src", "Chummer.Play.Web", "wwwroot", "index.html");
    var html = await File.ReadAllTextAsync(indexHtmlPath);

    Assert(html.Contains("<html lang=\"en\">", StringComparison.Ordinal), "play shell must declare an explicit document language");
    Assert(html.Contains("<main>", StringComparison.Ordinal), "play shell must expose a main landmark");
    Assert(html.Contains("<h1>Chummer Play</h1>", StringComparison.Ordinal), "play shell must expose a top-level heading");
    Assert(html.Contains("id=\"output\" role=\"status\" aria-live=\"polite\" aria-atomic=\"true\"", StringComparison.Ordinal), "play shell resume status region must expose polite live updates");
}

static Task VerifyBootstrapRoleShellEntryPointsAsync()
{
    var playerCapabilities = PlayRouteHandlers.ResolveRoleCapabilities(
        PlayRouteHandlers.ToSnapshot(PlayerShellModule.CreateDescriptor())
    );
    var gmCapabilities = PlayRouteHandlers.ResolveRoleCapabilities(
        PlayRouteHandlers.ToSnapshot(GmTacticalShellModule.CreateDescriptor())
    );
    var playerActions = PlayRouteHandlers.BuildQuickActions(PlaySurfaceRole.Player, playerCapabilities);
    var gmActions = PlayRouteHandlers.BuildQuickActions(PlaySurfaceRole.GameMaster, gmCapabilities);

    Assert(playerActions.Count > 0, "player bootstrap entry points must expose quick actions");
    Assert(gmActions.Count > 0, "gm bootstrap entry points must expose quick actions");
    Assert(playerActions.All(action => !action.ActionId.StartsWith("gm-", StringComparison.Ordinal)), "player role entry points must not expose gm quick actions");
    Assert(gmActions.All(action => !action.ActionId.StartsWith("player-", StringComparison.Ordinal)), "gm role entry points must not expose player quick actions");
    Assert(playerActions.All(action => !string.IsNullOrWhiteSpace(action.Label)), "player role entry points must expose non-empty action labels");
    Assert(gmActions.All(action => !string.IsNullOrWhiteSpace(action.Label)), "gm role entry points must expose non-empty action labels");
    return Task.CompletedTask;
}

static async Task VerifyQuickActionRejectsCrossRoleAuthorizationAsync()
{
    const string sessionId = "session-role-gate";
    var store = new BrowserSessionEventLogStore(new InMemoryBrowserKeyValueStore());
    var cache = new BrowserSessionOfflineCacheService(new InMemoryBrowserKeyValueStore());
    var queue = new BrowserSessionOfflineQueueService(store, cache);
    var session = new EngineSessionEnvelope(sessionId, "scene-role", "scene-r2", "runtime-role");
    await store.AppendPendingEventsAsync(
        session.SessionId,
        session.SceneId,
        session.SceneRevision,
        session.RuntimeFingerprint,
        ["evt-existing"],
        5
    );
    await cache.SetCheckpointAsync(
        new SyncCheckpoint(
            session.SessionId,
            session.SceneId,
            session.SceneRevision,
            session.RuntimeFingerprint,
            5,
            DateTimeOffset.UtcNow
        )
    );

    var response = await ExecuteResultAsync<PlayQuickActionResponse>(
        await PlayRouteHandlers.HandleQuickActionAsync(
            new PlayQuickActionRequest(new EngineSessionCursor(session, 5), PlaySurfaceRole.Player, "gm-advance-initiative"),
            cache,
            store,
            queue,
            PlayerShellModule.CreateDescriptor(),
            GmTacticalShellModule.CreateDescriptor(),
            CancellationToken.None
        )
    );

    Assert(!response.Accepted, "cross-role quick action must be denied");
    Assert(!response.Stale, "cross-role quick action denial must be authorization-based, not stale-lineage");
    Assert(response.Reason == "action not permitted for role capabilities", "cross-role quick action denial must return deterministic reason");
    Assert(response.Projection.Cursor.AppliedThroughSequence == 5, "cross-role quick action denial must preserve sequence ownership");
    Assert(response.Projection.Cursor.Session.SceneId == "scene-role", "cross-role quick action denial must preserve lineage");

    var ledger = await store.GetExistingAsync(sessionId);
    Assert(ledger is not null, "cross-role quick action denial must preserve existing ledger");
    Assert(ledger!.LastKnownSequence == 5, "cross-role quick action denial must not mutate sequence ownership");
    Assert(ledger.PendingEvents.SequenceEqual(["evt-existing"]), "cross-role quick action denial must not mutate pending events");
}

static async Task VerifyCachePressureBudgetContractAsync()
{
    var cache = new BrowserSessionOfflineCacheService(new InMemoryBrowserKeyValueStore());
    var baseTime = DateTimeOffset.UtcNow.AddMinutes(-30);

    for (var i = 1; i <= 9; i++)
    {
        await cache.CacheRuntimeBundleAsync(
            new RuntimeBundleCacheEntry(
                $"session-budget-{i}",
                $"runtime-budget-{i}",
                $"scene-budget-r{i}",
                $"bundle-budget-{i}",
                baseTime.AddMinutes(i),
                baseTime.AddMinutes(i)
            )
        );
    }

    var pressure = await cache.GetCachePressureAsync();
    Assert(pressure.RuntimeBundleCount == pressure.RuntimeBundleQuota, "runtime-bundle cache pressure must stay bounded at quota");
    Assert(pressure.RuntimeBundleQuota == 8, "runtime-bundle cache pressure must preserve the M10 quota budget");
    Assert(pressure.BackpressureActive, "runtime-bundle cache pressure must report backpressure when at budget");
}

static async Task VerifyOfflineQueueRejectsStaleLineageAsync()
{
    var storeState = new InMemoryBrowserKeyValueStore();
    var store = new BrowserSessionEventLogStore(storeState);
    var cache = new BrowserSessionOfflineCacheService(storeState);
    var queue = new BrowserSessionOfflineQueueService(store, cache);
    const string sessionId = "session-stale-queue";

    await store.AppendPendingEventsAsync(sessionId, "scene-stored", "scene-r2", "runtime-stored", ["evt-1"], 4);

    var staleCursor = new EngineSessionCursor(
        new EngineSessionEnvelope(sessionId, "scene-request", "scene-r1", "runtime-request"),
        4
    );

    await AssertThrowsAsync<InvalidOperationException>(
        () => queue.EnqueueAsync(staleCursor, "evt-new"),
        "offline queue enqueue must reject stale lineage before mutating stored replay state"
    );

    await AssertThrowsAsync<InvalidOperationException>(
        () => queue.SyncReplayAsync(new PlaySyncRequest(staleCursor, ["evt-1"])),
        "offline queue sync must reject stale lineage before acknowledging stored replay state"
    );
}

static void VerifyCursorValidationRejectsNegativeSequence()
{
    var valid = PlayRouteHandlers.TryValidateCursor(
        new EngineSessionCursor(new EngineSessionEnvelope("session-valid", "scene-a", "scene-r1", "runtime-a"), 0),
        out var validError
    );
    Assert(valid, $"cursor validation should accept non-negative sequence: {validError}");

    var invalid = PlayRouteHandlers.TryValidateCursor(
        new EngineSessionCursor(new EngineSessionEnvelope("session-negative", "scene-a", "scene-r1", "runtime-a"), -1),
        out var invalidError
    );
    Assert(!invalid, "cursor validation must reject negative sequence values");
    Assert(
        string.Equals(invalidError, "applied through sequence cannot be negative.", StringComparison.Ordinal),
        "cursor validation must return a deterministic negative-sequence error"
    );
}

static async Task VerifyReconnectLineageTransitionContinuityAsync()
{
    var store = new BrowserSessionEventLogStore(new InMemoryBrowserKeyValueStore());
    var cache = new BrowserSessionOfflineCacheService(new InMemoryBrowserKeyValueStore());
    var queue = new BrowserSessionOfflineQueueService(store, cache);
    const string sessionId = "session-reconnect";

    await store.AppendPendingEventsAsync(sessionId, "scene-a", "scene-r1", "runtime-a", ["evt-old"], 4);
    await cache.SetCheckpointAsync(new SyncCheckpoint(sessionId, "scene-a", "scene-r1", "runtime-a", 4, DateTimeOffset.UtcNow));

    var reconnectCursor = new EngineSessionCursor(
        new EngineSessionEnvelope(sessionId, "scene-b", "scene-r2", "runtime-b"),
        0
    );
    var ledger = await store.GetOrCreateAsync(
        reconnectCursor.Session.SessionId,
        reconnectCursor.Session.SceneId,
        reconnectCursor.Session.SceneRevision,
        reconnectCursor.Session.RuntimeFingerprint
    );
    var effectiveSession = new EngineSessionEnvelope(
        ledger.SessionId,
        ledger.SceneId,
        ledger.SceneRevision,
        ledger.RuntimeFingerprint
    );
    var existingCheckpoint = await cache.GetCheckpointAsync(sessionId);
    var appliedThroughSequence = Math.Max(reconnectCursor.AppliedThroughSequence, ledger.LastKnownSequence);
    var reconnectCheckpoint = existingCheckpoint is not null
        && SessionLineage.IsCheckpointAligned(existingCheckpoint, effectiveSession)
            ? existingCheckpoint with
            {
                AppliedThroughSequence = appliedThroughSequence,
                CapturedAtUtc = DateTimeOffset.UtcNow,
            }
            : new SyncCheckpoint(
                effectiveSession.SessionId,
                effectiveSession.SceneId,
                effectiveSession.SceneRevision,
                effectiveSession.RuntimeFingerprint,
                appliedThroughSequence,
                DateTimeOffset.UtcNow
            );
    await cache.SetCheckpointAsync(reconnectCheckpoint);

    var storedLedger = await store.GetExistingAsync(sessionId);
    Assert(
        SessionLineage.IsStoredLineageAligned(effectiveSession, reconnectCheckpoint, storedLedger),
        "reconnect must realign checkpoint and ledger lineage before sync/quick-action stale checks"
    );

    var enqueueResult = await queue.EnqueueAsync(new EngineSessionCursor(effectiveSession, reconnectCheckpoint.AppliedThroughSequence), "evt-new");
    var syncResult = await queue.SyncReplayAsync(
        new PlaySyncRequest(new EngineSessionCursor(effectiveSession, enqueueResult.AppliedThroughSequence), ["evt-new"])
    );
    Assert(syncResult.AcceptedEventCount == 1, "sync should continue on the new lineage after reconnect realignment");
}

static async Task VerifyStoredLineageStaleResponsesAsync()
{
    const string sessionId = "session-stale";
    var storedSession = new EngineSessionEnvelope(sessionId, "scene-stored", "scene-r2", "runtime-stored");
    var requestSession = new EngineSessionEnvelope(sessionId, "scene-request", "scene-r1", "runtime-request");
    var app = PlayWebApplication.Build([]);

    try
    {
        var store = app.Services.GetRequiredService<BrowserSessionEventLogStore>();
        await store.AppendPendingEventsAsync(
            storedSession.SessionId,
            storedSession.SceneId,
            storedSession.SceneRevision,
            storedSession.RuntimeFingerprint,
            ["evt-stored"],
            4
        );

        var existingLedger = await store.GetExistingAsync(sessionId);
        Assert(existingLedger is not null, "stored-ledger stale regression must have an existing ledger");
        Assert(
            !SessionLineage.IsStoredLineageAligned(requestSession, checkpoint: null, existingLedger),
            "mismatched request lineage must be rejected when only stored ledger lineage exists"
        );
        var offlineCacheService = app.Services.GetRequiredService<BrowserSessionOfflineCacheService>();
        var offlineQueueService = app.Services.GetRequiredService<BrowserSessionOfflineQueueService>();
        var cursor = new EngineSessionCursor(requestSession, 4);
        var quickActionResponse = await ExecuteResultAsync<PlayQuickActionResponse>(
            await PlayRouteHandlers.HandleQuickActionAsync(
                new PlayQuickActionRequest(cursor, PlaySurfaceRole.Player, "player-mark-ready"),
                offlineCacheService,
                store,
                offlineQueueService,
                PlayerShellModule.CreateDescriptor(),
                GmTacticalShellModule.CreateDescriptor(),
                CancellationToken.None
            )
        );
        Assert(!quickActionResponse.Accepted, "stale quick action must be rejected");
        Assert(quickActionResponse.Stale, "stale quick action must be marked stale");
        Assert(
            quickActionResponse.Projection.Cursor.Session.SceneId == storedSession.SceneId,
            "stale quick action projection must prefer stored ledger scene id"
        );
        Assert(
            quickActionResponse.Checkpoint?.ProjectionFingerprint == storedSession.RuntimeFingerprint,
            "stale quick action checkpoint must prefer stored ledger runtime fingerprint"
        );
        Assert(
            quickActionResponse.Projection.Cursor.AppliedThroughSequence == 4,
            "stale quick action projection must prefer stored ledger sequence ownership when checkpoint is missing"
        );
        Assert(
            quickActionResponse.Projection.Timeline.Contains("pending:evt-stored", StringComparer.Ordinal),
            "stale quick action projection must preserve stored pending events when checkpoint is missing"
        );

        var syncResponse = await ExecuteResultAsync<PlaySyncResponse>(
            await PlayRouteHandlers.HandleSyncAsync(
                new PlaySyncRequest(cursor, ["evt-client"]),
                offlineCacheService,
                store,
                offlineQueueService,
                CancellationToken.None
            )
        );
        Assert(!syncResponse.Accepted, "stale sync must be rejected");
        Assert(syncResponse.Stale, "stale sync must be marked stale");
        Assert(
            syncResponse.Projection.Cursor.Session.SceneRevision == storedSession.SceneRevision,
            "stale sync projection must prefer stored ledger scene revision"
        );
        Assert(
            syncResponse.Checkpoint?.SceneId == storedSession.SceneId,
            "stale sync checkpoint must prefer stored ledger scene id"
        );
        Assert(
            syncResponse.Projection.Cursor.AppliedThroughSequence == 4,
            "stale sync projection must prefer stored ledger sequence ownership when checkpoint is missing"
        );
        Assert(
            syncResponse.Projection.Timeline.Contains("pending:evt-stored", StringComparer.Ordinal),
            "stale sync projection must preserve stored pending events when checkpoint is missing"
        );

        var recoveredResult = await PlayRouteHandlers.HandleSyncAsync(
            new PlaySyncRequest(syncResponse.Projection.Cursor, ["evt-stored"]),
            offlineCacheService,
            store,
            offlineQueueService,
            CancellationToken.None
        );
        var recoveredSync = await ExecuteResultAsync<PlaySyncResponse>(recoveredResult);
        Assert(recoveredSync.Accepted, "retrying sync with the stored stale checkpoint lineage must recover");
        Assert(!recoveredSync.Stale, "retrying sync with the stored stale checkpoint lineage must not stay stale");
    }
    finally
    {
        await app.DisposeAsync();
    }
}

static async Task VerifyReconnectRejectsStaleLineageWithoutMutationAsync()
{
    const string sessionId = "session-reconnect-stale";
    var app = PlayWebApplication.Build([]);

    try
    {
        var store = app.Services.GetRequiredService<BrowserSessionEventLogStore>();
        var cache = app.Services.GetRequiredService<BrowserSessionOfflineCacheService>();
        await store.AppendPendingEventsAsync(sessionId, "scene-stored", "scene-r4", "runtime-stored", ["evt-stored"], 8);
        await cache.SetCheckpointAsync(
            new SyncCheckpoint(sessionId, "scene-stored", "scene-r4", "runtime-stored", 8, DateTimeOffset.UtcNow)
        );

        var staleResult = await PlayRouteHandlers.HandleReconnectAsync(
            new PlayReconnectRequest(
                new EngineSessionCursor(
                    new EngineSessionEnvelope(sessionId, "scene-request", "scene-r1", "runtime-request"),
                    3
                )
            ),
            store,
            cache,
            CancellationToken.None
        );
        var staleResponse = await ExecuteResultWithStatusAsync<ReconnectConflictPayload>(
            staleResult,
            StatusCodes.Status409Conflict
        );

        Assert(staleResponse.Stale, "stale reconnect must return stale conflict metadata");
        Assert(staleResponse.Error == "session lineage changed", "stale reconnect must return lineage conflict reason");
        Assert(staleResponse.Projection.Cursor.Session.SceneId == "scene-stored", "stale reconnect conflict must preserve stored scene lineage");
        Assert(staleResponse.Projection.Cursor.AppliedThroughSequence == 8, "stale reconnect conflict must preserve stored sequence ownership");
        Assert(staleResponse.Projection.Timeline.Contains("pending:evt-stored", StringComparer.Ordinal), "stale reconnect conflict must preserve stored pending events");

        var ledgerAfterConflict = await store.GetExistingAsync(sessionId);
        Assert(ledgerAfterConflict is not null, "stale reconnect must keep existing ledger");
        Assert(ledgerAfterConflict!.SceneId == "scene-stored", "stale reconnect must not reset ledger scene");
        Assert(ledgerAfterConflict.LastKnownSequence == 8, "stale reconnect must not reset ledger sequence ownership");
        Assert(ledgerAfterConflict.PendingEvents.Count == 1 && ledgerAfterConflict.PendingEvents[0] == "evt-stored", "stale reconnect must not drop pending events");

        var checkpointAfterConflict = await cache.GetCheckpointAsync(sessionId);
        Assert(checkpointAfterConflict is not null, "stale reconnect must preserve existing checkpoint");
        Assert(checkpointAfterConflict!.SceneId == "scene-stored", "stale reconnect must not mutate checkpoint lineage");
        Assert(checkpointAfterConflict.AppliedThroughSequence == 8, "stale reconnect must not mutate checkpoint sequence ownership");
    }
    finally
    {
        await app.DisposeAsync();
    }
}

static async Task VerifyReconnectClientThrowsTypedStaleAsync()
{
    var projection = new PlaySessionProjection(
        new EngineSessionCursor(
            new EngineSessionEnvelope("session-reconnect-client-stale", "scene-stored", "scene-r7", "runtime-stored"),
            12
        ),
        ["projection ready", "pending:evt-stored"],
        DateTimeOffset.UtcNow
    );
    var checkpoint = new SyncCheckpoint(
        "session-reconnect-client-stale",
        "scene-stored",
        "scene-r7",
        "runtime-stored",
        12,
        DateTimeOffset.UtcNow
    );
    var payload = new ReconnectConflictPayload("session lineage changed", true, projection, checkpoint);
    var apiClient = CreateApiClient(
        new StubHttpMessageHandler((request, _) =>
        {
            Assert(request.Method == HttpMethod.Post, "reconnect client must issue POST requests");
            Assert(
                string.Equals(request.RequestUri?.AbsolutePath, PlayApiRoutes.Reconnect, StringComparison.Ordinal),
                "reconnect client must call the reconnect route"
            );

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            var response = new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
            return Task.FromResult(response);
        })
    );

    PlayReconnectStaleException? staleException = null;
    try
    {
        await apiClient.ReconnectAsync(
            new PlayReconnectRequest(
                new EngineSessionCursor(
                    new EngineSessionEnvelope("session-reconnect-client-stale", "scene-request", "scene-r1", "runtime-request"),
                    1
                )
            )
        );
    }
    catch (PlayReconnectStaleException ex)
    {
        staleException = ex;
    }

    Assert(staleException is not null, "reconnect client must throw typed stale exception on 409 conflict");
    Assert(staleException!.Projection.Cursor.Session.SceneId == "scene-stored", "typed stale reconnect exception must include stored projection lineage");
    Assert(staleException.Projection.Cursor.AppliedThroughSequence == 12, "typed stale reconnect exception must include stored projection sequence ownership");
    Assert(staleException.Checkpoint.SceneRevision == "scene-r7", "typed stale reconnect exception must include stored checkpoint lineage");
    Assert(staleException.Checkpoint.AppliedThroughSequence == 12, "typed stale reconnect exception must include stored checkpoint sequence ownership");
}

static async Task VerifyContinuityClaimRejectsStaleLineageWithoutMutationAsync()
{
    const string sessionId = "session-continuity-stale";
    var app = PlayWebApplication.Build([]);

    try
    {
        var store = app.Services.GetRequiredService<BrowserSessionEventLogStore>();
        var cache = app.Services.GetRequiredService<BrowserSessionOfflineCacheService>();
        await store.AppendPendingEventsAsync(sessionId, "scene-stored", "scene-r7", "runtime-stored", ["evt-stored"], 6);
        await cache.SetCheckpointAsync(
            new SyncCheckpoint(sessionId, "scene-stored", "scene-r7", "runtime-stored", 6, DateTimeOffset.UtcNow)
        );

        var staleResultResponse = await PlayRouteHandlers.HandleContinuityClaimAsync(
            new PlayContinuityClaimRequest(
                new EngineSessionCursor(
                    new EngineSessionEnvelope(sessionId, "scene-request", "scene-r1", "runtime-request"),
                    2
                ),
                PlaySurfaceRole.Player,
                "device-request",
                "observer-request"
            ),
            store,
            cache,
            app.Services.GetRequiredService<IBrowserKeyValueStore>(),
            CancellationToken.None
        );
        var staleResult = await ExecuteResultAsync<PlayContinuityClaimResponse>(staleResultResponse);

        Assert(!staleResult.Accepted, "stale continuity claim must not be accepted");
        Assert(staleResult.Stale, "stale continuity claim must mark stale lineage");
        Assert(staleResult.Reason == "session lineage changed", "stale continuity claim must return stale reason");
        Assert(staleResult.Projection.Cursor.Session.SceneId == "scene-stored", "stale continuity claim must preserve stored scene lineage");
        Assert(staleResult.Checkpoint.SceneRevision == "scene-r7", "stale continuity claim must preserve stored checkpoint lineage");

        var ledgerAfterClaim = await store.GetExistingAsync(sessionId);
        Assert(ledgerAfterClaim is not null, "stale continuity claim must keep existing ledger");
        Assert(ledgerAfterClaim!.SceneId == "scene-stored", "stale continuity claim must not mutate stored ledger scene");
        Assert(ledgerAfterClaim.LastKnownSequence == 6, "stale continuity claim must not mutate stored sequence ownership");
    }
    finally
    {
        await app.DisposeAsync();
    }
}

static async Task VerifyContinuityClaimRejectsUnknownSessionWithoutMutationAsync()
{
    const string sessionId = "session-continuity-unknown";
    var app = PlayWebApplication.Build([]);

    try
    {
        var store = app.Services.GetRequiredService<BrowserSessionEventLogStore>();
        var cache = app.Services.GetRequiredService<BrowserSessionOfflineCacheService>();
        var browserStore = app.Services.GetRequiredService<IBrowserKeyValueStore>();

        var resultResponse = await PlayRouteHandlers.HandleContinuityClaimAsync(
            new PlayContinuityClaimRequest(
                new EngineSessionCursor(
                    new EngineSessionEnvelope(sessionId, "scene-unknown", "scene-r1", "runtime-unknown"),
                    3
                ),
                PlaySurfaceRole.Player,
                "device-unknown",
                "observer-unknown"
            ),
            store,
            cache,
            browserStore,
            CancellationToken.None
        );
        var result = await ExecuteResultAsync<PlayContinuityClaimResponse>(resultResponse);

        Assert(!result.Accepted, "unknown-session continuity claim must not be accepted");
        Assert(!result.Stale, "unknown-session continuity claim must not report stale lineage");
        Assert(result.Reason == "session not bootstrapped", "unknown-session continuity claim must require trusted bootstrap lineage");

        var ledgerAfterClaim = await store.GetExistingAsync(sessionId);
        Assert(ledgerAfterClaim is null, "unknown-session continuity claim must not create ledger state");

        var checkpointAfterClaim = await cache.GetCheckpointAsync(sessionId);
        Assert(checkpointAfterClaim is null, "unknown-session continuity claim must not create checkpoint state");

        var continuityAfterClaim = await browserStore.GetAsync<ObserverContinuityEntry>(PlayBrowserStateKeys.Continuity(sessionId));
        Assert(continuityAfterClaim is null, "unknown-session continuity claim must not create continuity state");
    }
    finally
    {
        await app.DisposeAsync();
    }
}

static async Task VerifyObserveReturnsLineageSafeContinuityAsync()
{
    const string sessionId = "session-observe-continuity";
    var app = PlayWebApplication.Build([]);

    try
    {
        var store = app.Services.GetRequiredService<BrowserSessionEventLogStore>();
        var cache = app.Services.GetRequiredService<BrowserSessionOfflineCacheService>();
        var browserStore = app.Services.GetRequiredService<IBrowserKeyValueStore>();
        await store.AppendPendingEventsAsync(sessionId, "scene-observe", "scene-r9", "runtime-observe", ["evt-pending"], 9);
        await cache.SetCheckpointAsync(
            new SyncCheckpoint(sessionId, "scene-observe", "scene-r9", "runtime-observe", 9, DateTimeOffset.UtcNow)
        );
        await cache.CacheRuntimeBundleAsync(sessionId, "runtime-observe", "scene-r9", "bundle:scene-observe:runtime-observe");

        var claimResult = await PlayRouteHandlers.HandleContinuityClaimAsync(
            new PlayContinuityClaimRequest(
                new EngineSessionCursor(
                    new EngineSessionEnvelope(sessionId, "scene-observe", "scene-r9", "runtime-observe"),
                    9
                ),
                PlaySurfaceRole.GameMaster,
                "device-gm-a",
                "observer-gm-a"
            ),
            store,
            cache,
            browserStore,
            CancellationToken.None
        );
        var continuityClaim = await ExecuteResultAsync<PlayContinuityClaimResponse>(claimResult);
        Assert(continuityClaim.Accepted, "continuity claim must be accepted for aligned lineage");
        Assert(!continuityClaim.Stale, "continuity claim must not be stale for aligned lineage");

        var observeResult = await PlayRouteHandlers.HandleObserveAsync(
            sessionId,
            store,
            cache,
            browserStore,
            CancellationToken.None
        );
        var observe = await ExecuteResultAsync<PlayObserveResponse>(observeResult);
        Assert(observe.Continuity is not null, "observe must return continuity metadata after accepted claim");
        Assert(observe.Continuity!.ObserverId == "observer-gm-a", "observe must return stored observer id");
        Assert(observe.Continuity.DeviceId == "device-gm-a", "observe must return stored device id");
        Assert(observe.Continuity.ObservedThroughSequence == 9, "observe must preserve continuity sequence ownership");
        Assert(observe.RuntimeBundle is not null, "observe must return runtime bundle metadata for cross-device resume");
        Assert(observe.Projection.Timeline.Contains("pending:evt-pending", StringComparer.Ordinal), "observe must preserve stored pending replay events");

        await browserStore.SetAsync(
            PlayBrowserStateKeys.Continuity(sessionId),
            new ObserverContinuityEntry(
                sessionId,
                "scene-old",
                "scene-r1",
                "runtime-old",
                "observer-old",
                "device-old",
                PlaySurfaceRole.Player,
                12,
                DateTimeOffset.UtcNow,
                "old-token"
            )
        );

        var observeAfterTamperResult = await PlayRouteHandlers.HandleObserveAsync(
            sessionId,
            store,
            cache,
            browserStore,
            CancellationToken.None
        );
        var observeAfterTamper = await ExecuteResultAsync<PlayObserveResponse>(observeAfterTamperResult);
        Assert(observeAfterTamper.Continuity is null, "observe must drop stale/mismatched continuity metadata");
    }
    finally
    {
        await app.DisposeAsync();
    }
}

static async Task VerifyObserveDoesNotSeedStateForEmptySessionAsync()
{
    const string sessionId = "session-observe-empty";
    var app = PlayWebApplication.Build([]);

    try
    {
        var store = app.Services.GetRequiredService<BrowserSessionEventLogStore>();
        var cache = app.Services.GetRequiredService<BrowserSessionOfflineCacheService>();
        var browserStore = app.Services.GetRequiredService<IBrowserKeyValueStore>();

        var observeResult = await PlayRouteHandlers.HandleObserveAsync(
            sessionId,
            store,
            cache,
            browserStore,
            CancellationToken.None
        );
        var observe = await ExecuteResultAsync<PlayObserveResponse>(observeResult);

        Assert(observe.SessionId == sessionId, "observe must preserve requested session id on empty state");
        Assert(observe.Checkpoint.SceneId == "scene-main", "observe must return fallback scene metadata on empty state");
        Assert(observe.Checkpoint.ProjectionFingerprint == "runtime-local", "observe must return fallback runtime metadata on empty state");
        Assert(observe.Projection.Cursor.AppliedThroughSequence == 0, "observe must start empty-state projections at sequence zero");
        Assert(observe.Continuity is null, "observe must not synthesize continuity metadata on empty state");

        var ledgerAfterObserve = await store.GetExistingAsync(sessionId);
        var checkpointAfterObserve = await cache.GetCheckpointAsync(sessionId);
        Assert(ledgerAfterObserve is null, "observe must not create a ledger for an empty session");
        Assert(checkpointAfterObserve is null, "observe must not persist a checkpoint for an empty session");
    }
    finally
    {
        await app.DisposeAsync();
    }
}

static async Task VerifyObserveDoesNotMutateStoredStateOrReturnStaleRuntimeBundleAsync()
{
    const string sessionId = "session-observe-stored";
    var app = PlayWebApplication.Build([]);

    try
    {
        var store = app.Services.GetRequiredService<BrowserSessionEventLogStore>();
        var cache = app.Services.GetRequiredService<BrowserSessionOfflineCacheService>();
        var browserStore = app.Services.GetRequiredService<IBrowserKeyValueStore>();
        await store.AppendPendingEventsAsync(sessionId, "scene-observe", "scene-r9", "runtime-observe", ["evt-pending"], 9);
        await cache.SetCheckpointAsync(
            new SyncCheckpoint(sessionId, "scene-observe", "scene-r9", "runtime-observe", 9, DateTimeOffset.UtcNow.AddMinutes(-5))
        );
        await cache.CacheRuntimeBundleAsync(sessionId, "runtime-stale", "scene-r1", "bundle:scene-observe:runtime-stale");

        var ledgerBeforeObserve = await store.GetExistingAsync(sessionId);
        var checkpointBeforeObserve = await cache.GetCheckpointAsync(sessionId);

        var observeResult = await PlayRouteHandlers.HandleObserveAsync(
            sessionId,
            store,
            cache,
            browserStore,
            CancellationToken.None
        );
        var observe = await ExecuteResultAsync<PlayObserveResponse>(observeResult);

        var ledgerAfterObserve = await store.GetExistingAsync(sessionId);
        var checkpointAfterObserve = await cache.GetCheckpointAsync(sessionId);

        Assert(observe.RuntimeBundle is null, "observe must drop runtime bundle metadata when its lineage no longer matches stored state");
        Assert(observe.Checkpoint.SceneRevision == "scene-r9", "observe must keep stored checkpoint scene revision");
        Assert(observe.Checkpoint.ProjectionFingerprint == "runtime-observe", "observe must keep stored checkpoint runtime fingerprint");
        Assert(observe.Projection.Timeline.Contains("pending:evt-pending", StringComparer.Ordinal), "observe must preserve stored pending replay events");
        Assert(Equals(ledgerBeforeObserve, ledgerAfterObserve), "observe must not mutate the stored ledger during a read path");
        Assert(Equals(checkpointBeforeObserve, checkpointAfterObserve), "observe must not rewrite the stored checkpoint during a read path");
    }
    finally
    {
        await app.DisposeAsync();
    }
}

static async Task VerifyObserveKeepsRequestedSessionIdWhenStoredCheckpointDriftsAsync()
{
    const string sessionId = "session-observe-route";
    const string driftedSessionId = "session-observe-drift";
    var app = PlayWebApplication.Build([]);

    try
    {
        var browserStore = app.Services.GetRequiredService<IBrowserKeyValueStore>();
        await browserStore.SetAsync(
            PlayBrowserStateKeys.Checkpoint(sessionId),
            new SyncCheckpoint(driftedSessionId, "scene-observe", "scene-r11", "runtime-observe", 11, DateTimeOffset.UtcNow)
        );
        await browserStore.SetAsync(
            PlayBrowserStateKeys.Continuity(driftedSessionId),
            new ObserverContinuityEntry(
                driftedSessionId,
                "scene-observe",
                "scene-r11",
                "runtime-observe",
                "observer-drift",
                "device-drift",
                PlaySurfaceRole.Player,
                11,
                DateTimeOffset.UtcNow,
                "token-drift"
            )
        );
        await browserStore.SetAsync(
            PlayBrowserStateKeys.Continuity(sessionId),
            new ObserverContinuityEntry(
                sessionId,
                "scene-observe",
                "scene-r11",
                "runtime-observe",
                "observer-route",
                "device-route",
                PlaySurfaceRole.GameMaster,
                11,
                DateTimeOffset.UtcNow,
                "token-route"
            )
        );

        var observe = await ExecuteRouteRequestAsync<PlayObserveResponse>(
            app,
            HttpMethod.Get,
            PlayApiRoutes.Observe,
            routeValues: new Dictionary<string, string> { ["sessionId"] = sessionId }
        );

        Assert(observe.SessionId == sessionId, "observe route must preserve the requested session id");
        Assert(observe.Checkpoint.SessionId == sessionId, "observe checkpoint must normalize corrupted stored session ids back to the route session id");
        Assert(observe.Projection.Cursor.Session.SessionId == sessionId, "observe projection must keep the route session id authoritative");
        Assert(observe.Continuity is not null, "observe should return continuity stored under the requested session id when lineage matches");
        Assert(observe.Continuity!.ObserverId == "observer-route", "observe must not read continuity data from a drifted stored session id");
    }
    finally
    {
        await app.DisposeAsync();
    }
}

static async Task VerifyObservePreservesContinuityWhenClaimCursorLeadsLedgerAsync()
{
    const string sessionId = "session-observe-claim-ahead";
    var app = PlayWebApplication.Build([]);

    try
    {
        var store = app.Services.GetRequiredService<BrowserSessionEventLogStore>();
        var cache = app.Services.GetRequiredService<BrowserSessionOfflineCacheService>();
        var browserStore = app.Services.GetRequiredService<IBrowserKeyValueStore>();
        await store.AppendPendingEventsAsync(sessionId, "scene-observe", "scene-r12", "runtime-observe", ["evt-pending"], 9);
        await cache.SetCheckpointAsync(
            new SyncCheckpoint(sessionId, "scene-observe", "scene-r12", "runtime-observe", 9, DateTimeOffset.UtcNow)
        );

        var claimResult = await PlayRouteHandlers.HandleContinuityClaimAsync(
            new PlayContinuityClaimRequest(
                new EngineSessionCursor(
                    new EngineSessionEnvelope(sessionId, "scene-observe", "scene-r12", "runtime-observe"),
                    12
                ),
                PlaySurfaceRole.Player,
                "device-player-a",
                "observer-player-a"
            ),
            store,
            cache,
            browserStore,
            CancellationToken.None
        );
        var continuityClaim = await ExecuteResultAsync<PlayContinuityClaimResponse>(claimResult);
        Assert(continuityClaim.Accepted, "continuity claim should be accepted when cursor leads ledger but lineage matches");
        Assert(continuityClaim.Checkpoint.AppliedThroughSequence == 12, "continuity claim must align checkpoint to the leading cursor sequence");

        var observeResult = await PlayRouteHandlers.HandleObserveAsync(
            sessionId,
            store,
            cache,
            browserStore,
            CancellationToken.None
        );
        var observe = await ExecuteResultAsync<PlayObserveResponse>(observeResult);
        Assert(observe.Continuity is not null, "observe must preserve continuity metadata after a leading-cursor claim");
        Assert(observe.Continuity!.ObservedThroughSequence == 12, "observe continuity sequence must match checkpoint-aligned claim sequence");
        Assert(observe.Checkpoint.AppliedThroughSequence == 12, "observe checkpoint sequence must stay checkpoint-aligned");
        Assert(observe.Projection.Cursor.AppliedThroughSequence == 12, "observe projection cursor sequence must align with checkpoint sequence");
    }
    finally
    {
        await app.DisposeAsync();
    }
}

static async Task VerifyObserveRouteRoundTripAsync()
{
    const string sessionId = "session-observe-routes";
    var app = PlayWebApplication.Build([]);

    try
    {
        var store = app.Services.GetRequiredService<BrowserSessionEventLogStore>();
        var cache = app.Services.GetRequiredService<BrowserSessionOfflineCacheService>();
        var browserStore = app.Services.GetRequiredService<IBrowserKeyValueStore>();
        await store.AppendPendingEventsAsync(sessionId, "scene-observe", "scene-r13", "runtime-observe", ["evt-pending"], 13);
        await cache.SetCheckpointAsync(
            new SyncCheckpoint(sessionId, "scene-observe", "scene-r13", "runtime-observe", 13, DateTimeOffset.UtcNow)
        );

        var claim = await PlayRouteHandlers.HandleContinuityClaimAsync(
            new PlayContinuityClaimRequest(
                new EngineSessionCursor(
                    new EngineSessionEnvelope(sessionId, "scene-observe", "scene-r13", "runtime-observe"),
                    13
                ),
                PlaySurfaceRole.GameMaster,
                "device-routes",
                "observer-routes"
            ),
            store,
            cache,
            browserStore,
            CancellationToken.None
        );
        var continuityClaim = await ExecuteResultAsync<PlayContinuityClaimResponse>(claim);
        Assert(continuityClaim.Accepted, "continuity claim setup must accept aligned lineage before route observe");

        var observe = await ExecuteRouteRequestAsync<PlayObserveResponse>(
            app,
            HttpMethod.Get,
            PlayApiRoutes.Observe,
            routeValues: new Dictionary<string, string> { ["sessionId"] = sessionId }
        );
        Assert(observe.Continuity is not null, "observe route must round-trip continuity metadata");
        Assert(observe.Continuity!.ObserverId == "observer-routes", "observe route must serialize continuity payloads correctly");
        Assert(observe.Projection.Cursor.AppliedThroughSequence == 13, "observe route must preserve checkpoint-aligned sequence state");
    }
    finally
    {
        await app.DisposeAsync();
    }
}

static async Task VerifyQueueMutationLineageExceptionReturnsStaleAsync()
{
    const string sessionId = "session-race";
    var storeState = new InMemoryBrowserKeyValueStore();
    var store = new BrowserSessionEventLogStore(storeState);
    var cache = new BrowserSessionOfflineCacheService(storeState);
    var requestSession = new EngineSessionEnvelope(sessionId, "scene-a", "scene-r1", "runtime-a");
    await store.AppendPendingEventsAsync(sessionId, requestSession.SceneId, requestSession.SceneRevision, requestSession.RuntimeFingerprint, ["evt-0"], 1);
    await cache.SetCheckpointAsync(
        new SyncCheckpoint(sessionId, requestSession.SceneId, requestSession.SceneRevision, requestSession.RuntimeFingerprint, 1, DateTimeOffset.UtcNow)
    );

    var driftedSession = new EngineSessionEnvelope(sessionId, "scene-drift", "scene-r2", "runtime-drift");
    var quickActionResult = await PlayRouteHandlers.HandleQuickActionAsync(
        new PlayQuickActionRequest(new EngineSessionCursor(requestSession, 1), PlaySurfaceRole.Player, "player-mark-ready"),
        cache,
        store,
        new ThrowingLineageDriftQueueService(store, cache, driftedSession, throwOnEnqueue: true, throwOnSync: false),
        PlayerShellModule.CreateDescriptor(),
        GmTacticalShellModule.CreateDescriptor(),
        CancellationToken.None
    );
    var quickActionResponse = await ExecuteResultAsync<PlayQuickActionResponse>(quickActionResult);
    Assert(!quickActionResponse.Accepted, "quick action must reject race-condition lineage drift as stale instead of 500");
    Assert(quickActionResponse.Stale, "quick action must report stale after queue-level lineage exception");
    Assert(quickActionResponse.Projection.Cursor.Session.SceneId == driftedSession.SceneId, "quick action stale response must use refreshed stored lineage");
    Assert(quickActionResponse.Projection.Timeline.Contains("pending:evt-drift", StringComparer.Ordinal), "quick action stale response must preserve refreshed pending events");

    var syncResult = await PlayRouteHandlers.HandleSyncAsync(
        new PlaySyncRequest(new EngineSessionCursor(requestSession, 1), ["evt-client"]),
        cache,
        store,
        new ThrowingLineageDriftQueueService(store, cache, driftedSession, throwOnEnqueue: false, throwOnSync: true),
        CancellationToken.None
    );
    var syncResponse = await ExecuteResultAsync<PlaySyncResponse>(syncResult);
    Assert(!syncResponse.Accepted, "sync must reject race-condition lineage drift as stale instead of 500");
    Assert(syncResponse.Stale, "sync must report stale after queue-level lineage exception");
    Assert(syncResponse.Projection.Cursor.Session.SceneRevision == driftedSession.SceneRevision, "sync stale response must use refreshed stored lineage");
    Assert(syncResponse.Projection.Timeline.Contains("pending:evt-drift", StringComparer.Ordinal), "sync stale response must preserve refreshed pending events");
}

static async Task VerifyBootstrapRejectsStaleLineageWithoutLedgerResetAsync()
{
    const string sessionId = "session-bootstrap-stale";
    var app = PlayWebApplication.Build([]);

    try
    {
        var store = app.Services.GetRequiredService<BrowserSessionEventLogStore>();
        var cache = app.Services.GetRequiredService<BrowserSessionOfflineCacheService>();
        await store.AppendPendingEventsAsync(sessionId, "scene-stored", "scene-r4", "runtime-stored", ["evt-stored"], 9);
        await cache.SetCheckpointAsync(
            new SyncCheckpoint(sessionId, "scene-stored", "scene-r4", "runtime-stored", 9, DateTimeOffset.UtcNow)
        );

        var query = $"?sessionId={Uri.EscapeDataString(sessionId)}&role={Uri.EscapeDataString(PlaySurfaceRole.Player.ToString())}&sceneId=scene-request&sceneRevision=scene-r1&runtimeFingerprint=runtime-request";
        var bootstrap = await ExecuteRouteRequestAsync<PlayBootstrapResponse>(
            app,
            HttpMethod.Get,
            PlayApiRoutes.Bootstrap,
            query,
            expectedStatusCode: StatusCodes.Status200OK
        );

        Assert(bootstrap.Projection.Cursor.Session.SceneId == "scene-stored", "bootstrap endpoint must normalize stale request lineage to stored scene id");
        Assert(bootstrap.Projection.Cursor.Session.SceneRevision == "scene-r4", "bootstrap endpoint must normalize stale request lineage to stored scene revision");
        Assert(bootstrap.Projection.Cursor.Session.RuntimeFingerprint == "runtime-stored", "bootstrap endpoint must normalize stale request lineage to stored runtime fingerprint");
        Assert(bootstrap.Projection.Cursor.AppliedThroughSequence == 9, "bootstrap endpoint must preserve stored sequence ownership");
        Assert(bootstrap.Projection.Timeline.Contains("pending:evt-stored", StringComparer.Ordinal), "bootstrap endpoint must preserve pending replay events during stale request recovery");

        var ledgerAfterBootstrap = await store.GetExistingAsync(sessionId);
        Assert(ledgerAfterBootstrap is not null, "bootstrap endpoint must keep stored ledger");
        Assert(ledgerAfterBootstrap!.SceneId == "scene-stored", "bootstrap endpoint must not reset stored ledger scene");
        Assert(ledgerAfterBootstrap.LastKnownSequence == 9, "bootstrap endpoint must not reset stored ledger sequence ownership");
        Assert(ledgerAfterBootstrap.PendingEvents.Count == 1 && ledgerAfterBootstrap.PendingEvents[0] == "evt-stored", "bootstrap endpoint must not drop stored pending events");

        var checkpointAfterBootstrap = await cache.GetCheckpointAsync(sessionId);
        Assert(checkpointAfterBootstrap is not null, "bootstrap endpoint must preserve checkpoint");
        Assert(checkpointAfterBootstrap!.SceneId == "scene-stored", "bootstrap endpoint must preserve checkpoint scene lineage");
        Assert(checkpointAfterBootstrap.SceneRevision == "scene-r4", "bootstrap endpoint must preserve checkpoint revision lineage");
        Assert(checkpointAfterBootstrap.ProjectionFingerprint == "runtime-stored", "bootstrap endpoint must preserve checkpoint runtime lineage");
        Assert(checkpointAfterBootstrap.AppliedThroughSequence == 9, "bootstrap endpoint must preserve checkpoint sequence ownership");
    }
    finally
    {
        await app.DisposeAsync();
    }
}

static async Task VerifyProjectionPrefersStoredLedgerWithoutCheckpointAsync()
{
    const string sessionId = "session-projection-ledger";
    var storeState = new InMemoryBrowserKeyValueStore();
    var store = new BrowserSessionEventLogStore(storeState);
    var cache = new BrowserSessionOfflineCacheService(storeState);

    await store.AppendPendingEventsAsync(sessionId, "scene-ledger", "scene-r7", "runtime-ledger", ["evt-ledger"], 12);

    var (session, ledger) = await PlayWebApplication.ResolveProjectionSessionAsync(
        sessionId,
        store,
        cache,
        CancellationToken.None
    );

    Assert(session.SceneId == "scene-ledger", "projection must prefer stored ledger scene id when checkpoint is missing");
    Assert(session.SceneRevision == "scene-r7", "projection must prefer stored ledger scene revision when checkpoint is missing");
    Assert(session.RuntimeFingerprint == "runtime-ledger", "projection must prefer stored ledger runtime when checkpoint is missing");
    Assert(ledger.PendingEvents.Count == 1 && ledger.PendingEvents[0] == "evt-ledger", "projection must preserve stored pending events");
    Assert(ledger.LastKnownSequence == 12, "projection must preserve stored sequence ownership");
}

static async Task VerifyResumePreservesLedgerWhenRuntimeBundleDriftsAsync()
{
    const string sessionId = "session-resume-lineage";
    var storeState = new InMemoryBrowserKeyValueStore();
    var store = new BrowserSessionEventLogStore(storeState);
    var cache = new BrowserSessionOfflineCacheService(storeState);

    await store.AppendPendingEventsAsync(sessionId, "scene-ledger", "scene-r9", "runtime-ledger", ["evt-pending"], 6);
    await cache.SetCheckpointAsync(
        new SyncCheckpoint(sessionId, "scene-ledger", "scene-r9", "runtime-ledger", 6, DateTimeOffset.UtcNow)
    );
    await cache.CacheRuntimeBundleAsync(
        new RuntimeBundleCacheEntry(
            sessionId,
            "runtime-stale",
            "scene-r3",
            "bundle:scene-ledger:runtime-stale",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow
        )
    );

    var resumeState = await PlayWebApplication.ResolveResumeStateAsync(
        sessionId,
        store,
        cache,
        CancellationToken.None
    );

    Assert(resumeState.Session.SceneRevision == "scene-r9", "resume must keep checkpoint scene revision when runtime metadata drifts");
    Assert(resumeState.Session.RuntimeFingerprint == "runtime-ledger", "resume must keep checkpoint runtime when runtime metadata drifts");
    Assert(resumeState.Ledger.PendingEvents.Count == 1 && resumeState.Ledger.PendingEvents[0] == "evt-pending", "resume must preserve stored pending events");
    Assert(resumeState.Ledger.LastKnownSequence == 6, "resume must preserve stored sequence ownership");
}

static async Task VerifyResumeNormalizesCheckpointToLedgerLineageAsync()
{
    const string sessionId = "session-resume-normalize";
    var storeState = new InMemoryBrowserKeyValueStore();
    var store = new BrowserSessionEventLogStore(storeState);
    var cache = new BrowserSessionOfflineCacheService(storeState);

    await store.AppendPendingEventsAsync(sessionId, "scene-ledger", "scene-r8", "runtime-ledger", ["evt-a"], 7);
    await cache.SetCheckpointAsync(
        new SyncCheckpoint(sessionId, "scene-old", "scene-r1", "runtime-old", 3, DateTimeOffset.UtcNow)
    );

    var resumeState = await PlayWebApplication.ResolveResumeStateAsync(
        sessionId,
        store,
        cache,
        CancellationToken.None
    );

    Assert(resumeState.Checkpoint is not null, "resume must emit a checkpoint");
    var checkpoint = resumeState.Checkpoint!;
    Assert(checkpoint.SceneId == "scene-ledger", "resume must normalize checkpoint scene id to ledger lineage");
    Assert(checkpoint.SceneRevision == "scene-r8", "resume must normalize checkpoint revision to ledger lineage");
    Assert(checkpoint.ProjectionFingerprint == "runtime-ledger", "resume must normalize checkpoint runtime to ledger lineage");
    Assert(checkpoint.AppliedThroughSequence == 7, "resume must advance checkpoint sequence to ledger ownership");
}

static async Task VerifyRuntimeBundleSessionLockReleasesOnCanceledAcquireAsync()
{
    const string sessionId = "session-runtime-lock-cancel";
    var browserStore = new CoordinatedRuntimeWriteBrowserStore(sessionId);
    var cache = new BrowserSessionOfflineCacheService(browserStore);
    browserStore.EnableReadTouchWriteBarrier();

    var holdTask = cache.CacheRuntimeBundleAsync(
        new RuntimeBundleCacheEntry(
            sessionId,
            "runtime-initial",
            "scene-r1",
            "bundle-lock-initial",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow
        ),
        CancellationToken.None
    );
    await browserStore.WaitForBlockedReadTouchWriteAsync();

    using var cancelSource = new CancellationTokenSource();
    cancelSource.Cancel();
    await AssertThrowsAsync<OperationCanceledException>(
        () => cache.GetRuntimeBundleAsync(sessionId, cancelSource.Token),
        "runtime-bundle lock acquire cancellation must propagate operation canceled"
    );

    browserStore.ReleaseReadTouchWriteBarrier();
    await holdTask;
    await cache.GetRuntimeBundleAsync(sessionId, CancellationToken.None);

    var lockCount = GetRuntimeBundleSessionLockCount();
    Assert(lockCount == 0, "runtime-bundle session locks must be released after canceled acquire attempts");
}

static int GetRuntimeBundleSessionLockCount()
{
    var field = typeof(BrowserSessionOfflineCacheService).GetField(
        "RuntimeBundleSessionLocks",
        BindingFlags.NonPublic | BindingFlags.Static
    ) ?? throw new InvalidOperationException("Could not access runtime bundle lock state for regression validation.");

    var dictionary = field.GetValue(null) as System.Collections.IDictionary
        ?? throw new InvalidOperationException("Runtime bundle lock state has an unexpected type.");
    return dictionary.Count;
}

static void VerifyStoredStaleStatePrefersLedgerOverOlderCheckpoint()
{
    var requestSession = new EngineSessionEnvelope("session-stale-priority", "scene-request", "scene-r9", "runtime-request");
    var requestCursor = new EngineSessionCursor(requestSession, 12);
    var checkpoint = new SyncCheckpoint(
        "session-stale-priority",
        "scene-checkpoint",
        "scene-r3",
        "runtime-checkpoint",
        4,
        DateTimeOffset.UtcNow
    );
    var ledger = new Chummer.Play.Core.Offline.OfflineLedgerEnvelope(
        "session-stale-priority",
        "scene-ledger",
        "scene-r7",
        "runtime-ledger",
        ["evt-ledger"],
        8,
        DateTimeOffset.UtcNow,
        null,
        0
    );

    var staleState = PlayRouteHandlers.BuildStoredStaleState(requestSession, requestCursor, checkpoint, ledger);

    Assert(staleState.Projection.Cursor.Session.SceneId == "scene-ledger", "stale projection must prefer ledger scene over older checkpoint");
    Assert(staleState.Projection.Cursor.Session.SceneRevision == "scene-r7", "stale projection must prefer ledger revision over older checkpoint");
    Assert(staleState.Projection.Cursor.Session.RuntimeFingerprint == "runtime-ledger", "stale projection must prefer ledger runtime over older checkpoint");
    Assert(staleState.Checkpoint.SceneId == "scene-ledger", "stale checkpoint must normalize to the stored ledger scene when checkpoint is older");
    Assert(staleState.Checkpoint.SceneRevision == "scene-r7", "stale checkpoint must normalize to the stored ledger revision when checkpoint is older");
    Assert(staleState.Checkpoint.ProjectionFingerprint == "runtime-ledger", "stale checkpoint must normalize to the stored ledger runtime when checkpoint is older");
    Assert(staleState.Checkpoint.AppliedThroughSequence == 8, "stale checkpoint sequence must advance to the newer ledger sequence");
}

static async Task<TResponse> ExecuteResultAsync<TResponse>(IResult result)
{
    var context = new DefaultHttpContext
    {
        RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider(),
    };
    context.Response.Body = new MemoryStream();

    await result.ExecuteAsync(context);

    if (context.Response.StatusCode is < 200 or >= 300)
    {
        throw new InvalidOperationException($"Expected success response, got {context.Response.StatusCode}.");
    }

    context.Response.Body.Position = 0;
    using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
    var json = await reader.ReadToEndAsync();
    return JsonSerializer.Deserialize<TResponse>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))
        ?? throw new InvalidOperationException("Expected JSON response payload.");
}

static async Task<TResponse> ExecuteResultWithStatusAsync<TResponse>(IResult result, int expectedStatusCode)
{
    var context = new DefaultHttpContext
    {
        RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider(),
    };
    context.Response.Body = new MemoryStream();

    await result.ExecuteAsync(context);

    if (context.Response.StatusCode != expectedStatusCode)
    {
        throw new InvalidOperationException($"Expected status {expectedStatusCode}, got {context.Response.StatusCode}.");
    }

    context.Response.Body.Position = 0;
    using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
    var json = await reader.ReadToEndAsync();
    return JsonSerializer.Deserialize<TResponse>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))
        ?? throw new InvalidOperationException("Expected JSON response payload.");
}

static void VerifyCheckpointLineageAlignment()
{
    var checkpoint = new SyncCheckpoint("session-c", "scene-a", "scene-r1", "runtime-a", 5, DateTimeOffset.UtcNow);
    var aligned = new EngineSessionEnvelope("session-c", "scene-a", "scene-r1", "runtime-a");
    var mismatchedRuntime = new EngineSessionEnvelope("session-c", "scene-a", "scene-r1", "runtime-b");

    Assert(SessionLineage.IsCheckpointAligned(checkpoint, aligned), "aligned checkpoint lineage must pass");
    Assert(!SessionLineage.IsCheckpointAligned(checkpoint, mismatchedRuntime), "runtime mismatch must fail stale protection");
}

static void VerifyStoredLineageAlignment()
{
    var session = new EngineSessionEnvelope("session-e", "scene-a", "scene-r1", "runtime-a");
    var checkpoint = new SyncCheckpoint("session-e", "scene-a", "scene-r1", "runtime-a", 1, DateTimeOffset.UtcNow);
    var ledger = new Chummer.Play.Core.Offline.OfflineLedgerEnvelope(
        "session-e",
        "scene-a",
        "scene-r1",
        "runtime-a",
        [],
        1,
        DateTimeOffset.UtcNow,
        null,
        0
    );
    var mismatchedLedger = ledger with { RuntimeFingerprint = "runtime-b" };

    Assert(SessionLineage.IsStoredLineageAligned(session, checkpoint, ledger), "stored lineage must pass when checkpoint and ledger match");
    Assert(!SessionLineage.IsStoredLineageAligned(session, checkpoint, mismatchedLedger), "stored lineage must fail when ledger lineage changes");
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static async Task AssertThrowsAsync<TException>(Func<Task> action, string message)
    where TException : Exception
{
    try
    {
        await action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException(message);
}

static BrowserSessionApiClient CreateApiClient(HttpMessageHandler handler) =>
    new(
        new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost"),
        }
    );

static string GetRepoRoot()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "WORKLIST.md")))
        {
            return directory.FullName;
        }

        directory = directory.Parent;
    }

    throw new InvalidOperationException("Could not locate repository root from regression checks runtime directory.");
}

static async Task<TResponse> ExecuteRouteRequestAsync<TResponse>(
    WebApplication app,
    HttpMethod method,
    string route,
    string query = "",
    string? jsonBody = null,
    IReadOnlyDictionary<string, string>? routeValues = null,
    int expectedStatusCode = StatusCodes.Status200OK
)
{
    var endpointRouteBuilder = (IEndpointRouteBuilder)app;
    var endpoint = endpointRouteBuilder
        .DataSources
        .SelectMany(static source => source.Endpoints)
        .OfType<RouteEndpoint>()
        .FirstOrDefault(candidate =>
            candidate.RequestDelegate is not null
            && MethodMatches(candidate, method)
            && RouteMatches(candidate, route)
        );
    if (endpoint is null)
    {
        throw new InvalidOperationException($"Could not resolve endpoint for route '{route}' and method '{method.Method}'.");
    }

    var context = new DefaultHttpContext
    {
        RequestServices = app.Services,
    };
    context.Request.Method = method.Method;
    context.Request.Path = NormalizePath(route);
    context.Request.QueryString = new QueryString(query);
    context.Response.Body = new MemoryStream();
    if (routeValues is not null)
    {
        foreach (var routeValue in routeValues)
        {
            context.Request.RouteValues[routeValue.Key] = routeValue.Value;
        }
    }

    if (!string.IsNullOrWhiteSpace(jsonBody))
    {
        var bytes = Encoding.UTF8.GetBytes(jsonBody);
        context.Request.Body = new MemoryStream(bytes);
        context.Request.ContentLength = bytes.Length;
        context.Request.ContentType = "application/json";
    }

    await endpoint.RequestDelegate!(context);
    Assert(
        context.Response.StatusCode == expectedStatusCode,
        $"Expected status code {expectedStatusCode} for '{route}' but received {context.Response.StatusCode}."
    );

    context.Response.Body.Position = 0;
    using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
    var payload = await reader.ReadToEndAsync();
    return JsonSerializer.Deserialize<TResponse>(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web))
        ?? throw new InvalidOperationException($"Route '{route}' returned an empty JSON payload.");
}

static bool MethodMatches(RouteEndpoint endpoint, HttpMethod method)
{
    var methods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods;
    return methods is null || methods.Any(allowed => string.Equals(allowed, method.Method, StringComparison.OrdinalIgnoreCase));
}

static bool RouteMatches(RouteEndpoint endpoint, string route)
{
    var endpointRoute = endpoint.RoutePattern.RawText ?? endpoint.RoutePattern.ToString();
    return string.Equals(NormalizePath(endpointRoute), NormalizePath(route), StringComparison.OrdinalIgnoreCase);
}

static string NormalizePath(string? path)
{
    if (string.IsNullOrWhiteSpace(path))
    {
        return "/";
    }

    var normalized = path.Trim();
    if (!normalized.StartsWith("/", StringComparison.Ordinal))
    {
        normalized = "/" + normalized;
    }

    return normalized.Length > 1
        ? normalized.TrimEnd('/')
        : normalized;
}

file sealed record ReconnectConflictPayload(
    string Error,
    bool Stale,
    PlaySessionProjection Projection,
    SyncCheckpoint Checkpoint
);

file sealed class CoordinatedRuntimeWriteBrowserStore : IBrowserKeyValueStore
{
    private readonly InMemoryBrowserKeyValueStore _inner = new();
    private readonly string _runtimeBundleKey;
    private readonly TaskCompletionSource _readTouchWriteBlocked =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _readTouchWriteRelease =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private volatile bool _barrierEnabled;

    public CoordinatedRuntimeWriteBrowserStore(string sessionId)
    {
        _runtimeBundleKey = PlayBrowserStateKeys.RuntimeBundle(sessionId);
    }

    public void EnableReadTouchWriteBarrier() => _barrierEnabled = true;

    public async Task WaitForBlockedReadTouchWriteAsync()
    {
        await _readTouchWriteBlocked.Task;
    }

    public void ReleaseReadTouchWriteBarrier()
    {
        _readTouchWriteRelease.TrySetResult();
    }

    public Task<TValue?> GetAsync<TValue>(string key, CancellationToken cancellationToken = default)
    {
        return _inner.GetAsync<TValue>(key, cancellationToken);
    }

    public async Task SetAsync<TValue>(string key, TValue value, CancellationToken cancellationToken = default)
    {
        if (_barrierEnabled
            && string.Equals(key, _runtimeBundleKey, StringComparison.Ordinal)
            && value is RuntimeBundleCacheEntry runtimeBundleEntry
            && string.Equals(runtimeBundleEntry.RuntimeFingerprint, "runtime-initial", StringComparison.Ordinal))
        {
            _readTouchWriteBlocked.TrySetResult();
            await _readTouchWriteRelease.Task.WaitAsync(cancellationToken);
        }

        await _inner.SetAsync(key, value, cancellationToken);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        return _inner.RemoveAsync(key, cancellationToken);
    }

    public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
    {
        return _inner.ListKeysAsync(prefix, cancellationToken);
    }
}

file sealed class DelayedMutationBrowserStore : IBrowserKeyValueStore
{
    private readonly InMemoryBrowserKeyValueStore _inner = new();
    private readonly TimeSpan _mutationDelay;

    public DelayedMutationBrowserStore(TimeSpan mutationDelay)
    {
        _mutationDelay = mutationDelay;
    }

    public Task<TValue?> GetAsync<TValue>(string key, CancellationToken cancellationToken = default)
    {
        return _inner.GetAsync<TValue>(key, cancellationToken);
    }

    public async Task SetAsync<TValue>(string key, TValue value, CancellationToken cancellationToken = default)
    {
        await Task.Delay(_mutationDelay, cancellationToken);
        await _inner.SetAsync(key, value, cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await Task.Delay(_mutationDelay, cancellationToken);
        await _inner.RemoveAsync(key, cancellationToken);
    }

    public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
    {
        return _inner.ListKeysAsync(prefix, cancellationToken);
    }
}

file sealed class GateableGetBrowserStore : IBrowserKeyValueStore
{
    private readonly InMemoryBrowserKeyValueStore _inner = new();
    private readonly string _gatedKey;
    private readonly TaskCompletionSource _getStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _releaseGet = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private volatile bool _gateEnabled;

    public GateableGetBrowserStore(string gatedKey)
    {
        _gatedKey = gatedKey;
    }

    public void EnableGate()
    {
        _gateEnabled = true;
    }

    public async Task<TValue?> GetAsync<TValue>(string key, CancellationToken cancellationToken = default)
    {
        if (_gateEnabled && string.Equals(key, _gatedKey, StringComparison.Ordinal))
        {
            _getStarted.TrySetResult();
            using var registration = cancellationToken.Register(() => _releaseGet.TrySetCanceled(cancellationToken));
            await _releaseGet.Task.WaitAsync(cancellationToken);
        }

        return await _inner.GetAsync<TValue>(key, cancellationToken);
    }

    public Task SetAsync<TValue>(string key, TValue value, CancellationToken cancellationToken = default)
    {
        return _inner.SetAsync(key, value, cancellationToken);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        return _inner.RemoveAsync(key, cancellationToken);
    }

    public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
    {
        return _inner.ListKeysAsync(prefix, cancellationToken);
    }

    public Task WaitForGetAsync()
    {
        return _getStarted.Task;
    }

    public void ReleaseGet()
    {
        _releaseGet.TrySetResult();
    }
}

file sealed class ThrowingLineageDriftQueueService : IPlayOfflineQueueService
{
    private readonly BrowserSessionEventLogStore _store;
    private readonly BrowserSessionOfflineCacheService _cache;
    private readonly EngineSessionEnvelope _driftedSession;
    private readonly bool _throwOnEnqueue;
    private readonly bool _throwOnSync;

    public ThrowingLineageDriftQueueService(
        BrowserSessionEventLogStore store,
        BrowserSessionOfflineCacheService cache,
        EngineSessionEnvelope driftedSession,
        bool throwOnEnqueue,
        bool throwOnSync
    )
    {
        _store = store;
        _cache = cache;
        _driftedSession = driftedSession;
        _throwOnEnqueue = throwOnEnqueue;
        _throwOnSync = throwOnSync;
    }

    public async Task<OfflineQueueEnqueueResult> EnqueueAsync(
        EngineSessionCursor cursor,
        string queuedEvent,
        CancellationToken cancellationToken = default
    )
    {
        if (!_throwOnEnqueue)
        {
            throw new NotSupportedException("Test queue is configured only for sync mutation failure.");
        }

        await DriftLineageAsync(cancellationToken);
        throw new InvalidOperationException("Stored lineage drifted during queue mutation.");
    }

    public async Task<OfflineQueueSyncResult> SyncReplayAsync(
        PlaySyncRequest request,
        CancellationToken cancellationToken = default
    )
    {
        if (!_throwOnSync)
        {
            throw new NotSupportedException("Test queue is configured only for quick-action mutation failure.");
        }

        await DriftLineageAsync(cancellationToken);
        throw new InvalidOperationException("Stored lineage drifted during queue mutation.");
    }

    private async Task DriftLineageAsync(CancellationToken cancellationToken)
    {
        await _store.GetOrCreateAsync(
            _driftedSession.SessionId,
            _driftedSession.SceneId,
            _driftedSession.SceneRevision,
            _driftedSession.RuntimeFingerprint,
            cancellationToken
        );
        await _store.AppendPendingEventsAsync(
            _driftedSession.SessionId,
            _driftedSession.SceneId,
            _driftedSession.SceneRevision,
            _driftedSession.RuntimeFingerprint,
            ["evt-drift"],
            2,
            cancellationToken
        );
        await _cache.SetCheckpointAsync(
            new SyncCheckpoint(
                _driftedSession.SessionId,
                _driftedSession.SceneId,
                _driftedSession.SceneRevision,
                _driftedSession.RuntimeFingerprint,
                2,
                DateTimeOffset.UtcNow
            ),
            cancellationToken
        );
    }
}

file sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

    public StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) =>
        _handler = handler;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => await _handler(request, cancellationToken);
}
