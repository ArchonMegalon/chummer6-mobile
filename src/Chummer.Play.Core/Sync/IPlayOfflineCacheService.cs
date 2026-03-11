using Chummer.Play.Core.PlayApi;

namespace Chummer.Play.Core.Sync;

public interface IPlayOfflineCacheService
{
    Task<PlayCachePressureSnapshot> CacheRuntimeBundleAsync(
        string sessionId,
        string runtimeFingerprint,
        string sceneRevision,
        string bundleTag,
        CancellationToken cancellationToken = default
    );

    Task<PlayRuntimeBundleMetadata?> GetRuntimeBundleAsync(
        string sessionId,
        CancellationToken cancellationToken = default
    );

    Task SetCheckpointAsync(
        SyncCheckpoint checkpoint,
        CancellationToken cancellationToken = default
    );

    Task<SyncCheckpoint?> GetCheckpointAsync(
        string sessionId,
        CancellationToken cancellationToken = default
    );

    Task<PlayCachePressureSnapshot> GetCachePressureAsync(
        CancellationToken cancellationToken = default
    );
}
