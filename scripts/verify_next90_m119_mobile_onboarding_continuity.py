#!/usr/bin/env python3
from __future__ import annotations

import json
import re
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
PACKAGE_ID = "next90-m119-mobile-onboarding-continuity"
FRONTIER_ID = "2766704797"
MILESTONE_ID = "119"
WORK_TASK_ID = "119.3"
REPO_LABEL = "chummer6-mobile"
CHECKOUT_ROOT = str(ROOT)

REGISTRY = ROOT / ".codex-design" / "product" / "NEXT_90_DAY_PRODUCT_ADVANCE_REGISTRY.yaml"
DESIGN_QUEUE = ROOT / ".codex-design" / "product" / "NEXT_90_DAY_QUEUE_STAGING.generated.yaml"
FLEET_QUEUE = Path("/docker/fleet/.codex-studio/published/NEXT_90_DAY_QUEUE_STAGING.generated.yaml")
PROOF_DOC = ROOT / "docs" / "next90-m119-mobile-onboarding-continuity.proof.md"
PLAY_SIGNOFF = ROOT / "docs" / "PLAY_RELEASE_SIGNOFF.md"
GENERATED_PROOF = ROOT / ".codex-studio" / "published" / "MOBILE_LOCAL_RELEASE_PROOF.generated.json"

IMPLEMENTATION_MARKERS = {
    "src/Chummer.Play.Core/Application/PlayCampaignWorkspaceServerPlane.cs": (
        'Kind: "campaign_primer"',
        'Label: $"{session.SceneId} starter primer"',
        'ArtifactId: $"artifact:{resume.SessionId}:starter-primer"',
        'Kind: "mission_briefing"',
        'Label: $"{session.SceneId} first-session briefing"',
        'ArtifactId: $"artifact:{resume.SessionId}:first-session-briefing"',
    ),
    "src/Chummer.Play.Core/Application/PlayCampaignWorkspaceLiteProjector.cs": (
        "LaunchPrimerSummary",
        "LaunchPrimerProvenanceSummary",
        "FirstSessionBriefingSummary",
        "FirstSessionBriefingProvenanceSummary",
        "StarterArtifactContinuitySummary",
        "StarterArtifactContinuityLabels",
        'BuildStarterArtifactSummary("Starter primer"',
        'BuildStarterArtifactSummary("First-session briefing"',
    ),
    "src/Chummer.Play.Core/Roaming/RoamingWorkspaceSyncPlanner.cs": (
        "StarterPrimerFollowThrough",
        "StarterPrimerFollowThroughHref",
        "FirstSessionBriefingFollowThrough",
        "FirstSessionBriefingFollowThroughHref",
        'return $"/play?deviceId={Uri.EscapeDataString(targetDevice.InstallationId)}&role={Uri.EscapeDataString(ResolvePlayRole(targetDevice.DeviceRole))}";',
        'BuildStarterArtifactFollowThroughHref(targetDevice, artifactSessionId, "artifact:{sessionId}:starter-primer")',
        'BuildStarterArtifactFollowThroughHref(targetDevice, artifactSessionId, "artifact:{sessionId}:first-session-briefing")',
        'return $"/artifacts/{Uri.EscapeDataString(sessionId)}/{Uri.EscapeDataString(artifactId)}?deviceId={Uri.EscapeDataString(targetDevice.InstallationId)}&role={Uri.EscapeDataString(ResolvePlayRole(targetDevice.DeviceRole))}&view=travel";',
    ),
    "src/Chummer.Play.Core/Application/PlayEntryRecoveryProjector.cs": (
        'recommendedActionLabel = "Open starter primer";',
        'recommendedActionHref = restorePlan.StarterPrimerFollowThroughHref;',
        '"Starter primer lane: {restorePlan.StarterPrimerFollowThrough}"',
        '"First-session briefing lane: {restorePlan.FirstSessionBriefingFollowThrough}"',
    ),
    "src/Chummer.Play.Web/wwwroot/index.html": (
        'id="workspace-launch-primer"',
        'id="workspace-first-session-briefing"',
        'id="workspace-starter-artifact-continuity"',
        'id="restore-starter-primer-follow-through"',
        'id="restore-first-session-briefing-follow-through"',
        'setText("workspace-launch-primer", payload.launchPrimerSummary, "No starter primer summary is available yet.");',
        'setLink("workspace-launch-primer-link", payload.launchPrimerHref, "Open starter primer", "/artifacts", "Open starter primer");',
        'setText("workspace-first-session-briefing", payload.firstSessionBriefingSummary, "No first-session briefing summary is available yet.");',
        'setLink("workspace-first-session-briefing-link", payload.firstSessionBriefingHref, "Open first-session briefing", "/artifacts", "Open first-session briefing");',
        'setText("workspace-starter-artifact-continuity", payload.starterArtifactContinuitySummary, "No starter artifact continuity summary is available yet.");',
        'setText("restore-starter-primer-follow-through", payload.starterPrimerFollowThrough, "No travel starter-primer follow-through is available yet.");',
        'setText("restore-first-session-briefing-follow-through", payload.firstSessionBriefingFollowThrough, "No travel first-session briefing follow-through is available yet.");',
    ),
    "src/Chummer.Play.RegressionChecks/Program.cs": (
        'Assert(projection.LaunchPrimerSummary.Contains("Starter primer:", StringComparison.Ordinal)',
        'Assert(projection.FirstSessionBriefingSummary.Contains("First-session briefing:", StringComparison.Ordinal)',
        'Assert(projection.StarterArtifactContinuitySummary.Contains("Starter continuity:", StringComparison.Ordinal)',
        'Assert(plan.StarterPrimerFollowThrough.Contains("starter primer", StringComparison.OrdinalIgnoreCase)',
        'Assert(plan.StarterPrimerFollowThroughHref.Contains("deviceId=install-tablet", StringComparison.Ordinal)',
        'Assert(plan.FirstSessionBriefingFollowThrough.Contains("first-session briefing", StringComparison.OrdinalIgnoreCase)',
        'Assert(plan.FirstSessionBriefingFollowThroughHref.Contains("view=travel", StringComparison.Ordinal)',
        'Assert(noSessionProjection.RecommendedActionLabel.Contains("starter primer", StringComparison.OrdinalIgnoreCase)',
        'Assert(noCampaignProjection.RecommendedActionHref.Contains("starter-primer", StringComparison.OrdinalIgnoreCase)',
        'Assert(restorePlanTravel.StarterPrimerFollowThroughHref.Contains($"deviceId={Uri.EscapeDataString(expectedDeviceId)}", StringComparison.Ordinal)',
        'Assert(restorePlanTravel.FirstSessionBriefingFollowThroughHref.Contains("view=travel", StringComparison.Ordinal)',
    ),
    "scripts/materialize_mobile_local_release_proof.py": (
        PACKAGE_ID,
        FRONTIER_ID,
        "mobile_onboarding_continuity",
        "starter_onboarding:mobile",
        "first_session_briefing:mobile",
        "Mobile onboarding continuity criteria (M119)",
        "scripts/verify_next90_m119_mobile_onboarding_continuity.py",
    ),
    "scripts/ai/verify.sh": (
        "docs/next90-m119-mobile-onboarding-continuity.proof.md",
        "scripts/verify_next90_m119_mobile_onboarding_continuity.py",
        '"mobile_onboarding_continuity"',
    ),
}

