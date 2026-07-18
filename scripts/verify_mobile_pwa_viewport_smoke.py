#!/usr/bin/env python3
from __future__ import annotations

import os
import json
import re
import shutil
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
RECEIPT_PATH = ROOT / ".codex-studio" / "published" / "MOBILE_PWA_VIEWPORT_SMOKE.generated.json"
SESSION_ID = "session-mobile-viewport"
DEVICE_ID = "viewport-player-shell"
ROLE_NAME = "Player"
GM_DEVICE_ID = "viewport-gm-shell"
GM_ROLE_NAME = "GameMaster"
SCREENSHOT_TIMEOUT_MS = 120_000
LOCAL_ORIGIN_PLACEHOLDER = "http://127.0.0.1:<port>"
LOCAL_ORIGIN_RE = re.compile(r"http://(?:127\.0\.0\.1|localhost):\d+")


def assert_true(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def redact_local_url(value: object) -> str:
    return LOCAL_ORIGIN_RE.sub(LOCAL_ORIGIN_PLACEHOLDER, str(value or ""))


def has_maskable_icon(manifest_data: dict[str, object], src: str) -> bool:
    icons = manifest_data.get("icons")
    if not isinstance(icons, list):
        return False
    for icon in icons:
        if not isinstance(icon, dict):
            continue
        purpose = str(icon.get("purpose") or "")
        purpose_tokens = {token.strip() for token in purpose.split() if token.strip()}
        if icon.get("src") == src and "any" in purpose_tokens and "maskable" in purpose_tokens:
            return True
    return False


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


def start_server() -> tuple[subprocess.Popen[str], str]:
    port = free_port()
    base_url = f"http://127.0.0.1:{port}"
    state_dir = tempfile.mkdtemp(prefix="chummer-play-viewport-smoke-")
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
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        text=True,
    )
    setattr(process, "_chummer_state_dir", state_dir)
    wait_for_health(base_url)
    return process, base_url


def stop_server(process: subprocess.Popen[str]) -> None:
    if process.poll() is not None:
        cleanup_server_artifacts(process)
        return
    process.terminate()
    try:
        process.wait(timeout=10)
    except subprocess.TimeoutExpired:
        process.kill()
        process.wait(timeout=10)
    finally:
        cleanup_server_artifacts(process)


def cleanup_server_artifacts(process: subprocess.Popen[str]) -> None:
    state_dir = getattr(process, "_chummer_state_dir", "")
    if state_dir:
        shutil.rmtree(state_dir, ignore_errors=True)


def wait_for_mobile_shell(page: Page) -> None:
    page.wait_for_selector("[data-turn-root]")
    page.wait_for_selector("#turn-shell-summary")
    page.wait_for_selector("#turn-jump-nav")
    page.wait_for_selector("#turn-glance-grid")


def assert_role_navigation_semantics(page: Page, expected_role: str, label: str) -> dict[str, object]:
    navigation = page.get_by_role("navigation", name="Choose play role", exact=True)
    assert_true(navigation.count() == 1, f"{label} must expose one named role-navigation landmark")
    links = navigation.get_by_role("link")
    assert_true(links.count() == 3, f"{label} role navigation must expose exactly three native links")

    state = navigation.evaluate(
        """(element) => {
            const links = Array.from(element.querySelectorAll("a[href]"));
            const currentLinks = links.filter((link) => link.getAttribute("aria-current") === "page");
            return {
                accessibleLabel: element.getAttribute("aria-label") || "",
                linkNames: links.map((link) => (link.textContent || "").trim()),
                roleNames: links.map((link) => link.getAttribute("data-role-name") || ""),
                currentCount: currentLinks.length,
                currentRole: currentLinks[0]?.getAttribute("data-role-name") || "",
                tablistCount: document.querySelectorAll('[role="tablist"]').length,
            };
        }"""
    )
    assert_true(state["accessibleLabel"] == "Choose play role", f"{label} role navigation must keep its accessible name")
    assert_true(state["linkNames"] == ["Player", "GM", "Observer"], f"{label} role navigation must keep all role choices named")
    assert_true(state["currentCount"] == 1, f"{label} role navigation must expose exactly one current route")
    assert_true(state["currentRole"] == expected_role, f"{label} role navigation must announce {expected_role} as current")
    assert_true(state["tablistCount"] == 0, f"{label} must not claim tablist semantics without tab keyboard behavior")
    return state


def assert_keyboard_focus_visibility(page: Page, label: str) -> dict[str, object]:
    page.evaluate(
        """() => {
            const active = document.activeElement;
            if (active instanceof HTMLElement) {
                active.blur();
            }
        }"""
    )

    state: dict[str, object] | None = None
    for _ in range(12):
        page.keyboard.press("Tab")
        candidate = page.evaluate(
            """() => {
                const active = document.activeElement;
                if (!(active instanceof HTMLElement)) {
                    return null;
                }
                const style = getComputedStyle(active);
                const rect = active.getBoundingClientRect();
                return {
                    inRoleNavigation: !!active.closest('nav[aria-label="Choose play role"]'),
                    tagName: active.tagName.toLowerCase(),
                    roleName: active.getAttribute("data-role-name") || "",
                    focusVisible: active.matches(":focus-visible"),
                    outlineStyle: style.outlineStyle,
                    outlineWidth: style.outlineWidth,
                    outlineWidthPx: Number.parseFloat(style.outlineWidth) || 0,
                    outlineColor: style.outlineColor,
                    outlineOffset: style.outlineOffset,
                    left: rect.left,
                    right: rect.right,
                    top: rect.top,
                    bottom: rect.bottom,
                    viewportWidth: window.innerWidth,
                    viewportHeight: window.innerHeight,
                };
            }"""
        )
        if candidate and candidate["inRoleNavigation"] is True:
            state = candidate
            break

    assert_true(state is not None, f"{label} keyboard traversal must reach the role navigation")
    assert_true(state["tagName"] == "a", f"{label} keyboard focus target must remain a native link")
    assert_true(bool(state["roleName"]), f"{label} keyboard focus target must identify its role route")
    assert_true(state["focusVisible"] is True, f"{label} keyboard focus must activate :focus-visible")
    assert_true(state["outlineStyle"] not in {"none", "hidden"}, f"{label} keyboard focus must render an outline")
    assert_true(float(state["outlineWidthPx"]) >= 3.0, f"{label} keyboard focus outline must be at least 3px")
    assert_true(state["outlineColor"] not in {"transparent", "rgba(0, 0, 0, 0)"}, f"{label} keyboard focus outline must be visible")
    assert_true(float(state["left"]) >= -1 and float(state["right"]) <= float(state["viewportWidth"]) + 1, f"{label} focused role link must stay inside the horizontal viewport")
    assert_true(float(state["top"]) >= -1 and float(state["bottom"]) <= float(state["viewportHeight"]) + 1, f"{label} focused role link must stay inside the visible viewport")
    return state


