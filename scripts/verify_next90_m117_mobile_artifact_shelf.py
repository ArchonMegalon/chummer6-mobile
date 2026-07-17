#!/usr/bin/env python3
from __future__ import annotations

import json
import re
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
PACKAGE_ID = "next90-m117-mobile-artifact-shelf"
SUCCESSOR_FRONTIER_ID = "3440617449"
ACTIVE_FLAGSHIP_FRONTIER_ID = "3371889980"
FRONTIER_IDS = [SUCCESSOR_FRONTIER_ID, ACTIVE_FLAGSHIP_FRONTIER_ID]
MILESTONE_ID = "117"
WORK_TASK_ID = "117.4"
REPO_LABEL = "chummer6-mobile"
CHECKOUT_ROOT = str(ROOT)
QUEUE_PROOF_ROOT = CHECKOUT_ROOT

REGISTRY = Path("/docker/chummercomplete/chummer-design/products/chummer/NEXT_90_DAY_PRODUCT_ADVANCE_REGISTRY.yaml")
DESIGN_QUEUE = Path("/docker/chummercomplete/chummer-design/products/chummer/NEXT_90_DAY_QUEUE_STAGING.generated.yaml")
FLEET_QUEUE = Path("/docker/fleet/.codex-studio/published/NEXT_90_DAY_QUEUE_STAGING.generated.yaml")
PROOF_DOC = ROOT / "docs" / "next90-m117-mobile-artifact-shelf.proof.md"
PLAY_SIGNOFF = ROOT / "docs" / "PLAY_RELEASE_SIGNOFF.md"
GENERATED_PROOF = ROOT / ".codex-studio" / "published" / "MOBILE_LOCAL_RELEASE_PROOF.generated.json"

