const CACHE_VERSION = "v21";
const CACHE_CONTRACT = "play-source-v2";
const IS_MOBILE_PLAY_SCOPE = new URL(self.registration.scope).pathname.startsWith("/mobile/");
const CACHE_FAMILY = IS_MOBILE_PLAY_SCOPE ? "chummer-mobile-play" : "chummer-public-root";
const SHELL_CACHE = `${CACHE_FAMILY}-static-${CACHE_CONTRACT}-${CACHE_VERSION}`;
const MEDIA_CACHE = `${CACHE_FAMILY}-media-${CACHE_CONTRACT}-${CACHE_VERSION}`;
const MEDIA_META_CACHE = `${CACHE_FAMILY}-media-meta-${CACHE_CONTRACT}-${CACHE_VERSION}`;
const RUNTIME_CACHE = `${SHELL_CACHE}-runtime`;
const MANAGED_CACHE_PREFIXES = [
  `${CACHE_FAMILY}-static-`,
  `${CACHE_FAMILY}-media-`,
  `${CACHE_FAMILY}-media-meta-`
];
const LEGACY_PRIVATE_CACHE_PREFIXES = [
  "chummer-shell-play-shell-",
  "chummer-media-play-shell-",
  "chummer-media-meta-play-shell-"
];
const MOBILE_NAV_FALLBACK = "/mobile/player";
const MOBILE_PLAYER_NAV_FALLBACK = "/mobile/player";
const MOBILE_GM_NAV_FALLBACK = "/mobile/gm";
const MOBILE_OBSERVER_NAV_FALLBACK = "/mobile/observer";
const OFFLINE_NAV_FALLBACK = MOBILE_NAV_FALLBACK;
const MEDIA_MAX_ENTRIES = 40;
const MEDIA_MAX_AGE_MS = 1000 * 60 * 60 * 24 * 3;
let playNetworkOffline = false;
const NON_CACHEABLE_PATHS = new Set([
  "/mobile/pwa/ledger.json"
]);
const NON_CACHEABLE_PATH_PREFIXES = [
  "/account",
  "/admin",
  "/api",
  "/auth",
  "/signin",
  "/signout",
  "/support"
];
const NOTIFICATION_ICON = "/apple-touch-icon.png";
const NOTIFICATION_BADGE = "/favicon.ico";
const DEFAULT_NOTIFICATION_TITLE = "Chummer update";
const DEFAULT_NOTIFICATION_BODY = "Open Chummer to review the latest activity.";
const DEFAULT_NOTIFICATION_HREF = "/account/ledger/notifications";
const NOTIFICATION_ROUTE_PATHS = new Set([
  "/account/ledger/notifications",
  "/mobile",
  "/play",
  "/play/continuity",
  "/ledger/map",
  "/passport",
  "/account",
  "/account/ledger",
  "/account/ledger/advisory",
  "/account/ledger/worldtick/validation",
  "/account/ledger/onboarding",
  "/account/passport",
  "/account/passport/open",
  "/ledger",
  "/ledger/newsroom",
  "/passport/identity-network"
]);
const NOTIFICATION_ROUTE_PREFIXES = [
  "/account/ledger/factions/",
  "/ledger/turns/",
  "/ledger/newsroom/",
  "/passport/receipts/",
  "/passport/"
];
const NOTIFICATION_ASSET_PATHS = new Set([
  "/apple-touch-icon.png",
  "/favicon.ico",
  "/favicon.svg",
  "/pwa-icon.svg",
  "/pwa-maskable.svg"
]);
const NOTIFICATION_ASSET_PREFIXES = [
  "/images/",
  "/media/ledger/",
  "/media/promo/"
];
const NOTIFICATION_ASSET_SUFFIXES = [
  ".ico",
  ".jpg",
  ".jpeg",
  ".png",
  ".svg",
  ".webp"
];
const PUBLIC_CACHEABLE_ASSETS = new Map([
  ["/mobile.css", new Set(["text/css"])],
  ["/mobile-install-shell.js", new Set(["application/javascript", "text/javascript"])],
  ["/manifest.webmanifest", new Set(["application/manifest+json", "application/json"])],
  ["/manifest.player.webmanifest", new Set(["application/manifest+json", "application/json"])],
  ["/manifest.gm.webmanifest", new Set(["application/manifest+json", "application/json"])],
  ["/manifest.observer.webmanifest", new Set(["application/manifest+json", "application/json"])],
  ["/icons/icon-192.png", new Set(["image/png"])],
  ["/icons/icon-512.png", new Set(["image/png"])],
  ["/icons/icon-192.svg", new Set(["image/svg+xml"])],
  ["/icons/icon-512.svg", new Set(["image/svg+xml"])]
]);
const PUBLIC_MEDIA_PATH_PREFIXES = [
  "/images/",
  "/icons/",
  "/media/ledger/",
  "/media/promo/",
  "/media/public/"
];
const PUBLIC_MEDIA_CONTENT_TYPES = new Set([
  "audio/aac",
  "audio/flac",
  "audio/mpeg",
  "audio/mp4",
  "audio/ogg",
  "audio/wav",
  "audio/webm",
  "image/avif",
  "image/gif",
  "image/jpeg",
  "image/png",
  "image/svg+xml",
  "image/webp",
  "video/mp4",
  "video/ogg",
  "video/webm"
]);
const CRITICAL_SHELL_ASSETS = [
  "/mobile-install-shell.js",
  "/manifest.webmanifest",
  "/manifest.player.webmanifest",
  "/manifest.gm.webmanifest",
  "/manifest.observer.webmanifest",
  "/icons/icon-192.svg",
  "/icons/icon-512.svg"
];

