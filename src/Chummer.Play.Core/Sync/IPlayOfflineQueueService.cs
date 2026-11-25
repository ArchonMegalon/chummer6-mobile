using Chummer.Play.Core.PlayApi;
using Chummer.Play.Core.Offline;

namespace Chummer.Play.Core.Sync;

public interface IPlayOfflineQueueService
{
    Task<OfflineQueueEnqueueResult> EnqueueReplayBatchAsync(
        string sessionId, string scope, PlayReplayBatch batch,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("This queue does not support durable replay identities.");

    Task<OfflineQueueEnqueueResult> EnqueueAsync(
        EngineSessionCursor cursor,
        string queuedEvent,
        CancellationToken cancellationToken = default
    );

    Task<OfflineQueueSyncResult> SyncReplayAsync(
        PlaySyncRequest request,
        CancellationToken cancellationToken = default
    );
}
