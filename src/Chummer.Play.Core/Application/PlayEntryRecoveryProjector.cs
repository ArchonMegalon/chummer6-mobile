using Chummer.Play.Core.PlayApi;
using Chummer.Play.Core.Roaming;

namespace Chummer.Play.Core.Application;

public sealed record PlayEntryRecoveryProjection(
    string EntryState,
    string EntryStateSummary,
    string RecommendedActionLabel,
    string RecommendedActionHref,
    string RetryActionLabel,
    string RetryActionHref,
    string CancelActionLabel,
    string CancelActionHref,
    string RestoreActionLabel,
    string RestoreActionHref,
    IReadOnlyList<string> RecoveryActions);

public static class PlayEntryRecoveryProjector
{
    public static PlayEntryRecoveryProjection Create(PlayResumeResponse resume, RoamingWorkspaceRestorePlan restorePlan)
    {
        ArgumentNullException.ThrowIfNull(resume);
        ArgumentNullException.ThrowIfNull(restorePlan);

        bool isNoSession = resume.Checkpoint is null
            && resume.RuntimeBundle is null
            && resume.Bootstrap.Projection.Cursor.AppliedThroughSequence == 0;
        bool isNoCampaign = string.IsNullOrWhiteSpace(restorePlan.ReturnTargetCampaignName);
        bool isPostFailure = restorePlan.RequiresConflictReview
            || resume.CachePressure.BackpressureActive
            || string.Equals(resume.SupportNotice?.StatusLabel, "Runtime proof missing", StringComparison.OrdinalIgnoreCase);

        string entryState = "ready";
        string entryStateSummary = "Play shell continuity is aligned, and one-tap recovery remains available if state drifts.";
        string recommendedActionLabel = $"Continue {resume.Bootstrap.Projection.Cursor.Session.SceneId}";
        string recommendedActionHref = restorePlan.ResumeFollowThroughHref;
        string[] recoveryActions =
        [
            "Retry keeps the current role and scene route pinned.",
            "Cancel stays local and read-only until the next explicit action.",
            "Restore re-opens claimed-device continuity planning with install-local guardrails."
        ];

        if (isNoSession)
        {
            entryState = "no_session";
            entryStateSummary = "No session state is cached on this device yet, so onboarding should start from a single role-owned entry point.";
            recommendedActionLabel = "Start role-safe session bootstrap";
            recommendedActionHref = resume.DeepLinkOwnerRoute;
        }
        else if (isNoCampaign)
        {
            entryState = "no_campaign";
            entryStateSummary = "A session exists but no campaign return target is attached yet, so onboarding should create one bounded return lane.";
            recommendedActionLabel = "Create campaign return target";
            recommendedActionHref = "/campaigns/new?source=mobile-play";
        }
        else if (isPostFailure)
        {
            entryState = "post_failure";
            entryStateSummary = "Recovery is in a post-failure lane, so the next action should be explicit and one-tap with no silent rollback.";
            recommendedActionLabel = restorePlan.SafeNextAction;
            recommendedActionHref = restorePlan.ResumeFollowThroughHref;
        }

        return new PlayEntryRecoveryProjection(
            EntryState: entryState,
            EntryStateSummary: entryStateSummary,
            RecommendedActionLabel: recommendedActionLabel,
            RecommendedActionHref: recommendedActionHref,
            RetryActionLabel: "Retry recovery",
            RetryActionHref: resume.DeepLinkOwnerRoute,
            CancelActionLabel: "Cancel and stay read-only",
            CancelActionHref: "/",
            RestoreActionLabel: "Restore claimed-device plan",
            RestoreActionHref: restorePlan.ResumeFollowThroughHref,
            RecoveryActions: recoveryActions);
    }
}
