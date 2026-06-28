using Chummer.Play.Core.Application;

namespace Chummer.Play.Web.BrowserState;

public static class PlayBrowserStateKeys
{
    public const string RuntimeBundlePrefix = "play:runtime-bundle:";
    public const string ContinuityPrefix = "play:continuity:";
    public const string TurnCompanionPrefix = "play:turn-companion:";

    public static string Ledger(string sessionId) => $"play:ledger:{sessionId}";

    public static string Checkpoint(string sessionId) => $"play:checkpoint:{sessionId}";

    public static string RuntimeBundle(string sessionId) => $"{RuntimeBundlePrefix}{sessionId}";

    public static string Continuity(string sessionId) => $"{ContinuityPrefix}{sessionId}";

    public static string TurnCompanion(string sessionId) => $"{TurnCompanionPrefix}{sessionId}";

    public static string TurnCompanion(
        string sessionId,
        PlaySurfaceRole role,
        string? deviceId)
    {
        string normalizedDeviceId = string.IsNullOrWhiteSpace(deviceId)
            ? "unclaimed"
            : deviceId.Trim();
        return $"{TurnCompanionPrefix}{sessionId}:{role}:{normalizedDeviceId}";
    }

    public static string SessionIdFromRuntimeBundleKey(string key) =>
        key.StartsWith(RuntimeBundlePrefix, StringComparison.Ordinal) ? key[RuntimeBundlePrefix.Length..] : key;
}
