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
    string RolePosture,
    string RulePosture,
    string SafeNextAction,
    string ContinuityPosture,
    string CachePosture,
    string UpdatePosture,
    string SupportPosture,
    string UpdateFollowThrough,
    string UpdateFollowThroughHref,
    string SupportFollowThrough,
    string SupportFollowThroughHref,
    string RoleFollowThrough,
    string RoleFollowThroughHref,
    IReadOnlyList<string> AttentionItems,
    IReadOnlyList<string> QuickActionLabels,
    IReadOnlyList<string> FollowThroughLabels,
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
        string rolePosture = BuildRolePosture(resume, session);
        string safeNextAction = BuildSafeNextAction(resume, session);
        string updatePosture = BuildUpdatePosture(resume, session);
        string supportPosture = BuildSupportPosture(resume, session);
        string updateFollowThrough = BuildUpdateFollowThrough(resume, session);
        string updateFollowThroughHref = BuildUpdateFollowThroughHref(resume, session);
        string supportFollowThrough = BuildSupportFollowThrough(resume, session);
        string supportFollowThroughHref = BuildSupportFollowThroughHref(resume, session);
        string roleFollowThrough = BuildRoleFollowThrough(resume, session);
        string roleFollowThroughHref = BuildRoleFollowThroughHref(resume, session);
        string[] followThroughLabels = BuildFollowThroughLabels(resume, session, updateFollowThrough, supportFollowThrough, roleFollowThrough);

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
            RolePosture: rolePosture,
            RulePosture: $"{session.RuntimeFingerprint}. {runtimeBundleSummary}",
            SafeNextAction: safeNextAction,
            ContinuityPosture: continuityPosture,
            CachePosture: cachePosture,
            UpdatePosture: updatePosture,
            SupportPosture: supportPosture,
            UpdateFollowThrough: updateFollowThrough,
            UpdateFollowThroughHref: updateFollowThroughHref,
            SupportFollowThrough: supportFollowThrough,
            SupportFollowThroughHref: supportFollowThroughHref,
            RoleFollowThrough: roleFollowThrough,
            RoleFollowThroughHref: roleFollowThroughHref,
            AttentionItems: attentionItems.Count == 0
                ? ["No blocking continuity issues are active on this device."]
                : attentionItems,
            QuickActionLabels: resume.Bootstrap.QuickActions.Select(action => action.Label).ToArray(),
            FollowThroughLabels: followThroughLabels,
            CoachHints: resume.Bootstrap.CoachHints.Select(hint => hint.Message).ToArray());
    }

    private static string BuildRolePosture(PlayResumeResponse resume, EngineSessionEnvelope session)
    {
        string route = string.IsNullOrWhiteSpace(resume.DeepLinkOwnerRoute)
            ? $"/play/{resume.SessionId}"
            : resume.DeepLinkOwnerRoute;

        return resume.Role switch
        {
            PlaySurfaceRole.GameMaster => $"Role posture: GM runboard on {route}. This device is the coordination lane for {session.SceneId}, so scene changes should originate here before they fan out.",
            PlaySurfaceRole.Observer => $"Role posture: observer lane on {route}. Keep this shell read-mostly until the owner lane confirms the next scene revision.",
            _ => $"Role posture: player lane on {route}. Keep this shell focused on one grounded move at a time and leave prep-heavy changes to the workbench."
        };
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

    private static string BuildUpdatePosture(PlayResumeResponse resume, EngineSessionEnvelope session)
    {
        if (resume.RuntimeBundle is null)
        {
            return $"Update posture: reconnect {session.SceneId} before you trust offline play on this device; no validated runtime bundle is cached locally yet.";
        }

        return $"Update posture: bundle {resume.RuntimeBundle.BundleTag} for {session.RuntimeFingerprint} was validated at {resume.RuntimeBundle.LastValidatedAtUtc:yyyy-MM-dd HH:mm} UTC and is the grounded local update target.";
    }

    private static string BuildSupportPosture(PlayResumeResponse resume, EngineSessionEnvelope session)
    {
        if (resume.RuntimeBundle is null)
        {
            return $"Support posture: report {resume.SessionId}/{session.SceneId} with runtime {session.RuntimeFingerprint} and note that this device has no local runtime bundle yet.";
        }

        return $"Support posture: report {resume.SessionId}/{session.SceneId}, runtime {session.RuntimeFingerprint}, and bundle {resume.RuntimeBundle.BundleTag} so support can ground the case against this mobile shell.";
    }

    private static string BuildUpdateFollowThrough(PlayResumeResponse resume, EngineSessionEnvelope session)
        => resume.RuntimeBundle is null
            ? $"Reconnect {session.SceneId} and validate a grounded runtime bundle before trusting offline updates on this device."
            : $"Review update posture for bundle {resume.RuntimeBundle.BundleTag} before the next offline or travel session.";

    private static string BuildUpdateFollowThroughHref(PlayResumeResponse resume, EngineSessionEnvelope session)
        => resume.RuntimeBundle is null
            ? $"/downloads?runtime={Uri.EscapeDataString(session.RuntimeFingerprint)}&sessionId={Uri.EscapeDataString(resume.SessionId)}"
            : $"/downloads?bundle={Uri.EscapeDataString(resume.RuntimeBundle.BundleTag)}&runtime={Uri.EscapeDataString(session.RuntimeFingerprint)}";

    private static string BuildSupportFollowThrough(PlayResumeResponse resume, EngineSessionEnvelope session)
        => resume.RuntimeBundle is null
            ? $"Prepare support context for {resume.SessionId}/{session.SceneId} with runtime {session.RuntimeFingerprint} and note that this device still lacks a local bundle."
            : $"Prepare support context for {resume.SessionId}/{session.SceneId} with runtime {session.RuntimeFingerprint} and bundle {resume.RuntimeBundle.BundleTag}.";

    private static string BuildSupportFollowThroughHref(PlayResumeResponse resume, EngineSessionEnvelope session)
    {
        string bundle = resume.RuntimeBundle?.BundleTag ?? string.Empty;
        return $"/contact?sessionId={Uri.EscapeDataString(resume.SessionId)}&sceneId={Uri.EscapeDataString(session.SceneId)}&runtime={Uri.EscapeDataString(session.RuntimeFingerprint)}&bundle={Uri.EscapeDataString(bundle)}";
    }

    private static string BuildRoleFollowThrough(PlayResumeResponse resume, EngineSessionEnvelope session)
        => resume.Role switch
        {
            PlaySurfaceRole.GameMaster => $"Keep GM changes anchored on {session.SceneId} before they fan out to other devices.",
            PlaySurfaceRole.Observer => $"Keep the observer lane read-mostly until the owner lane confirms the next scene revision.",
            _ => $"Keep the player lane focused on one grounded move before you reopen build or support follow-through elsewhere."
        };

    private static string BuildRoleFollowThroughHref(PlayResumeResponse resume, EngineSessionEnvelope session)
        => string.IsNullOrWhiteSpace(resume.DeepLinkOwnerRoute)
            ? $"/play?sessionId={Uri.EscapeDataString(resume.SessionId)}&role={Uri.EscapeDataString(resume.Role.ToString())}"
            : resume.DeepLinkOwnerRoute!;

    private static string[] BuildFollowThroughLabels(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        string updateFollowThrough,
        string supportFollowThrough,
        string roleFollowThrough)
    {
        List<string> labels =
        [
            updateFollowThrough,
            supportFollowThrough,
            roleFollowThrough
        ];

        if (resume.CachePressure.BackpressureActive)
        {
            labels.Insert(0, "Clear cache pressure before you pin more travel or observer state on this device.");
        }

        if (resume.Checkpoint is null)
        {
            labels.Add("Seed a local continuity checkpoint before you trust this device as a stable return path.");
        }

        return labels.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }
}