QUEUE_BASE_TOKENS = (
    f"package_id: {PACKAGE_ID}",
    f"work_task_id: {WORK_TASK_ID}",
    f"milestone_id: {MILESTONE_ID}",
    "wave: W14",
    f"repo: {REPO_LABEL}",
    "task: Add travel and mobile starter continuity for primer and briefing artifacts.",
    "starter_onboarding:mobile",
    "first_session_briefing:mobile",
)

FLEET_QUEUE_TOKENS = QUEUE_BASE_TOKENS + (
    "status: complete",
    "completion_action: verify_closed_package_only",
    "do_not_reopen_reason:",
)

DESIGN_QUEUE_ALLOWED_STATES = ("status: in_progress", "status: complete")

REGISTRY_TOKENS = (
    "id: 119.3",
    "owner: chummer6-mobile",
    "title: Add travel/mobile starter continuity for primer, briefing, and recap artifacts.",
)

PROOF_TOKENS = (
    f"Package: `{PACKAGE_ID}`",
    f"Frontier: `{FRONTIER_ID}`",
    f"Work task: `{WORK_TASK_ID}`",
    f"Milestone: `{MILESTONE_ID}`",
    f"Concrete checkout root: `{CHECKOUT_ROOT}`",
    f"Canonical queue/registry repo label: `{REPO_LABEL}`",
    "starter_onboarding:mobile",
    "first_session_briefing:mobile",
    "starter primer",
    "first-session briefing",
    "claimed-device artifact lane",
    "The successor-wave registry row is complete, and the Fleet queue mirror already carries the closed-package repeat-prevention posture.",
    "The mirrored design staging queue still shows the same package row as `in_progress`",
)

