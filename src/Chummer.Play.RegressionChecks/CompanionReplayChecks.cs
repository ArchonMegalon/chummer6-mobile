using Chummer.Play.Core.Application;
using Chummer.Play.Core.Offline;
using Chummer.Play.Web;
using Chummer.Play.Web.BrowserState;

internal static class CompanionReplayChecks
{
    private const string Session = "synthetic-replay-session";
    private const string Text = "turn:metric:ammo:-1";

    public static async Task RunAsync()
    {
        await CheckAsync(nameof(LostReplySurvivesRestartAndAcknowledgementAsync), LostReplySurvivesRestartAndAcknowledgementAsync);
        await CheckAsync(nameof(ConcurrentRetryAndScopeSeparationAsync), ConcurrentRetryAndScopeSeparationAsync);
        await CheckAsync(nameof(LegacyAndIdentityConflictsAsync), LegacyAndIdentityConflictsAsync);
        await CheckAsync(nameof(FailedAtomicCommitRetainsLegacyLedgerAsync), FailedAtomicCommitRetainsLegacyLedgerAsync);
        await CheckAsync(nameof(CheckpointFailureAfterCommitIsRetryableAsync), CheckpointFailureAfterCommitIsRetryableAsync);
    }

    private static async Task LostReplySurvivesRestartAndAcknowledgementAsync(string root)
    {
        var first = Create(root);
        var request = Batch("batch-1", "event-1", "event-2");
        var posted = await first.Service.ReplayClientQueueAsync(Session, PlaySurfaceRole.Player, request, "owner-a");
        AssertAck(posted, request);
        Assert(posted.PendingQueueCount == 2, "separate actions with identical text must both append");
        // Ignore the original reply and retry through entirely new service/store instances.
        var restarted = Create(root);
        var retry = await restarted.Service.ReplayClientQueueAsync(Session, PlaySurfaceRole.Player, request, "owner-a");
        AssertAck(retry, request);
        Assert(retry.PendingQueueCount == 2, "lost reply retry must not append again after restart");
        await restarted.Service.AcknowledgePendingQueueAsync(Session, PlaySurfaceRole.Player, "owner-a");
        var afterAcknowledgement = await Create(root).Service.ReplayClientQueueAsync(Session, PlaySurfaceRole.Player, request, "owner-a");
        AssertAck(afterAcknowledgement, request);
        Assert(afterAcknowledgement.PendingQueueCount == 0, "deduplication must survive pending-event trimming");
        var fresh = await Create(root).Service.ReplayClientQueueAsync(Session, PlaySurfaceRole.Player, Batch("batch-2", "event-3"), "owner-a");
        Assert(fresh.PendingQueueCount == 1, "same text with a fresh identity remains a distinct action");
        var ledger = await Create(root).Ledger.GetExistingAsync(Session);
        Assert(ledger!.LastKnownSequence == 3, "retry must not advance sequence ownership");
    }