self.addEventListener("install", (event) => {
  event.waitUntil(precacheCriticalShell());
});

async function precacheCriticalShell() {
  const verified = await Promise.all(CRITICAL_SHELL_ASSETS.map(async (asset) => {
    const request = new Request(asset, { method: "GET", cache: "reload", credentials: "omit" });
    const response = await fetch(request);
    if (!shouldCacheResponse(request, response)) {
      throw new Error(`critical public shell asset failed validation: ${asset}`);
    }
    return { request, response };
  }));
  const cache = await caches.open(SHELL_CACHE);
  await Promise.all(verified.map(({ request, response }) => cache.put(request, response)));
}

self.addEventListener("activate", (event) => {
  event.waitUntil(
    caches.keys().then((keys) =>
      Promise.all(
        keys
          .filter((key) => (isManagedWorkerCache(key) && ![SHELL_CACHE, MEDIA_CACHE, MEDIA_META_CACHE, RUNTIME_CACHE].includes(key))
            || isLegacyPrivateCache(key))
          .map((key) => caches.delete(key))
      )
    ).then(async () => {
      await pruneMediaCache();
      if ("navigationPreload" in self.registration) {
        await self.registration.navigationPreload.enable();
      }

    })
  );
});

self.addEventListener("message", (event) => {
  const data = event.data || {};
  if (data.type === "chummer-play-network-state") {
    playNetworkOffline = data.online === false;
    if (event.source && typeof event.source.postMessage === "function") {
      event.source.postMessage({
        type: "chummer-play-network-state-ack",
        online: !playNetworkOffline
      });
    }
  }
});

self.addEventListener("push", (event) => {
  event.waitUntil(handlePush(event));
});

self.addEventListener("notificationclick", (event) => {
  event.waitUntil(handleNotificationClick(event));
});

self.addEventListener("notificationclose", (event) => {
  event.waitUntil(handleNotificationClose(event));
});

function isManagedWorkerCache(cacheName) {
  return MANAGED_CACHE_PREFIXES.some((prefix) => cacheName.startsWith(prefix));
}

function isLegacyPrivateCache(cacheName) {
  return LEGACY_PRIVATE_CACHE_PREFIXES.some((prefix) => cacheName.startsWith(prefix));
}

function isNonCacheableRequest(url) {
  const pathname = String(url.pathname || "");
  if (NON_CACHEABLE_PATHS.has(pathname)) {
    return true;
  }

  return NON_CACHEABLE_PATH_PREFIXES.some((prefix) =>
    pathname === prefix || pathname.startsWith(`${prefix}/`)
  );
}

function isPublicRuntimeCacheableRequest(request) {
  if (!request) {
    return false;
  }

  try {
    const url = new URL(request.url, self.location.origin);
    if (url.origin !== self.location.origin) {
      return false;
    }

    if (url.search) {
      return false;
    }

    if (request.mode === "navigate") return false;
    return PUBLIC_CACHEABLE_ASSETS.has(url.pathname);
  } catch {
    return false;
  }
}