IMPLEMENTATION_MARKERS = {
    "src/Chummer.Play.Core/Application/PlayCampaignWorkspaceLiteProjector.cs": (
        "ArtifactShelfSelectionSummary",
        "string selectedArtifactView,",
        'PlayArtifactShelfViewLink[] artifactShelfViews = BuildArtifactShelfViews(',
        'BuildArtifactShelfSelectionSummary(',
        'ViewId: "travel"',
        'Label: "Travel cache"',
        'BuildArtifactShelfHref(sessionId, role, "travel")',
        'HumanizeArtifactShelfViewId(selectedArtifactView)',
        '=> selectedArtifactView switch',
        '=> $"/artifacts/{Uri.EscapeDataString(sessionId)}?role={Uri.EscapeDataString(role.ToString())}&view={Uri.EscapeDataString(viewId)}";',
    ),
    "src/Chummer.Play.Web/PlayWebApplication.cs": (
        'string? artifactView,',
        'string? artifactId,',
        'PlayCampaignWorkspaceLiteProjector.Create(response, artifactView, artifactId)',
        '"/artifacts/{sessionId}"',
        '"/artifacts/{sessionId}/{artifactId}"',
        'BuildPlayIndexHref(sessionId, deviceId, role, artifactView: view)',
        'BuildPlayIndexHref(sessionId, deviceId, role, artifactView: view, artifactId: artifactId)',
        'queryParts.Add($"artifactView={Uri.EscapeDataString(artifactView)}");',
        'queryParts.Add($"artifactId={Uri.EscapeDataString(artifactId)}");',
    ),
    "src/Chummer.Play.Web/wwwroot/index.html": (
        'id="workspace-artifact-shelf-summary"',
        'id="workspace-artifact-selection"',
        'id="workspace-artifact-selection-link"',
        'id="workspace-artifact-shelf-link"',
        'setText("workspace-artifact-shelf-summary", payload.artifactShelfSelectionSummary, "No mobile artifact shelf summary is available yet.");',
        'setText("workspace-artifact-selection", payload.selectedRecapArtifactSummary, "Selected recap artifact: no recap artifact is pinned yet.");',
        'setLink("workspace-artifact-selection-link", payload.selectedRecapArtifactHref || selectedArtifactView?.href, artifactId',
        'document.getElementById("workspace-artifact-shelf-link").href = selectedArtifactView?.href || "/artifacts";',
        '? `Browse ${selectedArtifactView.label}`',
        'const artifactView = params.get("artifactView") || "";',
        'const artifactId = params.get("artifactId") || "";',
        'workspaceQuery.set("artifactView", artifactView);',
        'workspaceQuery.set("artifactId", artifactId);',
        'renderWorkspace(payload, artifactId);',
    ),
    "src/Chummer.Play.RegressionChecks/Program.cs": (
        'var travelProjection = PlayCampaignWorkspaceLiteProjector.Create(response, artifactView: "travel");',
        'var recapProjection = PlayCampaignWorkspaceLiteProjector.Create(response, artifactView: "travel", artifactId: "artifact-recap-1");',
        'Assert(projection.ArtifactShelfViews.Count == 4',
        'Assert(projection.ArtifactShelfViews.Any(item => item.ViewId == "travel" && item.Label == "Travel cache" && item.Href == "/artifacts/session-redmond?role=Player&view=travel")',
        'Assert(travelProjection.SelectedArtifactView == "travel"',
        'Assert(travelProjection.ArtifactShelfSelectionSummary.Contains("Travel shelf:", StringComparison.Ordinal)',
        'Assert(recapProjection.SelectedRecapArtifactSummary.Contains("artifact-recap-1", StringComparison.Ordinal)',
        'Assert(recapProjection.SelectedRecapArtifactSummary.Contains("travel shelf", StringComparison.OrdinalIgnoreCase)',
        'Assert(recapProjection.SelectedRecapArtifactHref == "/artifacts/session-redmond/artifact-recap-1?role=Player&view=travel"',
        'Assert(observerProjection.ArtifactShelfViews.Any(item => item.ViewId == "travel" && item.Href.Contains("role=Observer", StringComparison.Ordinal))',
        'Assert(gmProjection.ArtifactShelfViews.Any(item => item.ViewId == "travel" && item.Href.Contains("role=GameMaster", StringComparison.Ordinal))',
        'Assert(html.Contains("id=\\"workspace-artifact-shelf-summary\\"", StringComparison.Ordinal)',
        'Assert(html.Contains("id=\\"workspace-artifact-selection\\"", StringComparison.Ordinal)',
        'Assert(html.Contains("id=\\"workspace-artifact-selection-link\\"", StringComparison.Ordinal)',
        'Assert(html.Contains("setText(\\"workspace-artifact-shelf-summary\\", payload.artifactShelfSelectionSummary, \\"No mobile artifact shelf summary is available yet.\\");", StringComparison.Ordinal)',
        'Assert(html.Contains("setText(\\"workspace-artifact-selection\\", payload.selectedRecapArtifactSummary, \\"Selected recap artifact: no recap artifact is pinned yet.\\");", StringComparison.Ordinal)',
        'Assert(html.Contains("setLink(\\"workspace-artifact-selection-link\\", payload.selectedRecapArtifactHref || selectedArtifactView?.href, artifactId", StringComparison.Ordinal)',
        'Assert(html.Contains("? `Browse ${selectedArtifactView.label}`", StringComparison.Ordinal)',
        'Assert(html.Contains("const artifactView = params.get(\\"artifactView\\") || \\"\\";", StringComparison.Ordinal)',
        'Assert(html.Contains("const artifactId = params.get(\\"artifactId\\") || \\"\\";", StringComparison.Ordinal)',
        'Assert(html.Contains("workspaceQuery.set(\\"artifactId\\", artifactId);", StringComparison.Ordinal)',
        'Assert(html.Contains("renderWorkspace(payload, artifactId);", StringComparison.Ordinal)',
        'Assert(artifactShelfBrowseRedirect.Location == $"/index.html?sessionId={Uri.EscapeDataString(sessionId)}&role={Uri.EscapeDataString(role.ToString())}&artifactView=campaign"',
        'Assert(artifactShelfRedirect.Location == $"/index.html?sessionId={Uri.EscapeDataString(sessionId)}&role={Uri.EscapeDataString(role.ToString())}&artifactView=travel&artifactId=artifact-recap"',
    ),
    "scripts/materialize_mobile_local_release_proof.py": (
        PACKAGE_ID,
        SUCCESSOR_FRONTIER_ID,
        ACTIVE_FLAGSHIP_FRONTIER_ID,
        '"title": "Add mobile artifact shelf views for campaign, travel, and recap artifacts."',
        '"repo": REPO_LABEL',
        '"checkout_root": CHECKOUT_ROOT',
        '"allowed_paths": [',
        '"src",',
        '"tests",',
        '"docs",',
        '"scripts",',
        "artifact_shelf:mobile",
        "artifact_recap_view:mobile",
        "Mobile artifact shelf criteria (M117)",
        "scripts/verify_next90_m117_mobile_artifact_shelf.py",
    ),
    "scripts/ai/verify.sh": (
        "docs/next90-m117-mobile-artifact-shelf.proof.md",
        "next90-m117-mobile-artifact-shelf",
        "python3 scripts/verify_next90_m117_mobile_artifact_shelf.py >/dev/null",
    ),
}

