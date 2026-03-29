using Chummer.Campaign.Contracts;
using Chummer.Control.Contracts.Support;
using Chummer.Play.Core.PlayApi;

namespace Chummer.Play.Core.Application;

public sealed record PlayCampaignWorkspaceServerPlane(
    WorkspaceSummary Workspace,
    CampaignWorkspaceSummary Campaign,
    RosterReadinessSummary Roster,
    RunboardSummary Runboard,
    IReadOnlyList<RecapShelfEntry> RecapShelf,
    IReadOnlyList<RuleEnvironmentHealthCue> RuleHealth,
    IReadOnlyList<ContinuityConflictCue> ContinuityConflicts,
    IReadOnlyList<SupportClosureCue> SupportClosures,
    IReadOnlyList<KnownIssueAffectingInstall> KnownIssues,
    IReadOnlyList<DecisionNotice> DecisionNotices,
    NextSafeActionCue NextSafeAction);

public static class PlayCampaignWorkspaceServerPlaneProjector
{
    public static PlayCampaignWorkspaceServerPlane Create(PlayResumeResponse resume)
    {
        ArgumentNullException.ThrowIfNull(resume);

        EngineSessionEnvelope session = resume.Bootstrap.Projection.Cursor.Session;
        string roleLabel = ResolveRoleLabel(resume.Role);
        string roleSummary = ResolveRoleSummary(resume.Role);
        string latestTimeline = resume.Bootstrap.Projection.Timeline.LastOrDefault()
            ?? "No timeline events are cached yet.";
        DateTimeOffset updatedAtUtc = resume.RuntimeBundle?.LastValidatedAtUtc
            ?? resume.Checkpoint?.CapturedAtUtc
            ?? resume.Bootstrap.Projection.GeneratedAtUtc;
        string nextSafeActionSummary = BuildNextSafeActionSummary(resume, session);
        SupportClosureCue supportClosure = BuildSupportClosureCue(resume, session, roleSummary);
        string activeSceneSummary = $"{session.SceneId} is pinned at {session.SceneRevision} on {session.RuntimeFingerprint}.";

        string[] blockers = BuildBlockers(resume, session);
        RuleEnvironmentHealthCue[] ruleHealth = BuildRuleHealth(resume, session);
        ContinuityConflictCue[] continuityConflicts = BuildContinuityConflicts(resume, session);
        KnownIssueAffectingInstall[] knownIssues = BuildKnownIssues(resume, session, roleSummary);
        DecisionNotice[] decisionNotices = BuildDecisionNotices(resume, session);
        RecapShelfEntry[] recapShelf =
        [
            new(
                EntryId: $"recap:{resume.SessionId}",
                Kind: "recap",
                Label: $"{session.SceneId} recap-safe packet",
                Summary: $"Recap stays anchored on '{latestTimeline}' and checkpoint {resume.Checkpoint?.AppliedThroughSequence.ToString() ?? "pending"} for the {roleLabel}.",
                ArtifactId: $"artifact:{resume.SessionId}:recap",
                UpdatedAtUtc: updatedAtUtc)
        ];

        WorkspaceSummary workspace = new(
            WorkspaceId: $"workspace:{resume.SessionId}",
            CampaignId: $"campaign:{resume.SessionId}",
            CampaignName: $"{session.SceneId} mobile return",
            Visibility: "private",
            ReturnSummary: $"Return to {session.SceneId} on the {roleLabel} with {latestTimeline}.",
            DeviceRoleSummary: roleSummary,
            SupportClosureSummary: $"{supportClosure.StageLabel}: {supportClosure.NextSafeAction}",
            ActiveSceneSummary: activeSceneSummary,
            UpdatedAtUtc: updatedAtUtc);

        CampaignWorkspaceSummary campaign = new(
            WorkspaceId: workspace.WorkspaceId,
            CampaignId: workspace.CampaignId,
            CampaignName: workspace.CampaignName,
            RuleEnvironmentSummary: $"{session.RuntimeFingerprint} · {(resume.RuntimeBundle is null ? "candidate" : "approved")} · campaign",
            SessionReadinessSummary: BuildSessionReadinessSummary(resume, session),
            RestoreSummary: BuildRestoreSummary(resume, roleLabel),
            PublicationSummary: BuildPublicationSummary(resume, session, recapShelf[0]),
            NextSafeAction: nextSafeActionSummary,
            UpdatedAtUtc: updatedAtUtc);

        RosterReadinessSummary roster = new(
            Summary: BuildRosterSummary(resume, session, roleLabel, blockers),
            ReadyDossierCount: blockers.Length == 0 ? 1 : 0,
            NeedsAttentionCount: blockers.Length == 0 ? 0 : blockers.Length,
            CrewCount: 1,
            RunCount: 1,
            Highlights:
            [
                $"{roleLabel} owns the current grounded move for {session.SceneId}.",
                resume.RuntimeBundle is null
                    ? $"Runtime proof for {session.RuntimeFingerprint} is still missing on this device."
                    : $"Bundle {resume.RuntimeBundle.BundleTag} is ready for traced follow-through on this shell.",
                supportClosure.NextSafeAction
            ]);

        RunboardSummary runboard = new(
            RunId: $"run:{resume.SessionId}",
            Title: $"{session.SceneId} live runboard",
            Status: resume.Checkpoint is null ? RunStatuses.Planned : RunStatuses.Active,
            ActiveSceneId: session.SceneId,
            ActiveSceneSummary: activeSceneSummary,
            ObjectiveSummary: $"Latest table signal: {latestTimeline}. Next grounded step: {nextSafeActionSummary}",
            Blockers: blockers,
            ReturnSummary: workspace.ReturnSummary);

        NextSafeActionCue nextSafeAction = new(
            ActionId: $"next-safe-action:{resume.SessionId}",
            Label: "Next safe action",
            Summary: nextSafeActionSummary,
            SourceKind: "play_workspace_server_plane");

        return new PlayCampaignWorkspaceServerPlane(
            Workspace: workspace,
            Campaign: campaign,
            Roster: roster,
            Runboard: runboard,
            RecapShelf: recapShelf,
            RuleHealth: ruleHealth,
            ContinuityConflicts: continuityConflicts,
            SupportClosures: [supportClosure],
            KnownIssues: knownIssues,
            DecisionNotices: decisionNotices,
            NextSafeAction: nextSafeAction);
    }

