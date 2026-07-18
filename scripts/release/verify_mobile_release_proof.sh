#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "${script_dir}/../.." && pwd)"

python3 "$repo_root/scripts/verify_mobile_pwa_performance_budget.py" --check-only >/dev/null

CHUMMER_PLAY_REPO_ROOT="${repo_root}" python3 - <<'PY'
import hashlib
import json
import os
from pathlib import Path

repo = Path(os.environ["CHUMMER_PLAY_REPO_ROOT"]).resolve()
path = repo / ".codex-studio" / "published" / "MOBILE_LOCAL_RELEASE_PROOF.generated.json"
verification_mode_path = repo / ".codex-studio" / "published" / "MOBILE_VERIFICATION_MODE.generated.json"
root_release_blockers_path = Path("/docker/chummercomplete/RELEASE_BLOCKERS.generated.json")
self_referential_release_wrapper_ids = {"release_truth:release_ready"}
verification_mode = os.environ.get("CHUMMER_VERIFY_MODE", "slice").strip() or "slice"
verification_run_id = os.environ.get("CHUMMER_VERIFY_RUN_ID", "").strip()

expected_journeys = {
    "install_claim_restore_continue",
    "campaign_session_recover_recap",
    "recover_from_sync_conflict",
    "quality_release_hardening",
    "pwa_runtime_smoke",
    "mobile_pwa_viewport_smoke",
    "mobile_pwa_analytics_smoke",
    "turn_companion_live_session",
    "migration_boundary_evidence",
    "mobile_campaign_continuity",
    "mobile_onboarding_continuity",
    "mobile_artifact_shelf",
    "mobile_live_combat_confidence",
    "mobile_runner_goal_updates",
    "quick_explain_follow_up",
}

expected_source_files = {
    "docs/migration-map.md",
    "docs/PLAY_RELEASE_SIGNOFF.md",
    "docs/next90-m112-mobile-campaign-continuity.proof.md",
    "docs/next90-m117-mobile-artifact-shelf.proof.md",
    "docs/next90-m119-mobile-onboarding-continuity.proof.md",
    "docs/next90-m121-mobile-live-combat-confidence.proof.md",
    "docs/next90-m122-mobile-runner-goal-updates-and-consequence-feed.proof.md",
    "docs/next90-m145-mobile-quick-explain-and-follow-up.proof.md",
    "scripts/ai/with-package-plane.sh",
    "scripts/ai/write_verification_mode_receipt.py",
    "src/Chummer.Play.Web/wwwroot/index.html",
    "src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js",
    "src/Chummer.Play.Web/wwwroot/mobile-install-shell.js",
    "src/Chummer.Play.Web/wwwroot/mobile.css",
    "src/Chummer.Play.Web/wwwroot/service-worker.js",
    "src/Chummer.Play.Web/wwwroot/manifest.webmanifest",
    "src/Chummer.Play.Web/wwwroot/manifest.player.webmanifest",
    "src/Chummer.Play.Web/wwwroot/manifest.gm.webmanifest",
    "src/Chummer.Play.Web/wwwroot/manifest.observer.webmanifest",
    "src/Chummer.Play.Web/PlayWebApplication.cs",
    "src/Chummer.Play.Web/PlaySessionGrant.cs",
    "src/Chummer.Play.Web/PlayRouteHandlers.cs",
    "src/Chummer.Play.Web/PlayTurnCompanionService.cs",
    "src/Chummer.Play.Core/Application/PlayTurnCompanionProjector.cs",
    "src/Chummer.Play.Web/Components/App.razor",
    "src/Chummer.Play.Web/Components/_Imports.razor",
    "src/Chummer.Play.Web/Components/Pages/MobileTurnCompanionPage.razor",
    "src/Chummer.Play.Web/Components/Pages/MobileLiveTurnCompanionPage.razor",
    "src/Chummer.Play.Web/Dockerfile",
    "src/Chummer.Play.RegressionChecks/Program.cs",
    "scripts/ai/verify.sh",
    "scripts/release/verify_mobile_release_proof.sh",
    "scripts/verify_next90_m112_mobile_campaign_continuity.py",
    "scripts/verify_next90_m117_mobile_artifact_shelf.py",
    "scripts/verify_next90_m119_mobile_onboarding_continuity.py",
    "scripts/verify_next90_m121_mobile_live_combat_confidence.py",
    "scripts/verify_next90_m122_mobile_runner_goal_updates.py",
    "scripts/verify_next90_m145_mobile_quick_explain_and_follow_up.py",
    "scripts/verify_mobile_pwa_runtime_smoke.py",
    "scripts/verify_mobile_pwa_viewport_smoke.py",
    "scripts/verify_mobile_pwa_analytics_smoke.py",
    "scripts/verify_mobile_pwa_performance_budget.py",
    "scripts/materialize_mobile_cross_surface_readiness.py",
    "scripts/materialize_mobile_release_boundary.py",
}

expected_package_ids = {
    "next90-m112-mobile-campaign-continuity",
    "next90-m119-mobile-onboarding-continuity",
    "next90-m117-mobile-artifact-shelf",
    "next90-m121-mobile-add-player-table-cards-between-turn-affordances-and-gm-l",
    "next90-m122-mobile-add-mobile-runner-goal-updates-and-player-safe-consequen",
    "next90-m145-mobile-quick-explain-and-follow-up",
}

expected_verification_commands = {
    "mobile_pwa_analytics_smoke": "python3 scripts/verify_mobile_pwa_analytics_smoke.py",
    "pwa_runtime_smoke": "python3 scripts/verify_mobile_pwa_runtime_smoke.py",
    "mobile_pwa_viewport_smoke": "python3 scripts/verify_mobile_pwa_viewport_smoke.py",
}

expected_smoke_receipt_contracts = {
    "mobile_pwa_analytics_smoke": "chummer_play.mobile_pwa_analytics_smoke.v2",
    "pwa_runtime_smoke": "chummer_play.mobile_pwa_runtime_smoke.v2",
    "mobile_pwa_viewport_smoke": "chummer_play.mobile_pwa_viewport_smoke.v2",
}
expected_local_origin = "http://127.0.0.1:<port>"

expected_smoke_receipt_paths = {
    "mobile_pwa_analytics_smoke": ".codex-studio/published/MOBILE_PWA_ANALYTICS_SMOKE.generated.json",
    "pwa_runtime_smoke": ".codex-studio/published/MOBILE_PWA_RUNTIME_SMOKE.generated.json",
    "mobile_pwa_viewport_smoke": ".codex-studio/published/MOBILE_PWA_VIEWPORT_SMOKE.generated.json",
}

expected_release_receipts = {
    ".codex-studio/published/MOBILE_CROSS_SURFACE_READINESS.generated.json",
    ".codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json",
    ".codex-studio/published/MOBILE_RELEASE_BOUNDARY.generated.json",
    ".codex-studio/published/MOBILE_STRICT_PUBLIC_EDGE_FOLLOW_THROUGH.generated.json",
    ".codex-studio/published/MOBILE_PWA_ANALYTICS_SMOKE.generated.json",
    ".codex-studio/published/MOBILE_PWA_PERFORMANCE_BUDGET.generated.json",
    ".codex-studio/published/MOBILE_PWA_RUNTIME_SMOKE.generated.json",
    ".codex-studio/published/MOBILE_PWA_VIEWPORT_SMOKE.generated.json",
    ".codex-studio/published/MOBILE_VERIFICATION_MODE.generated.json",
}
expected_mode_bound_receipts = expected_release_receipts - {
    ".codex-studio/published/MOBILE_VERIFICATION_MODE.generated.json",
}

