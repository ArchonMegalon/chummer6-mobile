#!/usr/bin/env python3
from __future__ import annotations

import sys
from pathlib import Path

import yaml


REPO_ROOT = Path(__file__).resolve().parents[2]
DESIGN_ROOT = Path("/docker/chummercomplete/chummer-design")
MANIFEST_PATH = DESIGN_ROOT / "products" / "chummer" / "sync" / "sync-manifest.yaml"
TARGET_REPO = "chummer6-mobile"


def load_manifest() -> dict[str, object]:
    data = yaml.safe_load(MANIFEST_PATH.read_text(encoding="utf-8")) or {}
    if not isinstance(data, dict):
        raise ValueError("sync_manifest_not_object")
    return data


def expand_product_sources(manifest: dict[str, object], mirror: dict[str, object]) -> list[str]:
    groups = manifest.get("product_source_groups") or {}
    if not isinstance(groups, dict):
        raise ValueError("sync_manifest_product_source_groups_not_object")

    expanded: list[str] = []
    for group_name in mirror.get("product_groups") or []:
        group_items = groups.get(group_name)
        if not isinstance(group_items, list):
            raise ValueError(f"sync_manifest_product_group_not_list:{group_name}")
        expanded.extend(str(item or "").strip() for item in group_items)

    explicit_sources = mirror.get("product_sources") or mirror.get("sources") or []
    if explicit_sources and not isinstance(explicit_sources, list):
        raise ValueError("sync_manifest_product_sources_not_list")
    expanded.extend(str(item or "").strip() for item in explicit_sources)

    ordered: list[str] = []
    seen: set[str] = set()
    for source in expanded:
        if not source or source in seen:
            continue
        seen.add(source)
        ordered.append(source)
    return ordered


def relative_product_target(source_rel: str, product_target: str) -> Path:
    source_path = Path(source_rel)
    parts = list(source_path.parts)
    if len(parts) >= 2 and parts[0] == "products" and parts[1] == "chummer":
        return Path(product_target) / Path(*parts[2:])
    return Path(product_target) / source_path.name


def check_file(source: Path, destination: Path, problems: list[str]) -> None:
    if not destination.is_file():
        problems.append(f"missing {destination.relative_to(REPO_ROOT)}")
        return
    if source.read_bytes() != destination.read_bytes():
        problems.append(f"stale {destination.relative_to(REPO_ROOT)}")


def main() -> int:
    manifest = load_manifest()
    mirrors = manifest.get("mirrors") or []
    if not isinstance(mirrors, list):
        raise ValueError("sync_manifest_mirrors_not_list")

    mirror = next(
        (item for item in mirrors if isinstance(item, dict) and str(item.get("repo") or "").strip() == TARGET_REPO),
        None,
    )
    if mirror is None:
        raise ValueError(f"sync_manifest_repo_missing:{TARGET_REPO}")

    product_target = str(mirror.get("product_target") or ".codex-design/product").strip()
    problems: list[str] = []

    for source_rel in expand_product_sources(manifest, mirror):
        source = DESIGN_ROOT / source_rel
        if not source.is_file():
            continue
        destination = REPO_ROOT / relative_product_target(source_rel, product_target)
        check_file(source, destination, problems)

    repo_source = str(mirror.get("repo_source") or "").strip()
    if repo_source:
        check_file(
            DESIGN_ROOT / repo_source,
            REPO_ROOT / str(mirror.get("repo_target") or ".codex-design/repo/IMPLEMENTATION_SCOPE.md").strip(),
            problems,
        )

    review_source = str(mirror.get("review_source") or "").strip()
    if review_source:
        check_file(
            DESIGN_ROOT / review_source,
            REPO_ROOT / str(mirror.get("review_target") or ".codex-design/review/REVIEW_CONTEXT.md").strip(),
            problems,
        )

    if problems:
        print("design mirror drift detected for chummer6-mobile:", file=sys.stderr)
        for problem in problems:
            print(f"  {problem}", file=sys.stderr)
        print(
            "repair with: python3 /docker/chummercomplete/chummer-design/scripts/ai/publish_local_mirrors.py",
            file=sys.stderr,
        )
        return 1

    print("design mirror ok")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