QUEUE_BASE_TOKENS = (
    f"package_id: {PACKAGE_ID}",
    f"work_task_id: {WORK_TASK_ID}",
    f"frontier_id: {SUCCESSOR_FRONTIER_ID}",
    f"milestone_id: {MILESTONE_ID}",
    "status: complete",
    "completion_action: verify_closed_package_only",
    "do_not_reopen_reason: M117 chummer6-mobile artifact shelf is complete",
    "wave: W13",
    f"repo: {REPO_LABEL}",
    "task: Add mobile artifact shelf views for campaign, travel, and recap artifacts.",
    "artifact_shelf:mobile",
    "artifact_recap_view:mobile",
)

DESIGN_QUEUE_TOKENS = QUEUE_BASE_TOKENS + (
    f"{QUEUE_PROOF_ROOT}/docs/next90-m117-mobile-artifact-shelf.proof.md",
    f"{QUEUE_PROOF_ROOT}/scripts/verify_next90_m117_mobile_artifact_shelf.py",
    f"{QUEUE_PROOF_ROOT}/scripts/materialize_mobile_local_release_proof.py",
    f"{QUEUE_PROOF_ROOT}/.codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json",
)

FLEET_QUEUE_TOKENS = QUEUE_BASE_TOKENS + (
    f"{CHECKOUT_ROOT}/docs/next90-m117-mobile-artifact-shelf.proof.md",
    f"{CHECKOUT_ROOT}/scripts/verify_next90_m117_mobile_artifact_shelf.py",
    f"{CHECKOUT_ROOT}/scripts/materialize_mobile_local_release_proof.py",
    f"{CHECKOUT_ROOT}/.codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json",
)

REGISTRY_TOKENS = (
    "id: 117.4",
    "owner: chummer6-mobile",
    "title: Add mobile artifact shelf views for campaign, travel, and recap artifacts.",
)

PROOF_TOKENS = (
    f"Package: `{PACKAGE_ID}`",
    f"Successor frontier: `{SUCCESSOR_FRONTIER_ID}`",
    f"Active flagship frontier: `{ACTIVE_FLAGSHIP_FRONTIER_ID}`",
    f"Work task: `{WORK_TASK_ID}`",
    f"Milestone: `{MILESTONE_ID}`",
    f"Concrete checkout root: `{CHECKOUT_ROOT}`",
    f"Canonical queue/registry repo label: `{REPO_LABEL}`",
    "artifact_shelf:mobile",
    "artifact_recap_view:mobile",
    "campaign, travel, and recap shelf visibility",
    "session-aware and role-aware",
    "Selected recap artifact",
    "selected travel shelf",
    "Queue-closure guard",
    "Canonical closeout anchors:",
    "NEXT_90_DAY_PRODUCT_ADVANCE_REGISTRY.yaml",
    "NEXT_90_DAY_QUEUE_STAGING.generated.yaml",
    "verify_closed_package_only",
    "Future shards should verify these anchors instead of reopening the package.",
    "MOBILE_LOCAL_RELEASE_PROOF.generated.json",
    "package receipt",
    "The package is materially complete for the `chummer6-mobile` slice in this checkout.",
)

SIGNOFF_TOKENS = (
    "Mobile artifact shelf criteria (M117)",
    "SelectedArtifactView",
    "ArtifactShelfSelectionSummary",
    "next90-m117-mobile-artifact-shelf",
)

GENERATED_TOKENS = (
    PACKAGE_ID,
    SUCCESSOR_FRONTIER_ID,
    ACTIVE_FLAGSHIP_FRONTIER_ID,
    REPO_LABEL,
    CHECKOUT_ROOT,
    "artifact_shelf:mobile",
    "artifact_recap_view:mobile",
    "Mobile artifact shelf criteria (M117)",
    "docs/next90-m117-mobile-artifact-shelf.proof.md",
    "scripts/verify_next90_m117_mobile_artifact_shelf.py",
)

