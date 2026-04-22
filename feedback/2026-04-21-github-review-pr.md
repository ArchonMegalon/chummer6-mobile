# GitHub Codex Review

PR: https://github.com/ArchonMegalon/chummer6-mobile/pull/9

Findings:
- [high] scripts/ai/verify.sh [tests] mirror-verifier-untracked
`scripts/ai/verify.sh` now hard-requires and executes `scripts/ai/verify_design_mirror.py` at lines 53 and 67.; `git ls-files --error-unmatch scripts/ai/verify_design_mirror.py` fails, and `git status --short -- scripts/ai/verify_design_mirror.py` reports `?? scripts/ai/verify_design_mirror.py`, so the checker is not part of the branch.; A clean checkout of the branch will therefore fail `scripts/ai/verify.sh` before any real verification can run, and the mirror-drift protection this slice claims to add is not reproducible from the branch itself.
Expected fix: Commit the mirror verifier with the branch, or remove the new hard dependency from `verify.sh`; then keep the fail-close coverage in tracked verification.
- [high] src/Chummer.Play.Core/Roaming/RoamingWorkspaceSyncPlanner.cs [state] empty-state-restore-href-dead-route
`RoamingWorkspaceSyncPlanner.BuildResumeFollowThroughHref()` returns `/play?deviceId=...` when no campaign/dossier session is available (lines 451-463).; `PlayEntryRecoveryProjector` always exposes that value as `RestoreActionHref` for onboarding/recovery (lines 76-77), including the `no_session` and `no_campaign` states this slice added.; `PlayWebApplication` only maps `/play/{sessionId}` (lines 317-324); there is no `/play` route, so the generated empty-state restore link is dead.; The regression suite does not catch this: the no-session synthetic test injects `/play/session-empty` by hand instead of using the planner output (`Program.cs` lines 456-463, 488-490), and the route test only asserts sessionful restore hrefs (`Program.cs` lines 1949-1951).
Expected fix: Make empty-state restore follow-through point at a real route, or add a `/play` entry route that can honor `deviceId`; add a regression that exercises the planner-generated no-session/no-campaign restore href end to end.