def assert_reduced_motion_runtime(page: Page, label: str) -> dict[str, object]:
    page.emulate_media(reduced_motion="reduce")
    try:
        page.wait_for_function("() => window.matchMedia('(prefers-reduced-motion: reduce)').matches")
        state = page.evaluate(
            """() => {
                const durationMs = (value) => Math.max(...String(value || "0s").split(",").map((part) => {
                    const token = part.trim();
                    if (token.endsWith("ms")) return Number.parseFloat(token) || 0;
                    if (token.endsWith("s")) return (Number.parseFloat(token) || 0) * 1000;
                    return 0;
                }));
                const roleLink = document.querySelector('a[data-role-name="Player"]');
                const style = roleLink ? getComputedStyle(roleLink) : null;
                return {
                    mediaMatches: window.matchMedia("(prefers-reduced-motion: reduce)").matches,
                    rootScrollBehavior: getComputedStyle(document.documentElement).scrollBehavior,
                    transitionDurationMs: style ? durationMs(style.transitionDuration) : Number.POSITIVE_INFINITY,
                    animationDurationMs: style ? durationMs(style.animationDuration) : Number.POSITIVE_INFINITY,
                    animationIterationCount: style ? style.animationIterationCount : "",
                };
            }"""
        )
    finally:
        page.emulate_media(reduced_motion="no-preference")

    assert_true(state["mediaMatches"] is True, f"{label} must observe the emulated reduced-motion preference")
    assert_true(state["rootScrollBehavior"] == "auto", f"{label} reduced-motion mode must disable smooth scrolling")
    assert_true(float(state["transitionDurationMs"]) <= 0.011, f"{label} reduced-motion mode must suppress nonessential transitions")
    assert_true(float(state["animationDurationMs"]) <= 0.011, f"{label} reduced-motion mode must suppress nonessential animations")
    assert_true(state["animationIterationCount"] == "1", f"{label} reduced-motion mode must bound animation repetition")
    return state


def assert_living_world_boundary(page: Page, label: str) -> dict[str, object]:
    page.wait_for_selector("#turn-living-world-card")
    state = page.evaluate(
        """() => {
            const card = document.getElementById("turn-living-world-card");
            const text = (card ? card.textContent || "" : "").toLowerCase();
            return {
                cardPresent: !!card,
                marker: card ? card.getAttribute("data-living-world-opt-in-boundary") || "" : "",
                mentionsLivingWorld: text.includes("living-world") || text.includes("living world"),
                mentionsBlackLedger: text.includes("black ledger"),
                mentionsHeat: text.includes("heat"),
                mentionsOptIn: text.includes("opt in") || text.includes("opt-in"),
                mentionsOffline: text.includes("offline"),
                mentionsNoPrivateCache: text.includes("does not cache private table state"),
            };
        }"""
    )
    assert_true(state["cardPresent"] is True, f"{label} mobile viewport must render the Living World opt-in boundary card")
    assert_true(
        state["marker"] == "true",
        f"{label} mobile viewport must mark the Living World boundary card for automated proof",
    )
    assert_true(state["mentionsLivingWorld"] is True, f"{label} mobile viewport must visibly name Living World")
    assert_true(state["mentionsBlackLedger"] is True, f"{label} mobile viewport must visibly name Black Ledger")
    assert_true(state["mentionsHeat"] is True, f"{label} mobile viewport must visibly name heat")
    assert_true(state["mentionsOptIn"] is True, f"{label} mobile viewport must visibly state the opt-in boundary")
    assert_true(state["mentionsOffline"] is True, f"{label} mobile viewport must visibly name offline posture")
    assert_true(
        state["mentionsNoPrivateCache"] is True,
        f"{label} mobile viewport must state that offline mode does not cache private table state",
    )
    return state


def install_surface_state(page: Page) -> dict[str, object]:
    return page.evaluate(
        """() => {
            const button = document.getElementById("turn-install-button");
            const detail = document.getElementById("turn-install-detail");
            const status = document.getElementById("turn-install-status");
            return {
                buttonText: button ? (button.textContent || "").trim() : "",
                buttonDisabled: button ? button.disabled === true : false,
                detail: detail ? (detail.textContent || "").trim() : "",
                status: status ? (status.textContent || "").trim() : "",
                role: document.querySelector("[data-turn-root]")?.getAttribute("data-role") || ""
            };
        }"""
    )


def wait_for_control_state(page: Page, expression: str, message: str, arg: object | None = None) -> None:
    try:
        page.wait_for_function(expression, arg=arg, timeout=10_000)
    except PlaywrightTimeoutError as error:
        raise AssertionError(message) from error


