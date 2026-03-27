using System;
using System.Collections.Generic;
using System.Linq;
using Chummer.Play.Core.PlayApi;

namespace Chummer.Play.Core.Application;

public sealed record PlayCampaignWorkspaceLiteProjection(
    string SessionId,
    PlaySurfaceRole Role,
    string Summary,
    string CurrentSceneSummary,
    string RulePosture,
    string SafeNextAction,
    string ContinuityPosture,
    string CachePosture,
    IReadOnlyList<string> AttentionItems,
    IReadOnlyList<string> QuickActionLabels,
    IReadOnlyList<string> CoachHints);

public static class PlayCampaignWorkspaceLiteProjector
{
    public static PlayCampaignWorkspaceLiteProjection Create(PlayResumeResponse resume)
    {
        ArgumentNullException.ThrowIfNull(resume);

        EngineSessionEnvelope session = resume.Bootstrap.Projection.Cursor.Session;
        string roleLabel = resume.Role switch
        {
            PlaySurfaceRole.GameMaster => "GM runboard",
            PlaySurfaceRole.Observer => "observer lane",
            _ => "player lane"
        };
        string latestTimeline = resume.Bootstrap.Projection.Timeline.LastOrDefault()
            ?? "No timeline events are cached yet.";
        string continuityPosture = resume.Checkpoint is null
            ? "No local checkpoint is cached yet, so this device still needs an owned continuity anchor."
            : $"Checkpoint {resume.Checkpoint.AppliedThroughSequence} stays aligned to {resume.Checkpoint.SceneRevision} for {roleLabel}.";
        string cachePosture = resume.CachePressure.BackpressureActive
            ? $"Cache pressure is active: {resume.CachePressure.RuntimeBundleCount}/{resume.CachePressure.RuntimeBundleQuota} runtime bundles are pinned and eviction already touched {resume.CachePressure.EvictedEntryCount} session(s)."
            : $"Cache pressure is calm: {resume.CachePressure.RuntimeBundleCount}/{resume.CachePressure.RuntimeBundleQuota} runtime bundles are pinned for this shell.";
        string runtimeBundleSummary = resume.RuntimeBundle is null
            ? "No runtime bundle is cached locally yet."
            : $"Bundle {resume.RuntimeBundle.BundleTag} was validated at {resume.RuntimeBundle.LastValidatedAtUtc:yyyy-MM-dd HH:mm} UTC.";
        string safeNextAction = BuildSafeNextAction(resume, session);

        List<string> attentionItems = [];
        if (resume.RuntimeBundle is null)
        {
            attentionItems.Add("Download the current runtime bundle before you trust offline continuation.");
        }

        if (resume.CachePressure.BackpressureActive)
        {
            attentionItems.Add("Cache pressure is active, so unpin stale sessions before you seed more travel or observer state.");
        }

        if (resume.Bootstrap.QuickActions.Count == 0)
        {
            attentionItems.Add("No quick actions are available for this role yet, so the table shell is still in a review-only posture.");
        }

        if (resume.Role == PlaySurfaceRole.Observer)
        {
            attentionItems.Add("Observer continuity should stay read-mostly until the owner lane confirms the latest scene revision.");
        }

        return new PlayCampaignWorkspaceLiteProjection(
            SessionId: resume.SessionId,
            Role: resume.Role,
            Summary: $"Resume {resume.SessionId} on the {roleLabel}. Scene {session.SceneId} is pinned at {session.SceneRevision}, and the latest table signal is '{latestTimeline}'.",
            CurrentSceneSummary: $"{session.SceneId} · revision {session.SceneRevision} · sequence {resume.Bootstrap.Projection.Cursor.AppliedThroughSequence}",
            RulePosture: $"{session.RuntimeFingerprint}. {runtimeBundleSummary}",
            SafeNextAction: safeNextAction,
            ContinuityPosture: continuityPosture,
            CachePosture: cachePosture,
            AttentionItems: attentionItems.Count == 0
                ? ["No blocking continuity issues are active on this device."]
                : attentionItems,
            QuickActionLabels: resume.Bootstrap.QuickActions.Select(action => action.Label).ToArray(),
            CoachHints: resume.Bootstrap.CoachHints.Select(hint => hint.Message).ToArray());
    }

    private static string BuildSafeNextAction(PlayResumeResponse resume, EngineSessionEnvelope session)
    {
        if (resume.CachePressure.BackpressureActive)
        {
            return "Clear cache pressure before you pin additional offline state or trust this device as a travel-safe shell.";
        }

        if (resume.RuntimeBundle is null)
        {
            return $"Reopen {session.SceneId} once while connected so this device can cache the grounded runtime bundle before the next table session.";
        }

        return resume.Role switch
        {
            PlaySurfaceRole.GameMaster => $"Open the GM shell, confirm scene {session.SceneId}, then use the next stale-protected action from the runboard.",
            PlaySurfaceRole.Observer => $"Resume the observer lane for {session.SceneId} and confirm continuity before you mirror any tactical update.",
            _ => $"Sync before taking the next quick action in {session.SceneId}, then keep the player lane focused on one grounded move."
        };
    }
}
