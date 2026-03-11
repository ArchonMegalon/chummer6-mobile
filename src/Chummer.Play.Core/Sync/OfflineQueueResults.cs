using Chummer.Play.Core.Offline;

namespace Chummer.Play.Core.Sync;

public sealed record OfflineQueueEnqueueResult(
    OfflineLedgerEnvelope Ledger,
    SyncCheckpoint Checkpoint,
    long AppliedThroughSequence
);

public sealed record OfflineQueueSyncResult(
    OfflineLedgerEnvelope Ledger,
    SyncCheckpoint Checkpoint,
    long AppliedThroughSequence,
    int AcceptedEventCount
);