critical_markers = {
    "quality_release_hardening": [
        "function normalizePlayRouteForMobileShell(href, roleFallback, sessionIdFallback, deviceFallback)",
        "function syncHeroActionMenu()",
        'setLink("shell-play-action-link", "/play", "Play", "/play", "Play");',
        "Release-proof cadence criteria:",
    ],
    "pwa_runtime_smoke": [
        "mobile_pwa_runtime_smoke ok",
        "Hero launch criteria:",
        "blazor_shell: interactive-server",
        "blazor_boot_script: /_framework/blazor.web.js",
        "hero_player_launch:",
        "hero_gm_launch:",
        "hero_menu_player_launch:",
        "hero_menu_gm_launch:",
        "service_worker_cache: chummer-shell-play-shell-v16",
        "normalized_role_fallbacks_cached: player / gm",
        "cached_manifest_start_urls: player / gm",
        "cached_manifest_shortcuts: player / gm",
        "cached_manifest_icon_purpose: any maskable",
        "stale_cache_cleanup: chummer-shell-play-shell-v14 -> removed",
        "legacy_cache_cleanup: chummer-shell-play-shell-v10 -> removed",
        'page.wait_for_selector("[data-turn-root][data-blazor-shell=\'interactive-server\']", timeout=NAVIGATION_TIMEOUT_MS)',
        'page.wait_for_selector("script[src=\'/_framework/blazor.web.js\']", state="attached", timeout=NAVIGATION_TIMEOUT_MS)',
        'page.wait_for_selector("#workspace-shell:not([hidden])", timeout=NAVIGATION_TIMEOUT_MS)',
        "data-blazor-shell=\"interactive-server\"",
        "data-enhance-nav=\"false\"",
        "AddInteractiveServerRenderMode",
        "StaticWebAssetsLoader.UseStaticWebAssets",
        "app.MapStaticAssets();",
        "const NON_CACHEABLE_PATHS = new Set([",
        "\"/mobile/pwa/ledger.json\"",
        "const NON_CACHEABLE_PATH_PREFIXES = [",
        "function isNonCacheableRequest(url)",
        "play_public_route_network_unavailable",
        "def describe_control_state(page: Page, selector: str) -> str",
        "control was not ready for click:",
        "lifecycle_persisted:",
        "path_gm_resume:",
        "role_switch_device_isolated:",
        "reverse_role_switch_device_isolated:",
        "offline_fresh_launch:",
        "offline Player and GM local replay/ack",
        "offline_player_queue_replay: local 1->0 / server 0->1->0 / ammo 8->7",
        "offline_gm_queue_replay: local 1->0 / server 0->1->0 / gm-advance-initiative",
        "device-neutral session handoff receivers",
        "offline_handoff_receiver:",
        "private_api_boundary:",
        "play_api_network_unavailable",
    ],
    "mobile_pwa_viewport_smoke": [
        "mobile_pwa_viewport_smoke ok",
        "Role-specific manifest criteria:",
        "manifest_url:",
        "gm_manifest_url:",
        "manifest_icon_purpose: any maskable",
        "gm_manifest_icon_purpose: any maskable",
        "installability_errors:",
        "gm_installability_errors:",
        "gm_overflow_free:",
        "gm_key_bounds:",
        "gm_compact_layout:",
        "gm_touch_target_min:",
        "narrow_touch_target_min:",
        "query_role_manifest: player / gm",
        "gm_query_manifest_url:",
        "gm_query_installability_errors:",
        "standalone_install_ui: player / gm",
        "standalone_install_button: player Installed / gm Installed",
    ],
    "mobile_pwa_analytics_smoke": [
        "mobile_pwa_analytics_smoke ok",
        "Rybbit analytics criteria:",
        "shell_open_role_analytics: player / gm",
        "shell_open_display_mode: browser / standalone",
        "standalone_shell_open_analytics: player / gm",
        "install_prompt_analytics: available / open / accepted",
        "install_prompt_role_analytics: player / gm",
        "def click_mobile_control(page: Page, selector: str, context: str) -> None",
        "control was not ready:",
        "role_switch_analytics: player->gm / gm->player",
        "gm_link_session_handoff:",
        "gm_link_receiver_device: <minted-device>",
        "gm_link_share_method: link",
        "analytics_default_disabled: true",
        "privacy_blocked: dnt_gpc",
        "secret_leak_free: true",
    ],
    "turn_companion_live_session": [
        "RYBBIT_CHUMMER_PLAY_SITE_ID",
        "function resolveResumeRoute(params, requestedRoleName)",
        '"/manifest.player.webmanifest"',
        '"/manifest.gm.webmanifest"',
        "function sessionHandoffHref(ownerRoute, client)",
        "BuildMobileOwnerRoute",
        "PlayRouteHandlers.BuildMobileOwnerRoute(sessionId, role, deviceId)",
        "/mobile/player?sessionId=session-turn-projection",
        "/mobile/gm?sessionId=session-turn-gm-projection",
        "deviceId=player-shell-main",
        "deviceId=gm-shell-main",
        "turn companion sync surface must expose the player PWA handoff route instead of only the legacy play alias",
        "gm turn companion trust posture must not leak the legacy play alias as its visible PWA handoff",
    ],
}

critical_markers["quality_release_hardening"] = [
    "function normalizePlayRouteForMobileShell(href, roleFallback, sessionIdFallback, deviceFallback)",
    "void sessionIdFallback;",
    "void deviceFallback;",
    "return `/mobile/${mode}`;",
    "Release-proof cadence criteria:",
]
critical_markers["pwa_runtime_smoke"] = [
    "mobile_pwa_runtime_smoke ok",
    "public_install_boundary: /mobile /mobile/player /mobile/gm /mobile/observer",
    "public_authority: none",
    "query_parameters_grant_access: false",
    "live_session_boundary: /mobile/live",
    "live_grant_source: trusted_server_headers",
    "live_owner_route: /mobile/live",
    "private_api_boundary: online 200 private,no-store",
]
critical_markers["mobile_pwa_viewport_smoke"] = [
    "mobile_pwa_viewport_smoke ok",
    "public_install_phone_layouts: player / gm / observer",
    "public_install_desktop_layout: 3 columns",
    "manifest_start_urls: clean player / gm / observer",
    "live_session_viewport: 390x844 /mobile/live",
]
critical_markers["mobile_pwa_analytics_smoke"] = [
    "mobile_pwa_analytics_smoke ok",
    "public_install_analytics: disabled",
    "live_analytics_route: /mobile/live",
    "live_analytics_role_source: trusted_server_headers",
    "analytics_default_disabled: true",
    "privacy_blocked: dnt_gpc",
    "secret_leak_free: true",
]
critical_markers["turn_companion_live_session"] = [
    '@page "/mobile/live"',
    "PlaySessionGrantPolicy.ResolveCurrent",
    "PlayRouteHandlers.BuildMobileOwnerRoute()",
    'return "/mobile/live";',
    "RYBBIT_CHUMMER_PLAY_SITE_ID",
    '"/manifest.player.webmanifest"',
    '"/manifest.gm.webmanifest"',
    '"/manifest.observer.webmanifest"',
]

errors: list[str] = []

def require(condition: bool, message: str) -> None:
    if not condition:
        errors.append(message)

if not path.is_file():
    raise SystemExit(f"missing mobile release proof: {path}")
if verification_mode not in {"scaffold", "slice", "integration", "release"}:
    raise SystemExit(f"unsupported CHUMMER_VERIFY_MODE: {verification_mode}")
if not verification_mode_path.is_file():
    raise SystemExit(f"missing mobile verification mode receipt: {verification_mode_path}")

payload = json.loads(path.read_text(encoding="utf-8"))
verification_mode_receipt = json.loads(verification_mode_path.read_text(encoding="utf-8"))
require(payload.get("contract_name") == "chummer6-mobile.local_release_proof", "unexpected contract_name")
require(payload.get("verification_mode") == verification_mode, "mobile release proof verification mode drifted")
require(
    verification_mode_receipt.get("contractName") == "chummer6-mobile.verification-mode/v1",
    "unexpected verification mode receipt contract",
)
require(verification_mode_receipt.get("mode") == verification_mode, "verification mode receipt mode drifted")
require(verification_mode_receipt.get("status") in {"in_progress", "pass"}, "verification mode receipt is not active or passing")
if verification_mode == "release":
    require(bool(verification_run_id), "release verification run id is missing")
    require(payload.get("verification_run_id") == verification_run_id, "mobile release proof verification run drifted")
    require(
        verification_mode_receipt.get("verificationRunId") == verification_run_id,
        "verification mode receipt verification run drifted",
    )
    require(verification_mode_receipt.get("status") == "pass", "release verification mode receipt is not passing")
    require(verification_mode_receipt.get("stubPackagesAllowed") is False, "release verification used stub packages")
    require(verification_mode_receipt.get("skipCount") == 0, "release verification contains skipped proof")
    require(verification_mode_receipt.get("skips") == [], "release verification skip list is not empty")
    require(verification_mode_receipt.get("releaseEvidenceEligible") is True, "release verification is not evidence eligible")
    for relative_receipt in sorted(expected_mode_bound_receipts):
        receipt_path = repo / relative_receipt
        require(receipt_path.is_file(), f"release verification receipt missing: {relative_receipt}")
        if not receipt_path.is_file():
            continue
        receipt_payload = json.loads(receipt_path.read_text(encoding="utf-8"))
        require(
            receipt_payload.get("verification_mode") == "release",
            f"release verification receipt mode drifted: {relative_receipt}",
        )
        require(
            receipt_payload.get("verification_run_id") == verification_run_id,
            f"release verification receipt run drifted: {relative_receipt}",
        )
require(payload.get("proof_kind") == "source_backed_local_regression_contract", "unexpected proof_kind")
require(str(payload.get("status")).lower() == "passed", f"mobile release proof status is not passed: {payload.get('status')}")
generated_at = payload.get("generated_at")
generated_at_utc = payload.get("generated_at_utc")
require(isinstance(generated_at, str) and generated_at.strip(), "mobile release proof missing generated_at")
require(isinstance(generated_at_utc, str) and generated_at_utc.strip(), "mobile release proof missing generated_at_utc")
require(generated_at == generated_at_utc, "mobile release proof generated_at and generated_at_utc drifted")

performance_receipt_path = repo / ".codex-studio" / "published" / "MOBILE_PWA_PERFORMANCE_BUDGET.generated.json"
require(performance_receipt_path.is_file(), "mobile PWA performance budget receipt is missing")
if performance_receipt_path.is_file():
    performance_payload = json.loads(performance_receipt_path.read_text(encoding="utf-8"))
    require(
        performance_payload.get("contract_name") == "chummer_play.mobile_pwa_performance_budget.v1",
        "mobile PWA performance budget receipt contract drifted",
    )
    require(performance_payload.get("status") == "pass", "mobile PWA performance budget is not pass")
    require(
        isinstance(performance_payload.get("failures"), list) and not performance_payload.get("failures"),
        "mobile PWA performance budget receipt contains failures",
    )
    require(
        performance_payload.get("framework_asset_exceptions") == ["/_framework/blazor.web.js"],
        "mobile PWA performance budget framework exception drifted",
    )

