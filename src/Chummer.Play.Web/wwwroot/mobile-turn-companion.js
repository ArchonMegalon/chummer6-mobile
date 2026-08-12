(function () {
  "use strict";

  var legacyPrivateStoragePrefixes = [
    "chummer-play-turn-companion:",
    "chummer-play-mobile-device-id:",
    "chummer-play-mobile-handoff-device-id:"
  ];
  var legacyPrivateStorageExactKeys = ["chummer-play-mobile-observer-id"];
  var analyticsQueueName = "ChummerPlayAnalyticsQueue";
  var activeClientName = "__chummerPlayActiveClient";
  var initializationTimerName = "__chummerPlayInitializationTimer";
  var initializationRetryTimersName = "__chummerPlayInitializationRetryTimers";
  var initializationObserverName = "__chummerPlayInitializationObserver";
  var windowListenersBoundName = "__chummerPlayWindowListenersBound";
  var shellOpenKeyName = "__chummerPlayShellOpenKey";
  var serviceWorkerNetworkAckName = "__chummerPlayServiceWorkerNetworkStateAck";
  var serviceWorkerNetworkMessageBoundName = "__chummerPlayServiceWorkerNetworkMessageBound";
  var magazineCapacity = 12;
  var historyLimit = 8;
  var ephemeralDeviceIds = Object.create(null);
  var ephemeralObserverId = "";

  purgeLegacyPrivateDeviceStorage();
  exposePrivateDeviceDataLifecycle();
  scheduleTurnCompanionInitialization();

  function scheduleTurnCompanionInitialization() {
    if (document.readyState === "loading") {
      document.addEventListener("DOMContentLoaded", initializeTurnCompanion, { once: true });
    } else {
      initializeTurnCompanion();
    }

    window.addEventListener("load", initializeTurnCompanion);
    window.addEventListener("pageshow", initializeTurnCompanion);
    window[initializationRetryTimersName] = [0, 250, 1000, 2500].map(function (delay) {
      return window.setTimeout(initializeTurnCompanion, delay);
    });

    if ("MutationObserver" in window && !window[initializationObserverName]) {
      var target = document.documentElement || document.body;
      if (target) {
        window[initializationObserverName] = new MutationObserver(queueTurnCompanionInitialization);
        window[initializationObserverName].observe(target, { childList: true, subtree: true });
      }
    }
  }

  function queueTurnCompanionInitialization() {
    var readyRoot = document.querySelector("[data-turn-root][data-client-ready=\"true\"]");
    if (readyRoot && (readyRoot.__chummerPlayClient || window[activeClientName])) {
      return;
    }

    if (window[initializationTimerName]) {
      window.clearTimeout(window[initializationTimerName]);
    }

    window[initializationTimerName] = window.setTimeout(function () {
      window[initializationTimerName] = null;
      initializeTurnCompanion();
    }, 0);
  }

  function cancelTurnCompanionInitializationRetries() {
    var retryTimers = window[initializationRetryTimersName] || [];
    for (var index = 0; index < retryTimers.length; index += 1) {
      window.clearTimeout(retryTimers[index]);
    }
    window[initializationRetryTimersName] = [];
  }

  function initializeTurnCompanion() {
    var root = document.querySelector("[data-turn-root]");
    var bootstrapNode = document.getElementById("turn-companion-bootstrap");
    if (!root || !bootstrapNode) {
      return;
    }

    if (root.getAttribute("data-client-ready") === "true") {
      cancelTurnCompanionInitializationRetries();
      rehydrateReadyRoot(root);
      return;
    }

    var bootstrap = parseJson(bootstrapNode.textContent);
    if (!bootstrap || !bootstrap.projection) {
      return;
    }

    var params = new URLSearchParams(window.location.search || "");
    var requestedRoleName = bootstrap.roleName || root.getAttribute("data-role") || "Player";
    var resumeRoute = resolveResumeRoute(params, requestedRoleName);
    var sessionId = bootstrap.sessionId || root.getAttribute("data-session-id") || "";
    var roleName = requestedRoleName;
    var explicitDeviceId = bootstrap.deviceId || root.getAttribute("data-device-id") || "";
    var deviceId = resolveStableDeviceId(sessionId, roleName, explicitDeviceId);
    var observerId = resolveObserverId();
    removePrivateIdentityFromVisibleRoute(params, roleName);

    var projection = mergeProjectionState(bootstrap.projection, null);
    var bootstrapMismatch = bootstrap.sessionId && bootstrap.sessionId !== sessionId;
    if (bootstrapMismatch) {
      applyRequestedRouteFallback(projection, sessionId, roleName);
    }
    var client = {
      sessionId: sessionId,
      roleName: roleName,
      deviceId: deviceId,
      observerId: observerId,
      projection: projection,
      statusMessage: buildInitialStatusMessage(bootstrapMismatch, bootstrap, resumeRoute),
      manualHits: 0,
      manualGlitch: false,
      localReplayQueue: [],
      continuityPayload: null,
      serviceWorkerStatus: "Checking service worker and install cache posture for this shell.",
      installPromptEvent: null,
      installBusy: false,
      networkBusy: false,
      visibleHandoffUrl: "",
      ownerRouteShareStatus: ""
    };

    initializeMobileAnalytics(client, resumeRoute);
    window[activeClientName] = client;
    root.__chummerPlayClient = client;
    bindTurnCompanionWindowListeners();
    normalizeProjection(client.projection);
    render(client);
    attachHandlers(root, client);
    void bootInstallBoundary(client);
    root.setAttribute("data-client-ready", "true");
    root.__chummerPlayClient = client;
    cancelTurnCompanionInitializationRetries();
    if (navigator.onLine) {
      void refreshNetworkSurfaces(
        client,
        bootstrapMismatch
          ? "Refreshing the requested route after loading the cached mobile fallback."
          : "Refreshing trust, queue, and claimed-device posture for this shell."
      );
    }
  }

  function bindTurnCompanionWindowListeners() {
    if (window[windowListenersBoundName]) {
      return;
    }

    window[windowListenersBoundName] = true;
    window.addEventListener("online", function () {
      var client = window[activeClientName];
      if (!client) {
        return;
      }

      publishNetworkStateToServiceWorker();
      void refreshNetworkSurfaces(client, "Reconnect detected. Refreshing trust, queue, and claimed-device posture for this shell.");
    });
    window.addEventListener("offline", function () {
      var client = window[activeClientName];
      if (!client) {
        return;
      }

      publishNetworkStateToServiceWorker();
      client.statusMessage = "Offline mode: play tracking remains available in this open tab. Private table state is discarded when the page closes or reloads.";
      render(client);
    });

    var handleInstallPromptAvailable = function (event) {
      var client = window[activeClientName];
      if (!client) {
        return;
      }

      event.preventDefault();
      client.installPromptEvent = event;
      client.serviceWorkerStatus = "This shell is installable from the browser prompt on this device.";
      trackMobileEvent("mobile_install_prompt_available", client, { installPrompt: "available" });
      render(client);
    };
    window.ChummerPlayInstallPromptForTest = handleInstallPromptAvailable;
    window.ChummerPlayInstallShellForTest = function () {
      var client = window[activeClientName];
      return client ? installShell(client) : Promise.resolve();
    };
    window.addEventListener("beforeinstallprompt", handleInstallPromptAvailable);
    window.addEventListener("appinstalled", function () {
      var client = window[activeClientName];
      if (!client) {
        return;
      }

      client.installPromptEvent = null;
      client.serviceWorkerStatus = "This shell is installed and can reopen from the local app icon. Private table state still remains open-tab only.";
      trackMobileEvent("mobile_shell_installed", client, { installPrompt: "installed" });
      render(client);
    });
  }

  function rehydrateReadyRoot(root) {
    var client = root.__chummerPlayClient || window[activeClientName];
    if (!client) {
      return;
    }

    root.__chummerPlayClient = client;
    attachHandlers(root, client);
    restoreHandoffSurface(client);
  }

  function removePrivateIdentityFromVisibleRoute(params, roleName) {
    var hadPrivateIdentity = params.has("sessionId") || params.has("deviceId") || params.has("role");
    params.delete("sessionId");
    params.delete("deviceId");
    params.delete("role");
    if (!hadPrivateIdentity) {
      return;
    }

    var safeQuery = params.toString();
    var safePath = "/mobile/live";
    var safeRoute = safePath
      + (safeQuery ? "?" + safeQuery : "")
      + (window.location.hash || "");
    window.history.replaceState({}, "", safeRoute);
  }

  function restoreHandoffSurface(client) {
    if (!client) {
      return;
    }

    var statusElement = document.getElementById("turn-owner-route-share-status");
    var linkElement = document.getElementById("turn-owner-route-link");
    if (!statusElement && !linkElement) {
      return;
    }

    var expectedStatus = client.ownerRouteShareStatus || "";
    var currentStatus = statusElement ? (statusElement.textContent || "").trim() : "";
    var needsStatus = statusElement && currentStatus !== expectedStatus;
    var needsLink = false;
    if (client.visibleHandoffUrl && linkElement) {
      var currentHref = absoluteMobileUrl(linkElement.getAttribute("href") || "");
      var currentLabel = (linkElement.textContent || "").trim();
      needsLink = currentHref !== client.visibleHandoffUrl || currentLabel !== "Open session handoff link";
    }

    if (needsStatus || needsLink) {
      renderClaimedDeviceSurface(client);
    }
  }

  function initializeMobileAnalytics(client, resumeRoute) {
    window.ChummerPlayAnalytics = {
      track: function (name, payload) {
        trackMobileEvent(name, client, payload || {});
      },
      flush: flushAnalyticsQueue
    };

    var config = readAnalyticsConfig();
    if (!config || isAnalyticsBlocked()) {
      return;
    }

    window.ChummerPlayAnalyticsConfig = config;
    loadRybbitProvider(config);
    var shellOpenKey = analyticsRoute(client, config)
      + "|"
      + analyticsRole(client && client.roleName ? client.roleName : config.role)
      + "|"
      + (client && client.sessionId ? client.sessionId : "")
      + "|"
      + (client && client.deviceId ? client.deviceId : "");
    if (window[shellOpenKeyName] !== shellOpenKey) {
      window[shellOpenKeyName] = shellOpenKey;
      trackMobileEvent("mobile_shell_open", client, {
        resumeSource: resumeRoute && resumeRoute.resumeSource ? resumeRoute.resumeSource : "direct",
        privateStateLifetime: "open_tab",
        installed: isInstalledShell() ? "true" : "false"
      });
    }
  }

  function readAnalyticsConfig() {
    if (window.ChummerPlayAnalyticsConfig && window.ChummerPlayAnalyticsConfig.enabled === true) {
      return window.ChummerPlayAnalyticsConfig;
    }

    var node = document.getElementById("chummer-play-analytics-config");
    if (!node) {
      return null;
    }

    var parsed = parseJson(node.textContent);
    if (!parsed || parsed.enabled !== true || !parsed.scriptUrl || !parsed.siteId) {
      return null;
    }

    return {
      enabled: true,
      scriptUrl: String(parsed.scriptUrl),
      siteId: String(parsed.siteId),
      tag: String(parsed.tag || "mobile_play_shell"),
      route: String(parsed.route || ""),
      mode: String(parsed.mode || ""),
      role: String(parsed.role || ""),
      skipPatterns: String(parsed.skipPatterns || "[\"/mobile\",\"/mobile/**\"]"),
      maskPatterns: String(parsed.maskPatterns || "[\"/mobile\",\"/mobile/**\",\"/api/play/**\"]"),
      replayBlockSelector: String(parsed.replayBlockSelector || "[data-turn-root]")
    };
  }

  function loadRybbitProvider(config) {
    if (!config || !config.scriptUrl || !config.siteId || isAnalyticsBlocked()) {
      return;
    }

    if (document.querySelector("script[data-rybbit='analytics'][data-tag='mobile_play_shell']")) {
      flushAnalyticsQueue();
      return;
    }

    var loadRybbit = function () {
      if (document.querySelector("script[data-rybbit='analytics'][data-tag='mobile_play_shell']")) {
        flushAnalyticsQueue();
        return;
      }

      var rybbit = document.createElement("script");
      rybbit.src = config.scriptUrl;
      rybbit.async = true;
      rybbit.dataset.siteId = config.siteId;
      rybbit.dataset.rybbit = "analytics";
      rybbit.dataset.tag = config.tag || "mobile_play_shell";
      rybbit.dataset.skipPatterns = config.skipPatterns || "[\"/mobile\",\"/mobile/**\"]";
      rybbit.dataset.maskPatterns = config.maskPatterns || "[\"/mobile\",\"/mobile/**\",\"/api/play/**\"]";
      rybbit.dataset.replayBlockSelector = config.replayBlockSelector || "[data-turn-root]";
      rybbit.dataset.replayMaskAllInputs = "true";
      rybbit.referrerPolicy = "strict-origin-when-cross-origin";
      rybbit.addEventListener("load", flushAnalyticsQueue, { once: true });
      document.head.appendChild(rybbit);
    };

    if ("requestIdleCallback" in window) {
      window.requestIdleCallback(loadRybbit, { timeout: 1500 });
      return;
    }

    window.setTimeout(loadRybbit, 250);
  }

  function trackMobileEvent(name, client, payload) {
    var config = readAnalyticsConfig();
    if (!config || isAnalyticsBlocked()) {
      return;
    }

    var eventName = normalizeAnalyticsToken(name);
    if (!eventName) {
      return;
    }

    var record = {
      event: eventName,
      ts: new Date().toISOString(),
      surface: "mobile_turn_companion",
      route: analyticsRoute(client, config),
      role: analyticsRole(client && client.roleName ? client.roleName : config.role),
      mode: analyticsMode(client && client.roleName ? client.roleName : config.role, config),
      online: navigator.onLine ? "true" : "false",
      displayMode: currentDisplayMode()
    };

    var safePayload = sanitizeAnalyticsPayload(payload || {});
    Object.keys(safePayload).forEach(function (key) {
      record[key] = safePayload[key];
    });

    ensureAnalyticsQueue().push(record);
    window.dispatchEvent(new CustomEvent("chummer-play:analytics", { detail: record }));
    flushAnalyticsQueue();
  }

  function flushAnalyticsQueue() {
    var queue = ensureAnalyticsQueue();
    if (!queue.length) {
      return;
    }

    var retained = [];
    for (var index = 0; index < queue.length; index += 1) {
      if (!sendRybbitRecord(queue[index])) {
        retained.push(queue[index]);
      }
    }

    queue.length = 0;
    for (var retainedIndex = 0; retainedIndex < retained.length; retainedIndex += 1) {
      queue.push(retained[retainedIndex]);
    }
  }

  function sendRybbitRecord(record) {
    var rybbitApi = window.rybbit;
    if (!rybbitApi || !record || !record.event) {
      return false;
    }

    var properties = {};
    Object.keys(record).forEach(function (key) {
      if (key !== "event") {
        properties[key] = record[key];
      }
    });

    try {
      if (typeof rybbitApi.event === "function") {
        rybbitApi.event(record.event, properties);
        return true;
      }

      if (typeof rybbitApi.track === "function") {
        rybbitApi.track(record.event, properties);
        return true;
      }
    } catch (error) {
      void error;
    }

    return false;
  }

  function ensureAnalyticsQueue() {
    if (!Array.isArray(window[analyticsQueueName])) {
      window[analyticsQueueName] = [];
    }

    return window[analyticsQueueName];
  }

  function sanitizeAnalyticsPayload(payload) {
    var output = {};
    Object.keys(payload || {}).forEach(function (key) {
      var normalizedKey = normalizeAnalyticsToken(key);
      if (!normalizedKey || isSensitiveAnalyticsKey(normalizedKey)) {
        return;
      }

      var value = payload[key];
      if (typeof value === "boolean") {
        output[normalizedKey] = value ? "true" : "false";
      } else if (typeof value === "number" && Number.isFinite(value)) {
        output[normalizedKey] = value;
      } else if (typeof value === "string") {
        var safeValue = safeAnalyticsValue(value);
        if (!safeValue || isSensitiveAnalyticsValue(safeValue)) {
          return;
        }

        output[normalizedKey] = safeValue;
      }
    });

    return output;
  }

  function normalizeAnalyticsToken(value) {
    return String(value || "").trim().replace(/[^a-zA-Z0-9_:-]/g, "_").slice(0, 80);
  }

  function safeAnalyticsValue(value) {
    return String(value || "").trim().replace(/[?&#=]/g, "_").slice(0, 120);
  }

  function isSensitiveAnalyticsKey(key) {
    return /session|device|token|continuity|owner|secret|key|href|url/i.test(key);
  }

  function isSensitiveAnalyticsValue(value) {
    var text = String(value || "").trim().toLowerCase();
    if (!text) {
      return false;
    }

    return /^https?:\/\//.test(text)
      || /(?:^|[/\s])(?:session|device|token|secret|key|continuity|owner)[_-][a-z0-9_-]+/.test(text)
      || /(?:^|[?&])(sessionid|deviceid|token|key|secret|ownerroute)=/i.test(text)
      || text.indexOf("/play/") >= 0
      || text.indexOf("/mobile?") >= 0
      || (text.indexOf("/mobile/") >= 0 && text.indexOf("?") >= 0);
  }

  function analyticsRole(roleName) {
    if (isGameMasterRole(roleName)) {
      return "GameMaster";
    }
    var lowered = String(roleName || "").toLowerCase();
    if (lowered.indexOf("observer") >= 0) {
      return "Observer";
    }
    return "Player";
  }

  function analyticsMode(roleName, config) {
    var configuredMode = String(config && config.mode ? config.mode : "").toLowerCase();
    if (configuredMode === "gm" || configuredMode === "observer" || configuredMode === "player") {
      return configuredMode;
    }

    return roleSegmentForAnalytics(roleName);
  }

  function analyticsRoute(client, config) {
    void client;
    void config;
    return "/mobile/live";
  }

  function roleSegmentForAnalytics(roleName) {
    if (isGameMasterRole(roleName)) {
      return "gm";
    }
    var lowered = String(roleName || "").toLowerCase();
    if (lowered.indexOf("observer") >= 0) {
      return "observer";
    }
    return "player";
  }

  function isGameMasterRole(roleName) {
    var lowered = String(roleName || "").toLowerCase();
    return lowered === "gm"
      || lowered.indexOf("gamemaster") >= 0
      || lowered.indexOf("game master") >= 0;
  }

  function currentDisplayMode() {
    if (isInstalledShell()) {
      return "standalone";
    }

    return "browser";
  }

  function isAnalyticsBlocked() {
    return window.doNotTrack === "1"
      || navigator.doNotTrack === "1"
      || navigator.msDoNotTrack === "1"
      || navigator.globalPrivacyControl === true;
  }

  function makeStableId(prefix) {
    return prefix + "-" + Math.random().toString(36).slice(2, 10);
  }

  function exposePrivateDeviceDataLifecycle() {
    window.ChummerPlayPrivateDeviceData = {
      clear: function () {
        clearPrivateDeviceData(window[activeClientName] || null, true);
      },
      purgeLegacyStorage: purgeLegacyPrivateDeviceStorage
    };

    window.addEventListener("chummer-play:clear-private-device-data", function () {
      clearPrivateDeviceData(window[activeClientName] || null, true);
    });
  }

  function purgeLegacyPrivateDeviceStorage() {
    try {
      var keysToRemove = [];
      for (var index = 0; index < window.localStorage.length; index += 1) {
        var key = window.localStorage.key(index);
        if (!key) {
          continue;
        }

        if (legacyPrivateStorageExactKeys.indexOf(key) >= 0
          || legacyPrivateStoragePrefixes.some(function (prefix) { return key.indexOf(prefix) === 0; })) {
          keysToRemove.push(key);
        }
      }

      for (var removeIndex = 0; removeIndex < keysToRemove.length; removeIndex += 1) {
        window.localStorage.removeItem(keysToRemove[removeIndex]);
      }
      return keysToRemove.length;
    } catch (error) {
      void error;
      return 0;
    }
  }

  function clearPrivateDeviceData(client, navigateToCleanShell) {
    purgeLegacyPrivateDeviceStorage();
    ephemeralDeviceIds = Object.create(null);
    ephemeralObserverId = "";

    if (client) {
      client.localReplayQueue = [];
      client.continuityPayload = null;
      client.visibleHandoffUrl = "";
      client.ownerRouteShareStatus = "";
      client.manualHits = 0;
      client.manualGlitch = false;
      client.statusMessage = "Private play state was cleared from this device. Reopen a trusted session link to continue.";
    }

    if (navigateToCleanShell === true && window.location) {
      var roleName = client && client.roleName ? client.roleName : inferRoleFromPath(window.location.pathname);
      var cleanRoute = "/mobile/" + mobileModeSegment(roleName || "Player");
      window.location.replace(cleanRoute);
    }
  }

  function devicePrefixForRole(roleName) {
    if (isGameMasterRole(roleName)) {
      return "gm-shell";
    }

    var lowered = String(roleName || "").toLowerCase();
    if (lowered.indexOf("observer") >= 0) {
      return "observer-shell";
    }

    return "player-shell";
  }

  function resolveStableDeviceId(sessionId, roleName, explicitDeviceId) {
    var normalizedSessionId = String(sessionId || "").trim();
    if (explicitDeviceId) {
      return explicitDeviceId;
    }

    var memoryKey = normalizedSessionId + ":" + deviceRoleSegment(roleName);
    if (!ephemeralDeviceIds[memoryKey]) {
      ephemeralDeviceIds[memoryKey] = makeStableId(devicePrefixForRole(roleName));
    }
    return ephemeralDeviceIds[memoryKey];
  }

  function deviceRoleSegment(roleName) {
    if (isGameMasterRole(roleName)) {
      return "gm";
    }
    var lowered = String(roleName || "").toLowerCase();
    if (lowered.indexOf("observer") >= 0) {
      return "observer";
    }
    return "player";
  }

  function inferRoleFromPath(pathname) {
    var normalized = String(pathname || "").toLowerCase().replace(/\/+$/, "");
    if (normalized === "/mobile/gm") {
      return "GameMaster";
    }
    if (normalized === "/mobile/observer") {
      return "Observer";
    }
    if (normalized === "/mobile/player") {
      return "Player";
    }
    return "";
  }

  function resolveResumeRoute(params, requestedRoleName) {
    void params;
    void requestedRoleName;
    return null;
  }

  function saveLastRoute(client) {
    void client;
  }

  function persistClientState(client) {
    void client;
  }

  function buildInitialStatusMessage(bootstrapMismatch, bootstrap, resumeRoute) {
    void resumeRoute;
    if (bootstrapMismatch) {
      return "The requested session differs from the server shell. Reconnect through a trusted session link; Chummer did not restore private state from browser storage.";
    }

    return bootstrap.statusMessage || "Turn companion loaded. Private play state stays in memory for this open page only.";
  }

  function resolveObserverId() {
    if (!ephemeralObserverId) {
      ephemeralObserverId = makeStableId("observer");
    }
    return ephemeralObserverId;
  }

  function attachHandlers(root, client) {
    if (root.getAttribute("data-handlers-attached") === "true") {
      attachRoleLinkAnalytics(client);
      return;
    }

    root.setAttribute("data-handlers-attached", "true");
    root.addEventListener("click", function (event) {
      var control = event.target.closest("[data-turn-kind]");
      if (!control || control.disabled) {
        return;
      }

      var turnKind = control.getAttribute("data-turn-kind");
      if (!isClickHandledTurnKind(turnKind)) {
        return;
      }

      stopEvent(event);
      switch (turnKind) {
        case "adjust-metric":
          adjustMetric(client, control.getAttribute("data-metric-id"), parseInt(control.getAttribute("data-delta") || "0", 10));
          break;
        case "select-action":
          selectAction(client, control.getAttribute("data-action-id"));
          break;
        case "resolve-digital":
          resolveAction(client, false);
          break;
        case "resolve-manual":
          resolveAction(client, true);
          break;
        case "queue-quick-action":
          queueQuickActionLocally(client, control.getAttribute("data-action-id"));
          break;
        case "replay-local-queue":
          void replayLocalQueue(client);
          break;
        case "ack-server-queue":
          void acknowledgeServerQueue(client);
          break;
        case "claim-device":
          void claimContinuityOnThisDevice(client);
          break;
        case "install-shell":
          void installShell(client);
          break;
        case "share-owner-route":
          void shareOwnerRoute(client);
          break;
        case "clear-private-device-data":
          clearPrivateDeviceData(client, true);
          break;
      }
    }, true);

    root.addEventListener("change", function (event) {
      var control = event.target.closest("[data-turn-kind]");
      if (!control || control.disabled) {
        return;
      }

      var turnKind = control.getAttribute("data-turn-kind");
      if (turnKind !== "toggle-modifier" && turnKind !== "select-anchor") {
        return;
      }

      stopEvent(event);
      switch (turnKind) {
        case "toggle-modifier":
          toggleModifier(client, control.getAttribute("data-modifier-id"), control.checked === true);
          break;
        case "select-anchor":
          selectAnchor(client, control.value);
          break;
      }
    }, true);

    var manualHits = document.getElementById("manual-hits");
    var manualGlitch = document.getElementById("manual-glitch");
    if (manualHits) {
      manualHits.addEventListener("input", function () {
        client.manualHits = Math.max(0, parseInt(manualHits.value || "0", 10) || 0);
      });
    }

    if (manualGlitch) {
      manualGlitch.addEventListener("change", function () {
        client.manualGlitch = manualGlitch.checked === true;
      });
    }

    attachRoleLinkAnalytics(client);
  }

  function isClickHandledTurnKind(turnKind) {
    switch (turnKind) {
      case "adjust-metric":
      case "select-action":
      case "resolve-digital":
      case "resolve-manual":
      case "queue-quick-action":
      case "replay-local-queue":
      case "ack-server-queue":
      case "claim-device":
      case "install-shell":
      case "share-owner-route":
      case "clear-private-device-data":
        return true;
      default:
        return false;
    }
  }

  function attachRoleLinkAnalytics(client) {
    var links = document.querySelectorAll("[data-role-name]");
    for (var index = 0; index < links.length; index += 1) {
      if (links[index].dataset.analyticsAttached === "true") {
        continue;
      }

      links[index].dataset.analyticsAttached = "true";
      links[index].addEventListener("click", function (event) {
        var targetRole = event.currentTarget.getAttribute("data-role-name") || "";
        var targetMode = roleSegmentForAnalytics(targetRole);
        if (targetMode !== roleSegmentForAnalytics(client.roleName)) {
          trackMobileEvent("mobile_role_switch", client, {
            targetRole: analyticsRole(targetRole),
            targetMode: targetMode
          });
        }

        if (window.__chummerPlaySuppressRoleNavigation === true) {
          event.preventDefault();
        }
      }, true);
    }
  }

  function adjustMetric(client, metricId, delta) {
    if (!client.projection.canMutate || !metricId || !delta) {
      return;
    }

    var projection = client.projection;
    if (metricId.indexOf("inventory:") === 0) {
      var itemId = metricId.slice("inventory:".length);
      var item = findInventoryCard(projection, itemId);
      if (!item) {
          return;
      }

      item.quantity = Math.max(0, item.quantity + delta);
      appendHistory(
        projection,
        "Inventory adjusted",
        item.label + " changed by " + formatSigned(delta) + " and is now " + item.quantity + ".",
        false,
        null
      );
      queueLocalEvent(client, "turn:inventory:" + itemId + ":" + signedToken(delta));
    } else {
      var card = findStatCard(projection, metricId);
      if (!card) {
        return;
      }

      card.value = Math.max(0, card.value + delta);
      appendHistory(
        projection,
        humanizeMetric(metricId) + " adjusted",
        humanizeMetric(metricId) + " changed by " + formatSigned(delta) + ".",
        false,
        null
      );
      queueLocalEvent(client, "turn:metric:" + metricId + ":" + signedToken(delta));
    }

    projection.localRevision += 1;
    client.statusMessage = "Updated " + metricId + " locally.";
    render(client);
  }

  function selectAction(client, actionId) {
    if (!client.projection.canMutate || !actionId) {
      return;
    }

    var actions = client.projection.act.actions || [];
    for (var index = 0; index < actions.length; index += 1) {
      actions[index].selected = actions[index].actionId === actionId;
    }

    client.projection.localRevision += 1;
    queueLocalEvent(client, "turn:action:" + actionId);
    client.statusMessage = "Selected " + selectedAction(client.projection).label + ".";
    render(client);
  }

  function toggleModifier(client, modifierId, enabled) {
    if (!client.projection.canMutate || !modifierId) {
      return;
    }

    var options = client.projection.adjust.options || [];
    for (var index = 0; index < options.length; index += 1) {
      if (options[index].modifierId === modifierId) {
        options[index].enabled = enabled;
        client.projection.localRevision += 1;
        appendHistory(
          client.projection,
          options[index].label + " " + (enabled ? "enabled" : "disabled"),
          options[index].label + " now contributes " + formatSigned(options[index].diceModifier) + " dice from " + options[index].source + ".",
          false,
          null
        );
        client.statusMessage = "Updated " + modifierId + " locally.";
        queueLocalEvent(client, "turn:modifier:" + modifierId + ":" + (enabled ? "on" : "off"));
        render(client);
        return;
      }
    }
  }

  function selectAnchor(client, anchorId) {
    if (!client.projection.canMutate || !anchorId) {
      return;
    }

    var anchors = client.projection.runsite.anchors || [];
    for (var index = 0; index < anchors.length; index += 1) {
      anchors[index].selected = anchors[index].anchorId === anchorId;
    }

    client.projection.runsite.selectedAnchorId = anchorId;
    client.projection.localRevision += 1;
    client.statusMessage = "Updated RUNSITE anchor locally.";
    queueLocalEvent(client, "turn:anchor:" + anchorId);
    render(client);
  }

  function resolveAction(client, useManualEntry) {
    if (!client.projection.canMutate) {
      return;
    }

    var projection = client.projection;
    var action = selectedAction(projection);
    if (!action) {
      return;
    }

    var dicePool = Math.max(0, projection.resolve.dicePool || 0);
    var result = useManualEntry
      ? { hits: Math.max(0, client.manualHits || 0), glitch: client.manualGlitch === true, mode: "manual" }
      : rollDicePool(dicePool);
    var before = snapshotCounters(projection);
    applyActionDelta(projection, action);
    projection.localRevision += 1;

    var detail = action.label + ": " + result.hits + " hit(s) on " + dicePool + " dice via " + result.mode + " entry. "
      + buildResolveDeltaSummary(before, projection, action)
      + (result.glitch ? " Glitch posture flagged." : "");
    appendHistory(projection, action.label + " resolved", detail, useManualEntry, result.hits);
    projection.resolve.lastOutcomeSummary = detail;
    queueLocalEvent(client, "turn:resolve:" + action.actionId + ":" + result.mode + ":" + result.hits + ":" + (result.glitch ? "glitch1" : "glitch0"));
    client.statusMessage = useManualEntry ? "Manual receipt captured locally." : "Digital receipt captured locally.";
    render(client);
  }

  function render(client) {
    var logicalFocus = captureLogicalFocus();
    normalizeProjection(client.projection);
    recomputeProjection(client.projection);

    var projection = client.projection;
    setBanner(client);
    setText("turn-shell-summary", projection.shellSummary);
    setText("turn-boundary-summary", projection.localBoundarySummary);
    setText("turn-scene-summary", projection.currentSceneSummary);
    setText("turn-status-message", client.statusMessage || "");
    setHidden("turn-status-message", !client.statusMessage);
    setText("turn-install-status", client.serviceWorkerStatus || "");
    renderInstallSurface(client);

    setText("turn-trust-status", projection.trust.statusLabel);
    setClassName("turn-trust-status", "status-pill " + trustToneClass(projection.trust.statusLabel));
    setText("turn-checkpoint-label", projection.trust.checkpointLabel);
    setText("turn-runtime-label", projection.trust.runtimeLabel);
    setText("turn-queue-label", projection.trust.queueLabel);
    setText("turn-trust-summary", projection.trust.summary);
    renderTrustLabels(projection.trust.labels || []);

    setText("turn-runsite-summary", projection.runsite.summary);
    setText("turn-runsite-posture", projection.runsite.truthPosture);
    renderAnchorSelect(projection);

    setText("turn-actor-label", projection.now.actorLabel);
    setText("turn-weapon-label", projection.now.weaponLabel);
    renderQuickGlance(client);
    renderMetricGrid(projection);
    renderInventoryGrid(projection);
    renderActionGrid(projection);
    renderModifierList(projection);

    setText("turn-resolve-label", projection.resolve.selectedActionLabel);
    setText("turn-resolve-pool", projection.resolve.dicePool + " dice");
    setText("turn-odds-summary", projection.resolve.oddsSummary);
    setText("turn-manual-hint", projection.resolve.manualEntryHint);
    setText("turn-last-outcome", projection.resolve.lastOutcomeSummary);
    renderOddsGrid(projection.resolve.odds || []);
    updateRoleLinks(client);
    renderSyncSurface(client);
    renderClaimedDeviceSurface(client);

    setText("turn-history-summary", projection.history.summary);
    renderHistoryList(projection.history.entries || []);

    var manualHits = document.getElementById("manual-hits");
    var manualGlitch = document.getElementById("manual-glitch");
    if (manualHits) {
      manualHits.value = String(client.manualHits || 0);
    }

    if (manualGlitch) {
      manualGlitch.checked = client.manualGlitch === true;
    }

    saveLastRoute(client);
    saveSnapshot(client);
    restoreLogicalFocus(logicalFocus);
  }

  function captureLogicalFocus() {
    var active = document.activeElement;
    if (!active || typeof active.getAttribute !== "function" || !active.getAttribute("data-turn-kind")) {
      return null;
    }

    var attributeNames = [
      "data-turn-kind",
      "data-metric-id",
      "data-delta",
      "data-action-id",
      "data-modifier-id"
    ];
    var attributes = {};
    for (var index = 0; index < attributeNames.length; index += 1) {
      attributes[attributeNames[index]] = active.getAttribute(attributeNames[index]);
    }

    return attributes;
  }

  function restoreLogicalFocus(logicalFocus) {
    if (!logicalFocus) {
      return;
    }

    var candidates = document.querySelectorAll("[data-turn-kind]");
    var attributeNames = Object.keys(logicalFocus);
    for (var index = 0; index < candidates.length; index += 1) {
      var matches = true;
      for (var attributeIndex = 0; attributeIndex < attributeNames.length; attributeIndex += 1) {
        var attributeName = attributeNames[attributeIndex];
        if (candidates[index].getAttribute(attributeName) !== logicalFocus[attributeName]) {
          matches = false;
          break;
        }
      }

      if (!matches || candidates[index].disabled || typeof candidates[index].focus !== "function") {
        continue;
      }

      try {
        candidates[index].focus({ preventScroll: true });
      } catch (error) {
        void error;
        candidates[index].focus();
      }
      return;
    }
  }

  function recomputeProjection(projection) {
    var action = selectedAction(projection);
    var modifierTotal = 0;
    var options = projection.adjust.options || [];
    for (var index = 0; index < options.length; index += 1) {
      if (options[index].enabled) {
        modifierTotal += options[index].diceModifier;
      }
    }

    projection.act.selectedActionId = action.actionId;
    projection.act.selectedActionLabel = action.label;
    projection.adjust.totalModifier = modifierTotal;
    projection.adjust.summary = buildModifierSummary(modifierTotal, options);

    var dicePool = Math.max(0, (action.baseDicePool || 0) + modifierTotal);
    projection.resolve.selectedActionLabel = action.label;
    projection.resolve.dicePool = dicePool;
    projection.resolve.oddsSummary = buildOddsSummary(dicePool);
    projection.resolve.odds = buildOddsBadges(dicePool);

    var ammo = statValue(projection, "ammo");
    var reserve = statValue(projection, "reserve");
    projection.now.weaponLabel = action.ammoCost > 0
      ? "Active action: " + action.label + " · magazine " + ammo + " · reserve " + reserve
      : "Active action: " + action.label + " · queue-safe local tracking";

    var anchor = selectedAnchor(projection);
    projection.runsite.selectedAnchorId = anchor.anchorId;
    projection.runsite.summary = "RUNSITE anchor: " + anchor.label + ". Keep spatial orientation visible without turning this shell into exact tactical-position truth.";
    projection.history.summary = buildHistorySummary(projection.history.entries || [], projection.pendingQueueCount || 0);
  }

  function applyActionDelta(projection, action) {
    if (action.ammoCost > 0) {
      setStatValue(projection, "ammo", Math.max(0, statValue(projection, "ammo") - action.ammoCost));
    }

    if (action.actionId === "reload") {
      var ammo = statValue(projection, "ammo");
      var reserve = statValue(projection, "reserve");
      var needed = Math.max(0, magazineCapacity - ammo);
      var transferred = Math.min(needed, reserve);
      setStatValue(projection, "ammo", ammo + transferred);
      setStatValue(projection, "reserve", reserve - transferred);
    }

    if (action.actionId === "cast-or-sustain") {
      setStatValue(projection, "charges", Math.max(0, statValue(projection, "charges") - 1));
    }

    if (action.actionId === "use-consumable") {
      var item = findInventoryCard(projection, "stim-patch");
      if (item) {
        item.quantity = Math.max(0, item.quantity - 1);
      }
    }
  }

  function buildResolveDeltaSummary(before, projection, action) {
    var deltas = [];
    var afterAmmo = statValue(projection, "ammo");
    var afterReserve = statValue(projection, "reserve");
    var afterCharges = statValue(projection, "charges");

    if (before.ammoInMagazine !== afterAmmo) {
      deltas.push("Magazine " + before.ammoInMagazine + " -> " + afterAmmo);
    }

    if (before.ammoReserve !== afterReserve) {
      deltas.push("Reserve " + before.ammoReserve + " -> " + afterReserve);
    }

    if (before.charges !== afterCharges) {
      deltas.push("Charges " + before.charges + " -> " + afterCharges);
    }

    if (action.actionId === "use-consumable") {
      var item = findInventoryCard(projection, "stim-patch");
      if (item) {
        deltas.push(item.label + " now " + item.quantity);
      }
    }

    return deltas.length === 0
      ? "No automatic local counter change was applied."
      : deltas.join(" · ");
  }

  function appendHistory(projection, title, detail, manual, hits) {
    var queued = (projection.pendingQueueCount || 0) > 0;
    var entries = projection.history.entries || [];
    entries.unshift({
      title: title,
      detail: detail,
      when: formatUtcTime(new Date()),
      queued: queued,
      manual: manual === true,
      hits: hits
    });

    projection.history.entries = entries.slice(0, historyLimit);
  }

  function renderSyncSurface(client) {
    var projection = client.projection;
    var sync = projection.sync || { quickActions: [] };
    setText("turn-sync-summary", sync.pendingSummary || "");
    setText("turn-sync-reconnect", sync.reconnectSummary || "");
    setText("turn-sync-claim", sync.claimedDeviceSummary || "");
    setText("turn-local-queue-count", String(client.localReplayQueue.length));
    setText("turn-server-queue-count", String(projection.pendingQueueCount || 0));
    setText("turn-network-state", client.networkBusy ? "Busy" : (navigator.onLine ? "Online" : "Offline"));
    renderQueueActionGrid(client, sync.quickActions || []);

    setButtonDisabled("turn-replay-local-button", client.networkBusy || !projection.canMutate || client.localReplayQueue.length === 0 || !navigator.onLine);
    setButtonDisabled("turn-ack-server-button", client.networkBusy || !projection.canMutate || (projection.pendingQueueCount || 0) === 0 || !navigator.onLine);
  }

  function updateRoleLinks(client) {
    var links = document.querySelectorAll("[data-role-name]");
    for (var index = 0; index < links.length; index += 1) {
      var roleName = links[index].getAttribute("data-role-name") || client.roleName;
      var isCurrentRole = roleSegmentForAnalytics(roleName) === roleSegmentForAnalytics(client.roleName);
      links[index].setAttribute("href", isCurrentRole ? mobileHref() : mobileRoleHref(roleName));
    }
  }

  function renderClaimedDeviceSurface(client) {
    var continuityPayload = client.continuityPayload;
    var continuity = continuityPayload && continuityPayload.continuity ? continuityPayload.continuity : null;
    var ownerRoute = continuityPayload && continuityPayload.continuityOwnerRoute
      ? continuityPayload.continuityOwnerRoute
      : claimedTurnRoute(client.sessionId, client.roleName, client.deviceId);
    var normalizedOwnerRoute = normalizePlayRouteForMobileShell(
      ownerRoute,
      client.roleName,
      client.sessionId,
      client.deviceId
    ) || claimedTurnRoute(client.sessionId, client.roleName, client.deviceId);
    var claimMatchesThisDevice = continuity && continuity.deviceId === client.deviceId;
    var hasContinuityCursor = continuityPayload
      && continuityPayload.projection
      && continuityPayload.projection.cursor;

    setText("turn-continuity-device", client.deviceId);
    var visibleOwnerRoute = client.visibleHandoffUrl || normalizedOwnerRoute;
    var visibleOwnerRouteLabel = client.visibleHandoffUrl ? "Open session handoff link" : "Open owner route";
    var visibleOwnerRoutePrefix = client.visibleHandoffUrl ? "Session handoff" : "Owner route";
    setText(
      "turn-owner-route-copy",
      visibleOwnerRoutePrefix + ": " + visibleOwnerRoute + ". This handoff remains available only while this page stays open; reopening requires a trusted session link."
    );
    setLink("turn-owner-route-link", visibleOwnerRoute, visibleOwnerRouteLabel);
    setText("turn-owner-route-share-status", client.ownerRouteShareStatus || "");
    setButtonDisabled("turn-share-owner-route-button", !normalizedOwnerRoute);

    if (continuity && continuity.continuityToken) {
      setText("turn-continuity-status", claimMatchesThisDevice ? "Claimed on this device" : ("Claimed on " + continuity.deviceId));
      setText(
        "turn-continuity-detail",
        "Continuity token " + continuity.continuityToken + " tracks " + continuity.role + " through sequence " + continuity.observedThroughSequence + "."
      );
      setText("turn-continuity-token", continuity.continuityToken);
      setText("turn-continuity-sequence", String(continuity.observedThroughSequence));
      setButtonText("turn-claim-device-button", claimMatchesThisDevice ? "Refresh this device claim" : "Claim this device");
    } else {
      setText("turn-continuity-status", "Not claimed on this device");
      setText(
        "turn-continuity-detail",
        "Claim this open-tab session before the next handoff so the owner route and replay-safe sequence remain available while this page stays open. Reopening requires a trusted session link."
      );
      setText("turn-continuity-token", "Pending");
      setText("turn-continuity-sequence", "0");
      setButtonText("turn-claim-device-button", "Claim this device");
    }

    setButtonDisabled("turn-claim-device-button", client.networkBusy || !navigator.onLine || !hasContinuityCursor);
  }

  function renderQueueActionGrid(client, actions) {
    var element = document.getElementById("turn-queue-action-grid");
    if (!element) {
      return;
    }

    if (!actions.length) {
      element.innerHTML = "<p class=\"support-copy\">No replay-safe quick action is available for this lane.</p>";
      return;
    }

    element.innerHTML = actions.map(function (action) {
      return "<button type=\"button\" class=\"action-tile\" data-turn-kind=\"queue-quick-action\" data-action-id=\"" + escapeAttribute(action.actionId) + "\""
        + (!client.projection.canMutate || client.networkBusy ? " disabled" : "") + ">"
        + "<span class=\"action-label\">" + escapeHtml(action.label) + "</span>"
        + "<span class=\"action-summary\">" + escapeHtml(action.summary) + "</span>"
        + "</button>";
    }).join("");
  }

  function queueQuickActionLocally(client, actionId) {
    if (!client.projection.canMutate || !actionId) {
      return;
    }

    appendHistory(
      client.projection,
      "Quick action queued",
      actionId + " is staged in this open tab and can be replayed when the owner route reconnects. It will be discarded if this page closes or reloads first.",
      false,
      null
    );
    queueLocalEvent(client, "quick-action:" + actionId);
    client.projection.localRevision += 1;
    client.statusMessage = "Queued " + actionId + " locally for replay.";
    render(client);
  }

  async function replayLocalQueue(client) {
    if (!client.localReplayQueue.length) {
      client.statusMessage = "No local replay receipts are queued in this open tab.";
      render(client);
      return;
    }

    if (!navigator.onLine) {
      client.statusMessage = "Reconnect first, then replay the local queue into the server ledger.";
      render(client);
      return;
    }

    client.networkBusy = true;
    client.statusMessage = "Replaying local receipts into the server queue.";
    render(client);

    try {
      var response = await fetch(replayRoute(client.sessionId, client.roleName, client.deviceId), {
        method: "POST",
        headers: { "content-type": "application/json" },
        body: JSON.stringify({ events: client.localReplayQueue })
      });
      var payload = parseJson(await response.text());
      if (!response.ok || !payload) {
        throw new Error("replay_failed");
      }

      applyServerQueueStatus(client, payload);
      await refreshClaimedDeviceStatus(client);
      if (payload.accepted) {
        client.localReplayQueue = [];
      }
      client.statusMessage = payload.message || "Replay request completed.";
    } catch (error) {
      void error;
      client.statusMessage = "Replay failed before the server queue could confirm the local receipts.";
    } finally {
      client.networkBusy = false;
      render(client);
    }
  }

  async function acknowledgeServerQueue(client) {
    if ((client.projection.pendingQueueCount || 0) === 0) {
      client.statusMessage = "Server replay queue is already empty.";
      render(client);
      return;
    }

    if (!navigator.onLine) {
      client.statusMessage = "Reconnect first, then acknowledge the queued server events.";
      render(client);
      return;
    }

    client.networkBusy = true;
    client.statusMessage = "Acknowledging the queued server events after reconnect confirmation.";
    render(client);

    try {
      var response = await fetch(acknowledgeRoute(client.sessionId, client.roleName, client.deviceId), {
        method: "POST"
      });
      var payload = parseJson(await response.text());
      if (!response.ok || !payload) {
        throw new Error("acknowledge_failed");
      }

      applyServerQueueStatus(client, payload);
      await refreshClaimedDeviceStatus(client);
      client.statusMessage = payload.message || "Queue acknowledgement completed.";
    } catch (error) {
      void error;
      client.statusMessage = "Queue acknowledgement failed before the owner lane could confirm sync.";
    } finally {
      client.networkBusy = false;
      render(client);
    }
  }

  function applyServerQueueStatus(client, payload) {
    if (typeof payload.pendingQueueCount === "number") {
      client.projection.pendingQueueCount = payload.pendingQueueCount;
    }

    if (payload.currentSceneSummary) {
      client.projection.currentSceneSummary = payload.currentSceneSummary;
    }

    if (payload.trust) {
      client.projection.trust = payload.trust;
    }

    if (payload.sync) {
      client.projection.sync = payload.sync;
    }
  }

  async function refreshQueueStatus(client, statusMessage) {
    client.networkBusy = true;
    client.statusMessage = statusMessage;
    render(client);

    try {
      var response = await fetch(queueStatusRoute(client.sessionId, client.roleName, client.deviceId));
      var payload = parseJson(await response.text());
      if (!response.ok || !payload) {
        throw new Error("queue_status_failed");
      }

      applyServerQueueStatus(client, payload);
      await refreshClaimedDeviceStatus(client);
      client.statusMessage = payload.message || "Queue status refreshed.";
    } catch (error) {
      void error;
      client.statusMessage = "Queue status refresh failed. Local tracking stays available in this open tab until the page closes or reloads.";
    } finally {
      client.networkBusy = false;
      render(client);
    }
  }

  async function refreshClaimedDeviceStatus(client) {
    var response = await fetch(observeRoute(client.sessionId));
    var payload = parseJson(await response.text());
    if (!response.ok || !payload) {
      throw new Error("observe_failed");
    }

    client.continuityPayload = {
      sessionId: payload.sessionId || client.sessionId,
      projection: payload.projection || null,
      checkpoint: payload.checkpoint || null,
      continuity: payload.continuity || null,
      runtimeBundle: payload.runtimeBundle || null,
      continuityOwnerRoute: claimedTurnRoute(client.sessionId, client.roleName, client.deviceId)
    };
  }

  async function bootInstallBoundary(client) {
    if (!("serviceWorker" in navigator)) {
      client.serviceWorkerStatus = "This browser cannot register the public-shell service worker boundary for the mobile app.";
      render(client);
      return;
    }

    bindServiceWorkerNetworkMessages();
    try {
      await navigator.serviceWorker.register("/mobile/service-worker.js", { scope: "/mobile/" });
      publishNetworkStateToServiceWorker();
      publishCurrentRouteToServiceWorker();
      client.serviceWorkerStatus = navigator.serviceWorker.controller
        ? "Service worker is active; public shell assets can be cached, while private table state remains open-tab only."
        : "Service worker registered; public shell asset caching begins after the next load, and private table state remains open-tab only.";
    } catch (error) {
      console.error("service worker registration failed", error);
      client.serviceWorkerStatus = "Service worker registration failed, so public shell asset caching is unavailable until this page reloads cleanly. Private table state was not cached.";
    }

    render(client);
  }

  function bindServiceWorkerNetworkMessages() {
    if (!("serviceWorker" in navigator) || window[serviceWorkerNetworkMessageBoundName]) {
      return;
    }

    window[serviceWorkerNetworkMessageBoundName] = true;
    navigator.serviceWorker.addEventListener("message", function (event) {
      var data = event && event.data ? event.data : {};
      if (data.type === "chummer-play-network-state-ack") {
        window[serviceWorkerNetworkAckName] = data.online === true;
      }
    });
    navigator.serviceWorker.addEventListener("controllerchange", function () {
      publishNetworkStateToServiceWorker();
      publishCurrentRouteToServiceWorker();
    });
  }

  function publishNetworkStateToServiceWorker() {
    if (!("serviceWorker" in navigator)) {
      return;
    }

    var message = {
      type: "chummer-play-network-state",
      online: navigator.onLine === true
    };
    window[serviceWorkerNetworkAckName] = null;
    if (navigator.serviceWorker.controller && typeof navigator.serviceWorker.controller.postMessage === "function") {
      navigator.serviceWorker.controller.postMessage(message);
    }

    if (navigator.serviceWorker.ready && typeof navigator.serviceWorker.ready.then === "function") {
      navigator.serviceWorker.ready
        .then(function (registration) {
          var worker = registration && (registration.active || registration.waiting || registration.installing);
          if (worker && worker !== navigator.serviceWorker.controller && typeof worker.postMessage === "function") {
            worker.postMessage(message);
          }
        })
        .catch(function () {
          window[serviceWorkerNetworkAckName] = null;
        });
    }
  }

  function publishCurrentRouteToServiceWorker() {
    if (!("serviceWorker" in navigator)) {
      return;
    }

    var message = {
      type: "chummer-play-cache-current-route",
      pathname: window.location && window.location.pathname ? window.location.pathname : "/mobile/player"
    };

    if (navigator.serviceWorker.controller && typeof navigator.serviceWorker.controller.postMessage === "function") {
      navigator.serviceWorker.controller.postMessage(message);
    }

    if (navigator.serviceWorker.ready && typeof navigator.serviceWorker.ready.then === "function") {
      navigator.serviceWorker.ready
        .then(function (registration) {
          var worker = registration && (registration.active || registration.waiting || registration.installing);
          if (worker && worker !== navigator.serviceWorker.controller && typeof worker.postMessage === "function") {
            worker.postMessage(message);
          }
        })
        .catch(function () {
          // Route warming is best-effort; the shell still works online without it.
        });
    }
  }

  function renderInstallSurface(client) {
    var installed = isInstalledShell();
    var buttonLabel = "Install app";
    var detail = "Install the public shell assets for quick reopening. Private table state remains in this open tab only.";
    var disabled = false;

    if (installed) {
      buttonLabel = "Installed";
      detail = "This device has the public turn-companion shell installed. Reopen it from the app icon, then use a trusted session link; private table state is not restored from the install cache.";
      disabled = true;
    } else if (client.installBusy) {
      buttonLabel = "Opening install prompt";
      detail = "Confirm the browser install flow to pin the public shell assets on the device. Private table state remains in this open tab only.";
      disabled = true;
    } else if (client.installPromptEvent && typeof client.installPromptEvent.prompt === "function") {
      buttonLabel = "Install app";
      detail = "Use the browser install prompt to pin the public shell assets before the next play session. Private table state is never installed.";
    } else if (isAppleMobileBrowser()) {
      buttonLabel = "Add to Home Screen";
      detail = "Use Safari Share and choose Add to Home Screen to install the public shell assets. Private table state remains in this open tab only.";
    } else {
      buttonLabel = "Install from browser menu";
      detail = "The inline prompt is not available yet. Use the browser install menu after the service worker registers; it caches public shell assets, never private table state.";
    }

    setButtonText("turn-install-button", buttonLabel);
    setButtonDisabled("turn-install-button", disabled);
    setText("turn-install-detail", detail);
  }

  async function installShell(client) {
    if (isInstalledShell()) {
      client.serviceWorkerStatus = "This shell is already installed on this device.";
      render(client);
      return;
    }

    if (client.installPromptEvent && typeof client.installPromptEvent.prompt === "function") {
      client.installBusy = true;
      client.serviceWorkerStatus = "Opening the install prompt for this device.";
      trackMobileEvent("mobile_install_prompt_open", client, { installPrompt: "open" });
      render(client);

      try {
        var promptEvent = client.installPromptEvent;
        await promptEvent.prompt();
        var choice = promptEvent.userChoice ? await promptEvent.userChoice : null;
        if (choice && choice.outcome === "accepted") {
          client.serviceWorkerStatus = "Install accepted. Finish the browser flow to pin this shell on the device.";
          trackMobileEvent("mobile_install_prompt_choice", client, { installPrompt: "accepted" });
        } else {
          client.serviceWorkerStatus = "Install dismissed. You can reopen the prompt from this shell when the browser offers it again.";
          trackMobileEvent("mobile_install_prompt_choice", client, { installPrompt: "dismissed" });
        }
        client.installPromptEvent = null;
      } catch (error) {
        console.error("install prompt failed", error);
        client.serviceWorkerStatus = "Install prompt failed. Use the browser install menu until this shell can prompt again.";
        trackMobileEvent("mobile_install_prompt_choice", client, { installPrompt: "failed" });
      } finally {
        client.installBusy = false;
        render(client);
      }
      return;
    }

    client.serviceWorkerStatus = isAppleMobileBrowser()
      ? "Open Safari Share and choose Add to Home Screen to install this shell on this device."
      : "Use the browser install menu to add this shell to the device because the inline prompt is not available yet.";
    trackMobileEvent("mobile_install_prompt_unavailable", client, {
      installPrompt: isAppleMobileBrowser() ? "ios_manual" : "browser_menu"
    });
    render(client);
  }

  async function shareOwnerRoute(client) {
    var ownerRoute = readOwnerRouteHref();
    if (!ownerRoute) {
      client.ownerRouteShareStatus = "Owner route is not ready yet.";
      setText("turn-owner-route-share-status", client.ownerRouteShareStatus);
      saveSnapshot(client);
      return;
    }

    var shareUrl = absoluteMobileUrl(sessionHandoffHref(ownerRoute, client));
    var shareTitle = "Chummer " + analyticsRole(client && client.roleName ? client.roleName : "Player") + " session handoff";
    if (navigator.share && typeof navigator.share === "function") {
      try {
        await navigator.share({
          title: shareTitle,
          url: shareUrl
        });
        client.ownerRouteShareStatus = "Session handoff shared.";
        setText("turn-owner-route-share-status", client.ownerRouteShareStatus);
        saveSnapshot(client);
        trackMobileEvent("mobile_session_handoff_share", client, { shareMethod: "native" });
        return;
      } catch (error) {
        if (error && error.name === "AbortError") {
          client.ownerRouteShareStatus = "Share cancelled.";
          setText("turn-owner-route-share-status", client.ownerRouteShareStatus);
          saveSnapshot(client);
          return;
        }
      }
    }

    if (navigator.clipboard && typeof navigator.clipboard.writeText === "function") {
      try {
        await navigator.clipboard.writeText(shareUrl);
        client.ownerRouteShareStatus = "Session handoff copied to clipboard.";
        setText("turn-owner-route-share-status", client.ownerRouteShareStatus);
        saveSnapshot(client);
        trackMobileEvent("mobile_session_handoff_share", client, { shareMethod: "clipboard" });
        return;
      } catch (error) {
        void error;
      }
    }

    writeHandoffLink(shareUrl);
    trackMobileEvent("mobile_session_handoff_share", client, { shareMethod: "link" });
  }

  function writeHandoffLink(shareUrl) {
    var client = window[activeClientName];
    if (!client) {
      return;
    }

    client.visibleHandoffUrl = shareUrl;
    client.ownerRouteShareStatus = "Session handoff is ready in the link above.";
    setLink("turn-owner-route-link", shareUrl, "Open session handoff link");
    setText("turn-owner-route-share-status", client.ownerRouteShareStatus);
    render(client);
    saveSnapshot(client);
  }

  function readOwnerRouteHref() {
    var ownerRouteLink = document.getElementById("turn-owner-route-link");
    if (!ownerRouteLink) {
      return "";
    }

    return ownerRouteLink.getAttribute("href") || "";
  }

  function absoluteMobileUrl(href) {
    try {
      return new URL(href, window.location.origin).toString();
    } catch {
      return String(href || "");
    }
  }

  function sessionHandoffHref(ownerRoute, client) {
    void ownerRoute;
    try {
      var roleName = (client && client.roleName) || "Player";
      return "/mobile/" + mobileModeSegment(roleName);
    } catch {
      return "/mobile/player";
    }
  }

  function sanitizeVisibleHandoffUrl(value) {
    if (!value) {
      return "";
    }

    try {
      var url = new URL(String(value), window.location.origin);
      if (url.origin !== window.location.origin || url.pathname.indexOf("/mobile/") !== 0) {
        return "";
      }

      return url.toString();
    } catch {
      return "";
    }
  }

  function sanitizeShareStatus(value) {
    var status = String(value || "");
    switch (status) {
      case "Owner route is not ready yet.":
      case "Session handoff shared.":
      case "Session handoff copied to clipboard.":
      case "Session handoff is ready in the link above.":
      case "Share cancelled.":
        return status;
      default:
        return "";
    }
  }

  function isInstalledShell() {
    return (window.matchMedia && window.matchMedia("(display-mode: standalone)").matches)
      || window.navigator.standalone === true;
  }

  function isAppleMobileBrowser() {
    var userAgent = window.navigator.userAgent || "";
    return /iPhone|iPad|iPod/i.test(userAgent);
  }

  async function refreshNetworkSurfaces(client, statusMessage) {
    client.networkBusy = true;
    client.statusMessage = statusMessage;
    render(client);

    try {
      var queueResponse = await fetch(queueStatusRoute(client.sessionId, client.roleName, client.deviceId));
      var queuePayload = parseJson(await queueResponse.text());
      if (!queueResponse.ok || !queuePayload) {
        throw new Error("queue_status_failed");
      }

      applyServerQueueStatus(client, queuePayload);
      await refreshClaimedDeviceStatus(client);
      client.statusMessage = queuePayload.message || "Queue and claimed-device status refreshed.";
    } catch (error) {
      void error;
      client.statusMessage = "Network refresh failed. Local tracking remains available only while this page stays open.";
    } finally {
      client.networkBusy = false;
      render(client);
    }
  }

  async function claimContinuityOnThisDevice(client) {
    if (!navigator.onLine) {
      client.statusMessage = "Reconnect first, then claim this open-tab session.";
      render(client);
      return;
    }

    if (!client.continuityPayload || !client.continuityPayload.projection || !client.continuityPayload.projection.cursor) {
      client.statusMessage = "Claimed-device continuity is not ready yet. Refresh trust first.";
      render(client);
      return;
    }

    client.networkBusy = true;
    client.statusMessage = "Claiming this open-tab session.";
    render(client);

    try {
      var response = await fetch("/api/play/continuity/claim", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          cursor: client.continuityPayload.projection.cursor,
          role: client.roleName,
          deviceId: client.deviceId,
          observerId: client.observerId
        })
      });
      var claimPayload = parseJson(await response.text());
      if (!response.ok || !claimPayload) {
        throw new Error("claim_failed");
      }

      client.continuityPayload = {
        sessionId: client.sessionId,
        projection: claimPayload.projection || client.continuityPayload.projection,
        checkpoint: claimPayload.checkpoint || client.continuityPayload.checkpoint,
        continuity: claimPayload.continuity || client.continuityPayload.continuity,
        runtimeBundle: client.continuityPayload.runtimeBundle || null,
        continuityOwnerRoute: claimedTurnRoute(client.sessionId, client.roleName, client.deviceId)
      };
      var claimStatus = claimPayload.reason && claimPayload.reason !== "accepted"
        ? claimPayload.reason
        : "Claimed-device continuity updated.";
      var continuityToken = claimPayload.continuity && claimPayload.continuity.continuityToken
        ? claimPayload.continuity.continuityToken
        : "";
      client.statusMessage = (claimStatus + (continuityToken ? ". " + continuityToken : "")).trim();
    } catch (error) {
      void error;
      client.statusMessage = "Claim refresh failed. Keep this page open and use the current owner route until it reconnects cleanly.";
    } finally {
      client.networkBusy = false;
      render(client);
    }
  }

  function renderTrustLabels(labels) {
    var element = document.getElementById("turn-trust-labels");
    if (!element) {
      return;
    }

    element.innerHTML = labels.map(function (label) {
      return "<li>" + escapeHtml(label) + "</li>";
    }).join("");
  }

  function renderAnchorSelect(projection) {
    var select = document.getElementById("runsite-anchor");
    if (!select) {
      return;
    }

    select.innerHTML = (projection.runsite.anchors || []).map(function (anchor) {
      return "<option value=\"" + escapeAttribute(anchor.anchorId) + "\"" + (anchor.selected ? " selected" : "") + ">"
        + escapeHtml(anchor.label) + "</option>";
    }).join("");
    select.disabled = projection.canMutate !== true;
  }

  function renderMetricGrid(projection) {
    var element = document.getElementById("turn-metric-grid");
    if (!element) {
      return;
    }

    element.innerHTML = (projection.now.statCards || []).map(function (card) {
      return "<article class=\"metric-card metric-card--" + escapeAttribute(card.accent) + "\">"
        + "<div class=\"metric-meta\"><span class=\"metric-label\">" + escapeHtml(card.label) + "</span><span class=\"metric-value\">" + escapeHtml(String(card.value)) + "</span></div>"
        + "<p class=\"metric-detail\">" + escapeHtml(card.detail) + "</p>"
        + "<div class=\"stepper\">"
        + buildActionButton(
          "adjust-metric",
          "-",
          { "metric-id": card.metricId, delta: "-1" },
          projection.canMutate,
          "Decrease " + card.label + ", currently " + card.value
        )
        + buildActionButton(
          "adjust-metric",
          "+",
          { "metric-id": card.metricId, delta: "1" },
          projection.canMutate,
          "Increase " + card.label + ", currently " + card.value
        )
        + "</div></article>";
    }).join("");
  }

  function renderQuickGlance(client) {
    var projection = client.projection;
    setText("turn-glance-physical", String(statValue(projection, "physical")));
    setText("turn-glance-stun", String(statValue(projection, "stun")));
    setText("turn-glance-edge", String(statValue(projection, "edge")));
    setText("turn-glance-ammo", String(statValue(projection, "ammo")));
    setText("turn-glance-local-queue", String(client.localReplayQueue.length));
    setText("turn-glance-anchor", selectedAnchor(projection).label);
  }

  function renderInventoryGrid(projection) {
    var element = document.getElementById("turn-inventory-grid");
    if (!element) {
      return;
    }

    element.innerHTML = (projection.now.inventoryCards || []).map(function (card) {
      return "<article class=\"inventory-card\">"
        + "<div class=\"metric-meta\"><span class=\"metric-label\">" + escapeHtml(card.label) + "</span><span class=\"metric-value\">" + escapeHtml(String(card.quantity)) + "</span></div>"
        + "<p class=\"metric-detail\">" + escapeHtml(card.detail) + "</p>"
        + "<div class=\"stepper\">"
        + buildActionButton(
          "adjust-metric",
          "-",
          { "metric-id": "inventory:" + card.itemId, delta: "-1" },
          projection.canMutate,
          "Decrease " + card.label + ", currently " + card.quantity
        )
        + buildActionButton(
          "adjust-metric",
          "+",
          { "metric-id": "inventory:" + card.itemId, delta: "1" },
          projection.canMutate,
          "Increase " + card.label + ", currently " + card.quantity
        )
        + "</div></article>";
    }).join("");
  }

  function renderActionGrid(projection) {
    var element = document.getElementById("turn-action-grid");
    if (!element) {
      return;
    }

    element.innerHTML = (projection.act.actions || []).map(function (action) {
      var classes = action.selected ? "action-tile action-tile--selected" : "action-tile";
      return "<button type=\"button\" class=\"" + classes + "\" data-turn-kind=\"select-action\" data-action-id=\"" + escapeAttribute(action.actionId) + "\" aria-pressed=\"" + (action.selected ? "true" : "false") + "\""
        + (action.enabled ? "" : " disabled") + ">"
        + "<span class=\"action-label\">" + escapeHtml(action.label) + "</span>"
        + "<span class=\"action-summary\">" + escapeHtml(action.summary) + "</span>"
        + "<span class=\"action-pool\">" + escapeHtml(String(action.baseDicePool)) + " dice</span>"
        + "</button>";
    }).join("");
  }

  function renderModifierList(projection) {
    var element = document.getElementById("turn-modifier-list");
    if (!element) {
      return;
    }

    setText("turn-modifier-summary", projection.adjust.summary);
    element.innerHTML = (projection.adjust.options || []).map(function (modifier) {
      return "<label class=\"modifier-row\">"
        + "<input type=\"checkbox\" data-turn-kind=\"toggle-modifier\" data-modifier-id=\"" + escapeAttribute(modifier.modifierId) + "\""
        + (modifier.enabled ? " checked" : "") + (projection.canMutate ? "" : " disabled") + " />"
        + "<span class=\"modifier-copy\"><strong>" + escapeHtml(modifier.label) + "</strong><span>" + escapeHtml(modifier.source) + "</span></span>"
        + "<span class=\"modifier-delta\">" + escapeHtml(formatSigned(modifier.diceModifier)) + "</span>"
        + "</label>";
    }).join("");
  }

  function renderOddsGrid(odds) {
    var element = document.getElementById("turn-odds-grid");
    if (!element) {
      return;
    }

    element.innerHTML = odds.map(function (badge) {
      return "<div class=\"odds-badge\"><span>" + escapeHtml(badge.label) + "</span><strong>" + escapeHtml(badge.value) + "</strong></div>";
    }).join("");
  }

  function renderHistoryList(entries) {
    var element = document.getElementById("turn-history-list");
    if (!element) {
      return;
    }

    element.innerHTML = entries.map(function (item) {
      var flags = "";
      if (item.manual) {
        flags += "<span class=\"status-pill status-pill--manual\">Manual</span>";
      }

      if (item.queued) {
        flags += "<span class=\"status-pill status-pill--queued\">Queued</span>";
      }

      return "<li class=\"history-item\">"
        + "<div class=\"history-meta\"><strong>" + escapeHtml(item.title) + "</strong><span>" + escapeHtml(item.when) + "</span></div>"
        + "<p>" + escapeHtml(item.detail) + "</p>"
        + "<div class=\"history-flags\">" + flags + "</div>"
        + "</li>";
    }).join("");
  }

  function setBanner(client) {
    var banner = document.getElementById("mobile-client-banner");
    var title = document.getElementById("mobile-client-banner-title");
    var copy = document.getElementById("mobile-client-banner-copy");
    if (!banner || !title || !copy) {
      return;
    }

    if (!navigator.onLine) {
      banner.setAttribute("data-tone", "offline");
      title.textContent = "Offline: open-tab tracker is active.";
      copy.textContent = "This page holds " + client.localReplayQueue.length + " local receipt(s) in memory only while this tab stays open. Reconnect and replay them before closing or reloading.";
      return;
    }

    if (client.localReplayQueue.length > 0) {
      banner.setAttribute("data-tone", "restored");
      title.textContent = "Local replay queue is waiting.";
      copy.textContent = client.localReplayQueue.length + " receipt(s) remain in this open tab only. Reconnect and replay them before closing or reloading this page.";
      return;
    }

    banner.setAttribute("data-tone", "ready");
    title.textContent = "Grounded open-tab tracker is ready.";
    copy.textContent = "Private table state stays in memory for this open page only. Installation caches public shell assets, never table state.";
  }

  function applyRequestedRouteFallback(projection, sessionId, roleName) {
    projection.sessionId = sessionId;
    projection.role = roleName;
    projection.pendingQueueCount = 0;
    projection.shellSummary = "Open-tab fallback · " + sessionId;
    projection.localBoundarySummary = "This open page has no route-specific server projection yet. Reconnect to load " + sessionId + ", or continue with a temporary tracker that is discarded when this page closes or reloads.";
    projection.currentSceneSummary = "Route-specific scene lineage is unavailable until this page reconnects.";
    projection.trust = {
      statusLabel: "Reconnect before trust",
      summary: "This open-tab fallback has no route-specific server projection, so trust and scene lineage must be refreshed once the device reconnects.",
      checkpointLabel: "Checkpoint pending",
      runtimeLabel: "Runtime bundle proof pending",
      queueLabel: "No queued replay-safe mutations are pinned for this route yet.",
      labels: [
        "Requested route: " + sessionId + ".",
        "Reconnect to load a grounded runtime bundle and continuity checkpoint.",
        "Until then, this page stays a bounded temporary tracker only."
      ]
    };
    projection.sync.pendingSummary = "Server replay queue is not confirmed for this route until reconnect.";
    projection.sync.reconnectSummary = "Reconnect once before you replay or acknowledge any queue state for the requested route.";
    projection.sync.claimedDeviceSummary = "Open-tab fallback is active for " + sessionId + ". Route-specific sync posture will appear after reconnect.";
    projection.history.entries = [
      {
        title: "Requested route not loaded yet",
        detail: "Reconnect on this page to load the server projection for " + sessionId + ".",
        when: formatUtcTime(new Date()),
        queued: false,
        manual: false
      }
    ];
  }

  function mergeProjectionState(serverProjection, restoredProjection) {
    if (!restoredProjection) {
      return serverProjection;
    }

    var merged = deepClone(restoredProjection);
    merged.sessionId = serverProjection.sessionId;
    merged.role = serverProjection.role;
    merged.canMutate = serverProjection.canMutate;
    merged.trust = serverProjection.trust;
    merged.sync = serverProjection.sync;
    merged.currentSceneSummary = serverProjection.currentSceneSummary;
    merged.pendingQueueCount = serverProjection.pendingQueueCount;
    merged.localBoundarySummary = restoredProjection.localBoundarySummary || serverProjection.localBoundarySummary;
    merged.shellSummary = restoredProjection.shellSummary || serverProjection.shellSummary;
    return merged;
  }

  function normalizeProjection(projection) {
    projection.now = projection.now || {};
    projection.now.statCards = projection.now.statCards || [];
    projection.now.inventoryCards = projection.now.inventoryCards || [];
    projection.act = projection.act || { actions: [] };
    projection.act.actions = projection.act.actions || [];
    projection.act.existingQuickActions = projection.act.existingQuickActions || [];
    projection.adjust = projection.adjust || { options: [] };
    projection.adjust.options = projection.adjust.options || [];
    projection.resolve = projection.resolve || { odds: [] };
    projection.resolve.odds = projection.resolve.odds || [];
    projection.history = projection.history || { entries: [] };
    projection.history.entries = projection.history.entries || [];
    projection.runsite = projection.runsite || { anchors: [] };
    projection.runsite.anchors = projection.runsite.anchors || [];
    projection.sync = projection.sync || { quickActions: [] };
    projection.sync.quickActions = projection.sync.quickActions || [];
    projection.sync.canReplayLocalQueue = projection.sync.canReplayLocalQueue !== false;
    projection.sync.canAcknowledgeServerQueue = projection.sync.canAcknowledgeServerQueue === true;
    projection.pendingQueueCount = typeof projection.pendingQueueCount === "number" ? projection.pendingQueueCount : 0;
    projection.localRevision = typeof projection.localRevision === "number" ? projection.localRevision : 1;
  }

  function queueLocalEvent(client, replayEvent) {
    if (!replayEvent) {
      return;
    }

    client.localReplayQueue.push(replayEvent);
    var entries = client.projection.history.entries || [];
    if (entries.length > 0) {
      entries[0].queued = true;
    }
  }

  function replayRoute(sessionId, roleName, deviceId) {
    return "/api/play/turn-companion/" + encodeURIComponent(sessionId)
      + "/replay?role=" + encodeURIComponent(roleName)
      + "&deviceId=" + encodeURIComponent(deviceId);
  }

  function queueStatusRoute(sessionId, roleName, deviceId) {
    return "/api/play/turn-companion/" + encodeURIComponent(sessionId)
      + "/queue-status?role=" + encodeURIComponent(roleName)
      + "&deviceId=" + encodeURIComponent(deviceId);
  }

  function acknowledgeRoute(sessionId, roleName, deviceId) {
    return "/api/play/turn-companion/" + encodeURIComponent(sessionId)
      + "/acknowledge?role=" + encodeURIComponent(roleName)
      + "&deviceId=" + encodeURIComponent(deviceId);
  }

  function mobileHref() {
    return "/mobile/live";
  }

  function mobileRoleHref(roleName) {
    return "/mobile/" + mobileModeSegment(roleName);
  }

  function mobileModeSegment(roleName) {
    if (isGameMasterRole(roleName)) {
      return "gm";
    }

    var lowered = String(roleName || "").toLowerCase();
    if (lowered.indexOf("observer") >= 0) {
      return "observer";
    }

    return "player";
  }

  function roleToMobileMode(roleText) {
    var lowered = String(roleText || "").toLowerCase();
    if (lowered.indexOf("observer") >= 0) {
      return "observer";
    }

    if (isGameMasterRole(roleText) || lowered === "gamer" || lowered.indexOf("game") >= 0) {
      return "gm";
    }

    return "player";
  }

  function normalizePlayRouteForMobileShell(href, roleFallback, sessionIdFallback, deviceFallback) {
    void roleFallback;
    void sessionIdFallback;
    void deviceFallback;
    if (!href || typeof href !== "string") {
      return "";
    }

    var trimmedHref = href.trim();
    if (!trimmedHref) {
      return "";
    }

    try {
      var routeUrl = new URL(trimmedHref, window.location.origin);
      return routeUrl.pathname === "/mobile/live"
        || routeUrl.pathname === "/play"
        || routeUrl.pathname.indexOf("/play/") === 0
        ? "/mobile/live"
        : trimmedHref;
    } catch {
      return trimmedHref;
    }
  }

  function claimedTurnRoute(sessionId, roleName, deviceId) {
    void sessionId;
    void roleName;
    void deviceId;
    return "/mobile/live";
  }

  function observeRoute(sessionId) {
    return "/api/play/observe/" + encodeURIComponent(sessionId);
  }

  function selectedAction(projection) {
    var actions = projection.act.actions || [];
    for (var index = 0; index < actions.length; index += 1) {
      if (actions[index].selected) {
        return actions[index];
      }
    }

    if (actions.length === 0) {
      return { actionId: "review", label: "Review", summary: "", baseDicePool: 0, ammoCost: 0, selected: true, enabled: false };
    }

    actions[0].selected = true;
    return actions[0];
  }

  function selectedAnchor(projection) {
    var anchors = projection.runsite.anchors || [];
    for (var index = 0; index < anchors.length; index += 1) {
      if (anchors[index].selected) {
        return anchors[index];
      }
    }

    if (anchors.length === 0) {
      return { anchorId: "", label: "No anchor selected" };
    }

    anchors[0].selected = true;
    return anchors[0];
  }

  function findStatCard(projection, metricId) {
    var cards = projection.now.statCards || [];
    for (var index = 0; index < cards.length; index += 1) {
      if (cards[index].metricId === metricId) {
        return cards[index];
      }
    }

    return null;
  }

  function findInventoryCard(projection, itemId) {
    var cards = projection.now.inventoryCards || [];
    for (var index = 0; index < cards.length; index += 1) {
      if (cards[index].itemId === itemId) {
        return cards[index];
      }
    }

    return null;
  }

  function statValue(projection, metricId) {
    var card = findStatCard(projection, metricId);
    return card ? card.value : 0;
  }

  function setStatValue(projection, metricId, value) {
    var card = findStatCard(projection, metricId);
    if (card) {
      card.value = value;
    }
  }

  function snapshotCounters(projection) {
    return {
      ammoInMagazine: statValue(projection, "ammo"),
      ammoReserve: statValue(projection, "reserve"),
      charges: statValue(projection, "charges")
    };
  }

  function buildModifierSummary(modifierTotal, modifiers) {
    var activeCount = 0;
    for (var index = 0; index < modifiers.length; index += 1) {
      if (modifiers[index].enabled) {
        activeCount += 1;
      }
    }

    return activeCount === 0
      ? "No modifier is active; the resolver stays on the clean base pool."
      : activeCount + " modifier(s) active for a total of " + formatSigned(modifierTotal) + " dice.";
  }

  function buildOddsSummary(dicePool) {
    if (dicePool <= 0) {
      return "Dice pool is zero; adjust modifiers or choose a different action before resolving.";
    }

    return "At " + dicePool + " dice: "
      + toPercent(probabilityAtLeastHits(dicePool, 1)) + "% for 1+ hit, "
      + toPercent(probabilityAtLeastHits(dicePool, 2)) + "% for 2+, "
      + toPercent(probabilityAtLeastHits(dicePool, 3)) + "% for 3+, and "
      + toPercent(probabilityOfGlitch(dicePool)) + "% glitch risk.";
  }

  function buildOddsBadges(dicePool) {
    if (dicePool <= 0) {
      return [
        { label: "1+ hits", value: "0%" },
        { label: "2+ hits", value: "0%" },
        { label: "3+ hits", value: "0%" },
        { label: "Glitch", value: "0%" }
      ];
    }

    return [
      { label: "1+ hits", value: toPercent(probabilityAtLeastHits(dicePool, 1)) + "%" },
      { label: "2+ hits", value: toPercent(probabilityAtLeastHits(dicePool, 2)) + "%" },
      { label: "3+ hits", value: toPercent(probabilityAtLeastHits(dicePool, 3)) + "%" },
      { label: "Glitch", value: toPercent(probabilityOfGlitch(dicePool)) + "%" }
    ];
  }

  function buildHistorySummary(history, pendingQueueCount) {
    if (!history.length) {
      return pendingQueueCount === 0
        ? "No local history is recorded yet."
        : pendingQueueCount + " queued replay-safe mutation(s) are pending without a local turn receipt yet.";
    }

    return pendingQueueCount === 0
      ? "Latest local receipt: " + history[0].title + "."
      : "Latest local receipt: " + history[0].title + ". " + pendingQueueCount + " queued replay-safe mutation(s) still need sync.";
  }

  function humanizeMetric(metricId) {
    switch (metricId) {
      case "physical":
        return "Physical";
      case "stun":
        return "Stun";
      case "edge":
        return "Edge";
      case "ammo":
        return "Magazine";
      case "reserve":
        return "Reserve";
      case "charges":
        return "Charges";
      default:
        return metricId;
    }
  }

  function trustToneClass(statusLabel) {
    var lowered = String(statusLabel || "").toLowerCase();
    if (lowered.indexOf("reconnect") >= 0) {
      return "status-pill status-pill--danger";
    }

    if (lowered.indexOf("pressure") >= 0 || lowered.indexOf("queued") >= 0) {
      return "status-pill status-pill--warning";
    }

    if (lowered.indexOf("observer") >= 0) {
      return "status-pill status-pill--cool";
    }

    return "status-pill status-pill--ok";
  }

  function rollDicePool(dicePool) {
    var hits = 0;
    var ones = 0;
    for (var index = 0; index < dicePool; index += 1) {
      var die = Math.floor(Math.random() * 6) + 1;
      if (die >= 5) {
        hits += 1;
      } else if (die === 1) {
        ones += 1;
      }
    }

    return {
      hits: hits,
      glitch: ones >= Math.ceil(dicePool / 2),
      mode: "digital"
    };
  }

  function probabilityAtLeastHits(dicePool, minimumHits) {
    var total = 0;
    for (var hits = minimumHits; hits <= dicePool; hits += 1) {
      total += combination(dicePool, hits) * Math.pow(1 / 3, hits) * Math.pow(2 / 3, dicePool - hits);
    }

    return total;
  }

  function probabilityOfGlitch(dicePool) {
    var total = 0;
    var onesThreshold = Math.ceil(dicePool / 2);
    for (var hits = 0; hits <= dicePool; hits += 1) {
      for (var ones = onesThreshold; ones <= dicePool - hits; ones += 1) {
        var neutral = dicePool - hits - ones;
        total += multinomialProbability(hits, ones, neutral, 1 / 3, 1 / 6, 1 / 2);
      }
    }

    return total;
  }

  function multinomialProbability(hits, ones, neutral, hitProbability, oneProbability, neutralProbability) {
    var total = hits + ones + neutral;
    var coefficient = factorial(total) / (factorial(hits) * factorial(ones) * factorial(neutral));
    return coefficient
      * Math.pow(hitProbability, hits)
      * Math.pow(oneProbability, ones)
      * Math.pow(neutralProbability, neutral);
  }

  function combination(count, picks) {
    return factorial(count) / (factorial(picks) * factorial(count - picks));
  }

  function factorial(value) {
    var result = 1;
    for (var current = 2; current <= value; current += 1) {
      result *= current;
    }

    return result;
  }

  function toPercent(value) {
    return Math.round(value * 100);
  }

  function saveSnapshot(client) {
    void client;
  }

  function loadSnapshot(sessionId, roleName, deviceId) {
    void sessionId;
    void roleName;
    void deviceId;
    return null;
  }

  function setText(id, value) {
    var element = document.getElementById(id);
    if (element) {
      element.textContent = value;
    }
  }

  function setHidden(id, hidden) {
    var element = document.getElementById(id);
    if (!element) {
      return;
    }

    if (hidden) {
      element.setAttribute("hidden", "hidden");
    } else {
      element.removeAttribute("hidden");
    }
  }

  function setClassName(id, className) {
    var element = document.getElementById(id);
    if (element) {
      element.className = className;
    }
  }

  function setButtonDisabled(id, disabled) {
    var element = document.getElementById(id);
    if (element) {
      element.disabled = disabled === true;
    }
  }

  function setButtonText(id, text) {
    var element = document.getElementById(id);
    if (element) {
      element.textContent = text;
    }
  }

  function setLink(id, href, text) {
    var element = document.getElementById(id);
    if (element) {
      element.setAttribute("href", href || "#");
      element.textContent = text || "Open route";
    }
  }

  function buildActionButton(kind, label, data, enabled, accessibleLabel) {
    var attributes = " type=\"button\" data-turn-kind=\"" + kind + "\"";
    Object.keys(data).forEach(function (key) {
      attributes += " data-" + key + "=\"" + escapeAttribute(String(data[key])) + "\"";
    });

    if (!enabled) {
      attributes += " disabled";
    }

    if (accessibleLabel) {
      attributes += " aria-label=\"" + escapeAttribute(accessibleLabel) + "\"";
    }

    return "<button" + attributes + ">" + escapeHtml(label) + "</button>";
  }

  function parseJson(text) {
    try {
      return JSON.parse(text || "{}");
    } catch (error) {
      void error;
      return null;
    }
  }

  function deepClone(value) {
    return parseJson(JSON.stringify(value));
  }

  function formatUtcTime(date) {
    var hours = String(date.getUTCHours()).padStart(2, "0");
    var minutes = String(date.getUTCMinutes()).padStart(2, "0");
    return hours + ":" + minutes + " UTC";
  }

  function formatSigned(value) {
    if (value > 0) {
      return "+" + value;
    }

    if (value < 0) {
      return String(value);
    }

    return "0";
  }

  function signedToken(value) {
    return value >= 0 ? "+" + value : String(value);
  }

  function stopEvent(event) {
    event.preventDefault();
    event.stopPropagation();
    if (typeof event.stopImmediatePropagation === "function") {
      event.stopImmediatePropagation();
    }
  }

  function escapeHtml(value) {
    return String(value)
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/\"/g, "&quot;")
      .replace(/'/g, "&#39;");
  }

  function escapeAttribute(value) {
    return escapeHtml(value);
  }
}());