def assert_interactive_control_accessibility(page: Page, label: str) -> dict[str, object]:
    modifier = page.locator(
        '#turn-modifier-list input[data-turn-kind="toggle-modifier"]:not([disabled])'
    ).first
    increment = page.locator(
        '.stepper button[data-turn-kind="adjust-metric"][data-delta="1"]:not([disabled])'
    ).first
    unselected_action = page.locator(
        '#turn-action-grid button[data-turn-kind="select-action"][aria-pressed="false"]:not([disabled])'
    ).first
    wait_for_control_state(
        page,
        """() => document.querySelector('[data-turn-root][data-client-ready="true"]')
            && window.__chummerPlayActiveClient?.networkBusy === false
            && document.querySelectorAll('.stepper button[data-turn-kind="adjust-metric"]').length > 1
            && document.querySelector('#turn-modifier-list input[data-turn-kind="toggle-modifier"]:not([disabled])')
            && document.querySelector('#turn-action-grid button[data-turn-kind="select-action"][aria-pressed="false"]:not([disabled])')""",
        f"{label} must finish its initial refresh and expose enabled metric, modifier, and unselected action controls",
    )

    stepper_labels = page.locator('.stepper button[data-turn-kind="adjust-metric"]').evaluate_all(
        "buttons => buttons.map(button => button.getAttribute('aria-label') || '')"
    )
    assert_true(all(stepper_labels), f"{label} every metric and inventory stepper must have an accessible name")
    assert_true(
        len(stepper_labels) == len(set(stepper_labels)),
        f"{label} metric and inventory stepper accessible names must be unique",
    )
    assert_true(
        all(name.startswith(("Increase ", "Decrease ")) and ", currently " in name for name in stepper_labels),
        f"{label} stepper accessible names must identify direction, item, and current value",
    )

    selected_action_count = page.locator(
        '#turn-action-grid button[data-turn-kind="select-action"][aria-pressed="true"]'
    ).count()
    assert_true(selected_action_count == 1, f"{label} must expose exactly one pressed action button")

    modifier_id = modifier.get_attribute("data-modifier-id") or ""
    initial_modifier_state = modifier.is_checked()
    modifier.focus()
    page.keyboard.press("Space")
    wait_for_control_state(
        page,
        """([modifierId, initialState]) => {
            const control = document.querySelector(`[data-turn-kind="toggle-modifier"][data-modifier-id="${CSS.escape(modifierId)}"]`);
            return control && control.checked !== initialState;
        }""",
        f"{label} modifier Space activation must change the checkbox state",
        [modifier_id, initial_modifier_state],
    )
    modifier_focus = page.evaluate(
        """modifierId => document.activeElement?.getAttribute("data-turn-kind") === "toggle-modifier"
            && document.activeElement?.getAttribute("data-modifier-id") === modifierId""",
        modifier_id,
    )
    assert_true(modifier_focus is True, f"{label} modifier activation must retain logical keyboard focus")

    metric_id = increment.get_attribute("data-metric-id") or ""
    metric_delta = increment.get_attribute("data-delta") or ""
    initial_increment_label = increment.get_attribute("aria-label") or ""
    increment.focus()
    page.keyboard.press("Enter")
    metric_state = page.evaluate(
        """([metricId, delta, previousLabel]) => {
            const control = Array.from(document.querySelectorAll('[data-turn-kind="adjust-metric"]')).find((candidate) =>
                candidate.getAttribute("data-metric-id") === metricId
                && candidate.getAttribute("data-delta") === delta);
            return {
                present: !!control,
                focusRetained: !!control && document.activeElement === control,
                previousLabel,
                currentLabel: control ? control.getAttribute("aria-label") || "" : "",
            };
        }""",
        [metric_id, metric_delta, initial_increment_label],
    )
    assert_true(
        metric_state["present"] is True and metric_state["focusRetained"] is True,
        f"{label} metric activation must retain focus: {metric_state}",
    )
    assert_true(
        metric_state["currentLabel"] != metric_state["previousLabel"],
        f"{label} metric activation must update its current-value label: {metric_state}",
    )

    action_id = unselected_action.get_attribute("data-action-id") or ""
    unselected_action.focus()
    page.keyboard.press("Enter")
    action_state = page.evaluate(
        """actionId => {
            const control = document.querySelector(`[data-turn-kind="select-action"][data-action-id="${CSS.escape(actionId)}"]`);
            return {
                present: !!control,
                pressed: control ? control.getAttribute("aria-pressed") : null,
                focusRetained: !!control && document.activeElement === control,
            };
        }""",
        action_id,
    )
    assert_true(
        action_state["present"] is True
        and action_state["pressed"] == "true"
        and action_state["focusRetained"] is True,
        f"{label} action activation must expose pressed state and retain focus: {action_state}",
    )

    native_select_click = page.evaluate(
        """() => {
            const control = document.querySelector('[data-turn-kind="select-anchor"]');
            if (!control) return null;
            let cancelled = null;
            control.addEventListener("click", (event) => { cancelled = event.defaultPrevented; }, { once: true });
            control.dispatchEvent(new MouseEvent("click", { bubbles: true, cancelable: true }));
            return cancelled;
        }"""
    )
    assert_true(native_select_click is False, f"{label} native select clicks must not be cancelled by delegation")

    initialization_bounded = page.evaluate(
        """async () => {
            window.__chummerPlayInitializationTimer = null;
            const marker = document.createElement("span");
            document.body.appendChild(marker);
            await new Promise((resolve) => setTimeout(resolve, 25));
            marker.remove();
            await new Promise((resolve) => setTimeout(resolve, 25));
            return window.__chummerPlayInitializationTimer === null;
        }"""
    )
    assert_true(initialization_bounded is True, f"{label} ready-root mutations must not reschedule initialization")

    banner_copy = page.locator("#mobile-client-banner-copy").inner_text().strip()
    install_detail = page.locator("#turn-install-detail").inner_text().strip()
    assert_true(
        "open page only" in banner_copy or "open tab only" in banner_copy,
        f"{label} banner must state the open-page private-state lifetime",
    )
    assert_true(
        "public shell assets" in install_detail and "private table state" in install_detail,
        f"{label} install guidance must limit installation caching to public shell assets",
    )

    return {
        "stepper_accessible_name_count": len(stepper_labels),
        "selected_action_count": selected_action_count,
        "modifier_focus_retained": modifier_focus,
        "increment_focus_retained": metric_state["focusRetained"],
        "action_focus_retained": action_state["focusRetained"],
        "native_select_click_cancelled": native_select_click,
        "initialization_reschedule_bounded": initialization_bounded,
        "banner_copy": banner_copy,
        "install_detail": install_detail,
    }


def capture_viewport_receipt(page: Page, path: Path) -> str:
    screenshot_options = {
        "path": str(path),
        "animations": "disabled",
        "caret": "hide",
        "timeout": SCREENSHOT_TIMEOUT_MS,
    }
    try:
        page.screenshot(full_page=True, **screenshot_options)
        mode = "full-page"
    except PlaywrightTimeoutError:
        page.screenshot(full_page=False, **screenshot_options)
        mode = "viewport-fallback"

    assert_true(path.exists() and path.stat().st_size > 0, f"viewport receipt screenshot must be written at {path}")
    return mode