source_files = payload.get("source_files")
require(isinstance(source_files, list) and all(isinstance(item, str) for item in source_files), "source_files must be a string list")
source_file_set = set(source_files or [])
for expected in sorted(expected_source_files):
    require(expected in source_file_set, f"source_files missing {expected}")

source_file_digests = payload.get("source_file_digests")
require(isinstance(source_file_digests, list), "source_file_digests must be a list")
digest_by_path: dict[str, str] = {}
for entry in source_file_digests or []:
    if not isinstance(entry, dict):
        errors.append("source_file_digests contains a non-object entry")
        continue
    digest_path = entry.get("path")
    digest_value = entry.get("sha256")
    if not isinstance(digest_path, str) or not digest_path.strip():
        errors.append("source_file_digests contains an entry without path")
        continue
    if not isinstance(digest_value, str) or len(digest_value) != 64:
        errors.append(f"source_file_digests entry for {digest_path} has invalid sha256")
        continue
    digest_by_path[digest_path] = digest_value
require(set(digest_by_path) == source_file_set, f"source_file_digests paths drifted: {sorted(set(digest_by_path) ^ source_file_set)}")
require(payload.get("source_file_count") == len(source_files), f"source_file_count drifted: {payload.get('source_file_count')!r}")
require(payload.get("source_file_digest_count") == len(source_file_digests), f"source_file_digest_count drifted: {payload.get('source_file_digest_count')!r}")

source_texts: list[str] = []
for source_file in source_files or []:
    source_path = (repo / source_file).resolve()
    require(source_path.is_relative_to(repo), f"source file escapes repo: {source_file}")
    if not source_path.is_file():
        errors.append(f"source file missing on disk: {source_file}")
        continue
    actual_digest = hashlib.sha256(source_path.read_bytes()).hexdigest()
    require(digest_by_path.get(source_file) == actual_digest, f"source file digest drifted for {source_file}")
    source_texts.append(source_path.read_text(encoding="utf-8", errors="replace"))

journeys = payload.get("journeys_passed")
require(isinstance(journeys, list) and all(isinstance(item, str) for item in journeys), "journeys_passed must be a string list")
journey_set = set(journeys or [])
require(journey_set == expected_journeys, f"journeys_passed drifted: {sorted(journey_set ^ expected_journeys)}")

required_markers = payload.get("required_markers")
require(isinstance(required_markers, dict), "required_markers must be an object")
marker_keys = set(required_markers or {})
require(marker_keys == expected_journeys, f"required_markers keys drifted: {sorted(marker_keys ^ expected_journeys)}")

combined_text = "\n".join(source_texts)
marker_misses: list[str] = []
for journey in sorted(expected_journeys):
    markers = required_markers.get(journey, []) if isinstance(required_markers, dict) else []
    require(isinstance(markers, list) and all(isinstance(item, str) for item in markers), f"{journey} markers must be a string list")
    for marker in markers:
        if marker not in combined_text:
            marker_misses.append(f"{journey}: {marker}")

for journey, markers in critical_markers.items():
    journey_markers = required_markers.get(journey, []) if isinstance(required_markers, dict) else []
    for marker in markers:
        require(marker in journey_markers, f"{journey} missing critical marker claim: {marker}")

package_receipts = payload.get("package_receipts")
require(isinstance(package_receipts, list), "package_receipts must be a list")
package_ids = [item.get("package_id") for item in package_receipts or [] if isinstance(item, dict)]
require(set(package_ids) == expected_package_ids, f"package_receipts drifted: {sorted(set(package_ids) ^ expected_package_ids)}")
require(len(package_ids) == len(set(package_ids)), "package_receipts contains duplicate package_id values")

proof_marker_sets: list[str] = []
for receipt in package_receipts or []:
    if not isinstance(receipt, dict):
        errors.append("package_receipts contains a non-object entry")
        continue
    package_id = receipt.get("package_id")
    proof_marker_set = receipt.get("proof_marker_set")
    proof_receipt = receipt.get("proof_receipt")
    status = str(receipt.get("status") or "").lower()
    require(status in {"implemented", "closed"}, f"{package_id} has unexpected status {status!r}")
    require(proof_marker_set in expected_journeys, f"{package_id} references unknown proof_marker_set {proof_marker_set!r}")
    proof_marker_sets.append(str(proof_marker_set))
    require(isinstance(proof_receipt, str) and (repo / proof_receipt).is_file(), f"{package_id} proof_receipt missing on disk: {proof_receipt}")

require(len(proof_marker_sets) == len(set(proof_marker_sets)), "package_receipts reuse proof_marker_set values")

verification_commands = payload.get("verification_commands")
require(isinstance(verification_commands, list), "verification_commands must be a list")
verification_by_id = {
    item.get("id"): item
    for item in verification_commands or []
    if isinstance(item, dict)
}
require(
    set(verification_by_id) == set(expected_verification_commands),
    f"verification_commands drifted: {sorted(set(verification_by_id) ^ set(expected_verification_commands))}",
)
for command_id, expected_command in expected_verification_commands.items():
    receipt = verification_by_id.get(command_id)
    if not isinstance(receipt, dict):
        continue
    require(receipt.get("command") == expected_command, f"{command_id} command drifted: {receipt.get('command')!r}")
    require(receipt.get("required_before_materialize") is True, f"{command_id} must be required before materialization")
    proves = receipt.get("proves")
    require(
        isinstance(proves, list) and all(isinstance(item, str) and item.strip() for item in proves) and len(proves) >= 3,
        f"{command_id} must name concrete proof coverage",
    )

smoke_receipts = payload.get("smoke_receipts")
require(isinstance(smoke_receipts, list), "smoke_receipts must be a list")
smoke_receipt_by_id = {
    item.get("id"): item
    for item in smoke_receipts or []
    if isinstance(item, dict)
}
require(
    set(smoke_receipt_by_id) == set(expected_verification_commands),
    f"smoke_receipts drifted: {sorted(set(smoke_receipt_by_id) ^ set(expected_verification_commands))}",
)
for command_id, expected_contract in expected_smoke_receipt_contracts.items():
    receipt_summary = smoke_receipt_by_id.get(command_id)
    command_summary = verification_by_id.get(command_id)
    if not isinstance(receipt_summary, dict) or not isinstance(command_summary, dict):
        continue
    require(receipt_summary.get("command") == expected_verification_commands[command_id], f"{command_id} smoke receipt command drifted: {receipt_summary.get('command')!r}")
    require(receipt_summary.get("required_before_materialize") is True, f"{command_id} smoke receipt must be required before materialization")
    require(receipt_summary.get("receipt_path") == command_summary.get("receipt_path"), f"{command_id} smoke receipt path no longer matches verification command")
    require(receipt_summary.get("receipt_path") == expected_smoke_receipt_paths[command_id], f"{command_id} smoke receipt path drifted: {receipt_summary.get('receipt_path')!r}")
    require(receipt_summary.get("contract_name") == expected_contract, f"{command_id} smoke receipt contract drifted: {receipt_summary.get('contract_name')!r}")
    require(receipt_summary.get("status") == "pass", f"{command_id} smoke receipt summary is not pass")
    require(receipt_summary.get("verification_mode") == verification_mode, f"{command_id} smoke receipt verification mode drifted")
    require(isinstance(receipt_summary.get("generated_at_utc"), str) and receipt_summary.get("generated_at_utc").strip(), f"{command_id} smoke receipt summary missing generated_at_utc")

runtime_command = verification_by_id.get("pwa_runtime_smoke")
runtime_receipt_path = runtime_command.get("receipt_path") if isinstance(runtime_command, dict) else None
require(runtime_receipt_path == ".codex-studio/published/MOBILE_PWA_RUNTIME_SMOKE.generated.json", "pwa_runtime_smoke receipt_path drifted")
runtime_receipt = repo / str(runtime_receipt_path or "")
if runtime_receipt_path and runtime_receipt.is_file():
    runtime_payload = json.loads(runtime_receipt.read_text(encoding="utf-8"))
    require(runtime_payload.get("contract_name") == "chummer_play.mobile_pwa_runtime_smoke.v2", "runtime smoke receipt contract drifted")
    require(runtime_payload.get("status") == "pass", "runtime smoke receipt is not pass")
    public_install = runtime_payload.get("public_install_boundary")
    require(isinstance(public_install, dict), "runtime smoke receipt public install boundary missing")
    require(public_install.get("routes") == ["/mobile", "/mobile/player", "/mobile/gm", "/mobile/observer"], "runtime smoke receipt public install routes drifted")
    require(public_install.get("authority") == "none", "runtime smoke receipt public authority drifted")
    require(public_install.get("live_state_loaded") is False and public_install.get("live_runtime_loaded") is False, "runtime smoke receipt public shell loaded live state")
    require(public_install.get("query_parameters_grant_access") is False, "runtime smoke receipt lets query parameters grant public access")
    live_session = runtime_payload.get("live_session_boundary")
    require(isinstance(live_session, dict), "runtime smoke receipt live session boundary missing")
    require(live_session.get("route") == "/mobile/live", "runtime smoke receipt live route drifted")
    require(live_session.get("grant_source") == "trusted_server_headers", "runtime smoke receipt live grant source drifted")
    require(live_session.get("query_parameters_grant_access") is False, "runtime smoke receipt lets query parameters grant live access")
    require(live_session.get("owner_route") == "/mobile/live", "runtime smoke receipt owner route drifted")
    require(live_session.get("role_change_exit") == "/mobile/gm", "runtime smoke receipt role change must exit live authority")
    private_api_boundary = runtime_payload.get("private_api_boundary")
    require(isinstance(private_api_boundary, dict), "runtime smoke receipt private API boundary missing")
    require(private_api_boundary.get("online_status") == 200, "runtime smoke receipt online private API status drifted")
    require("private" in str(private_api_boundary.get("online_cache_control") or "").lower() and "no-store" in str(private_api_boundary.get("online_cache_control") or "").lower(), "runtime smoke receipt private API cache boundary drifted")
