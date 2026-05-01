#!/usr/bin/env python3
from __future__ import annotations

import json
import re
import subprocess
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
PACKAGE_ID = "next90-m110-mobile-runsite-orientation"
FRONTIER_ID = "3664656855"
MILESTONE_ID = "110"
WORK_TASK_ID = "110.5"
LANDED_COMMIT = "bc84b37"
REPO_LABEL = "chummer6-mobile"
CHECKOUT_ROOT = str(ROOT)
ALLOWED_PREFIXES = ("src/", "tests/", "docs/", "scripts/", ".codex-studio/published/")

REGISTRY = Path("/docker/chummercomplete/chummer-design/products/chummer/NEXT_90_DAY_PRODUCT_ADVANCE_REGISTRY.yaml")
DESIGN_QUEUE = Path("/docker/chummercomplete/chummer-design/products/chummer/NEXT_90_DAY_QUEUE_STAGING.generated.yaml")
FLEET_QUEUE = Path("/docker/fleet/.codex-studio/published/NEXT_90_DAY_QUEUE_STAGING.generated.yaml")
PROOF_DOC = ROOT / "docs" / "next90-m110-mobile-runsite-orientation.proof.md"
GENERATED_PROOF = ROOT / ".codex-studio" / "published" / "MOBILE_LOCAL_RELEASE_PROOF.generated.json"

IMPLEMENTATION_MARKERS = {
    "src/Chummer.Play.Core/Application/PlayCampaignWorkspaceServerPlane.cs": (
        "runsite_host_mode:mobile surface and the runsite_host_mode:entry and campaign_orientation:mobile surfaces",
        "pre-session-orientation-only-not-tactical-truth",
    ),
    "src/Chummer.Play.Core/Application/PlayCampaignWorkspaceLiteProjector.cs": (
        'RunsiteOrientationSummary',
        'RunsiteOrientationHref',
        'RunsiteOrientationTruthPosture',
        'ViewId: "runsite_host_mode"',
    ),
    "src/Chummer.Play.Web/PlayWebApplication.cs": (
        '"runsite-orientation" or "runsite_orientation" => "runsite_host_mode"',
        '"campaign_orientation"',
        "artifactSurface",
    ),
    "src/Chummer.Play.Web/wwwroot/index.html": (
        'id="entry-runsite-orientation-link"',
        'id="workspace-runsite-orientation-link"',
        'id="restore-runsite-orientation-link"',
        "Active in-shell launch:",
        "Active onboarding launch:",
        "Active travel-shell launch:",
        "orientationSurface=campaign_orientation",
    ),
    "src/Chummer.Play.RegressionChecks/Program.cs": (
        'Assert(projection.RunsiteOrientationProvenanceSummary.Contains("runsite_host_mode:mobile", StringComparison.Ordinal)',
        'Assert(projection.RunsiteOrientationProvenanceSummary.Contains("runsite_host_mode:entry", StringComparison.Ordinal)',
        'Assert(projection.RunsiteOrientationHref.Contains("orientationSurface=campaign_orientation", StringComparison.Ordinal)',
        'Assert(plan.RunsiteOrientationFollowThroughHref.Contains("artifactSurface=runsite_host_mode", StringComparison.Ordinal)',
        'Assert(runsiteOrientationResponse.Location.Contains("artifactSurface=runsite_host_mode", StringComparison.Ordinal)',
        "direct runsite orientation artifact route must infer the runsite_host_mode surface",
        "direct runsite orientation artifact route must infer campaign-orientation context",
    ),
    "scripts/materialize_mobile_local_release_proof.py": (
        PACKAGE_ID,
        FRONTIER_ID,
        "scripts/verify_next90_m110_mobile_runsite_orientation.py",
    ),
    "scripts/ai/verify.sh": (
        "docs/next90-m110-mobile-runsite-orientation.proof.md",
        "next90-m110-mobile-runsite-orientation",
        "python3 scripts/verify_next90_m110_mobile_runsite_orientation.py >/dev/null",
    ),
}

QUEUE_TOKENS = (
    f"package_id: {PACKAGE_ID}",
    f"frontier_id: {FRONTIER_ID}",
    f"work_task_id: {WORK_TASK_ID}",
    f"milestone_id: {MILESTONE_ID}",
    f"repo: {REPO_LABEL}",
    "status: complete",
    f"landed_commit: {LANDED_COMMIT}",
    "completion_action: verify_closed_package_only",
    "do_not_reopen_reason: M110 chummer6-mobile runsite orientation is complete",
    "task: Launch runsite host mode from campaign and travel shells before live play without replacing inspectable route truth.",
    "runsite_host_mode:mobile",
    "campaign_orientation:mobile",
    "/docker/chummercomplete/chummer-play/src/Chummer.Play.Core/Application/PlayCampaignWorkspaceLiteProjector.cs",
    "/docker/chummercomplete/chummer-play/src/Chummer.Play.Core/Application/PlayCampaignWorkspaceServerPlane.cs",
    "/docker/chummercomplete/chummer-play/src/Chummer.Play.Web/PlayWebApplication.cs",
    "/docker/chummercomplete/chummer-play/src/Chummer.Play.Web/wwwroot/index.html",
    "/docker/chummercomplete/chummer-play/src/Chummer.Play.RegressionChecks/Program.cs",
    "/docker/chummercomplete/chummer-play/scripts/materialize_mobile_local_release_proof.py",
    "/docker/chummercomplete/chummer-play/scripts/ai/verify.sh",
    "/docker/chummercomplete/chummer-play/.codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json",
)

