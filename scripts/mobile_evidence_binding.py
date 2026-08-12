from __future__ import annotations

import hashlib
import os
import subprocess
from pathlib import Path, PurePosixPath
from typing import Iterable


AUTHORITY_ENVIRONMENT = {
    "releaseVersion": "CHUMMER_RELEASE_VERSION",
    "snapshotSha256": "CHUMMER_RELEASE_SNAPSHOT_SHA256",
    "manifestSha256": "CHUMMER_RELEASE_MANIFEST_SHA256",
    "releaseDecisionSha256": "CHUMMER_RELEASE_DECISION_SHA256",
    "releaseScopeDecisionSha256": "CHUMMER_RELEASE_SCOPE_DECISION_SHA256",
}


def sha256_file(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def _git(root: Path, *arguments: str) -> str:
    return subprocess.check_output(
        ["git", "-C", str(root), *arguments],
        text=True,
        stderr=subprocess.DEVNULL,
    ).strip()


def _git_status_paths(root: Path) -> list[str]:
    raw = subprocess.check_output(
        [
            "git",
            "-C",
            str(root),
            "status",
            "--porcelain=v1",
            "-z",
            "--untracked-files=all",
        ],
        stderr=subprocess.DEVNULL,
    )
    records = raw.split(b"\0")
    paths: list[str] = []
    index = 0
    while index < len(records):
        record = records[index]
        index += 1
        if not record:
            continue
        if len(record) < 4 or record[2:3] != b" ":
            raise RuntimeError("unexpected git status porcelain record")
        status = record[:2].decode("ascii")
        paths.append(record[3:].decode("utf-8", errors="surrogateescape"))
        if "R" in status or "C" in status:
            if index >= len(records) or not records[index]:
                raise RuntimeError("incomplete git rename/copy status record")
            paths.append(records[index].decode("utf-8", errors="surrogateescape"))
            index += 1
    return paths


def _is_governed_generated_receipt(path: str) -> bool:
    candidate = PurePosixPath(path)
    return (
        candidate.parent == PurePosixPath(".codex-studio/published")
        and candidate.name.endswith(".generated.json")
    )


def _source_worktree_clean(root: Path) -> bool:
    return all(_is_governed_generated_receipt(path) for path in _git_status_paths(root))


def build_evidence_binding(root: Path, source_files: Iterable[Path]) -> dict[str, object]:
    verification_mode = os.environ.get("CHUMMER_VERIFY_MODE", "slice").strip() or "slice"
    verification_run_id = os.environ.get("CHUMMER_VERIFY_RUN_ID", "").strip()
    source_commit = _git(root, "rev-parse", "HEAD")
    source_tree = _git(root, "rev-parse", "HEAD^{tree}")
    worktree_clean = _source_worktree_clean(root)
    candidate_authority = {
        field: os.environ.get(environment_name, "").strip()
        for field, environment_name in AUTHORITY_ENVIRONMENT.items()
    }
    authority_complete = all(candidate_authority.values())

    if verification_mode == "release":
        missing = [field for field, value in candidate_authority.items() if not value]
        if missing:
            raise RuntimeError(
                "release evidence requires candidate authority fields: " + ", ".join(missing)
            )
        if not verification_run_id:
            raise RuntimeError("release evidence requires CHUMMER_VERIFY_RUN_ID")
        if not worktree_clean:
            raise RuntimeError("release evidence requires a clean source worktree")

    digests: dict[str, str] = {}
    for path in source_files:
        resolved = path.resolve()
        if not resolved.is_relative_to(root):
            raise RuntimeError(f"evidence source is outside the repository: {resolved}")
        if not resolved.is_file():
            raise RuntimeError(f"evidence source is missing: {resolved}")
        digests[str(resolved.relative_to(root))] = sha256_file(resolved)

    return {
        "contract_name": "chummer6-mobile.evidence_binding.v1",
        "binding_status": "candidate_bound" if authority_complete else "source_bound_only",
        "release_evidence_eligible": (
            verification_mode == "release"
            and authority_complete
            and bool(verification_run_id)
            and worktree_clean
        ),
        "verification_mode": verification_mode,
        "verification_run_id": verification_run_id,
        "source": {
            "repository": "ArchonMegalon/chummer6-mobile",
            "commit": source_commit,
            "tree": source_tree,
            "worktree_clean": worktree_clean,
            "cleanliness_policy": "all repository paths except direct .codex-studio/published/*.generated.json outputs",
            "file_sha256": dict(sorted(digests.items())),
        },
        "candidate_authority": candidate_authority,
        "readiness_scope": "mobile_pwa_evidence",
        "does_not_assert": [
            "desktop_artifact_delivery_readiness",
            "whole_product_preview_readiness",
            "stable_readiness",
            "flagship_readiness",
        ],
    }