def write_viewport_receipt(payload: dict[str, object]) -> None:
    payload["verification_mode"] = os.environ.get("CHUMMER_VERIFY_MODE", "slice").strip() or "slice"
    payload["verification_run_id"] = os.environ.get("CHUMMER_VERIFY_RUN_ID", "").strip()
    RECEIPT_PATH.parent.mkdir(parents=True, exist_ok=True)
    RECEIPT_PATH.write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def main() -> int:
    process: subprocess.Popen[str] | None = None
    current_step = "boot"
    current_page: Page | None = None
    try:
        process, base_url = start_server()
        screenshot_path = ROOT / "_tmp" / "mobile-viewport-smoke-player-390x844.png"
        gm_screenshot_path = ROOT / "_tmp" / "mobile-viewport-smoke-gm-390x844.png"
        narrow_screenshot_path = ROOT / "_tmp" / "mobile-viewport-smoke-player-360x740.png"
        screenshot_path.parent.mkdir(exist_ok=True)

        with sync_playwright() as playwright:
            browser = playwright.chromium.launch(headless=True)
            page = browser.new_page(viewport={"width": 390, "height": 844}, is_mobile=True)
            current_page = page

            current_step = "open mobile player lane"
            page.goto(
                f"{base_url}/mobile?sessionId={SESSION_ID}&role={ROLE_NAME}&deviceId={DEVICE_ID}",
                wait_until="domcontentloaded",
            )
            wait_for_mobile_shell(page)
            player_living_world_boundary = assert_living_world_boundary(page, "player")
            player_role_navigation = assert_role_navigation_semantics(page, ROLE_NAME, "player mobile viewport")
            player_keyboard_focus = assert_keyboard_focus_visibility(page, "player mobile viewport")
            player_reduced_motion = assert_reduced_motion_runtime(page, "player mobile viewport")
            player_interactive_controls = assert_interactive_control_accessibility(
                page,
                "player mobile viewport",
            )

            current_step = "inspect installability"
            cdp_session = page.context.new_cdp_session(page)
            cdp_session.send("Page.enable")
            manifest = cdp_session.send("Page.getAppManifest")
            manifest_errors = manifest.get("errors", [])
            assert_true(len(manifest_errors) == 0, "mobile viewport must keep the app manifest free of Chromium parse errors")
            manifest_url = str(manifest.get("url") or "")
            assert_true(
                manifest_url.endswith("/manifest.player.webmanifest"),
                "player mobile viewport must advertise the player-specific PWA manifest",
            )
            manifest_data = json.loads(str(manifest.get("data") or "{}"))
            assert_true(manifest_data.get("id") == "/mobile/player", "player manifest must use the player app id")
            assert_true(
                manifest_data.get("start_url") == "/mobile/player?role=Player",
                "player manifest must launch the player runtime mode directly",
            )
            assert_true(
                manifest_data.get("scope") == "/mobile/",
                "player manifest must keep installed navigation scoped to mobile turn-companion routes",
            )
            assert_true(
                has_maskable_icon(manifest_data, "/icons/icon-192.png"),
                "player manifest must expose an adaptive 192px any+maskable icon",
            )
            assert_true(
                has_maskable_icon(manifest_data, "/icons/icon-512.png"),
                "player manifest must expose an adaptive 512px any+maskable icon",
            )
            installability = cdp_session.send("Page.getInstallabilityErrors")
            installability_errors = installability.get("installabilityErrors", [])
            assert_true(len(installability_errors) == 0, "mobile viewport must stay installable without Chromium PWA installability errors")

            current_step = "inspect GM installability"
            gm_page = browser.new_page(viewport={"width": 390, "height": 844}, is_mobile=True)
            current_page = gm_page
            gm_page.goto(
                f"{base_url}/mobile/gm?sessionId={SESSION_ID}&role={GM_ROLE_NAME}&deviceId={GM_DEVICE_ID}",
                wait_until="domcontentloaded",
                timeout=60_000,
            )
            wait_for_mobile_shell(gm_page)
            gm_living_world_boundary = assert_living_world_boundary(gm_page, "GM")
            gm_role = gm_page.locator("[data-turn-root]").get_attribute("data-role") or ""
            assert_true(gm_role == GM_ROLE_NAME, "GM mobile viewport must preserve the GM runtime mode on the shell root")
            gm_cdp_session = gm_page.context.new_cdp_session(gm_page)
            gm_cdp_session.send("Page.enable")
            gm_manifest = gm_cdp_session.send("Page.getAppManifest")
            gm_manifest_errors = gm_manifest.get("errors", [])
            assert_true(len(gm_manifest_errors) == 0, "GM mobile viewport must keep the app manifest free of Chromium parse errors")
            gm_manifest_url = str(gm_manifest.get("url") or "")
            assert_true(
                gm_manifest_url.endswith("/manifest.gm.webmanifest"),
                "GM mobile viewport must advertise the GM-specific PWA manifest",
            )
            gm_manifest_data = json.loads(str(gm_manifest.get("data") or "{}"))
            assert_true(gm_manifest_data.get("id") == "/mobile/gm", "GM manifest must use the GM app id")
            assert_true(
                gm_manifest_data.get("start_url") == "/mobile/gm?role=GameMaster",
                "GM manifest must launch the GM runtime mode directly",
            )
            assert_true(
                gm_manifest_data.get("scope") == "/mobile/",
                "GM manifest must keep installed navigation scoped to mobile turn-companion routes",
            )
            assert_true(
                has_maskable_icon(gm_manifest_data, "/icons/icon-192.png"),
                "GM manifest must expose an adaptive 192px any+maskable icon",
            )
            assert_true(
                has_maskable_icon(gm_manifest_data, "/icons/icon-512.png"),
                "GM manifest must expose an adaptive 512px any+maskable icon",
            )
            gm_installability = gm_cdp_session.send("Page.getInstallabilityErrors")
            gm_installability_errors = gm_installability.get("installabilityErrors", [])
            assert_true(len(gm_installability_errors) == 0, "GM mobile viewport must stay installable without Chromium PWA installability errors")

            current_step = "inspect query-role GM installability"
            gm_query_page = browser.new_page(viewport={"width": 390, "height": 844}, is_mobile=True)
            current_page = gm_query_page
            gm_query_page.goto(
                f"{base_url}/mobile?sessionId={SESSION_ID}&role={GM_ROLE_NAME}&deviceId={GM_DEVICE_ID}",
                wait_until="domcontentloaded",
                timeout=60_000,
            )
            wait_for_mobile_shell(gm_query_page)
            gm_query_role = gm_query_page.locator("[data-turn-root]").get_attribute("data-role") or ""
            assert_true(gm_query_role == GM_ROLE_NAME, "query-role GM mobile viewport must preserve the GM runtime mode on the shell root")
            gm_query_cdp_session = gm_query_page.context.new_cdp_session(gm_query_page)
            gm_query_cdp_session.send("Page.enable")
            gm_query_manifest = gm_query_cdp_session.send("Page.getAppManifest")
            gm_query_manifest_errors = gm_query_manifest.get("errors", [])
            assert_true(len(gm_query_manifest_errors) == 0, "query-role GM mobile viewport must keep the app manifest free of Chromium parse errors")
            gm_query_manifest_url = str(gm_query_manifest.get("url") or "")
            assert_true(
                gm_query_manifest_url.endswith("/manifest.gm.webmanifest"),
                "query-role GM mobile viewport must advertise the GM-specific PWA manifest",
            )
            gm_query_installability = gm_query_cdp_session.send("Page.getInstallabilityErrors")
            gm_query_installability_errors = gm_query_installability.get("installabilityErrors", [])
            assert_true(
                len(gm_query_installability_errors) == 0,
                "query-role GM mobile viewport must stay installable without Chromium PWA installability errors",
            )
            gm_query_page.close()

            current_step = "verify standalone install UI"
            standalone_context = browser.new_context(viewport={"width": 390, "height": 844}, is_mobile=True)
            standalone_context.add_init_script(
                """
                const originalMatchMedia = window.matchMedia ? window.matchMedia.bind(window) : null;
                window.matchMedia = function (query) {
                    if (query === "(display-mode: standalone)") {
                        return {
                            matches: true,
                            media: query,
                            onchange: null,
                            addListener: function () {},
                            removeListener: function () {},
                            addEventListener: function () {},
                            removeEventListener: function () {},
                            dispatchEvent: function () { return false; }
                        };
                    }
                    return originalMatchMedia
                        ? originalMatchMedia(query)
                        : {
                            matches: false,
                            media: query,
                            onchange: null,
                            addListener: function () {},
                            removeListener: function () {},
                            addEventListener: function () {},
                            removeEventListener: function () {},
                            dispatchEvent: function () { return false; }
                        };
                };
                Object.defineProperty(window.navigator, "standalone", {
                    configurable: true,
                    value: true
                });
                """
            )
            standalone_player_page = standalone_context.new_page()
            current_page = standalone_player_page
            standalone_player_page.goto(
                f"{base_url}/mobile/player?sessionId={SESSION_ID}&role={ROLE_NAME}&deviceId={DEVICE_ID}",
                wait_until="domcontentloaded",
                timeout=60_000,
            )
            wait_for_mobile_shell(standalone_player_page)
            standalone_player_page.wait_for_function(
                """() => {
                    const button = document.getElementById("turn-install-button");
                    const detail = document.getElementById("turn-install-detail")?.textContent || "";
                    return button
                        && (button.textContent || "").trim() === "Installed"
                        && button.disabled === true
                        && detail.includes("public turn-companion shell installed");
                }""",
                timeout=60_000,
            )
            standalone_player_state = install_surface_state(standalone_player_page)
            assert_true(standalone_player_state["role"] == ROLE_NAME, "standalone player install UI must stay on the player role")

            standalone_gm_page = standalone_context.new_page()
            current_page = standalone_gm_page
            standalone_gm_page.goto(
                f"{base_url}/mobile/gm?sessionId={SESSION_ID}&role={GM_ROLE_NAME}&deviceId={GM_DEVICE_ID}",
                wait_until="domcontentloaded",
                timeout=60_000,
            )
            wait_for_mobile_shell(standalone_gm_page)
            standalone_gm_page.wait_for_function(
                """() => {
                    const button = document.getElementById("turn-install-button");
                    const detail = document.getElementById("turn-install-detail")?.textContent || "";
                    return button
                        && (button.textContent || "").trim() === "Installed"
                        && button.disabled === true
                        && detail.includes("public turn-companion shell installed");
                }""",
                timeout=60_000,
            )
            standalone_gm_state = install_surface_state(standalone_gm_page)
            assert_true(standalone_gm_state["role"] == GM_ROLE_NAME, "standalone GM install UI must stay on the GM role")
            standalone_context.close()

            current_step = "capture GM viewport metrics"
            current_page = gm_page
            gm_metrics = gm_page.evaluate(
                """() => {
                    const actionGrid = document.getElementById('turn-action-grid');
                    const oddsGrid = document.getElementById('turn-odds-grid');
                    const jumpNav = document.getElementById('turn-jump-nav');
                    const glanceGrid = document.getElementById('turn-glance-grid');
                    const nowCard = document.getElementById('turn-now-card');
                    const trustCard = document.getElementById('turn-trust-card');
                    const quickLane = document.querySelector('.turn-quick-lane');
                    const keyTargets = Array.from(document.querySelectorAll('.turn-card, .glance-chip, .primary-button, .secondary-button'));
                    const touchTargets = Array.from(document.querySelectorAll('.role-button, .primary-button, .secondary-button, .jump-chip, .stepper button, .field-input'))
                        .map((element) => element.getBoundingClientRect())
                        .filter((rect) => rect.width > 0 && rect.height > 0);
                    const viewportWidth = window.innerWidth;
                    const incoherentOverflow = keyTargets.some((element) => {
                        const rect = element.getBoundingClientRect();
                        return rect.left < -1 || rect.right > viewportWidth + 1;
                    });
                    const minTouchTarget = touchTargets.reduce((minimum, rect) => Math.min(minimum, rect.width, rect.height), Number.POSITIVE_INFINITY);

                    return {
                        innerWidth: window.innerWidth,
                        docWidth: document.documentElement.scrollWidth,
                        hasHorizontalOverflow: document.documentElement.scrollWidth > window.innerWidth,
                        incoherentOverflow,
                        actionColumns: getComputedStyle(actionGrid).gridTemplateColumns.split(' ').filter(Boolean).length,
                        oddsColumns: getComputedStyle(oddsGrid).gridTemplateColumns.split(' ').filter(Boolean).length,
                        jumpChipCount: jumpNav.querySelectorAll('a').length,
                        glanceChipCount: glanceGrid.children.length,
                        quickLaneTop: quickLane.getBoundingClientRect().top,
                        nowCardTop: nowCard.getBoundingClientRect().top,
                        trustCardTop: trustCard.getBoundingClientRect().top,
                        minTouchTarget,
                        ammoValue: document.getElementById('turn-glance-ammo')?.textContent?.trim() || '',
                        anchorValue: document.getElementById('turn-glance-anchor')?.textContent?.trim() || '',
                    };
                }"""
            )
            assert_true(gm_metrics["hasHorizontalOverflow"] is False, "GM mobile viewport must avoid horizontal overflow on a 390px-wide phone")
            assert_true(gm_metrics["incoherentOverflow"] is False, "GM mobile viewport must keep key cards, chips, and buttons inside the visual viewport")
            assert_true(gm_metrics["jumpChipCount"] >= 5, "GM mobile viewport must expose quick jump targets for the high-frequency play surfaces")
            assert_true(gm_metrics["glanceChipCount"] == 6, "GM mobile viewport must expose the compact quick-glance tracker strip")
            assert_true(gm_metrics["quickLaneTop"] < gm_metrics["trustCardTop"], "GM mobile viewport must keep the quick-glance lane above the lower-context trust rail")
            assert_true(gm_metrics["nowCardTop"] < gm_metrics["trustCardTop"], "GM mobile viewport must prioritize live tracker controls above the trust and RUNSITE detail rails")
            assert_true(gm_metrics["actionColumns"] == 1, "GM mobile viewport must collapse the bounded action rail to a single column")
            assert_true(gm_metrics["oddsColumns"] == 1, "GM mobile viewport must collapse quick odds to a single column")
            assert_true(gm_metrics["minTouchTarget"] >= 43, "GM mobile viewport must keep primary controls near the 44px touch target floor")
            assert_true(gm_metrics["ammoValue"].isdigit(), "GM mobile viewport quick-glance rail must show the current magazine value")
            assert_true(len(gm_metrics["anchorValue"]) > 0, "GM mobile viewport quick-glance rail must show the selected RUNSITE anchor")
            gm_screenshot_mode = capture_viewport_receipt(gm_page, gm_screenshot_path)

            current_step = "capture viewport metrics"
            current_page = page
            metrics = page.evaluate(
                """() => {
                    const actionGrid = document.getElementById('turn-action-grid');
                    const oddsGrid = document.getElementById('turn-odds-grid');
                    const jumpNav = document.getElementById('turn-jump-nav');
                    const glanceGrid = document.getElementById('turn-glance-grid');
                    const nowCard = document.getElementById('turn-now-card');
                    const trustCard = document.getElementById('turn-trust-card');
                    const quickLane = document.querySelector('.turn-quick-lane');
                    const shell = document.querySelector('.turn-shell');

                    return {
                        innerWidth: window.innerWidth,
                        docWidth: document.documentElement.scrollWidth,
                        hasHorizontalOverflow: document.documentElement.scrollWidth > window.innerWidth,
                        actionColumns: getComputedStyle(actionGrid).gridTemplateColumns.split(' ').filter(Boolean).length,
                        oddsColumns: getComputedStyle(oddsGrid).gridTemplateColumns.split(' ').filter(Boolean).length,
                        jumpChipCount: jumpNav.querySelectorAll('a').length,
                        glanceChipCount: glanceGrid.children.length,
                        quickLaneTop: quickLane.getBoundingClientRect().top,
                        nowCardTop: nowCard.getBoundingClientRect().top,
                        trustCardTop: trustCard.getBoundingClientRect().top,
                        shellPaddingTop: getComputedStyle(shell).paddingTop,
                        shellPaddingBottom: getComputedStyle(shell).paddingBottom,
                        ammoValue: document.getElementById('turn-glance-ammo')?.textContent?.trim() || '',
                        anchorValue: document.getElementById('turn-glance-anchor')?.textContent?.trim() || '',
                    };
                }"""
            )

            assert_true(metrics["hasHorizontalOverflow"] is False, "mobile viewport must avoid horizontal overflow on a 390px-wide phone")
            assert_true(metrics["jumpChipCount"] >= 5, "mobile viewport must expose quick jump targets for the high-frequency play surfaces")
            assert_true(metrics["glanceChipCount"] == 6, "mobile viewport must expose the compact quick-glance tracker strip")
            assert_true(metrics["quickLaneTop"] < metrics["trustCardTop"], "mobile viewport must keep the quick-glance lane above the lower-context trust rail")
            assert_true(metrics["nowCardTop"] < metrics["trustCardTop"], "mobile viewport must prioritize live tracker controls above the trust and RUNSITE detail rails")
            assert_true(metrics["actionColumns"] == 1, "mobile viewport must collapse the bounded action rail to a single column")
            assert_true(metrics["oddsColumns"] == 1, "mobile viewport must collapse quick odds to a single column")
            assert_true(metrics["ammoValue"].isdigit(), "mobile viewport quick-glance rail must show the current magazine value")
            assert_true(len(metrics["anchorValue"]) > 0, "mobile viewport quick-glance rail must show the selected RUNSITE anchor")

            current_step = "capture screenshot"
            screenshot_mode = capture_viewport_receipt(page, screenshot_path)

            current_step = "inspect narrow player viewport"
            narrow_page = browser.new_page(viewport={"width": 360, "height": 740}, is_mobile=True)
            current_page = narrow_page
            narrow_page.goto(
                f"{base_url}/mobile/player?sessionId={SESSION_ID}&role={ROLE_NAME}&deviceId={DEVICE_ID}",
                wait_until="domcontentloaded",
                timeout=60_000,
            )
            wait_for_mobile_shell(narrow_page)
            narrow_living_world_boundary = assert_living_world_boundary(narrow_page, "narrow player")
            narrow_role_navigation = assert_role_navigation_semantics(
                narrow_page,
                ROLE_NAME,
                "narrow player mobile viewport",
            )
            narrow_metrics = narrow_page.evaluate(
                """() => {
                    const actionGrid = document.getElementById('turn-action-grid');
                    const oddsGrid = document.getElementById('turn-odds-grid');
                    const glanceGrid = document.getElementById('turn-glance-grid');
                    const statusPill = document.querySelector('.status-pill--ok') || document.querySelector('.status-pill');
                    const statusStyle = statusPill ? getComputedStyle(statusPill) : null;
                    const keyTargets = Array.from(document.querySelectorAll('.turn-card, .glance-chip, .primary-button, .secondary-button'));
                    const touchTargets = Array.from(document.querySelectorAll('.role-button, .primary-button, .secondary-button, .jump-chip, .stepper button, .field-input'))
                        .map((element) => element.getBoundingClientRect())
                        .filter((rect) => rect.width > 0 && rect.height > 0);
                    const viewportWidth = window.innerWidth;
                    const incoherentOverflow = keyTargets.some((element) => {
                        const rect = element.getBoundingClientRect();
                        return rect.left < -1 || rect.right > viewportWidth + 1;
                    });
                    const minTouchTarget = touchTargets.reduce((minimum, rect) => Math.min(minimum, rect.width, rect.height), Number.POSITIVE_INFINITY);

                    return {
                        innerWidth: window.innerWidth,
                        docWidth: document.documentElement.scrollWidth,
                        hasHorizontalOverflow: document.documentElement.scrollWidth > window.innerWidth,
                        incoherentOverflow,
                        actionColumns: getComputedStyle(actionGrid).gridTemplateColumns.split(' ').filter(Boolean).length,
                        oddsColumns: getComputedStyle(oddsGrid).gridTemplateColumns.split(' ').filter(Boolean).length,
                        glanceColumns: getComputedStyle(glanceGrid).gridTemplateColumns.split(' ').filter(Boolean).length,
                        minTouchTarget,
                        statusColor: statusStyle ? statusStyle.color : '',
                        statusBorderColor: statusStyle ? statusStyle.borderTopColor : ''
                    };
                }"""
            )
            assert_true(narrow_metrics["hasHorizontalOverflow"] is False, "narrow mobile viewport must avoid document-level horizontal overflow at 360px")
            assert_true(narrow_metrics["incoherentOverflow"] is False, "narrow mobile viewport must keep key cards, chips, and buttons inside the visual viewport")
            assert_true(narrow_metrics["actionColumns"] == 1, "narrow mobile viewport must keep bounded actions to one column")
            assert_true(narrow_metrics["oddsColumns"] == 1, "narrow mobile viewport must keep quick odds to one column")
            assert_true(narrow_metrics["glanceColumns"] == 2, "narrow mobile viewport must collapse quick-glance trackers to two columns")
            assert_true(narrow_metrics["minTouchTarget"] >= 43, "narrow mobile viewport must keep primary controls near the 44px touch target floor")
            assert_true(narrow_metrics["statusColor"] != "", "narrow mobile viewport must expose computed status-pill text color")
            assert_true(narrow_metrics["statusBorderColor"] != "", "narrow mobile viewport must expose computed status-pill border color")
            narrow_screenshot_mode = capture_viewport_receipt(narrow_page, narrow_screenshot_path)
            browser.close()

        write_viewport_receipt(
            {
                "contract_name": "chummer_play.mobile_pwa_viewport_smoke.v1",
                "status": "pass",
                "generated_at_utc": datetime.now(UTC).replace(microsecond=0).isoformat().replace("+00:00", "Z"),
                "viewports": {
                    "player": {"width": 390, "height": 844, "role": ROLE_NAME},
                    "gm": {"width": 390, "height": 844, "role": GM_ROLE_NAME},
                    "narrow_player": {"width": 360, "height": 740, "role": ROLE_NAME},
                },
                "overflow": {
                    "player": not metrics["hasHorizontalOverflow"],
                    "gm": not gm_metrics["hasHorizontalOverflow"],
                    "gm_key_bounds": not gm_metrics["incoherentOverflow"],
                    "narrow_player": not narrow_metrics["hasHorizontalOverflow"],
                    "narrow_key_bounds": not narrow_metrics["incoherentOverflow"],
                },
                "layout": {
                    "player_action_columns": metrics["actionColumns"],
                    "player_odds_columns": metrics["oddsColumns"],
                    "gm_action_columns": gm_metrics["actionColumns"],
                    "gm_odds_columns": gm_metrics["oddsColumns"],
                    "narrow_action_columns": narrow_metrics["actionColumns"],
                    "narrow_odds_columns": narrow_metrics["oddsColumns"],
                    "narrow_glance_columns": narrow_metrics["glanceColumns"],
                    "gm_min_touch_target": gm_metrics["minTouchTarget"],
                    "narrow_min_touch_target": narrow_metrics["minTouchTarget"],
                    "quick_lane_before_trust": metrics["quickLaneTop"] < metrics["trustCardTop"],
                    "gm_quick_lane_before_trust": gm_metrics["quickLaneTop"] < gm_metrics["trustCardTop"],
                },
                "quick_glance": {
                    "player_ammo": metrics["ammoValue"],
                    "player_anchor": metrics["anchorValue"],
                    "gm_ammo": gm_metrics["ammoValue"],
                    "gm_anchor": gm_metrics["anchorValue"],
                },
                "living_world_boundary": {
                    "player": player_living_world_boundary,
                    "gm": gm_living_world_boundary,
                    "narrow_player": narrow_living_world_boundary,
                },
                "accessibility": {
                    "keyboard_focus": player_keyboard_focus,
                    "reduced_motion": player_reduced_motion,
                    "interactive_controls": player_interactive_controls,
                    "role_navigation": {
                        "player": player_role_navigation,
                        "narrow_player": narrow_role_navigation,
                    },
                },
                "manifests": {
                    "player": {
                        "url": redact_local_url(manifest_url),
                        "id": manifest_data.get("id"),
                        "start_url": manifest_data.get("start_url"),
                        "scope": manifest_data.get("scope"),
                        "installability_error_count": len(installability_errors),
                        "has_maskable_192": has_maskable_icon(manifest_data, "/icons/icon-192.png"),
                        "has_maskable_512": has_maskable_icon(manifest_data, "/icons/icon-512.png"),
                    },
                    "gm": {
                        "url": redact_local_url(gm_manifest_url),
                        "id": gm_manifest_data.get("id"),
                        "start_url": gm_manifest_data.get("start_url"),
                        "scope": gm_manifest_data.get("scope"),
                        "installability_error_count": len(gm_installability_errors),
                        "has_maskable_192": has_maskable_icon(gm_manifest_data, "/icons/icon-192.png"),
                        "has_maskable_512": has_maskable_icon(gm_manifest_data, "/icons/icon-512.png"),
                    },
                    "query_role_gm": {
                        "url": redact_local_url(gm_query_manifest_url),
                        "installability_error_count": len(gm_query_installability_errors),
                    },
                },
                "standalone_install_ui": {
                    "player_button": standalone_player_state["buttonText"],
                    "gm_button": standalone_gm_state["buttonText"],
                },
                "screenshots": {
                    "player": str(screenshot_path.relative_to(ROOT)),
                    "gm": str(gm_screenshot_path.relative_to(ROOT)),
                    "narrow_player": str(narrow_screenshot_path.relative_to(ROOT)),
                    "player_mode": screenshot_mode,
                    "gm_mode": gm_screenshot_mode,
                    "narrow_player_mode": narrow_screenshot_mode,
                },
            }
        )

        print("mobile_pwa_viewport_smoke ok")
        print("  viewport: 390x844 player lane")
        print("  narrow_viewport: 360x740 player lane")
        print("  gm_viewport: 390x844 gm lane")
        print(f"  overflow_free: {str(not metrics['hasHorizontalOverflow']).lower()}")
        print(f"  gm_overflow_free: {str(not gm_metrics['hasHorizontalOverflow']).lower()}")
        print(f"  gm_key_bounds: {str(not gm_metrics['incoherentOverflow']).lower()}")
        print(f"  narrow_overflow_free: {str(not narrow_metrics['hasHorizontalOverflow']).lower()}")
        print(f"  narrow_key_bounds: {str(not narrow_metrics['incoherentOverflow']).lower()}")
        print(f"  quick_lane_priority: now {metrics['nowCardTop']:.1f} / trust {metrics['trustCardTop']:.1f}")
        print(f"  gm_quick_lane_priority: now {gm_metrics['nowCardTop']:.1f} / trust {gm_metrics['trustCardTop']:.1f}")
        print(f"  compact_layout: actions {metrics['actionColumns']} col / odds {metrics['oddsColumns']} col")
        print(f"  gm_compact_layout: actions {gm_metrics['actionColumns']} col / odds {gm_metrics['oddsColumns']} col")
        print(f"  narrow_compact_layout: actions {narrow_metrics['actionColumns']} col / odds {narrow_metrics['oddsColumns']} col / glance {narrow_metrics['glanceColumns']} col")
        print(f"  gm_touch_target_min: {gm_metrics['minTouchTarget']:.1f}px")
        print(f"  narrow_touch_target_min: {narrow_metrics['minTouchTarget']:.1f}px")
        print(f"  accessibility_keyboard_focus: visible / {player_keyboard_focus['outlineWidth']} / {player_keyboard_focus['roleName']}")
        print(f"  accessibility_reduced_motion: reduce / {player_reduced_motion['rootScrollBehavior']} / {player_reduced_motion['transitionDurationMs']:.3f}ms")
        print(f"  accessibility_narrow_role_navigation: navigation / {narrow_role_navigation['currentCount']} current / {narrow_role_navigation['currentRole']}")
        print(f"  status_pill_style: {narrow_metrics['statusColor']} / {narrow_metrics['statusBorderColor']}")
        print(f"  quick_glance: ammo {metrics['ammoValue']} / anchor {metrics['anchorValue']}")
        print(f"  gm_quick_glance: ammo {gm_metrics['ammoValue']} / anchor {gm_metrics['anchorValue']}")
        print(f"  manifest_url: {redact_local_url(manifest_url)}")
        print(f"  manifest_scope: {manifest_data.get('scope')}")
        print(f"  gm_manifest_url: {redact_local_url(gm_manifest_url)}")
        print(f"  gm_manifest_scope: {gm_manifest_data.get('scope')}")
        print("  manifest_icon_purpose: any maskable")
        print("  gm_manifest_icon_purpose: any maskable")
        print(f"  installability_errors: {len(installability_errors)}")
        print(f"  gm_installability_errors: {len(gm_installability_errors)}")
        print("  query_role_manifest: player / gm")
        print(f"  gm_query_manifest_url: {redact_local_url(gm_query_manifest_url)}")
        print(f"  gm_query_installability_errors: {len(gm_query_installability_errors)}")
        print("  standalone_install_ui: player / gm")
        print(f"  standalone_install_button: player {standalone_player_state['buttonText']} / gm {standalone_gm_state['buttonText']}")
        print(f"  safe_area_padding: top {metrics['shellPaddingTop']} / bottom {metrics['shellPaddingBottom']}")
        print(f"  screenshot: {screenshot_path}")
        print(f"  gm_screenshot: {gm_screenshot_path}")
        print(f"  narrow_screenshot: {narrow_screenshot_path}")
        print(f"  screenshot_mode: player {screenshot_mode} / gm {gm_screenshot_mode} / narrow {narrow_screenshot_mode}")
        return 0
    except Exception as error:  # noqa: BLE001
        print(f"mobile_pwa_viewport_smoke failed during {current_step}: {error}", file=sys.stderr)
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