REGISTRY_IMPLEMENTATION_TOKENS = (
    "id: 110.3",
    "owner: chummer6-mobile",
    "status: complete",
    "Surface orientation entry points and preview-safe pre-session launch flows for campaign members.",
    "runsite_host_mode:entry, campaign_orientation:mobile, and pre-session-orientation-only-not-tactical-truth provenance",
    "RunsiteOrientationSummary, RunsiteOrientationHref, RunsiteOrientationProvenanceSummary, RunsiteOrientationTruthPosture",
    "RunsiteOrientationFollowThrough and RunsiteOrientationFollowThroughHref",
    "scripts/verify_next90_m110_mobile_runsite_orientation_entry.py fail-closes canonical queue and registry proof",
)

REGISTRY_CLOSURE_TOKENS = (
    "id: 110.5",
    "owner: chummer6-mobile",
    "status: complete",
    "Close the mobile runsite orientation successor package against the canonical queue and local proof receipt.",
    f"landed_commit: {LANDED_COMMIT}",
    "docs/next90-m110-mobile-runsite-orientation.proof.md",
    "scripts/verify_next90_m110_mobile_runsite_orientation.py fail-closes canonical queue, design queue, registry, generated-proof package receipt, landed-commit scope, and worker-unsafe proof drift",
    "MOBILE_LOCAL_RELEASE_PROOF.generated.json includes a `package_receipts` entry for `next90-m110-mobile-runsite-orientation`",
    "scripts/materialize_mobile_local_release_proof.py emits the M110 successor package receipt",
    "commit f95c490 lands the mobile campaign-shell and travel-shell runsite host-mode follow-through",
    f"commit {LANDED_COMMIT} stabilizes the closure verifier and release-proof receipt",
    "NEXT_90_DAY_QUEUE_STAGING.generated.yaml closes `next90-m110-mobile-runsite-orientation` on `verify_closed_package_only`",
    "python3 scripts/verify_next90_m110_mobile_runsite_orientation.py exits 0.",
)

PROOF_TOKENS = (
    f"Package: `{PACKAGE_ID}`",
    f"Frontier: `{FRONTIER_ID}`",
    f"Work task: `{WORK_TASK_ID}`",
    f"Concrete checkout root: `{CHECKOUT_ROOT}`",
    f"Canonical queue/registry repo label: `{REPO_LABEL}`",
    "runsite_host_mode:mobile",
    "campaign_orientation:mobile",
    "queue-closure guard",
    "existing M110 implementation row `110.3`",
    "Runsite host mode remains pre-session orientation only.",
    "The package is materially complete for the `chummer6-mobile` slice in this checkout",
    "Canonical closeout anchors:",
    "NEXT_90_DAY_PRODUCT_ADVANCE_REGISTRY.yaml",
    "NEXT_90_DAY_QUEUE_STAGING.generated.yaml",
    "scripts/verify_next90_m110_mobile_runsite_orientation.py",
    "verify_closed_package_only",
    "Future shards should verify these anchors instead of reopening",
)

GENERATED_MARKERS = (
    PACKAGE_ID,
    "Runsite host and orientation launch criteria (M110)",
)

FORBIDDEN_PROOF_MARKERS = (
    "operator telemetry",
    "active-run helper",
    "supervisor status",
    "TASK_LOCAL_TELEMETRY.generated.json",
    "ACTIVE_RUN_HANDOFF.generated.md",
)


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
    start = text.rfind("\n  - title:", 0, position)
    end = text.find("\n  - title:", position)
    if start < 0:
        start = 0
    if end < 0:
        end = len(text)
    return text[start:end]


def require_single_queue_row(label: str, text: str) -> list[str]:
    count = len(re.findall(rf"(?m)^    package_id: {re.escape(PACKAGE_ID)}$", text))
    if count != 1:
        return [f"{label}: expected exactly one {PACKAGE_ID} row, found {count}"]
    return []


def registry_block(text: str) -> str:
    match = re.search(r"(?ms)^      - id: 110\.3\b.*?(?=^      - id: |\Z)", text)
    return match.group(0) if match else ""


def registry_closure_block(text: str) -> str:
    match = re.search(r"(?ms)^      - id: 110\.5\b.*?(?=^      - id: |\Z)", text)
    return match.group(0) if match else ""


def git_lines(args: list[str]) -> list[str]:
    result = subprocess.run(
        ["git", *args],
        cwd=ROOT,
        check=True,
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
    )
    return [line.strip() for line in result.stdout.splitlines() if line.strip()]


