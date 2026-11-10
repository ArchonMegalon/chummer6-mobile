using System.Text;
using System.Text.Json;
using Chummer.Play.Core.Offline;
using Chummer.Play.Web;
using Chummer.Play.Web.BrowserState;

internal static class FileBackedLedgerChecks
{
    private const string SessionId = "file-backed-ledger-regression";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task RunAsync()
    {
        await CheckAsync(nameof(CancellationPreservesPendingEventsAsync), CancellationPreservesPendingEventsAsync);
        await CheckAsync(nameof(FailedWritePreservesPendingEventsAsync), FailedWritePreservesPendingEventsAsync);
        await CheckAsync(nameof(SuccessPersistsAcrossInstancesAsync), SuccessPersistsAcrossInstancesAsync);
        await CheckAsync(nameof(ConcurrentMutationsFailWithoutReplacingAsync), ConcurrentMutationsFailWithoutReplacingAsync);
        await CheckAsync(nameof(CorruptEvidenceIsRetainedAsync), CorruptEvidenceIsRetainedAsync);
        await CheckAsync(nameof(InvalidLedgerIsRetainedAsync), InvalidLedgerIsRetainedAsync);
    }

    private static async Task CancellationPreservesPendingEventsAsync(string root)
    {
        await SeedAsync(root);
        string path = EntryPath(root);
        byte[] original = await File.ReadAllBytesAsync(path);
        string unrelatedTemporaryPath = $"{path}.another-writer.tmp";
        await File.WriteAllTextAsync(unrelatedTemporaryPath, "recovery evidence");
        using CancellationTokenSource cancellation = new();
        TaskCompletionSource beforeCommit = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        FileSystemBrowserKeyValueStore browserStore = new(root, async _ =>
        {
            beforeCommit.SetResult();
            await release.Task.WaitAsync(TimeSpan.FromSeconds(10));
        });
        BrowserSessionEventLogStore ledgerStore = new(browserStore);
        Task write = AppendAsync(ledgerStore, "evt-cancelled", cancellation.Token);
        try
        {
            await beforeCommit.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert(Directory.GetFiles(root, "*.tmp").Length == 2, "write must reach an owned temporary file before cancellation");
            await AssertBytesUnchangedAsync(path, original);
            await AssertPendingAsync(new BrowserSessionEventLogStore(new FileSystemBrowserKeyValueStore(root)), "evt-old");
            cancellation.Cancel();
        }
        finally
        {
            release.TrySetResult();
        }

        await ThrowsAsync<OperationCanceledException>(() => write);
        await AssertBytesUnchangedAsync(path, original);
        await AssertPendingAsync(ledgerStore, "evt-old");
        await AssertPendingAsync(new BrowserSessionEventLogStore(new FileSystemBrowserKeyValueStore(root)), "evt-old");
        Assert(Directory.GetFiles(root, "*.tmp").SequenceEqual([unrelatedTemporaryPath]), "cleanup must remove only this write's temporary file");
    }

    private static async Task FailedWritePreservesPendingEventsAsync(string root)
    {
        await SeedAsync(root);
        byte[] original = await File.ReadAllBytesAsync(EntryPath(root));
        IOException expected = new("controlled failure before replacement");
        FileSystemBrowserKeyValueStore browserStore = new(root, _ => throw expected);
        IOException actual = await ThrowsAsync<IOException>(() => AppendAsync(new BrowserSessionEventLogStore(browserStore), "evt-failed"));
        Assert(ReferenceEquals(expected, actual), "write failures must propagate without substitution");
        await AssertBytesUnchangedAsync(EntryPath(root), original);
        Assert(Directory.GetFiles(root, "*.tmp").Length == 0, "failed write must clean its temporary file");
        await AssertPendingAsync(new BrowserSessionEventLogStore(new FileSystemBrowserKeyValueStore(root)), "evt-old");
    }

