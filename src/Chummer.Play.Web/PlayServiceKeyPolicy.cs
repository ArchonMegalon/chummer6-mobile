using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Chummer.Play.Web;

public static class PlayServiceKeyPolicy
{
    public const int MinimumKeyLength = 32;

    private static readonly string[] PlaceholderFragments =
    [
        "public-edge-play-api-key",
        "change-me",
        "changeme",
        "default",
        "example",
        "placeholder"
    ];

    public static void ValidateProductionReadiness(
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        if (!environment.IsProduction())
        {
            return;
        }

        string key = configuration["CHUMMER_PLAY_API_KEY"]?.Trim() ?? string.Empty;
        if (!IsStrong(key))
        {
            throw new InvalidOperationException(
                $"CHUMMER_PLAY_API_KEY must be an explicit, non-placeholder secret with at least {MinimumKeyLength} characters in Production.");
        }
    }

    public static bool IsStrong(string? key)
    {
        string value = key?.Trim() ?? string.Empty;
        if (value.Length < MinimumKeyLength || value.Distinct().Count() < 12)
        {
            return false;
        }

        return !PlaceholderFragments.Any(fragment =>
            value.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }
}