def verify_landed_commit_scope() -> list[str]:
    missing: list[str] = []
    try:
        git_lines(["cat-file", "-e", f"{LANDED_COMMIT}^{{commit}}"])
    except subprocess.CalledProcessError:
        return [f"landed commit does not resolve locally: {LANDED_COMMIT}"]

    changed = git_lines(["diff-tree", "--no-commit-id", "--name-only", "-r", LANDED_COMMIT])
    if not changed:
        missing.append(f"landed commit has no changed files: {LANDED_COMMIT}")

    out_of_scope = [path for path in changed if not path.startswith(ALLOWED_PREFIXES)]
    if out_of_scope:
        missing.append(f"landed commit changed paths outside package proof scope: {', '.join(out_of_scope)}")

    return missing


def require_clean_markers(label: str, text: str, markers: tuple[str, ...]) -> list[str]:
    lowered = text.lower()
    return [f"{label}: forbidden marker {marker}" for marker in markers if marker.lower() in lowered]


def main() -> int:
    missing: list[str] = []

    queue_text = read_text(FLEET_QUEUE)
    design_queue_text = read_text(DESIGN_QUEUE)
    registry_text = read_text(REGISTRY)
    proof_text = read_text(PROOF_DOC)

    queue_row = queue_block(queue_text)
    design_queue_row = queue_block(design_queue_text)
    registry_row = registry_block(registry_text)
    registry_closure_row = registry_closure_block(registry_text)

    missing.extend(require_single_queue_row("fleet queue", queue_text))
    missing.extend(require_single_queue_row("design queue", design_queue_text))
    missing.extend(require_tokens("fleet queue", queue_row, QUEUE_TOKENS))
    missing.extend(require_tokens("design queue", design_queue_row, QUEUE_TOKENS))
    missing.extend(require_tokens("registry implementation row", registry_row, REGISTRY_IMPLEMENTATION_TOKENS))
    missing.extend(require_tokens("registry closure row", registry_closure_row, REGISTRY_CLOSURE_TOKENS))
    missing.extend(require_tokens("proof doc", proof_text, PROOF_TOKENS))
    missing.extend(require_clean_markers("registry implementation row", registry_row, FORBIDDEN_PROOF_MARKERS))
    missing.extend(require_clean_markers("registry closure row", registry_closure_row, FORBIDDEN_PROOF_MARKERS))
    missing.extend(require_clean_markers("fleet queue", queue_row, FORBIDDEN_PROOF_MARKERS))
    missing.extend(require_clean_markers("design queue", design_queue_row, FORBIDDEN_PROOF_MARKERS))
    missing.extend(require_clean_markers("proof doc", proof_text, FORBIDDEN_PROOF_MARKERS))

    for relative_path, tokens in IMPLEMENTATION_MARKERS.items():
        file_text = read_text(ROOT / relative_path)
        missing.extend(require_tokens(relative_path, file_text, tokens))

    payload = json.loads(read_text(GENERATED_PROOF))
    if payload.get("contract_name") != "chummer6-mobile.local_release_proof":
        missing.append("generated proof has wrong contract_name")
    if payload.get("status") != "passed":
        missing.append("generated proof is not passed")
    package_receipts = payload.get("package_receipts", [])
    if not isinstance(package_receipts, list):
        missing.append("generated proof package_receipts is not a list")
    else:
        m110_receipt = next((item for item in package_receipts if item.get("package_id") == PACKAGE_ID), None)
        if m110_receipt is None:
            missing.append("generated proof missing M110 successor package receipt")
        else:
            if m110_receipt.get("frontier_id") != FRONTIER_ID:
                missing.append("generated proof M110 successor package receipt has wrong frontier_id")
            if str(m110_receipt.get("milestone_id")) != MILESTONE_ID:
                missing.append("generated proof M110 successor package receipt has wrong milestone_id")
            if m110_receipt.get("status") != "complete":
                missing.append("generated proof M110 successor package receipt is not complete")
            if m110_receipt.get("proof_marker_set") != "runsite_orientation_launch":
                missing.append("generated proof M110 successor package receipt has wrong proof_marker_set")
            if m110_receipt.get("proof_doc") != "docs/next90-m110-mobile-runsite-orientation.proof.md":
                missing.append("generated proof M110 successor package receipt has wrong proof_doc")
            if m110_receipt.get("verifier") != "scripts/verify_next90_m110_mobile_runsite_orientation.py":
                missing.append("generated proof M110 successor package receipt has wrong verifier")

    markers = payload.get("required_markers", {}).get("runsite_orientation_launch", [])
    if not isinstance(markers, list):
        missing.append("generated proof runsite_orientation_launch markers are not a list")
    else:
        marker_text = "\n".join(str(item) for item in markers)
        missing.extend(require_tokens("generated proof", marker_text, GENERATED_MARKERS))

    missing.extend(verify_landed_commit_scope())

    if missing:
        for item in missing:
            print(f"m110_mobile_runsite_orientation_missing: {item}", file=sys.stderr)
        return 1

    print("m110 mobile runsite orientation proof ok")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
