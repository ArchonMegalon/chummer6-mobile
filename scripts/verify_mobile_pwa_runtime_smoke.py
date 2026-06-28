#!/usr/bin/env python3
from __future__ import annotations

import json
import os
import re
import socket
import subprocess
import sys
import tempfile
import time
import urllib.error
import urllib.request
from pathlib import Path

from playwright.sync_api import Page, sync_playwright


ROOT = Path(__file__).resolve().parents[1]
WEB_PROJECT = ROOT / "src" / "Chummer.Play.Web" / "Chummer.Play.Web.csproj"
PLAYER_SESSION = "session-runtime-smoke"
PLAYER_DEVICE = "runtime-player-shell"
PLAYER_ROLE = "Player"
GM_ROLE = "GameMaster"
LAST_ROUTE_KEY = "chummer-play-turn-companion:last-route"
PLAYER_ROUTE_KEY = f"{LAST_ROUTE_KEY}:{PLAYER_ROLE.lower()}"
GM_ROUTE_KEY = f"{LAST_ROUTE_KEY}:{GM_ROLE.lower()}"
TURN_SNAPSHOT_PREFIX = "chummer-play-turn-companion:"


def assert_true(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def free_port() -> int:
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as sock:
        sock.bind(("127.0.0.1", 0))
        return int(sock.getsockname()[1])


def wait_for_health(base_url: str, timeout_seconds: float = 30.0) -> None:
    deadline = time.time() + timeout_seconds
    health_url = f"{base_url}/health"
    last_error: Exception | None = None
    while time.time() < deadline:
        try:
            with urllib.request.urlopen(health_url, timeout=2.0) as response:
                body = response.read().decode("utf-8").strip()
                if response.status == 200 and body == "ok":
                    return
        except (urllib.error.URLError, OSError) as error:
            last_error = error
        time.sleep(0.25)
    raise RuntimeError(f"web app did not become healthy at {health_url}: {last_error}")


def wait_for_mobile_shell(page: Page) -> None:
    page.wait_for_selector("[data-turn-root]")
    page.wait_for_selector("#turn-shell-summary")
    page.wait_for_function("() => document.getElementById('turn-install-button') !== null")


def parse_local_storage_json(page: Page, key: str) -> dict[str, object]:
    raw = page.evaluate("(storageKey) => window.localStorage.getItem(storageKey)", key)
    assert_true(isinstance(raw, str) and raw != "", f"expected localStorage item {key!r} to exist")
    payload = json.loads(raw)
    assert_true(isinstance(payload, dict), f"expected localStorage item {key!r} to decode to an object")
    return payload


def turn_snapshot_key(session_id: str, role: str, device_id: str) -> str:
    return f"{TURN_SNAPSHOT_PREFIX}{session_id}:{role}:{device_id}"


def wait_for_text(page: Page, selector: str, expected: str) -> None:
    page.wait_for_function(
        """([targetSelector, expectedText]) => {
            const element = document.querySelector(targetSelector);
            return !!element && (element.textContent || "").includes(expectedText);
        }""",
        arg=[selector, expected],
    )


def wait_for_exact_text(page: Page, selector: str, expected: str) -> None:
    page.wait_for_function(
        """([targetSelector, expectedText]) => {
            const element = document.querySelector(targetSelector);
            return !!element && (element.textContent || "").trim() === expectedText;
        }""",
        arg=[selector, expected],
    )


def text_content(page: Page, selector: str) -> str:
    return page.locator(selector).text_content() or ""


def wait_for_offline_posture(page: Page, timeout_ms: int = 60_000) -> None:
    page.wait_for_function(
        """() => {
            const banner = document.getElementById('mobile-client-banner-title')?.textContent || '';
            const network = document.getElementById('turn-network-state')?.textContent || '';
            const status = document.getElementById('turn-status-message')?.textContent || '';
            return navigator.onLine === false
                || banner.includes('Offline')
                || network.trim() === 'Offline'
                || status.includes('Offline mode');
        }""",
        timeout=timeout_ms,
    )


def find_stat_value(snapshot: dict[str, object], metric_id: str) -> int:
    projection = snapshot.get("projection")
    assert_true(isinstance(projection, dict), "turn snapshot must contain a projection")
    now = projection.get("now")
    assert_true(isinstance(now, dict), "turn snapshot must contain a now surface")
    stat_cards = now.get("statCards")
    assert_true(isinstance(stat_cards, list), "turn snapshot must contain stat cards")
    for card in stat_cards:
        if isinstance(card, dict) and card.get("metricId") == metric_id:
            value = card.get("value")
            assert_true(isinstance(value, int), f"turn snapshot stat card {metric_id!r} must expose an integer value")
            return value
    raise AssertionError(f"turn snapshot did not contain stat card {metric_id!r}")


def wait_for_service_worker_control(page: Page) -> None:
    page.evaluate("() => navigator.serviceWorker.ready.then(() => true)")
    page.wait_for_function("() => !!navigator.serviceWorker?.controller")


def wait_for_query(page: Page, *, session_id: str, role: str) -> None:
    page.wait_for_function(
        """([expectedSessionId, expectedRole]) => {
            const params = new URLSearchParams(window.location.search);
            return params.get("sessionId") === expectedSessionId && params.get("role") === expectedRole && !!params.get("deviceId");
        }""",
        arg=[session_id, role],
    )


def query_params(page: Page) -> dict[str, str]:
    return page.evaluate(
        """() => {
            const params = new URLSearchParams(window.location.search);
            return {
                sessionId: params.get("sessionId") || "",
                role: params.get("role") || "",
                deviceId: params.get("deviceId") || ""
            };
        }"""
    )


def start_server() -> tuple[subprocess.Popen[str], str, str]:
    port = free_port()
    base_url = f"http://127.0.0.1:{port}"
    state_dir = tempfile.mkdtemp(prefix="chummer-play-runtime-smoke-")
    env = os.environ.copy()
    env["CHUMMER_PLAY_BROWSER_STATE_DIR"] = state_dir
    env["CHUMMER_PLAY_BROWSER_STATE_ISOLATE_PER_APP"] = "true"
    env["ASPNETCORE_URLS"] = base_url

    process = subprocess.Popen(
        [
            "dotnet",
            "run",
            "--no-launch-profile",
            "--project",
            str(WEB_PROJECT),
            "--no-build",
        ],
        cwd=str(ROOT),
        env=env,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        text=True,
    )
    wait_for_health(base_url)
    return process, base_url, state_dir


def stop_server(process: subprocess.Popen[str]) -> None:
    if process.poll() is not None:
        return
    process.terminate()
    try:
        process.wait(timeout=10)
    except subprocess.TimeoutExpired:
        process.kill()
        process.wait(timeout=10)


def main() -> int:
    process: subprocess.Popen[str] | None = None
    current_step = "boot"
    current_page: Page | None = None
    try:
        process, base_url, _state_dir = start_server()
        with sync_playwright() as playwright:
            browser = playwright.chromium.launch(headless=True)
            context = browser.new_context(service_workers="allow")

            current_step = "open explicit player lane"
            player_page = context.new_page()
            current_page = player_page
            player_page.goto(
                f"{base_url}/mobile?sessionId={PLAYER_SESSION}&role={PLAYER_ROLE}&deviceId={PLAYER_DEVICE}",
                wait_until="domcontentloaded",
            )
            wait_for_mobile_shell(player_page)
            player_page.wait_for_function(
                "() => document.getElementById('turn-continuity-device')?.textContent?.length > 0"
            )
            current_step = "activate service worker on explicit player lane"
            wait_for_service_worker_control(player_page)
            player_page.reload(wait_until="domcontentloaded")
            wait_for_mobile_shell(player_page)
            wait_for_service_worker_control(player_page)

            current_step = "assert player local storage"
            player_last_route = parse_local_storage_json(player_page, LAST_ROUTE_KEY)
            assert_true(player_last_route.get("sessionId") == PLAYER_SESSION, "global last route must persist the player session")
            assert_true(player_last_route.get("roleName") == PLAYER_ROLE, "global last route must persist the player role")
            assert_true(player_last_route.get("deviceId") == PLAYER_DEVICE, "global last route must persist the explicit player device")

            player_role_route = parse_local_storage_json(player_page, PLAYER_ROUTE_KEY)
            assert_true(player_role_route.get("deviceId") == PLAYER_DEVICE, "player role route must persist the explicit player device")

            current_step = "player live interactions"
            player_page.wait_for_function(
                "() => document.getElementById('turn-network-state')?.textContent === 'Online'"
            )
            player_page.select_option("#runsite-anchor", "server-room")
            wait_for_text(player_page, "#turn-runsite-summary", "Server Room")
            wait_for_exact_text(player_page, "#turn-local-queue-count", "1")

            player_page.click("button[data-turn-kind='adjust-metric'][data-metric-id='ammo'][data-delta='-1']")
            wait_for_text(player_page, "#turn-weapon-label", "magazine 11")
            wait_for_exact_text(player_page, "#turn-local-queue-count", "2")

            player_page.fill("#manual-hits", "3")
            player_page.click("button[data-turn-kind='resolve-manual']")
            wait_for_text(player_page, "#turn-last-outcome", "3 hit(s)")
            wait_for_text(player_page, "#turn-last-outcome", "Magazine 11 -> 8")
            wait_for_exact_text(player_page, "#turn-local-queue-count", "3")
            wait_for_text(player_page, "#turn-history-list", "resolved")

            player_snapshot = parse_local_storage_json(
                player_page,
                turn_snapshot_key(PLAYER_SESSION, PLAYER_ROLE, PLAYER_DEVICE),
            )
            player_projection = player_snapshot.get("projection")
            assert_true(isinstance(player_projection, dict), "player turn snapshot must include the projection payload")
            player_runsite = player_projection.get("runsite")
            assert_true(isinstance(player_runsite, dict), "player turn snapshot must include runsite state")
            assert_true(player_runsite.get("selectedAnchorId") == "server-room", "player snapshot must persist the selected RUNSITE anchor")
            assert_true(find_stat_value(player_snapshot, "ammo") == 8, "player snapshot must persist the reduced magazine value after manual resolve")
            player_resolve = player_projection.get("resolve")
            assert_true(isinstance(player_resolve, dict), "player turn snapshot must include resolve state")
            assert_true(
                isinstance(player_resolve.get("lastOutcomeSummary"), str) and "3 hit(s)" in str(player_resolve.get("lastOutcomeSummary")),
                "player snapshot must persist the manual resolve receipt",
            )
            local_replay_queue = player_snapshot.get("localReplayQueue")
            assert_true(isinstance(local_replay_queue, list) and len(local_replay_queue) == 3, "player snapshot must persist the queued local replay receipts")

            current_step = "player replay and acknowledgement"
            player_page.click("#turn-replay-local-button")
            wait_for_exact_text(player_page, "#turn-local-queue-count", "0")
            wait_for_exact_text(player_page, "#turn-server-queue-count", "3")

            player_page.click("#turn-ack-server-button")
            wait_for_exact_text(player_page, "#turn-server-queue-count", "0")

            acknowledged_snapshot = parse_local_storage_json(
                player_page,
                turn_snapshot_key(PLAYER_SESSION, PLAYER_ROLE, PLAYER_DEVICE),
            )
            acknowledged_queue = acknowledged_snapshot.get("localReplayQueue")
            assert_true(isinstance(acknowledged_queue, list) and len(acknowledged_queue) == 0, "player snapshot must clear the local replay queue after replay succeeds")

            current_step = "generic resume"
            generic_page = context.new_page()
            current_page = generic_page
            generic_page.goto(f"{base_url}/mobile", wait_until="domcontentloaded")
            wait_for_mobile_shell(generic_page)
            wait_for_query(generic_page, session_id=PLAYER_SESSION, role=PLAYER_ROLE)
            generic_params = query_params(generic_page)
            assert_true(generic_params["deviceId"] == PLAYER_DEVICE, "generic mobile launch must resume the last claimed player lane")
            wait_for_text(generic_page, "#turn-runsite-summary", "Server Room")
            wait_for_text(generic_page, "#turn-last-outcome", "3 hit(s)")
            wait_for_exact_text(generic_page, "#turn-local-queue-count", "0")
            wait_for_text(generic_page, "#turn-history-list", "resolved")

            current_step = "gm shortcut session resume"
            gm_first_page = context.new_page()
            current_page = gm_first_page
            gm_first_page.goto(f"{base_url}/mobile?role={GM_ROLE}", wait_until="domcontentloaded")
            wait_for_mobile_shell(gm_first_page)
            wait_for_query(gm_first_page, session_id=PLAYER_SESSION, role=GM_ROLE)
            gm_first_params = query_params(gm_first_page)
            assert_true(gm_first_params["sessionId"] == PLAYER_SESSION, "GM shortcut must resume the last live session")
            assert_true(gm_first_params["role"] == GM_ROLE, "GM shortcut must stay in the GM lane")
            assert_true(gm_first_params["deviceId"] != "", "GM shortcut must land on a claimed device lane")

            gm_role_route = parse_local_storage_json(gm_first_page, GM_ROUTE_KEY)
            assert_true(gm_role_route.get("sessionId") == PLAYER_SESSION, "GM role route must persist the resumed session")
            assert_true(gm_role_route.get("roleName") == GM_ROLE, "GM role route must persist the GM role")
            assert_true(gm_role_route.get("deviceId") == gm_first_params["deviceId"], "GM role route must persist the resolved GM device lane")

            current_step = "gm shortcut claimed-device resume"
            gm_second_page = context.new_page()
            current_page = gm_second_page
            gm_second_page.goto(f"{base_url}/mobile?role={GM_ROLE}", wait_until="domcontentloaded")
            wait_for_mobile_shell(gm_second_page)
            wait_for_query(gm_second_page, session_id=PLAYER_SESSION, role=GM_ROLE)
            gm_second_params = query_params(gm_second_page)
            assert_true(gm_second_params["deviceId"] == gm_first_params["deviceId"], "GM shortcut must resume the last GM claimed-device lane after it exists")

            current_step = "gm live interactions"
            gm_second_page.wait_for_function(
                "() => document.getElementById('turn-network-state')?.textContent === 'Online'"
            )
            wait_for_text(gm_second_page, "#turn-actor-label", "GM focus actor")
            gm_second_page.click("button[data-turn-kind='select-action'][data-action-id='reveal-threat']")
            wait_for_text(gm_second_page, "#turn-resolve-label", "Reveal Threat")
            gm_second_page.select_option("#runsite-anchor", "fire-stairs")
            wait_for_text(gm_second_page, "#turn-runsite-summary", "Fire Stairs")
            gm_second_page.click("button[data-turn-kind='queue-quick-action'][data-action-id='gm-advance-initiative']")
            wait_for_exact_text(gm_second_page, "#turn-local-queue-count", "3")
            wait_for_text(gm_second_page, "#turn-history-list", "Quick action queued")

            gm_snapshot = parse_local_storage_json(
                gm_second_page,
                turn_snapshot_key(PLAYER_SESSION, GM_ROLE, gm_second_params["deviceId"]),
            )
            gm_projection = gm_snapshot.get("projection")
            assert_true(isinstance(gm_projection, dict), "gm turn snapshot must include the projection payload")
            gm_runsite = gm_projection.get("runsite")
            assert_true(isinstance(gm_runsite, dict), "gm turn snapshot must include runsite state")
            assert_true(gm_runsite.get("selectedAnchorId") == "fire-stairs", "gm snapshot must persist the selected RUNSITE anchor")
            gm_act = gm_projection.get("act")
            assert_true(isinstance(gm_act, dict), "gm turn snapshot must include action state")
            assert_true(gm_act.get("selectedActionId") == "reveal-threat", "gm snapshot must persist the selected bounded GM action")
            gm_local_replay_queue = gm_snapshot.get("localReplayQueue")
            assert_true(isinstance(gm_local_replay_queue, list) and len(gm_local_replay_queue) == 3, "gm snapshot must persist the GM action, RUNSITE, and quick-action replay receipts before replay")

            gm_second_page.click("#turn-replay-local-button")
            wait_for_exact_text(gm_second_page, "#turn-local-queue-count", "0")
            wait_for_exact_text(gm_second_page, "#turn-server-queue-count", "3")

            gm_second_page.click("#turn-ack-server-button")
            wait_for_exact_text(gm_second_page, "#turn-server-queue-count", "0")

            gm_ack_snapshot = parse_local_storage_json(
                gm_second_page,
                turn_snapshot_key(PLAYER_SESSION, GM_ROLE, gm_second_params["deviceId"]),
            )
            gm_ack_queue = gm_ack_snapshot.get("localReplayQueue")
            assert_true(isinstance(gm_ack_queue, list) and len(gm_ack_queue) == 0, "gm snapshot must clear the local replay queue after replay succeeds")

            current_step = "offline reopen"
            context.set_offline(True)
            time.sleep(0.75)
            offline_page = context.new_page()
            current_page = offline_page
            offline_page.goto(f"{base_url}/mobile", wait_until="domcontentloaded")
            wait_for_mobile_shell(offline_page)
            wait_for_offline_posture(offline_page)
            offline_params = query_params(offline_page)
            assert_true(offline_params["sessionId"] == PLAYER_SESSION, "offline reopen must keep the last resumed session id")
            assert_true(offline_params["role"] == GM_ROLE, "offline reopen must keep the last resumed role lane")
            offline_banner = offline_page.locator("#mobile-client-banner-title").text_content() or ""
            offline_network = text_content(offline_page, "#turn-network-state").strip()
            assert_true(
                "Offline" in offline_banner or offline_network == "Offline",
                "offline reopen must surface the offline posture",
            )
            wait_for_text(offline_page, "#turn-runsite-summary", "Fire Stairs")
            wait_for_text(offline_page, "#turn-resolve-label", "Reveal Threat")
            wait_for_exact_text(offline_page, "#turn-local-queue-count", "0")

            interaction_outcome = text_content(player_page, "#turn-last-outcome")
            interaction_hits_match = re.search(r"(\d+) hit\(s\)", interaction_outcome)
            interaction_hits = interaction_hits_match.group(1) if interaction_hits_match else "?"
            resumed_runsite_summary = text_content(generic_page, "#turn-runsite-summary")
            gm_resume_summary = text_content(offline_page, "#turn-runsite-summary")

            browser.close()

        print("mobile_pwa_runtime_smoke ok")
        print(f"  service_worker_controlled: true")
        print(f"  player_interactions: server-room / ammo 8 / manual {interaction_hits} hit(s)")
        print(f"  replay_ack: local 3->0 / server 0->3->0")
        print(f"  player_resume_snapshot: {resumed_runsite_summary}")
        print(f"  gm_interactions: fire-stairs / reveal-threat / local 3->0 / server 0->3->0")
        print(f"  gm_resume_snapshot: {gm_resume_summary}")
        print(f"  generic_resume: {PLAYER_SESSION} / {PLAYER_ROLE} / {PLAYER_DEVICE}")
        print(f"  gm_resume: {PLAYER_SESSION} / {GM_ROLE} / {gm_second_params['deviceId']}")
        print(f"  offline_reopen: {offline_params['sessionId']} / {offline_params['role']} / {offline_params['deviceId']}")
        return 0
    except Exception as error:  # noqa: BLE001
        print(f"mobile_pwa_runtime_smoke failed during {current_step}: {error}", file=sys.stderr)
        if current_page is not None:
            try:
                print(f"current_url: {current_page.url}", file=sys.stderr)
                print("--- page snippet ---", file=sys.stderr)
                print(current_page.content()[:1000], file=sys.stderr)
            except Exception:  # noqa: BLE001
                pass
        return 1
    finally:
        if process:
            stop_server(process)


if __name__ == "__main__":
    raise SystemExit(main())
