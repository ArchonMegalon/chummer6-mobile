from __future__ import annotations

import json
import re
import subprocess
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[1]
WWWROOT = REPO_ROOT / "src" / "Chummer.Play.Web" / "wwwroot"


def _manifest(name: str) -> dict[str, object]:
    return json.loads((WWWROOT / name).read_text(encoding="utf-8"))


def test_companion_manifests_have_distinct_stable_mobile_identities() -> None:
    generic = _manifest("manifest.webmanifest")
    player = _manifest("manifest.player.webmanifest")
    gm = _manifest("manifest.gm.webmanifest")
    observer = _manifest("manifest.observer.webmanifest")

    assert generic["id"] == "/mobile"
    assert generic["name"] == "Chummer Turn Companion"
    assert generic["short_name"] == "Chummer Play"
    assert generic["start_url"] == "/mobile/player"
    assert generic["scope"] == "/mobile/"

    assert player["id"] == "/mobile/player"
    assert player["start_url"] == "/mobile/player"
    assert gm["id"] == "/mobile/gm"
    assert gm["start_url"] == "/mobile/gm"
    assert observer["id"] == "/mobile/observer"
    assert observer["name"] == "Chummer Observer Companion"
    assert observer["start_url"] == "/mobile/observer"

    manifests = (generic, player, gm, observer)
    ids = {manifest["id"] for manifest in manifests}
    assert len(ids) == 4
    assert all(str(identity).startswith("/mobile") for identity in ids)
    assert all(manifest["scope"] == "/mobile/" for manifest in manifests)
    assert all(manifest["display"] == "standalone" for manifest in manifests)
    assert all(
        "?" not in shortcut["url"]
        for manifest in manifests
        for shortcut in manifest["shortcuts"]
    )


def test_install_shell_has_recoverable_manual_install_and_normalized_role_identity() -> None:
    install_script = (WWWROOT / "mobile-install-shell.js").read_text(encoding="utf-8")
    page = (
        REPO_ROOT / "src" / "Chummer.Play.Web" / "Components" / "Pages" / "MobileTurnCompanionPage.razor"
    ).read_text(encoding="utf-8")
    app = (REPO_ROOT / "src" / "Chummer.Play.Web" / "Components" / "App.razor").read_text(encoding="utf-8")

    assert "try {" in install_script
    assert "} catch {" in install_script
    assert "} finally {" in install_script
    assert "installButton.disabled = accepted || isInstalled();" in install_script
    assert 'window.addEventListener("pagehide", cleanup' in install_script
    assert 'displayModeQuery.addEventListener("change", handleDisplayModeChange);' in install_script
    assert "iPhone or iPad:" in page
    assert "Share, then Add to Home Screen" in page
    assert "Android:" in page
    assert "keep using this public install shell in the browser" in page
    assert 'data-play-surface="install-only"' in page
    assert 'data-live-session="unavailable"' in page
    assert 'data-authority="none"' in page
    assert 'src="/mobile-turn-companion.js"' not in page
    assert '.ToLowerInvariant()' in page
    assert '"/mobile/observer" => "/manifest.observer.webmanifest"' in page
    assert '"/mobile/player" => "Install Chummer Player Companion"' in page
    assert '<meta name="apple-mobile-web-app-title" content="@AppleAppTitle" />' in page
    assert 'apple-mobile-web-app-title' not in app


