using Chummer.Play.Core.Offline;
using Chummer.Play.Web.BrowserState;

namespace Chummer.Play.Web;

public sealed class BrowserSessionEventLogStore : IPlayEventLogStore
{
    private readonly IBrowserKeyValueStore _browserStore;

    public BrowserSessionEventLogStore(IBrowserKeyValueStore browserStore)
    {
        _browserStore = browserStore;
    }

    public async Task<OfflineLedgerEnvelope> GetOrCreateAsync(
        string sessionId,
        string sceneId,
        string sceneRevision,
        string runtimeFingerprint,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sceneId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sceneRevision);
        ArgumentException.ThrowIfNullOrWhiteSpace(runtimeFingerprint);
        cancellationToken.ThrowIfCancellationRequested();

        var key = PlayBrowserStateKeys.Ledger(sessionId);
        var existing = await ReadLedgerAsync(sessionId, cancellationToken);
        if (existing is not null)
        {
            if (!IsLedgerValid(existing))
            {
                throw MalformedLedger(sessionId);
            }

            if (SessionLineage.IsLedgerAligned(existing, sessionId, sceneId, sceneRevision, runtimeFingerprint))
            {
                return existing;
            }

            var reset = CreateLedger(sessionId, sceneId, sceneRevision, runtimeFingerprint);
            await SaveLedgerAsync(reset, cancellationToken);
            return reset;
        }