function isExpectedPublicAssetResponse(request, response) {
  if (!response || !response.ok || response.status !== 200 || !isPublicRuntimeCacheableRequest(request)) {
    return false;
  }
  const url = new URL(request.url, self.location.origin);
  const expected = PUBLIC_CACHEABLE_ASSETS.get(url.pathname);
  const actual = String(response.headers.get("Content-Type") || "").split(";", 1)[0].trim().toLowerCase();
  return Boolean(expected && expected.has(actual));
}

function hasForbiddenPublicCacheDirective(response) {
  const cacheControl = String(response.headers.get("Cache-Control") || "").toLowerCase();
  return cacheControl.includes("no-store")
    || cacheControl.includes("no-cache")
    || cacheControl.includes("private");
}

function variesByPrivateIdentity(response) {
  const vary = String(response.headers.get("Vary") || "").toLowerCase();
  return vary.split(",").some((header) => {
    const normalized = header.trim();
    return normalized === "*" || normalized === "authorization" || normalized === "cookie";
  });
}

function hasSameOriginResponseUrl(response) {
  const responseUrl = String(response?.url || "");
  if (!responseUrl) {
    return false;
  }

  try {
    return new URL(responseUrl).origin === self.location.origin;
  } catch {
    return false;
  }
}

function passesPublicResponseCacheBoundary(response) {
  return hasSameOriginResponseUrl(response)
    && !hasForbiddenPublicCacheDirective(response)
    && !variesByPrivateIdentity(response);
}

function shouldCacheResponse(request, response) {
  if (!isExpectedPublicAssetResponse(request, response)) {
    return false;
  }

  return passesPublicResponseCacheBoundary(response);
}

function isPublicMediaCacheableRequest(request) {
  if (!request || request.method !== "GET" || request.mode === "navigate") {
    return false;
  }

  try {
    const url = new URL(request.url, self.location.origin);
    if (url.origin !== self.location.origin || url.search || isNonCacheableRequest(url)) {
      return false;
    }

    return PUBLIC_CACHEABLE_ASSETS.has(url.pathname)
      || PUBLIC_MEDIA_PATH_PREFIXES.some((prefix) => url.pathname.startsWith(prefix));
  } catch {
    return false;
  }
}

function shouldCachePublicMediaResponse(request, response) {
  if (!response || !response.ok || response.status !== 200 || !isPublicMediaCacheableRequest(request)) {
    return false;
  }

  if (!passesPublicResponseCacheBoundary(response)) {
    return false;
  }

  if (isExpectedPublicAssetResponse(request, response)) {
    return mediaResponseFreshnessLifetimeMs(response) > 0;
  }

  const contentType = String(response.headers.get("Content-Type") || "")
    .split(";", 1)[0]
    .trim()
    .toLowerCase();
  if (!PUBLIC_MEDIA_CONTENT_TYPES.has(contentType)) {
    return false;
  }

  const cacheControl = String(response.headers.get("Cache-Control") || "").toLowerCase();
  if (!cacheControl.split(",").some((directive) => directive.trim() === "public")
      || mediaResponseFreshnessLifetimeMs(response) <= 0) {
    return false;
  }

  return true;
}

function mediaResponseFreshnessLifetimeMs(response) {
  const cacheControl = String(response.headers.get("Cache-Control") || "").toLowerCase();
  const explicitLifetimes = [];

  for (const directive of cacheControl.split(",")) {
    const normalized = directive.trim();
    if (!/^(?:s-maxage|max-age)\s*=/.test(normalized)) {
      continue;
    }

    const match = /^(?:s-maxage|max-age)\s*=\s*"?([0-9]+)"?$/.exec(normalized);
    if (!match) {
      return 0;
    }

    const seconds = Number.parseInt(match[1], 10);
    if (!Number.isSafeInteger(seconds)) {
      return 0;
    }
    explicitLifetimes.push(seconds * 1000);
  }

  const declaredLifetime = explicitLifetimes.length > 0
    ? Math.min(...explicitLifetimes)
    : MEDIA_MAX_AGE_MS;
  const ageHeader = String(response.headers.get("Age") || "").trim();
  const ageSeconds = /^[0-9]+$/.test(ageHeader) ? Number.parseInt(ageHeader, 10) : 0;
  const remainingLifetime = declaredLifetime - (Number.isSafeInteger(ageSeconds) ? ageSeconds * 1000 : declaredLifetime);
  return Math.max(0, Math.min(MEDIA_MAX_AGE_MS, remainingLifetime));
}

