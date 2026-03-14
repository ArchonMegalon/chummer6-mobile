using Chummer.Play.Core.Application;

namespace Chummer.Play.Web.BrowserState;

public sealed record ObserverContinuityEntry(
    string SessionId,
    string SceneId,
    string SceneRevision,
    string RuntimeFingerprint,
    string ObserverId,
    string DeviceId,
    PlaySurfaceRole Role,
    long ObservedThroughSequence,
    DateTimeOffset ObservedAtUtc,
    string ContinuityToken
);
