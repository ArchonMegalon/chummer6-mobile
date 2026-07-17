from __future__ import annotations

import http.client
import os
import socket
import subprocess
import tempfile
import time
from collections.abc import Iterator
from pathlib import Path

import pytest
from playwright.sync_api import sync_playwright


ROOT = Path(__file__).resolve().parents[1]
WEB_PROJECT = ROOT / "src" / "Chummer.Play.Web" / "Chummer.Play.Web.csproj"


def _free_port() -> int:
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as sock:
        sock.bind(("127.0.0.1", 0))
        return int(sock.getsockname()[1])


def _request(
    port: int,
    target: str,
    headers: dict[str, str] | None = None,
) -> tuple[int, dict[str, str], str]:
    connection = http.client.HTTPConnection("127.0.0.1", port, timeout=15)
    try:
        connection.request("GET", target, headers=headers or {})
        response = connection.getresponse()
        return response.status, {key.lower(): value for key, value in response.getheaders()}, response.read().decode("utf-8")
    finally:
        connection.close()


def _request_headers(port: int, target: str) -> tuple[int, dict[str, str]]:
    connection = http.client.HTTPConnection("127.0.0.1", port, timeout=15)
    try:
        connection.request("GET", target)
        response = connection.getresponse()
        headers = {key.lower(): value for key, value in response.getheaders()}
        response.read()
        return response.status, headers
    finally:
        connection.close()


@pytest.fixture(scope="module")
def play_host() -> Iterator[tuple[int, str]]:
    port = _free_port()
    base_url = f"http://127.0.0.1:{port}"
    with tempfile.TemporaryDirectory(prefix="chummer-play-hardening-") as state_root:
        environment = os.environ.copy()
        environment.update(
            {
                "ASPNETCORE_ENVIRONMENT": "Development",
                "CHUMMER_PLAY_BROWSER_STATE_DIR": state_root,
                "CHUMMER_PLAY_BROWSER_STATE_ISOLATE_PER_APP": "true",
                "DOTNET_CLI_TELEMETRY_OPTOUT": "1",
            }
        )
        process = subprocess.Popen(
            [
                "dotnet",
                "run",
                "--no-build",
                "--no-launch-profile",
                "--project",
                str(WEB_PROJECT),
                "--",
                "--urls",
                base_url,
            ],
            cwd=ROOT,
            env=environment,
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
        )
        try:
            deadline = time.monotonic() + 45
            while time.monotonic() < deadline:
                try:
                    status, _, body = _request(port, "/health")
                    if status == 200 and body.strip() == "ok":
                        break
                except OSError:
                    pass
                if process.poll() is not None:
                    raise RuntimeError("Play host exited before becoming healthy")
                time.sleep(0.2)
            else:
                raise RuntimeError("Play host did not become healthy")
            yield port, base_url
        finally:
            process.terminate()
            try:
                process.wait(timeout=10)
            except subprocess.TimeoutExpired:
                process.kill()
                process.wait(timeout=5)


def test_mobile_documents_fail_closed_with_private_headers(play_host: tuple[int, str]) -> None:
    port, _ = play_host

    status, headers, body = _request(port, "/mobile/gm?role=Player")
    assert status == 200
    assert headers.get("cache-control") == "private, no-store"
    assert headers.get("referrer-policy") == "no-referrer"
    assert 'data-play-surface="install-only"' in body
    assert 'data-authority="none"' in body
    assert "This URL does not grant Game Master authority." in body
    assert "session-main" not in body
    assert 'src="/mobile-turn-companion.js"' not in body

    status, headers, body = _request(port, "/mobile/not-a-role?sessionId=runtime-private-sentinel")
    assert status == 404
    assert headers.get("cache-control") == "private, no-store"
    assert headers.get("referrer-policy") == "no-referrer"
    assert "runtime-private-sentinel" not in body

    status, headers, _ = _request(port, "/mobile/service-worker.js")
    assert status == 200
    assert "private" not in headers.get("cache-control", "")

    for asset_path in (
        "/mobile.css",
        "/mobile-install-shell.js",
        "/manifest.webmanifest",
        "/manifest.player.webmanifest",
        "/manifest.gm.webmanifest",
        "/manifest.observer.webmanifest",
        "/icons/icon-192.png",
        "/icons/icon-512.png",
        "/icons/icon-192.svg",
        "/icons/icon-512.svg",
    ):
        status, headers = _request_headers(port, asset_path)
        assert status == 200
        assert headers.get("cache-control") == "public, max-age=300, must-revalidate"
        assert headers.get("x-content-type-options") == "nosniff"
        vary = {token.strip().lower() for token in headers.get("vary", "").split(",") if token.strip()}
        assert not vary.intersection({"*", "authorization", "cookie"})