async function refreshRuntime(request) {
  const response = await fetch(request);
  if (!shouldCacheResponse(request, response)) {
    return response;
  }

  const copy = response.clone();
  await cacheWithQuotaHandling(RUNTIME_CACHE, request, copy);
  return response;
}

function isBuildOwnedRequest(url) {
  if (!url || url.origin !== self.location.origin) {
    return false;
  }

  return url.pathname === "/blazor" || url.pathname.startsWith("/blazor/");
}

self.addEventListener("fetch", (event) => {
  const request = event.request;
  const url = new URL(request.url);

  if (request.method !== "GET") {
    return;
  }

  if (isBuildOwnedRequest(url)) {
    return;
  }

  if (url.pathname.startsWith("/api/play/")) {
    // Private play state is network-only; do not replay another account's cached API response.
    event.respondWith(
      fetch(request)
        .then((response) => {
          playNetworkOffline = false;
          return response;
        })
        .catch(() => playApiNetworkUnavailableResponse())
    );
    return;
  }

  if (isNonCacheableRequest(url)) {
    event.respondWith(
      fetch(request)
        .catch(() => new Response(
          JSON.stringify({
            error: "play_public_route_network_unavailable",
            detail: "Reconnect before loading account, ledger, support, or API data."
          }),
          {
            status: 503,
            headers: {
              "content-type": "application/problem+json",
              "cache-control": "no-store"
            }
          }
        ))
    );
    return;
  }

  if (request.mode === "navigate") {
    event.respondWith(handleNavigationRequest(event, request, url));
    return;
  }

  if (isMediaRequest(request, url)) {
    event.respondWith(handleMediaRequest(request));
    return;
  }

  event.respondWith(
    caches.open(SHELL_CACHE).then((cache) => cache.match(request)).then((cached) => {
      if (cached) {
        return cached;
      }

      return fetch(request).then((response) => {
        if (!shouldCacheResponse(request, response)) {
          return response;
        }

        event.waitUntil(cacheWithQuotaHandling(SHELL_CACHE, request, response.clone()));
        return response;
      });
    })
  );
});

async function handleNavigationRequest(event, request, url) {
  if (event.preloadResponse) {
    try {
      const preloaded = await event.preloadResponse;
      if (preloaded) {
        playNetworkOffline = false;
        return preloaded;
      }
    } catch {
      // A failed preload still gets one ordinary network attempt below.
    }
  }

  try {
    const response = await fetch(request);
    playNetworkOffline = false;
    return response;
  } catch {
    return offlineNavigationResponse(url.pathname);
  }
}

function offlineNavigationResponse(pathname) {
  const normalized = String(pathname || "").toLowerCase();
  const mobileRoute = normalized === "/mobile" || normalized.startsWith("/mobile/");
  const title = mobileRoute ? "Chummer Play needs a connection" : "Chummer needs a connection";
  const heading = mobileRoute ? "Reconnect to reopen this play shell" : "You're offline";
  const summary = mobileRoute
    ? "Private table state is never restored from Cache Storage. Reopen a trusted session link after reconnecting."
    : "Reconnect, then reload this page to continue.";
  const html = `<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>${title}</title>
  <style>
    :root { color-scheme: dark; font-family: system-ui, sans-serif; background: #101318; color: #f4f7fb; }
    body { margin: 0; min-height: 100vh; display: grid; place-items: center; }
    main { box-sizing: border-box; width: min(42rem, 100%); padding: 2rem; }
    p { color: #c9d2de; line-height: 1.6; }
  </style>
</head>
<body>
  <main><div role="status" aria-live="polite" aria-atomic="true"><h1>${heading}</h1><p>${summary}</p></div></main>
</body>
</html>`;
  return new Response(html, {
    status: 503,
    statusText: "Service Unavailable",
    headers: {
      "content-type": "text/html; charset=utf-8",
      "cache-control": "no-store",
      "content-security-policy": "default-src 'none'; style-src 'unsafe-inline'; base-uri 'none'; form-action 'none'; frame-ancestors 'none'",
      "x-content-type-options": "nosniff"
    }
  });
}

