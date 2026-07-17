from __future__ import annotations

from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[1]
WEB_ROOT = REPO_ROOT / "src" / "Chummer.Play.Web"
CORE_PROJECTOR = REPO_ROOT / "src" / "Chummer.Play.Core" / "Application" / "PlayTurnCompanionProjector.cs"
CLIENT = WEB_ROOT / "wwwroot" / "mobile-turn-companion.js"
INSTALL_CLIENT = WEB_ROOT / "wwwroot" / "mobile-install-shell.js"
PAGE = WEB_ROOT / "Components" / "Pages" / "MobileTurnCompanionPage.razor"
LIVE_PAGE = WEB_ROOT / "Components" / "Pages" / "MobileLiveTurnCompanionPage.razor"
APPLICATION = WEB_ROOT / "PlayWebApplication.cs"
WORKER = WEB_ROOT / "wwwroot" / "service-worker.js"


def test_private_play_projection_and_continuity_are_memory_only() -> None:
    client = CLIENT.read_text(encoding="utf-8")
    page = PAGE.read_text(encoding="utf-8")

    assert "localStorage.setItem" not in client
    assert "localStorage.getItem" not in client
    assert "window.localStorage.removeItem(keysToRemove[removeIndex])" in client
    assert "ephemeralDeviceIds" in client
    assert "ephemeralObserverId" in client
    assert "projection = mergeProjectionState(bootstrap.projection, null)" in client
    assert "localReplayQueue: []" in client
    assert "continuityPayload: null" in client
    assert "restoredFromStorage" not in client

    for stale_claim in (
        "This cached shell is running from local storage.",
        "stay on this device until you reconnect",
        "Device-local snapshot restored.",
        "will survive reloads, install reopen",
        "persists the bounded session tracker locally",
    ):
        assert stale_claim not in client

    assert "Private table state stays in memory for this open page only." in client
    assert "Installation caches public shell assets, never table state." in client
    assert 'privateStateLifetime: "open_tab"' in client
    assert "This public page never opens table state and does not create a Play session." in page
    assert "Installation stores only public shell assets." in page
    assert 'data-play-surface="install-only"' in page
    assert 'data-authority="none"' in page
    assert 'src="/mobile-turn-companion.js"' not in page
    assert "Restoring the device-local snapshot" not in page


def test_startup_purges_only_owned_legacy_private_keys_and_exposes_clear_hook() -> None:
    client = CLIENT.read_text(encoding="utf-8")
    page = PAGE.read_text(encoding="utf-8")

    for legacy_key in (
        "chummer-play-turn-companion:",
        "chummer-play-mobile-device-id:",
        "chummer-play-mobile-handoff-device-id:",
        "chummer-play-mobile-observer-id",
    ):
        assert legacy_key in client

    assert "purgeLegacyPrivateDeviceStorage();" in client
    assert "window.ChummerPlayPrivateDeviceData" in client
    assert 'window.addEventListener("chummer-play:clear-private-device-data"' in client
    assert 'case "clear-private-device-data":' in client
    assert 'src="/mobile-install-shell.js"' in page
    assert 'data-turn-kind="clear-private-device-data"' not in page
    assert "Clear private data from this device" not in page
    assert "does not cache a character, campaign, table projection" in page


def test_mobile_client_registers_only_the_narrow_play_scope() -> None:
    client = CLIENT.read_text(encoding="utf-8")
    install_client = INSTALL_CLIENT.read_text(encoding="utf-8")

    assert 'serviceWorker.register("/mobile/service-worker.js", { scope: "/mobile/" })' in client
    assert 'serviceWorker.register("/service-worker.js", { scope: "/" })' not in client
    assert 'serviceWorker.register("/mobile/service-worker.js", { scope: "/mobile/" })' in install_client
    assert 'serviceWorker.register("/service-worker.js", { scope: "/" })' not in install_client
    assert "/api/play" not in install_client
    assert "sessionId" not in install_client


