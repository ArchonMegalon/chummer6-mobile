# GitHub Codex Review

PR: https://github.com/ArchonMegalon/chummer6-mobile/pull/5

Findings:
- [high] .codex-design/product/README.md [review] design-mirror-still-stale-key-files
Direct file compare against canonical source (`/docker/chummercomplete/chummer-design/products/chummer`) shows these files are not in sync (`cmp` reports `DIFF`).; README and START_HERE are missing newer canonical references/questions (including newer roadmap/telemetry-era entries present in canonical 2026-04-09 files).; GROUP_BLOCKERS content diverges from canonical blocker posture/order (canonical and mirror differ for BLK-008/BLK-010 language and structure).
Expected fix: Resync these product mirror files from the approved canonical bundle so `.codex-design/product/*` matches the intended source snapshot consistently.