SIGNOFF_TOKENS = (
    "Mobile onboarding continuity criteria (M119)",
    "LaunchPrimerSummary",
    "FirstSessionBriefingSummary",
    "StarterArtifactContinuitySummary",
    PACKAGE_ID,
)

GENERATED_TOKENS = (
    PACKAGE_ID,
    FRONTIER_ID,
    "mobile_onboarding_continuity",
    "starter_onboarding:mobile",
    "first_session_briefing:mobile",
    "docs/next90-m119-mobile-onboarding-continuity.proof.md",
    "scripts/verify_next90_m119_mobile_onboarding_continuity.py",
)

GENERATED_REQUIRED_MARKERS = (
    'Assert(projection.LaunchPrimerHref == "/artifacts/session-redmond/artifact%3Asession-redmond%3Astarter-primer?role=Player&view=personal"',
    'Assert(projection.FirstSessionBriefingHref == "/artifacts/session-redmond/artifact%3Asession-redmond%3Afirst-session-briefing?role=Player&view=travel"',
    'Assert(plan.StarterPrimerFollowThroughHref.Contains("deviceId=install-tablet", StringComparison.Ordinal)',
    'Assert(plan.FirstSessionBriefingFollowThroughHref.Contains("view=travel", StringComparison.Ordinal)',
    'Assert(noCampaignProjection.RecommendedActionHref.Contains("starter-primer", StringComparison.OrdinalIgnoreCase)',
    'Assert(restorePlanTravel.StarterPrimerFollowThroughHref.Contains($"deviceId={Uri.EscapeDataString(expectedDeviceId)}", StringComparison.Ordinal)',
    'Assert(restorePlanTravel.FirstSessionBriefingFollowThroughHref.Contains("view=travel", StringComparison.Ordinal)',
)

FORBIDDEN_PROOF_MARKERS = (
    "operator telemetry",
    "active-run helper",
    "supervisor status",
    "TASK_LOCAL_TELEMETRY.generated.json",
    "ACTIVE_RUN_HANDOFF.generated.md",
)

FORBIDDEN_IN_PROGRESS_ROW_MARKERS = (
)

EXPECTED_QUEUE_ROW = """- title: Add travel and mobile starter continuity for primer and briefing artifacts
  task: Add travel and mobile starter continuity for primer and briefing artifacts.
  package_id: next90-m119-mobile-onboarding-continuity
  work_task_id: 119.3
  milestone_id: 119
  status: complete
  wave: W14
  repo: chummer6-mobile
  completion_action: verify_closed_package_only
  do_not_reopen_reason: M119 chummer6-mobile is complete for starter primer continuity, first-session briefing continuity, and travel reopen follow-through. Future shards must verify the closed-package guard and proof evidence, and canonical row integrity before reopening this slice.
  allowed_paths:
  - src
  - tests
  - docs
  - scripts
  owned_surfaces:
  - starter_onboarding:mobile
  - first_session_briefing:mobile"""


def require_design_queue_row(label: str, text: str) -> list[str]:
    missing = require_tokens(label, text, QUEUE_BASE_TOKENS)
    if not any(token in text for token in DESIGN_QUEUE_ALLOWED_STATES):
        missing.append(f"{label}: expected one of {DESIGN_QUEUE_ALLOWED_STATES!r}")
    return missing


def read_text(path: Path) -> str:
    if not path.is_file():
        raise FileNotFoundError(path)
    return path.read_text(encoding="utf-8")


def require_tokens(label: str, text: str, tokens: tuple[str, ...]) -> list[str]:
    return [f"{label}: {token}" for token in tokens if token not in text]


def queue_block(text: str) -> str:
    position = text.find(f"package_id: {PACKAGE_ID}")
    if position < 0:
        return ""
    start = text.rfind("\n- title:", 0, position)
    end = text.find("\n- title:", position)
    if start < 0:
        start = 0
    if end < 0:
        end = len(text)
    return text[start:end]


def registry_block(text: str) -> str:
    match = re.search(r"(?ms)^      - id: 119\.3\b.*?(?=^      - id: |\Z)", text)
    return match.group(0) if match else ""