    private static async Task ConcurrentRetryAndScopeSeparationAsync(string root)
    {
        var request = Batch("shared-batch", "shared-event");
        // No pre-seeding: resume/ledger initialization is part of this race.
        var batch = new PlayReplayBatch(request.BatchId!, request.IdentifiedEvents!);
        var initialized = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ =>
            Create(root).Queue.EnqueueReplayBatchAsync(Session, "Player:owner-a", batch)));
        foreach (var result in initialized)
            Assert(result.Ledger.PendingEvents.Count == 1 && result.AppliedThroughSequence == 1,
                "concurrent first submissions must initialize and append only once");
        // The private page normally initializes its projection before replay.
        // Exercise full concurrent service replies with that existing state.
        await Create(root).Service.GetProjectionAsync(Session, PlaySurfaceRole.Player, "owner-a");
        var replies = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ =>
            Create(root).Service.ReplayClientQueueAsync(Session, PlaySurfaceRole.Player, request, "owner-a")));
        foreach (var reply in replies) AssertAck(reply, request);
        var same = await Create(root).Ledger.GetExistingAsync(Session);
        Assert(same!.PendingEvents.Count == 1 && same.LastKnownSequence == 1, "concurrent identical batches must append once");
        await Create(root).Service.ReplayClientQueueAsync(Session, PlaySurfaceRole.Player, request, "owner-b");
        await Create(root).Service.ReplayClientQueueAsync(Session, PlaySurfaceRole.GameMaster, request, "owner-a");
        var scoped = await Create(root).Ledger.GetExistingAsync(Session);
        Assert(scoped!.PendingEvents.Count == 3, "device and role owner scopes must not consume each other's identities");
        await Create(root).Service.ReplayClientQueueAsync("another-session", PlaySurfaceRole.Player, request, "owner-a");
        Assert((await Create(root).Ledger.GetExistingAsync("another-session"))!.PendingEvents.Count == 1,
            "another session must not share deduplication receipts");
        var observer = await Create(root).Service.ReplayClientQueueAsync(Session, PlaySurfaceRole.Observer, request, "owner-a");
        Assert(!observer.Accepted && observer.AcknowledgedEventIds is null, "observer lane must never acknowledge a mutation");
    }

    private static async Task LegacyAndIdentityConflictsAsync(string root)
    {
        var stores = Create(root);
        // Old clients retain the text-only contract. No stable identity can be inferred.
        var legacy = await stores.Service.ReplayClientQueueAsync(Session, PlaySurfaceRole.Player, new[] { Text }, "owner-a");
        Assert(legacy.Accepted && !legacy.RetrySafe && legacy.AcknowledgedEventIds is null && legacy.AcknowledgedBatchId is null,
            "legacy acknowledgement must explicitly lack a retry-safe identity");
        var request = Batch("new-batch", "new-event");
        await stores.Service.ReplayClientQueueAsync(Session, PlaySurfaceRole.Player, request, "owner-a");
        Assert((await stores.Ledger.GetExistingAsync(Session))!.PendingEvents.Count == 2,
            "pre-upgrade persisted text must not be mistaken for a new identified action");
        await ThrowsAsync<ArgumentException>(() => stores.Service.ReplayClientQueueAsync(Session, PlaySurfaceRole.Player,
            Batch("new-batch", "different-event"), "owner-a"));
        await ThrowsAsync<ArgumentException>(() => stores.Service.ReplayClientQueueAsync(Session, PlaySurfaceRole.Player,
            new PlayTurnCompanionReplayRequest(BatchId: "another-batch", IdentifiedEvents: [new("new-event", "turn:metric:ammo:-2")]), "owner-a"));
        await ThrowsAsync<ArgumentException>(() => stores.Service.ReplayClientQueueAsync(Session, PlaySurfaceRole.Player,
            Batch("duplicate-ids", "duplicate", "duplicate"), "owner-a"));
        var regrouped = await stores.Service.ReplayClientQueueAsync(Session, PlaySurfaceRole.Player,
            Batch("regrouped", "new-event"), "owner-a");
        Assert(regrouped.PendingQueueCount == 2, "event identity must deduplicate across batches in its scope");
        // Legacy append after migration and acknowledgement must preserve durable identities.
        await stores.Service.ReplayClientQueueAsync(Session, PlaySurfaceRole.Player, new[] { Text }, "owner-a");
        var retry = await Create(root).Service.ReplayClientQueueAsync(Session, PlaySurfaceRole.Player, request, "owner-a");
        Assert(retry.PendingQueueCount == 3, "legacy writes after migration must preserve identified receipts");
    }

    private static async Task FailedAtomicCommitRetainsLegacyLedgerAsync(string root)
    {
        var seeded = Create(root);
        await seeded.Service.ReplayClientQueueAsync(Session, PlaySurfaceRole.Player, new[] { Text }, "owner-a");
        var original = (await seeded.Ledger.GetExistingAsync(Session))!;
        var failing = new BrowserSessionEventLogStore(new FileSystemBrowserKeyValueStore(root,
            _ => throw new IOException("synthetic before-commit failure")));
        var batch = new PlayReplayBatch("atomic-batch", [new("atomic-event", Text)]);
        await ThrowsAsync<IOException>(() => failing.AppendReplayBatchAsync(Session, original.SceneId,
            original.SceneRevision, original.RuntimeFingerprint, "Player:owner-a", batch, original.LastKnownSequence));
        Assert((await Create(root).Ledger.GetExistingAsync(Session))!.PendingEvents.Count == 1,
            "failed migration must preserve the pre-upgrade pending event");
        var request = new PlayTurnCompanionReplayRequest(BatchId: batch.BatchId, IdentifiedEvents: batch.Events);
        var retry = await Create(root).Service.ReplayClientQueueAsync(Session, PlaySurfaceRole.Player, request, "owner-a");
        AssertAck(retry, request);
        Assert(retry.PendingQueueCount == 2, "failed commit must not leave a phantom deduplication receipt");
    }

    private static async Task CheckpointFailureAfterCommitIsRetryableAsync(string root)
    {
        await Create(root).Service.GetProjectionAsync(Session, PlaySurfaceRole.Player);
        var backing = new FileSystemBrowserKeyValueStore(root);
        var store = new FailCheckpointStore(backing);
        var stores = Create(store);
        var request = Batch("checkpoint-batch", "checkpoint-event");
        await ThrowsAsync<IOException>(() => stores.Service.ReplayClientQueueAsync(Session, PlaySurfaceRole.Player, request, "owner-a"));
        Assert((await Create(root).Ledger.GetExistingAsync(Session))!.PendingEvents.Count == 1,
            "ledger and replay receipt must commit before checkpoint failure");
        var retry = await Create(root).Service.ReplayClientQueueAsync(Session, PlaySurfaceRole.Player, request, "owner-a");
        AssertAck(retry, request);
        Assert(retry.PendingQueueCount == 1, "post-commit failure must be recoverable without duplication");
    }

    private static PlayTurnCompanionReplayRequest Batch(string batchId, params string[] ids)
        => new(BatchId: batchId, IdentifiedEvents: ids.Select(id => new PlayReplayEvent(id, Text)).ToArray());

    private static (PlayTurnCompanionService Service, BrowserSessionEventLogStore Ledger, BrowserSessionOfflineQueueService Queue) Create(string root)
        => Create(new FileSystemBrowserKeyValueStore(root));

    private static (PlayTurnCompanionService Service, BrowserSessionEventLogStore Ledger, BrowserSessionOfflineQueueService Queue) Create(IBrowserKeyValueStore store)
    {
        var ledger = new BrowserSessionEventLogStore(store);
        var cache = new BrowserSessionOfflineCacheService(store);
        var queue = new BrowserSessionOfflineQueueService(ledger, cache);
        return (new PlayTurnCompanionService(store, ledger, cache, queue), ledger, queue);
    }

    private static void AssertAck(PlayTurnCompanionQueueStatusResponse response, PlayTurnCompanionReplayRequest request)
        => Assert(response.Accepted && response.RetrySafe && response.AcknowledgedBatchId == request.BatchId
            && response.AcknowledgedEventIds!.SequenceEqual(request.IdentifiedEvents!.Select(item => item.EventId)),
            "only the submitted stable identities may be acknowledged");

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static async Task ThrowsAsync<T>(Func<Task> action) where T : Exception
    {
        try { await action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }

    private static async Task CheckAsync(string name, Func<string, Task> check)
    {
        string root = Path.Combine(Path.GetTempPath(), "chummer-replay-checks", Guid.NewGuid().ToString("N"));
        try { await check(root); Console.WriteLine($"PASS {name}"); }
        finally { if (Directory.Exists(root)) Directory.Delete(root, recursive: true); }
    }

    private sealed class FailCheckpointStore(IBrowserKeyValueStore inner) : IBrowserKeyValueStore
    {
        private int _checkpointWrites;
        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) => inner.GetAsync<T>(key, cancellationToken);
        public Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default)
            => key == PlayBrowserStateKeys.Checkpoint(Session) && Interlocked.Increment(ref _checkpointWrites) == 2
                ? throw new IOException("synthetic checkpoint failure") : inner.SetAsync(key, value, cancellationToken);
        public Task RemoveAsync(string key, CancellationToken cancellationToken = default) => inner.RemoveAsync(key, cancellationToken);
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) => inner.ListKeysAsync(prefix, cancellationToken);
    }
}
