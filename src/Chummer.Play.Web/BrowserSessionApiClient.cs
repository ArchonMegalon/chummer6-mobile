using System.Net.Http.Json;
using System.Net;
using Chummer.Play.Core.Application;
using Chummer.Play.Core.PlayApi;
using Chummer.Play.Core.Sync;

namespace Chummer.Play.Web;

public sealed class BrowserSessionApiClient
{
    private readonly HttpClient _httpClient;

    public BrowserSessionApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PlayBootstrapResponse> BootstrapAsync(
        PlayBootstrapRequest request,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = $"?sessionId={Uri.EscapeDataString(request.Session.SessionId)}&role={Uri.EscapeDataString(request.Role.ToString())}&sceneId={Uri.EscapeDataString(request.Session.SceneId)}&sceneRevision={Uri.EscapeDataString(request.Session.SceneRevision)}&runtimeFingerprint={Uri.EscapeDataString(request.Session.RuntimeFingerprint)}";
        return await _httpClient.GetFromJsonAsync<PlayBootstrapResponse>(PlayApiRoutes.Bootstrap + query, cancellationToken)
            ?? throw new InvalidOperationException("Play bootstrap response was empty.");
    }

    public async Task<PlaySessionProjection> GetProjectionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        return await _httpClient.GetFromJsonAsync<PlaySessionProjection>(
                PlayApiRoutes.Projection.Replace("{sessionId}", Uri.EscapeDataString(sessionId)),
                cancellationToken
            )
            ?? throw new InvalidOperationException("Play projection response was empty.");
    }

    public async Task<PlayReconnectResponse> ReconnectAsync(
        PlayReconnectRequest request,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        using var response = await _httpClient.PostAsJsonAsync(PlayApiRoutes.Reconnect, request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var stalePayload = await response.Content.ReadFromJsonAsync<ReconnectConflictPayload>(cancellationToken: cancellationToken);
            if (stalePayload is null)
            {
                throw new InvalidOperationException("Play reconnect stale response was empty.");
            }

            throw new PlayReconnectStaleException(
                stalePayload.Error,
                stalePayload.Projection,
                stalePayload.Checkpoint
            );
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PlayReconnectResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Play reconnect response was empty.");
    }

    public async Task<PlaySyncResponse> SyncAsync(PlaySyncRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var response = await _httpClient.PostAsJsonAsync(PlayApiRoutes.Sync, request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PlaySyncResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Play sync response was empty.");
    }

    public async Task<PlayQuickActionResponse> ExecuteQuickActionAsync(
        PlayQuickActionRequest request,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        using var response = await _httpClient.PostAsJsonAsync(PlayApiRoutes.QuickAction, request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PlayQuickActionResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Play quick-action response was empty.");
    }

    public async Task<PlayResumeResponse> ResumeAsync(
        string sessionId,
        PlaySurfaceRole role,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        var route = PlayApiRoutes.Resume.Replace("{sessionId}", Uri.EscapeDataString(sessionId));
        var query = $"?role={Uri.EscapeDataString(role.ToString())}";
        return await _httpClient.GetFromJsonAsync<PlayResumeResponse>(route + query, cancellationToken)
            ?? throw new InvalidOperationException("Play resume response was empty.");
    }

    public async Task<PlayCachePressureSnapshot> GetCachePressureAsync(
        string sessionId,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        var route = PlayApiRoutes.CachePressure.Replace("{sessionId}", Uri.EscapeDataString(sessionId));
        using var response = await _httpClient.GetAsync(route, cancellationToken);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<PlayCachePressureSnapshot>(cancellationToken: cancellationToken);
        return payload ?? throw new InvalidOperationException("Play cache-pressure response was empty.");
    }

    private sealed record ReconnectConflictPayload(
        string Error,
        bool Stale,
        PlaySessionProjection Projection,
        SyncCheckpoint Checkpoint
    );
}

public sealed class PlayReconnectStaleException : Exception
{
    public PlayReconnectStaleException(
        string message,
        PlaySessionProjection projection,
        SyncCheckpoint checkpoint
    )
        : base(message)
    {
        Projection = projection;
        Checkpoint = checkpoint;
    }

    public PlaySessionProjection Projection { get; }

    public SyncCheckpoint Checkpoint { get; }
}