    private static string ResolveRoleLabel(PlaySurfaceRole role)
        => role switch
        {
            PlaySurfaceRole.GameMaster => "GM runboard",
            PlaySurfaceRole.Observer => "observer lane",
            _ => "player lane"
        };

    private static string ResolveRoleSummary(PlaySurfaceRole role)
        => role switch
        {
            PlaySurfaceRole.GameMaster => "Device role: GM runboard owner for the current scene handoff.",
            PlaySurfaceRole.Observer => "Device role: observer lane that should stay read-mostly until the owner lane confirms the next revision.",
            _ => "Device role: play tablet that should stay focused on one grounded move at a time."
        };

    private static string BuildNextSafeActionSummary(PlayResumeResponse resume, EngineSessionEnvelope session)
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

    private static string BuildSessionReadinessSummary(PlayResumeResponse resume, EngineSessionEnvelope session)
    {
        if (resume.RuntimeBundle is null)
        {
            return $"Session readiness is provisional for {session.SceneId}: this shell still needs grounded runtime proof before offline continuation is trustworthy.";
        }

        if (resume.CachePressure.BackpressureActive)
        {
            return $"Session readiness is warning-only for {session.SceneId}: cache pressure can still evict bundle {resume.RuntimeBundle.BundleTag} before the next table handoff.";
        }

        return $"Session readiness is green for {session.SceneId}: checkpoint, runtime proof, and role posture are aligned on this device.";
    }

    private static string BuildRestoreSummary(PlayResumeResponse resume, string roleLabel)
        => resume.Checkpoint is null
            ? $"Restore summary: seed a local checkpoint before the {roleLabel} becomes the default return path."
            : $"Restore summary: checkpoint {resume.Checkpoint.AppliedThroughSequence} keeps the {roleLabel} aligned to the current scene revision.";

    private static string BuildPublicationSummary(PlayResumeResponse resume, EngineSessionEnvelope session, RecapShelfEntry recapEntry)
        => resume.RuntimeBundle is null
            ? $"Publication summary: {recapEntry.Label} exists, but reconnect once before you trust it as the final recap handoff for {session.SceneId}."
            : $"Publication summary: {recapEntry.Label} is grounded against bundle {resume.RuntimeBundle.BundleTag} for {session.SceneId}.";

    private static string BuildRosterSummary(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        string roleLabel,
        IReadOnlyList<string> blockers)
    {
        if (blockers.Count == 0)
        {
            return $"Roster readiness: the {roleLabel} is clear to continue {session.SceneId} with one grounded runner context and no active continuity blockers.";
        }

        return $"Roster readiness: the {roleLabel} can still resume {session.SceneId}, but {blockers.Count} blocker(s) need attention before you trust wider handoff or travel posture.";
    }