        await EnsureLedgerMissingAsync(sessionId, key, cancellationToken);
        var created = CreateLedger(sessionId, sceneId, sceneRevision, runtimeFingerprint);
        await SaveLedgerAsync(created, cancellationToken);
        return created;
    }

    public Task<OfflineLedgerEnvelope?> GetExistingAsync(
        string sessionId,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        return GetValidatedExistingAsync(sessionId, cancellationToken);
    }

    public async Task<OfflineLedgerEnvelope> AppendPendingEventsAsync(
        string sessionId,
        string sceneId,
        string sceneRevision,
        string runtimeFingerprint,
        IEnumerable<string> pendingEvents,
        long lastKnownSequence,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sceneId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sceneRevision);
        ArgumentException.ThrowIfNullOrWhiteSpace(runtimeFingerprint);
        ArgumentNullException.ThrowIfNull(pendingEvents);
        if (lastKnownSequence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lastKnownSequence), lastKnownSequence, "Last-known sequence cannot be negative.");
        }

        var pendingEventList = pendingEvents.ToArray();
        if (pendingEventList.Length == 0)
        {
            throw new ArgumentException("pending events payload cannot be empty.", nameof(pendingEvents));
        }

        for (var i = 0; i < pendingEventList.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(pendingEventList[i]))
            {
                throw new ArgumentException("pending events cannot contain blank values.", nameof(pendingEvents));
            }
        }

        var current = await GetOrCreateAsync(sessionId, sceneId, sceneRevision, runtimeFingerprint, cancellationToken);
        if (lastKnownSequence < current.LastKnownSequence)
        {
            throw new InvalidOperationException(
                $"Last-known sequence {lastKnownSequence} cannot regress below stored sequence {current.LastKnownSequence} for session '{sessionId}'."
            );
        }

        var updated = current with
        {
            SceneId = sceneId,
            SceneRevision = sceneRevision,
            RuntimeFingerprint = runtimeFingerprint,
            PendingEvents = current.PendingEvents.Concat(pendingEventList).ToArray(),
            LastKnownSequence = lastKnownSequence,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };

        await SaveLedgerAsync(updated, cancellationToken);
        return updated;
    }

    public async Task<OfflineLedgerEnvelope> AcknowledgePendingEventsAsync(
        string sessionId,
        int acceptedEventCount,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        if (acceptedEventCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(acceptedEventCount), acceptedEventCount, "Accepted event count cannot be negative.");
        }

        var key = PlayBrowserStateKeys.Ledger(sessionId);
        var current = await ReadLedgerAsync(sessionId, cancellationToken);
        if (current is null)
        {
            await EnsureLedgerMissingAsync(sessionId, key, cancellationToken);
            throw new InvalidOperationException($"No offline ledger exists for session '{sessionId}'.");
        }

        if (!IsLedgerValid(current))
        {
            throw MalformedLedger(sessionId);
        }

        var trimCount = Math.Min(acceptedEventCount, current.PendingEvents.Count);
        var updated = current with
        {
            PendingEvents = current.PendingEvents.Skip(trimCount).ToArray(),
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            LastSyncedAtUtc = DateTimeOffset.UtcNow,
            LastAcceptedEventCount = trimCount,
        };

        await SaveLedgerAsync(updated, cancellationToken);
        return updated;
    }

    private async Task<OfflineLedgerEnvelope?> GetValidatedExistingAsync(
        string sessionId,
        CancellationToken cancellationToken
    )
    {
        var key = PlayBrowserStateKeys.Ledger(sessionId);
        var existing = await ReadLedgerAsync(sessionId, cancellationToken);
        if (existing is null)
        {
            await EnsureLedgerMissingAsync(sessionId, key, cancellationToken);
            return null;
        }

        if (!IsLedgerValid(existing))
        {
            throw MalformedLedger(sessionId);
        }

        return existing;
    }

    public async Task<OfflineLedgerEnvelope> AppendReplayBatchAsync(
        string sessionId, string sceneId, string sceneRevision, string runtimeFingerprint,
        string scope, PlayReplayBatch batch, long appliedThroughSequence,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);
        ValidateReplayBatch(batch);
        if (appliedThroughSequence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(appliedThroughSequence));
        }

        batch = batch with { Events = batch.Events.ToArray() };
        var current = await GetOrCreateAsync(sessionId, sceneId, sceneRevision, runtimeFingerprint, cancellationToken);
        var stored = await ReadReplayStateAsync(sessionId, cancellationToken);
        var receipts = stored?.ReplayBatches ?? Array.Empty<ScopedReplayBatch>();
        var priorBatch = receipts.FirstOrDefault(item => item.Scope == scope && item.Batch.BatchId == batch.BatchId);
        if (priorBatch is not null)
        {
            if (!priorBatch.Batch.Events.SequenceEqual(batch.Events))
            {
                throw new ArgumentException("A replay batch identity cannot be reused with different events.", nameof(batch));
            }

            return current;
        }

        var knownEvents = receipts.Where(item => item.Scope == scope)
            .SelectMany(item => item.Batch.Events)
            .GroupBy(item => item.EventId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().Text, StringComparer.Ordinal);
        foreach (var item in batch.Events)
        {
            if (knownEvents.TryGetValue(item.EventId, out var text) && text != item.Text)
            {
                throw new ArgumentException("A replay event identity cannot be reused with different text.", nameof(batch));
            }
        }

        var added = batch.Events.Where(item => !knownEvents.ContainsKey(item.EventId)).ToArray();
        var updated = current with
        {
            PendingEvents = current.PendingEvents.Concat(added.Select(item => item.Text)).ToArray(),
            LastKnownSequence = added.Length == 0 ? current.LastKnownSequence
                : checked(Math.Max(current.LastKnownSequence, appliedThroughSequence) + added.Length),
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };
        await _browserStore.SetAsync(PlayBrowserStateKeys.Ledger(sessionId), new ReplayLedgerState(
            1, updated, receipts.Append(new ScopedReplayBatch(scope, batch)).ToArray()), cancellationToken);
        return updated;
    }

    internal static void ValidateReplayBatch(PlayReplayBatch batch)
    {
        ArgumentNullException.ThrowIfNull(batch);
        if (!IsReplayIdentity(batch.BatchId) || batch.Events is null || batch.Events.Count is < 1 or > 100
            || batch.Events.Any(item => item is null || !IsReplayIdentity(item.EventId) || string.IsNullOrWhiteSpace(item.Text))
            || batch.Events.Select(item => item.EventId).Distinct(StringComparer.Ordinal).Count() != batch.Events.Count)
        {
            throw new ArgumentException("Replay requires a bounded batch with unique stable event identities.", nameof(batch));
        }
    }

    private static bool IsReplayIdentity(string? value) => value is { Length: > 0 and <= 80 }
        && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');

    private async Task<ReplayLedgerState?> ReadReplayStateAsync(string sessionId, CancellationToken cancellationToken)
    {
        var stored = await _browserStore.GetAsync<ReplayLedgerState>(PlayBrowserStateKeys.Ledger(sessionId), cancellationToken);
        if (stored is null || (stored.ReplayFormatVersion == 0 && stored.Ledger is null && stored.ReplayBatches is null))
        {
            return null; // Pre-upgrade plain ledger. Never infer IDs from its text or position.
        }

        if (stored.ReplayFormatVersion != 1 || stored.Ledger is null || !IsLedgerValid(stored.Ledger)
            || stored.Ledger.SessionId != sessionId || stored.ReplayBatches is null)
        {
            throw MalformedLedger(sessionId);
        }

        try
        {
            var batches = new HashSet<(string, string)>();
            var events = new Dictionary<(string, string), string>();
            foreach (var receipt in stored.ReplayBatches)
            {
                if (receipt is null || string.IsNullOrWhiteSpace(receipt.Scope)) throw MalformedLedger(sessionId);
                ValidateReplayBatch(receipt.Batch);
                if (!batches.Add((receipt.Scope, receipt.Batch.BatchId))) throw MalformedLedger(sessionId);
                foreach (var item in receipt.Batch.Events)
                {
                    if (events.TryGetValue((receipt.Scope, item.EventId), out var text) && text != item.Text)
                        throw MalformedLedger(sessionId);
                    events[(receipt.Scope, item.EventId)] = item.Text;
                }
            }
        }
        catch (ArgumentException)
        {
            throw MalformedLedger(sessionId);
        }

        return stored;
    }

    private async Task<OfflineLedgerEnvelope?> ReadLedgerAsync(string sessionId, CancellationToken cancellationToken)
    {
        var stored = await ReadReplayStateAsync(sessionId, cancellationToken);
        return stored?.Ledger ?? await _browserStore.GetAsync<OfflineLedgerEnvelope>(PlayBrowserStateKeys.Ledger(sessionId), cancellationToken);
    }

    private async Task SaveLedgerAsync(OfflineLedgerEnvelope ledger, CancellationToken cancellationToken)
    {
        var stored = await ReadReplayStateAsync(ledger.SessionId, cancellationToken);
        if (stored is null)
            await _browserStore.SetAsync(PlayBrowserStateKeys.Ledger(ledger.SessionId), ledger, cancellationToken);
        else
            await _browserStore.SetAsync(PlayBrowserStateKeys.Ledger(ledger.SessionId), stored with { Ledger = ledger }, cancellationToken);
    }

    private async Task EnsureLedgerMissingAsync(string sessionId, string key, CancellationToken cancellationToken)
    {
        var matchingKeys = await _browserStore.ListKeysAsync(key, cancellationToken);
        if (matchingKeys.Contains(key, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                $"An offline ledger key exists for session '{sessionId}', but no ledger was read. It has been retained; retry or recover it before creating a new ledger.");
        }
    }

    private static bool IsLedgerValid(OfflineLedgerEnvelope ledger)
    {
        if (string.IsNullOrWhiteSpace(ledger.SessionId)
            || string.IsNullOrWhiteSpace(ledger.SceneId)
            || string.IsNullOrWhiteSpace(ledger.SceneRevision)
            || string.IsNullOrWhiteSpace(ledger.RuntimeFingerprint)
            || ledger.LastKnownSequence < 0
            || ledger.LastAcceptedEventCount < 0
            || ledger.PendingEvents is null)
        {
            return false;
        }

        for (var i = 0; i < ledger.PendingEvents.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(ledger.PendingEvents[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static InvalidOperationException MalformedLedger(string sessionId)
        => new($"Stored offline ledger for session '{sessionId}' is malformed. It has been retained; recover or explicitly remove it before retrying.");

    private static OfflineLedgerEnvelope CreateLedger(
        string sessionId,
        string sceneId,
        string sceneRevision,
        string runtimeFingerprint
    ) =>
        new(
            sessionId,
            sceneId,
            sceneRevision,
            runtimeFingerprint,
            Array.Empty<string>(),
            0,
            DateTimeOffset.UtcNow,
            null,
            0
        );
}
