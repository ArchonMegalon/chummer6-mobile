namespace Chummer.Play.Web.BrowserState;

public sealed record RuntimeBundleCacheEntry(
    string SessionId,
    string RuntimeFingerprint,
    string SceneRevision,
    string BundleTag,
    DateTimeOffset CachedAtUtc,
    DateTimeOffset LastValidatedAtUtc
);