def main() -> int:
    missing: list[str] = []

    for relative_path, markers in IMPLEMENTATION_MARKERS.items():
        text = read_text(ROOT / relative_path)
        missing.extend(require_tokens(relative_path, text, markers))

    registry_text = read_text(REGISTRY)
    design_queue_text = read_text(DESIGN_QUEUE)
    fleet_queue_text = read_text(FLEET_QUEUE)
    proof_text = read_text(PROOF_DOC)
    signoff_text = read_text(PLAY_SIGNOFF)
    generated = json.loads(read_text(GENERATED_PROOF))
    generated_text = json.dumps(generated, indent=2, sort_keys=True)

    missing.extend(require_tokens("registry", registry_block(registry_text), REGISTRY_TOKENS))

    design_queue_block = queue_block(design_queue_text)
    missing.extend(require_design_queue_row("design queue", design_queue_block))

    fleet_queue_block = queue_block(fleet_queue_text)
    missing.extend(require_tokens("fleet queue", fleet_queue_block, FLEET_QUEUE_TOKENS))

    for forbidden in FORBIDDEN_IN_PROGRESS_ROW_MARKERS:
        if forbidden in design_queue_block:
            missing.append(f"design queue: forbidden in-progress marker present: {forbidden}")
        if forbidden in fleet_queue_block:
            missing.append(f"fleet queue: forbidden in-progress marker present: {forbidden}")

    missing.extend(require_tokens("proof doc", proof_text, PROOF_TOKENS))
    missing.extend(require_tokens("play signoff", signoff_text, SIGNOFF_TOKENS))
    missing.extend(require_tokens("generated proof", generated_text, GENERATED_TOKENS))

    package_receipts = generated.get("package_receipts", [])
    required_markers = generated.get("required_markers", {})
    marker_block = required_markers.get("mobile_onboarding_continuity")
    if not isinstance(marker_block, list):
        missing.append("generated proof: mobile_onboarding_continuity required markers are missing or not a list")
    else:
        for marker in GENERATED_REQUIRED_MARKERS:
            if marker not in marker_block:
                missing.append(f"generated proof: mobile_onboarding_continuity required markers missing {marker}")

    matching_receipts = [item for item in package_receipts if item.get("package_id") == PACKAGE_ID]
    if not matching_receipts:
        missing.append(f"generated proof: missing package receipt for {PACKAGE_ID}")
    else:
        if len(matching_receipts) != 1:
            missing.append(f"generated proof: expected exactly one package receipt for {PACKAGE_ID}, found {len(matching_receipts)}")
        receipt = matching_receipts[0]
        if receipt.get("status") != "implemented":
            missing.append(f"generated proof: expected implemented receipt for {PACKAGE_ID}, found {receipt.get('status')!r}")
        if receipt.get("title") != "Add travel and mobile starter continuity for primer and briefing artifacts.":
            missing.append(f"generated proof: unexpected title for {PACKAGE_ID}: {receipt.get('title')!r}")
        if tuple(receipt.get("owned_surfaces", [])) != ("starter_onboarding:mobile", "first_session_briefing:mobile"):
            missing.append("generated proof: owned surfaces drifted for M119")
        if receipt.get("proof_marker_set") != "mobile_onboarding_continuity":
            missing.append(f"generated proof: unexpected proof marker set {receipt.get('proof_marker_set')!r}")

    matching_marker_receipts = [item for item in package_receipts if item.get("proof_marker_set") == "mobile_onboarding_continuity"]
    if len(matching_marker_receipts) != 1:
        missing.append(f"generated proof: expected exactly one mobile_onboarding_continuity proof-marker receipt, found {len(matching_marker_receipts)}")

    matching_proof_receipts = [item for item in package_receipts if item.get("proof_receipt") == "docs/next90-m119-mobile-onboarding-continuity.proof.md"]
    if len(matching_proof_receipts) != 1:
        missing.append(f"generated proof: expected exactly one receipt for docs/next90-m119-mobile-onboarding-continuity.proof.md, found {len(matching_proof_receipts)}")

    for forbidden in FORBIDDEN_PROOF_MARKERS:
        if forbidden.lower() in proof_text.lower():
            missing.append(f"proof doc: forbidden marker present: {forbidden}")

    if missing:
        for item in missing:
            print(f"m119_mobile_onboarding_continuity_verify_failed: {item}", file=sys.stderr)
        return 1

    print("m119_mobile_onboarding_continuity_verify_ok")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
