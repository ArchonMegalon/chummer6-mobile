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
        var existing = await _browserStore.GetAsync<OfflineLedgerEnvelope>(key, cancellationToken);
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
            await _browserStore.SetAsync(key, reset, cancellationToken);
            return reset;
        }

        await EnsureLedgerMissingAsync(sessionId, key, cancellationToken);
        var created = CreateLedger(sessionId, sceneId, sceneRevision, runtimeFingerprint);
        await _browserStore.SetAsync(key, created, cancellationToken);
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

        await _browserStore.SetAsync(PlayBrowserStateKeys.Ledger(sessionId), updated, cancellationToken);
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
        var current = await _browserStore.GetAsync<OfflineLedgerEnvelope>(key, cancellationToken);
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

        await _browserStore.SetAsync(key, updated, cancellationToken);
        return updated;
    }

    private async Task<OfflineLedgerEnvelope?> GetValidatedExistingAsync(
        string sessionId,
        CancellationToken cancellationToken
    )
    {
        var key = PlayBrowserStateKeys.Ledger(sessionId);
        var existing = await _browserStore.GetAsync<OfflineLedgerEnvelope>(key, cancellationToken);
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
