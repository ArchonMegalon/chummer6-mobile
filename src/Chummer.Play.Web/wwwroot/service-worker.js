const CACHE_VERSION = "play-shell-v2";
const SHELL_CACHE = `chummer-shell-${CACHE_VERSION}`;
const API_CACHE = `chummer-api-${CACHE_VERSION}`;
const MEDIA_CACHE = `chummer-media-${CACHE_VERSION}`;
const MEDIA_META_CACHE = `chummer-media-meta-${CACHE_VERSION}`;
const OFFLINE_NAV_FALLBACK = "/index.html";
const MEDIA_MAX_ENTRIES = 40;
const MEDIA_MAX_AGE_MS = 1000 * 60 * 60 * 24 * 3;
const SHELL_ASSETS = [
  "/",
  "/index.html",
  "/manifest.webmanifest",
  "/icons/icon-192.svg",
  "/icons/icon-512.svg"
];

self.addEventListener("install", (event) => {
  event.waitUntil(
    caches.open(SHELL_CACHE).then((cache) => cache.addAll(SHELL_ASSETS)).then(() => self.skipWaiting())
  );
});

self.addEventListener("activate", (event) => {
  event.waitUntil(
    caches.keys().then((keys) =>
      Promise.all(
        keys
          .filter((key) => ![SHELL_CACHE, API_CACHE, MEDIA_CACHE, MEDIA_META_CACHE].includes(key))
          .map((key) => caches.delete(key))
      )
    ).then(async () => {
      await pruneMediaCache();
      await self.clients.claim();
    })
  );
});

self.addEventListener("fetch", (event) => {
  const request = event.request;
  const url = new URL(request.url);

  if (request.method !== "GET") {
    return;
  }

  if (url.pathname.startsWith("/api/play/")) {
    // Keep API reads fresh; fallback to cache only when network is unavailable.
    event.respondWith(
      fetch(request)
        .then((response) => {
          if (response.ok) {
            cacheWithQuotaHandling(API_CACHE, request, response.clone());
          }
          return response;
        })
        .catch(() => caches.open(API_CACHE).then((cache) => cache.match(request)))
    );
    return;
  }

  if (request.mode === "navigate") {
    event.respondWith(
      fetch(request).catch(async () => {
        const cached = await caches.match(request);
        if (cached) {
          return cached;
        }
        return caches.match(OFFLINE_NAV_FALLBACK);
      })
    );
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
        if (!response.ok) {
          return response;
        }

        cacheWithQuotaHandling(SHELL_CACHE, request, response.clone());
        return response;
      });
    })
  );
});

async function handleMediaRequest(request) {
  const cache = await caches.open(MEDIA_CACHE);
  const cached = await cache.match(request);

  if (cached) {
    recordMediaTouch(request.url);
    return cached;
  }

  try {
    const response = await fetch(request);
    if (!response.ok) {
      return response;
    }

    await cacheWithQuotaHandling(MEDIA_CACHE, request, response.clone());
    await recordMediaTouch(request.url);
    await pruneMediaCache();
    return response;
  } catch {
    const fallback = await cache.match(request);
    if (fallback) {
      return fallback;
    }
    throw new Error("media unavailable offline");
  }
}

function isMediaRequest(request, url) {
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

async function recordMediaTouch(url) {
  const metaCache = await caches.open(MEDIA_META_CACHE);
  const metaRequest = new Request(url, { method: "GET" });
  await metaCache.put(metaRequest, new Response(String(Date.now()), { headers: { "content-type": "text/plain" } }));
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
    const touchedAt = await readMediaTouch(metaCache, request.url);
    const effectiveTouchedAt = Number.isFinite(touchedAt) ? touchedAt : 0;
    const ageMs = now - effectiveTouchedAt;
    const stale = ageMs > MEDIA_MAX_AGE_MS;
    candidates.push({ request, touchedAt: effectiveTouchedAt, stale });
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
    const touchedAt = await readMediaTouch(metaCache, request.url);
    remainingCandidates.push({ request, touchedAt: Number.isFinite(touchedAt) ? touchedAt : 0 });
  }

  remainingCandidates.sort((left, right) => left.touchedAt - right.touchedAt);
  const maxEntries = forceBackpressure ? Math.floor(MEDIA_MAX_ENTRIES * 0.75) : MEDIA_MAX_ENTRIES;
  const deleteCount = Math.max(0, remainingCandidates.length - maxEntries);
  for (let index = 0; index < deleteCount; index += 1) {
    const target = remainingCandidates[index];
    await mediaCache.delete(target.request);
    await metaCache.delete(new Request(target.request.url, { method: "GET" }));
  }
}

async function readMediaTouch(metaCache, url) {
  const response = await metaCache.match(new Request(url, { method: "GET" }));
  if (!response) {
    return Number.NaN;
  }

  const value = await response.text();
  const parsed = Number.parseInt(value, 10);
  return Number.isFinite(parsed) ? parsed : Number.NaN;
}