else:
    require(False, f"runtime smoke receipt missing on disk: {runtime_receipt_path}")

viewport_command = verification_by_id.get("mobile_pwa_viewport_smoke")
viewport_receipt_path = viewport_command.get("receipt_path") if isinstance(viewport_command, dict) else None
require(viewport_receipt_path == ".codex-studio/published/MOBILE_PWA_VIEWPORT_SMOKE.generated.json", "mobile_pwa_viewport_smoke receipt_path drifted")
viewport_receipt = repo / str(viewport_receipt_path or "")
if viewport_receipt_path and viewport_receipt.is_file():
    viewport_payload = json.loads(viewport_receipt.read_text(encoding="utf-8"))
    require(viewport_payload.get("contract_name") == "chummer_play.mobile_pwa_viewport_smoke.v2", "viewport smoke receipt contract drifted")
    require(viewport_payload.get("status") == "pass", "viewport smoke receipt is not pass")
    manifests = viewport_payload.get("manifests")
    require(isinstance(manifests, dict), "viewport smoke receipt manifests missing")
    for mode, route in {"player": "/mobile/player", "gm": "/mobile/gm", "observer": "/mobile/observer"}.items():
        manifest = manifests.get(mode) if isinstance(manifests, dict) else None
        require(isinstance(manifest, dict) and manifest.get("url") == f"{expected_local_origin}/manifest.{mode}.webmanifest", f"viewport smoke receipt {mode} manifest url hygiene drifted")
        require(isinstance(manifest, dict) and manifest.get("id") == route and manifest.get("start_url") == route, f"viewport smoke receipt {mode} manifest authority-free route drifted")
        require(isinstance(manifest, dict) and manifest.get("scope") == "/mobile/", f"viewport smoke receipt {mode} manifest scope drifted")
        require(isinstance(manifest, dict) and manifest.get("installability_error_count") == 0, f"viewport smoke receipt {mode} installability errors are not zero")
        require(isinstance(manifest, dict) and manifest.get("has_maskable_192") is True and manifest.get("has_maskable_512") is True, f"viewport smoke receipt {mode} maskable icons missing")
    public_layout = viewport_payload.get("public_install_boundary")
    require(isinstance(public_layout, dict) and public_layout.get("authority") == "none", "viewport smoke receipt public install authority drifted")
    require(isinstance(public_layout, dict) and public_layout.get("query_parameters_grant_access") is False, "viewport smoke receipt public query authority drifted")
    phone_layouts = public_layout.get("phone_layouts") if isinstance(public_layout, dict) else None
    require(isinstance(phone_layouts, dict) and set(phone_layouts) == {"player", "gm", "observer"}, "viewport smoke receipt phone layouts drifted")
    require(isinstance(phone_layouts, dict) and all(isinstance(row, dict) and row.get("overflowFree") is True and float(row.get("installTouchTarget") or 0) >= 43 for row in phone_layouts.values()), "viewport smoke receipt public phone layout proof failed")
    desktop_layout = public_layout.get("desktop_layout") if isinstance(public_layout, dict) else None
    require(isinstance(desktop_layout, dict) and desktop_layout.get("overflowFree") is True and int(desktop_layout.get("gridColumns") or 0) >= 3, "viewport smoke receipt public desktop layout proof failed")
    live_layout = viewport_payload.get("live_session_boundary")
    require(isinstance(live_layout, dict) and live_layout.get("route") == "/mobile/live" and live_layout.get("grant_source") == "trusted_server_headers", "viewport smoke receipt live boundary drifted")
    live_phone = live_layout.get("phone_layout") if isinstance(live_layout, dict) else None
    require(isinstance(live_phone, dict) and live_phone.get("overflowFree") is True and live_phone.get("glanceCount") == 6 and live_phone.get("actionColumns") == 1 and float(live_phone.get("minTouchTarget") or 0) >= 43, "viewport smoke receipt live phone layout proof failed")
else:
    require(False, f"viewport smoke receipt missing on disk: {viewport_receipt_path}")

analytics_command = verification_by_id.get("mobile_pwa_analytics_smoke")
analytics_receipt_path = analytics_command.get("receipt_path") if isinstance(analytics_command, dict) else None
require(analytics_receipt_path == ".codex-studio/published/MOBILE_PWA_ANALYTICS_SMOKE.generated.json", "mobile_pwa_analytics_smoke receipt_path drifted")
analytics_receipt = repo / str(analytics_receipt_path or "")
if analytics_receipt_path and analytics_receipt.is_file():
    analytics_payload = json.loads(analytics_receipt.read_text(encoding="utf-8"))
    require(analytics_payload.get("contract_name") == "chummer_play.mobile_pwa_analytics_smoke.v2", "analytics smoke receipt contract drifted")
    require(analytics_payload.get("status") == "pass", "analytics smoke receipt is not pass")
    provider = analytics_payload.get("provider_script")
    require(isinstance(provider, dict) and provider.get("site_id") == "site-mobile-analytics-smoke", "analytics smoke receipt provider site drifted")
    require(isinstance(provider, dict) and provider.get("script_path") == "/mobile-rybbit-smoke.js", "analytics smoke receipt provider script drifted")
    public_analytics = analytics_payload.get("public_install_boundary")
    require(isinstance(public_analytics, dict) and public_analytics.get("analytics_enabled") is False, "analytics smoke receipt public analytics must be disabled")
    require(isinstance(public_analytics, dict) and public_analytics.get("provider_requests") == 0 and public_analytics.get("event_count") == 0, "analytics smoke receipt public counters drifted")
    require(isinstance(public_analytics, dict) and public_analytics.get("query_secret_leak_free") is True, "analytics smoke receipt public secret hygiene drifted")
    live_analytics = analytics_payload.get("live_session_boundary")
    require(isinstance(live_analytics, dict) and live_analytics.get("route") == "/mobile/live", "analytics smoke receipt live route drifted")
    require(isinstance(live_analytics, dict) and live_analytics.get("grant_source") == "trusted_server_headers", "analytics smoke receipt live grant source drifted")
    require(isinstance(live_analytics, dict) and live_analytics.get("secret_leak_free") is True, "analytics smoke receipt live secret hygiene drifted")
    events = set(live_analytics.get("events") or []) if isinstance(live_analytics, dict) else set()
    require({"mobile_shell_open", "mobile_privacy_probe"}.issubset(events), "analytics smoke receipt live event coverage drifted")
    privacy = analytics_payload.get("privacy")
    require(isinstance(privacy, dict) and privacy.get("dnt_gpc_blocked") is True, "analytics smoke receipt DNT/GPC proof drifted")
    require(isinstance(privacy, dict) and privacy.get("privacy_provider_requests") == 0 and privacy.get("privacy_event_count") == 0, "analytics smoke receipt privacy-blocked counters drifted")
    require(isinstance(privacy, dict) and privacy.get("default_disabled") is True and privacy.get("default_provider_requests") == 0 and privacy.get("default_event_count") == 0, "analytics smoke receipt default-disabled proof drifted")
    require(isinstance(privacy, dict) and privacy.get("secret_leak_free") is True, "analytics smoke receipt secret leak proof drifted")
else:
    require(False, f"analytics smoke receipt missing on disk: {analytics_receipt_path}")

role_pwa_contract = payload.get("role_pwa_contract")
require(isinstance(role_pwa_contract, dict), "role_pwa_contract must be an object")
if isinstance(role_pwa_contract, dict) and role_pwa_contract.get("contract_name") == "chummer6-mobile.role_pwa_contract.v2":
    require(role_pwa_contract.get("status") == "pass", "role_pwa_contract is not pass")
    role_checks = role_pwa_contract.get("checks")
    expected_role_checks = {
        "public_install_routes_are_distinct_and_authority_free",
        "query_parameters_cannot_grant_live_access",
        "live_route_requires_trusted_server_grant",
        "role_change_exits_live_authority",
        "private_api_is_no_store",
        "manifests_are_clean_install_labels",
        "manifest_shortcuts_are_authority_free",
        "viewport_manifests_are_installable_and_clean",
        "public_phone_and_desktop_layouts_proven",
        "live_mobile_viewport_is_grant_backed",
        "public_install_analytics_are_disabled",
        "live_analytics_are_sanitized_and_grant_backed",
        "analytics_privacy_controls_hold",
    }
    require(isinstance(role_checks, dict) and set(role_checks) == expected_role_checks, "role_pwa_contract v2 checks drifted")
    if isinstance(role_checks, dict):
        for check_id in sorted(expected_role_checks):
            require(role_checks.get(check_id) is True, f"role_pwa_contract check is not true: {check_id}")
    public_boundary = role_pwa_contract.get("public_install_boundary")
    require(isinstance(public_boundary, dict) and public_boundary.get("routes") == ["/mobile", "/mobile/player", "/mobile/gm", "/mobile/observer"], "role_pwa_contract public routes drifted")
    require(isinstance(public_boundary, dict) and public_boundary.get("authority") == "none", "role_pwa_contract public authority drifted")
    public_roles = public_boundary.get("roles") if isinstance(public_boundary, dict) else None
    require(isinstance(public_roles, list) and {row.get("mode") for row in public_roles if isinstance(row, dict)} == {"player", "gm", "observer"}, "role_pwa_contract public roles drifted")
    live_boundary = role_pwa_contract.get("live_session_boundary")
    require(isinstance(live_boundary, dict) and live_boundary.get("route") == "/mobile/live" and live_boundary.get("grant_source") == "trusted_server_headers", "role_pwa_contract live boundary drifted")
    source_receipts = role_pwa_contract.get("source_receipts")
    require(isinstance(source_receipts, dict), "role_pwa_contract source_receipts must be an object")
    if isinstance(source_receipts, dict):
        require(source_receipts.get("runtime") == ".codex-studio/published/MOBILE_PWA_RUNTIME_SMOKE.generated.json", "role_pwa_contract runtime source receipt drifted")
        require(source_receipts.get("viewport") == ".codex-studio/published/MOBILE_PWA_VIEWPORT_SMOKE.generated.json", "role_pwa_contract viewport source receipt drifted")
        require(source_receipts.get("analytics") == ".codex-studio/published/MOBILE_PWA_ANALYTICS_SMOKE.generated.json", "role_pwa_contract analytics source receipt drifted")

