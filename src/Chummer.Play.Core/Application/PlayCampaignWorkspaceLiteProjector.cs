using System;
using System.Collections.Generic;
using System.Linq;
using Chummer.Campaign.Contracts;
using Chummer.Control.Contracts.Support;
using Chummer.Play.Core.PlayApi;

namespace Chummer.Play.Core.Application;

public sealed record PlayCampaignWorkspaceLiteProjection(
    string SessionId,
    PlaySurfaceRole Role,
    string Summary,
    string CurrentSceneSummary,
    string ChangePacketSummary,
    string ServerPlaneSummary,
    string RunboardSummary,
    string RosterSummary,
    string DecisionNotice,
    string DecisionNoticeHref,
    string RecapSummary,
    string RolePosture,
    string RulePosture,
    string SafeNextAction,
    string ContinuityPosture,
    string CachePosture,
    string TravelPosture,
    string OfflinePrefetchSummary,
    string UpdatePosture,
    string SupportPosture,
    string SupportStatus,
    string KnownIssueSummary,
    string FixAvailabilitySummary,
    string UpdateFollowThrough,
    string UpdateFollowThroughHref,
    string SupportFollowThrough,
    string SupportFollowThroughHref,
    string RoleFollowThrough,
    string RoleFollowThroughHref,
    IReadOnlyList<string> AttentionItems,
    IReadOnlyList<string> ChangePacketLabels,
    IReadOnlyList<string> QuickActionLabels,
    IReadOnlyList<string> FollowThroughLabels,
    IReadOnlyList<string> CoachHints);

public static class PlayCampaignWorkspaceLiteProjector
{
    public static PlayCampaignWorkspaceLiteProjection Create(PlayResumeResponse resume)
    {
        ArgumentNullException.ThrowIfNull(resume);

        EngineSessionEnvelope session = resume.Bootstrap.Projection.Cursor.Session;
        PlayCampaignWorkspaceServerPlane serverPlane = PlayCampaignWorkspaceServerPlaneProjector.Create(resume);
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
        string travelPosture = BuildTravelPosture(resume, session);
        string offlinePrefetchSummary = BuildOfflinePrefetchSummary(resume, serverPlane, roleLabel);
        string runtimeBundleSummary = resume.RuntimeBundle is null
            ? "No runtime bundle is cached locally yet."
            : $"Bundle {resume.RuntimeBundle.BundleTag} was validated at {resume.RuntimeBundle.LastValidatedAtUtc:yyyy-MM-dd HH:mm} UTC.";
        string changePacketSummary = BuildChangePacketSummary(resume, session, latestTimeline);
        string serverPlaneSummary = $"{serverPlane.Campaign.SessionReadinessSummary} {serverPlane.Campaign.RestoreSummary}";
        string runboardSummary = $"{serverPlane.Runboard.Title}: {serverPlane.Runboard.ObjectiveSummary}";
        string rosterSummary = serverPlane.Roster.Summary;
        DecisionNotice? decisionNotice = serverPlane.DecisionNotices.FirstOrDefault();
        string decisionNoticeSummary = decisionNotice is null
            ? "No campaign decision notices are active for this shell."
            : $"{decisionNotice.Summary} {decisionNotice.ActionLabel}.";
        string decisionNoticeHref = decisionNotice?.ActionHref ?? "/";
        string recapSummary = serverPlane.RecapShelf.FirstOrDefault() is { } recapEntry
            ? $"{recapEntry.Label}: {recapEntry.Summary}"
            : "No recap-safe packet is available yet.";
        string rolePosture = BuildRolePosture(resume, session);
        string safeNextAction = serverPlane.NextSafeAction.Summary;
        string updatePosture = BuildUpdatePosture(resume, session);
        string supportPosture = BuildSupportPosture(resume, serverPlane);
        string supportStatus = BuildSupportStatus(serverPlane);
        string knownIssueSummary = BuildKnownIssueSummary(resume, session, serverPlane);
        string fixAvailabilitySummary = BuildFixAvailabilitySummary(resume, session, serverPlane);
        string updateFollowThrough = BuildUpdateFollowThrough(resume, session);
        string updateFollowThroughHref = BuildUpdateFollowThroughHref(resume, session);
        string supportFollowThrough = BuildSupportFollowThrough(resume, session);
        string supportFollowThroughHref = BuildSupportFollowThroughHref(resume, session);
        string roleFollowThrough = BuildRoleFollowThrough(resume, session);
        string roleFollowThroughHref = BuildRoleFollowThroughHref(resume, session);
        string[] changePacketLabels = BuildChangePacketLabels(resume, session, latestTimeline);
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

        foreach (RuleEnvironmentHealthCue cue in serverPlane.RuleHealth.Where(static cue => !string.Equals(cue.Severity, "info", StringComparison.OrdinalIgnoreCase)))
        {
            attentionItems.Add(cue.Summary);
        }

        foreach (ContinuityConflictCue cue in serverPlane.ContinuityConflicts)
        {
            attentionItems.Add($"{cue.Summary} {cue.ResolutionAction}");
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
            ChangePacketSummary: changePacketSummary,
            ServerPlaneSummary: serverPlaneSummary,
            RunboardSummary: runboardSummary,
            RosterSummary: rosterSummary,
            DecisionNotice: decisionNoticeSummary,
            DecisionNoticeHref: decisionNoticeHref,
            RecapSummary: recapSummary,
            RolePosture: rolePosture,
            RulePosture: $"{session.RuntimeFingerprint}. {runtimeBundleSummary}",
            SafeNextAction: safeNextAction,
            ContinuityPosture: continuityPosture,
            CachePosture: cachePosture,
            TravelPosture: travelPosture,
            OfflinePrefetchSummary: offlinePrefetchSummary,
            UpdatePosture: updatePosture,
            SupportPosture: supportPosture,
            SupportStatus: supportStatus,
            KnownIssueSummary: knownIssueSummary,
            FixAvailabilitySummary: fixAvailabilitySummary,
            UpdateFollowThrough: updateFollowThrough,
            UpdateFollowThroughHref: updateFollowThroughHref,
            SupportFollowThrough: supportFollowThrough,
            SupportFollowThroughHref: supportFollowThroughHref,
            RoleFollowThrough: roleFollowThrough,
            RoleFollowThroughHref: roleFollowThroughHref,
            AttentionItems: attentionItems.Count == 0
                ? ["No blocking continuity issues are active on this device."]
                : attentionItems,
            ChangePacketLabels: changePacketLabels,
            QuickActionLabels: resume.Bootstrap.QuickActions.Select(action => action.Label).ToArray(),
            FollowThroughLabels: followThroughLabels,
            CoachHints: resume.Bootstrap.CoachHints.Select(hint => hint.Message).ToArray());
    }