def test_clean_mobile_launch_never_serializes_private_identity_into_the_url() -> None:
    client = CLIENT.read_text(encoding="utf-8")
    application = (WEB_ROOT / "PlayWebApplication.cs").read_text(encoding="utf-8")

    assert 'params.set("sessionId"' not in client
    assert 'params.set("deviceId"' not in client
    assert 'params.set("role"' not in client
    assert 'params.delete("sessionId")' in client
    assert 'params.delete("deviceId")' in client
    assert 'params.delete("role")' in client
    sanitizer = client.split("function removePrivateIdentityFromVisibleRoute", 1)[1].split(
        "function restoreHandoffSurface", 1
    )[0]
    assert 'if (!hadPrivateIdentity)' in sanitizer
    assert 'var safePath = "/mobile/live";' in sanitizer
    assert 'window.history.replaceState({}, "", safeRoute);' in sanitizer
    clear_lifecycle = client.split("function clearPrivateDeviceData", 1)[1].split(
        "function devicePrefixForRole", 1
    )[0]
    assert 'var cleanRoute = "/mobile/" + mobileModeSegment(' in clear_lifecycle
    assert '"?role="' not in clear_lifecycle
    role_links = client.split("function updateRoleLinks", 1)[1].split(
        "function renderClaimedDeviceSurface", 1
    )[0]
    assert "readStoredValue" not in role_links
    assert "deviceIdStorageKey" not in role_links
    assert "mobileRoleHref(roleName)" in role_links
    assert "client.sessionId" not in role_links
    fallback = client.split("function applyRequestedRouteFallback", 1)[1].split(
        "function mergeProjectionState", 1
    )[0]
    assert "cached fallback" not in fallback
    assert "not cached" not in fallback
    assert "to seed" not in fallback
    assert "temporary tracker that is discarded when this page closes or reloads" in fallback
    mobile_href = client.split("function mobileHref", 1)[1].split(
        "function mobileModeSegment", 1
    )[0]
    assert '"&role="' not in mobile_href
    assert '"&deviceId="' not in mobile_href
    assert 'response.Headers["Referrer-Policy"] = "no-referrer";' in application


def test_native_change_controls_are_not_cancelled_by_the_click_delegate() -> None:
    client = CLIENT.read_text(encoding="utf-8")
    handlers = client.split("function attachHandlers", 1)[1].split(
        "function isClickHandledTurnKind", 1
    )[0]
    click_handler = handlers.split('root.addEventListener("click"', 1)[1].split(
        'root.addEventListener("change"', 1
    )[0]
    change_handler = handlers.split('root.addEventListener("change"', 1)[1]

    assert "if (!isClickHandledTurnKind(turnKind))" in click_handler
    assert click_handler.index("if (!isClickHandledTurnKind(turnKind))") < click_handler.index("stopEvent(event);")
    assert 'turnKind !== "toggle-modifier" && turnKind !== "select-anchor"' in change_handler
    assert change_handler.index('turnKind !== "toggle-modifier"') < change_handler.index("stopEvent(event);")

    click_kinds = client.split("function isClickHandledTurnKind", 1)[1].split(
        "function attachRoleLinkAnalytics", 1
    )[0]
    assert 'case "toggle-modifier":' not in click_kinds
    assert 'case "select-anchor":' not in click_kinds


def test_dynamic_controls_expose_state_and_restore_logical_focus() -> None:
    client = CLIENT.read_text(encoding="utf-8")
    page = PAGE.read_text(encoding="utf-8")

    assert '"Decrease " + card.label + ", currently " + card.value' in client
    assert '"Increase " + card.label + ", currently " + card.value' in client
    assert '"Decrease " + card.label + ", currently " + card.quantity' in client
    assert '"Increase " + card.label + ", currently " + card.quantity' in client
    assert 'aria-pressed=\\"" + (action.selected ? "true" : "false")' in client
    assert 'data-play-surface="install-only"' in page
    assert 'data-live-session="unavailable"' in page
    assert 'data-turn-kind="adjust-metric"' not in page
    assert 'data-turn-kind="select-action"' not in page
    assert "var logicalFocus = captureLogicalFocus();" in client
    assert "restoreLogicalFocus(logicalFocus);" in client
    assert 'candidates[index].focus({ preventScroll: true });' in client


def test_ready_root_bounds_retry_and_mutation_initialization_work() -> None:
    client = CLIENT.read_text(encoding="utf-8")

    assert 'document.querySelector("[data-turn-root][data-client-ready=\\"true\\"]")' in client
    assert "cancelTurnCompanionInitializationRetries();" in client
    assert "window[initializationRetryTimersName] = [];" in client
    assert "if (window[windowListenersBoundName])" in client
    assert "window[windowListenersBoundName] = true;" in client