if isinstance(role_pwa_contract, dict) and role_pwa_contract.get("contract_name") != "chummer6-mobile.role_pwa_contract.v2":
    require(role_pwa_contract.get("contract_name") == "chummer6-mobile.role_pwa_contract.v1", "role_pwa_contract contract_name drifted")
    require(role_pwa_contract.get("status") == "pass", "role_pwa_contract is not pass")
    role_checks = role_pwa_contract.get("checks")
    require(isinstance(role_checks, dict), "role_pwa_contract checks must be an object")
    expected_role_checks = {
        "generic_manifest_exposes_role_shortcuts",
        "player_manifest_direct_launch",
        "gm_manifest_direct_launch",
        "role_manifests_share_mobile_scope",
        "role_manifests_are_standalone",
        "hero_dropdown_play_opens_player_and_gm",
        "direct_hero_play_opens_player_and_gm",
        "interactive_blazor_shell_proven",
        "service_worker_offline_shell_proven",
        "offline_player_queue_replay_proven",
        "offline_gm_queue_replay_proven",
        "device_neutral_handoff_receivers_proven",
        "private_play_api_fails_closed_offline",
        "rybbit_default_disabled",
        "rybbit_dnt_gpc_blocked",
        "rybbit_secret_leak_free",
        "rybbit_skips_and_masks_mobile_routes",
        "role_analytics_cover_browser_and_standalone",
        "role_switch_analytics_cover_both_directions",
        "viewport_manifest_urls_use_placeholder_origin",
        "handoff_routes_preserve_role_and_mint_receiver_device",
        "handoff_routes_use_placeholder_origin",
    }
    role_check_keys = set(role_checks or {}) if isinstance(role_checks, dict) else set()
    require(role_check_keys == expected_role_checks, f"role_pwa_contract checks drifted: {sorted(role_check_keys ^ expected_role_checks)}")
    if isinstance(role_checks, dict):
        for check_id in sorted(expected_role_checks):
            require(role_checks.get(check_id) is True, f"role_pwa_contract check is not true: {check_id}")

    roles = role_pwa_contract.get("roles")
    require(isinstance(roles, list) and len(roles) == 2, "role_pwa_contract roles must contain Player and GM")
    roles_by_name = {
        item.get("role"): item
        for item in roles or []
        if isinstance(item, dict)
    } if isinstance(roles, list) else {}
    require(set(roles_by_name) == {"Player", "GameMaster"}, f"role_pwa_contract roles drifted: {sorted(set(roles_by_name) ^ {'Player', 'GameMaster'})}")
    expected_role_rows = {
        "Player": {
            "mode": "player",
            "route": "/mobile/player",
            "manifest_path": "src/Chummer.Play.Web/wwwroot/manifest.player.webmanifest",
            "manifest_id": "/mobile/player",
            "manifest_start_url": "/mobile/player?role=Player",
            "launch_mode": "player",
            "handoff_fragment": "/mobile/player?sessionId=<session>&role=Player",
        },
        "GameMaster": {
            "mode": "gm",
            "route": "/mobile/gm",
            "manifest_path": "src/Chummer.Play.Web/wwwroot/manifest.gm.webmanifest",
            "manifest_id": "/mobile/gm",
            "manifest_start_url": "/mobile/gm?role=GameMaster",
            "launch_mode": "gm",
            "handoff_fragment": "/mobile/gm?sessionId=<session>&role=GameMaster",
        },
    }
    for role_name, expected in expected_role_rows.items():
        row = roles_by_name.get(role_name)
        if not isinstance(row, dict):
            continue
        for key in ["mode", "route", "manifest_path", "manifest_id", "manifest_start_url"]:
            require(row.get(key) == expected[key], f"{role_name} role_pwa_contract {key} drifted: {row.get(key)!r}")
        require(row.get("manifest_scope") == "/mobile/", f"{role_name} role_pwa_contract manifest_scope drifted")
        require(row.get("manifest_display") == "standalone", f"{role_name} role_pwa_contract manifest_display drifted")
        require(row.get("installability_error_count") == 0, f"{role_name} role_pwa_contract installability errors are not zero")
        require(row.get("standalone_install_button") == "Installed", f"{role_name} role_pwa_contract installed UI drifted")
        require("any maskable" in set(row.get("manifest_icon_purposes") or []), f"{role_name} role_pwa_contract maskable icon proof missing")
        shortcut_set = set(row.get("manifest_shortcut_urls") or [])
        require({"/mobile/player?role=Player", "/mobile/gm?role=GameMaster"}.issubset(shortcut_set), f"{role_name} role_pwa_contract shortcuts drifted")
        hero_launch = row.get("hero_launch")
        hero_dropdown_launch = row.get("hero_dropdown_launch")
        require(isinstance(hero_launch, dict) and hero_launch.get("mode") == expected["launch_mode"], f"{role_name} role_pwa_contract hero launch drifted")
        require(isinstance(hero_dropdown_launch, dict) and hero_dropdown_launch.get("mode") == expected["launch_mode"], f"{role_name} role_pwa_contract hero dropdown launch drifted")
        handoff_routes = row.get("handoff_routes")
        require(isinstance(handoff_routes, dict), f"{role_name} role_pwa_contract handoff routes missing")
        if isinstance(handoff_routes, dict):
            for method in ["clipboard", "native", "link"]:
                method_row = handoff_routes.get(method)
                require(isinstance(method_row, dict) and str(method_row.get("route") or "").startswith(expected_local_origin), f"{role_name} role_pwa_contract {method} handoff origin hygiene drifted")
                require(isinstance(method_row, dict) and expected["handoff_fragment"] in str(method_row.get("route") or ""), f"{role_name} role_pwa_contract {method} handoff route drifted")
                require(isinstance(method_row, dict) and method_row.get("receiver_device") == "<minted-device>", f"{role_name} role_pwa_contract {method} receiver device proof drifted")

    offline_online = role_pwa_contract.get("offline_online")
    require(isinstance(offline_online, dict), "role_pwa_contract offline_online must be an object")
    if isinstance(offline_online, dict):
        require(offline_online.get("service_worker_controlled") is True, "role_pwa_contract service worker control drifted")
        require(offline_online.get("service_worker_cache") == "chummer-shell-play-shell-v16", "role_pwa_contract service worker cache drifted")
        require(offline_online.get("player_queue_replay") == "local 1->0 / server 0->1->0 / ammo 8->7", "role_pwa_contract player replay drifted")
        require(offline_online.get("gm_queue_replay") == "local 1->0 / server 0->1->0 / gm-advance-initiative", "role_pwa_contract GM replay drifted")
        boundary = offline_online.get("private_api_boundary")
        require(isinstance(boundary, dict) and boundary.get("online_status") == 200 and boundary.get("offline_status") == 503 and boundary.get("offline_error") == "play_api_network_unavailable", "role_pwa_contract private API boundary drifted")

    session_handoff = role_pwa_contract.get("session_handoff")
    require(isinstance(session_handoff, dict), "role_pwa_contract session_handoff must be an object")
    if isinstance(session_handoff, dict):
        require(session_handoff.get("share_methods") == ["clipboard", "native", "link"], "role_pwa_contract share methods drifted")
        require(session_handoff.get("sender_device_id_stripped") is True, "role_pwa_contract sender device stripping drifted")
        require(session_handoff.get("receiver_device_id_minted") is True, "role_pwa_contract receiver device minting drifted")
        receivers = session_handoff.get("device_neutral_receivers")
        require(isinstance(receivers, dict) and receivers.get("device_neutral") is True, "role_pwa_contract device-neutral receivers drifted")

    rybbit = role_pwa_contract.get("rybbit")
    require(isinstance(rybbit, dict), "role_pwa_contract rybbit must be an object")
    if isinstance(rybbit, dict):
        require(rybbit.get("site_id") == "site-mobile-analytics-smoke", "role_pwa_contract Rybbit site id drifted")
        require(rybbit.get("script_path") == "/mobile-rybbit-smoke.js", "role_pwa_contract Rybbit script path drifted")
        require(rybbit.get("skip_patterns") == "[\"/mobile\",\"/mobile/**\"]", "role_pwa_contract Rybbit skip patterns drifted")
        require(rybbit.get("mask_patterns") == "[\"/mobile\",\"/mobile/**\",\"/api/play/**\"]", "role_pwa_contract Rybbit mask patterns drifted")
        require(rybbit.get("default_disabled") is True, "role_pwa_contract Rybbit default disabled drifted")
        require(rybbit.get("dnt_gpc_blocked") is True, "role_pwa_contract Rybbit DNT/GPC proof drifted")
        require(rybbit.get("secret_leak_free") is True, "role_pwa_contract Rybbit secret leak proof drifted")
        events = set(rybbit.get("events") or [])
        require({"mobile_shell_open", "mobile_install_prompt_choice", "mobile_role_switch", "mobile_session_handoff_share"}.issubset(events), "role_pwa_contract Rybbit event coverage drifted")

    source_receipts = role_pwa_contract.get("source_receipts")
    require(isinstance(source_receipts, dict), "role_pwa_contract source_receipts must be an object")
    if isinstance(source_receipts, dict):
        require(source_receipts.get("runtime") == ".codex-studio/published/MOBILE_PWA_RUNTIME_SMOKE.generated.json", "role_pwa_contract runtime source receipt drifted")
        require(source_receipts.get("viewport") == ".codex-studio/published/MOBILE_PWA_VIEWPORT_SMOKE.generated.json", "role_pwa_contract viewport source receipt drifted")
        require(source_receipts.get("analytics") == ".codex-studio/published/MOBILE_PWA_ANALYTICS_SMOKE.generated.json", "role_pwa_contract analytics source receipt drifted")

