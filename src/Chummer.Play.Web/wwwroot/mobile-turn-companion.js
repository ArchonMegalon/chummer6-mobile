(function () {
  "use strict";

  var storagePrefix = "chummer-play-turn-companion:";
  var lastRouteKey = storagePrefix + "last-route";
  var magazineCapacity = 12;
  var historyLimit = 8;

  document.addEventListener("DOMContentLoaded", function () {
    var root = document.querySelector("[data-turn-root]");
    var bootstrapNode = document.getElementById("turn-companion-bootstrap");
    if (!root || !bootstrapNode) {
      return;
    }

    var bootstrap = parseJson(bootstrapNode.textContent);
    if (!bootstrap || !bootstrap.projection) {
      return;
    }

    var params = new URLSearchParams(window.location.search || "");
    var requestedRoleName = params.get("role") || "";
    var resumeRoute = resolveResumeRoute(params);
    var sessionId = params.get("sessionId") || (resumeRoute && resumeRoute.sessionId) || bootstrap.sessionId || root.getAttribute("data-session-id") || "session-main";
    var roleName = requestedRoleName || (resumeRoute && resumeRoute.roleName) || bootstrap.roleName || root.getAttribute("data-role") || "Player";
    var explicitDeviceId = params.get("deviceId") || (resumeRoute && resumeRoute.deviceId) || root.getAttribute("data-device-id") || "";
    var deviceId = resolveStableDeviceId(roleName, explicitDeviceId);
    var observerId = resolveObserverId();
    if (!params.get("sessionId")) {
      params.set("sessionId", sessionId);
    }
    if (!params.get("role")) {
      params.set("role", roleName);
    }
    if (!params.get("deviceId")) {
      params.set("deviceId", deviceId);
    }
    window.history.replaceState({}, "", window.location.pathname + "?" + params.toString());

    var restored = loadSnapshot(sessionId, roleName, deviceId);
    var projection = mergeProjectionState(bootstrap.projection, restored && restored.projection ? restored.projection : null);
    var bootstrapMismatch = bootstrap.sessionId && bootstrap.sessionId !== sessionId;
    if (bootstrapMismatch && !restored) {
      applyRequestedRouteFallback(projection, sessionId, roleName);
    }
    var client = {
      sessionId: sessionId,
      roleName: roleName,
      deviceId: deviceId,
      observerId: observerId,
      projection: projection,
      statusMessage: buildInitialStatusMessage(bootstrapMismatch, restored, bootstrap, resumeRoute),
      manualHits: restored && typeof restored.manualHits === "number" ? restored.manualHits : 0,
      manualGlitch: restored ? restored.manualGlitch === true : false,
      restoredFromStorage: !!restored,
      localReplayQueue: restored && Array.isArray(restored.localReplayQueue) ? restored.localReplayQueue : [],
      continuityPayload: restored && restored.continuityPayload ? restored.continuityPayload : null,
      serviceWorkerStatus: restored && restored.serviceWorkerStatus
        ? restored.serviceWorkerStatus
        : "Checking service worker and install cache posture for this shell.",
      installPromptEvent: null,
      installBusy: false,
      networkBusy: false
    };

    normalizeProjection(client.projection);
    render(client);
    attachHandlers(root, client);
    void bootInstallBoundary(client);
    document.addEventListener("visibilitychange", function () {
      if (shouldPersistGlobalLastRoute()) {
        saveLastRoute(client);
      }
    });
    window.addEventListener("online", function () {
      void refreshNetworkSurfaces(client, "Reconnect detected. Refreshing trust, queue, and claimed-device posture for this shell.");
    });
    window.addEventListener("offline", function () {
      client.statusMessage = "Offline mode: device-local play tracking remains available from this install cache.";
      render(client);
    });
    window.addEventListener("beforeinstallprompt", function (event) {
      event.preventDefault();
      client.installPromptEvent = event;
      client.serviceWorkerStatus = "This shell is installable from the browser prompt on this device.";
      render(client);
    });
    window.addEventListener("appinstalled", function () {
      client.installPromptEvent = null;
      client.serviceWorkerStatus = "This shell is installed and can reopen from the local app icon.";
      render(client);
    });
    if (navigator.onLine) {
      void refreshNetworkSurfaces(
        client,
        bootstrapMismatch
          ? "Refreshing the requested route after loading the cached mobile fallback."
          : "Refreshing trust, queue, and claimed-device posture for this shell."
      );
    }
  });

  function makeStableId(prefix) {
    return prefix + "-" + Math.random().toString(36).slice(2, 10);
  }

  function readStoredValue(key) {
    try {
      return window.localStorage.getItem(key) || "";
    } catch (error) {
      void error;
      return "";
    }
  }

  function writeStoredValue(key, value) {
    try {
      window.localStorage.setItem(key, value);
    } catch (error) {
      void error;
    }
  }

  function devicePrefixForRole(roleName) {
    var lowered = String(roleName || "").toLowerCase();
    if (lowered.indexOf("gm") >= 0) {
      return "gm-shell";
    }

    if (lowered.indexOf("observer") >= 0) {
      return "observer-shell";
    }

    return "player-shell";
  }

  function resolveStableDeviceId(roleName, explicitDeviceId) {
    if (explicitDeviceId) {
      writeStoredValue("chummer-play-mobile-device-id", explicitDeviceId);
      return explicitDeviceId;
    }

    var stored = readStoredValue("chummer-play-mobile-device-id");
    if (stored) {
      return stored;
    }

    var generated = makeStableId(devicePrefixForRole(roleName));
    writeStoredValue("chummer-play-mobile-device-id", generated);
    return generated;
  }

  function resolveResumeRoute(params) {
    if (params.get("sessionId") || params.get("deviceId")) {
      return null;
    }

    var requestedRoleName = params.get("role") || "";
    if (!requestedRoleName) {
      var genericRoute = loadLastRoute();
      if (genericRoute) {
        genericRoute.resumeSource = "generic";
      }
      return genericRoute;
    }

    var roleRoute = loadLastRoute(requestedRoleName);
    if (roleRoute) {
      roleRoute.resumeSource = "role";
      return roleRoute;
    }

    var globalRoute = loadLastRoute();
    if (!globalRoute || !globalRoute.sessionId) {
      return null;
    }

    return {
      sessionId: globalRoute.sessionId,
      roleName: requestedRoleName,
      deviceId: "",
      resumeSource: "session-only"
    };
  }

  function lastRouteKeyForRole(roleName) {
    return lastRouteKey + ":" + String(roleName || "").trim().toLowerCase();
  }

  function loadLastRoute(roleName) {
    var scopedRoleName = String(roleName || "").trim();
    var stored = readStoredValue(scopedRoleName ? lastRouteKeyForRole(scopedRoleName) : lastRouteKey);
    if (!stored) {
      return null;
    }

    var parsed = parseJson(stored);
    if (!parsed || !parsed.sessionId || !parsed.roleName || !parsed.deviceId) {
      return null;
    }

    if (scopedRoleName && String(parsed.roleName).toLowerCase() !== scopedRoleName.toLowerCase()) {
      return null;
    }

    return parsed;
  }

  function saveLastRoute(client) {
    if (!client || !client.sessionId || !client.roleName || !client.deviceId) {
      return;
    }

    var payload = JSON.stringify({
      sessionId: client.sessionId,
      roleName: client.roleName,
      deviceId: client.deviceId,
      savedAtUtc: new Date().toISOString()
    });
    writeStoredValue(lastRouteKeyForRole(client.roleName), payload);
    if (!shouldPersistGlobalLastRoute()) {
      return;
    }

    writeStoredValue(lastRouteKey, payload);
  }

  function shouldPersistGlobalLastRoute() {
    if (typeof document === "undefined" || typeof document.visibilityState !== "string") {
      return true;
    }

    return document.visibilityState === "visible" || document.hasFocus();
  }

  function buildInitialStatusMessage(bootstrapMismatch, restored, bootstrap, resumeRoute) {
    if (bootstrapMismatch && !restored) {
      if (resumeRoute && resumeRoute.resumeSource === "session-only") {
        return "Resuming " + resumeRoute.sessionId + " in the " + resumeRoute.roleName + " lane on this install. Reconnect once to seed a local snapshot.";
      }

      if (resumeRoute) {
        return "Resuming " + resumeRoute.sessionId + " on this install, but no local snapshot is cached for the requested route yet. Reconnect once to seed it.";
      }

      return "This cached shell has no session-local snapshot for the requested route yet. Reconnect once to seed it.";
    }

    if (resumeRoute && restored) {
      return resumeRoute.resumeSource === "role"
        ? "Resumed the last " + resumeRoute.roleName + " claimed-device route for this install."
        : "Resumed the last claimed-device route for this install.";
    }

    return restored && restored.statusMessage ? restored.statusMessage : (bootstrap.statusMessage || "Turn companion loaded.");
  }

  function resolveObserverId() {
    var stored = readStoredValue("chummer-play-mobile-observer-id");
    if (stored) {
      return stored;
    }

    var generated = makeStableId("observer");
    writeStoredValue("chummer-play-mobile-observer-id", generated);
    return generated;
  }

  function attachHandlers(root, client) {
    root.addEventListener("click", function (event) {
      var control = event.target.closest("[data-turn-kind]");
      if (!control || control.disabled) {
        return;
      }

      stopEvent(event);
      switch (control.getAttribute("data-turn-kind")) {
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
      }
    }, true);

    root.addEventListener("change", function (event) {
      var control = event.target.closest("[data-turn-kind]");
      if (!control || control.disabled) {
        return;
      }

      stopEvent(event);
      switch (control.getAttribute("data-turn-kind")) {
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
        saveSnapshot(client);
      });
    }

    if (manualGlitch) {
      manualGlitch.addEventListener("change", function () {
        client.manualGlitch = manualGlitch.checked === true;
        saveSnapshot(client);
      });
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
      links[index].setAttribute("href", mobileHref(client.sessionId, roleName, client.deviceId));
    }
  }

  function renderClaimedDeviceSurface(client) {
    var continuityPayload = client.continuityPayload;
    var continuity = continuityPayload && continuityPayload.continuity ? continuityPayload.continuity : null;
    var ownerRoute = continuityPayload && continuityPayload.continuityOwnerRoute
      ? continuityPayload.continuityOwnerRoute
      : claimedTurnRoute(client.sessionId, client.roleName, client.deviceId);
    var claimMatchesThisDevice = continuity && continuity.deviceId === client.deviceId;
    var hasContinuityCursor = continuityPayload
      && continuityPayload.projection
      && continuityPayload.projection.cursor;

    setText("turn-continuity-device", client.deviceId);
    setText("turn-owner-route-copy", "Owner route: " + ownerRoute + ". Keep the claimed-device handoff visible on the same install-local shell.");
    setLink("turn-owner-route-link", ownerRoute, "Open owner route");

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
        "Claim this device before the next handoff so the owner route and replay-safe sequence stay attached to one install-local shell."
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
      actionId + " is staged on this device and can be replayed when the owner route reconnects.",
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
      client.statusMessage = "No local replay receipts are queued on this device.";
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
      client.statusMessage = "Queue status refresh failed. Local tracking stays available on this shell.";
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
      client.serviceWorkerStatus = "This browser cannot register the install-local service worker boundary for the mobile shell.";
      render(client);
      return;
    }

    try {
      await navigator.serviceWorker.register("/service-worker.js", { scope: "/" });
      client.serviceWorkerStatus = navigator.serviceWorker.controller
        ? "Service worker is active on this device; install-local shell caching is ready."
        : "Service worker registered now; the install-local cache boundary becomes active after the next shell load.";
    } catch (error) {
      console.error("service worker registration failed", error);
      client.serviceWorkerStatus = "Service worker registration failed, so install-local cache trust is reduced until this shell reloads cleanly.";
    }

    render(client);
  }

  function renderInstallSurface(client) {
    var installed = isInstalledShell();
    var buttonLabel = "Install app";
    var detail = "Install this shell so the bounded turn companion can reopen from the device during play.";
    var disabled = false;

    if (installed) {
      buttonLabel = "Installed";
      detail = "This device already has the bounded turn companion installed. Reopen it from the app icon during play.";
      disabled = true;
    } else if (client.installBusy) {
      buttonLabel = "Opening install prompt";
      detail = "Confirm the browser install flow to pin this shell on the device.";
      disabled = true;
    } else if (client.installPromptEvent && typeof client.installPromptEvent.prompt === "function") {
      buttonLabel = "Install app";
      detail = "Use the browser install prompt to pin this bounded shell before the next play session.";
    } else if (isAppleMobileBrowser()) {
      buttonLabel = "Add to Home Screen";
      detail = "Use Safari Share and choose Add to Home Screen because this browser does not expose the inline install prompt.";
    } else {
      buttonLabel = "Install from browser menu";
      detail = "The inline install prompt is not available yet. Use the browser install menu after the service worker finishes registering.";
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
      render(client);

      try {
        var promptEvent = client.installPromptEvent;
        await promptEvent.prompt();
        var choice = promptEvent.userChoice ? await promptEvent.userChoice : null;
        if (choice && choice.outcome === "accepted") {
          client.serviceWorkerStatus = "Install accepted. Finish the browser flow to pin this shell on the device.";
        } else {
          client.serviceWorkerStatus = "Install dismissed. You can reopen the prompt from this shell when the browser offers it again.";
        }
        client.installPromptEvent = null;
      } catch (error) {
        console.error("install prompt failed", error);
        client.serviceWorkerStatus = "Install prompt failed. Use the browser install menu until this shell can prompt again.";
      } finally {
        client.installBusy = false;
        render(client);
      }
      return;
    }

    client.serviceWorkerStatus = isAppleMobileBrowser()
      ? "Open Safari Share and choose Add to Home Screen to install this shell on this device."
      : "Use the browser install menu to add this shell to the device because the inline prompt is not available yet.";
    render(client);
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
      client.statusMessage = "Network refresh failed. Local tracking stays available on this shell.";
    } finally {
      client.networkBusy = false;
      render(client);
    }
  }

  async function claimContinuityOnThisDevice(client) {
    if (!navigator.onLine) {
      client.statusMessage = "Reconnect first, then claim this install-local device.";
      render(client);
      return;
    }

    if (!client.continuityPayload || !client.continuityPayload.projection || !client.continuityPayload.projection.cursor) {
      client.statusMessage = "Claimed-device continuity is not ready yet. Refresh trust first.";
      render(client);
      return;
    }

    client.networkBusy = true;
    client.statusMessage = "Claiming this install-local device.";
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
      client.statusMessage = "Claim refresh failed. Keep using the current owner route until this shell reconnects cleanly.";
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
        + buildActionButton("adjust-metric", "-", { "metric-id": card.metricId, delta: "-1" }, projection.canMutate)
        + buildActionButton("adjust-metric", "+", { "metric-id": card.metricId, delta: "1" }, projection.canMutate)
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
        + buildActionButton("adjust-metric", "-", { "metric-id": "inventory:" + card.itemId, delta: "-1" }, projection.canMutate)
        + buildActionButton("adjust-metric", "+", { "metric-id": "inventory:" + card.itemId, delta: "1" }, projection.canMutate)
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
      return "<button type=\"button\" class=\"" + classes + "\" data-turn-kind=\"select-action\" data-action-id=\"" + escapeAttribute(action.actionId) + "\""
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
      title.textContent = "Offline: device-local tracker is active.";
      copy.textContent = "This cached shell is running from local storage. " + client.localReplayQueue.length + " local receipt(s) stay on this device until you reconnect.";
      return;
    }

    if (client.localReplayQueue.length > 0) {
      banner.setAttribute("data-tone", "restored");
      title.textContent = "Local replay queue is waiting.";
      copy.textContent = client.localReplayQueue.length + " device-local receipt(s) are staged for replay when the owner route reconnects.";
      return;
    }

    if (client.restoredFromStorage) {
      banner.setAttribute("data-tone", "restored");
      title.textContent = "Device-local snapshot restored.";
      copy.textContent = "Stored revision " + client.projection.localRevision + " is live on this shell and will survive reloads, install reopen, and temporary reconnect churn.";
      return;
    }

    banner.setAttribute("data-tone", "ready");
    title.textContent = "Grounded local tracker is ready.";
      copy.textContent = "This shell persists the bounded session tracker locally so the mobile lane can reopen quickly from the install cache.";
  }

  function applyRequestedRouteFallback(projection, sessionId, roleName) {
    projection.sessionId = sessionId;
    projection.role = roleName;
    projection.pendingQueueCount = 0;
    projection.shellSummary = "Claimed-device fallback · " + sessionId;
    projection.localBoundarySummary = "This cached fallback has no route-specific snapshot yet. Reconnect once to seed " + sessionId + " on this device, or continue with a fresh local tracker.";
    projection.currentSceneSummary = "Route-specific scene lineage is not cached for this requested shell yet.";
    projection.trust = {
      statusLabel: "Reconnect before trust",
      summary: "This offline fallback opened without a route-specific snapshot, so trust and scene lineage must be refreshed once the device reconnects.",
      checkpointLabel: "Checkpoint pending",
      runtimeLabel: "Runtime bundle proof pending",
      queueLabel: "No queued replay-safe mutations are pinned for this route yet.",
      labels: [
        "Requested route: " + sessionId + ".",
        "Reconnect once to seed a grounded runtime bundle and continuity checkpoint.",
        "Until then, this shell stays a bounded local tracker only."
      ]
    };
    projection.sync.pendingSummary = "Server replay queue is not confirmed for this route until reconnect.";
    projection.sync.reconnectSummary = "Reconnect once before you replay or acknowledge any queue state for the requested route.";
    projection.sync.claimedDeviceSummary = "Claimed-device fallback is active for " + sessionId + ". Route-specific sync posture will appear after reconnect.";
    projection.history.entries = [
      {
        title: "Requested route not cached yet",
        detail: "Reconnect once on this device to seed a route-local snapshot for " + sessionId + ".",
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

  function mobileHref(sessionId, roleName, deviceId) {
    return "/mobile?sessionId=" + encodeURIComponent(sessionId)
      + "&role=" + encodeURIComponent(roleName)
      + "&deviceId=" + encodeURIComponent(deviceId);
  }

  function claimedTurnRoute(sessionId, roleName, deviceId) {
    return mobileHref(sessionId, roleName, deviceId);
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
    try {
      window.localStorage.setItem(storageKey(client.sessionId, client.roleName, client.deviceId), JSON.stringify({
        projection: client.projection,
        statusMessage: client.statusMessage,
        manualHits: client.manualHits,
        manualGlitch: client.manualGlitch,
        localReplayQueue: client.localReplayQueue,
        continuityPayload: client.continuityPayload,
        serviceWorkerStatus: client.serviceWorkerStatus,
        savedAtUtc: new Date().toISOString()
      }));
    } catch (error) {
      void error;
    }
  }

  function loadSnapshot(sessionId, roleName, deviceId) {
    try {
      var payload = window.localStorage.getItem(storageKey(sessionId, roleName, deviceId));
      if (!payload) {
        payload = window.localStorage.getItem(legacyStorageKey(sessionId, roleName));
      }
      if (!payload) {
        return null;
      }

      var parsed = parseJson(payload);
      if (!parsed || !parsed.projection || !parsed.projection.now || !parsed.projection.now.statCards) {
        return null;
      }

      return parsed;
    } catch (error) {
      void error;
      return null;
    }
  }

  function storageKey(sessionId, roleName, deviceId) {
    return storagePrefix + sessionId + ":" + roleName + ":" + deviceId;
  }

  function legacyStorageKey(sessionId, roleName) {
    return storagePrefix + sessionId + ":" + roleName;
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

  function buildActionButton(kind, label, data, enabled) {
    var attributes = " type=\"button\" data-turn-kind=\"" + kind + "\"";
    Object.keys(data).forEach(function (key) {
      attributes += " data-" + key + "=\"" + escapeAttribute(String(data[key])) + "\"";
    });

    if (!enabled) {
      attributes += " disabled";
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
