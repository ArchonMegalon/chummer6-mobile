namespace Chummer.Play.Web.BrowserState;

public static class PlayBrowserStateKeys
{
    public const string RuntimeBundlePrefix = "play:runtime-bundle:";
    public const string ContinuityPrefix = "play:continuity:";

    public static string Ledger(string sessionId) => $"play:ledger:{sessionId}";

    public static string Checkpoint(string sessionId) => $"play:checkpoint:{sessionId}";

    public static string RuntimeBundle(string sessionId) => $"{RuntimeBundlePrefix}{sessionId}";

    public static string Continuity(string sessionId) => $"{ContinuityPrefix}{sessionId}";

    public static string SessionIdFromRuntimeBundleKey(string key) =>
        key.StartsWith(RuntimeBundlePrefix, StringComparison.Ordinal) ? key[RuntimeBundlePrefix.Length..] : key;
}