cross_surface_readiness = payload.get("cross_surface_readiness")
require(isinstance(cross_surface_readiness, dict), "cross_surface_readiness must be an object")
if isinstance(cross_surface_readiness, dict):
    require(cross_surface_readiness.get("contract_name") == "chummer6-mobile.cross_surface_readiness.v1", "cross_surface_readiness contract_name drifted")
    require(cross_surface_readiness.get("status") == "pass", "cross_surface_readiness is not pass")
    require(
        cross_surface_readiness.get("fleet_flagship_readiness_path") == "/docker/fleet/.codex-studio/published/FLAGSHIP_PRODUCT_READINESS.generated.json",
        "cross_surface_readiness fleet path drifted",
    )
    require(isinstance(cross_surface_readiness.get("fleet_generated_at"), str) and cross_surface_readiness.get("fleet_generated_at").strip(), "cross_surface_readiness missing fleet_generated_at")
    require(isinstance(cross_surface_readiness.get("fleet_status"), str) and cross_surface_readiness.get("fleet_status").strip(), "cross_surface_readiness missing fleet_status")

    checks = cross_surface_readiness.get("checks")
    require(isinstance(checks, dict), "cross_surface_readiness checks must be an object")
    expected_cross_surface_checks = {
        "fleet_receipt_present",
        "fleet_mobile_play_shell_ready",
        "fleet_mobile_local_release_passed",
        "fleet_mobile_scope_not_listed_as_blocker",
    }
    check_keys = set(checks or {}) if isinstance(checks, dict) else set()
    require(check_keys == expected_cross_surface_checks, f"cross_surface_readiness checks drifted: {sorted(check_keys ^ expected_cross_surface_checks)}")
    if isinstance(checks, dict):
        for check_id in sorted(expected_cross_surface_checks):
            require(checks.get(check_id) is True, f"cross_surface_readiness check is not true: {check_id}")

    coverage = cross_surface_readiness.get("coverage")
    require(isinstance(coverage, dict), "cross_surface_readiness coverage must be an object")
    if isinstance(coverage, dict):
        require(coverage.get("mobile_play_shell") == "ready", "cross_surface_readiness mobile_play_shell coverage drifted")
        require(str(coverage.get("desktop_client") or "").strip() in {"ready", "warning", "missing"}, "cross_surface_readiness desktop_client coverage drifted")

    mobile_detail = cross_surface_readiness.get("mobile_play_shell")
    require(isinstance(mobile_detail, dict), "cross_surface_readiness mobile_play_shell detail must be an object")
    if isinstance(mobile_detail, dict):
        require(mobile_detail.get("status") == "ready", "cross_surface_readiness mobile_play_shell status drifted")
        require(isinstance(mobile_detail.get("summary"), str) and mobile_detail.get("summary").strip(), "cross_surface_readiness mobile_play_shell summary missing")
        require(mobile_detail.get("mobile_local_release_status") == "passed", "cross_surface_readiness mobile_local_release_status drifted")
        require(mobile_detail.get("campaign_session_recover_recap_effective_state") == "ready", "cross_surface_readiness campaign_session_recover_recap state drifted")
        require(mobile_detail.get("recover_from_sync_conflict_owner_scoped_effective_state") == "ready", "cross_surface_readiness recover_from_sync_conflict owner-scoped state drifted")

    flagship_ready = cross_surface_readiness.get("flagship_ready")
    require(isinstance(flagship_ready, dict), "cross_surface_readiness flagship_ready must be an object")
    if isinstance(flagship_ready, dict):
        require(str(flagship_ready.get("status") or "").strip() in {"ready", "warning"}, "cross_surface_readiness flagship_ready status drifted")
        require(isinstance(flagship_ready.get("summary"), str) and flagship_ready.get("summary").strip(), "cross_surface_readiness flagship_ready summary missing")
        require(isinstance(flagship_ready.get("reasons"), list), "cross_surface_readiness flagship_ready reasons must be a list")

    for key in ["ready_keys", "warning_keys", "missing_keys", "readiness_plane_gap_keys", "non_mobile_blockers"]:
        require(isinstance(cross_surface_readiness.get(key), list), f"cross_surface_readiness {key} must be a list")
    warning_keys = cross_surface_readiness.get("warning_keys") if isinstance(cross_surface_readiness.get("warning_keys"), list) else []
    missing_keys = cross_surface_readiness.get("missing_keys") if isinstance(cross_surface_readiness.get("missing_keys"), list) else []
    non_mobile_blockers = cross_surface_readiness.get("non_mobile_blockers") if isinstance(cross_surface_readiness.get("non_mobile_blockers"), list) else []
    require("mobile_play_shell" not in set(warning_keys) | set(missing_keys), "cross_surface_readiness lists mobile_play_shell as a blocker")
    require(all(item != "mobile_play_shell" for item in non_mobile_blockers), "cross_surface_readiness non_mobile_blockers includes mobile_play_shell")