    private static string[] BuildBlockers(PlayResumeResponse resume, EngineSessionEnvelope session)
    {
        List<string> blockers = [];

        if (resume.RuntimeBundle is null)
        {
            blockers.Add($"Reconnect {session.SceneId} once so this device can validate the current runtime bundle.");
        }

        if (resume.CachePressure.BackpressureActive)
        {
            blockers.Add("Cache pressure is active, so the local runtime proof can still drift before the next handoff.");
        }

        if (resume.Role == PlaySurfaceRole.Observer)
        {
            blockers.Add("Observer posture stays read-mostly until the owner lane confirms the next scene revision.");
        }

        return blockers.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static RuleEnvironmentHealthCue[] BuildRuleHealth(PlayResumeResponse resume, EngineSessionEnvelope session)
    {
        List<RuleEnvironmentHealthCue> cues = [];

        if (resume.RuntimeBundle is null)
        {
            cues.Add(
                new RuleEnvironmentHealthCue(
                    EnvironmentId: $"environment:{session.RuntimeFingerprint}",
                    Severity: "warning",
                    Title: "Runtime proof pending",
                    Summary: $"Rule environment {session.RuntimeFingerprint} is loaded, but this device still needs grounded runtime proof before offline continuation is trustworthy."));
        }
        else
        {
            cues.Add(
                new RuleEnvironmentHealthCue(
                    EnvironmentId: $"environment:{session.RuntimeFingerprint}",
                    Severity: "info",
                    Title: "Runtime proof grounded",
                    Summary: $"Bundle {resume.RuntimeBundle.BundleTag} is aligned to {session.RuntimeFingerprint} on this shell."));
        }

        if (resume.CachePressure.BackpressureActive)
        {
            cues.Add(
                new RuleEnvironmentHealthCue(
                    EnvironmentId: $"environment:{session.RuntimeFingerprint}:cache",
                    Severity: "warning",
                    Title: "Cache pressure active",
                    Summary: $"Cache pressure already touched {resume.CachePressure.EvictedEntryCount} session(s), so local rule proof can drift if you pin more travel state."));
        }

        return cues.ToArray();
    }

    private static ContinuityConflictCue[] BuildContinuityConflicts(PlayResumeResponse resume, EngineSessionEnvelope session)
    {
        List<ContinuityConflictCue> cues = [];

        if (resume.Checkpoint is null)
        {
            cues.Add(
                new ContinuityConflictCue(
                    CueId: $"continuity:{resume.SessionId}:checkpoint",
                    Severity: "warning",
                    Summary: $"No local checkpoint is pinned yet for {session.SceneId}, so return continuity is still provisional on this device.",
                    ResolutionAction: "Seed a checkpoint before you trust this shell as the default return path."));
        }

        if (resume.CachePressure.BackpressureActive)
        {
            cues.Add(
                new ContinuityConflictCue(
                    CueId: $"continuity:{resume.SessionId}:cache",
                    Severity: "warning",
                    Summary: $"Cache pressure can still evict active bundle proof for {session.SceneId}.",
                    ResolutionAction: "Clear stale travel or observer state before the next offline handoff."));
        }

        return cues.ToArray();
    }

    private static SupportClosureCue BuildSupportClosureCue(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        string roleSummary)
    {
        if (resume.SupportNotice is not null)
        {
            return new SupportClosureCue(
                CaseId: $"play-support:{resume.SessionId}",
                Status: resume.SupportNotice.StatusLabel.ToLowerInvariant().Replace(' ', '_'),
                StageLabel: resume.SupportNotice.StatusLabel,
                Summary: resume.SupportNotice.KnownIssueSummary,
                NextSafeAction: resume.SupportNotice.NextSafeAction,
                FixedReleaseLabel: resume.RuntimeBundle?.BundleTag,
                AffectedInstallSummary: roleSummary);
        }

        return new SupportClosureCue(
            CaseId: $"play-support:{resume.SessionId}",
            Status: resume.RuntimeBundle is null ? "runtime_proof_missing" : "ready_to_verify",
            StageLabel: resume.RuntimeBundle is null ? "Runtime proof missing" : "Ready to verify",
            Summary: resume.RuntimeBundle is null
                ? $"This device resumed {resume.SessionId}/{session.SceneId} without a validated local bundle, so support closure is still provisional."
                : $"Bundle {resume.RuntimeBundle.BundleTag} is grounded for {resume.SessionId}/{session.SceneId} on this device.",
            NextSafeAction: BuildNextSafeActionSummary(resume, session),
            FixedReleaseLabel: resume.RuntimeBundle?.BundleTag,
            AffectedInstallSummary: roleSummary);
    }

    private static KnownIssueAffectingInstall[] BuildKnownIssues(
        PlayResumeResponse resume,
        EngineSessionEnvelope session,
        string roleSummary)
    {
        List<KnownIssueAffectingInstall> items = [];

        if (resume.SupportNotice is not null)
        {
            items.Add(
                new KnownIssueAffectingInstall(
                    CaseId: $"play-known-issue:{resume.SessionId}",
                    Severity: resume.CachePressure.BackpressureActive ? "warning" : "info",
                    Summary: resume.SupportNotice.KnownIssueSummary,
                    AffectedInstallSummary: roleSummary,
                    DetailHref: resume.SupportNotice.FollowThroughHref));
        }
        else if (resume.RuntimeBundle is null)
        {
            items.Add(
                new KnownIssueAffectingInstall(
                    CaseId: $"play-known-issue:{resume.SessionId}",
                    Severity: "warning",
                    Summary: $"Runtime proof is still missing for {resume.SessionId}/{session.SceneId} on this mobile shell.",
                    AffectedInstallSummary: roleSummary,
                    DetailHref: $"/downloads?runtime={Uri.EscapeDataString(session.RuntimeFingerprint)}&sessionId={Uri.EscapeDataString(resume.SessionId)}"));
        }

        return items.ToArray();
    }

    private static DecisionNotice[] BuildDecisionNotices(PlayResumeResponse resume, EngineSessionEnvelope session)
    {
        if (resume.RuntimeBundle is null)
        {
            return
            [
                new DecisionNotice(
                    NoticeId: $"notice:{resume.SessionId}:runtime",
                    Kind: "runtime_proof",
                    Summary: $"Reconnect {session.SceneId} once before you trust offline continuation or support closure on this shell.",
                    ActionLabel: "Open downloads guidance",
                    ActionHref: $"/downloads?runtime={Uri.EscapeDataString(session.RuntimeFingerprint)}&sessionId={Uri.EscapeDataString(resume.SessionId)}")
            ];
        }

        if (resume.CachePressure.BackpressureActive)
        {
            return
            [
                new DecisionNotice(
                    NoticeId: $"notice:{resume.SessionId}:cache",
                    Kind: "cache_pressure",
                    Summary: $"Clear cache pressure before you seed more travel or observer state on this device.",
                    ActionLabel: BuildSupportDecisionActionLabel(resume, session),
                    ActionHref: resume.SupportNotice?.FollowThroughHref
                        ?? $"/contact?kind=install_help&sessionId={Uri.EscapeDataString(resume.SessionId)}&sceneId={Uri.EscapeDataString(session.SceneId)}")
            ];
        }

        return
        [
            new DecisionNotice(
                NoticeId: $"notice:{resume.SessionId}:continue",
                Kind: "continue",
                Summary: $"Continue {session.SceneId} from the current grounded lane and keep the next move scoped to this device role.",
                ActionLabel: "Open the current lane",
                ActionHref: string.IsNullOrWhiteSpace(resume.DeepLinkOwnerRoute)
                    ? $"/play?sessionId={Uri.EscapeDataString(resume.SessionId)}&role={Uri.EscapeDataString(resume.Role.ToString())}"
                    : resume.DeepLinkOwnerRoute!)
        ];
    }

    private static string BuildSupportDecisionActionLabel(PlayResumeResponse resume, EngineSessionEnvelope session)
    {
        string rawAction = !string.IsNullOrWhiteSpace(resume.SupportNotice?.NextSafeAction)
            ? resume.SupportNotice.NextSafeAction!
            : resume.RuntimeBundle is null
                ? $"Review install support for {session.SceneId}"
                : $"Review support for bundle {resume.RuntimeBundle.BundleTag}";

        string trimmed = rawAction.Trim().TrimEnd('.', '!', '?');
        int delimiter = trimmed.IndexOfAny([',', ';']);
        string clause = delimiter > 0 ? trimmed[..delimiter] : trimmed;
        clause = clause.Trim();
        if (clause.Length > 52)
        {
            int boundary = clause.LastIndexOf(' ', 52);
            clause = boundary > 12 ? clause[..boundary] : clause[..52];
        }

        return string.IsNullOrWhiteSpace(clause)
            ? "Review install support"
            : clause;
    }
}
