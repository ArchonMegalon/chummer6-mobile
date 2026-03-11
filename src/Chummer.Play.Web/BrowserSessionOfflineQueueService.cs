using Chummer.Play.Core.Offline;
using Chummer.Play.Core.PlayApi;
using Chummer.Play.Core.Sync;
using System.Collections.Concurrent;

namespace Chummer.Play.Web;

public sealed class BrowserSessionOfflineQueueService
    : IPlayOfflineQueueService
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> SessionEnqueueLocks = new(StringComparer.Ordinal);
    private readonly IPlayEventLogStore _eventLogStore;
    private readonly IPlayOfflineCacheService _offlineCacheService;

    public BrowserSessionOfflineQueueService(
        IPlayEventLogStore eventLogStore,
        IPlayOfflineCacheService offlineCacheService
    )
    {
        _eventLogStore = eventLogStore;
        _offlineCacheService = offlineCacheService;
    }

    public async Task<OfflineQueueEnqueueResult> EnqueueAsync(
        EngineSessionCursor cursor,
        string queuedEvent,
        CancellationToken cancellationToken = default
    )
    {
        ValidateCursor(cursor, nameof(cursor));
        ArgumentException.ThrowIfNullOrWhiteSpace(queuedEvent);

        var session = cursor.Session;
        var enqueueLock = GetSessionLock(session.SessionId);
        await enqueueLock.WaitAsync(cancellationToken);
        try
        {
            await EnsureStoredLineageAlignedAsync(session, cancellationToken);
            var ledgerBeforeAppend = await _eventLogStore.GetOrCreateAsync(
                session.SessionId,
                session.SceneId,
                session.SceneRevision,
                session.RuntimeFingerprint,
                cancellationToken
            );
            var nextSequence = Math.Max(ledgerBeforeAppend.LastKnownSequence, cursor.AppliedThroughSequence) + 1;
            var ledger = await _eventLogStore.AppendPendingEventsAsync(
                session.SessionId,
                session.SceneId,
                session.SceneRevision,
                session.RuntimeFingerprint,
                [queuedEvent],
                nextSequence,
                cancellationToken
            );

            var checkpoint = new SyncCheckpoint(
                session.SessionId,
                session.SceneId,
                session.SceneRevision,
                session.RuntimeFingerprint,
                nextSequence,
                DateTimeOffset.UtcNow
            );
            await _offlineCacheService.SetCheckpointAsync(checkpoint, cancellationToken);

            return new OfflineQueueEnqueueResult(ledger, checkpoint, nextSequence);
        }
        finally
        {
            enqueueLock.Release();
        }
    }

    public async Task<OfflineQueueSyncResult> SyncReplayAsync(
        PlaySyncRequest request,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateCursor(request.Cursor, nameof(request.Cursor));
        ValidatePendingEvents(request.PendingEvents);

        var requestSession = request.Cursor.Session;
        var sessionLock = GetSessionLock(requestSession.SessionId);
        await sessionLock.WaitAsync(cancellationToken);
        try
        {
            await EnsureStoredLineageAlignedAsync(requestSession, cancellationToken);
            var currentLedger = await _eventLogStore.GetOrCreateAsync(
                requestSession.SessionId,
                requestSession.SceneId,
                requestSession.SceneRevision,
                requestSession.RuntimeFingerprint,
                cancellationToken
            );
            var acceptedEventCount = CountAcceptedEventPrefix(request.PendingEvents, currentLedger.PendingEvents);
            var acknowledgedLedger = await _eventLogStore.AcknowledgePendingEventsAsync(
                requestSession.SessionId,
                acceptedEventCount,
                cancellationToken
            );
            var nextSequence = Math.Max(request.Cursor.AppliedThroughSequence, acknowledgedLedger.LastKnownSequence);
            var checkpoint = new SyncCheckpoint(
                requestSession.SessionId,
                requestSession.SceneId,
                requestSession.SceneRevision,
                requestSession.RuntimeFingerprint,
                nextSequence,
                DateTimeOffset.UtcNow
            );
            await _offlineCacheService.SetCheckpointAsync(checkpoint, cancellationToken);

            return new OfflineQueueSyncResult(acknowledgedLedger, checkpoint, nextSequence, acceptedEventCount);
        }
        finally
        {
            sessionLock.Release();
        }
    }

    private static SemaphoreSlim GetSessionLock(string sessionId) =>
        SessionEnqueueLocks.GetOrAdd(sessionId, static _ => new SemaphoreSlim(1, 1));

    private async Task EnsureStoredLineageAlignedAsync(EngineSessionEnvelope session, CancellationToken cancellationToken)
    {
        var currentLedger = await _eventLogStore.GetExistingAsync(session.SessionId, cancellationToken);
        var checkpoint = await _offlineCacheService.GetCheckpointAsync(session.SessionId, cancellationToken);
        if (!SessionLineage.IsStoredLineageAligned(session, checkpoint, currentLedger))
        {
            throw new InvalidOperationException(
                $"Stored lineage is stale for session '{session.SessionId}'. Reconnect before mutating the offline queue."
            );
        }
    }

    private static int CountAcceptedEventPrefix(IReadOnlyList<string> requestedPendingEvents, IReadOnlyList<string> ledgerPendingEvents)
    {
        var max = Math.Min(requestedPendingEvents.Count, ledgerPendingEvents.Count);
        var accepted = 0;
        for (var i = 0; i < max; i++)
        {
            if (!StringComparer.Ordinal.Equals(requestedPendingEvents[i], ledgerPendingEvents[i]))
            {
                break;
            }

            accepted++;
        }

        return accepted;
    }

    private static void ValidatePendingEvents(IReadOnlyList<string>? pendingEvents)
    {
        if (pendingEvents is null)
        {
            throw new ArgumentException("pending events payload is required.", nameof(pendingEvents));
        }

        for (var i = 0; i < pendingEvents.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(pendingEvents[i]))
            {
                throw new ArgumentException("pending events cannot contain blank values.", nameof(pendingEvents));
            }
        }
    }

    private static void ValidateCursor(EngineSessionCursor cursor, string paramName)
    {
        ArgumentNullException.ThrowIfNull(cursor, paramName);
        if (cursor.AppliedThroughSequence < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, cursor.AppliedThroughSequence, "Applied-through sequence cannot be negative.");
        }
    }
}