cross_surface_refresh = payload.get("cross_surface_refresh")
require(isinstance(cross_surface_refresh, dict), "cross_surface_refresh must be an object")
if isinstance(cross_surface_refresh, dict):
    require(cross_surface_refresh.get("script_path") == "scripts/materialize_mobile_cross_surface_readiness.py", "cross_surface_refresh script_path drifted")
    require(cross_surface_refresh.get("receipt_path") == ".codex-studio/published/MOBILE_CROSS_SURFACE_READINESS.generated.json", "cross_surface_refresh receipt_path drifted")
    require(cross_surface_refresh.get("required") is False, "cross_surface_refresh required flag drifted")
    refresh_status = cross_surface_refresh.get("status")
    require(refresh_status in {"pass", "fail", "not_materialized"}, f"cross_surface_refresh status drifted: {refresh_status!r}")
    if refresh_status in {"pass", "fail"}:
        require(cross_surface_refresh.get("contract_name") == "chummer6-mobile.cross_surface_readiness_refresh.v1", "cross_surface_refresh contract_name drifted")
        require(isinstance(cross_surface_refresh.get("generated_at_utc"), str) and cross_surface_refresh.get("generated_at_utc").strip(), "cross_surface_refresh missing generated_at_utc")
        checks = cross_surface_refresh.get("checks")
        require(isinstance(checks, dict), "cross_surface_refresh checks must be an object")
        required_checks = {
            "fleet_mobile_play_shell_ready",
            "fleet_mobile_local_release_passed",
            "fleet_mobile_scope_not_blocking",
        }
        live_mobile_checks = [
            "public_edge_frontdoor_navigation_pass",
            "public_edge_frontdoor_route_is_player",
            "public_edge_handoff_launch_route_is_player",
            "public_edge_role_alias_routes_pass",
            "public_edge_public_targets_keep_play_only",
            "frontdoor_player_gm_blazor_shells_live",
            "frontdoor_player_gm_role_manifests_live",
            "frontdoor_player_gm_handoff_links_preserve_role_and_strip_device",
            "frontdoor_rybbit_roles_live",
        ]
        if isinstance(checks, dict):
            for check_id in sorted(required_checks):
                require(checks.get(check_id) is True, f"cross_surface_refresh check is not true: {check_id}")
            public_edge = cross_surface_refresh.get("public_edge")
            if isinstance(public_edge, dict) and public_edge.get("skipped") is not True:
                if refresh_status == "pass":
                    for check_id in live_mobile_checks:
                        require(checks.get(check_id) is True, f"cross_surface_refresh live public-edge check is not true: {check_id}")
                    for check_id in [
                        "public_edge_pwa_static_pass",
                        "public_edge_mobile_ledger_pass",
                        "public_edge_gate_pass",
                        "strict_public_edge_gate_pass",
                    ]:
                        require(checks.get(check_id) is True, f"cross_surface_refresh strict public-edge check is not true: {check_id}")
                elif refresh_status == "fail":
                    require(checks.get("strict_public_edge_gate_pass") is not True, "cross_surface_refresh failed receipt still reports strict_public_edge_gate_pass")

        require(isinstance(cross_surface_refresh.get("base_url"), str) and cross_surface_refresh.get("base_url").strip(), "cross_surface_refresh missing base_url")
        require(isinstance(cross_surface_refresh.get("fleet_status"), str) and cross_surface_refresh.get("fleet_status").strip(), "cross_surface_refresh missing fleet_status")
        require(isinstance(cross_surface_refresh.get("fleet_warning_keys"), list), "cross_surface_refresh fleet_warning_keys must be a list")
        require(isinstance(cross_surface_refresh.get("fleet_missing_keys"), list), "cross_surface_refresh fleet_missing_keys must be a list")
        mobile_detail = cross_surface_refresh.get("mobile_play_shell")
        require(isinstance(mobile_detail, dict), "cross_surface_refresh mobile_play_shell must be an object")
        if isinstance(mobile_detail, dict):
            require(mobile_detail.get("status") == "ready", "cross_surface_refresh mobile_play_shell status drifted")
            require(isinstance(mobile_detail.get("summary"), str) and mobile_detail.get("summary").strip(), "cross_surface_refresh mobile_play_shell summary missing")
            require(mobile_detail.get("mobile_local_release_status") == "passed", "cross_surface_refresh mobile_local_release_status drifted")
        public_edge = cross_surface_refresh.get("public_edge")
        require(isinstance(public_edge, dict), "cross_surface_refresh public_edge must be an object")
        if verification_mode == "release" and isinstance(public_edge, dict):
            require(public_edge.get("skipped") is False, "release cross_surface_refresh skipped public-edge proof")
        if isinstance(public_edge, dict) and public_edge.get("skipped") is not True:
            require(public_edge.get("status") == refresh_status, "cross_surface_refresh public_edge status drifted")
            require(isinstance(public_edge.get("failures"), list), "cross_surface_refresh public_edge failures must be a list")
            live_build_lock_probe = public_edge.get("live_build_lock_probe")
            require(isinstance(live_build_lock_probe, dict), "cross_surface_refresh public_edge live_build_lock_probe must be an object")
            if isinstance(live_build_lock_probe, dict):
                require(live_build_lock_probe.get("command") == "ps -eo pid=,args=", "cross_surface_refresh public_edge live_build_lock_probe command drifted")
                require(isinstance(live_build_lock_probe.get("status"), str) and live_build_lock_probe.get("status").strip(), "cross_surface_refresh public_edge live_build_lock_probe status missing")
                require(isinstance(live_build_lock_probe.get("process_count"), int), "cross_surface_refresh public_edge live_build_lock_probe process_count missing")
                if live_build_lock_probe.get("status") == "present":
                    require(isinstance(live_build_lock_probe.get("entries"), list) and len(live_build_lock_probe.get("entries")) > 0, "cross_surface_refresh public_edge live_build_lock_probe present state must list entries")
            require(
                public_edge.get("strict_postdeploy_stale") in {True, False, None},
                "cross_surface_refresh public_edge strict_postdeploy_stale drifted",
            )
            require(
                public_edge.get("strict_postdeploy_strict_preflight") in {True, False},
                "cross_surface_refresh public_edge strict_postdeploy_strict_preflight drifted",
            )
            require(
                public_edge.get("strict_postdeploy_strict_invocation") in {True, False},
                "cross_surface_refresh public_edge strict_postdeploy_strict_invocation drifted",
            )
            require(
                public_edge.get("strict_postdeploy_strict_no_allowance_invocation") in {True, False},
                "cross_surface_refresh public_edge strict_postdeploy_strict_no_allowance_invocation drifted",
            )
            stale_failure_present = any(
                isinstance(item, str) and "older than the current strict preflight receipt" in item
                for item in public_edge.get("failures", [])
            )
            if refresh_status == "pass":
                require(public_edge.get("strict_postdeploy_stale") is not True, "cross_surface_refresh passing public-edge receipt drifted to stale strict postdeploy state")
                require(public_edge.get("strict_postdeploy_strict_preflight") is True, "cross_surface_refresh passing public-edge receipt must prove strict preflight mode")
                require(public_edge.get("strict_postdeploy_strict_no_allowance_invocation") is True, "cross_surface_refresh passing public-edge receipt must prove no-allowance strict invocation")
                require(isinstance(public_edge.get("frontdoor_navigation_status"), str) and public_edge.get("frontdoor_navigation_status").strip(), "cross_surface_refresh public_edge frontdoor_navigation_status missing")
                require(public_edge.get("frontdoor_route") == "/mobile/player", "cross_surface_refresh public_edge frontdoor_route drifted")
                require(public_edge.get("handoff_launch_route") == "/mobile/player", "cross_surface_refresh public_edge handoff_launch_route drifted")
                require(public_edge.get("player_manifest_path") == "/manifest.player.webmanifest", "cross_surface_refresh public_edge player manifest drifted")
                require(public_edge.get("gm_manifest_path") == "/manifest.gm.webmanifest", "cross_surface_refresh public_edge GM manifest drifted")
                require(public_edge.get("player_handoff_strips_device") is True, "cross_surface_refresh public_edge player handoff stripping drifted")
                require(public_edge.get("gm_handoff_strips_device") is True, "cross_surface_refresh public_edge GM handoff stripping drifted")
                require(public_edge.get("rybbit_player") is True, "cross_surface_refresh public_edge player Rybbit drifted")
                require(public_edge.get("rybbit_gm") is True, "cross_surface_refresh public_edge GM Rybbit drifted")
            elif refresh_status == "fail":
                require(len(public_edge.get("failures")) > 0, "cross_surface_refresh failed public-edge receipt must list blockers")
                if stale_failure_present:
                    require(public_edge.get("strict_postdeploy_stale") is True, "cross_surface_refresh stale strict-postdeploy blocker must set strict_postdeploy_stale")
        source_fingerprint = cross_surface_refresh.get("surface_source_fingerprint")
        require(isinstance(source_fingerprint, dict), "cross_surface_refresh surface_source_fingerprint must be an object")
        if isinstance(source_fingerprint, dict):
            paths = source_fingerprint.get("paths")
            require(source_fingerprint.get("kind") == "current_checkout_sha256", "cross_surface_refresh source fingerprint kind drifted")
            require(isinstance(paths, list) and len(paths) > 0, "cross_surface_refresh source fingerprint paths missing")
            require(source_fingerprint.get("matches_current_checkout") is True, "cross_surface_refresh source fingerprint no longer matches the checkout")
            require(isinstance(source_fingerprint.get("file_count"), int), "cross_surface_refresh source fingerprint file_count missing")
            if isinstance(paths, list):
                require(source_fingerprint.get("file_count") == len(paths), "cross_surface_refresh source fingerprint file_count drifted")

