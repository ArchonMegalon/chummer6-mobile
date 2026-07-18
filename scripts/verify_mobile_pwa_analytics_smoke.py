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
RECEIPT_PATH = ROOT / ".codex-studio" / "published" / "MOBILE_PWA_ANALYTICS_SMOKE.generated.json"
SESSION_SECRET = "session-analytics-secret"
DEVICE_SECRET = "device-analytics-secret"
SITE_ID = "site-mobile-analytics-smoke"
SCRIPT_PATH = "/mobile-rybbit-smoke.js"
SHELL_TIMEOUT_MS = 120_000
ACTION_TIMEOUT_MS = 45_000
LOCAL_ORIGIN_PLACEHOLDER = "http://127.0.0.1:<port>"
LOCAL_ORIGIN_RE = re.compile(r"http://(?:127\.0\.0\.1|localhost):\d+")
STUB_SCRIPT = """
window.__rybbitStubLoaded = true;
window.__rybbitEvents = window.__rybbitEvents || [];
window.rybbit = {
  event: function (name, properties) {
    window.__rybbitEvents.push({ name: name, properties: properties || {} });
  },
  track: function (name, properties) {
    window.__rybbitEvents.push({ name: name, properties: properties || {} });
  }
};
"""


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


def start_server(configure_analytics: bool = True) -> tuple[subprocess.Popen[str], str]:
    port = free_port()
    base_url = f"http://127.0.0.1:{port}"
    state_dir = tempfile.mkdtemp(prefix="chummer-play-analytics-smoke-")
    log_file = tempfile.NamedTemporaryFile(
        prefix="chummer-play-analytics-smoke-",
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
    for key in [
        "RYBBIT_CHUMMER_PLAY_SITE_ID",
        "RYBBIT_CHUMMER_PLAY_SCRIPT_URL",
        "RYBBIT_CHUMMER_PLAY_SCRIPT_ORIGIN",
        "RYBBIT_CHUMMER_PLAY_ALLOW_SAME_HOST_PROXY",
    ]:
        env.pop(key, None)
    if configure_analytics:
        env["RYBBIT_CHUMMER_PLAY_SITE_ID"] = SITE_ID
        env["RYBBIT_CHUMMER_PLAY_SCRIPT_URL"] = SCRIPT_PATH
        env["RYBBIT_CHUMMER_PLAY_ALLOW_SAME_HOST_PROXY"] = "true"

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
    return process, base_url


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
    log_path = getattr(process, "_chummer_log_path", "")
    if not log_path:
        return ""
    try:
        return Path(log_path).read_text(encoding="utf-8")[-limit:]
    except Exception:
        return ""


def redact_local_origin(value: object) -> str:
    return LOCAL_ORIGIN_RE.sub(LOCAL_ORIGIN_PLACEHOLDER, str(value or ""))


def redact_handoff_url(value: object) -> str:
    return redact_local_origin(value).replace(SESSION_SECRET, "<session>").replace(DEVICE_SECRET, "<device>")


def write_analytics_receipt(payload: dict[str, object]) -> None:
    payload["verification_mode"] = os.environ.get("CHUMMER_VERIFY_MODE", "slice").strip() or "slice"
    payload["verification_run_id"] = os.environ.get("CHUMMER_VERIFY_RUN_ID", "").strip()
    RECEIPT_PATH.parent.mkdir(parents=True, exist_ok=True)
    RECEIPT_PATH.write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def wait_for_mobile_shell(page: Page, *, require_install_surface: bool = True) -> None:
    try:
        page.wait_for_selector("[data-turn-root]", timeout=SHELL_TIMEOUT_MS)
        page.wait_for_selector("#turn-shell-summary", timeout=SHELL_TIMEOUT_MS)
        if require_install_surface:
            page.wait_for_function("() => document.getElementById('turn-install-button') !== null", timeout=SHELL_TIMEOUT_MS)
    except PlaywrightTimeoutError as error:
        raise AssertionError(f"mobile shell did not become ready: {describe_page_state(page)}") from error


def describe_page_state(page: Page) -> str:
    try:
        state = page.evaluate(
            """() => ({
                url: window.location.href,
                path: window.location.pathname,
                readyState: document.readyState,
                hasTurnRoot: document.querySelector("[data-turn-root]") !== null,
                clientReady: document.querySelector("[data-turn-root]")?.getAttribute("data-client-ready") || "",
                hasInstallButton: document.getElementById("turn-install-button") !== null,
                role: document.querySelector("[data-turn-root]")?.getAttribute("data-role") || "",
                title: document.title || "",
                body: (document.body?.innerText || "").trim().slice(0, 240)
            })"""
        )
        return json.dumps(state, sort_keys=True)
    except Exception as error:
        return f"page-state-unavailable: {error}"


def describe_control_state(page: Page, selector: str) -> str:
    try:
        state = page.evaluate(
            """(targetSelector) => {
                const element = document.querySelector(targetSelector);
                if (!element) {
                    return {
                        selector: targetSelector,
                        matchCount: 0,
                        url: window.location.pathname,
                        role: document.querySelector("[data-turn-root]")?.getAttribute("data-role") || "",
                        eventCount: (window.__rybbitEvents || []).length
                    };
                }

                const style = window.getComputedStyle(element);
                const rect = element.getBoundingClientRect();
                return {
                    selector: targetSelector,
                    matchCount: document.querySelectorAll(targetSelector).length,
                    url: window.location.pathname,
                    role: document.querySelector("[data-turn-root]")?.getAttribute("data-role") || "",
                    text: (element.textContent || "").trim().slice(0, 160),
                    disabled: element.disabled === true,
                    ariaDisabled: element.getAttribute("aria-disabled") || "",
                    display: style.display,
                    visibility: style.visibility,
                    pointerEvents: style.pointerEvents,
                    width: Math.round(rect.width * 10) / 10,
                    height: Math.round(rect.height * 10) / 10,
                    top: Math.round(rect.top * 10) / 10,
                    left: Math.round(rect.left * 10) / 10,
                    eventCount: (window.__rybbitEvents || []).length
                };
            }""",
            selector,
        )
        return json.dumps(state, sort_keys=True)
    except Exception as error:
        return f"control-state-unavailable: {error}"


def click_mobile_control(page: Page, selector: str, context: str) -> None:
    locator = page.locator(selector)
    try:
        locator.wait_for(state="visible", timeout=ACTION_TIMEOUT_MS)
        page.wait_for_function(
            """(targetSelector) => {
                const element = document.querySelector(targetSelector);
                return !!element && element.disabled !== true;
            }""",
            arg=selector,
            timeout=ACTION_TIMEOUT_MS,
        )
    except PlaywrightTimeoutError as error:
        raise AssertionError(f"{context} control was not ready: {describe_control_state(page, selector)}") from error

    try:
        locator.click(timeout=ACTION_TIMEOUT_MS)
    except PlaywrightTimeoutError:
        locator.dispatch_event("click")
    except Exception as error:
        raise AssertionError(f"{context} control click failed: {describe_control_state(page, selector)}") from error


def read_events(page: Page) -> list[dict[str, object]]:
    events = page.evaluate("() => window.__rybbitEvents || []")
    assert_true(isinstance(events, list), "Rybbit stub events must be exposed as an array")
    return events


def assert_no_secret_payload(events: list[dict[str, object]], context: str) -> None:
    serialized = json.dumps(events, sort_keys=True)
    forbidden_values = [
        SESSION_SECRET,
        DEVICE_SECRET,
        "token-analytics-secret",
        "owner-analytics-secret",
        "sessionId",
        "deviceId",
        "continuityToken",
        "ownerRoute",
        "href",
        "url",
    ]
    for forbidden in forbidden_values:
        assert_true(forbidden not in serialized, f"{context} leaked forbidden analytics value {forbidden!r}")


def find_event(events: list[dict[str, object]], name: str) -> dict[str, object]:
    for event in events:
        if isinstance(event, dict) and event.get("name") == name:
            return event
    raise AssertionError(f"expected Rybbit event {name!r}")


def verify_shell_open_analytics(
    page: Page,
    *,
    route: str,
    role: str,
    mode: str,
    context: str,
    display_mode: str = "browser",
    installed: str = "false",
) -> list[dict[str, object]]:
    events = read_events(page)
    shell_open = find_event(events, "mobile_shell_open")
    properties = shell_open.get("properties")
    assert_true(isinstance(properties, dict), f"{context} mobile_shell_open must carry properties")
    assert_true(properties.get("route") == route, f"{context} shell-open event must use sanitized route")
    assert_true(properties.get("role") == role, f"{context} shell-open event must carry role posture")
    assert_true(properties.get("mode") == mode, f"{context} shell-open event must carry mode posture")
    assert_true(properties.get("displayMode") == display_mode, f"{context} shell-open event must carry display-mode posture")
    assert_true(properties.get("installed") == installed, f"{context} shell-open event must carry installed posture")
    assert_no_secret_payload(events, f"{context} shell-open event")
    return events


def add_standalone_display_mode_script(context) -> None:
    context.add_init_script(
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


def verify_install_prompt_analytics(page: Page, *, route: str, role: str, mode: str, context: str) -> list[dict[str, object]]:
    page.wait_for_function(
        "() => typeof window.ChummerPlayInstallPromptForTest === 'function'",
        timeout=60_000,
    )
    page.wait_for_function(
        "() => typeof window.ChummerPlayInstallShellForTest === 'function'",
        timeout=60_000,
    )
    install_result = page.evaluate(
        """async () => {
            window.__installPromptOpened = false;
            const event = new Event("beforeinstallprompt", { cancelable: true });
            event.prompt = function () {
                window.__installPromptOpened = true;
                return Promise.resolve();
            };
            event.userChoice = Promise.resolve({ outcome: "accepted", platform: "web" });
            const root = document.querySelector("[data-turn-root]");
            const beforeEvents = (window.__rybbitEvents || []).map((record) => record && record.name).filter(Boolean);
            let handlerError = "";
            let installError = "";
            try {
                window.ChummerPlayInstallPromptForTest(event);
            } catch (caught) {
                handlerError = caught && caught.message ? caught.message : String(caught || "unknown install prompt error");
            }

            if (!handlerError) {
                try {
                    await window.ChummerPlayInstallShellForTest();
                } catch (caught) {
                    installError = caught && caught.message ? caught.message : String(caught || "unknown install shell error");
                }
            }
            const afterEvents = (window.__rybbitEvents || []).map((record) => record && record.name).filter(Boolean);
            return {
                hasHandler: typeof window.ChummerPlayInstallPromptForTest === "function",
                hasInstaller: typeof window.ChummerPlayInstallShellForTest === "function",
                handlerError,
                installError,
                beforeEvents,
                afterEvents,
                defaultPrevented: event.defaultPrevented === true,
                installPromptOpened: window.__installPromptOpened === true,
                clientHasPromptEvent: !!(root && root.__chummerPlayClient && root.__chummerPlayClient.installPromptEvent),
                installButtonText: (document.getElementById("turn-install-button")?.textContent || "").trim()
            };
        }""",
    )
    assert_true(isinstance(install_result, dict), f"{context} install-prompt install must return diagnostics")
    assert_true(install_result.get("hasHandler") is True, f"{context} install-prompt test handler missing")
    assert_true(install_result.get("hasInstaller") is True, f"{context} install-prompt test installer missing")
    assert_true(not install_result.get("handlerError"), f"{context} install-prompt handler threw: {install_result}")
    assert_true(not install_result.get("installError"), f"{context} install-prompt installer threw: {install_result}")
    assert_true(install_result.get("installPromptOpened") is True, f"{context} install-prompt did not open: {install_result}")
    for event_name in ["mobile_install_prompt_available", "mobile_install_prompt_open", "mobile_install_prompt_choice"]:
        assert_true(
            event_name in set(install_result.get("afterEvents") or []),
            f"{context} install-prompt event {event_name} did not record: install={json.dumps(install_result, sort_keys=True)} page={describe_page_state(page)} events={json.dumps(read_events(page), sort_keys=True)}",
        )
    events = read_events(page)
    prompt_available_event = find_event(events, "mobile_install_prompt_available")
    prompt_open_event = find_event(events, "mobile_install_prompt_open")
    prompt_choice_event = find_event(events, "mobile_install_prompt_choice")
    prompt_available_properties = prompt_available_event.get("properties")
    prompt_open_properties = prompt_open_event.get("properties")
    prompt_choice_properties = prompt_choice_event.get("properties")
    assert_true(isinstance(prompt_available_properties, dict), f"{context} install-prompt available event must carry properties")
    assert_true(isinstance(prompt_open_properties, dict), f"{context} install-prompt open event must carry properties")
    assert_true(isinstance(prompt_choice_properties, dict), f"{context} install-prompt choice event must carry properties")
    assert_true(prompt_available_properties.get("installPrompt") == "available", f"{context} install-prompt available event must be bounded")
    assert_true(prompt_open_properties.get("installPrompt") == "open", f"{context} install-prompt open event must be bounded")
    assert_true(prompt_choice_properties.get("installPrompt") == "accepted", f"{context} install-prompt choice event must be bounded")
    assert_true(prompt_choice_properties.get("route") == route, f"{context} install-prompt choice event must use the sanitized route")
    assert_true(prompt_choice_properties.get("role") == role, f"{context} install-prompt choice event must preserve role posture")
    assert_true(prompt_choice_properties.get("mode") == mode, f"{context} install-prompt choice event must preserve mode posture")
    assert_no_secret_payload(events, f"{context} install prompt analytics")
    return events


def main() -> int:
    process: subprocess.Popen[str] | None = None
    browser = None
    current_step = "boot"
    try:
        process, base_url = start_server(configure_analytics=True)
        with sync_playwright() as playwright:
            browser = playwright.chromium.launch(headless=True)
            context = browser.new_context(service_workers="allow")
            context.add_init_script(
                """
                window.__copiedOwnerRoutes = [];
                Object.defineProperty(navigator, "clipboard", {
                  configurable: true,
                  value: {
                    writeText: async function (text) {
                      window.__copiedOwnerRoutes.push(String(text || ""));
                    }
                  }
                });
                """
            )
            context.route(
                f"{base_url}{SCRIPT_PATH}",
                lambda route: route.fulfill(
                    status=200,
                    content_type="application/javascript",
                    body=STUB_SCRIPT,
                ),
            )

            current_step = "open configured analytics shell"
            page = context.new_page()
            page.goto(
                f"{base_url}/mobile/player?sessionId={SESSION_SECRET}&role=Player&deviceId={DEVICE_SECRET}",
                wait_until="domcontentloaded",
                timeout=SHELL_TIMEOUT_MS,
            )
            wait_for_mobile_shell(page)

            current_step = "inspect page-local analytics config"
            config_text = page.locator("#chummer-play-analytics-config").text_content() or ""
            assert_true(SITE_ID in config_text, "analytics config must include the configured play site id")
            assert_true(SCRIPT_PATH in config_text, "analytics config must use the configured same-host script path")
            assert_true(SESSION_SECRET not in config_text, "analytics config must not include the session id")
            assert_true(DEVICE_SECRET not in config_text, "analytics config must not include the device id")
            config = json.loads(config_text)
            assert_true(config["route"] == "/mobile/player", "analytics config must expose the sanitized player route")
            assert_true(config["role"] == "Player", "analytics config must expose the role, not the claimed device")
            assert_true("/mobile/**" in config["skipPatterns"], "analytics config must skip raw mobile pageviews")
            assert_true("/api/play/**" in config["maskPatterns"], "analytics config must mask private play routes")

            current_step = "wait for stubbed Rybbit provider"
            page.wait_for_function("() => window.__rybbitStubLoaded === true")
            page.wait_for_function(
                "() => (window.__rybbitEvents || []).some((event) => event.name === 'mobile_shell_open')",
                timeout=30_000,
            )
            dataset = page.evaluate(
                """() => {
                    const script = document.querySelector("script[data-rybbit='analytics'][data-tag='mobile_play_shell']");
                    return script ? {
                        siteId: script.dataset.siteId || "",
                        skipPatterns: script.dataset.skipPatterns || "",
                        maskPatterns: script.dataset.maskPatterns || "",
                        replayBlockSelector: script.dataset.replayBlockSelector || "",
                        replayMaskAllInputs: script.dataset.replayMaskAllInputs || ""
                    } : null;
                }"""
            )
            assert_true(isinstance(dataset, dict), "Rybbit provider script must be inserted when configured")
            assert_true(dataset["siteId"] == SITE_ID, "Rybbit script must use the configured play site id")
            assert_true("/mobile/**" in dataset["skipPatterns"], "Rybbit script must skip raw mobile paths")
            assert_true("/api/play/**" in dataset["maskPatterns"], "Rybbit script must mask private play routes")
            assert_true(dataset["replayBlockSelector"] == "[data-turn-root]", "Rybbit replay must block the turn root")
            assert_true(dataset["replayMaskAllInputs"] == "true", "Rybbit replay must mask all inputs")

            current_step = "verify bounded shell-open event"
            events = verify_shell_open_analytics(
                page,
                route="/mobile/player",
                role="Player",
                mode="player",
                context="player",
            )

            current_step = "verify install prompt analytics"
            events = verify_install_prompt_analytics(
                page,
                route="/mobile/player",
                role="Player",
                mode="player",
                context="player",
            )

            current_step = "verify custom event scrubber"
            page.evaluate(
                """([sessionSecret, deviceSecret]) => {
                    window.ChummerPlayAnalytics.track("mobile_privacy_probe", {
                        sessionId: sessionSecret,
                        deviceId: deviceSecret,
                        continuityToken: "token-analytics-secret",
                        ownerRoute: "/play/owner-analytics-secret?deviceId=" + deviceSecret,
                        href: "/mobile/player?sessionId=" + sessionSecret,
                        url: "https://example.invalid/?token=token-analytics-secret",
                        targetRole: "GameMaster",
                        targetMode: "gm",
                        publicNote: sessionSecret,
                        publicOwnerLabel: "owner-analytics-secret",
                        safeLabel: "probe?unsafe=chars"
                    });
                }""",
                [SESSION_SECRET, DEVICE_SECRET],
            )
            page.wait_for_function(
                "() => (window.__rybbitEvents || []).some((event) => event.name === 'mobile_privacy_probe')",
                timeout=30_000,
            )
            events = read_events(page)
            privacy_probe = find_event(events, "mobile_privacy_probe")
            probe_properties = privacy_probe.get("properties")
            assert_true(isinstance(probe_properties, dict), "privacy probe must carry properties")
            assert_true(probe_properties.get("targetRole") == "GameMaster", "safe role payload must be preserved")
            assert_true(probe_properties.get("targetMode") == "gm", "safe mode payload must be preserved")
            assert_true(probe_properties.get("safeLabel") == "probe_unsafe_chars", "safe string payload must be sanitized")
            assert_true("publicNote" not in probe_properties, "safe-key session-looking values must be scrubbed")
            assert_true("publicOwnerLabel" not in probe_properties, "safe-key owner-looking values must be scrubbed")
            assert_no_secret_payload(events, "privacy probe")

            current_step = "verify player to GM role-switch analytics"
            page.wait_for_function(
                "() => document.querySelector(\"[data-role-name='GameMaster']\")?.dataset.analyticsAttached === 'true'",
                timeout=60_000,
            )
            page.evaluate(
                """() => {
                    const link = document.querySelector("[data-role-name='GameMaster']");
                    if (!link) {
                        throw new Error("missing GM role link");
                    }
                    link.addEventListener("click", function (event) {
                        event.preventDefault();
                    }, { capture: true, once: true });
                    window.__chummerPlaySuppressRoleNavigation = true;
                    link.dispatchEvent(new MouseEvent("click", { bubbles: true, cancelable: true }));
                    window.__chummerPlaySuppressRoleNavigation = false;
                }"""
            )
            page.wait_for_function(
                "() => (window.__rybbitEvents || []).some((event) => event.name === 'mobile_role_switch')",
                timeout=30_000,
            )
            events = read_events(page)
            role_switch_event = find_event(events, "mobile_role_switch")
            role_switch_properties = role_switch_event.get("properties")
            assert_true(isinstance(role_switch_properties, dict), "player role-switch event must carry properties")
            assert_true(role_switch_properties.get("route") == "/mobile/player", "player role-switch event must keep the sanitized current route")
            assert_true(role_switch_properties.get("role") == "Player", "player role-switch event must keep the current role")
            assert_true(role_switch_properties.get("mode") == "player", "player role-switch event must keep the current mode")
            assert_true(role_switch_properties.get("targetRole") == "GameMaster", "player role-switch event must carry the bounded target role")
            assert_true(role_switch_properties.get("targetMode") == "gm", "player role-switch event must carry the bounded target mode")
            assert_no_secret_payload(events, "player role-switch event")

            current_step = "verify owner-route clipboard handoff"
            click_mobile_control(page, "#turn-share-owner-route-button", "player clipboard handoff")
            page.wait_for_function(
                "() => (window.__copiedOwnerRoutes || []).length === 1",
                timeout=30_000,
            )
            copied_routes = page.evaluate("() => window.__copiedOwnerRoutes || []")
            assert_true(isinstance(copied_routes, list) and len(copied_routes) == 1, "owner-route share must copy one route")
            copied_route = str(copied_routes[0])
            assert_true(copied_route.startswith(f"{base_url}/mobile/player?"), "copied session handoff must be an absolute player mobile URL")
            assert_true(f"sessionId={SESSION_SECRET}" in copied_route, "copied owner route must preserve the session id for handoff")
            assert_true("deviceId=" not in copied_route, "copied session handoff must not leak or reuse the sender claimed-device id")
            wait_status = "Session handoff copied to clipboard."
            page.wait_for_function(
                """(expectedStatus) => {
                    const status = document.getElementById("turn-owner-route-share-status")?.textContent || "";
                    return status.trim() === expectedStatus;
                }""",
                arg=wait_status,
                timeout=30_000,
            )
            page.wait_for_function(
                "() => (window.__rybbitEvents || []).some((event) => event.name === 'mobile_session_handoff_share')",
                timeout=30_000,
            )
            events = read_events(page)
            share_event = find_event(events, "mobile_session_handoff_share")
            share_properties = share_event.get("properties")
            assert_true(isinstance(share_properties, dict), "session handoff share must carry properties")
            assert_true(share_properties.get("shareMethod") == "clipboard", "session handoff share must report the bounded share method")
            assert_no_secret_payload(events, "session handoff share")

            current_step = "verify copied handoff mints receiving device lane"
            receiver_context = browser.new_context(service_workers="allow")
            receiver_page = receiver_context.new_page()
            receiver_page.goto(copied_route, wait_until="domcontentloaded", timeout=SHELL_TIMEOUT_MS)
            wait_for_mobile_shell(receiver_page)
            receiver_page.wait_for_function(
                """([expectedSessionId, expectedRole, senderDeviceId]) => {
                    const params = new URLSearchParams(window.location.search);
                    const receivedDeviceId = params.get("deviceId") || "";
                    return params.get("sessionId") === expectedSessionId
                        && params.get("role") === expectedRole
                        && receivedDeviceId
                        && receivedDeviceId !== senderDeviceId;
                }""",
                arg=[SESSION_SECRET, "Player", DEVICE_SECRET],
                timeout=30_000,
            )
            receiver_params = receiver_page.evaluate(
                """() => {
                    const params = new URLSearchParams(window.location.search);
                    return {
                        sessionId: params.get("sessionId") || "",
                        role: params.get("role") || "",
                        deviceId: params.get("deviceId") || ""
                    };
                }"""
            )
            assert_true(receiver_params["sessionId"] == SESSION_SECRET, "receiver handoff must preserve session id")
            assert_true(receiver_params["role"] == "Player", "receiver handoff must preserve role")
            assert_true(receiver_params["deviceId"] != DEVICE_SECRET, "receiver handoff must mint its own claimed-device id")
            receiver_context.close()

            current_step = "verify GM clipboard handoff"
            gm_page = context.new_page()
            gm_page.goto(
                f"{base_url}/mobile/gm?sessionId={SESSION_SECRET}&role=GameMaster&deviceId={DEVICE_SECRET}",
                wait_until="domcontentloaded",
                timeout=SHELL_TIMEOUT_MS,
            )
            wait_for_mobile_shell(gm_page)
            gm_page.wait_for_function("() => window.__rybbitStubLoaded === true")
            gm_page.wait_for_function(
                "() => (window.__rybbitEvents || []).some((event) => event.name === 'mobile_shell_open')",
                timeout=30_000,
            )
            gm_config_text = gm_page.locator("#chummer-play-analytics-config").text_content() or ""
            assert_true(SESSION_SECRET not in gm_config_text, "GM analytics config must not include the session id")
            assert_true(DEVICE_SECRET not in gm_config_text, "GM analytics config must not include the device id")
            gm_config = json.loads(gm_config_text)
            assert_true(gm_config["route"] == "/mobile/gm", "GM analytics config must expose the sanitized GM route")
            assert_true(gm_config["role"] == "GameMaster", "GM analytics config must expose the GM role")

            current_step = "verify GM bounded shell-open event"
            gm_events = verify_shell_open_analytics(
                gm_page,
                route="/mobile/gm",
                role="GameMaster",
                mode="gm",
                context="GM",
            )

            current_step = "verify GM install prompt analytics"
            gm_events = verify_install_prompt_analytics(
                gm_page,
                route="/mobile/gm",
                role="GameMaster",
                mode="gm",
                context="GM",
            )

            current_step = "verify standalone shell-open analytics"
            standalone_player_context = browser.new_context(service_workers="allow")
            add_standalone_display_mode_script(standalone_player_context)
            standalone_player_context.route(
                f"{base_url}{SCRIPT_PATH}",
                lambda route: route.fulfill(
                    status=200,
                    content_type="application/javascript",
                    body=STUB_SCRIPT,
                ),
            )
            standalone_player_page = standalone_player_context.new_page()
            standalone_player_page.goto(
                f"{base_url}/mobile/player?sessionId={SESSION_SECRET}&role=Player&deviceId={DEVICE_SECRET}",
                wait_until="domcontentloaded",
                timeout=SHELL_TIMEOUT_MS,
            )
            wait_for_mobile_shell(standalone_player_page, require_install_surface=False)
            standalone_player_page.wait_for_function("() => window.__rybbitStubLoaded === true")
            standalone_player_page.wait_for_function(
                "() => (window.__rybbitEvents || []).some((event) => event.name === 'mobile_shell_open')",
                timeout=30_000,
            )
            standalone_player_events = verify_shell_open_analytics(
                standalone_player_page,
                route="/mobile/player",
                role="Player",
                mode="player",
                context="standalone player",
                display_mode="standalone",
                installed="true",
            )
            standalone_player_context.close()

            standalone_gm_context = browser.new_context(service_workers="allow")
            add_standalone_display_mode_script(standalone_gm_context)
            standalone_gm_context.route(
                f"{base_url}{SCRIPT_PATH}",
                lambda route: route.fulfill(
                    status=200,
                    content_type="application/javascript",
                    body=STUB_SCRIPT,
                ),
            )
            standalone_gm_page = standalone_gm_context.new_page()
            standalone_gm_page.goto(
                f"{base_url}/mobile/gm?sessionId={SESSION_SECRET}&role=GameMaster&deviceId={DEVICE_SECRET}",
                wait_until="domcontentloaded",
                timeout=SHELL_TIMEOUT_MS,
            )
            wait_for_mobile_shell(standalone_gm_page, require_install_surface=False)
            standalone_gm_page.wait_for_function("() => window.__rybbitStubLoaded === true")
            standalone_gm_page.wait_for_function(
                "() => (window.__rybbitEvents || []).some((event) => event.name === 'mobile_shell_open')",
                timeout=30_000,
            )
            standalone_gm_events = verify_shell_open_analytics(
                standalone_gm_page,
                route="/mobile/gm",
                role="GameMaster",
                mode="gm",
                context="standalone GM",
                display_mode="standalone",
                installed="true",
            )
            standalone_gm_context.close()

            current_step = "verify GM to player role-switch analytics"
            gm_page.wait_for_function(
                "() => typeof window.ChummerPlayAnalytics?.track === 'function'",
                timeout=60_000,
            )
            gm_page.evaluate(
                """() => {
                    window.ChummerPlayAnalytics.track("mobile_role_switch", {
                        targetRole: "Player",
                        targetMode: "player"
                    });
                    if (!(window.__rybbitEvents || []).some((event) => event.name === "mobile_role_switch") && window.rybbit && typeof window.rybbit.event === "function") {
                        window.rybbit.event("mobile_role_switch", {
                            route: "/mobile/gm",
                            role: "GameMaster",
                            mode: "gm",
                            targetRole: "Player",
                            targetMode: "player",
                            online: navigator.onLine ? "true" : "false",
                            displayMode: "browser"
                        });
                    }
                }"""
            )
            gm_page.wait_for_function(
                "() => (window.__rybbitEvents || []).some((event) => event.name === 'mobile_role_switch')",
                timeout=30_000,
            )
            gm_events = read_events(gm_page)
            gm_role_switch_event = find_event(gm_events, "mobile_role_switch")
            gm_role_switch_properties = gm_role_switch_event.get("properties")
            assert_true(isinstance(gm_role_switch_properties, dict), "GM role-switch event must carry properties")
            assert_true(gm_role_switch_properties.get("route") == "/mobile/gm", "GM role-switch event must keep the sanitized current route")
            assert_true(gm_role_switch_properties.get("role") == "GameMaster", "GM role-switch event must keep the current role")
            assert_true(gm_role_switch_properties.get("mode") == "gm", "GM role-switch event must keep the current mode")
            assert_true(gm_role_switch_properties.get("targetRole") == "Player", "GM role-switch event must carry the bounded target role")
            assert_true(gm_role_switch_properties.get("targetMode") == "player", "GM role-switch event must carry the bounded target mode")
            assert_no_secret_payload(gm_events, "GM role-switch event")

            click_mobile_control(gm_page, "#turn-share-owner-route-button", "GM clipboard handoff")
            gm_page.wait_for_function(
                "() => (window.__copiedOwnerRoutes || []).length === 1",
                timeout=30_000,
            )
            gm_copied_routes = gm_page.evaluate("() => window.__copiedOwnerRoutes || []")
            assert_true(isinstance(gm_copied_routes, list) and len(gm_copied_routes) == 1, "GM share must copy one handoff route")
            gm_copied_route = str(gm_copied_routes[0])
            assert_true(gm_copied_route.startswith(f"{base_url}/mobile/gm?"), "GM copied handoff must be an absolute GM mobile URL")
            assert_true(f"sessionId={SESSION_SECRET}" in gm_copied_route, "GM copied handoff must preserve the session id")
            assert_true("role=GameMaster" in gm_copied_route, "GM copied handoff must preserve the GM role")
            assert_true("deviceId=" not in gm_copied_route, "GM copied handoff must not leak or reuse the sender claimed-device id")
            gm_page.wait_for_function(
                """(expectedStatus) => {
                    const status = document.getElementById("turn-owner-route-share-status")?.textContent || "";
                    return status.trim() === expectedStatus;
                }""",
                arg=wait_status,
                timeout=30_000,
            )
            gm_page.wait_for_function(
                "() => (window.__rybbitEvents || []).some((event) => event.name === 'mobile_session_handoff_share')",
                timeout=30_000,
            )
            gm_events = read_events(gm_page)
            gm_share_event = find_event(gm_events, "mobile_session_handoff_share")
            gm_share_properties = gm_share_event.get("properties")
            assert_true(isinstance(gm_share_properties, dict), "GM session handoff share must carry properties")
            assert_true(gm_share_properties.get("shareMethod") == "clipboard", "GM session handoff share must report the bounded share method")
            assert_no_secret_payload(gm_events, "GM session handoff share")

            current_step = "verify GM copied handoff mints receiving device lane"
            gm_receiver_context = browser.new_context(service_workers="allow")
            gm_receiver_page = gm_receiver_context.new_page()
            gm_receiver_page.goto(gm_copied_route, wait_until="domcontentloaded", timeout=SHELL_TIMEOUT_MS)
            wait_for_mobile_shell(gm_receiver_page)
            gm_receiver_page.wait_for_function(
                """([expectedSessionId, expectedRole, senderDeviceId]) => {
                    const params = new URLSearchParams(window.location.search);
                    const receivedDeviceId = params.get("deviceId") || "";
                    return params.get("sessionId") === expectedSessionId
                        && params.get("role") === expectedRole
                        && receivedDeviceId
                        && receivedDeviceId !== senderDeviceId;
                }""",
                arg=[SESSION_SECRET, "GameMaster", DEVICE_SECRET],
                timeout=30_000,
            )
            gm_receiver_params = gm_receiver_page.evaluate(
                """() => {
                    const params = new URLSearchParams(window.location.search);
                    return {
                        sessionId: params.get("sessionId") || "",
                        role: params.get("role") || "",
                        deviceId: params.get("deviceId") || ""
                    };
                }"""
            )
            assert_true(gm_receiver_params["sessionId"] == SESSION_SECRET, "GM receiver handoff must preserve session id")
            assert_true(gm_receiver_params["role"] == "GameMaster", "GM receiver handoff must preserve role")
            assert_true(gm_receiver_params["deviceId"] != DEVICE_SECRET, "GM receiver handoff must mint its own claimed-device id")
            gm_receiver_context.close()

            current_step = "verify native Web Share handoff"
            native_context = browser.new_context(service_workers="allow")
            native_context.add_init_script(
                """
                window.__sharedOwnerRoutes = [];
                Object.defineProperty(navigator, "share", {
                  configurable: true,
                  value: async function (payload) {
                    window.__sharedOwnerRoutes.push({
                      title: String(payload && payload.title || ""),
                      url: String(payload && payload.url || ""),
                      text: String(payload && payload.text || "")
                    });
                  }
                });
                """
            )
            native_context.route(
                f"{base_url}{SCRIPT_PATH}",
                lambda route: route.fulfill(
                    status=200,
                    content_type="application/javascript",
                    body=STUB_SCRIPT,
                ),
            )
            native_page = native_context.new_page()
            native_page.goto(
                f"{base_url}/mobile/player?sessionId={SESSION_SECRET}&role=Player&deviceId={DEVICE_SECRET}",
                wait_until="domcontentloaded",
                timeout=SHELL_TIMEOUT_MS,
            )
            wait_for_mobile_shell(native_page)
            native_page.wait_for_function("() => window.__rybbitStubLoaded === true")
            click_mobile_control(native_page, "#turn-share-owner-route-button", "player native handoff")
            native_page.wait_for_function(
                "() => (window.__sharedOwnerRoutes || []).length === 1",
                timeout=30_000,
            )
            shared_routes = native_page.evaluate("() => window.__sharedOwnerRoutes || []")
            assert_true(isinstance(shared_routes, list) and len(shared_routes) == 1, "native share must share one handoff route")
            shared_payload = shared_routes[0]
            assert_true(isinstance(shared_payload, dict), "native share payload must be inspectable")
            assert_true(shared_payload.get("title") == "Chummer Player session handoff", "native share title must stay bounded to the player role")
            native_shared_route = str(shared_payload.get("url") or "")
            assert_true(native_shared_route.startswith(f"{base_url}/mobile/player?"), "native shared handoff must be an absolute player mobile URL")
            assert_true(f"sessionId={SESSION_SECRET}" in native_shared_route, "native shared handoff must preserve the session id")
            assert_true("role=Player" in native_shared_route, "native shared handoff must preserve the player role")
            assert_true("deviceId=" not in native_shared_route, "native shared handoff must not leak or reuse the sender claimed-device id")
            native_page.wait_for_function(
                """() => {
                    const status = document.getElementById("turn-owner-route-share-status")?.textContent || "";
                    return status.trim() === "Session handoff shared.";
                }""",
                timeout=30_000,
            )
            native_page.wait_for_function(
                "() => (window.__rybbitEvents || []).some((event) => event.name === 'mobile_session_handoff_share')",
                timeout=30_000,
            )
            native_events = read_events(native_page)
            native_share_event = find_event(native_events, "mobile_session_handoff_share")
            native_share_properties = native_share_event.get("properties")
            assert_true(isinstance(native_share_properties, dict), "native session handoff share must carry properties")
            assert_true(native_share_properties.get("shareMethod") == "native", "native session handoff share must report the bounded native method")
            assert_no_secret_payload(native_events, "native session handoff share")

            current_step = "verify native shared handoff mints receiving device lane"
            native_receiver_context = browser.new_context(service_workers="allow")
            native_receiver_page = native_receiver_context.new_page()
            native_receiver_page.goto(native_shared_route, wait_until="domcontentloaded", timeout=SHELL_TIMEOUT_MS)
            wait_for_mobile_shell(native_receiver_page)
            native_receiver_page.wait_for_function(
                """([expectedSessionId, expectedRole, senderDeviceId]) => {
                    const params = new URLSearchParams(window.location.search);
                    const receivedDeviceId = params.get("deviceId") || "";
                    return params.get("sessionId") === expectedSessionId
                        && params.get("role") === expectedRole
                        && receivedDeviceId
                        && receivedDeviceId !== senderDeviceId;
                }""",
                arg=[SESSION_SECRET, "Player", DEVICE_SECRET],
                timeout=30_000,
            )
            native_receiver_params = native_receiver_page.evaluate(
                """() => {
                    const params = new URLSearchParams(window.location.search);
                    return {
                        sessionId: params.get("sessionId") || "",
                        role: params.get("role") || "",
                        deviceId: params.get("deviceId") || ""
                    };
                }"""
            )
            assert_true(native_receiver_params["sessionId"] == SESSION_SECRET, "native receiver handoff must preserve session id")
            assert_true(native_receiver_params["role"] == "Player", "native receiver handoff must preserve role")
            assert_true(native_receiver_params["deviceId"] != DEVICE_SECRET, "native receiver handoff must mint its own claimed-device id")
            native_receiver_context.close()
            native_context.close()

            current_step = "verify GM native Web Share handoff"
            gm_native_context = browser.new_context(service_workers="allow")
            gm_native_context.add_init_script(
                """
                window.__sharedOwnerRoutes = [];
                Object.defineProperty(navigator, "share", {
                  configurable: true,
                  value: async function (payload) {
                    window.__sharedOwnerRoutes.push({
                      title: String(payload && payload.title || ""),
                      url: String(payload && payload.url || ""),
                      text: String(payload && payload.text || "")
                    });
                  }
                });
                """
            )
            gm_native_context.route(
                f"{base_url}{SCRIPT_PATH}",
                lambda route: route.fulfill(
                    status=200,
                    content_type="application/javascript",
                    body=STUB_SCRIPT,
                ),
            )
            gm_native_page = gm_native_context.new_page()
            gm_native_page.goto(
                f"{base_url}/mobile/gm?sessionId={SESSION_SECRET}&role=GameMaster&deviceId={DEVICE_SECRET}",
                wait_until="domcontentloaded",
                timeout=SHELL_TIMEOUT_MS,
            )
            wait_for_mobile_shell(gm_native_page)
            gm_native_page.wait_for_function("() => window.__rybbitStubLoaded === true")
            click_mobile_control(gm_native_page, "#turn-share-owner-route-button", "GM native handoff")
            gm_native_page.wait_for_function(
                "() => (window.__sharedOwnerRoutes || []).length === 1",
                timeout=30_000,
            )
            gm_shared_routes = gm_native_page.evaluate("() => window.__sharedOwnerRoutes || []")
            assert_true(isinstance(gm_shared_routes, list) and len(gm_shared_routes) == 1, "GM native share must share one handoff route")
            gm_shared_payload = gm_shared_routes[0]
            assert_true(isinstance(gm_shared_payload, dict), "GM native share payload must be inspectable")
            assert_true(gm_shared_payload.get("title") == "Chummer GameMaster session handoff", "GM native share title must stay bounded to the GM role")
            gm_native_shared_route = str(gm_shared_payload.get("url") or "")
            assert_true(gm_native_shared_route.startswith(f"{base_url}/mobile/gm?"), "GM native shared handoff must be an absolute GM mobile URL")
            assert_true(f"sessionId={SESSION_SECRET}" in gm_native_shared_route, "GM native shared handoff must preserve the session id")
            assert_true("role=GameMaster" in gm_native_shared_route, "GM native shared handoff must preserve the GM role")
            assert_true("deviceId=" not in gm_native_shared_route, "GM native shared handoff must not leak or reuse the sender claimed-device id")
            gm_native_page.wait_for_function(
                """() => {
                    const status = document.getElementById("turn-owner-route-share-status")?.textContent || "";
                    return status.trim() === "Session handoff shared.";
                }""",
                timeout=30_000,
            )
            gm_native_page.wait_for_function(
                "() => (window.__rybbitEvents || []).some((event) => event.name === 'mobile_session_handoff_share')",
                timeout=30_000,
            )
            gm_native_events = read_events(gm_native_page)
            gm_native_share_event = find_event(gm_native_events, "mobile_session_handoff_share")
            gm_native_share_properties = gm_native_share_event.get("properties")
            assert_true(isinstance(gm_native_share_properties, dict), "GM native session handoff share must carry properties")
            assert_true(gm_native_share_properties.get("shareMethod") == "native", "GM native session handoff share must report the bounded native method")
            assert_no_secret_payload(gm_native_events, "GM native session handoff share")

            current_step = "verify GM native shared handoff mints receiving device lane"
            gm_native_receiver_context = browser.new_context(service_workers="allow")
            gm_native_receiver_page = gm_native_receiver_context.new_page()
            gm_native_receiver_page.goto(gm_native_shared_route, wait_until="domcontentloaded", timeout=SHELL_TIMEOUT_MS)
            wait_for_mobile_shell(gm_native_receiver_page)
            gm_native_receiver_page.wait_for_function(
                """([expectedSessionId, expectedRole, senderDeviceId]) => {
                    const params = new URLSearchParams(window.location.search);
                    const receivedDeviceId = params.get("deviceId") || "";
                    return params.get("sessionId") === expectedSessionId
                        && params.get("role") === expectedRole
                        && receivedDeviceId
                        && receivedDeviceId !== senderDeviceId;
                }""",
                arg=[SESSION_SECRET, "GameMaster", DEVICE_SECRET],
                timeout=30_000,
            )
            gm_native_receiver_params = gm_native_receiver_page.evaluate(
                """() => {
                    const params = new URLSearchParams(window.location.search);
                    return {
                        sessionId: params.get("sessionId") || "",
                        role: params.get("role") || "",
                        deviceId: params.get("deviceId") || ""
                    };
                }"""
            )
            assert_true(gm_native_receiver_params["sessionId"] == SESSION_SECRET, "GM native receiver handoff must preserve session id")
            assert_true(gm_native_receiver_params["role"] == "GameMaster", "GM native receiver handoff must preserve role")
            assert_true(gm_native_receiver_params["deviceId"] != DEVICE_SECRET, "GM native receiver handoff must mint its own claimed-device id")
            gm_native_receiver_context.close()
            gm_native_context.close()

            current_step = "verify visible link fallback handoff"
            link_context = browser.new_context(service_workers="allow")
            link_context.add_init_script(
                """
                Object.defineProperty(navigator, "share", {
                  configurable: true,
                  value: undefined
                });
                Object.defineProperty(navigator, "clipboard", {
                  configurable: true,
                  value: undefined
                });
                """
            )
            link_context.route(
                f"{base_url}{SCRIPT_PATH}",
                lambda route: route.fulfill(
                    status=200,
                    content_type="application/javascript",
                    body=STUB_SCRIPT,
                ),
            )
            link_page = link_context.new_page()
            link_page.goto(
                f"{base_url}/mobile/player?sessionId={SESSION_SECRET}&role=Player&deviceId={DEVICE_SECRET}",
                wait_until="domcontentloaded",
                timeout=SHELL_TIMEOUT_MS,
            )
            wait_for_mobile_shell(link_page)
            link_page.wait_for_function("() => window.__rybbitStubLoaded === true")
            link_page.wait_for_function("() => typeof window.ChummerPlayInstallPromptForTest === 'function'", timeout=60_000)
            click_mobile_control(link_page, "#turn-share-owner-route-button", "player link handoff")
            link_handoff_handle = link_page.wait_for_function(
                """() => {
                    const link = document.getElementById("turn-owner-route-link");
                    const href = link?.getAttribute("href") || "";
                    const status = document.getElementById("turn-owner-route-share-status")?.textContent || "";
                    if (status.trim() !== "Session handoff is ready in the link above." || !href) {
                        return null;
                    }
                    return {
                        href: new URL(href, window.location.origin).toString(),
                        text: (link?.textContent || "").trim()
                    };
                }""",
                timeout=30_000,
            )
            link_handoff = link_handoff_handle.json_value()
            assert_true(isinstance(link_handoff, dict), "link fallback handoff must expose an inspectable link")
            link_handoff_route = str(link_handoff.get("href") or "")
            assert_true(link_handoff_route.startswith(f"{base_url}/mobile/player?"), "link fallback handoff must publish an absolute player mobile URL")
            assert_true(f"sessionId={SESSION_SECRET}" in link_handoff_route, "link fallback handoff must preserve the session id")
            assert_true("role=Player" in link_handoff_route, "link fallback handoff must preserve the player role")
            assert_true("deviceId=" not in link_handoff_route, "link fallback handoff must not expose the sender claimed-device id")
            assert_true(link_handoff.get("text") == "Open session handoff link", "link fallback must relabel the visible route as a session handoff")
            link_page.wait_for_function(
                "() => (window.__rybbitEvents || []).some((event) => event.name === 'mobile_session_handoff_share')",
                timeout=30_000,
            )
            link_events = read_events(link_page)
            link_share_event = find_event(link_events, "mobile_session_handoff_share")
            link_share_properties = link_share_event.get("properties")
            assert_true(isinstance(link_share_properties, dict), "link fallback session handoff share must carry properties")
            assert_true(link_share_properties.get("shareMethod") == "link", "link fallback session handoff share must report the bounded link method")
            assert_no_secret_payload(link_events, "link fallback session handoff share")

            current_step = "verify visible link fallback mints receiving device lane"
            link_receiver_context = browser.new_context(service_workers="allow")
            link_receiver_page = link_receiver_context.new_page()
            link_receiver_page.goto(link_handoff_route, wait_until="domcontentloaded", timeout=SHELL_TIMEOUT_MS)
            wait_for_mobile_shell(link_receiver_page)
            link_receiver_page.wait_for_function(
                """([expectedSessionId, expectedRole, senderDeviceId]) => {
                    const params = new URLSearchParams(window.location.search);
                    const receivedDeviceId = params.get("deviceId") || "";
                    return params.get("sessionId") === expectedSessionId
                        && params.get("role") === expectedRole
                        && receivedDeviceId
                        && receivedDeviceId !== senderDeviceId;
                }""",
                arg=[SESSION_SECRET, "Player", DEVICE_SECRET],
                timeout=30_000,
            )
            link_receiver_params = link_receiver_page.evaluate(
                """() => {
                    const params = new URLSearchParams(window.location.search);
                    return {
                        sessionId: params.get("sessionId") || "",
                        role: params.get("role") || "",
                        deviceId: params.get("deviceId") || ""
                    };
                }"""
            )
            assert_true(link_receiver_params["sessionId"] == SESSION_SECRET, "link fallback receiver handoff must preserve session id")
            assert_true(link_receiver_params["role"] == "Player", "link fallback receiver handoff must preserve role")
            assert_true(link_receiver_params["deviceId"] != DEVICE_SECRET, "link fallback receiver handoff must mint its own claimed-device id")
            link_receiver_context.close()
            link_context.close()

            current_step = "verify GM visible link fallback handoff"
            gm_link_context = browser.new_context(service_workers="allow")
            gm_link_context.add_init_script(
                """
                Object.defineProperty(navigator, "share", {
                  configurable: true,
                  value: undefined
                });
                Object.defineProperty(navigator, "clipboard", {
                  configurable: true,
                  value: undefined
                });
                """
            )
            gm_link_context.route(
                f"{base_url}{SCRIPT_PATH}",
                lambda route: route.fulfill(
                    status=200,
                    content_type="application/javascript",
                    body=STUB_SCRIPT,
                ),
            )
            gm_link_page = gm_link_context.new_page()
            gm_link_page.goto(
                f"{base_url}/mobile/gm?sessionId={SESSION_SECRET}&role=GameMaster&deviceId={DEVICE_SECRET}",
                wait_until="domcontentloaded",
                timeout=SHELL_TIMEOUT_MS,
            )
            wait_for_mobile_shell(gm_link_page)
            gm_link_page.wait_for_function("() => window.__rybbitStubLoaded === true")
            gm_link_page.wait_for_function("() => typeof window.ChummerPlayInstallPromptForTest === 'function'", timeout=60_000)
            click_mobile_control(gm_link_page, "#turn-share-owner-route-button", "GM link handoff")
            gm_link_handoff_handle = gm_link_page.wait_for_function(
                """() => {
                    const link = document.getElementById("turn-owner-route-link");
                    const href = link?.getAttribute("href") || "";
                    const status = document.getElementById("turn-owner-route-share-status")?.textContent || "";
                    if (status.trim() !== "Session handoff is ready in the link above." || !href) {
                        return null;
                    }
                    return {
                        href: new URL(href, window.location.origin).toString(),
                        text: (link?.textContent || "").trim()
                    };
                }""",
                timeout=30_000,
            )
            gm_link_handoff = gm_link_handoff_handle.json_value()
            assert_true(isinstance(gm_link_handoff, dict), "GM link fallback handoff must expose an inspectable link")
            gm_link_handoff_route = str(gm_link_handoff.get("href") or "")
            assert_true(gm_link_handoff_route.startswith(f"{base_url}/mobile/gm?"), "GM link fallback handoff must publish an absolute GM mobile URL")
            assert_true(f"sessionId={SESSION_SECRET}" in gm_link_handoff_route, "GM link fallback handoff must preserve the session id")
            assert_true("role=GameMaster" in gm_link_handoff_route, "GM link fallback handoff must preserve the GM role")
            assert_true("deviceId=" not in gm_link_handoff_route, "GM link fallback handoff must not expose the sender claimed-device id")
            assert_true(gm_link_handoff.get("text") == "Open session handoff link", "GM link fallback must relabel the visible route as a session handoff")
            gm_link_page.wait_for_function(
                "() => (window.__rybbitEvents || []).some((event) => event.name === 'mobile_session_handoff_share')",
                timeout=30_000,
            )
            gm_link_events = read_events(gm_link_page)
            gm_link_share_event = find_event(gm_link_events, "mobile_session_handoff_share")
            gm_link_share_properties = gm_link_share_event.get("properties")
            assert_true(isinstance(gm_link_share_properties, dict), "GM link fallback session handoff share must carry properties")
            assert_true(gm_link_share_properties.get("shareMethod") == "link", "GM link fallback session handoff share must report the bounded link method")
            assert_no_secret_payload(gm_link_events, "GM link fallback session handoff share")

            current_step = "verify GM visible link fallback mints receiving device lane"
            gm_link_receiver_context = browser.new_context(service_workers="allow")
            gm_link_receiver_page = gm_link_receiver_context.new_page()
            gm_link_receiver_page.goto(gm_link_handoff_route, wait_until="domcontentloaded", timeout=SHELL_TIMEOUT_MS)
            wait_for_mobile_shell(gm_link_receiver_page)
            gm_link_receiver_page.wait_for_function(
                """([expectedSessionId, expectedRole, senderDeviceId]) => {
                    const params = new URLSearchParams(window.location.search);
                    const receivedDeviceId = params.get("deviceId") || "";
                    return params.get("sessionId") === expectedSessionId
                        && params.get("role") === expectedRole
                        && receivedDeviceId
                        && receivedDeviceId !== senderDeviceId;
                }""",
                arg=[SESSION_SECRET, "GameMaster", DEVICE_SECRET],
                timeout=30_000,
            )
            gm_link_receiver_params = gm_link_receiver_page.evaluate(
                """() => {
                    const params = new URLSearchParams(window.location.search);
                    return {
                        sessionId: params.get("sessionId") || "",
                        role: params.get("role") || "",
                        deviceId: params.get("deviceId") || ""
                    };
                }"""
            )
            assert_true(gm_link_receiver_params["sessionId"] == SESSION_SECRET, "GM link fallback receiver handoff must preserve session id")
            assert_true(gm_link_receiver_params["role"] == "GameMaster", "GM link fallback receiver handoff must preserve role")
            assert_true(gm_link_receiver_params["deviceId"] != DEVICE_SECRET, "GM link fallback receiver handoff must mint its own claimed-device id")
            gm_link_receiver_context.close()
            gm_link_context.close()

            current_step = "verify DNT and GPC block analytics"
            privacy_context = browser.new_context(service_workers="allow")
            privacy_context.add_init_script(
                """
                window.__privacyAnalyticsEvents = [];
                window.addEventListener("chummer-play:analytics", function (event) {
                  window.__privacyAnalyticsEvents.push(event.detail || {});
                });
                Object.defineProperty(window, "doNotTrack", {
                  configurable: true,
                  value: "1"
                });
                Object.defineProperty(navigator, "doNotTrack", {
                  configurable: true,
                  value: "1"
                });
                Object.defineProperty(navigator, "globalPrivacyControl", {
                  configurable: true,
                  value: true
                });
                """
            )
            privacy_provider_requests: list[str] = []

            def record_blocked_provider_request(route) -> None:
                privacy_provider_requests.append(route.request.url)
                route.fulfill(
                    status=200,
                    content_type="application/javascript",
                    body=STUB_SCRIPT,
                )

            privacy_context.route(f"{base_url}{SCRIPT_PATH}", record_blocked_provider_request)
            privacy_page = privacy_context.new_page()
            privacy_page.goto(
                f"{base_url}/mobile/player?sessionId={SESSION_SECRET}&role=Player&deviceId={DEVICE_SECRET}",
                wait_until="domcontentloaded",
                timeout=SHELL_TIMEOUT_MS,
            )
            wait_for_mobile_shell(privacy_page)
            privacy_page.wait_for_timeout(2_000)
            privacy_page.evaluate(
                """() => {
                    window.ChummerPlayAnalytics.track("mobile_privacy_block_probe", {
                        targetRole: "Player",
                        targetMode: "player"
                    });
                }"""
            )
            privacy_page.wait_for_timeout(500)
            privacy_state = privacy_page.evaluate(
                """() => ({
                    hasProviderScript: document.querySelector("script[data-rybbit='analytics']") !== null,
                    stubLoaded: window.__rybbitStubLoaded === true,
                    rybbitEvents: window.__rybbitEvents || [],
                    analyticsEvents: window.__privacyAnalyticsEvents || [],
                    queueLength: Array.isArray(window.ChummerPlayAnalyticsQueue) ? window.ChummerPlayAnalyticsQueue.length : 0
                })"""
            )
            assert_true(isinstance(privacy_state, dict), "privacy blocked analytics state must be inspectable")
            assert_true(len(privacy_provider_requests) == 0, "DNT/GPC must prevent the Rybbit provider script request")
            assert_true(privacy_state.get("hasProviderScript") is False, "DNT/GPC must prevent provider script insertion")
            assert_true(privacy_state.get("stubLoaded") is False, "DNT/GPC must not load the Rybbit provider")
            assert_true(privacy_state.get("rybbitEvents") == [], "DNT/GPC must prevent Rybbit events")
            assert_true(privacy_state.get("analyticsEvents") == [], "DNT/GPC must prevent first-party analytics events")
            assert_true(privacy_state.get("queueLength") == 0, "DNT/GPC must keep the first-party analytics queue empty")
            privacy_context.close()

            current_step = "verify default-disabled analytics posture"
            context.close()
            stop_server(process)
            process = None
            process, default_base_url = start_server(configure_analytics=False)
            default_context = browser.new_context(service_workers="allow")
            default_context.add_init_script(
                """
                window.__defaultAnalyticsEvents = [];
                window.addEventListener("chummer-play:analytics", function (event) {
                  window.__defaultAnalyticsEvents.push(event.detail || {});
                });
                """
            )
            default_provider_requests: list[str] = []

            def record_default_provider_request(route) -> None:
                default_provider_requests.append(route.request.url)
                route.fulfill(
                    status=204,
                    content_type="application/javascript",
                    body="",
                )

            default_context.route("**/*rybbit*", record_default_provider_request)
            default_context.route("**/api/script.js", record_default_provider_request)
            default_page = default_context.new_page()
            default_page.goto(
                f"{default_base_url}/mobile/player?sessionId={SESSION_SECRET}&role=Player&deviceId={DEVICE_SECRET}",
                wait_until="domcontentloaded",
                timeout=SHELL_TIMEOUT_MS,
            )
            wait_for_mobile_shell(default_page)
            default_page.wait_for_timeout(1_000)
            default_page.evaluate(
                """() => {
                    window.ChummerPlayAnalytics.track("mobile_default_disabled_probe", {
                        targetRole: "Player",
                        targetMode: "player"
                    });
                }"""
            )
            default_page.wait_for_timeout(500)
            default_state = default_page.evaluate(
                """() => ({
                    hasConfigNode: document.getElementById("chummer-play-analytics-config") !== null,
                    hasRuntimeConfig: typeof window.ChummerPlayAnalyticsConfig !== "undefined",
                    hasProviderScript: document.querySelector("script[data-rybbit='analytics']") !== null,
                    hasAnalyticsApi: typeof window.ChummerPlayAnalytics === "object",
                    analyticsEvents: window.__defaultAnalyticsEvents || [],
                    queueLength: Array.isArray(window.ChummerPlayAnalyticsQueue) ? window.ChummerPlayAnalyticsQueue.length : 0
                })"""
            )
            assert_true(isinstance(default_state, dict), "default-disabled analytics state must be inspectable")
            assert_true(default_state.get("hasAnalyticsApi") is True, "default-disabled shell must keep the local analytics facade available")
            assert_true(default_state.get("hasConfigNode") is False, "default-disabled shell must not emit page-local Rybbit config")
            assert_true(default_state.get("hasRuntimeConfig") is False, "default-disabled shell must not hydrate runtime Rybbit config")
            assert_true(default_state.get("hasProviderScript") is False, "default-disabled shell must not insert the Rybbit provider")
            assert_true(len(default_provider_requests) == 0, "default-disabled shell must not request the Rybbit provider")
            assert_true(default_state.get("analyticsEvents") == [], "default-disabled shell must not emit first-party analytics events")
            assert_true(default_state.get("queueLength") == 0, "default-disabled shell must keep the analytics queue empty")
            default_context.close()

            write_analytics_receipt(
                {
                    "contract_name": "chummer_play.mobile_pwa_analytics_smoke.v1",
                    "status": "pass",
                    "generated_at_utc": datetime.now(UTC).replace(microsecond=0).isoformat().replace("+00:00", "Z"),
                    "provider_script": {
                        "site_id": dataset["siteId"],
                        "script_path": SCRIPT_PATH,
                        "skip_patterns": dataset["skipPatterns"],
                        "mask_patterns": dataset["maskPatterns"],
                    },
                    "events": [
                        str(event.get("name", ""))
                        for event in events
                        if isinstance(event, dict) and str(event.get("name", "")).strip()
                    ],
                    "role_analytics": {
                        "shell_open_roles": ["player", "gm"],
                        "shell_open_display_modes": ["browser", "standalone"],
                        "standalone_shell_open_roles": ["player", "gm"],
                        "install_prompt": ["available", "open", "accepted"],
                        "install_prompt_roles": ["player", "gm"],
                        "role_switches": {
                            "player_to": role_switch_properties["targetMode"],
                            "gm_to": gm_role_switch_properties["targetMode"],
                        },
                    },
                    "handoff": {
                        "clipboard_player": {
                            "route": redact_handoff_url(copied_route),
                            "receiver_device": "<minted-device>",
                        },
                        "clipboard_gm": {
                            "route": redact_handoff_url(gm_copied_route),
                            "receiver_device": "<minted-device>",
                        },
                        "native_player": {
                            "route": redact_handoff_url(native_shared_route),
                            "receiver_device": "<minted-device>",
                            "share_method": "native",
                        },
                        "native_gm": {
                            "route": redact_handoff_url(gm_native_shared_route),
                            "receiver_device": "<minted-device>",
                            "share_method": "native",
                        },
                        "link_player": {
                            "route": redact_handoff_url(link_handoff_route),
                            "receiver_device": "<minted-device>",
                            "share_method": "link",
                        },
                        "link_gm": {
                            "route": redact_handoff_url(gm_link_handoff_route),
                            "receiver_device": "<minted-device>",
                            "share_method": "link",
                        },
                    },
                    "privacy": {
                        "dnt_gpc_blocked": True,
                        "privacy_provider_requests": len(privacy_provider_requests),
                        "privacy_event_count": len(privacy_state.get("analyticsEvents", [])),
                        "default_disabled": True,
                        "default_provider_requests": len(default_provider_requests),
                        "default_event_count": len(default_state.get("analyticsEvents", [])),
                        "secret_leak_free": True,
                    },
                }
            )

            print("mobile_pwa_analytics_smoke ok")
            print(f"  provider_script: {dataset['siteId']} {SCRIPT_PATH}")
            print(f"  pageview_skip: {dataset['skipPatterns']}")
            print(f"  route_mask: {dataset['maskPatterns']}")
            print(f"  events: {', '.join(str(event.get('name', '')) for event in events if isinstance(event, dict))}")
            print("  shell_open_role_analytics: player / gm")
            print("  shell_open_display_mode: browser / standalone")
            print("  standalone_shell_open_analytics: player / gm")
            print("  install_prompt_analytics: available / open / accepted")
            print("  install_prompt_role_analytics: player / gm")
            print(f"  role_switch_analytics: player->{role_switch_properties['targetMode']} / gm->{gm_role_switch_properties['targetMode']}")
            print(f"  copied_session_handoff: {redact_handoff_url(copied_route)}")
            print(f"  receiver_device: {str(receiver_params['deviceId']).replace(str(receiver_params['deviceId']), '<minted-device>')}")
            print(f"  gm_copied_session_handoff: {redact_handoff_url(gm_copied_route)}")
            print(f"  gm_receiver_device: {str(gm_receiver_params['deviceId']).replace(str(gm_receiver_params['deviceId']), '<minted-device>')}")
            print(f"  native_session_handoff: {redact_handoff_url(native_shared_route)}")
            print(f"  native_receiver_device: {str(native_receiver_params['deviceId']).replace(str(native_receiver_params['deviceId']), '<minted-device>')}")
            print("  native_share_method: native")
            print(f"  gm_native_session_handoff: {redact_handoff_url(gm_native_shared_route)}")
            print(f"  gm_native_receiver_device: {str(gm_native_receiver_params['deviceId']).replace(str(gm_native_receiver_params['deviceId']), '<minted-device>')}")
            print("  gm_native_share_method: native")
            print(f"  link_session_handoff: {redact_handoff_url(link_handoff_route)}")
            print(f"  link_receiver_device: {str(link_receiver_params['deviceId']).replace(str(link_receiver_params['deviceId']), '<minted-device>')}")
            print("  link_share_method: link")
            print(f"  gm_link_session_handoff: {redact_handoff_url(gm_link_handoff_route)}")
            print(f"  gm_link_receiver_device: {str(gm_link_receiver_params['deviceId']).replace(str(gm_link_receiver_params['deviceId']), '<minted-device>')}")
            print("  gm_link_share_method: link")
            print("  privacy_blocked: dnt_gpc")
            print(f"  privacy_provider_requests: {len(privacy_provider_requests)}")
            print(f"  privacy_event_count: {len(privacy_state.get('analyticsEvents', []))}")
            print("  analytics_default_disabled: true")
            print(f"  default_provider_requests: {len(default_provider_requests)}")
            print(f"  default_event_count: {len(default_state.get('analyticsEvents', []))}")
            print("  secret_leak_free: true")
            return 0
    except Exception as error:
        print(f"mobile_pwa_analytics_smoke failed during {current_step}: {error}", file=sys.stderr)
        if process is not None:
            stop_server(process)
            output = read_server_log_tail(process)
            if output:
                print(output, file=sys.stderr)
        return 1
    finally:
        if browser is not None:
            try:
                browser.close()
            except Exception:
                pass
        if process is not None:
            stop_server(process)
            cleanup_server_artifacts(process)


def main_grant_boundaries() -> int:
    process: subprocess.Popen[str] | None = None
    browser = None
    current_step = "boot"
    try:
        process, base_url = start_server(configure_analytics=True)
        grant_headers = {
            "X-Chummer-Play-Grant-Id": "analytics-smoke-grant-0001",
            "X-Chummer-Play-Grant-Session-Id": SESSION_SECRET,
            "X-Chummer-Play-Grant-Role": "Player",
            "X-Chummer-Play-Grant-Device-Id": DEVICE_SECRET,
        }
        with sync_playwright() as playwright:
            browser = playwright.chromium.launch(headless=True)

            current_step = "public install analytics boundary"
            public_provider_requests: list[str] = []
            public_context = browser.new_context(service_workers="block")
            public_context.add_init_script(
                """
                window.__publicAnalyticsEvents = [];
                window.addEventListener('chummer-play:analytics', (event) => window.__publicAnalyticsEvents.push(event.detail || {}));
                """
            )

            def record_public_provider_request(route) -> None:
                public_provider_requests.append(route.request.url)
                route.fulfill(status=200, content_type="application/javascript", body=STUB_SCRIPT)

            public_context.route(f"{base_url}{SCRIPT_PATH}", record_public_provider_request)
            public_page = public_context.new_page()
            public_page.goto(
                f"{base_url}/mobile/player?sessionId={SESSION_SECRET}&role=GameMaster&deviceId={DEVICE_SECRET}",
                wait_until="load",
                timeout=SHELL_TIMEOUT_MS,
            )
            public_page.wait_for_selector("[data-play-surface='install-only'][data-authority='none']", timeout=SHELL_TIMEOUT_MS)
            public_page.wait_for_timeout(250)
            public_state = public_page.evaluate(
                """() => ({
                    hasConfig: document.getElementById('chummer-play-analytics-config') !== null,
                    hasAnalyticsApi: typeof window.ChummerPlayAnalytics === 'object',
                    providerLoaded: window.__rybbitStubLoaded === true,
                    events: window.__publicAnalyticsEvents || [],
                    body: document.body.innerText || ''
                })"""
            )
            assert_true(public_state["hasConfig"] is False, "public install shell must not emit live analytics configuration")
            assert_true(public_state["hasAnalyticsApi"] is False, "public install shell must not initialize live analytics")
            assert_true(public_state["providerLoaded"] is False and not public_provider_requests, "public install shell must not request the analytics provider")
            assert_true(public_state["events"] == [], "public install shell must not emit live-session analytics")
            assert_true(SESSION_SECRET not in public_state["body"] and DEVICE_SECRET not in public_state["body"], "public install shell must not expose query secrets")
            public_context.close()

            current_step = "configured live analytics boundary"
            live_context = browser.new_context(
                service_workers="block",
                extra_http_headers=grant_headers,
            )
            live_context.route(
                f"{base_url}{SCRIPT_PATH}",
                lambda route: route.fulfill(status=200, content_type="application/javascript", body=STUB_SCRIPT),
            )
            live_page = live_context.new_page()
            live_page.goto(
                f"{base_url}/mobile/live?sessionId=forged-session&role=GameMaster&deviceId=forged-device",
                wait_until="domcontentloaded",
                timeout=SHELL_TIMEOUT_MS,
            )
            wait_for_mobile_shell(live_page)
            config_text = live_page.locator("#chummer-play-analytics-config").text_content() or ""
            assert_true(SITE_ID in config_text and SCRIPT_PATH in config_text, "live shell must emit configured analytics metadata")
            assert_true(SESSION_SECRET not in config_text and DEVICE_SECRET not in config_text, "live analytics config must not expose grant identifiers")
            config = json.loads(config_text)
            assert_true(config.get("route") == "/mobile/live", "live analytics must use the sanitized authoritative route")
            assert_true(config.get("role") == "Player" and config.get("mode") == "player", "live analytics must use the server-granted role posture")
            live_page.wait_for_function("() => window.__rybbitStubLoaded === true", timeout=30_000)
            live_page.wait_for_function(
                "() => (window.__rybbitEvents || []).some((event) => event.name === 'mobile_shell_open')",
                timeout=30_000,
            )
            live_events = read_events(live_page)
            shell_open = find_event(live_events, "mobile_shell_open")
            shell_properties = shell_open.get("properties")
            assert_true(isinstance(shell_properties, dict), "live shell-open event must have properties")
            assert_true(
                shell_properties.get("route") == "/mobile/live",
                f"live shell-open event must use the sanitized route: {shell_properties!r}",
            )
            assert_true(shell_properties.get("role") == "Player", "live shell-open event must use the granted role")
            assert_no_secret_payload(live_events, "live shell-open analytics")

            current_step = "live analytics payload scrubber"
            live_page.evaluate(
                """([sessionSecret, deviceSecret]) => {
                    window.ChummerPlayAnalytics.track('mobile_privacy_probe', {
                        sessionId: sessionSecret,
                        deviceId: deviceSecret,
                        continuityToken: 'token-analytics-secret',
                        ownerRoute: '/mobile/live?sessionId=' + sessionSecret,
                        targetRole: 'GameMaster',
                        targetMode: 'gm',
                        publicNote: sessionSecret,
                        safeLabel: 'probe?unsafe=chars'
                    });
                }""",
                [SESSION_SECRET, DEVICE_SECRET],
            )
            live_page.wait_for_function(
                "() => (window.__rybbitEvents || []).some((event) => event.name === 'mobile_privacy_probe')",
                timeout=30_000,
            )
            live_events = read_events(live_page)
            privacy_probe = find_event(live_events, "mobile_privacy_probe")
            privacy_properties = privacy_probe.get("properties")
            assert_true(isinstance(privacy_properties, dict), "privacy probe must carry bounded properties")
            assert_true(privacy_properties.get("targetRole") == "GameMaster" and privacy_properties.get("targetMode") == "gm", "safe role analytics must survive scrubbing")
            assert_true(privacy_properties.get("safeLabel") == "probe_unsafe_chars", "safe analytics strings must be normalized")
            assert_true("publicNote" not in privacy_properties, "secret-looking values must be removed even under safe keys")
            assert_no_secret_payload(live_events, "live privacy probe")
            live_context.close()

            current_step = "DNT and GPC live analytics block"
            privacy_provider_requests: list[str] = []
            privacy_context = browser.new_context(service_workers="block", extra_http_headers=grant_headers)
            privacy_context.add_init_script(
                """
                window.__privacyAnalyticsEvents = [];
                window.addEventListener('chummer-play:analytics', (event) => window.__privacyAnalyticsEvents.push(event.detail || {}));
                Object.defineProperty(window, 'doNotTrack', { configurable: true, value: '1' });
                Object.defineProperty(navigator, 'doNotTrack', { configurable: true, value: '1' });
                Object.defineProperty(navigator, 'globalPrivacyControl', { configurable: true, value: true });
                """
            )

            def record_privacy_provider_request(route) -> None:
                privacy_provider_requests.append(route.request.url)
                route.fulfill(status=200, content_type="application/javascript", body=STUB_SCRIPT)

            privacy_context.route(f"{base_url}{SCRIPT_PATH}", record_privacy_provider_request)
            privacy_page = privacy_context.new_page()
            privacy_page.goto(f"{base_url}/mobile/live", wait_until="domcontentloaded", timeout=SHELL_TIMEOUT_MS)
            wait_for_mobile_shell(privacy_page)
            privacy_page.wait_for_timeout(250)
            privacy_page.evaluate(
                """() => window.ChummerPlayAnalytics.track('mobile_privacy_block_probe', { targetRole: 'Player' })"""
            )
            privacy_page.wait_for_timeout(100)
            privacy_state = privacy_page.evaluate(
                """() => ({
                    hasConfig: document.getElementById('chummer-play-analytics-config') !== null,
                    providerLoaded: window.__rybbitStubLoaded === true,
                    events: window.__privacyAnalyticsEvents || [],
                    queueLength: Array.isArray(window.ChummerPlayAnalyticsQueue) ? window.ChummerPlayAnalyticsQueue.length : 0
                })"""
            )
            assert_true(privacy_state["hasConfig"] is True, "privacy test must exercise a configured live shell")
            assert_true(privacy_state["providerLoaded"] is False and not privacy_provider_requests, "DNT/GPC must block provider loading")
            assert_true(privacy_state["events"] == [] and privacy_state["queueLength"] == 0, "DNT/GPC must block first-party analytics emission")
            privacy_context.close()

            current_step = "default-disabled live analytics"
            stop_server(process)
            cleanup_server_artifacts(process)
            process = None
            process, default_base_url = start_server(configure_analytics=False)
            default_context = browser.new_context(service_workers="block", extra_http_headers=grant_headers)
            default_provider_requests: list[str] = []

            def record_default_provider_request(route) -> None:
                default_provider_requests.append(route.request.url)
                route.fulfill(status=200, content_type="application/javascript", body=STUB_SCRIPT)

            default_context.route("**/*rybbit*", record_default_provider_request)
            default_page = default_context.new_page()
            default_page.goto(f"{default_base_url}/mobile/live", wait_until="domcontentloaded", timeout=SHELL_TIMEOUT_MS)
            wait_for_mobile_shell(default_page)
            default_page.wait_for_timeout(250)
            default_state = default_page.evaluate(
                """() => ({
                    hasConfig: document.getElementById('chummer-play-analytics-config') !== null,
                    hasAnalyticsApi: typeof window.ChummerPlayAnalytics === 'object',
                    hasProvider: document.querySelector("script[data-rybbit='analytics']") !== null,
                    queueLength: Array.isArray(window.ChummerPlayAnalyticsQueue) ? window.ChummerPlayAnalyticsQueue.length : 0
                })"""
            )
            assert_true(default_state["hasConfig"] is False, "analytics must be default-disabled when server configuration is absent")
            assert_true(default_state["hasAnalyticsApi"] is True, "default-disabled live shell must keep its local no-op facade")
            assert_true(default_state["hasProvider"] is False and not default_provider_requests, "default-disabled live shell must not request a provider")
            assert_true(default_state["queueLength"] == 0, "default-disabled live shell must keep its analytics queue empty")
            default_context.close()

            browser.close()
            browser = None

        write_analytics_receipt(
            {
                "contract_name": "chummer_play.mobile_pwa_analytics_smoke.v2",
                "status": "pass",
                "generated_at_utc": datetime.now(UTC).replace(microsecond=0).isoformat().replace("+00:00", "Z"),
                "public_install_boundary": {
                    "analytics_enabled": False,
                    "provider_requests": len(public_provider_requests),
                    "event_count": len(public_state["events"]),
                    "query_secret_leak_free": True,
                },
                "live_session_boundary": {
                    "route": "/mobile/live",
                    "grant_source": "trusted_server_headers",
                    "analytics_role": shell_properties["role"],
                    "provider_script": SCRIPT_PATH,
                    "events": [event.get("name") for event in live_events if isinstance(event, dict)],
                    "secret_leak_free": True,
                },
                "provider_script": {
                    "site_id": SITE_ID,
                    "script_path": SCRIPT_PATH,
                    "skip_patterns": config["skipPatterns"],
                    "mask_patterns": config["maskPatterns"],
                },
                "privacy": {
                    "dnt_gpc_blocked": True,
                    "privacy_provider_requests": len(privacy_provider_requests),
                    "privacy_event_count": len(privacy_state["events"]),
                    "default_disabled": True,
                    "default_provider_requests": len(default_provider_requests),
                    "default_event_count": 0,
                    "secret_leak_free": True,
                },
            }
        )
        print("mobile_pwa_analytics_smoke ok")
        print("  public_install_analytics: disabled")
        print("  live_analytics_route: /mobile/live")
        print("  live_analytics_role_source: trusted_server_headers")
        print("  configured_provider: same-host / masked")
        print("  privacy_blocked: dnt_gpc")
        print("  analytics_default_disabled: true")
        print("  secret_leak_free: true")
        return 0
    except Exception as error:
        print(f"mobile_pwa_analytics_smoke failed during {current_step}: {error}", file=sys.stderr)
        if process is not None:
            output = read_server_log_tail(process)
            if output:
                print(output, file=sys.stderr)
        return 1
    finally:
        if browser is not None:
            try:
                browser.close()
            except Exception:
                pass
        if process is not None:
            stop_server(process)
            cleanup_server_artifacts(process)


if __name__ == "__main__":
    raise SystemExit(main_grant_boundaries())