GENERATED_REQUIRED_MARKERS = (
    'Assert(projection.ArtifactShelfSelectionSummary.Contains("My stuff shelf:", StringComparison.Ordinal)',
    'Assert(projection.ArtifactShelfViews.Count == 4',
    'Assert(projection.ArtifactShelfViews.Any(item => item.ViewId == "travel" && item.Label == "Travel cache" && item.Href == "/artifacts/session-redmond?role=Player&view=travel")',
    'Assert(travelProjection.SelectedArtifactView == "travel"',
    'Assert(travelProjection.ArtifactShelfSelectionSummary.Contains("Travel shelf:", StringComparison.Ordinal)',
    'Assert(recapProjection.SelectedRecapArtifactSummary.Contains("travel shelf", StringComparison.OrdinalIgnoreCase)',
    'setText("workspace-artifact-shelf-summary", payload.artifactShelfSelectionSummary, "No mobile artifact shelf summary is available yet.");',
    'setText("workspace-artifact-selection", payload.selectedRecapArtifactSummary, "Selected recap artifact: no recap artifact is pinned yet.");',
    'setLink("workspace-artifact-selection-link", payload.selectedRecapArtifactHref || selectedArtifactView?.href, artifactId',
    'document.getElementById("workspace-artifact-shelf-link").href = selectedArtifactView?.href || "/artifacts";',
    '? `Browse ${selectedArtifactView.label}`',
    'const artifactView = params.get("artifactView") || "";',
    'const artifactId = params.get("artifactId") || "";',
    'workspaceQuery.set("artifactView", artifactView);',
    'workspaceQuery.set("artifactId", artifactId);',
    'renderWorkspace(payload, artifactId);',
    'Assert(artifactShelfBrowseRedirect.Location == $"/index.html?sessionId={Uri.EscapeDataString(sessionId)}&role={Uri.EscapeDataString(role.ToString())}&artifactView=campaign"',
    'Assert(artifactShelfRedirect.Location == $"/index.html?sessionId={Uri.EscapeDataString(sessionId)}&role={Uri.EscapeDataString(role.ToString())}&artifactView=travel&artifactId=artifact-recap"',
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


def require_clean_markers(label: str, text: str, markers: tuple[str, ...]) -> list[str]:
    lowered = text.lower()
    return [f"{label}: forbidden marker present: {marker}" for marker in markers if marker.lower() in lowered]


def require_exact_mapping_keys(
    label: str,
    payload: dict[str, object],
    expected_keys: tuple[str, ...]) -> list[str]:
    actual_keys = tuple(sorted(payload.keys()))
    if actual_keys != tuple(sorted(expected_keys)):
        return [f"{label}: keys drifted from the canonical M117 implementation-only receipt shape"]
    return []


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
    match = re.search(r"(?ms)^      - id: 117\.4\b.*?(?=^      - id: |\Z)", text)
    return match.group(0) if match else ""


def count_queue_blocks(text: str) -> int:
    return len(re.findall(rf"(?m)^  package_id: {re.escape(PACKAGE_ID)}$", text))


def count_registry_blocks(text: str) -> int:
    return len(re.findall(r"(?m)^      - id: 117\.4\b", text))


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

    registry_match_count = count_registry_blocks(registry_text)
    if registry_match_count != 1:
        missing.append(f"registry: expected exactly one row for 117.4, found {registry_match_count}")
    missing.extend(require_tokens("registry", registry_block(registry_text), REGISTRY_TOKENS))

    for label, queue_text, queue_tokens in (
        ("design queue", design_queue_text, DESIGN_QUEUE_TOKENS),
        ("fleet queue", fleet_queue_text, FLEET_QUEUE_TOKENS),
    ):
        match_count = count_queue_blocks(queue_text)
        if match_count != 1:
            missing.append(f"{label}: expected exactly one row for {PACKAGE_ID}, found {match_count}")
        block = queue_block(queue_text)
        missing.extend(require_tokens(label, block, queue_tokens))

    missing.extend(require_tokens("proof doc", proof_text, PROOF_TOKENS))
    missing.extend(require_tokens("play signoff", signoff_text, SIGNOFF_TOKENS))
    missing.extend(require_tokens("generated proof", generated_text, GENERATED_TOKENS))

    if generated.get("contract_name") != "chummer6-mobile.local_release_proof":
        missing.append("generated proof: wrong contract_name")
    if generated.get("status") != "passed":
        missing.append("generated proof: status is not passed")

    source_files = generated.get("source_files")
    if not isinstance(source_files, list):
        missing.append("generated proof: source_files missing")
    else:
        for required_source in (
            "docs/next90-m117-mobile-artifact-shelf.proof.md",
            "scripts/verify_next90_m117_mobile_artifact_shelf.py",
        ):
            if required_source not in source_files:
                missing.append(f"generated proof: source_files missing {required_source}")

    journeys = generated.get("journeys_passed")
    if not isinstance(journeys, list) or "mobile_artifact_shelf" not in journeys:
        missing.append("generated proof: mobile_artifact_shelf journey missing")

    required_markers = generated.get("required_markers")
    if not isinstance(required_markers, dict) or "mobile_artifact_shelf" not in required_markers:
        missing.append("generated proof: mobile_artifact_shelf required markers missing")
    else:
        mobile_markers = required_markers.get("mobile_artifact_shelf")
        if not isinstance(mobile_markers, list):
            missing.append("generated proof: mobile_artifact_shelf required markers are not a list")
        else:
            for required_marker in GENERATED_REQUIRED_MARKERS:
                if required_marker not in mobile_markers:
                    missing.append(f"generated proof: mobile_artifact_shelf markers missing {required_marker}")

    package_receipts = generated.get("package_receipts", [])
    if not isinstance(package_receipts, list):
        missing.append("generated proof: package_receipts missing")
        package_receipts = []

    receipt_dicts = [item for item in package_receipts if isinstance(item, dict)]
    if len(receipt_dicts) != len(package_receipts):
        missing.append("generated proof: package_receipts must contain only mappings")

    matching_receipts = [
        item for item in receipt_dicts
        if item.get("package_id") == PACKAGE_ID
    ]
    if not matching_receipts:
        missing.append(f"generated proof: missing package receipt for {PACKAGE_ID}")
    else:
        if len(matching_receipts) != 1:
            missing.append(f"generated proof: expected exactly one package receipt for {PACKAGE_ID}, found {len(matching_receipts)}")
        receipt = matching_receipts[0]
        missing.extend(require_exact_mapping_keys(
            "generated proof: M117 receipt",
            receipt,
            (
                "active_flagship_frontier_id",
                "allowed_paths",
                "checkout_root",
                "frontier_id",
                "frontier_ids",
                "milestone_id",
                "owned_surfaces",
                "package_id",
                "proof_marker_set",
                "proof_receipt",
                "repo",
                "status",
                "title",
                "work_task_id",
            )))
        if receipt.get("title") != "Add mobile artifact shelf views for campaign, travel, and recap artifacts.":
            missing.append("generated proof: wrong title for M117 receipt")
        if receipt.get("status") != "closed":
            missing.append(f"generated proof: expected closed receipt for {PACKAGE_ID}, found {receipt.get('status')!r}")
        if receipt.get("repo") != REPO_LABEL:
            missing.append(f"generated proof: expected repo {REPO_LABEL} for {PACKAGE_ID}, found {receipt.get('repo')!r}")
        if receipt.get("checkout_root") != CHECKOUT_ROOT:
            missing.append(f"generated proof: expected checkout_root {CHECKOUT_ROOT} for {PACKAGE_ID}, found {receipt.get('checkout_root')!r}")
        if tuple(receipt.get("allowed_paths", [])) != ("src", "tests", "docs", "scripts"):
            missing.append(f"generated proof: expected allowed_paths ['src', 'tests', 'docs', 'scripts'] for {PACKAGE_ID}, found {receipt.get('allowed_paths')!r}")
        if receipt.get("frontier_id") != SUCCESSOR_FRONTIER_ID:
            missing.append(
                f"generated proof: expected successor frontier_id {SUCCESSOR_FRONTIER_ID} for {PACKAGE_ID}, "
                f"found {receipt.get('frontier_id')!r}"
            )
        if receipt.get("frontier_ids") != FRONTIER_IDS:
            missing.append(
                f"generated proof: expected frontier_ids {FRONTIER_IDS!r} for {PACKAGE_ID}, "
                f"found {receipt.get('frontier_ids')!r}"
            )
        if receipt.get("active_flagship_frontier_id") != ACTIVE_FLAGSHIP_FRONTIER_ID:
            missing.append(
                "generated proof: expected active_flagship_frontier_id "
                f"{ACTIVE_FLAGSHIP_FRONTIER_ID} for {PACKAGE_ID}, found {receipt.get('active_flagship_frontier_id')!r}"
            )
        matching_proof_receipts = [
            item for item in receipt_dicts
            if item.get("proof_receipt") == receipt.get("proof_receipt")
        ]
        if len(matching_proof_receipts) != 1:
            missing.append(
                "generated proof: expected the M117 proof receipt anchor to belong to exactly one package receipt, "
                f"found {len(matching_proof_receipts)}"
            )
        matching_marker_sets = [
            item for item in receipt_dicts
            if item.get("proof_marker_set") == receipt.get("proof_marker_set")
        ]
        if len(matching_marker_sets) != 1:
            missing.append(
                "generated proof: expected the M117 proof marker set to belong to exactly one package receipt, "
                f"found {len(matching_marker_sets)}"
            )

    missing.extend(require_clean_markers("proof doc", proof_text, FORBIDDEN_PROOF_MARKERS))
    missing.extend(require_clean_markers("generated proof", generated_text, FORBIDDEN_PROOF_MARKERS))

    if missing:
        for item in missing:
            print(item, file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