    private static string BuildChangePacketSummary(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        string latestTimeline)
    {
        string checkpointSummary = resume.Checkpoint is null
            ? "No local return anchor is pinned yet."
            : $"Return anchor stays on checkpoint {resume.Checkpoint.AppliedThroughSequence}.";
        string bundleSummary = resume.RuntimeBundle is null
            ? "No validated bundle is cached locally."
            : $"Bundle {resume.RuntimeBundle.BundleTag} is already validated.";
        return $"Scene {session.SceneId} remains pinned at {session.SceneRevision}. Latest table signal: {latestTimeline}. {checkpointSummary} {bundleSummary}";
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

    private static string BuildTravelPosture(PlayResumeResponse resume, EngineSessionEnvelope session)
    {
        if (resume.RuntimeBundle is null)
        {
            return $"Travel posture: reconnect {session.SceneId} once so this claimed device can prefetch dossier, campaign, rule-environment, and recap truth before the next safehouse or travel handoff.";
        }

        if (resume.CachePressure.BackpressureActive)
        {
            return $"Travel posture: bundle {resume.RuntimeBundle.BundleTag} is grounded, but cache pressure already touched {resume.CachePressure.EvictedEntryCount} session(s), so bounded offline prefetch can still drift before the next travel handoff.";
        }

        if (resume.Checkpoint is null)
        {
            return $"Travel posture: bundle {resume.RuntimeBundle.BundleTag} is grounded, but seed a local checkpoint before you trust this shell as a safehouse return anchor.";
        }

        return $"Travel posture: checkpoint {resume.Checkpoint.AppliedThroughSequence}, bundle {resume.RuntimeBundle.BundleTag}, and the recap-safe packet are ready for bounded offline use on this claimed device.";
    }

    private static string BuildOfflinePrefetchSummary(
        PlayResumeResponse resume,
        PlayCampaignWorkspaceServerPlane serverPlane,
        string roleLabel)
    {
        string checkpointSummary = resume.Checkpoint is null
            ? "no local checkpoint yet"
            : $"checkpoint {resume.Checkpoint.AppliedThroughSequence}";
        string bundleSummary = resume.RuntimeBundle is null
            ? "runtime proof pending"
            : $"bundle {resume.RuntimeBundle.BundleTag}";
        string recapSummary = serverPlane.RecapShelf.FirstOrDefault()?.Label ?? "no recap-safe packet yet";
        return $"Offline prefetch: {checkpointSummary}, {bundleSummary}, {recapSummary}, and the {roleLabel} return lane stay install-local and bounded to this device.";
    }

    private static string BuildSupportPosture(PlayResumeResponse resume, PlayCampaignWorkspaceServerPlane serverPlane)
    {
        var cue = serverPlane.SupportClosures.FirstOrDefault();
        return cue is null
            ? "Support posture: no support closure cue is attached yet."
            : $"Support posture: {cue.StageLabel}. {cue.Summary}";
    }

    private static string BuildSupportStatus(PlayCampaignWorkspaceServerPlane serverPlane)
    {
        var cue = serverPlane.SupportClosures.FirstOrDefault();
        return cue is null
            ? "Support status: no support closure cue is attached yet."
            : $"Support status: {cue.StageLabel}.";
    }

    private static string BuildKnownIssueSummary(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        PlayCampaignWorkspaceServerPlane serverPlane)
        => serverPlane.KnownIssues.FirstOrDefault() is { } issue
            ? $"Known issue: {issue.Summary}"
            : resume.RuntimeBundle is null
                ? $"Known issue: {resume.SessionId}/{session.SceneId} resumed without a validated local bundle, so offline trust is still provisional."
                : $"Known issue: if {resume.SessionId}/{session.SceneId} still reproduces the same problem, support should cite the current bundle proof rather than reopen without runtime context.";

    private static string BuildFixAvailabilitySummary(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        PlayCampaignWorkspaceServerPlane serverPlane)
    {
        var cue = serverPlane.SupportClosures.FirstOrDefault();
        if (cue is not null && !string.IsNullOrWhiteSpace(cue.FixedReleaseLabel))
        {
            return $"Fix availability: {cue.FixedReleaseLabel} is the grounded local fix and update target for {session.RuntimeFingerprint}.";
        }

        return resume.RuntimeBundle is null
            ? $"Fix availability: no validated local fix target is cached yet for {session.RuntimeFingerprint}."
            : $"Fix availability: bundle {resume.RuntimeBundle.BundleTag} is the grounded local fix and update target for {session.RuntimeFingerprint}.";
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
        => resume.SupportNotice is not null
            ? $"Support follow-through for {resume.SessionId}/{session.SceneId}: {resume.SupportNotice.NextSafeAction}"
            : resume.RuntimeBundle is null
                ? $"Prepare support context for {resume.SessionId}/{session.SceneId} with runtime {session.RuntimeFingerprint} and note that this device still lacks a local bundle."
                : $"Prepare support context for {resume.SessionId}/{session.SceneId} with runtime {session.RuntimeFingerprint} and bundle {resume.RuntimeBundle.BundleTag}.";

    private static string BuildSupportFollowThroughHref(PlayResumeResponse resume, EngineSessionEnvelope session)
    {
        if (!string.IsNullOrWhiteSpace(resume.SupportNotice?.FollowThroughHref))
        {
            return resume.SupportNotice.FollowThroughHref!;
        }

        string bundle = resume.RuntimeBundle?.BundleTag ?? string.Empty;
        string title = resume.RuntimeBundle is null
            ? $"Mobile follow-through needs grounded runtime for {session.SceneId}"
            : $"Mobile follow-through needs support review for {session.SceneId}";
        string summary = resume.RuntimeBundle is null
            ? $"This mobile shell resumed {resume.SessionId}/{session.SceneId} on {session.RuntimeFingerprint} without a validated local bundle."
            : $"This mobile shell resumed {resume.SessionId}/{session.SceneId} on {session.RuntimeFingerprint} with bundle {bundle}.";
        string detail = resume.RuntimeBundle is null
            ? $"Session: {resume.SessionId}\nScene: {session.SceneId}\nRuntime: {session.RuntimeFingerprint}\nBundle: none cached locally\n\nWhat happened:\n- This device resumed the scene without a validated local runtime bundle.\n- Please ground the next support step against the mobile shell and reconnect path."
            : $"Session: {resume.SessionId}\nScene: {session.SceneId}\nRuntime: {session.RuntimeFingerprint}\nBundle: {bundle}\n\nWhat happened:\n- This device resumed the scene with a validated local runtime bundle.\n- Please ground the next support step against the current mobile shell and bundle.";
        return $"/contact?kind=install_help&title={Uri.EscapeDataString(title)}&summary={Uri.EscapeDataString(summary)}&detail={Uri.EscapeDataString(detail)}&sessionId={Uri.EscapeDataString(resume.SessionId)}&sceneId={Uri.EscapeDataString(session.SceneId)}&runtime={Uri.EscapeDataString(session.RuntimeFingerprint)}&bundle={Uri.EscapeDataString(bundle)}";
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

    private static string[] BuildChangePacketLabels(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        string latestTimeline)
    {
        List<string> labels =
        [
            $"Scene packet: {session.SceneId} · {session.SceneRevision}",
            $"Latest signal: {latestTimeline}"
        ];

        if (resume.Checkpoint is not null)
        {
            labels.Add($"Return anchor: checkpoint {resume.Checkpoint.AppliedThroughSequence}");
        }

        if (resume.RuntimeBundle is not null)
        {
            labels.Add($"Bundle proof: {resume.RuntimeBundle.BundleTag}");
        }

        labels.Add(
            resume.RuntimeBundle is not null && resume.Checkpoint is not null
                ? $"Travel-safe packet: checkpoint {resume.Checkpoint.AppliedThroughSequence} + {resume.RuntimeBundle.BundleTag}"
                : "Travel-safe packet: reconnect required");

        PlayQuickAction? nextAction = resume.Bootstrap.QuickActions.FirstOrDefault();
        if (nextAction is not null)
        {
            labels.Add($"Quick action ready: {nextAction.Label}");
        }

        return labels.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

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
