using Chummer.Play.Core.PlayApi;

namespace Chummer.Play.Core.Sync;

public interface IPlayOfflineQueueService
{
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
