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
            if (SessionLineage.IsLedgerAligned(existing, sessionId, sceneId, sceneRevision, runtimeFingerprint))
            {
                return existing;
            }

            var reset = CreateLedger(sessionId, sceneId, sceneRevision, runtimeFingerprint);
            await _browserStore.SetAsync(key, reset, cancellationToken);
            return reset;
        }

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
        return _browserStore.GetAsync<OfflineLedgerEnvelope>(PlayBrowserStateKeys.Ledger(sessionId), cancellationToken);
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
            throw new InvalidOperationException($"No offline ledger exists for session '{sessionId}'.");
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
