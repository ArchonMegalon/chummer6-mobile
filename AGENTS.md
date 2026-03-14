# chummer6-mobile Agent Guide

Work in this repo as the narrow mobile and play-shell client for Chummer6.

Guardrails:
- Treat `Chummer.Engine.Contracts`, `Chummer.Play.Contracts`, and `Chummer.Ui.Kit` as package inputs, not source-copy candidates.
- Do not add rules math, XML parsing, runtime fingerprint generation, or direct provider calls here.
- Optimize for mobile, tablet, offline replay, and role-aware play shells.
- Keep player and GM play flows separate from full workbench or authoring surfaces.
- Preserve evidence, runtime provenance, and stale-protection semantics in every play-facing workflow.

Verification:
- Keep `scripts/ai/verify.sh` fast and repo-local.
- Add focused checks for contract-copy drift, offline storage seams, role gating, and play route ownership as the repo grows.

<!-- fleet-design-mirror:start -->
## Fleet Design Mirror
- Load `.codex-design/product/README.md`, `.codex-design/repo/IMPLEMENTATION_SCOPE.md`, and `.codex-design/review/REVIEW_CONTEXT.md` when present.
- Treat `.codex-design/` as the approved local mirror of the cross-repo Chummer design front door.
<!-- fleet-design-mirror:end -->