async function handlePush(event) {
  const payload = normalizePushPayload(event);
  const href = normalizeNotificationHref(payload.href || payload.route || payload.url || DEFAULT_NOTIFICATION_HREF);
  const actionRoutes = {};
  const notificationData = {
    href,
    route: href,
    tag: payload.tag || "chummer-update",
    family: payload.family || "general",
    notificationId: payload.notificationId || payload.id || null,
    receivedAt: new Date().toISOString(),
    source: "service-worker-push"
  };

  await broadcastClientMessage("chummer:pwa-notification-push", {
    title: payload.title || DEFAULT_NOTIFICATION_TITLE,
    body: payload.body || DEFAULT_NOTIFICATION_BODY,
    data: notificationData
  });

  if (payload.silent === true) {
    return;
  }

  const options = {
    body: payload.body || DEFAULT_NOTIFICATION_BODY,
    icon: normalizeNotificationAssetPath(payload.icon, NOTIFICATION_ICON),
    badge: normalizeNotificationAssetPath(payload.badge, NOTIFICATION_BADGE),
    tag: notificationData.tag,
    data: notificationData,
    renotify: payload.renotify === true,
    requireInteraction: payload.requireInteraction === true,
    silent: false
  };

  if (Array.isArray(payload.actions) && payload.actions.length > 0) {
    options.actions = payload.actions
      .map((action) => {
        if (!action || !action.action || !action.title) {
          return null;
        }

        const actionId = String(action.action).trim();
        const actionTitle = String(action.title).trim();
        const actionHref = tryNormalizeNotificationHref(action.href || action.route || action.url || "");
        if (!actionId || !actionTitle) {
          return null;
        }
        if (!actionHref) {
          return null;
        }

        actionRoutes[actionId] = actionHref;
        return {
          action: actionId,
          title: actionTitle
        };
      })
      .filter(Boolean)
      .slice(0, 2);
  }

  if (Object.keys(actionRoutes).length > 0) {
    notificationData.actionRoutes = actionRoutes;
  }

  await self.registration.showNotification(payload.title || DEFAULT_NOTIFICATION_TITLE, options);
}

async function handleNotificationClick(event) {
  const notification = event.notification;
  if (notification) {
    notification.close();
  }

  const href = resolveNotificationActionHref(event.action, notification?.data);

  await broadcastClientMessage("chummer:pwa-notification-click", {
    action: event.action || null,
    href,
    data: notification?.data || null
  });

  const windowClients = await self.clients.matchAll({ type: "window", includeUncontrolled: true });
  const targetUrl = new URL(href, self.location.origin).href;

  for (const client of windowClients) {
    if (!("focus" in client)) {
      continue;
    }

    try {
      const clientUrl = new URL(client.url, self.location.origin);
      if (clientUrl.origin === self.location.origin) {
        await client.focus();
        if ("navigate" in client && client.url !== targetUrl) {
          await client.navigate(targetUrl);
        }
        return;
      }
    } catch {
      // Ignore malformed client URLs and continue trying.
    }
  }

  if (self.clients.openWindow) {
    await self.clients.openWindow(targetUrl);
  }
}

async function handleNotificationClose(event) {
  await broadcastClientMessage("chummer:pwa-notification-close", {
    href: normalizeNotificationHref(event.notification?.data?.href || event.notification?.data?.route || DEFAULT_NOTIFICATION_HREF),
    data: event.notification?.data || null
  });
}

function normalizePushPayload(event) {
  if (!event || !event.data) {
    return {};
  }

  try {
    const json = event.data.json();
    return json && typeof json === "object" ? json : {};
  } catch {
    try {
      const text = event.data.text();
      if (!text) {
        return {};
      }

      return {
        title: DEFAULT_NOTIFICATION_TITLE,
        body: text
      };
    } catch {
      return {};
    }
  }
}

function normalizeNotificationHref(value) {
  return tryNormalizeNotificationHref(value) || DEFAULT_NOTIFICATION_HREF;
}

function tryNormalizeNotificationHref(value) {
  if (!value) {
    return null;
  }

  try {
    const url = new URL(String(value), self.location.origin);
    if (url.origin !== self.location.origin) {
      return null;
    }

    if (!isAllowedNotificationHref(url.pathname)) {
      return null;
    }

    return `${url.pathname}${url.search}${url.hash}`;
  } catch {
    return null;
  }
}

function normalizeNotificationAssetPath(value, fallback) {
  return tryNormalizeNotificationAssetPath(value) || fallback;
}

function tryNormalizeNotificationAssetPath(value) {
  if (!value) {
    return null;
  }

  try {
    const url = new URL(String(value), self.location.origin);
    if (url.origin !== self.location.origin) {
      return null;
    }

    if (!isAllowedNotificationAssetPath(url.pathname)) {
      return null;
    }

    return `${url.pathname}${url.search}${url.hash}`;
  } catch {
    return null;
  }
}

