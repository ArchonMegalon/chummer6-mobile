#!/usr/bin/env python3
from __future__ import annotations

import os
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
SESSION_ID = "session-mobile-viewport"
DEVICE_ID = "viewport-player-shell"
ROLE_NAME = "Player"


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


def start_server() -> tuple[subprocess.Popen[str], str]:
    port = free_port()
    base_url = f"http://127.0.0.1:{port}"
    state_dir = tempfile.mkdtemp(prefix="chummer-play-viewport-smoke-")
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
    return process, base_url


def stop_server(process: subprocess.Popen[str]) -> None:
    if process.poll() is not None:
        return
    process.terminate()
    try:
        process.wait(timeout=10)
    except subprocess.TimeoutExpired:
        process.kill()
        process.wait(timeout=10)


def wait_for_mobile_shell(page: Page) -> None:
    page.wait_for_selector("[data-turn-root]")
    page.wait_for_selector("#turn-shell-summary")
    page.wait_for_selector("#turn-jump-nav")
    page.wait_for_selector("#turn-glance-grid")


def main() -> int:
    process: subprocess.Popen[str] | None = None
    current_step = "boot"
    current_page: Page | None = None
    try:
        process, base_url = start_server()
        screenshot_path = ROOT / "_tmp" / "mobile-viewport-smoke-player-390x844.png"
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

            current_step = "inspect installability"
            cdp_session = page.context.new_cdp_session(page)
            cdp_session.send("Page.enable")
            manifest = cdp_session.send("Page.getAppManifest")
            manifest_errors = manifest.get("errors", [])
            assert_true(len(manifest_errors) == 0, "mobile viewport must keep the app manifest free of Chromium parse errors")
            installability = cdp_session.send("Page.getInstallabilityErrors")
            installability_errors = installability.get("installabilityErrors", [])
            assert_true(len(installability_errors) == 0, "mobile viewport must stay installable without Chromium PWA installability errors")

            current_step = "capture viewport metrics"
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
            page.screenshot(path=str(screenshot_path), full_page=True)
            browser.close()

        print("mobile_pwa_viewport_smoke ok")
        print("  viewport: 390x844 player lane")
        print(f"  overflow_free: {str(not metrics['hasHorizontalOverflow']).lower()}")
        print(f"  quick_lane_priority: now {metrics['nowCardTop']:.1f} / trust {metrics['trustCardTop']:.1f}")
        print(f"  compact_layout: actions {metrics['actionColumns']} col / odds {metrics['oddsColumns']} col")
        print(f"  quick_glance: ammo {metrics['ammoValue']} / anchor {metrics['anchorValue']}")
        print(f"  installability_errors: {len(installability_errors)}")
        print(f"  safe_area_padding: top {metrics['shellPaddingTop']} / bottom {metrics['shellPaddingBottom']}")
        print(f"  screenshot: {screenshot_path}")
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


if __name__ == "__main__":
    raise SystemExit(main())