def test_live_companion_requires_a_trusted_server_grant(
    play_host: tuple[int, str],
) -> None:
    port, _ = play_host

    status, headers, body = _request(
        port,
        "/mobile/live?sessionId=forged-query-session&role=GameMaster",
    )
    assert status == 403
    assert headers.get("cache-control") == "private, no-store"
    assert headers.get("referrer-policy") == "no-referrer"
    assert "forged-query-session" not in body
    assert "play_session_grant_required" in body

    grant_headers = {
        "X-Chummer-Play-Grant-Id": "grant-runtime-browser-0001",
        "X-Chummer-Play-Grant-Session-Id": "runtime-granted-session",
        "X-Chummer-Play-Grant-Role": "Player",
        "X-Chummer-Play-Grant-Device-Id": "runtime-granted-device",
    }
    status, headers, body = _request(port, "/mobile/live", grant_headers)
    assert status == 200
    assert headers.get("cache-control") == "private, no-store"
    assert "connect-src 'self'" in headers.get("content-security-policy", "")
    assert 'data-session-grant-backed="true"' in body
    assert 'data-session-id="runtime-granted-session"' in body
    assert 'data-role="Player"' in body
    assert 'src="/mobile-turn-companion.js"' in body
    assert 'data-play-surface="install-only"' not in body


def test_browser_install_shell_never_opens_live_state_or_role_authority(play_host: tuple[int, str]) -> None:
    _, base_url = play_host
    with sync_playwright() as playwright:
        browser = playwright.chromium.launch(headless=True)
        context = browser.new_context(service_workers="allow")
        page = context.new_page()
        try:
            page.goto(
                f"{base_url}/mobile/player",
                wait_until="domcontentloaded",
            )
            page.wait_for_selector('[data-play-surface="install-only"]')
            assert page.url == f"{base_url}/mobile/player"
            assert page.locator('[data-play-surface="install-only"]').get_attribute("data-authority") == "none"
            assert page.locator('[data-live-session="unavailable"]').count() == 1
            assert page.locator("[data-turn-root]").count() == 0
            assert page.locator("#turn-companion-bootstrap").count() == 0

            private_keys = page.evaluate(
                """() => Object.keys(localStorage).filter((key) =>
                    key.startsWith('chummer-play-turn-companion:')
                    || key.startsWith('chummer-play-mobile-device-id:')
                    || key.startsWith('chummer-play-mobile-handoff-device-id:')
                    || key === 'chummer-play-mobile-observer-id')"""
            )
            assert private_keys == []

            resource_urls = page.evaluate(
                """() => performance.getEntriesByType('resource').map((entry) => entry.name)"""
            )
            assert not any("/api/play" in url for url in resource_urls)
            assert not any("/_blazor" in url for url in resource_urls)
            assert any("/mobile-install-shell.js" in url for url in resource_urls)

            page.wait_for_function(
                "() => navigator.serviceWorker.ready.then((registration) => registration.active?.state === 'activated')"
            )
            page.reload(wait_until="domcontentloaded")
            page.wait_for_selector('[data-play-surface="install-only"]')
            page.wait_for_function("() => !!navigator.serviceWorker.controller")

            forged = page.goto(
                f"{base_url}/mobile/gm?sessionId=runtime-browser-session&role=GameMaster&deviceId=runtime-browser-device",
                wait_until="domcontentloaded",
            )
            assert forged is not None and forged.status == 200
            page.wait_for_selector('[data-play-surface="install-only"]')
            assert page.locator('[data-authority="none"]').count() == 1
            assert page.locator("text=This URL does not grant Game Master authority.").count() == 1
            assert page.locator("[data-turn-root]").count() == 0
        finally:
            context.close()
            browser.close()