def test_mobile_projection_routes_fail_closed_and_all_mobile_documents_are_private() -> None:
    page = PAGE.read_text(encoding="utf-8")
    application = APPLICATION.read_text(encoding="utf-8")

    for route in ("/mobile", "/mobile/player", "/mobile/gm", "/mobile/observer"):
        assert f'@page "{route}"' in page
    assert '@page "/mobile/{Mode?}"' not in page
    assert 'value.StartsWith("/mobile/", StringComparison.OrdinalIgnoreCase)' in application
    assert 'value.Equals("/mobile/service-worker.js", StringComparison.OrdinalIgnoreCase)' in application
    assert 'response.Headers.CacheControl = "private, no-store";' in application
    assert 'response.Headers["Referrer-Policy"] = "no-referrer";' in application


def test_granted_role_wins_and_role_links_preserve_native_modified_clicks() -> None:
    client = CLIENT.read_text(encoding="utf-8")
    page = PAGE.read_text(encoding="utf-8")
    live_page = LIVE_PAGE.read_text(encoding="utf-8")

    role_resolution = client.split("var params = new URLSearchParams", 1)[1].split(
        "var resumeRoute", 1
    )[0]
    assert 'var requestedRoleName = bootstrap.roleName || root.getAttribute("data-role") || "Player";' in role_resolution
    assert 'params.get("role")' not in role_resolution
    assert "inferRoleFromPath" not in role_resolution
    assert "RoleQuery" not in page
    assert "PlayTurnCompanionService" not in page
    assert "This URL does not grant Game Master authority." in page
    assert '"/mobile/gm" => "/manifest.gm.webmanifest"' in page
    assert '@page "/mobile/live"' in live_page
    assert "PlaySessionGrantPolicy.ResolveCurrent" in live_page

    role_link_targets = client.split("function updateRoleLinks", 1)[1].split(
        "function renderClaimedDeviceSurface", 1
    )[0]
    assert "isCurrentRole ? mobileHref() : mobileRoleHref(roleName)" in role_link_targets
    assert 'return "/mobile/live";' in client.split("function mobileHref", 1)[1].split(
        "function mobileRoleHref", 1
    )[0]

    role_links = client.split("function attachRoleLinkAnalytics", 1)[1].split(
        "function adjustMetric", 1
    )[0]
    assert "window.location.assign" not in role_links
    assert "stopImmediatePropagation" not in role_links
    assert "event.preventDefault();" in role_links  # Test-only suppression hook.
    assert "window.__chummerPlaySuppressRoleNavigation === true" in role_links


def test_worker_retries_network_after_offline_hint_and_never_replays_private_navigation() -> None:
    worker = WORKER.read_text(encoding="utf-8")

    assert 'const CACHE_CONTRACT = "play-source-v2";' in worker
    assert '`${CACHE_FAMILY}-static-${CACHE_CONTRACT}-${CACHE_VERSION}`' in worker
    assert '`${CACHE_FAMILY}-media-${CACHE_CONTRACT}-${CACHE_VERSION}`' in worker
    assert '`${CACHE_FAMILY}-media-meta-${CACHE_CONTRACT}-${CACHE_VERSION}`' in worker
    assert "function isBuildOwnedRequest(url)" in worker
    assert "if (isBuildOwnedRequest(url))" in worker
    api_branch = worker.split('if (url.pathname.startsWith("/api/play/"))', 1)[1].split(
        "if (isNonCacheableRequest(url))", 1
    )[0]
    navigation = worker.split("async function handleNavigationRequest", 1)[1].split(
        "function offlineNavigationResponse", 1
    )[0]
    assert "if (playNetworkOffline)" not in api_branch
    assert "if (playNetworkOffline)" not in navigation
    assert "await event.preloadResponse" in navigation
    assert "await fetch(request)" in navigation
    assert "offlineNavigationResponse(url.pathname)" in navigation
    assert 'status: 503' in worker
    assert '"cache-control": "no-store"' in worker
    assert "caches.match(request)" not in navigation


def test_open_tab_copy_does_not_claim_install_local_private_state() -> None:
    client = CLIENT.read_text(encoding="utf-8")
    projector = CORE_PROJECTOR.read_text(encoding="utf-8")

    for stale_claim in (
        "same install-local shell",
        "one install-local shell",
        "claim this install-local device",
        "Claiming this install-local device",
    ):
        assert stale_claim not in client
    assert "Install-local" not in projector
    assert "install-local" not in projector
    assert "This handoff remains available only while this page stays open" in client
    assert "Claim this open-tab session before the next handoff" in client
    assert "Open-tab turn tracker" in projector
    assert "discarded when this page closes or reloads" in projector
