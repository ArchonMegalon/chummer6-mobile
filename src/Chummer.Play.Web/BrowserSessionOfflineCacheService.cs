using Chummer.Play.Core.Sync;
using Chummer.Play.Core.PlayApi;
using Chummer.Play.Web.BrowserState;

namespace Chummer.Play.Web;

public sealed class BrowserSessionOfflineCacheService : IPlayOfflineCacheService
{
    private const int RuntimeBundleQuota = 8;
    private readonly IBrowserKeyValueStore _browserStore;

    public BrowserSessionOfflineCacheService(IBrowserKeyValueStore browserStore)
    {
        _browserStore = browserStore;
    }

    public IReadOnlyList<string> CachePolicies =>
    [
        "cache the shell and role-safe play assets",
        "cache runtime bundle metadata for reconnect",
        "cache media with bounded lifecycle rules",
    ];

    public async Task<PlayCachePressureSnapshot> CacheRuntimeBundleAsync(
        string sessionId,
        string runtimeFingerprint,
        string sceneRevision,
        string bundleTag,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(runtimeFingerprint);
        ArgumentException.ThrowIfNullOrWhiteSpace(sceneRevision);
        ArgumentException.ThrowIfNullOrWhiteSpace(bundleTag);

        return await CacheRuntimeBundleAsync(
            new RuntimeBundleCacheEntry(
                sessionId,
                runtimeFingerprint,
                sceneRevision,
                bundleTag,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow
            ),
            cancellationToken
        );
    }

    public async Task<PlayCachePressureSnapshot> CacheRuntimeBundleAsync(
        RuntimeBundleCacheEntry entry,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(entry);
        ValidateRuntimeBundleEntry(entry);

        var evictedSessionIds = new List<string>();
        var runtimeBundleKey = PlayBrowserStateKeys.RuntimeBundle(entry.SessionId);
        var existingEntry = await _browserStore.GetAsync<RuntimeBundleCacheEntry>(runtimeBundleKey, cancellationToken);
        if (existingEntry is null)
        {
            await RemoveIfKeyPresentAsync(runtimeBundleKey, cancellationToken);
        }
        else
        {
            try
            {
                ValidateRuntimeBundleEntry(existingEntry);
            }
            catch (ArgumentException)
            {
                await _browserStore.RemoveAsync(runtimeBundleKey, cancellationToken);
                existingEntry = null;
            }
        }

        var runtimeBundleEntries = await GetValidatedRuntimeBundleEntriesAsync(cancellationToken);

        if (existingEntry is null && runtimeBundleEntries.Count >= RuntimeBundleQuota)
        {
            var neededEvictions = (runtimeBundleEntries.Count - RuntimeBundleQuota) + 1;
            foreach (var candidate in runtimeBundleEntries.OrderBy(item => item.Entry.LastValidatedAtUtc).Take(neededEvictions))
            {
                await _browserStore.RemoveAsync(candidate.Key, cancellationToken);
                evictedSessionIds.Add(PlayBrowserStateKeys.SessionIdFromRuntimeBundleKey(candidate.Key));
            }
        }

        await _browserStore.SetAsync(runtimeBundleKey, entry, cancellationToken);
        var finalEntries = await GetValidatedRuntimeBundleEntriesAsync(cancellationToken);
        return BuildPressureSnapshot(finalEntries.Count, evictedSessionIds.Count, evictedSessionIds);
    }

    public async Task<PlayRuntimeBundleMetadata?> GetRuntimeBundleAsync(
        string sessionId,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        var key = PlayBrowserStateKeys.RuntimeBundle(sessionId);
        var entry = await _browserStore.GetAsync<RuntimeBundleCacheEntry>(
            key,
            cancellationToken
        );
        if (entry is null)
        {
            await RemoveIfKeyPresentAsync(key, cancellationToken);
            return null;
        }

        try
        {
            ValidateRuntimeBundleEntry(entry);
        }
        catch (ArgumentException)
        {
            await _browserStore.RemoveAsync(key, cancellationToken);
            return null;
        }

        return entry is null ? null : ToRuntimeBundleMetadata(entry);
    }

    public Task SetCheckpointAsync(SyncCheckpoint checkpoint, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(checkpoint);
        ValidateCheckpoint(checkpoint);
        return _browserStore.SetAsync(PlayBrowserStateKeys.Checkpoint(checkpoint.SessionId), checkpoint, cancellationToken);
    }

