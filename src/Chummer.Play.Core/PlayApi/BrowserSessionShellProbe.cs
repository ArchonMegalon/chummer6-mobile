namespace Chummer.Play.Core.PlayApi;

public sealed record BrowserSessionShellProbe(
    bool OfflineCapable,
    bool RuntimeBundleCached,
    bool MediaCacheEnabled
);