def test_install_prompt_failures_recover_to_permanent_manual_instructions(play_host: tuple[int, str]) -> None:
    _, base_url = play_host
    with sync_playwright() as playwright:
        browser = playwright.chromium.launch(headless=True)
        page = browser.new_page(viewport={"width": 390, "height": 844})
        try:
            page.goto(f"{base_url}/mobile/player", wait_until="domcontentloaded")
            page.wait_for_selector("#turn-manual-install-help")
            assert "Share, then Add to Home Screen" in page.locator("#turn-manual-install-help").inner_text()
            assert "Android" in page.locator("#turn-manual-install-help").inner_text()
            assert page.locator("#turn-install-button").is_enabled()

            page.evaluate(
                """() => {
                    const event = new Event('beforeinstallprompt', { cancelable: true });
                    Object.defineProperty(event, 'prompt', {
                        value: () => Promise.reject(new Error('synthetic prompt rejection'))
                    });
                    Object.defineProperty(event, 'userChoice', {
                        get: () => Promise.resolve({ outcome: 'dismissed' })
                    });
                    window.dispatchEvent(event);
                }"""
            )
            page.locator("#turn-install-button").click()
            page.wait_for_function(
                "() => document.querySelector('#turn-install-status')?.textContent?.includes('direct install prompt was unavailable')"
            )
            assert page.locator("#turn-install-button").is_enabled()
            assert page.locator("#turn-manual-install-help").evaluate("node => node === document.activeElement")

            page.evaluate(
                """() => {
                    const event = new Event('beforeinstallprompt', { cancelable: true });
                    Object.defineProperty(event, 'prompt', { value: () => Promise.resolve() });
                    Object.defineProperty(event, 'userChoice', {
                        get: () => Promise.reject(new Error('synthetic choice rejection'))
                    });
                    window.dispatchEvent(event);
                }"""
            )
            page.locator("#turn-install-button").click()
            page.wait_for_function(
                "() => document.querySelector('#turn-install-status')?.textContent?.includes('direct install prompt was unavailable')"
            )
            assert page.locator("#turn-install-button").is_enabled()
        finally:
            browser.close()


def test_install_shell_identity_and_manual_copy_hold_at_small_widths(play_host: tuple[int, str]) -> None:
    _, base_url = play_host
    with sync_playwright() as playwright:
        browser = playwright.chromium.launch(headless=True)
        try:
            for width in (320, 390, 430):
                context = browser.new_context(
                    viewport={"width": width, "height": 844},
                    user_agent=(
                        "Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) "
                        "AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.0 Mobile/15E148 Safari/604.1"
                    ),
                )
                page = context.new_page()
                try:
                    page.goto(f"{base_url}/mobile/observer", wait_until="domcontentloaded")
                    assert page.locator('link[rel="manifest"]').get_attribute("href") == "/manifest.observer.webmanifest"
                    assert page.title() == "Install Chummer Observer Companion"
                    assert page.locator('meta[name="apple-mobile-web-app-title"]').get_attribute("content") == "Chummer Observer"
                    assert page.locator("#turn-manual-install-help").is_visible()
                    assert "Share, then Add to Home Screen" in page.locator("#turn-manual-install-help").inner_text()
                    assert page.locator("#turn-install-button").is_enabled()
                    overflow = page.evaluate(
                        "() => Math.max(document.documentElement.scrollWidth, document.body.scrollWidth) - window.innerWidth"
                    )
                    assert overflow <= 1
                finally:
                    context.close()

            context = browser.new_context()
            page = context.new_page()
            try:
                page.goto(f"{base_url}/mobile/GM", wait_until="domcontentloaded")
                assert page.locator('link[rel="manifest"]').get_attribute("href") == "/manifest.gm.webmanifest"
                assert page.title() == "Install Chummer GM Companion"
                assert page.locator("text=This URL does not grant Game Master authority.").count() == 1
            finally:
                context.close()
        finally:
            browser.close()