    private static async Task SuccessPersistsAcrossInstancesAsync(string root)
    {
        await SeedAsync(root);
        BrowserSessionEventLogStore writer = new(new FileSystemBrowserKeyValueStore(root));
        await AppendAsync(writer, "evt-success");
        await AssertPendingAsync(writer, "evt-old", "evt-success");
        await AssertPendingAsync(new BrowserSessionEventLogStore(new FileSystemBrowserKeyValueStore(root)), "evt-old", "evt-success");
        Assert(Directory.GetFiles(root, "*.tmp").Length == 0, "successful write must consume its temporary file");
        IReadOnlyList<string> keys = await new FileSystemBrowserKeyValueStore(root).ListKeysAsync("play:ledger:");
        Assert(keys.SequenceEqual([PlayBrowserStateKeys.Ledger(SessionId)]), "temporary and lock files must not appear as state keys");
        await new FileSystemBrowserKeyValueStore(root).RemoveAsync(PlayBrowserStateKeys.Ledger(SessionId));
        Assert(await new FileSystemBrowserKeyValueStore(root).GetAsync<OfflineLedgerEnvelope>(PlayBrowserStateKeys.Ledger(SessionId)) is null,
            "explicit removal and missing-key semantics must remain available");
        FileSystemBrowserKeyValueStore nullableStore = new(root);
        await nullableStore.SetAsync<string?>("play:test:null", null);
        Assert(await nullableStore.GetAsync<string>("play:test:null") is null, "non-ledger null values must keep their existing semantics");
    }

    private static async Task ConcurrentMutationsFailWithoutReplacingAsync(string root)
    {
        OfflineLedgerEnvelope original = await SeedAsync(root);
        TaskCompletionSource beforeCommit = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        FileSystemBrowserKeyValueStore writer = new(root, async _ =>
        {
            beforeCommit.SetResult();
            await release.Task.WaitAsync(TimeSpan.FromSeconds(10));
        });
        Task write = AppendAsync(new BrowserSessionEventLogStore(writer), "evt-writer");
        try
        {
            await beforeCommit.Task.WaitAsync(TimeSpan.FromSeconds(10));
            FileSystemBrowserKeyValueStore competitor = new(root);
            await ThrowsAsync<IOException>(() => competitor.SetAsync(PlayBrowserStateKeys.Ledger(SessionId), original));
            await ThrowsAsync<IOException>(() => competitor.RemoveAsync(PlayBrowserStateKeys.Ledger(SessionId)));
            await AssertPendingAsync(new BrowserSessionEventLogStore(competitor), "evt-old");
        }
        finally
        {
            release.TrySetResult();
        }

        await write;
        await AssertPendingAsync(new BrowserSessionEventLogStore(new FileSystemBrowserKeyValueStore(root)), "evt-old", "evt-writer");
        Assert(Directory.GetFiles(root, "*.tmp").Length == 0, "competing writes must not leave temporary files");
    }

    private static async Task CorruptEvidenceIsRetainedAsync(string root)
    {
        OfflineLedgerEnvelope ledger = await SeedAsync(root);
        string key = PlayBrowserStateKeys.Ledger(SessionId);
        string[] corruptEntries =
        [
            "{broken",
            "null",
            JsonSerializer.Serialize(new { key = key + "-wrong", payloadJson = JsonSerializer.Serialize(ledger, JsonOptions) }, JsonOptions),
            JsonSerializer.Serialize(new { key }, JsonOptions),
            JsonSerializer.Serialize(new { key, payloadJson = "{broken" }, JsonOptions),
        ];
        foreach (string corruptEntry in corruptEntries)
        {
            await File.WriteAllTextAsync(EntryPath(root), corruptEntry);
            BrowserSessionEventLogStore store = new(new FileSystemBrowserKeyValueStore(root));
            InvalidDataException error = await ThrowsAsync<InvalidDataException>(() => store.GetExistingAsync(SessionId));
            Assert(error.Message.Contains(EntryPath(root), StringComparison.Ordinal) && error.Message.Contains("recover", StringComparison.Ordinal),
                "corruption must identify the retained recovery file and next action");
            await ThrowsAsync<InvalidDataException>(() => store.GetOrCreateAsync(SessionId, "scene", "revision", "runtime"));
            await ThrowsAsync<InvalidDataException>(() => AppendAsync(store, "evt-must-not-replace"));
            await ThrowsAsync<InvalidDataException>(() => new BrowserSessionEventLogStore(new FileSystemBrowserKeyValueStore(root)).GetExistingAsync(SessionId));
            Assert(await File.ReadAllTextAsync(EntryPath(root)) == corruptEntry, "malformed state must remain byte-for-byte available");
            Assert((await new FileSystemBrowserKeyValueStore(root).ListKeysAsync("play:ledger:")).Contains(key), "corrupt state must remain discoverable");
        }
    }

