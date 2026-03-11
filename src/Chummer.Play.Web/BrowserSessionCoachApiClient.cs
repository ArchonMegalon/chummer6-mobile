namespace Chummer.Play.Web;

public sealed class BrowserSessionCoachApiClient
{
    public Task<IReadOnlyList<string>> GetHintsAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        IReadOnlyList<string> hints =
        [
            "Coach hints will move behind play-safe contracts.",
            "Rules answers in play mode must remain evidence-bearing.",
        ];
        return Task.FromResult(hints);
    }
}