def test_companion_worker_migrates_only_play_cache_namespaces() -> None:
    worker = (WWWROOT / "service-worker.js").read_text(encoding="utf-8")
    scoped_worker = (WWWROOT / "mobile" / "service-worker.js").read_text(encoding="utf-8")

    assert 'const CACHE_VERSION = "v21";' in worker
    assert 'const CACHE_CONTRACT = "play-source-v2";' in worker
    assert '`${CACHE_FAMILY}-static-${CACHE_CONTRACT}-${CACHE_VERSION}`' in worker
    assert '`${CACHE_FAMILY}-media-${CACHE_CONTRACT}-${CACHE_VERSION}`' in worker
    assert '`${CACHE_FAMILY}-media-meta-${CACHE_CONTRACT}-${CACHE_VERSION}`' in worker
    assert 'IS_MOBILE_PLAY_SCOPE' in worker
    assert '"chummer-mobile-play"' in worker
    assert '"chummer-public-root"' in worker
    assert '"chummer-shell-play-shell-"' in worker
    assert '"chummer-media-play-shell-"' in worker
    assert '"chummer-media-meta-play-shell-"' in worker
    assert "isManagedWorkerCache(key)" in worker
    assert "isLegacyPrivateCache(key)" in worker
    assert "event.waitUntil(precacheCriticalShell());" in worker
    assert "self.skipWaiting()" not in worker
    assert "self.clients.claim()" not in worker
    assert "Promise.allSettled" not in worker
    assert "chummer-build-static-" not in worker
    assert "chummer-online-static-" not in worker
    assert "if (isBuildOwnedRequest(url))" in worker
    assert 'url.pathname === "/blazor" || url.pathname.startsWith("/blazor/")' in worker
    assert 'importScripts("/service-worker.js")' in scoped_worker
    assert '"/mobile-install-shell.js"' in worker
    assert '"/manifest.observer.webmanifest"' in worker
    assert '"/mobile-turn-companion.js"' not in worker

    match = re.search(r"LEGACY_PRIVATE_CACHE_PREFIXES = \[(.*?)\];", worker, flags=re.DOTALL)
    assert match is not None
    managed_prefixes = set(re.findall(r'"([^"]+)"', match.group(1)))
    assert managed_prefixes == {
        "chummer-shell-play-shell-",
        "chummer-media-play-shell-",
        "chummer-media-meta-play-shell-",
    }
    assert not any("chummer-build-static-v2".startswith(prefix) for prefix in managed_prefixes)
    assert not any("chummer-online-static-v1".startswith(prefix) for prefix in managed_prefixes)


def test_companion_worker_never_caches_rendered_navigation_or_private_responses() -> None:
    worker = (WWWROOT / "service-worker.js").read_text(encoding="utf-8")
    navigation_handler = worker.split("async function handleNavigationRequest", 1)[1].split(
        "function offlineNavigationResponse", 1
    )[0]
    runtime_cache_policy = worker.split("function isPublicRuntimeCacheableRequest", 1)[1].split(
        "function shouldCacheResponse", 1
    )[0]
    media_handler = worker.split("async function handleMediaRequest", 1)[1].split(
        "function isMediaRequest", 1
    )[0]
    media_classifier = worker.split("function isMediaRequest", 1)[1].split(
        "async function cacheWithQuotaHandling", 1
    )[0]

    assert 'if (request.mode === "navigate") return false;' in worker
    assert 'cacheControl.includes("no-store")' in worker
    assert 'cacheControl.includes("no-cache")' in worker
    assert 'cacheControl.includes("private")' in worker
    assert "cacheMobileNavigationPath" not in worker
    assert "cacheMobileNavigationResponse" not in worker
    assert "caches.match" not in navigation_handler
    assert "cache.put" not in navigation_handler
    assert "if (playNetworkOffline)" not in navigation_handler
    assert "await event.preloadResponse" in navigation_handler
    assert "const response = await fetch(request);" in navigation_handler
    assert "playNetworkOffline = false;" in navigation_handler
    assert navigation_handler.count("return offlineNavigationResponse(url.pathname);") == 1
    assert "if (url.search)" in runtime_cache_policy
    assert "if (url.search)" in media_classifier
    assert "shouldCachePublicMediaResponse(request, response)" in media_handler
    assert "if (!response.ok)" not in media_handler
    assert "function isPublicMediaCacheableRequest" in worker
    assert "function shouldCachePublicMediaResponse" in worker
    assert "function passesPublicResponseCacheBoundary" in worker
    assert "function hasSameOriginResponseUrl" in worker
    assert "function variesByPrivateIdentity" in worker
    assert "function matchFreshMediaResponse" in worker
    assert "recordMediaTouch" not in worker
    assert '"/media/public/"' in worker
    assert '"/media/promo/"' in worker
    assert '"/media/ledger/"' in worker
    assert 'normalized === "authorization" || normalized === "cookie"' in worker