function isAllowedNotificationHref(pathname) {
  return NOTIFICATION_ROUTE_PATHS.has(pathname)
    || NOTIFICATION_ROUTE_PREFIXES.some((prefix) => pathname.startsWith(prefix));
}

function isAllowedNotificationAssetPath(pathname) {
  const lowerPath = String(pathname || "").toLowerCase();
  const hasAllowedExtension = NOTIFICATION_ASSET_SUFFIXES.some((suffix) => lowerPath.endsWith(suffix));
  if (!hasAllowedExtension) {
    return false;
  }

  return NOTIFICATION_ASSET_PATHS.has(pathname)
    || NOTIFICATION_ASSET_PREFIXES.some((prefix) => pathname.startsWith(prefix));
}

function resolveNotificationActionHref(actionId, data) {
  const actionRoutes = data?.actionRoutes;
  if (actionId && actionRoutes && typeof actionRoutes === "object") {
    const routed = actionRoutes[String(actionId)];
    if (routed) {
      return normalizeNotificationHref(routed);
    }
  }

  return normalizeNotificationHref(
    data?.href
      || data?.route
      || DEFAULT_NOTIFICATION_HREF
  );
}

async function broadcastClientMessage(type, payload) {
  const clients = await self.clients.matchAll({ type: "window", includeUncontrolled: true });
  await Promise.all(
    clients.map(async (client) => {
      try {
        client.postMessage({ type, payload });
      } catch {
        // Ignore postMessage failures for detached or unavailable clients.
      }
    })
  );
}

function playApiNetworkUnavailableResponse() {
  return new Response(
    JSON.stringify({
      error: "play_api_network_unavailable",
      detail: "Reconnect before loading private play data."
    }),
    {
      status: 503,
      headers: {
        "content-type": "application/problem+json",
        "cache-control": "no-store"
      }
    }
  );
}

async function handleMediaRequest(request) {
  const cache = await caches.open(MEDIA_CACHE);
  const metaCache = await caches.open(MEDIA_META_CACHE);
  const cached = await matchFreshMediaResponse(cache, metaCache, request);

  if (cached) {
    return cached;
  }

  let response;
  try {
    response = await fetch(request);
  } catch {
    const fallback = await matchFreshMediaResponse(cache, metaCache, request);
    if (fallback) {
      return fallback;
    }
    throw new Error("media unavailable offline");
  }

  if (!shouldCachePublicMediaResponse(request, response)) {
    return response;
  }

  try {
    await cacheWithQuotaHandling(MEDIA_CACHE, request, response.clone());
    await recordMediaEntry(metaCache, request.url, response);
    await pruneMediaCache();
  } catch {
    // A cache or metadata failure must not turn a successful network response
    // into an outage. Remove any partial entry so future reads still fail closed.
    await Promise.all([
      cache.delete(request).catch(() => false),
      metaCache.delete(new Request(request.url, { method: "GET" })).catch(() => false)
    ]);
  }

  return response;
}

async function matchFreshMediaResponse(cache, metaCache, request) {
  const cached = await cache.match(request);
  if (!cached) {
    return null;
  }

  const metadata = await readMediaMetadata(metaCache, request.url);
  if (isFreshMediaMetadata(metadata, Date.now())) {
    return cached;
  }

  await Promise.all([
    cache.delete(request),
    metaCache.delete(new Request(request.url, { method: "GET" }))
  ]);
  return null;
}

function isMediaRequest(request, url) {
  if (url.search) {
    return false;
  }

  if (request.destination === "image" || request.destination === "video" || request.destination === "audio") {
    return true;
  }

  if (url.pathname.startsWith("/media/")) {
    return true;
  }

  return /\.(png|jpg|jpeg|gif|webp|svg|avif|mp3|wav|ogg|mp4|webm)$/i.test(url.pathname);
}

async function cacheWithQuotaHandling(cacheName, request, response) {
  try {
    const cache = await caches.open(cacheName);
    await cache.put(request, response);
  } catch (error) {
    if (isQuotaExceededError(error)) {
      await pruneMediaCache(true);
      const cache = await caches.open(cacheName);
      await cache.put(request, response);
      return;
    }

    throw error;
  }
}