def main_grant_boundaries() -> int:
    process: subprocess.Popen[str] | None = None
    browser = None
    current_step = "boot"
    current_page: Page | None = None
    try:
        process, base_url = start_server()
        with sync_playwright() as playwright:
            browser = playwright.chromium.launch(headless=True)

            manifest_receipts: dict[str, dict[str, object]] = {}
            public_layouts: dict[str, dict[str, object]] = {}
            for mode, path in (("player", "/mobile/player"), ("gm", "/mobile/gm"), ("observer", "/mobile/observer")):
                current_step = f"{mode} public install viewport"
                page = browser.new_page(viewport={"width": 390, "height": 844}, is_mobile=True)
                current_page = page
                response = page.goto(
                    f"{base_url}{path}?sessionId=forged-session&role=GameMaster&deviceId=forged-device",
                    wait_until="load",
                    timeout=60_000,
                )
                assert_true(response is not None and response.status == 200, f"{mode} install page must remain public")
                page.wait_for_selector("[data-play-surface='install-only'][data-authority='none']", timeout=60_000)
                layout = page.evaluate(
                    """() => {
                        const button = document.getElementById('turn-install-button');
                        const rect = button?.getBoundingClientRect();
                        return {
                            overflowFree: document.documentElement.scrollWidth <= window.innerWidth,
                            hasLiveRoot: document.querySelector('[data-turn-root]') !== null,
                            hasLiveRuntime: document.querySelector('script[src="/mobile-turn-companion.js"]') !== null,
                            authority: document.querySelector('[data-play-surface="install-only"]')?.getAttribute('data-authority') || '',
                            installTouchTarget: rect ? Math.min(rect.width, rect.height) : 0,
                            body: document.body.innerText || ''
                        };
                    }"""
                )
                assert_true(layout["overflowFree"] is True, f"{mode} public install page must not overflow at 390px")
                assert_true(layout["hasLiveRoot"] is False and layout["hasLiveRuntime"] is False, f"{mode} install page must not render or load live state")
                assert_true(layout["authority"] == "none", f"{mode} install label must confer no authority")
                assert_true(float(layout["installTouchTarget"]) >= 43, f"{mode} install action must meet the touch-target floor")
                assert_true("forged-session" not in layout["body"] and "forged-device" not in layout["body"], f"{mode} install page must not expose query identifiers")
                public_layouts[mode] = {key: value for key, value in layout.items() if key != "body"}

                cdp_session = page.context.new_cdp_session(page)
                cdp_session.send("Page.enable")
                manifest = cdp_session.send("Page.getAppManifest")
                manifest_errors = manifest.get("errors", [])
                manifest_data = json.loads(str(manifest.get("data") or "{}"))
                installability_errors = cdp_session.send("Page.getInstallabilityErrors").get("installabilityErrors", [])
                expected_url = f"/manifest.{mode}.webmanifest"
                expected_id = path
                assert_true(str(manifest.get("url") or "").endswith(expected_url), f"{mode} install page must advertise its role-label manifest")
                assert_true(not manifest_errors, f"{mode} manifest must parse without errors")
                assert_true(not installability_errors, f"{mode} manifest must remain installable")
                assert_true(manifest_data.get("id") == expected_id, f"{mode} manifest id must remain canonical")
                assert_true(manifest_data.get("start_url") == expected_id, f"{mode} manifest start_url must not contain authority parameters")
                assert_true(manifest_data.get("scope") == "/mobile/", f"{mode} manifest must remain in the mobile scope")
                manifest_receipts[mode] = {
                    "url": redact_local_url(manifest.get("url") or ""),
                    "id": manifest_data.get("id"),
                    "start_url": manifest_data.get("start_url"),
                    "scope": manifest_data.get("scope"),
                    "installability_error_count": len(installability_errors),
                    "has_maskable_192": has_maskable_icon(manifest_data, "/icons/icon-192.png"),
                    "has_maskable_512": has_maskable_icon(manifest_data, "/icons/icon-512.png"),
                }
                page.close()

            current_step = "desktop public install layout"
            desktop_page = browser.new_page(viewport={"width": 1280, "height": 800})
            current_page = desktop_page
            desktop_page.goto(f"{base_url}/mobile/player", wait_until="load", timeout=60_000)
            desktop_page.wait_for_selector("[data-play-surface='install-only']", timeout=60_000)
            desktop_layout = desktop_page.evaluate(
                """() => ({
                    overflowFree: document.documentElement.scrollWidth <= window.innerWidth,
                    gridColumns: getComputedStyle(document.querySelector('.install-boundary-grid')).gridTemplateColumns.split(' ').filter(Boolean).length
                })"""
            )
            assert_true(desktop_layout["overflowFree"] is True, "desktop public install page must not overflow")
            assert_true(int(desktop_layout["gridColumns"]) >= 3, "desktop public install page must use the wide three-step layout")
            desktop_page.close()

            current_step = "server-granted live viewport"
            grant_headers = {
                "X-Chummer-Play-Grant-Id": "viewport-smoke-grant-0001",
                "X-Chummer-Play-Grant-Session-Id": SESSION_ID,
                "X-Chummer-Play-Grant-Role": ROLE_NAME,
                "X-Chummer-Play-Grant-Device-Id": DEVICE_ID,
            }
            live_context = browser.new_context(
                viewport={"width": 390, "height": 844},
                is_mobile=True,
                service_workers="block",
                extra_http_headers=grant_headers,
            )
            live_page = live_context.new_page()
            current_page = live_page
            live_page.goto(f"{base_url}/mobile/live?role=GameMaster", wait_until="domcontentloaded", timeout=60_000)
            live_page.wait_for_selector("[data-turn-root][data-session-grant-backed='true']", timeout=60_000)
            live_page.wait_for_selector("#turn-glance-grid", timeout=60_000)
            live_layout = live_page.evaluate(
                """() => {
                    const root = document.querySelector('[data-turn-root]');
                    const glanceGrid = document.getElementById('turn-glance-grid');
                    const actionGrid = document.getElementById('turn-action-grid');
                    const touchTargets = Array.from(document.querySelectorAll('.role-button, .primary-button, .secondary-button, .jump-chip, .stepper button, .field-input'))
                        .map((element) => element.getBoundingClientRect())
                        .filter((rect) => rect.width > 0 && rect.height > 0);
                    return {
                        overflowFree: document.documentElement.scrollWidth <= window.innerWidth,
                        sessionId: root?.getAttribute('data-session-id') || '',
                        role: root?.getAttribute('data-role') || '',
                        glanceCount: glanceGrid?.children.length || 0,
                        actionColumns: actionGrid ? getComputedStyle(actionGrid).gridTemplateColumns.split(' ').filter(Boolean).length : 0,
                        minTouchTarget: touchTargets.reduce((minimum, rect) => Math.min(minimum, rect.width, rect.height), Number.POSITIVE_INFINITY)
                    };
                }"""
            )
            assert_true(live_layout["overflowFree"] is True, "live companion must not overflow at 390px")
            assert_true(live_layout["sessionId"] == SESSION_ID and live_layout["role"] == ROLE_NAME, "live viewport must use its server grant")
            assert_true(live_layout["glanceCount"] == 6, "live phone layout must keep the six-card glance rail")
            assert_true(live_layout["actionColumns"] == 1, "live phone layout must collapse bounded actions to one column")
            assert_true(float(live_layout["minTouchTarget"]) >= 43, "live phone controls must meet the touch-target floor")
            live_context.close()

            browser.close()
            browser = None

        write_viewport_receipt(
            {
                "contract_name": "chummer_play.mobile_pwa_viewport_smoke.v2",
                "status": "pass",
                "generated_at_utc": datetime.now(UTC).replace(microsecond=0).isoformat().replace("+00:00", "Z"),
                "public_install_boundary": {
                    "authority": "none",
                    "query_parameters_grant_access": False,
                    "phone_layouts": public_layouts,
                    "desktop_layout": desktop_layout,
                },
                "live_session_boundary": {
                    "route": "/mobile/live",
                    "grant_source": "trusted_server_headers",
                    "phone_layout": live_layout,
                },
                "manifests": manifest_receipts,
            }
        )
        print("mobile_pwa_viewport_smoke ok")
        print("  public_install_phone_layouts: player / gm / observer")
        print("  public_install_desktop_layout: 3 columns")
        print("  public_authority: none")
        print("  manifest_start_urls: clean player / gm / observer")
        print("  live_session_viewport: 390x844 /mobile/live")
        print("  live_grant_source: trusted_server_headers")
        print(f"  live_touch_target_min: {live_layout['minTouchTarget']:.1f}px")
        return 0
    except Exception as error:  # noqa: BLE001
        print(f"mobile_pwa_viewport_smoke failed during {current_step}: {error}", file=sys.stderr)
        if current_page is not None:
            try:
                print(f"current_url: {current_page.url}", file=sys.stderr)
                print(current_page.content()[:1000], file=sys.stderr)
            except Exception:  # noqa: BLE001
                pass
        return 1
    finally:
        if browser is not None:
            try:
                browser.close()
            except Exception:  # noqa: BLE001
                pass
        if process is not None:
            stop_server(process)


if __name__ == "__main__":
    raise SystemExit(main_grant_boundaries())