def test_companion_worker_caches_only_explicit_public_media_responses() -> None:
    worker_path = json.dumps(str(WWWROOT / "service-worker.js"))
    harness = f"""
const fs = require("fs");
const vm = require("vm");
const workerSource = fs.readFileSync({worker_path}, "utf8");
const writes = [];
const workerOrigin = "https://play.example.test";
class WorkerRequest extends Request {{
  constructor(input, init) {{
    super(typeof input === "string" ? new URL(input, workerOrigin) : input, init);
  }}
}}
const caches = {{
  async open(name) {{
    return {{
      async match() {{ return undefined; }},
      async put(request) {{ writes.push({{ name, url: request.url }}); }},
      async keys() {{ return []; }},
      async delete() {{ return true; }}
    }};
  }},
  async keys() {{ return []; }},
  async delete() {{ return true; }}
}};
const self = {{
  registration: {{
    scope: "https://play.example.test/mobile/",
    navigationPreload: {{ async enable() {{}} }}
  }},
  location: {{ origin: "https://play.example.test" }},
  clients: {{ async matchAll() {{ return []; }} }},
  addEventListener() {{}}
}};
const context = {{
  URL,
  Request: WorkerRequest,
  Response,
  Headers,
  Date,
  Error,
  JSON,
  Math,
  Object,
  Promise,
  String,
  console,
  caches,
  self,
  fetch: async () => {{ throw new Error("fetch response not configured"); }}
}};
context.globalThis = context;
vm.createContext(context);
vm.runInContext(workerSource, context, {{ filename: "service-worker.js" }});

function responseWithUrl(body, init, responseUrl) {{
  const response = new Response(body, init);
  Object.defineProperty(response, "url", {{ value: responseUrl }});
  return response;
}}

async function mediaWrites(url, headers, responseUrl = url) {{
  writes.length = 0;
  context.fetch = async () => responseWithUrl("media", {{ status: 200, headers }}, responseUrl);
  await context.handleMediaRequest(new Request(url, {{ method: "GET", mode: "cors" }}));
  return writes.filter((item) => item.name.includes("-media-") && !item.name.includes("-media-meta-")).length;
}}

function contentTypeForAsset(url) {{
  const pathname = new URL(url).pathname;
  if (pathname.endsWith(".js")) return "application/javascript";
  if (pathname.endsWith(".svg")) return "image/svg+xml";
  return "application/manifest+json";
}}

async function installAttempt(cacheControl, vary = "", responseOrigin = workerOrigin) {{
  writes.length = 0;
  context.fetch = async (request) => responseWithUrl(
    "asset",
    {{
      status: 200,
      headers: {{
        "content-type": contentTypeForAsset(request.url),
        "cache-control": cacheControl,
        vary
      }}
    }},
    `${{responseOrigin}}${{new URL(request.url).pathname}}`
  );
  try {{
    await context.precacheCriticalShell();
    return {{ rejected: false, writes: writes.filter((item) => item.name.includes("-static-")).length }};
  }} catch {{
    return {{ rejected: true, writes: writes.filter((item) => item.name.includes("-static-")).length }};
  }}
}}

(async () => {{
  const publicHeaders = {{ "content-type": "image/webp", "cache-control": "public, max-age=600" }};
  const results = {{
    publicMedia: await mediaWrites("https://play.example.test/media/public/runner.webp", publicHeaders),
    fixedPublicIcon: await mediaWrites("https://play.example.test/icons/icon-192.png", {{ "content-type": "image/png" }}),
    fixedIdentityVarying: await mediaWrites("https://play.example.test/icons/icon-192.png", {{ "content-type": "image/png", "cache-control": "public, max-age=600", vary: "Cookie" }}),
    fixedNoCache: await mediaWrites("https://play.example.test/icons/icon-192.png", {{ "content-type": "image/png", "cache-control": "public, no-cache" }}),
    fixedCrossOriginRedirect: await mediaWrites("https://play.example.test/icons/icon-192.png", {{ "content-type": "image/png", "cache-control": "public, max-age=600" }}, "https://attacker.example/icon.png"),
    queryBearing: await mediaWrites("https://play.example.test/media/public/runner.webp?user=1", publicHeaders),
    privatePath: await mediaWrites("https://play.example.test/media/private/runner.webp", publicHeaders),
    privateResponse: await mediaWrites("https://play.example.test/media/public/runner.webp", {{ "content-type": "image/webp", "cache-control": "private, max-age=600" }}),
    noStoreResponse: await mediaWrites("https://play.example.test/media/public/runner.webp", {{ "content-type": "image/webp", "cache-control": "public, no-store" }}),
    immediatelyStale: await mediaWrites("https://play.example.test/media/public/runner.webp", {{ "content-type": "image/webp", "cache-control": "public, max-age=0" }}),
    identityVarying: await mediaWrites("https://play.example.test/media/public/runner.webp", {{ "content-type": "image/webp", "cache-control": "public, max-age=600", vary: "Cookie" }}),
    wrongType: await mediaWrites("https://play.example.test/media/public/runner.webp", {{ "content-type": "text/html", "cache-control": "public, max-age=600" }}),
    crossOrigin: await mediaWrites("https://cdn.example.test/media/public/runner.webp", publicHeaders),
    installPublic: await installAttempt("public, max-age=600"),
    installNoStore: await installAttempt("private, no-store"),
    installIdentityVarying: await installAttempt("public, max-age=600", "Authorization"),
    installCrossOriginRedirect: await installAttempt("public, max-age=600", "", "https://attacker.example")
  }};
  process.stdout.write(JSON.stringify(results));
}})().catch((error) => {{
  console.error(error);
  process.exitCode = 1;
}});
"""
    completed = subprocess.run(
        ["node", "-e", harness],
        cwd=REPO_ROOT,
        check=True,
        capture_output=True,
        text=True,
    )
    assert json.loads(completed.stdout) == {
        "publicMedia": 1,
        "fixedPublicIcon": 1,
        "fixedIdentityVarying": 0,
        "fixedNoCache": 0,
        "fixedCrossOriginRedirect": 0,
        "queryBearing": 0,
        "privatePath": 0,
        "privateResponse": 0,
        "noStoreResponse": 0,
        "immediatelyStale": 0,
        "identityVarying": 0,
        "wrongType": 0,
        "crossOrigin": 0,
        "installPublic": {"rejected": False, "writes": 7},
        "installNoStore": {"rejected": True, "writes": 0},
        "installIdentityVarying": {"rejected": True, "writes": 0},
        "installCrossOriginRedirect": {"rejected": True, "writes": 0},
    }