    public Task<SyncCheckpoint?> GetCheckpointAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        return GetValidatedCheckpointAsync(sessionId, cancellationToken);
    }

    public async Task<PlayCachePressureSnapshot> GetCachePressureAsync(CancellationToken cancellationToken = default)
    {
        var entries = await GetValidatedRuntimeBundleEntriesAsync(cancellationToken);
        return BuildPressureSnapshot(entries.Count, 0, Array.Empty<string>());
    }

    public static PlayRuntimeBundleMetadata ToRuntimeBundleMetadata(RuntimeBundleCacheEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return new PlayRuntimeBundleMetadata(
            entry.RuntimeFingerprint,
            entry.SceneRevision,
            entry.BundleTag,
            entry.CachedAtUtc,
            entry.LastValidatedAtUtc
        );
    }

    private static PlayCachePressureSnapshot BuildPressureSnapshot(
        int runtimeBundleCount,
        int evictedEntryCount,
        IReadOnlyList<string> evictedSessionIds
    ) =>
        new(
            runtimeBundleCount,
            RuntimeBundleQuota,
            runtimeBundleCount >= (int)Math.Ceiling(RuntimeBundleQuota * 0.75d),
            evictedEntryCount,
            evictedSessionIds,
            DateTimeOffset.UtcNow
        );

    private static void ValidateCheckpoint(SyncCheckpoint checkpoint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(checkpoint.SessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(checkpoint.SceneId);
        ArgumentException.ThrowIfNullOrWhiteSpace(checkpoint.SceneRevision);
        ArgumentException.ThrowIfNullOrWhiteSpace(checkpoint.ProjectionFingerprint);
        if (checkpoint.AppliedThroughSequence < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(checkpoint.AppliedThroughSequence),
                checkpoint.AppliedThroughSequence,
                "Applied-through sequence cannot be negative."
            );
        }
    }

    private static void ValidateRuntimeBundleEntry(RuntimeBundleCacheEntry entry)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entry.SessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(entry.RuntimeFingerprint);
        ArgumentException.ThrowIfNullOrWhiteSpace(entry.SceneRevision);
        ArgumentException.ThrowIfNullOrWhiteSpace(entry.BundleTag);
    }

    private async Task<SyncCheckpoint?> GetValidatedCheckpointAsync(
        string sessionId,
        CancellationToken cancellationToken
    )
    {
        var key = PlayBrowserStateKeys.Checkpoint(sessionId);
        var checkpoint = await _browserStore.GetAsync<SyncCheckpoint>(key, cancellationToken);
        if (checkpoint is null)
        {
            await RemoveIfKeyPresentAsync(key, cancellationToken);
            return null;
        }

        try
        {
            ValidateCheckpoint(checkpoint);
        }
        catch (ArgumentException)
        {
            await _browserStore.RemoveAsync(key, cancellationToken);
            return null;
        }

        return checkpoint;
    }

    private async Task<IReadOnlyList<(string Key, RuntimeBundleCacheEntry Entry)>> GetValidatedRuntimeBundleEntriesAsync(
        CancellationToken cancellationToken
    )
    {
        var keys = await _browserStore.ListKeysAsync(PlayBrowserStateKeys.RuntimeBundlePrefix, cancellationToken);
        var entries = new List<(string Key, RuntimeBundleCacheEntry Entry)>(keys.Count);
        foreach (var key in keys)
        {
            var entry = await _browserStore.GetAsync<RuntimeBundleCacheEntry>(key, cancellationToken);
            if (entry is null)
            {
                await _browserStore.RemoveAsync(key, cancellationToken);
                continue;
            }

            try
            {
                ValidateRuntimeBundleEntry(entry);
            }
            catch (ArgumentException)
            {
                await _browserStore.RemoveAsync(key, cancellationToken);
                continue;
            }

            entries.Add((key, entry));
        }

        return entries;
    }

    private async Task RemoveIfKeyPresentAsync(string key, CancellationToken cancellationToken)
    {
        var matchingKeys = await _browserStore.ListKeysAsync(key, cancellationToken);
        if (matchingKeys.Contains(key, StringComparer.Ordinal))
        {
            await _browserStore.RemoveAsync(key, cancellationToken);
        }
    }
}
