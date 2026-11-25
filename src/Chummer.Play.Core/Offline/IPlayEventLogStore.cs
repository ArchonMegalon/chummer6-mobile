namespace Chummer.Play.Core.Offline;

public interface IPlayEventLogStore
{
    Task<OfflineLedgerEnvelope> AppendReplayBatchAsync(
        string sessionId, string sceneId, string sceneRevision, string runtimeFingerprint,
        string scope, PlayReplayBatch batch, long appliedThroughSequence,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("This ledger does not support durable replay identities.");

    Task<OfflineLedgerEnvelope> GetOrCreateAsync(
        string sessionId,
        string sceneId,
        string sceneRevision,
        string runtimeFingerprint,
        CancellationToken cancellationToken = default
    );

    Task<OfflineLedgerEnvelope?> GetExistingAsync(
        string sessionId,
        CancellationToken cancellationToken = default
    );

    Task<OfflineLedgerEnvelope> AppendPendingEventsAsync(
        string sessionId,
        string sceneId,
        string sceneRevision,
        string runtimeFingerprint,
        IEnumerable<string> pendingEvents,
        long lastKnownSequence,
        CancellationToken cancellationToken = default
    );

    Task<OfflineLedgerEnvelope> AcknowledgePendingEventsAsync(
        string sessionId,
        int acceptedEventCount,
        CancellationToken cancellationToken = default
    );
}