function isQuotaExceededError(error) {
  return typeof error === "object" &&
    error !== null &&
    (error.name === "QuotaExceededError" || error.name === "NS_ERROR_DOM_QUOTA_REACHED");
}

async function recordMediaEntry(metaCache, url, response) {
  const storedAt = Date.now();
  const lifetimeMs = mediaResponseFreshnessLifetimeMs(response);
  const metaRequest = new Request(url, { method: "GET" });
  const metadata = JSON.stringify({
    storedAt,
    expiresAt: storedAt + lifetimeMs
  });
  await metaCache.put(metaRequest, new Response(metadata, {
    headers: {
      "content-type": "application/json",
      "cache-control": "no-store"
    }
  }));
}

async function pruneMediaCache(forceBackpressure = false) {
  const mediaCache = await caches.open(MEDIA_CACHE);
  const metaCache = await caches.open(MEDIA_META_CACHE);
  const mediaRequests = await mediaCache.keys();

  if (mediaRequests.length === 0) {
    return;
  }

  const now = Date.now();
  const candidates = [];

  for (const request of mediaRequests) {
    const metadata = await readMediaMetadata(metaCache, request.url);
    const storedAt = metadata && Number.isFinite(metadata.storedAt) ? metadata.storedAt : 0;
    candidates.push({ request, storedAt, stale: !isFreshMediaMetadata(metadata, now) });
  }

  for (const candidate of candidates) {
    if (candidate.stale) {
      await mediaCache.delete(candidate.request);
      await metaCache.delete(new Request(candidate.request.url, { method: "GET" }));
    }
  }

  const remainingRequests = await mediaCache.keys();
  if (!forceBackpressure && remainingRequests.length <= MEDIA_MAX_ENTRIES) {
    return;
  }

  const remainingCandidates = [];
  for (const request of remainingRequests) {
    const metadata = await readMediaMetadata(metaCache, request.url);
    const storedAt = metadata && Number.isFinite(metadata.storedAt) ? metadata.storedAt : 0;
    remainingCandidates.push({ request, storedAt });
  }

  remainingCandidates.sort((left, right) => left.storedAt - right.storedAt);
  const maxEntries = forceBackpressure ? Math.floor(MEDIA_MAX_ENTRIES * 0.75) : MEDIA_MAX_ENTRIES;
  const deleteCount = Math.max(0, remainingCandidates.length - maxEntries);
  for (let index = 0; index < deleteCount; index += 1) {
    const target = remainingCandidates[index];
    await mediaCache.delete(target.request);
    await metaCache.delete(new Request(target.request.url, { method: "GET" }));
  }
}

function isFreshMediaMetadata(metadata, now) {
  if (!metadata
      || !Number.isFinite(metadata.storedAt)
      || !Number.isFinite(metadata.expiresAt)
      || metadata.storedAt < 0
      || metadata.storedAt > now
      || metadata.expiresAt <= metadata.storedAt
      || metadata.expiresAt > metadata.storedAt + MEDIA_MAX_AGE_MS) {
    return false;
  }

  return now < metadata.expiresAt;
}

async function readMediaMetadata(metaCache, url) {
  const response = await metaCache.match(new Request(url, { method: "GET" }));
  if (!response) {
    return null;
  }

  const value = await response.text();
  try {
    const parsed = JSON.parse(value);
    if (parsed && typeof parsed === "object") {
      return {
        storedAt: Number(parsed.storedAt),
        expiresAt: Number(parsed.expiresAt)
      };
    }
  } catch {
    // Legacy metadata was a plain insertion/touch timestamp. It remains bounded
    // by the worker's maximum media lifetime and is never refreshed on read.
  }

  const legacyStoredAt = Number.parseInt(value, 10);
  if (!Number.isFinite(legacyStoredAt)) {
    return null;
  }

  return {
    storedAt: legacyStoredAt,
    expiresAt: legacyStoredAt + MEDIA_MAX_AGE_MS
  };
}

function resolveMobileFallback(pathname) {
  const normalized = String(pathname || "").toLowerCase();
  if (normalized.indexOf("/mobile/gm") === 0) {
    return MOBILE_GM_NAV_FALLBACK;
  }
  if (normalized.indexOf("/mobile/observer") === 0) {
    return MOBILE_OBSERVER_NAV_FALLBACK;
  }
  if (normalized.indexOf("/mobile/player") === 0) {
    return MOBILE_PLAYER_NAV_FALLBACK;
  }
  return MOBILE_NAV_FALLBACK;
}