release_boundary = payload.get("release_boundary")
require(isinstance(release_boundary, dict), "release_boundary must be an object")
if isinstance(release_boundary, dict):
    require(release_boundary.get("script_path") == "scripts/materialize_mobile_release_boundary.py", "release_boundary script_path drifted")
    require(release_boundary.get("receipt_path") == ".codex-studio/published/MOBILE_RELEASE_BOUNDARY.generated.json", "release_boundary receipt_path drifted")
    require(release_boundary.get("required") is True, "release_boundary required flag drifted")
    require(release_boundary.get("status") == "pass", "release_boundary status is not pass")
    require(release_boundary.get("contract_name") == "chummer6-mobile.release_boundary.v1", "release_boundary contract_name drifted")
    require(isinstance(release_boundary.get("generated_at_utc"), str) and release_boundary.get("generated_at_utc").strip(), "release_boundary missing generated_at_utc")
    ownership_checks = release_boundary.get("ownership_checks")
    require(isinstance(ownership_checks, dict), "release_boundary ownership_checks must be an object")
    expected_boundary_checks = {
        "owned_play_source_files_present",
        "owned_play_test_files_present",
        "owned_run_services_source_files_present",
        "owned_run_services_test_files_present",
        "owned_release_receipts_present",
        "release_receipts_machine_local_noise_free",
    }
    boundary_check_keys = set(ownership_checks or {}) if isinstance(ownership_checks, dict) else set()
    require(boundary_check_keys == expected_boundary_checks, f"release_boundary ownership_checks drifted: {sorted(boundary_check_keys ^ expected_boundary_checks)}")
    if isinstance(ownership_checks, dict):
        for check_id in sorted(expected_boundary_checks):
            require(ownership_checks.get(check_id) is True, f"release_boundary ownership check is not true: {check_id}")
    require(
        release_boundary.get("release_receipt_count") == len(expected_release_receipts),
        "release_boundary release_receipt_count drifted",
    )
    for key in [
        "play_owned_entry_count",
        "play_foreign_entry_count",
        "play_disposable_entry_count",
        "play_external_blocker_entry_count",
        "play_ambient_entry_count",
        "run_services_owned_entry_count",
        "run_services_foreign_entry_count",
        "run_services_ambient_entry_count",
        "owned_disposable_local_artifact_count",
        "shared_external_temp_artifact_count",
        "disposable_local_artifact_count",
    ]:
        require(isinstance(release_boundary.get(key), int), f"release_boundary {key} must be an int")
    require(release_boundary.get("play_foreign_entry_count") == 0, "release_boundary reviewable play foreign entry count must be zero")
    require(
        release_boundary.get("disposable_local_artifact_count")
        == release_boundary.get("owned_disposable_local_artifact_count") + release_boundary.get("shared_external_temp_artifact_count"),
        "release_boundary disposable artifact partition count drifted",
    )
    preflight_snapshot = release_boundary.get("preflight_snapshot")
    require(isinstance(preflight_snapshot, dict), "release_boundary preflight_snapshot must be an object")
    if isinstance(preflight_snapshot, dict):
        require(isinstance(preflight_snapshot.get("path"), str) and preflight_snapshot.get("path").strip(), "release_boundary preflight_snapshot path missing")
        require(isinstance(preflight_snapshot.get("status"), str) and preflight_snapshot.get("status").strip(), "release_boundary preflight_snapshot status missing")
        if preflight_snapshot.get("status") == "fail":
            require(isinstance(preflight_snapshot.get("blocking_findings"), list) and len(preflight_snapshot.get("blocking_findings")) > 0, "release_boundary failed preflight snapshot must list blocking findings")
    postdeploy_snapshot = release_boundary.get("postdeploy_snapshot")
    require(isinstance(postdeploy_snapshot, dict), "release_boundary postdeploy_snapshot must be an object")
    if isinstance(postdeploy_snapshot, dict):
        require(isinstance(postdeploy_snapshot.get("path"), str) and postdeploy_snapshot.get("path").strip(), "release_boundary postdeploy_snapshot path missing")
        require(isinstance(postdeploy_snapshot.get("status"), str) and postdeploy_snapshot.get("status").strip(), "release_boundary postdeploy_snapshot status missing")
        if postdeploy_snapshot.get("status") == "fail":
            require(isinstance(postdeploy_snapshot.get("failures"), list) and len(postdeploy_snapshot.get("failures")) > 0, "release_boundary failed postdeploy snapshot must list failures")
    design_mirror_snapshot = release_boundary.get("design_mirror_snapshot")
    require(isinstance(design_mirror_snapshot, dict), "release_boundary design_mirror_snapshot must be an object")
    if isinstance(design_mirror_snapshot, dict):
        require(design_mirror_snapshot.get("script_path") == "scripts/ai/verify_design_mirror.py", "release_boundary design_mirror_snapshot script_path drifted")
        require(design_mirror_snapshot.get("command") == "python3 scripts/ai/verify_design_mirror.py", "release_boundary design_mirror_snapshot command drifted")
        require(isinstance(design_mirror_snapshot.get("status"), str) and design_mirror_snapshot.get("status").strip(), "release_boundary design_mirror_snapshot status missing")
        if design_mirror_snapshot.get("status") == "fail":
            require(isinstance(design_mirror_snapshot.get("blocking_findings"), list) and len(design_mirror_snapshot.get("blocking_findings")) > 0, "release_boundary failed design_mirror_snapshot must list blocking findings")
            require(isinstance(design_mirror_snapshot.get("repair_commands"), list) and len(design_mirror_snapshot.get("repair_commands")) > 0, "release_boundary failed design_mirror_snapshot must list repair commands")
    live_build_lock_probe = release_boundary.get("live_build_lock_probe")
    require(isinstance(live_build_lock_probe, dict), "release_boundary live_build_lock_probe must be an object")
    if isinstance(live_build_lock_probe, dict):
        require(live_build_lock_probe.get("command") == "ps -eo pid=,args=", "release_boundary live_build_lock_probe command drifted")
        require(isinstance(live_build_lock_probe.get("status"), str) and live_build_lock_probe.get("status").strip(), "release_boundary live_build_lock_probe status missing")
        require(isinstance(live_build_lock_probe.get("process_count"), int), "release_boundary live_build_lock_probe process_count missing")
        if live_build_lock_probe.get("status") == "present":
            require(isinstance(live_build_lock_probe.get("entries"), list) and len(live_build_lock_probe.get("entries")) > 0, "release_boundary live_build_lock_probe present state must list entries")
    canonical_release_blockers = release_boundary.get("canonical_release_blockers")
    require(isinstance(canonical_release_blockers, dict), "release_boundary canonical_release_blockers must be an object")
    if isinstance(canonical_release_blockers, dict):
        require(canonical_release_blockers.get("status") == "present", "release_boundary canonical_release_blockers status drifted")
        require(isinstance(canonical_release_blockers.get("path"), str) and canonical_release_blockers.get("path").strip(), "release_boundary canonical_release_blockers path missing")
        require(isinstance(canonical_release_blockers.get("generated_at"), str) and canonical_release_blockers.get("generated_at").strip(), "release_boundary canonical_release_blockers generated_at missing")
        root_blocker_ids = canonical_release_blockers.get("root_blocker_ids")
        root_blockers = canonical_release_blockers.get("root_blockers")
        require(isinstance(root_blocker_ids, list), "release_boundary canonical_release_blockers root_blocker_ids missing")
        require(isinstance(root_blockers, list), "release_boundary canonical_release_blockers root_blockers missing")
        require(isinstance(canonical_release_blockers.get("root_blocker_count"), int), "release_boundary canonical_release_blockers root_blocker_count missing")
        if isinstance(root_blocker_ids, list):
            require(canonical_release_blockers.get("root_blocker_count") == len(root_blocker_ids), "release_boundary canonical_release_blockers root_blocker_count drifted")
        if isinstance(root_blockers, list):
            for row in root_blockers:
                require(isinstance(row, dict), "release_boundary canonical_release_blockers row must be an object")
                if isinstance(row, dict):
                    require(isinstance((row.get("blocker_id") or row.get("id")), str) and str((row.get("blocker_id") or row.get("id"))).strip(), "release_boundary canonical_release_blockers row missing blocker id")
                    require(isinstance(row.get("failing_gate"), str) and row.get("failing_gate").strip(), "release_boundary canonical_release_blockers row missing failing_gate")
        if root_release_blockers_path.is_file():
            live_release_blockers = json.loads(root_release_blockers_path.read_text(encoding="utf-8"))
            require(isinstance(live_release_blockers.get("generated_at"), str) and live_release_blockers.get("generated_at").strip(), "live release blockers generated_at missing")
            snapshot_root_blocker_ids = canonical_release_blockers.get("root_blocker_ids")
            live_root_blocker_ids = live_release_blockers.get("root_blocker_ids")
            require(isinstance(live_root_blocker_ids, list), "live release blockers root_blocker_ids missing")
            if isinstance(snapshot_root_blocker_ids, list) and isinstance(live_root_blocker_ids, list):
                snapshot_substantive_ids = [
                    blocker_id
                    for blocker_id in snapshot_root_blocker_ids
                    if blocker_id not in self_referential_release_wrapper_ids
                ]
                live_substantive_ids = [
                    blocker_id
                    for blocker_id in live_root_blocker_ids
                    if blocker_id not in self_referential_release_wrapper_ids
                ]
                require(
                    snapshot_substantive_ids == live_substantive_ids,
                    "release_boundary canonical_release_blockers substantive root_blocker_ids drifted from live receipt",
                )
    external_follow_through = release_boundary.get("external_follow_through")
    require(isinstance(external_follow_through, dict), "release_boundary external_follow_through must be an object")
    if isinstance(external_follow_through, dict):
        design_follow_through = external_follow_through.get("design_mirror")
        strict_follow_through = external_follow_through.get("strict_public_edge")
        require(isinstance(design_follow_through, dict), "release_boundary external_follow_through design_mirror must be an object")
        require(isinstance(strict_follow_through, dict), "release_boundary external_follow_through strict_public_edge must be an object")
        if isinstance(design_follow_through, dict):
            require(isinstance(design_follow_through.get("status"), str) and design_follow_through.get("status").strip(), "release_boundary external_follow_through design_mirror status missing")
            require(isinstance(design_follow_through.get("repair_commands"), list), "release_boundary external_follow_through design_mirror repair_commands missing")
        if isinstance(strict_follow_through, dict):
            require(isinstance(strict_follow_through.get("status"), str) and strict_follow_through.get("status").strip(), "release_boundary external_follow_through strict_public_edge status missing")
            require(isinstance(strict_follow_through.get("follow_through_command"), str) and strict_follow_through.get("follow_through_command").strip(), "release_boundary external_follow_through strict_public_edge follow_through_command missing")
            require(isinstance(strict_follow_through.get("follow_through_receipt_path"), str) and strict_follow_through.get("follow_through_receipt_path").strip(), "release_boundary external_follow_through strict_public_edge follow_through_receipt_path missing")
            require(strict_follow_through.get("follow_through_receipt_path") == ".codex-studio/published/MOBILE_STRICT_PUBLIC_EDGE_FOLLOW_THROUGH.generated.json", "release_boundary external_follow_through strict_public_edge follow_through_receipt_path drifted")
            require(isinstance(strict_follow_through.get("rerun_commands"), list) and len(strict_follow_through.get("rerun_commands")) >= 2, "release_boundary external_follow_through strict_public_edge rerun_commands missing")

if marker_misses:
    errors.extend(f"source-backed marker missing: {item}" for item in marker_misses[:25])
    if len(marker_misses) > 25:
        errors.append(f"{len(marker_misses) - 25} additional source-backed marker misses omitted")

if errors:
    raise SystemExit("\n".join(f"mobile release proof invalid: {error}" for error in errors))

print('mobile release proof ok')
PY