def test_companion_worker_evicts_expired_media_before_serving_or_touching_it() -> None:
    worker_path = json.dumps(str(WWWROOT / "service-worker.js"))
    harness = f"""
const fs = require("fs");
const vm = require("vm");
const workerSource = fs.readFileSync({worker_path}, "utf8");
const workerOrigin = "https://play.example.test";
const request = new Request(`${{workerOrigin}}/media/public/revoked.webp`, {{ method: "GET", mode: "cors" }});
const expiredAt = Date.now() - 1;
const storedAt = expiredAt - 600000;
let expiredEntryPresent = true;
let networkFetches = 0;
let mediaDeletes = 0;
let metadataDeletes = 0;
let metadataWrites = 0;

function responseWithUrl(body, init, responseUrl) {{
  const response = new Response(body, init);
  Object.defineProperty(response, "url", {{ value: responseUrl }});
  return response;
}}

const mediaCache = {{
  async match() {{
    return expiredEntryPresent
      ? responseWithUrl("revoked-body", {{ status: 200, headers: {{ "content-type": "image/webp", "cache-control": "public, max-age=600" }} }}, request.url)
      : undefined;
  }},
  async put() {{}},
  async keys() {{ return expiredEntryPresent ? [request] : []; }},
  async delete() {{ mediaDeletes += 1; expiredEntryPresent = false; return true; }}
}};
const metadataCache = {{
  async match() {{
    return expiredEntryPresent
      ? new Response(JSON.stringify({{ storedAt, expiresAt: expiredAt }}), {{ headers: {{ "content-type": "application/json" }} }})
      : undefined;
  }},
  async put() {{ metadataWrites += 1; }},
  async keys() {{ return []; }},
  async delete() {{ metadataDeletes += 1; return true; }}
}};
const caches = {{
  async open(name) {{ return name.includes("-media-meta-") ? metadataCache : mediaCache; }},
  async keys() {{ return []; }},
  async delete() {{ return true; }}
}};
const self = {{
  registration: {{ scope: `${{workerOrigin}}/mobile/`, navigationPreload: {{ async enable() {{}} }} }},
  location: {{ origin: workerOrigin }},
  clients: {{ async matchAll() {{ return []; }} }},
  addEventListener() {{}}
}};
const context = {{
  URL, Request, Response, Headers, Date, Error, JSON, Math, Number, Object, Promise, RegExp, String,
  console, caches, self,
  fetch: async () => {{
    networkFetches += 1;
    return responseWithUrl("gone", {{ status: 410, headers: {{ "cache-control": "no-store" }} }}, request.url);
  }}
}};
context.globalThis = context;
vm.createContext(context);
vm.runInContext(workerSource, context, {{ filename: "service-worker.js" }});

(async () => {{
  const response = await context.handleMediaRequest(request);
  process.stdout.write(JSON.stringify({{
    status: response.status,
    body: await response.text(),
    networkFetches,
    mediaDeletes,
    metadataDeletes,
    metadataWrites,
    expiredEntryPresent
  }}));
}})().catch((error) => {{
  console.error(error);
  process.exitCode = 1;
}});
"""
    completed = subprocess.run(
        ["node", "-e", harness],
        cwd=REPO_ROOT,
        check=True,
        capture_output=True,
        text=True,
    )
    assert json.loads(completed.stdout) == {
        "status": 410,
        "body": "gone",
        "networkFetches": 1,
        "mediaDeletes": 1,
        "metadataDeletes": 1,
        "metadataWrites": 0,
        "expiredEntryPresent": False,
    }