    private static async Task InvalidLedgerIsRetainedAsync(string root)
    {
        OfflineLedgerEnvelope ledger = await SeedAsync(root);
        OfflineLedgerEnvelope?[] invalidLedgers =
        [
            null,
            ledger with { LastKnownSequence = -1 },
            ledger with { PendingEvents = null! },
            ledger with { PendingEvents = ["evt-old", " "] },
        ];
        foreach (OfflineLedgerEnvelope? invalidLedger in invalidLedgers)
        {
            await new FileSystemBrowserKeyValueStore(root).SetAsync(PlayBrowserStateKeys.Ledger(SessionId), invalidLedger);
            byte[] original = await File.ReadAllBytesAsync(EntryPath(root));
            BrowserSessionEventLogStore store = new(new FileSystemBrowserKeyValueStore(root));
            await ThrowsAsync<InvalidOperationException>(() => store.GetExistingAsync(SessionId));
            await ThrowsAsync<InvalidOperationException>(() => store.GetOrCreateAsync(SessionId, "scene", "revision", "runtime"));
            await ThrowsAsync<InvalidOperationException>(() => AppendAsync(store, "evt-must-not-replace"));
            await ThrowsAsync<InvalidOperationException>(() => store.AcknowledgePendingEventsAsync(SessionId, 1));
            await ThrowsAsync<InvalidOperationException>(() => new BrowserSessionEventLogStore(new FileSystemBrowserKeyValueStore(root)).GetExistingAsync(SessionId));
            await AssertBytesUnchangedAsync(EntryPath(root), original);
        }
    }

    private static Task<OfflineLedgerEnvelope> SeedAsync(string root)
        => AppendAsync(new BrowserSessionEventLogStore(new FileSystemBrowserKeyValueStore(root)), "evt-old");

    private static Task<OfflineLedgerEnvelope> AppendAsync(BrowserSessionEventLogStore store, string pendingEvent, CancellationToken cancellationToken = default)
        => store.AppendPendingEventsAsync(SessionId, "scene", "revision", "runtime", [pendingEvent], 1, cancellationToken);

    private static async Task AssertPendingAsync(BrowserSessionEventLogStore store, params string[] expected)
    {
        OfflineLedgerEnvelope? ledger = await store.GetExistingAsync(SessionId);
        Assert(ledger is not null && ledger.PendingEvents.SequenceEqual(expected), "pending events must survive with their original order");
        Assert(ledger!.LastKnownSequence == 1, "pending-event sequence must survive");
    }

    private static async Task AssertBytesUnchangedAsync(string path, byte[] original)
    {
        byte[] current = await File.ReadAllBytesAsync(path);
        Assert(original.SequenceEqual(current), "stored bytes must remain unchanged");
    }

    private static string EntryPath(string root)
        => Path.Combine(root, Convert.ToBase64String(Encoding.UTF8.GetBytes(PlayBrowserStateKeys.Ledger(SessionId)))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_') + ".json");

    private static async Task<TException> ThrowsAsync<TException>(Func<Task> action) where TException : Exception
    {
        try
        {
            await action();
        }
        catch (TException exception)
        {
            return exception;
        }

        throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static async Task CheckAsync(string name, Func<string, Task> check)
    {
        string root = Path.Combine(Path.GetTempPath(), "chummer-file-backed-ledger-checks", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            await check(root);
            Console.WriteLine($"PASS {name}");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
