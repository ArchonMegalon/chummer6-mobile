#!/usr/bin/env python3
from __future__ import annotations

import json
import os
import re
import shutil
import signal
import socket
import subprocess
import sys
import tempfile
import time
import urllib.error
import urllib.request
from datetime import UTC, datetime
from pathlib import Path

from playwright.sync_api import Page, TimeoutError as PlaywrightTimeoutError, sync_playwright


ROOT = Path(__file__).resolve().parents[1]
WEB_PROJECT = ROOT / "src" / "Chummer.Play.Web" / "Chummer.Play.Web.csproj"
RECEIPT_PATH = ROOT / ".codex-studio" / "published" / "MOBILE_PWA_RUNTIME_SMOKE.generated.json"
PLAYER_SESSION = "session-runtime-smoke"
PLAYER_DEVICE = "runtime-player-shell"
PLAYER_ROLE = "Player"
GM_ROLE = "GameMaster"
HERO_SESSION = "session-hero-launch"
HERO_PLAYER_DEVICE = "hero-player-shell"
HERO_GM_DEVICE = "hero-gm-shell"
HERO_MENU_PLAYER_DEVICE = "hero-menu-player-shell"
HERO_MENU_GM_DEVICE = "hero-menu-gm-shell"
EXPECTED_SHELL_CACHE = "chummer-shell-play-shell-v16"
PREVIOUS_SHELL_CACHE = "chummer-shell-play-shell-v15"
LEGACY_SHELL_CACHE = "chummer-shell-play-shell-v10"
FOREIGN_CACHE = "foreign-origin-cache-smoke"
LAST_ROUTE_KEY = "chummer-play-turn-companion:last-route"
PLAYER_ROUTE_KEY = f"{LAST_ROUTE_KEY}:{PLAYER_ROLE.lower()}"
GM_ROUTE_KEY = f"{LAST_ROUTE_KEY}:{GM_ROLE.lower()}"
TURN_SNAPSHOT_PREFIX = "chummer-play-turn-companion:"
NAVIGATION_TIMEOUT_MS = 90_000
ACTION_TIMEOUT_MS = 45_000
EXPLICIT_DEVICE_IDS = {
    PLAYER_DEVICE,
    HERO_PLAYER_DEVICE,
    HERO_GM_DEVICE,
    HERO_MENU_PLAYER_DEVICE,
    HERO_MENU_GM_DEVICE,
}


