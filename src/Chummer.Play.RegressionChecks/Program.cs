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
using System.Text;
using System.Text.Json;

await VerifyLedgerLineageResetAsync();
await VerifyBootstrapProjectionPreservesReplayStateAsync();
await VerifyMonotonicSequenceOwnershipAsync();
await VerifyConcurrentEnqueueSequenceOwnershipAsync();
await VerifySyncPrefixAcknowledgementAsync();
await VerifySyncPreservesNewerLedgerSequenceAsync();
VerifyCursorValidationRejectsNegativeSequence();
await VerifyEventLogRejectsMalformedAppendAsync();
await VerifyEventLogRejectsSequenceRegressionAsync();
await VerifyEventLogPersistsAcrossServiceInstancesAsync();
await VerifyOfflineQueueRejectsMalformedPendingEventsAsync();
await VerifyOfflineQueueRejectsNegativeSequenceAsync();
await VerifyOfflineQueueRejectsMalformedSessionEnvelopeAsync();
await VerifyOfflineCacheRejectsMalformedCheckpointAndRuntimeEntryAsync();
await VerifyOfflineCacheDropsMalformedStoredEntriesAsync();
await VerifyOfflineCacheDropsUnparseableStoredEntriesAsync();
await VerifyOfflineCacheRuntimeBundleQuotaEvictionAsync();
await VerifyOfflineCacheReadTouchAffectsQuotaEvictionAsync();
await VerifyOfflineCacheQuotaIgnoresUnparseableRuntimeBundleKeysAsync();
await VerifyEventLogDropsMalformedStoredLedgerAsync();
await VerifyEventLogDropsUnparseableStoredLedgerKeysAsync();
await VerifyOfflineQueueRejectsStaleLineageAsync();
await VerifyReconnectLineageTransitionContinuityAsync();
await VerifyStoredLineageStaleResponsesAsync();
await VerifyReconnectRejectsStaleLineageWithoutMutationAsync();
await VerifyReconnectClientThrowsTypedStaleAsync();
await VerifyQueueMutationLineageExceptionReturnsStaleAsync();
await VerifyBootstrapRejectsStaleLineageWithoutLedgerResetAsync();
await VerifyProjectionPrefersStoredLedgerWithoutCheckpointAsync();
VerifyStoredStaleStatePrefersLedgerOverOlderCheckpoint();
await VerifyResumePreservesLedgerWhenRuntimeBundleDriftsAsync();
await VerifyResumeNormalizesCheckpointToLedgerLineageAsync();
VerifyCheckpointLineageAlignment();
VerifyStoredLineageAlignment();

Console.WriteLine("chummer-play regression checks ok");

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

static async Task VerifyOfflineCacheReadTouchAffectsQuotaEvictionAsync()
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

    var oldestUntouched = await cache.GetRuntimeBundleAsync("session-cache-touch-2");
    Assert(oldestUntouched is null, "quota eviction must evict the oldest untouched runtime bundle after read-touch");

    var touchedAfterEviction = await cache.GetRuntimeBundleAsync("session-cache-touch-1");
    Assert(touchedAfterEviction is not null, "read-touch must keep recently validated runtime bundles through quota eviction");
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

static async Task<TResponse> ExecuteRouteRequestAsync<TResponse>(
    WebApplication app,
    HttpMethod method,
    string route,
    string query = "",
    string? jsonBody = null,
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