def assert_true(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def redact_device_id(value: object) -> str:
    text = str(value or "").strip()
    if not text:
        return ""
    if text in EXPLICIT_DEVICE_IDS:
        return text
    return "<minted-device>"


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
    page.wait_for_selector("[data-turn-root][data-blazor-shell='interactive-server']", timeout=NAVIGATION_TIMEOUT_MS)
    page.wait_for_selector("script[src='/_framework/blazor.web.js']", state="attached", timeout=NAVIGATION_TIMEOUT_MS)
    page.wait_for_selector("#turn-shell-summary", timeout=NAVIGATION_TIMEOUT_MS)
    page.wait_for_function("() => document.getElementById('turn-install-button') !== null", timeout=NAVIGATION_TIMEOUT_MS)


def wait_for_play_shell_hero(page: Page) -> None:
    page.wait_for_selector("#workspace-shell:not([hidden])", timeout=NAVIGATION_TIMEOUT_MS)
    page.wait_for_function(
        """() => {
            const link = document.getElementById("shell-play-action-link");
            const menu = document.getElementById("shell-hero-action-menu");
            if (!link) {
                return false;
            }

            const url = new URL(link.href, window.location.origin);
            return url.pathname.startsWith("/mobile/")
                && !!menu
                && menu.querySelector("option[value='play']") !== null
                && (menu.dataset.playHref || "").includes("/mobile/");
        }""",
        timeout=NAVIGATION_TIMEOUT_MS,
    )


def goto_dom(page: Page, url: str) -> None:
    page.goto(url, wait_until="domcontentloaded", timeout=NAVIGATION_TIMEOUT_MS)


def describe_control_state(page: Page, selector: str) -> str:
    try:
        state = page.evaluate(
            """(targetSelector) => {
                const element = document.querySelector(targetSelector);
                const active = document.activeElement;
                const describeElement = (node) => {
                    if (!node) {
                        return null;
                    }

                    const style = window.getComputedStyle(node);
                    const rect = node.getBoundingClientRect();
                    return {
                        tag: node.tagName,
                        id: node.id || "",
                        classes: node.className || "",
                        text: (node.textContent || "").trim().slice(0, 160),
                        disabled: node.disabled === true,
                        ariaDisabled: node.getAttribute("aria-disabled") || "",
                        display: style.display,
                        visibility: style.visibility,
                        pointerEvents: style.pointerEvents,
                        width: Math.round(rect.width * 10) / 10,
                        height: Math.round(rect.height * 10) / 10,
                        top: Math.round(rect.top * 10) / 10,
                        left: Math.round(rect.left * 10) / 10
                    };
                };

                return {
                    url: window.location.pathname,
                    online: navigator.onLine === true,
                    selector: targetSelector,
                    matchCount: document.querySelectorAll(targetSelector).length,
                    target: describeElement(element),
                    active: describeElement(active),
                    role: document.querySelector("[data-turn-root]")?.getAttribute("data-role") || "",
                    network: (document.getElementById("turn-network-state")?.textContent || "").trim(),
                    localQueue: (document.getElementById("turn-local-queue-count")?.textContent || "").trim(),
                    serverQueue: (document.getElementById("turn-server-queue-count")?.textContent || "").trim(),
                    banner: (document.getElementById("mobile-client-banner-copy")?.textContent || "").trim().slice(0, 180),
                    syncSummary: (document.getElementById("turn-sync-summary")?.textContent || "").trim().slice(0, 180)
                };
            }""",
            selector,
        )
        return json.dumps(state, sort_keys=True)
    except Exception as error:
        return f"control-state-unavailable: {error}"


def click_control(page: Page, selector: str, *, require_idle_network: bool = True) -> None:
    locator = page.locator(selector)

    def wait_until_ready() -> None:
        locator.wait_for(state="visible", timeout=ACTION_TIMEOUT_MS)
        page.wait_for_function(
            """([targetSelector, idleRequired]) => {
                const element = document.querySelector(targetSelector);
                const network = (document.getElementById("turn-network-state")?.textContent || "").trim();
                return !!element
                    && element.disabled !== true
                    && (!idleRequired || network !== "Busy");
            }""",
            arg=[selector, require_idle_network],
            timeout=ACTION_TIMEOUT_MS,
        )

    try:
        wait_until_ready()
    except PlaywrightTimeoutError as error:
        raise AssertionError(f"control was not ready for click: {describe_control_state(page, selector)}") from error

    for _attempt in range(2):
        try:
            locator.click(timeout=ACTION_TIMEOUT_MS)
            return
        except PlaywrightTimeoutError:
            try:
                wait_until_ready()
            except PlaywrightTimeoutError:
                continue

    try:
        wait_until_ready()
        locator.dispatch_event("click")
    except PlaywrightTimeoutError as dispatch_error:
        raise AssertionError(f"control click did not complete: {describe_control_state(page, selector)}") from dispatch_error
    except Exception as dispatch_error:
        raise AssertionError(f"control click dispatch failed: {describe_control_state(page, selector)}") from dispatch_error


def verify_hero_mobile_launch(
    browser,
    base_url: str,
    *,
    role: str,
    device_id: str,
    expected_mode: str,
    launch_control: str = "link",
) -> dict[str, str]:
    context = browser.new_context(service_workers="allow")
    page = context.new_page()
    try:
        goto_dom(page, f"{base_url}/index.html?sessionId={HERO_SESSION}&role={role}&deviceId={device_id}")
        wait_for_play_shell_hero(page)
        hero_target = page.evaluate(
            """() => {
                const url = new URL(document.getElementById("shell-play-action-link").href, window.location.origin);
                return {
                    pathname: url.pathname,
                    sessionId: url.searchParams.get("sessionId") || "",
                    role: url.searchParams.get("role") || "",
                    deviceId: url.searchParams.get("deviceId") || ""
                };
            }"""
        )
        assert_true(hero_target["pathname"] == f"/mobile/{expected_mode}", f"hero launch must target the {expected_mode} mobile PWA route")
        assert_true(hero_target["sessionId"] == HERO_SESSION, "hero launch must preserve the current session id")
        assert_true(hero_target["role"] == role, "hero launch must preserve the current role")
        assert_true(hero_target["deviceId"] == device_id, "hero launch must preserve the active claimed-device id")

        if launch_control == "menu":
            menu_target = page.evaluate(
                """() => {
                    const menu = document.getElementById("shell-hero-action-menu");
                    const url = new URL(menu.dataset.playHref || "", window.location.origin);
                    return {
                        pathname: url.pathname,
                        sessionId: url.searchParams.get("sessionId") || "",
                        role: url.searchParams.get("role") || "",
                        deviceId: url.searchParams.get("deviceId") || ""
                    };
                }"""
            )
            assert_true(menu_target == hero_target, "hero action menu Play target must match the direct Play target")
            page.select_option("#shell-hero-action-menu", "play")
        else:
            click_control(page, "#shell-play-action-link")

        page.wait_for_url(f"**/mobile/{expected_mode}?**", timeout=NAVIGATION_TIMEOUT_MS)
        wait_for_mobile_shell(page)
        params = query_params(page)
        assert_true(params["sessionId"] == HERO_SESSION, "clicked hero launch must land in the current session")
        assert_true(params["role"] == role, "clicked hero launch must land in the current role lane")
        assert_true(params["deviceId"] == device_id, "clicked hero launch must land on the active claimed-device lane")
        return {
            "mode": expected_mode,
            "role": params["role"],
            "deviceId": params["deviceId"],
        }
    finally:
        context.close()


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


def close_page(page: Page) -> None:
    try:
        page.close()
    except Exception:  # noqa: BLE001
        pass


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
    wait_for_service_worker_network_ack(page, expected_online=False, timeout_ms=timeout_ms)


def wait_for_online_posture(page: Page, timeout_ms: int = 60_000) -> None:
    try:
        page.wait_for_function(
            "() => document.getElementById('turn-network-state')?.textContent === 'Online'",
            timeout=timeout_ms,
        )
    except PlaywrightTimeoutError:
        page.reload(wait_until="domcontentloaded", timeout=NAVIGATION_TIMEOUT_MS)
        wait_for_mobile_shell(page)
        page.wait_for_function(
            "() => document.getElementById('turn-network-state')?.textContent === 'Online'",
            timeout=timeout_ms,
        )
    wait_for_service_worker_network_ack(page, expected_online=True, timeout_ms=timeout_ms)


def wait_for_service_worker_network_ack(page: Page, *, expected_online: bool, timeout_ms: int = 60_000) -> None:
    page.wait_for_function(
        """(expectedOnline) => {
            if (!navigator.serviceWorker || !navigator.serviceWorker.controller) {
                return true;
            }

            return window.__chummerPlayServiceWorkerNetworkStateAck === expectedOnline;
        }""",
        arg=expected_online,
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


def wait_for_shell_cache(page: Page) -> None:
    page.wait_for_function(
        """async () => {
            if (!window.caches) {
                return false;
            }
            const requiredShellAssets = [
                "/mobile/player",
                "/mobile/gm",
                "/_framework/blazor.web.js",
                "/mobile.css",
                "/mobile-turn-companion.js",
                "/manifest.webmanifest",
                "/manifest.player.webmanifest",
                "/manifest.gm.webmanifest"
            ];
            const matches = await Promise.all(requiredShellAssets.map((asset) => caches.match(asset)));
            const cacheKeys = await caches.keys();
            return matches.every(Boolean)
                && cacheKeys.includes("chummer-shell-play-shell-v16")
                && !cacheKeys.includes("chummer-shell-play-shell-v15")
                && !cacheKeys.includes("chummer-shell-play-shell-v10")
                && cacheKeys.includes("foreign-origin-cache-smoke");
        }""",
        timeout=60_000,
    )


def seed_previous_shell_cache(context, base_url: str) -> None:
    seed_page = context.new_page()
    try:
        seed_page.goto(f"{base_url}/health", wait_until="domcontentloaded", timeout=30_000)
        seeded = seed_page.evaluate(
            """async ([previousCacheName, legacyCacheName, foreignCacheName]) => {
                if (!window.caches) {
                    return false;
                }
                const previousCache = await caches.open(previousCacheName);
                await previousCache.put(
                    "/mobile/player?stale-shell-cache=v10",
                    new Response("stale v10 shell", { headers: { "content-type": "text/plain" } })
                );
                const legacyCache = await caches.open(legacyCacheName);
                await legacyCache.put(
                    "/mobile/player?stale-shell-cache=v9",
                    new Response("legacy v9 shell", { headers: { "content-type": "text/plain" } })
                );
                const foreignCache = await caches.open(foreignCacheName);
                await foreignCache.put(
                    "/foreign-app-cache-marker",
                    new Response("foreign cache must survive play shell activation", { headers: { "content-type": "text/plain" } })
                );
                const cacheKeys = await caches.keys();
                return cacheKeys.includes(previousCacheName)
                    && cacheKeys.includes(legacyCacheName)
                    && cacheKeys.includes(foreignCacheName);
            }""",
            [PREVIOUS_SHELL_CACHE, LEGACY_SHELL_CACHE, FOREIGN_CACHE],
        )
        assert_true(seeded is True, "test setup must pre-seed stale managed shell caches and a foreign cache before service-worker activation")
    finally:
        seed_page.close()


def shell_cache_names(page: Page) -> list[str]:
    cache_names = page.evaluate("async () => window.caches ? await caches.keys() : []")
    assert_true(isinstance(cache_names, list), "service worker cache names must be inspectable")
    names = [str(item) for item in cache_names]
    assert_true(EXPECTED_SHELL_CACHE in names, "service worker must activate the refreshed v15 shell cache")
    assert_true(PREVIOUS_SHELL_CACHE not in names, "service worker must not keep the stale v14 shell cache after activation")
    assert_true(LEGACY_SHELL_CACHE not in names, "service worker must not keep the legacy v10 shell cache after activation")
    assert_true(FOREIGN_CACHE in names, "service worker activation must not delete unrelated origin caches")
    return names


def cached_manifest_summary(page: Page) -> dict[str, object]:
    summary = page.evaluate(
        """async () => {
            const readManifest = async (path) => {
                const response = await caches.match(path);
                if (!response) {
                    return null;
                }
                return await response.json();
            };
            const hasAdaptiveIcon = (manifest, src) => {
                const icons = Array.isArray(manifest?.icons) ? manifest.icons : [];
                return icons.some((icon) => {
                    const purpose = String(icon?.purpose || "").split(/\\s+/).filter(Boolean);
                    return icon?.src === src && purpose.includes("any") && purpose.includes("maskable");
                });
            };
            const hasShortcut = (manifest, url) => {
                const shortcuts = Array.isArray(manifest?.shortcuts) ? manifest.shortcuts : [];
                return shortcuts.some((shortcut) => shortcut?.url === url);
            };

            const player = await readManifest("/manifest.player.webmanifest");
            const gm = await readManifest("/manifest.gm.webmanifest");
            return {
                playerStartUrl: player?.start_url || "",
                gmStartUrl: gm?.start_url || "",
                playerHasSelfShortcut: hasShortcut(player, "/mobile/player?role=Player"),
                playerHasGmShortcut: hasShortcut(player, "/mobile/gm?role=GameMaster"),
                gmHasSelfShortcut: hasShortcut(gm, "/mobile/gm?role=GameMaster"),
                gmHasPlayerShortcut: hasShortcut(gm, "/mobile/player?role=Player"),
                playerHasAdaptive192: hasAdaptiveIcon(player, "/icons/icon-192.png"),
                playerHasAdaptive512: hasAdaptiveIcon(player, "/icons/icon-512.png"),
                gmHasAdaptive192: hasAdaptiveIcon(gm, "/icons/icon-192.png"),
                gmHasAdaptive512: hasAdaptiveIcon(gm, "/icons/icon-512.png")
            };
        }"""
    )
    assert_true(isinstance(summary, dict), "cached manifest summary must be inspectable")
    assert_true(summary.get("playerStartUrl") == "/mobile/player?role=Player", "cached player manifest must preserve the direct player start_url")
    assert_true(summary.get("gmStartUrl") == "/mobile/gm?role=GameMaster", "cached GM manifest must preserve the direct GM start_url")
    assert_true(summary.get("playerHasSelfShortcut") is True, "cached player manifest must keep the player self-launch shortcut")
    assert_true(summary.get("playerHasGmShortcut") is True, "cached player manifest must keep the GM switch shortcut")
    assert_true(summary.get("gmHasSelfShortcut") is True, "cached GM manifest must keep the GM self-launch shortcut")
    assert_true(summary.get("gmHasPlayerShortcut") is True, "cached GM manifest must keep the player switch shortcut")
    assert_true(summary.get("playerHasAdaptive192") is True, "cached player manifest must keep the adaptive 192px icon")
    assert_true(summary.get("playerHasAdaptive512") is True, "cached player manifest must keep the adaptive 512px icon")
    assert_true(summary.get("gmHasAdaptive192") is True, "cached GM manifest must keep the adaptive 192px icon")
    assert_true(summary.get("gmHasAdaptive512") is True, "cached GM manifest must keep the adaptive 512px icon")
    return summary


def wait_for_query(page: Page, *, session_id: str, role: str) -> None:
    page.wait_for_function(
        """([expectedSessionId, expectedRole]) => {
            const params = new URLSearchParams(window.location.search);
            return params.get("sessionId") === expectedSessionId && params.get("role") === expectedRole && !!params.get("deviceId");
        }""",
        arg=[session_id, role],
        timeout=60_000,
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


def verify_private_api_boundary(page: Page, *, session_id: str, role: str, device_id: str) -> dict[str, object]:
    return page.evaluate(
        """async ([targetSessionId, targetRole, targetDeviceId]) => {
            const apiPath = "/api/play/turn-companion/" + encodeURIComponent(targetSessionId)
                + "?role=" + encodeURIComponent(targetRole)
                + "&deviceId=" + encodeURIComponent(targetDeviceId);
            const response = await fetch(apiPath, { cache: "no-store" });
            let body = {};
            try {
                body = await response.json();
            } catch {
                body = {};
            }

            return {
                status: response.status,
                cacheControl: response.headers.get("cache-control") || "",
                contentType: response.headers.get("content-type") || "",
                error: body && typeof body.error === "string" ? body.error : "",
                shellSummary: body && typeof body.shellSummary === "string" ? body.shellSummary : ""
            };
        }""",
        arg=[session_id, role, device_id],
    )


def start_server() -> tuple[subprocess.Popen[str], str, str]:
    port = free_port()
    base_url = f"http://127.0.0.1:{port}"
    state_dir = tempfile.mkdtemp(prefix="chummer-play-runtime-smoke-")
    log_file = tempfile.NamedTemporaryFile(
        prefix="chummer-play-runtime-smoke-",
        suffix=".log",
        mode="w+",
        encoding="utf-8",
        delete=False,
    )
    env = os.environ.copy()
    env["CHUMMER_PLAY_BROWSER_STATE_DIR"] = state_dir
    env["CHUMMER_PLAY_BROWSER_STATE_ISOLATE_PER_APP"] = "true"
    env["ASPNETCORE_URLS"] = base_url
    env["ASPNETCORE_ENVIRONMENT"] = "Development"

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
        stdout=log_file,
        stderr=subprocess.STDOUT,
        text=True,
        start_new_session=True,
    )
    setattr(process, "_chummer_log_path", log_file.name)
    setattr(process, "_chummer_log_file", log_file)
    setattr(process, "_chummer_state_dir", state_dir)
    try:
        wait_for_health(base_url)
    except Exception:
        stop_server(process)
        cleanup_server_artifacts(process)
        raise
    return process, base_url, state_dir


def stop_server(process: subprocess.Popen[str]) -> None:
    def close_log() -> None:
        log_file = getattr(process, "_chummer_log_file", None)
        if log_file is not None and not log_file.closed:
            log_file.close()

    if process.poll() is not None:
        close_log()
        return

    try:
        os.killpg(process.pid, signal.SIGTERM)
    except ProcessLookupError:
        close_log()
        return
    except PermissionError:
        process.terminate()

    try:
        process.wait(timeout=20)
    except subprocess.TimeoutExpired:
        try:
            os.killpg(process.pid, signal.SIGKILL)
        except ProcessLookupError:
            pass
        except PermissionError:
            process.kill()
        process.wait(timeout=20)
    finally:
        close_log()


def cleanup_server_artifacts(process: subprocess.Popen[str]) -> None:
    state_dir = getattr(process, "_chummer_state_dir", "")
    if state_dir:
        shutil.rmtree(state_dir, ignore_errors=True)

    log_path = getattr(process, "_chummer_log_path", "")
    if log_path:
        Path(log_path).unlink(missing_ok=True)


def read_server_log_tail(process: subprocess.Popen[str], limit: int = 4000) -> str:
    log_file = getattr(process, "_chummer_log_file", None)
    if log_file is not None and not log_file.closed:
        try:
            log_file.flush()
        except Exception:
            pass

    log_path = getattr(process, "_chummer_log_path", "")
    if not log_path:
        return ""
    try:
        return Path(log_path).read_text(encoding="utf-8")[-limit:]
    except Exception:
        return ""


def write_runtime_receipt(payload: dict[str, object]) -> None:
    RECEIPT_PATH.parent.mkdir(parents=True, exist_ok=True)
    RECEIPT_PATH.write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def main() -> int:
    process: subprocess.Popen[str] | None = None
    browser = None
    current_step = "boot"
    current_page: Page | None = None
    cache_names: list[str] = []
    interaction_hits = "?"
    resumed_runsite_summary = ""
    gm_resume_summary = ""
    try:
        process, base_url, _state_dir = start_server()
        with sync_playwright() as playwright:
            browser = playwright.chromium.launch(headless=True)

            current_step = "hero player mobile launch"
            hero_player_launch = verify_hero_mobile_launch(
                browser,
                base_url,
                role=PLAYER_ROLE,
                device_id=HERO_PLAYER_DEVICE,
                expected_mode="player",
            )

            current_step = "hero GM mobile launch"
            hero_gm_launch = verify_hero_mobile_launch(
                browser,
                base_url,
                role=GM_ROLE,
                device_id=HERO_GM_DEVICE,
                expected_mode="gm",
            )

            current_step = "hero menu player mobile launch"
            hero_menu_player_launch = verify_hero_mobile_launch(
                browser,
                base_url,
                role=PLAYER_ROLE,
                device_id=HERO_MENU_PLAYER_DEVICE,
                expected_mode="player",
                launch_control="menu",
            )

            current_step = "hero menu GM mobile launch"
            hero_menu_gm_launch = verify_hero_mobile_launch(
                browser,
                base_url,
                role=GM_ROLE,
                device_id=HERO_MENU_GM_DEVICE,
                expected_mode="gm",
                launch_control="menu",
            )

            context = browser.new_context(service_workers="allow")
            current_step = "preseed stale service-worker cache"
            seed_previous_shell_cache(context, base_url)

            current_step = "open explicit player lane"
            player_page = context.new_page()
            current_page = player_page
            goto_dom(player_page, f"{base_url}/mobile?sessionId={PLAYER_SESSION}&role={PLAYER_ROLE}&deviceId={PLAYER_DEVICE}")
            wait_for_mobile_shell(player_page)
            player_page.wait_for_function(
                "() => document.getElementById('turn-continuity-device')?.textContent?.length > 0"
            )
            current_step = "activate service worker on explicit player lane"
            wait_for_service_worker_control(player_page)
            player_page.reload(wait_until="domcontentloaded")
            wait_for_mobile_shell(player_page)
            wait_for_service_worker_control(player_page)
            wait_for_shell_cache(player_page)
            cache_names = shell_cache_names(player_page)
            cached_manifest_summary(player_page)

            current_step = "assert player local storage"
            player_last_route = parse_local_storage_json(player_page, LAST_ROUTE_KEY)
            assert_true(player_last_route.get("sessionId") == PLAYER_SESSION, "global last route must persist the player session")
            assert_true(player_last_route.get("roleName") == PLAYER_ROLE, "global last route must persist the player role")
            assert_true(player_last_route.get("deviceId") == PLAYER_DEVICE, "global last route must persist the explicit player device")

            player_role_route = parse_local_storage_json(player_page, PLAYER_ROUTE_KEY)
            assert_true(player_role_route.get("deviceId") == PLAYER_DEVICE, "player role route must persist the explicit player device")

            current_step = "player live interactions"
            wait_for_online_posture(player_page)
            player_page.select_option("#runsite-anchor", "server-room")
            wait_for_text(player_page, "#turn-runsite-summary", "Server Room")
            wait_for_exact_text(player_page, "#turn-local-queue-count", "1")

            click_control(player_page, "button[data-turn-kind='adjust-metric'][data-metric-id='ammo'][data-delta='-1']")
            wait_for_text(player_page, "#turn-weapon-label", "magazine 11")
            wait_for_exact_text(player_page, "#turn-local-queue-count", "2")

            player_page.fill("#manual-hits", "3")
            click_control(player_page, "button[data-turn-kind='resolve-manual']")
            wait_for_text(player_page, "#turn-last-outcome", "3 hit(s)")
            wait_for_text(player_page, "#turn-last-outcome", "Magazine 11 -> 8")
            interaction_outcome = text_content(player_page, "#turn-last-outcome")
            interaction_hits_match = re.search(r"(\d+) hit\(s\)", interaction_outcome)
            interaction_hits = interaction_hits_match.group(1) if interaction_hits_match else "?"
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

            current_step = "player lifecycle persistence"
            player_snapshot_key = turn_snapshot_key(PLAYER_SESSION, PLAYER_ROLE, PLAYER_DEVICE)
            lifecycle_persisted = player_page.evaluate(
                """([snapshotKey, routeKey]) => {
                    window.localStorage.removeItem(snapshotKey);
                    window.localStorage.removeItem(routeKey);
                    window.dispatchEvent(new Event("pagehide"));
                    return {
                        snapshot: window.localStorage.getItem(snapshotKey) || "",
                        roleRoute: window.localStorage.getItem(routeKey) || ""
                    };
                }""",
                arg=[player_snapshot_key, PLAYER_ROUTE_KEY],
            )
            assert_true(isinstance(lifecycle_persisted, dict), "pagehide persistence result must be inspectable")
            lifecycle_snapshot = json.loads(str(lifecycle_persisted.get("snapshot") or "{}"))
            lifecycle_role_route = json.loads(str(lifecycle_persisted.get("roleRoute") or "{}"))
            assert_true(find_stat_value(lifecycle_snapshot, "ammo") == 8, "pagehide persistence must rewrite the latest player snapshot")
            assert_true(lifecycle_role_route.get("deviceId") == PLAYER_DEVICE, "pagehide persistence must rewrite the player role-specific relaunch lane")

            current_step = "player replay and acknowledgement"
            click_control(player_page, "#turn-replay-local-button")
            wait_for_exact_text(player_page, "#turn-local-queue-count", "0")
            wait_for_exact_text(player_page, "#turn-server-queue-count", "3")

            click_control(player_page, "#turn-ack-server-button")
            wait_for_exact_text(player_page, "#turn-server-queue-count", "0")

            acknowledged_snapshot = parse_local_storage_json(
                player_page,
                turn_snapshot_key(PLAYER_SESSION, PLAYER_ROLE, PLAYER_DEVICE),
            )
            acknowledged_queue = acknowledged_snapshot.get("localReplayQueue")
            assert_true(isinstance(acknowledged_queue, list) and len(acknowledged_queue) == 0, "player snapshot must clear the local replay queue after replay succeeds")
            close_page(player_page)

            current_step = "generic resume"
            generic_page = context.new_page()
            current_page = generic_page
            goto_dom(generic_page, f"{base_url}/mobile")
            wait_for_mobile_shell(generic_page)
            wait_for_query(generic_page, session_id=PLAYER_SESSION, role=PLAYER_ROLE)
            generic_params = query_params(generic_page)
            assert_true(generic_params["deviceId"] == PLAYER_DEVICE, "generic mobile launch must resume the last claimed player lane")
            wait_for_text(generic_page, "#turn-runsite-summary", "Server Room")
            resumed_runsite_summary = text_content(generic_page, "#turn-runsite-summary")
            wait_for_text(generic_page, "#turn-last-outcome", "3 hit(s)")
            wait_for_exact_text(generic_page, "#turn-local-queue-count", "0")
            wait_for_text(generic_page, "#turn-history-list", "resolved")
            close_page(generic_page)

            current_step = "path-only GM resume"
            path_only_gm_page = context.new_page()
            current_page = path_only_gm_page
            goto_dom(path_only_gm_page, f"{base_url}/mobile/gm")
            wait_for_mobile_shell(path_only_gm_page)
            wait_for_query(path_only_gm_page, session_id=PLAYER_SESSION, role=GM_ROLE)
            path_only_gm_params = query_params(path_only_gm_page)
            assert_true(path_only_gm_params["deviceId"] != PLAYER_DEVICE, "path-only GM launch must not reuse the player claimed-device lane")
            path_only_gm_role_route = parse_local_storage_json(path_only_gm_page, GM_ROUTE_KEY)
            assert_true(path_only_gm_role_route.get("sessionId") == PLAYER_SESSION, "path-only GM launch must preserve the last live session")
            assert_true(path_only_gm_role_route.get("roleName") == GM_ROLE, "path-only GM launch must persist a GM role route")
            assert_true(path_only_gm_role_route.get("deviceId") == path_only_gm_params["deviceId"], "path-only GM launch must persist its GM-specific device lane")
            close_page(path_only_gm_page)

            current_step = "player to GM role switch keeps device lanes isolated"
            role_switch_page = context.new_page()
            current_page = role_switch_page
            goto_dom(role_switch_page, f"{base_url}/mobile/player?sessionId={PLAYER_SESSION}&role={PLAYER_ROLE}&deviceId={PLAYER_DEVICE}")
            wait_for_mobile_shell(role_switch_page)
            role_switch_target = role_switch_page.evaluate(
                """() => {
                    const link = document.querySelector("[data-role-name='GameMaster']");
                    const url = new URL(link?.getAttribute("href") || "", window.location.origin);
                    return {
                        pathname: url.pathname,
                        sessionId: url.searchParams.get("sessionId") || "",
                        role: url.searchParams.get("role") || "",
                        hasDeviceId: url.searchParams.has("deviceId"),
                        deviceId: url.searchParams.get("deviceId") || ""
                    };
                }"""
            )
            assert_true(role_switch_target["pathname"] == "/mobile/gm", "player role switch must target the GM mobile PWA route")
            assert_true(role_switch_target["sessionId"] == PLAYER_SESSION, "player role switch must preserve the live session")
            assert_true(role_switch_target["role"] == GM_ROLE, "player role switch must target the GM role")
            if role_switch_target["hasDeviceId"]:
                assert_true(role_switch_target["deviceId"] == path_only_gm_params["deviceId"], "player role switch may carry only the known GM-specific device lane")
            assert_true(role_switch_target["deviceId"] != PLAYER_DEVICE, "player role switch target must not expose the player device id")

            click_control(role_switch_page, "[data-role-name='GameMaster']", require_idle_network=False)
            role_switch_page.wait_for_url("**/mobile/gm?**", timeout=60_000)
            wait_for_mobile_shell(role_switch_page)
            wait_for_query(role_switch_page, session_id=PLAYER_SESSION, role=GM_ROLE)
            role_switch_params = query_params(role_switch_page)
            assert_true(role_switch_params["deviceId"] != PLAYER_DEVICE, "GM role switch must mint or reuse a GM-specific device lane")
            assert_true(role_switch_params["deviceId"] == path_only_gm_params["deviceId"], "GM role switch must reuse the path-only GM device lane once it exists")
            close_page(role_switch_page)

            current_step = "gm shortcut session resume"
            gm_first_page = context.new_page()
            current_page = gm_first_page
            goto_dom(gm_first_page, f"{base_url}/mobile?role={GM_ROLE}")
            wait_for_mobile_shell(gm_first_page)
            wait_for_query(gm_first_page, session_id=PLAYER_SESSION, role=GM_ROLE)
            gm_first_params = query_params(gm_first_page)
            assert_true(gm_first_params["sessionId"] == PLAYER_SESSION, "GM shortcut must resume the last live session")
            assert_true(gm_first_params["role"] == GM_ROLE, "GM shortcut must stay in the GM lane")
            assert_true(gm_first_params["deviceId"] != "", "GM shortcut must land on a claimed device lane")
            assert_true(gm_first_params["deviceId"] == role_switch_params["deviceId"], "GM shortcut must reuse the role-specific lane created by role switch")

            gm_role_route = parse_local_storage_json(gm_first_page, GM_ROUTE_KEY)
            assert_true(gm_role_route.get("sessionId") == PLAYER_SESSION, "GM role route must persist the resumed session")
            assert_true(gm_role_route.get("roleName") == GM_ROLE, "GM role route must persist the GM role")
            assert_true(gm_role_route.get("deviceId") == gm_first_params["deviceId"], "GM role route must persist the resolved GM device lane")
            close_page(gm_first_page)

            current_step = "gm shortcut claimed-device resume"
            gm_second_page = context.new_page()
            current_page = gm_second_page
            goto_dom(gm_second_page, f"{base_url}/mobile?role={GM_ROLE}")
            wait_for_mobile_shell(gm_second_page)
            wait_for_query(gm_second_page, session_id=PLAYER_SESSION, role=GM_ROLE)
            gm_second_params = query_params(gm_second_page)
            assert_true(gm_second_params["deviceId"] == gm_first_params["deviceId"], "GM shortcut must resume the last GM claimed-device lane after it exists")

            current_step = "gm live interactions"
            wait_for_online_posture(gm_second_page)
            wait_for_text(gm_second_page, "#turn-actor-label", "GM focus actor")
            click_control(gm_second_page, "button[data-turn-kind='select-action'][data-action-id='reveal-threat']")
            wait_for_text(gm_second_page, "#turn-resolve-label", "Reveal Threat")
            gm_second_page.select_option("#runsite-anchor", "fire-stairs")
            wait_for_text(gm_second_page, "#turn-runsite-summary", "Fire Stairs")
            click_control(gm_second_page, "button[data-turn-kind='queue-quick-action'][data-action-id='gm-advance-initiative']")
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

            click_control(gm_second_page, "#turn-replay-local-button")
            wait_for_exact_text(gm_second_page, "#turn-local-queue-count", "0")
            wait_for_exact_text(gm_second_page, "#turn-server-queue-count", "3")

            click_control(gm_second_page, "#turn-ack-server-button")
            wait_for_exact_text(gm_second_page, "#turn-server-queue-count", "0")

            gm_ack_snapshot = parse_local_storage_json(
                gm_second_page,
                turn_snapshot_key(PLAYER_SESSION, GM_ROLE, gm_second_params["deviceId"]),
            )
            gm_ack_queue = gm_ack_snapshot.get("localReplayQueue")
            assert_true(isinstance(gm_ack_queue, list) and len(gm_ack_queue) == 0, "gm snapshot must clear the local replay queue after replay succeeds")

            current_step = "prepare controlled offline reopen shell"
            offline_page = context.new_page()
            current_page = offline_page
            goto_dom(offline_page, f"{base_url}/mobile")
            wait_for_mobile_shell(offline_page)
            wait_for_query(offline_page, session_id=PLAYER_SESSION, role=GM_ROLE)
            wait_for_text(offline_page, "#turn-runsite-summary", "Fire Stairs")
            wait_for_text(offline_page, "#turn-resolve-label", "Reveal Threat")
            wait_for_service_worker_control(offline_page)
            online_private_api = verify_private_api_boundary(
                offline_page,
                session_id=PLAYER_SESSION,
                role=GM_ROLE,
                device_id=gm_second_params["deviceId"],
            )
            assert_true(online_private_api["status"] == 200, "online private play API must be reachable before the offline boundary check")
            assert_true(
                "private, no-store" in str(online_private_api["cacheControl"]),
                "online private play API responses must carry private no-store headers",
            )
            assert_true(
                str(online_private_api["shellSummary"]).strip() != "",
                "online private play API must return a live projection before proving it is not replayed offline",
            )

            current_step = "offline reopen"
            wait_for_shell_cache(gm_second_page)
            context.set_offline(True)
            wait_for_offline_posture(offline_page)
            offline_private_api = verify_private_api_boundary(
                offline_page,
                session_id=PLAYER_SESSION,
                role=GM_ROLE,
                device_id=gm_second_params["deviceId"],
            )
            assert_true(offline_private_api["status"] == 503, "offline private play API must fail closed instead of replaying a cached projection")
            assert_true(
                offline_private_api["error"] == "play_api_network_unavailable",
                "offline private play API must return the typed service-worker network-unavailable error",
            )
            assert_true(
                "no-store" in str(offline_private_api["cacheControl"]),
                "offline private play API failure must be non-cacheable",
            )
            assert_true(
                str(offline_private_api["shellSummary"]).strip() == "",
                "offline private play API failure must not include cached projection data",
            )
            time.sleep(0.25)
            offline_page.reload(wait_until="domcontentloaded")
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
            gm_resume_summary = text_content(offline_page, "#turn-runsite-summary")
            wait_for_text(offline_page, "#turn-resolve-label", "Reveal Threat")
            wait_for_exact_text(offline_page, "#turn-local-queue-count", "0")
            close_page(offline_page)

            current_step = "offline fresh player PWA launch"
            offline_player_launch = context.new_page()
            current_page = offline_player_launch
            goto_dom(offline_player_launch, f"{base_url}/mobile/player?role={PLAYER_ROLE}")
            wait_for_mobile_shell(offline_player_launch)
            wait_for_offline_posture(offline_player_launch)
            wait_for_query(offline_player_launch, session_id=PLAYER_SESSION, role=PLAYER_ROLE)
            offline_player_params = query_params(offline_player_launch)
            assert_true(
                offline_player_params["deviceId"] == PLAYER_DEVICE,
                "offline player PWA launch must resume the last player claimed-device lane",
            )
            wait_for_text(offline_player_launch, "#turn-runsite-summary", "Server Room")
            wait_for_text(offline_player_launch, "#turn-last-outcome", "3 hit(s)")

            current_step = "offline player local queue replay after reconnect"
            click_control(offline_player_launch, "button[data-turn-kind='adjust-metric'][data-metric-id='ammo'][data-delta='-1']")
            wait_for_text(offline_player_launch, "#turn-weapon-label", "magazine 7")
            wait_for_exact_text(offline_player_launch, "#turn-local-queue-count", "1")
            wait_for_text(offline_player_launch, "#mobile-client-banner-copy", "1 local receipt")
            offline_player_snapshot = parse_local_storage_json(
                offline_player_launch,
                turn_snapshot_key(PLAYER_SESSION, PLAYER_ROLE, PLAYER_DEVICE),
            )
            offline_player_queue = offline_player_snapshot.get("localReplayQueue")
            assert_true(
                isinstance(offline_player_queue, list) and len(offline_player_queue) == 1,
                "offline player snapshot must persist the locally staged receipt before reconnect",
            )
            assert_true(
                find_stat_value(offline_player_snapshot, "ammo") == 7,
                "offline player snapshot must persist the locally staged tracker mutation",
            )

            context.set_offline(False)
            wait_for_online_posture(offline_player_launch)
            click_control(offline_player_launch, "#turn-replay-local-button")
            wait_for_exact_text(offline_player_launch, "#turn-local-queue-count", "0")
            wait_for_exact_text(offline_player_launch, "#turn-server-queue-count", "1")
            click_control(offline_player_launch, "#turn-ack-server-button")
            wait_for_exact_text(offline_player_launch, "#turn-server-queue-count", "0")
            offline_player_replayed_snapshot = parse_local_storage_json(
                offline_player_launch,
                turn_snapshot_key(PLAYER_SESSION, PLAYER_ROLE, PLAYER_DEVICE),
            )
            offline_player_replayed_queue = offline_player_replayed_snapshot.get("localReplayQueue")
            assert_true(
                isinstance(offline_player_replayed_queue, list) and len(offline_player_replayed_queue) == 0,
                "offline player snapshot must clear the locally staged receipt after reconnect replay succeeds",
            )
            context.set_offline(True)
            close_page(offline_player_launch)

            current_step = "offline fresh GM PWA launch"
            offline_gm_launch = context.new_page()
            current_page = offline_gm_launch
            goto_dom(offline_gm_launch, f"{base_url}/mobile/gm?role={GM_ROLE}")
            wait_for_mobile_shell(offline_gm_launch)
            wait_for_offline_posture(offline_gm_launch)
            wait_for_query(offline_gm_launch, session_id=PLAYER_SESSION, role=GM_ROLE)
            offline_gm_params = query_params(offline_gm_launch)
            assert_true(
                offline_gm_params["deviceId"] == gm_second_params["deviceId"],
                "offline GM PWA launch must resume the last GM claimed-device lane",
            )
            wait_for_text(offline_gm_launch, "#turn-runsite-summary", "Fire Stairs")
            wait_for_text(offline_gm_launch, "#turn-resolve-label", "Reveal Threat")

            current_step = "offline GM local queue replay after reconnect"
            click_control(offline_gm_launch, "button[data-turn-kind='queue-quick-action'][data-action-id='gm-advance-initiative']")
            wait_for_exact_text(offline_gm_launch, "#turn-local-queue-count", "1")
            wait_for_text(offline_gm_launch, "#turn-history-list", "Quick action queued")
            wait_for_text(offline_gm_launch, "#mobile-client-banner-copy", "1 local receipt")
            offline_gm_snapshot = parse_local_storage_json(
                offline_gm_launch,
                turn_snapshot_key(PLAYER_SESSION, GM_ROLE, offline_gm_params["deviceId"]),
            )
            offline_gm_queue = offline_gm_snapshot.get("localReplayQueue")
            assert_true(
                isinstance(offline_gm_queue, list) and len(offline_gm_queue) == 1,
                "offline GM snapshot must persist the locally staged receipt before reconnect",
            )

            context.set_offline(False)
            wait_for_online_posture(offline_gm_launch)
            click_control(offline_gm_launch, "#turn-replay-local-button")
            wait_for_exact_text(offline_gm_launch, "#turn-local-queue-count", "0")
            wait_for_exact_text(offline_gm_launch, "#turn-server-queue-count", "1")
            click_control(offline_gm_launch, "#turn-ack-server-button")
            wait_for_exact_text(offline_gm_launch, "#turn-server-queue-count", "0")
            offline_gm_replayed_snapshot = parse_local_storage_json(
                offline_gm_launch,
                turn_snapshot_key(PLAYER_SESSION, GM_ROLE, offline_gm_params["deviceId"]),
            )
            offline_gm_replayed_queue = offline_gm_replayed_snapshot.get("localReplayQueue")
            assert_true(
                isinstance(offline_gm_replayed_queue, list) and len(offline_gm_replayed_queue) == 0,
                "offline GM snapshot must clear the locally staged receipt after reconnect replay succeeds",
            )
            close_page(offline_gm_launch)

            current_step = "GM to player role switch keeps device lanes isolated"
            gm_second_page.bring_to_front()
            reverse_role_switch_target = gm_second_page.evaluate(
                """() => {
                    const link = document.querySelector("[data-role-name='Player']");
                    const url = new URL(link?.getAttribute("href") || "", window.location.origin);
                    return {
                        pathname: url.pathname,
                        sessionId: url.searchParams.get("sessionId") || "",
                        role: url.searchParams.get("role") || "",
                        hasDeviceId: url.searchParams.has("deviceId"),
                        deviceId: url.searchParams.get("deviceId") || ""
                    };
                }"""
            )
            assert_true(reverse_role_switch_target["pathname"] == "/mobile/player", "GM role switch must target the player mobile PWA route")
            assert_true(reverse_role_switch_target["sessionId"] == PLAYER_SESSION, "GM role switch must preserve the live session")
            assert_true(reverse_role_switch_target["role"] == PLAYER_ROLE, "GM role switch must target the player role")
            if reverse_role_switch_target["hasDeviceId"]:
                assert_true(reverse_role_switch_target["deviceId"] == PLAYER_DEVICE, "GM role switch may carry only the known player-specific device lane")
            assert_true(reverse_role_switch_target["deviceId"] != gm_second_params["deviceId"], "GM role switch target must not expose the GM device id")

            click_control(gm_second_page, "[data-role-name='Player']", require_idle_network=False)
            gm_second_page.wait_for_url("**/mobile/player?**", timeout=60_000)
            wait_for_mobile_shell(gm_second_page)
            wait_for_query(gm_second_page, session_id=PLAYER_SESSION, role=PLAYER_ROLE)
            reverse_role_switch_params = query_params(gm_second_page)
            assert_true(reverse_role_switch_params["deviceId"] == PLAYER_DEVICE, "player role switch must reuse the existing player-specific device lane")
            wait_for_text(gm_second_page, "#turn-runsite-summary", "Server Room")
            wait_for_text(gm_second_page, "#turn-last-outcome", "3 hit(s)")

            current_step = "offline device-neutral handoff opens receiver lanes"
            offline_handoff_session = "session-offline-handoff"
            gm_second_page.evaluate(
                """(sessionId) => {
                    window.localStorage.removeItem("chummer-play-mobile-device-id:player");
                    window.localStorage.removeItem("chummer-play-mobile-device-id:gm");
                    window.localStorage.removeItem(`chummer-play-mobile-handoff-device-id:${sessionId}:player`);
                    window.localStorage.removeItem(`chummer-play-mobile-handoff-device-id:${sessionId}:gm`);
                }""",
                arg=offline_handoff_session,
            )
            context.set_offline(True)
            offline_player_handoff = context.new_page()
            current_page = offline_player_handoff
            goto_dom(offline_player_handoff, f"{base_url}/mobile/player?sessionId={offline_handoff_session}&role={PLAYER_ROLE}")
            wait_for_mobile_shell(offline_player_handoff)
            wait_for_offline_posture(offline_player_handoff)
            wait_for_query(offline_player_handoff, session_id=offline_handoff_session, role=PLAYER_ROLE)
            offline_player_handoff_params = query_params(offline_player_handoff)
            assert_true(
                offline_player_handoff_params["deviceId"] != PLAYER_DEVICE,
                "offline player handoff receiver must not reuse the sender player claimed-device id",
            )
            assert_true(
                offline_player_handoff_params["deviceId"] != gm_second_params["deviceId"],
                "offline player handoff receiver must not reuse the GM claimed-device id",
            )
            wait_for_text(offline_player_handoff, "#mobile-client-banner-title", "Offline")

            offline_gm_handoff = context.new_page()
            current_page = offline_gm_handoff
            goto_dom(offline_gm_handoff, f"{base_url}/mobile/gm?sessionId={offline_handoff_session}&role={GM_ROLE}")
            wait_for_mobile_shell(offline_gm_handoff)
            wait_for_offline_posture(offline_gm_handoff)
            wait_for_query(offline_gm_handoff, session_id=offline_handoff_session, role=GM_ROLE)
            offline_gm_handoff_params = query_params(offline_gm_handoff)
            assert_true(
                offline_gm_handoff_params["deviceId"] != gm_second_params["deviceId"],
                "offline GM handoff receiver must not reuse the sender GM claimed-device id",
            )
            assert_true(
                offline_gm_handoff_params["deviceId"] != PLAYER_DEVICE,
                "offline GM handoff receiver must not reuse the player claimed-device id",
            )
            wait_for_text(offline_gm_handoff, "#mobile-client-banner-title", "Offline")
            context.set_offline(False)
            close_page(offline_player_handoff)
            close_page(offline_gm_handoff)
            close_page(gm_second_page)

            browser.close()

        write_runtime_receipt(
            {
                "contract_name": "chummer_play.mobile_pwa_runtime_smoke.v1",
                "status": "pass",
                "generated_at_utc": datetime.now(UTC).replace(microsecond=0).isoformat().replace("+00:00", "Z"),
                "service_worker_controlled": True,
                "blazor_shell": "interactive-server",
                "blazor_boot_script": "/_framework/blazor.web.js",
                "service_worker_cache": EXPECTED_SHELL_CACHE,
                "normalized_role_fallbacks_cached": ["player", "gm"],
                "cached_manifest_start_urls": ["player", "gm"],
                "cached_manifest_shortcuts": ["player", "gm"],
                "cached_manifest_icon_purpose": "any maskable",
                "stale_cache_cleanup": {
                    PREVIOUS_SHELL_CACHE: "removed",
                    LEGACY_SHELL_CACHE: "removed",
                    FOREIGN_CACHE: "preserved",
                },
                "hero_launches": {
                    "player": hero_player_launch,
                    "gm": hero_gm_launch,
                    "menu_player": hero_menu_player_launch,
                    "menu_gm": hero_menu_gm_launch,
                },
                "player_interactions": {
                    "runsite_anchor": "server-room",
                    "ammo_after_manual_resolve": 8,
                    "manual_hits": interaction_hits,
                    "lifecycle_persisted_device": redact_device_id(lifecycle_role_route["deviceId"]),
                    "replay_ack": "local 3->0 / server 0->3->0",
                    "resume_snapshot": resumed_runsite_summary,
                },
                "gm_interactions": {
                    "runsite_anchor": "fire-stairs",
                    "selected_action": "reveal-threat",
                    "replay_ack": "local 3->0 / server 0->3->0",
                    "resume_snapshot": gm_resume_summary,
                },
                "role_switch_device_isolation": {
                    "player_device": redact_device_id(PLAYER_DEVICE),
                    "gm_device": redact_device_id(role_switch_params["deviceId"]),
                    "reverse_player_device": redact_device_id(reverse_role_switch_params["deviceId"]),
                },
                "offline": {
                    "reopen": {
                        "sessionId": offline_params["sessionId"],
                        "role": offline_params["role"],
                        "deviceId": redact_device_id(offline_params["deviceId"]),
                    },
                    "fresh_launch": {
                        "player_device": redact_device_id(offline_player_params["deviceId"]),
                        "gm_device": redact_device_id(offline_gm_params["deviceId"]),
                    },
                    "player_queue_replay": "local 1->0 / server 0->1->0 / ammo 8->7",
                    "gm_queue_replay": "local 1->0 / server 0->1->0 / gm-advance-initiative",
                    "handoff_receivers": {
                        "device_neutral": True,
                        "player_device": redact_device_id(offline_player_handoff_params["deviceId"]),
                        "gm_device": redact_device_id(offline_gm_handoff_params["deviceId"]),
                    },
                },
                "private_api_boundary": {
                    "online_status": online_private_api["status"],
                    "online_cache_control": online_private_api["cacheControl"],
                    "offline_status": offline_private_api["status"],
                    "offline_error": offline_private_api["error"],
                    "offline_cache_control": offline_private_api["cacheControl"],
                },
                "cache_names": cache_names,
            }
        )

        print("mobile_pwa_runtime_smoke ok")
        print(f"  service_worker_controlled: true")
        print("  blazor_shell: interactive-server")
        print("  blazor_boot_script: /_framework/blazor.web.js")
        print(f"  service_worker_cache: {EXPECTED_SHELL_CACHE}")
        print("  normalized_role_fallbacks_cached: player / gm")
        print("  cached_manifest_start_urls: player / gm")
        print("  cached_manifest_shortcuts: player / gm")
        print("  cached_manifest_icon_purpose: any maskable")
        print(f"  stale_cache_cleanup: {PREVIOUS_SHELL_CACHE} -> removed")
        print(f"  legacy_cache_cleanup: {LEGACY_SHELL_CACHE} -> removed")
        print(f"  foreign_cache_preserved: {FOREIGN_CACHE}")
        print(f"  hero_player_launch: /mobile/{hero_player_launch['mode']} / {hero_player_launch['deviceId']}")
        print(f"  hero_gm_launch: /mobile/{hero_gm_launch['mode']} / {hero_gm_launch['deviceId']}")
        print(f"  hero_menu_player_launch: /mobile/{hero_menu_player_launch['mode']} / {hero_menu_player_launch['deviceId']}")
        print(f"  hero_menu_gm_launch: /mobile/{hero_menu_gm_launch['mode']} / {hero_menu_gm_launch['deviceId']}")
        print(f"  player_interactions: server-room / ammo 8 / manual {interaction_hits} hit(s)")
        print(f"  lifecycle_persisted: pagehide / player {redact_device_id(lifecycle_role_route['deviceId'])}")
        print(f"  replay_ack: local 3->0 / server 0->3->0")
        print(f"  player_resume_snapshot: {resumed_runsite_summary}")
        print(f"  path_gm_resume: {path_only_gm_params['sessionId']} / {path_only_gm_params['role']} / {redact_device_id(path_only_gm_params['deviceId'])}")
        print(f"  role_switch_device_isolated: player {redact_device_id(PLAYER_DEVICE)} / gm {redact_device_id(role_switch_params['deviceId'])}")
        print(f"  reverse_role_switch_device_isolated: gm {redact_device_id(gm_second_params['deviceId'])} / player {redact_device_id(reverse_role_switch_params['deviceId'])}")
        print(f"  gm_interactions: fire-stairs / reveal-threat / local 3->0 / server 0->3->0")
        print(f"  gm_resume_snapshot: {gm_resume_summary}")
        print(f"  generic_resume: {PLAYER_SESSION} / {PLAYER_ROLE} / {PLAYER_DEVICE}")
        print(f"  gm_resume: {PLAYER_SESSION} / {GM_ROLE} / {redact_device_id(gm_second_params['deviceId'])}")
        print(f"  offline_reopen: {offline_params['sessionId']} / {offline_params['role']} / {redact_device_id(offline_params['deviceId'])}")
        print(f"  offline_fresh_launch: player {redact_device_id(offline_player_params['deviceId'])} / gm {redact_device_id(offline_gm_params['deviceId'])}")
        print("  offline_player_queue_replay: local 1->0 / server 0->1->0 / ammo 8->7")
        print("  offline_gm_queue_replay: local 1->0 / server 0->1->0 / gm-advance-initiative")
        print(f"  offline_handoff_receiver: player {redact_device_id(offline_player_handoff_params['deviceId'])} / gm {redact_device_id(offline_gm_handoff_params['deviceId'])}")
        print(f"  private_api_boundary: online {online_private_api['status']} private,no-store / offline {offline_private_api['status']} {offline_private_api['error']}")
        print(f"  cache_names: {', '.join(cache_names)}")
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
        if process is not None:
            output = read_server_log_tail(process)
            if output:
                print(output, file=sys.stderr)
        return 1
    finally:
        if browser is not None:
            try:
                browser.close()
            except Exception:  # noqa: BLE001
                pass
        if process:
            stop_server(process)
            cleanup_server_artifacts(process)


def main_grant_boundaries() -> int:
    """Exercise the public install shell and the distinct server-granted live shell."""
    process: subprocess.Popen[str] | None = None
    browser = None
    current_step = "boot"
    current_page: Page | None = None
    try:
        process, base_url, _state_dir = start_server()
        with sync_playwright() as playwright:
            browser = playwright.chromium.launch(headless=True)

            current_step = "public install boundary"
            public_context = browser.new_context(service_workers="allow")
            public_page = public_context.new_page()
            current_page = public_page
            public_response = public_page.goto(
                f"{base_url}/mobile/player?sessionId=forged-session&role=GameMaster&deviceId=forged-device",
                wait_until="load",
                timeout=NAVIGATION_TIMEOUT_MS,
            )
            assert_true(public_response is not None and public_response.status == 200, "public install route must remain available")
            public_page.wait_for_selector("[data-play-surface='install-only'][data-authority='none']", timeout=NAVIGATION_TIMEOUT_MS)
            public_state = public_page.evaluate(
                """() => ({
                    path: window.location.pathname,
                    hasLiveRoot: document.querySelector('[data-turn-root]') !== null,
                    hasLiveRuntime: document.querySelector('script[src="/mobile-turn-companion.js"]') !== null,
                    body: document.body.innerText,
                    manifest: document.querySelector('link[rel="manifest"]')?.getAttribute('href') || ''
                })"""
            )
            assert_true(public_state["path"] == "/mobile/player", "public player install route must remain canonical")
            assert_true(public_state["hasLiveRoot"] is False, "public install route must not render live table state")
            assert_true(public_state["hasLiveRuntime"] is False, "public install route must not load the live runtime")
            assert_true(public_state["manifest"] == "/manifest.player.webmanifest", "public player route must advertise its install manifest")
            assert_true("forged-session" not in public_state["body"] and "forged-device" not in public_state["body"], "query identifiers must not enter public install content")
            public_cache_control = public_response.headers.get("cache-control", "")
            public_csp = public_response.headers.get("content-security-policy", "")
            assert_true(
                "private" in public_cache_control.lower() and "no-store" in public_cache_control.lower(),
                "public install document must remain private and no-store even though its static assets are cacheable",
            )
            assert_true("connect-src 'none'" in public_csp, "public install route must not open a live network boundary")

            current_step = "query-only live denial"
            denied_page = public_context.new_page()
            current_page = denied_page
            denied_response = denied_page.goto(
                f"{base_url}/mobile/live?sessionId=forged-session&role=GameMaster&deviceId=forged-device",
                wait_until="domcontentloaded",
                timeout=NAVIGATION_TIMEOUT_MS,
            )
            assert_true(denied_response is not None and denied_response.status == 403, "query parameters must not grant live companion access")
            denied_body = denied_page.locator("body").inner_text()
            assert_true("play_session_grant_required" in denied_body, "live denial must name the missing grant boundary")

            current_step = "authoritative live grant"
            granted_session = "session-runtime-grant"
            granted_device = "runtime-granted-device"
            grant_headers = {
                "X-Chummer-Play-Grant-Id": "runtime-smoke-grant-0001",
                "X-Chummer-Play-Grant-Session-Id": granted_session,
                "X-Chummer-Play-Grant-Role": PLAYER_ROLE,
                "X-Chummer-Play-Grant-Device-Id": granted_device,
            }
            live_context = browser.new_context(service_workers="block", extra_http_headers=grant_headers)
            live_page = live_context.new_page()
            current_page = live_page
            live_response = live_page.goto(
                f"{base_url}/mobile/live?sessionId=forged-session&role=GameMaster&deviceId=forged-device",
                wait_until="domcontentloaded",
                timeout=NAVIGATION_TIMEOUT_MS,
            )
            assert_true(live_response is not None and live_response.status == 200, "trusted server grant must open the live companion")
            live_page.wait_for_selector("[data-turn-root][data-session-grant-backed='true']", timeout=NAVIGATION_TIMEOUT_MS)
            live_page.wait_for_selector("script[src='/mobile-turn-companion.js']", state="attached", timeout=NAVIGATION_TIMEOUT_MS)
            live_state = live_page.evaluate(
                """() => {
                    const root = document.querySelector('[data-turn-root]');
                    const bootstrap = JSON.parse(document.getElementById('turn-companion-bootstrap')?.textContent || '{}');
                    const currentRoleLink = document.querySelector('[data-role-name="Player"]');
                    return {
                        sessionId: root?.getAttribute('data-session-id') || '',
                        role: root?.getAttribute('data-role') || '',
                        deviceId: root?.getAttribute('data-device-id') || '',
                        bootstrapSessionId: bootstrap.sessionId || '',
                        bootstrapRole: bootstrap.roleName || '',
                        bootstrapDeviceId: bootstrap.deviceId || '',
                        currentRoleHref: currentRoleLink?.getAttribute('href') || '',
                        gmExitHref: document.querySelector('[data-role-name="GameMaster"]')?.getAttribute('href') || '',
                        forgedVisible: (document.body.innerText || '').includes('forged-session')
                            || (document.body.innerText || '').includes('forged-device')
                    };
                }"""
            )
            assert_true(live_state["sessionId"] == granted_session, "live root must use the server-granted session")
            assert_true(live_state["role"] == PLAYER_ROLE, "live root must use the server-granted role")
            assert_true(live_state["deviceId"] == granted_device, "live root must use the server-granted device")
            assert_true(live_state["bootstrapSessionId"] == granted_session, "client bootstrap must use the server-granted session")
            assert_true(live_state["bootstrapRole"] == PLAYER_ROLE, "client bootstrap must use the server-granted role")
            assert_true(live_state["bootstrapDeviceId"] == granted_device, "client bootstrap must use the server-granted device")
            assert_true(
                live_state["currentRoleHref"] == "/mobile/live",
                f"the authoritative owner route must remain the live route: {live_state['currentRoleHref']!r}",
            )
            assert_true(live_state["gmExitHref"] == "/mobile/gm", "role changes must exit to a public install label instead of escalating authority")
            assert_true(live_state["forgedVisible"] is False, "query identifiers must not enter live companion content")
            live_cache_control = live_response.headers.get("cache-control", "")
            live_csp = live_response.headers.get("content-security-policy", "")
            assert_true("private" in live_cache_control.lower() and "no-store" in live_cache_control.lower(), "live document must be private and no-store")
            assert_true("connect-src 'self'" in live_csp, "live document must allow only its same-origin API lane")

            current_step = "private API boundary"
            private_api = verify_private_api_boundary(
                live_page,
                session_id=granted_session,
                role=PLAYER_ROLE,
                device_id=granted_device,
            )
            assert_true(private_api["status"] == 200, "trusted live shell must reach its bounded API")
            assert_true("private" in str(private_api["cacheControl"]).lower() and "no-store" in str(private_api["cacheControl"]).lower(), "private API must remain private and no-store")

            public_context.close()
            live_context.close()
            browser.close()
            browser = None

        write_runtime_receipt(
            {
                "contract_name": "chummer_play.mobile_pwa_runtime_smoke.v2",
                "status": "pass",
                "generated_at_utc": datetime.now(UTC).replace(microsecond=0).isoformat().replace("+00:00", "Z"),
                "public_install_boundary": {
                    "routes": ["/mobile", "/mobile/player", "/mobile/gm", "/mobile/observer"],
                    "authority": "none",
                    "live_state_loaded": False,
                    "live_runtime_loaded": False,
                    "query_parameters_grant_access": False,
                    "manifest": public_state["manifest"],
                    "cache_control": public_cache_control,
                    "content_security_policy": public_csp,
                },
                "live_session_boundary": {
                    "route": "/mobile/live",
                    "grant_source": "trusted_server_headers",
                    "query_parameters_grant_access": False,
                    "session_binding": granted_session,
                    "role_binding": PLAYER_ROLE,
                    "device_binding": granted_device,
                    "owner_route": live_state["currentRoleHref"],
                    "role_change_exit": live_state["gmExitHref"],
                    "cache_control": live_cache_control,
                    "content_security_policy": live_csp,
                },
                "private_api_boundary": {
                    "online_status": private_api["status"],
                    "online_cache_control": private_api["cacheControl"],
                },
            }
        )
        print("mobile_pwa_runtime_smoke ok")
        print("  public_install_boundary: /mobile /mobile/player /mobile/gm /mobile/observer")
        print("  public_authority: none")
        print("  query_parameters_grant_access: false")
        print("  live_session_boundary: /mobile/live")
        print("  live_grant_source: trusted_server_headers")
        print("  live_owner_route: /mobile/live")
        print("  private_api_boundary: online 200 private,no-store")
        return 0
    except Exception as error:  # noqa: BLE001
        print(f"mobile_pwa_runtime_smoke failed during {current_step}: {error}", file=sys.stderr)
        if current_page is not None:
            try:
                print(f"current_url: {current_page.url}", file=sys.stderr)
                print(current_page.content()[:1000], file=sys.stderr)
            except Exception:  # noqa: BLE001
                pass
        if process is not None:
            output = read_server_log_tail(process)
            if output:
                print(output, file=sys.stderr)
        return 1
    finally:
        if browser is not None:
            try:
                browser.close()
            except Exception:  # noqa: BLE001
                pass
        if process is not None:
            stop_server(process)
            cleanup_server_artifacts(process)


if __name__ == "__main__":
    raise SystemExit(main_grant_boundaries())
