(function () {
  "use strict";

  var root = document.querySelector("[data-campaign-root]");
  if (!root) {
    return;
  }

  var apiRoot = "/api/v1/campaigns";
  var antiforgeryRoute = "/api/v1/antiforgery";
  var maxRequestBytes = 64 * 1024;
  var requestTimeoutMs = 15 * 1000;
  var state = {
    campaigns: [],
    eligibleCharacters: [],
    campaign: null,
    roster: [],
    sheet: null,
    runsiteDraft: null,
    runsitePlayer: null,
    antiforgery: null,
    inviteId: boundedIdentifier(root.getAttribute("data-invite-id")),
    inviteSecret: takeInviteSecret(),
    requestedCampaignId: boundedIdentifier(root.getAttribute("data-campaign-id")),
    pendingMutations: Object.create(null),
    unsupportedAmbiguousMutations: Object.create(null),
    busy: false
  };

  bindCampaignControls();
  void initializeCampaignWorkspace();

  async function initializeCampaignWorkspace() {
    setStatus("Loading campaigns and eligible characters.", "working");
    try {
      var results = await Promise.all([
        apiRequest(apiRoot),
        apiRequest(apiRoot + "/eligible-characters")
      ]);
      state.campaigns = Array.isArray(results[0]) ? results[0] : [];
      state.eligibleCharacters = Array.isArray(results[1]) ? results[1] : [];
      renderCampaignList();
      renderEligibleCharacters();

      if (state.inviteId && state.inviteSecret) {
        showJoinLinkPosture();
        setStatus("Private invitation loaded in this open tab. Choose a character to redeem it.", "ready");
      } else if (state.requestedCampaignId) {
        await openCampaign(state.requestedCampaignId);
      } else {
        setStatus(
          state.campaigns.length
            ? "Campaigns are ready. Choose a crew or enter a join code."
            : "No campaign is attached to this account yet. Create one or enter a join code.",
          "ready"
        );
      }
    } catch (error) {
      handleTopLevelError(error, "Campaign workspace could not load.");
    }
  }

  function bindCampaignControls() {
    byId("campaign-refresh").addEventListener("click", function () {
      if (!state.busy) {
        void refreshCampaigns();
      }
    });
    byId("campaign-create-form").addEventListener("submit", function (event) {
      event.preventDefault();
      void createCampaign();
    });
    byId("campaign-join-form").addEventListener("submit", function (event) {
      event.preventDefault();
      void redeemCampaignInvitation();
    });
    byId("campaign-list").addEventListener("click", function (event) {
      var button = event.target.closest("[data-open-campaign]");
      if (button && !state.busy) {
        void openCampaign(boundedIdentifier(button.getAttribute("data-open-campaign")));
      }
    });
    byId("campaign-roster").addEventListener("click", function (event) {
      var button = event.target.closest("[data-open-sheet]");
      if (button && !state.busy) {
        void openSheet(boundedIdentifier(button.getAttribute("data-open-sheet")));
      }
    });
    byId("campaign-create-invite").addEventListener("click", function () {
      if (!state.busy) {
        void createCampaignInvite();
      }
    });
    byId("campaign-copy-invite").addEventListener("click", function () {
      void copyCampaignInvite();
    });
    byId("campaign-gm-edit-form").addEventListener("submit", function (event) {
      event.preventDefault();
      void saveGmCharacterEdit();
    });
    byId("campaign-consent-form").addEventListener("submit", function (event) {
      event.preventDefault();
      void saveGmAuthorityConsent();
    });
    byId("campaign-run-select").addEventListener("change", function () {
      if (!state.busy) {
        void loadRunsite(boundedIdentifier(byId("campaign-run-select").value));
      }
    });
    byId("campaign-runsite-form").addEventListener("submit", function (event) {
      event.preventDefault();
      void saveRunsiteDraft();
    });
    byId("campaign-runsite-publish").addEventListener("click", function () {
      if (!state.busy) {
        void publishRunsite();
      }
    });
  }

  async function refreshCampaigns() {
    return withBusy(async function () {
      state.campaigns = await apiRequest(apiRoot);
      renderCampaignList();
      setStatus("Campaign list refreshed.", "ready");
    });
  }

  async function createCampaign() {
    var name = boundedText(byId("campaign-name").value, 120);
    if (!name) {
      setStatus("Enter a campaign name first.", "error");
      return;
    }

    var summary = boundedText(byId("campaign-summary").value, 1000);
    var initialRunTitle = boundedText(byId("campaign-run-title").value, 160) || "First Run";
    var fingerprint = mutationFingerprint([name, summary, initialRunTitle]);
    if (isUnsupportedMutationBlocked("create-campaign", fingerprint)) {
      setStatus("The previous campaign creation outcome is still unconfirmed. Refresh the campaign list before starting another creation request.", "ambiguous");
      return;
    }
    var campaignIdsBefore = new Set(state.campaigns.map(function (campaign) { return campaign.campaignId; }));

    await withBusy(async function () {
      try {
        var campaign = await apiRequest(apiRoot, {
          method: "POST",
          body: {
            name: name,
            summary: summary,
            visibility: "campaign",
            initialRunTitle: initialRunTitle
          }
        });
        clearUnsupportedMutation("create-campaign");
        state.campaigns.push(campaign);
        renderCampaignList();
        byId("campaign-create-form").reset();
        byId("campaign-run-title").value = "First Run";
        await openCampaign(campaign.campaignId);
        setStatus("Campaign created. Issue a private invitation when the crew is ready.", "ready");
      } catch (error) {
        if (error instanceof AmbiguousMutationProblem) {
          var reconciled = await reconcileCreatedCampaign(campaignIdsBefore, name, summary);
          if (reconciled) {
            clearUnsupportedMutation("create-campaign");
            await openCampaign(reconciled.campaignId);
            setStatus("The create response was lost, but the new campaign was found in the authoritative campaign list.", "ready");
          } else {
            markUnsupportedMutationAmbiguous("create-campaign", fingerprint);
            setStatus("Campaign creation may have completed, but Hub cannot safely replay this contract. Refresh the list before creating again.", "ambiguous");
          }
          return;
        }
        clearUnsupportedMutation("create-campaign");
        handleActionError(error, "Campaign creation failed.");
      }
    });
  }

  async function redeemCampaignInvitation() {
    var character = selectedEligibleCharacter();
    if (!character) {
      setStatus("Choose an eligible character before joining.", "error");
      return;
    }

    var grantGmEditAuthority = byId("campaign-grant-gm").checked === true;
    var route;
    var joinAuthority;
    var code = "";
    if (state.inviteId && state.inviteSecret) {
      route = apiRoot + "/invites/" + encodeURIComponent(state.inviteId) + "/redeem";
      joinAuthority = "link:" + state.inviteId;
    } else {
      code = normalizeJoinCode(byId("campaign-code").value);
      if (!code) {
        setStatus("Enter the join code supplied by your GM.", "error");
        return;
      }
      route = apiRoot + "/join-code/redeem";
      joinAuthority = "code:" + code;
    }
    var fingerprint = mutationFingerprint([
      joinAuthority,
      character.dossierId,
      character.authoritativeCharacterId,
      numberOrZero(character.currentRevision),
      grantGmEditAuthority
    ]);
    var attempt = getOrCreateMutationAttempt("join", fingerprint);
    if (!attempt) {
      setStatus("This browser cannot mint a secure join receipt. Update the browser and try again.", "error");
      return;
    }
    var campaignIdsBefore = new Set(state.campaigns.map(function (campaign) { return campaign.campaignId; }));

    await withBusy(async function () {
      try {
        var body = {
          dossierId: character.dossierId,
          authoritativeCharacterId: character.authoritativeCharacterId,
          expectedCharacterRevision: numberOrZero(character.currentRevision),
          grantGmEditAuthority: grantGmEditAuthority,
          idempotencyKey: attempt.idempotencyKey
        };
        if (state.inviteId && state.inviteSecret) {
          body.secret = state.inviteSecret;
        } else {
          body.code = code;
        }

        var redemption = await apiRequest(route, { method: "POST", body: body });
        clearMutationAttempt("join");
        state.inviteSecret = "";
        byId("campaign-code").value = "";
        try {
          await refreshCampaignData();
          await openCampaign(redemption.campaignId);
        } catch (projectionError) {
          void projectionError;
          setStatus("The join was accepted, but the campaign projection could not reload. Refresh My campaigns to continue.", "ambiguous");
          return;
        }
        setStatus(
          redemption.alreadyJoined
            ? "This character was already attached to the campaign. The existing binding was preserved."
            : "Character joined the campaign. Every crew member can now inspect the player-safe sheet.",
          "ready"
        );
      } catch (error) {
        if (error instanceof AmbiguousMutationProblem) {
          attempt.ambiguous = true;
          var reconciled = await reconcileJoinedCampaign(character.dossierId, campaignIdsBefore);
          if (reconciled) {
            clearMutationAttempt("join");
            state.inviteSecret = "";
            byId("campaign-code").value = "";
            await openCampaign(reconciled.campaignId);
            setStatus("The join response was lost, but authoritative membership confirms that the character joined exactly once.", "ready");
          } else {
            setStatus("The join outcome is not confirmed. Retry from this open page to reuse the same secure command receipt.", "ambiguous");
          }
          return;
        }
        clearMutationAttempt("join");
        if (error instanceof ApiProblem && error.status === 409) {
          await refreshEligibleCharactersAfterConflict();
          setStatus("The character changed before the join completed. Its current revision is loaded; review and submit again.", "conflict");
          return;
        }
        handleActionError(error, "The invitation could not be redeemed.");
      }
    });
  }

  async function refreshEligibleCharactersAfterConflict() {
    state.eligibleCharacters = await apiRequest(apiRoot + "/eligible-characters");
    renderEligibleCharacters();
  }

  async function refreshCampaignData() {
    state.campaigns = await apiRequest(apiRoot);
    state.eligibleCharacters = await apiRequest(apiRoot + "/eligible-characters");
    renderCampaignList();
    renderEligibleCharacters();
  }

  async function openCampaign(campaignId) {
    if (!campaignId) {
      return;
    }

    var open = async function () {
      try {
        var results = await Promise.all([
          apiRequest(apiRoot + "/" + encodeURIComponent(campaignId)),
          apiRequest(apiRoot + "/" + encodeURIComponent(campaignId) + "/roster")
        ]);
        state.campaign = results[0];
        state.roster = Array.isArray(results[1]) ? results[1] : [];
        state.sheet = null;
        renderCampaignWorkspace();
        replaceCampaignRoute(campaignId);
        var firstRunId = Array.isArray(state.campaign.runIds) ? state.campaign.runIds[0] : "";
        if (firstRunId) {
          await loadRunsite(firstRunId);
        } else {
          clearRunsite("This campaign has no run yet.");
        }
        setStatus("Campaign workspace loaded from the Hub authority.", "ready");
      } catch (error) {
        handleActionError(error, "Campaign workspace could not load.");
      }
    };
    if (state.busy) {
      await open();
    } else {
      await withBusy(open);
    }
  }

  function renderCampaignWorkspace() {
    var campaign = state.campaign;
    if (!campaign) {
      byId("campaign-workspace").hidden = true;
      return;
    }

    byId("campaign-workspace").hidden = false;
    byId("campaign-name-heading").textContent = safeText(campaign.name, "Campaign");
    byId("campaign-summary-copy").textContent = safeText(campaign.summary, "No table summary has been added yet.");
    byId("campaign-role").textContent = campaign.canManage ? "Game Master" : safeText(campaign.role, "Player");
    byId("campaign-invite-card").hidden = campaign.canManage !== true;
    byId("campaign-invite-secret").hidden = true;
    byId("campaign-sheet-card").hidden = true;
    renderRoster();
    renderRunChoices();
  }

  function renderCampaignList() {
    var container = byId("campaign-list");
    if (!state.campaigns.length) {
      container.innerHTML = "<p class=\"support-copy\">No campaigns yet. A depleted or new account remains valid; create a campaign or join one without blocking the rest of the table.</p>";
      return;
    }

    container.innerHTML = state.campaigns.map(function (campaign) {
      var role = campaign.canManage ? "Game Master" : safeText(campaign.role, "Player");
      var rosterCount = Array.isArray(campaign.roster) ? campaign.roster.length : 0;
      return "<article class=\"campaign-member-card\">"
        + "<p class=\"eyebrow\">" + escapeHtml(role) + "</p>"
        + "<h3>" + escapeHtml(safeText(campaign.name, "Campaign")) + "</h3>"
        + "<p class=\"support-copy\">" + escapeHtml(safeText(campaign.summary, "No summary yet.")) + "</p>"
        + "<p class=\"support-copy\">" + rosterCount + " crew member(s)</p>"
        + "<button class=\"secondary-button\" type=\"button\" data-open-campaign=\"" + escapeAttribute(campaign.campaignId) + "\">Open crew</button>"
        + "</article>";
    }).join("");
  }

  function renderEligibleCharacters() {
    var select = byId("campaign-character");
    var noCharacters = byId("campaign-no-characters");
    var submit = byId("campaign-join-submit");
    if (!state.eligibleCharacters.length) {
      select.innerHTML = "<option value=\"\">No eligible character</option>";
      select.disabled = true;
      submit.disabled = true;
      noCharacters.hidden = false;
      return;
    }

    select.disabled = false;
    submit.disabled = false;
    noCharacters.hidden = true;
    select.innerHTML = state.eligibleCharacters.map(function (character, index) {
      return "<option value=\"" + index + "\">"
        + escapeHtml(safeText(character.runnerHandle, character.displayName || "Character"))
        + " · revision " + numberOrZero(character.currentRevision)
        + "</option>";
    }).join("");
  }

  function selectedEligibleCharacter() {
    var index = Number.parseInt(byId("campaign-character").value, 10);
    return Number.isInteger(index) && index >= 0 && index < state.eligibleCharacters.length
      ? state.eligibleCharacters[index]
      : null;
  }

  function renderRoster() {
    var container = byId("campaign-roster");
    if (!state.roster.length) {
      container.innerHTML = "<p class=\"support-copy\">No player characters have joined this campaign yet.</p>";
      return;
    }

    container.innerHTML = state.roster.map(function (member) {
      var consent = member.gmEditAuthorityGranted
        ? "GM edits consented · binding revision " + numberOrZero(member.gmAuthorityBindingRevision)
        : "GM edits not authorized";
      return "<article class=\"campaign-member-card\">"
        + "<p class=\"eyebrow\">" + escapeHtml(safeText(member.role, "Player")) + "</p>"
        + "<h3>" + escapeHtml(safeText(member.runnerHandle, member.displayName || "Character")) + "</h3>"
        + "<p class=\"support-copy\">" + escapeHtml(safeText(member.displayName, "")) + " · " + escapeHtml(safeText(member.status, "active")) + "</p>"
        + "<p class=\"support-copy\">Sheet revision " + numberOrZero(member.revision) + " · " + escapeHtml(consent) + "</p>"
        + "<button class=\"secondary-button\" type=\"button\" data-open-sheet=\"" + escapeAttribute(member.dossierId) + "\">View sheet</button>"
        + "</article>";
    }).join("");
  }

  async function openSheet(dossierId) {
    if (!state.campaign || !dossierId) {
      return;
    }
    try {
      state.sheet = await apiRequest(
        apiRoot + "/" + encodeURIComponent(state.campaign.campaignId) + "/sheets/" + encodeURIComponent(dossierId)
      );
      renderSheet();
    } catch (error) {
      handleActionError(error, "The player-safe sheet could not load.");
    }
  }

  function renderSheet() {
    var sheet = state.sheet;
    var card = byId("campaign-sheet-card");
    if (!sheet) {
      card.hidden = true;
      return;
    }

    card.hidden = false;
    byId("campaign-sheet-name").textContent = safeText(sheet.runnerHandle, sheet.displayName || "Character");
    byId("campaign-sheet-revision").textContent = "Revision " + numberOrZero(sheet.revision);
    byId("campaign-sheet-status").textContent = safeText(sheet.displayName, "Character")
      + " · " + safeText(sheet.status, "active")
      + " · rule environment " + safeText(sheet.ruleEnvironmentFingerprint, "not supplied");
    renderPlayerSafeSections(byId("campaign-sheet-sections"), sheet.sections);

    var canGmEdit = state.campaign.canManage === true && sheet.canManage === true && sheet.gmEditAuthorityGranted === true;
    var gmForm = byId("campaign-gm-edit-form");
    gmForm.hidden = !canGmEdit;
    byId("campaign-sheet-handle").value = safeText(sheet.runnerHandle, "");
    byId("campaign-sheet-display-name").value = safeText(sheet.displayName, "");
    byId("campaign-gm-edit-boundary").textContent = state.campaign.canManage !== true
      ? "This is a view-only crew sheet. Only a campaign GM with current player consent can edit it."
      : sheet.gmEditAuthorityGranted
        ? "Player consent is active at binding revision " + numberOrZero(sheet.gmAuthorityBindingRevision) + "."
        : "This player has not granted GM edit authority. The sheet remains view-only.";

    var ownCharacter = state.eligibleCharacters.some(function (character) {
      return character.dossierId === sheet.dossierId;
    });
    var consentForm = byId("campaign-consent-form");
    consentForm.hidden = !ownCharacter;
    byId("campaign-consent-toggle").checked = sheet.gmEditAuthorityGranted === true;
  }

  function renderPlayerSafeSections(container, sections) {
    var safeSections = Array.isArray(sections) ? sections : [];
    if (!safeSections.length) {
      container.innerHTML = "<p class=\"support-copy\">No player-safe sections are published for this character yet.</p>";
      return;
    }

    container.innerHTML = safeSections.map(function (section) {
      var details = [section.publicationSummary, section.ownershipSummary, section.trustBand, section.nextSafeAction]
        .filter(function (value) { return typeof value === "string" && value.trim(); })
        .map(function (value) { return "<li>" + escapeHtml(value) + "</li>"; })
        .join("");
      return "<article class=\"campaign-sheet-section\">"
        + "<p class=\"eyebrow\">" + escapeHtml(safeText(section.kind, "Sheet")) + "</p>"
        + "<h3>" + escapeHtml(safeText(section.label, "Character section")) + "</h3>"
        + "<p>" + escapeHtml(safeText(section.summary, "No summary supplied.")) + "</p>"
        + (details ? "<ul class=\"label-list\">" + details + "</ul>" : "")
        + "</article>";
    }).join("");
  }

  async function saveGmCharacterEdit() {
    if (!state.campaign || !state.sheet || state.campaign.canManage !== true || state.sheet.gmEditAuthorityGranted !== true) {
      setStatus("GM edit authority is not active for this sheet.", "error");
      return;
    }

    var dossierId = state.sheet.dossierId;
    var expectedRevision = numberOrZero(state.sheet.revision);
    var runnerHandle = boundedText(byId("campaign-sheet-handle").value, 120);
    var displayName = boundedText(byId("campaign-sheet-display-name").value, 160);
    var reason = boundedText(byId("campaign-sheet-reason").value, 240);
    var fingerprint = mutationFingerprint([
      state.campaign.campaignId,
      dossierId,
      expectedRevision,
      runnerHandle,
      displayName,
      safeText(state.sheet.status, "active"),
      reason
    ]);
    var attempt = getOrCreateMutationAttempt("gm-edit", fingerprint);
    var request = {
      expectedRevision: expectedRevision,
      idempotencyKey: attempt ? attempt.idempotencyKey : "",
      runnerHandle: runnerHandle,
      displayName: displayName,
      status: safeText(state.sheet.status, "active"),
      reason: reason,
      sections: null
    };
    if (!request.idempotencyKey || !request.runnerHandle || !request.displayName || !request.reason) {
      setStatus("Complete the handle, display name, and reason before saving.", "error");
      return;
    }

    await withBusy(async function () {
      try {
        var receipt = await apiRequest(
          apiRoot + "/" + encodeURIComponent(state.campaign.campaignId) + "/sheets/" + encodeURIComponent(dossierId),
          { method: "PUT", body: request, definitiveFailureStatuses: [503] }
        );
        clearMutationAttempt("gm-edit");
        var commandRevision = numberOrZero(receipt.revision);
        var currentRevision = receipt.currentRevision == null ? commandRevision : numberOrZero(receipt.currentRevision);
        try {
          await reloadRosterAndSheet(dossierId);
        } catch (projectionError) {
          void projectionError;
          setStatus("The GM edit was accepted at revision " + commandRevision + ", but the current sheet could not reload.", "ambiguous");
          return;
        }
        setStatus(
          currentRevision > commandRevision
            ? "GM edit was accepted at revision " + commandRevision + ", but the owner has newer revision " + currentRevision + ". The newer canonical sheet is loaded."
            : "Canonical GM edit saved at character revision " + commandRevision + ".",
          currentRevision > commandRevision ? "conflict" : "ready"
        );
      } catch (error) {
        if (error instanceof AmbiguousMutationProblem) {
          attempt.ambiguous = true;
          var reconciled = await reconcileGmCharacterEdit(dossierId, request);
          if (reconciled) {
            clearMutationAttempt("gm-edit");
            setStatus("The edit response was lost, but the canonical sheet confirms the edit at revision " + numberOrZero(reconciled.revision) + ".", "ready");
          } else {
            setStatus("The GM edit outcome is not confirmed. Retry without changing the fields to reuse the same command receipt.", "ambiguous");
          }
          return;
        }
        clearMutationAttempt("gm-edit");
        if (error instanceof ApiProblem && error.status === 409) {
          await reloadRosterAndSheet(dossierId);
          setStatus("The character changed elsewhere before this edit completed. The current canonical revision is loaded; review and submit again.", "conflict");
          return;
        }
        if (error instanceof ApiProblem && error.status === 503) {
          setStatus("Canonical Core editing is temporarily unavailable. Nothing was changed; retry after the owner adapter is healthy.", "error");
          return;
        }
        handleActionError(error, "The canonical GM edit failed.");
      }
    });
  }

  async function reloadRosterAndSheet(dossierId) {
    state.roster = await apiRequest(apiRoot + "/" + encodeURIComponent(state.campaign.campaignId) + "/roster");
    renderRoster();
    await openSheet(dossierId);
  }

  async function saveGmAuthorityConsent() {
    if (!state.campaign || !state.sheet) {
      return;
    }
    var dossierId = state.sheet.dossierId;
    var expectedBindingRevision = numberOrZero(state.sheet.gmAuthorityBindingRevision);
    var grantGmEditAuthority = byId("campaign-consent-toggle").checked === true;
    var reason = boundedText(byId("campaign-consent-reason").value, 240);
    var fingerprint = mutationFingerprint([
      state.campaign.campaignId,
      dossierId,
      expectedBindingRevision,
      grantGmEditAuthority,
      reason
    ]);
    var attempt = getOrCreateMutationAttempt("gm-consent", fingerprint);
    var request = {
      expectedBindingRevision: expectedBindingRevision,
      grantGmEditAuthority: grantGmEditAuthority,
      idempotencyKey: attempt ? attempt.idempotencyKey : "",
      reason: reason
    };
    if (!request.idempotencyKey || !request.reason) {
      setStatus("Enter a consent reason before saving.", "error");
      return;
    }

    await withBusy(async function () {
      try {
        var receipt = await apiRequest(
          apiRoot + "/" + encodeURIComponent(state.campaign.campaignId) + "/sheets/" + encodeURIComponent(dossierId) + "/gm-authority",
          { method: "PUT", body: request }
        );
        clearMutationAttempt("gm-consent");
        try {
          await reloadRosterAndSheet(dossierId);
        } catch (projectionError) {
          void projectionError;
          setStatus("The consent update was accepted at binding revision " + numberOrZero(receipt.bindingRevision) + ", but the sheet could not reload.", "ambiguous");
          return;
        }
        setStatus(
          receipt.gmEditAuthorityGranted
            ? "GM edit consent granted at binding revision " + numberOrZero(receipt.bindingRevision) + "."
            : "GM edit consent revoked. The crew sheet remains viewable, but GM edits are blocked.",
          "ready"
        );
      } catch (error) {
        if (error instanceof AmbiguousMutationProblem) {
          attempt.ambiguous = true;
          var reconciled = await reconcileGmAuthority(dossierId, request);
          if (reconciled) {
            clearMutationAttempt("gm-consent");
            setStatus(
              request.grantGmEditAuthority
                ? "The consent response was lost, but the authoritative binding confirms that GM edits are enabled."
                : "The consent response was lost, but the authoritative binding confirms that GM edits are revoked.",
              "ready"
            );
          } else {
            setStatus("The consent outcome is not confirmed. Retry without changing the choice to reuse the same command receipt.", "ambiguous");
          }
          return;
        }
        clearMutationAttempt("gm-consent");
        if (error instanceof ApiProblem && error.status === 409) {
          await reloadRosterAndSheet(dossierId);
          setStatus("Consent changed on another device. The current binding revision is loaded; review it before trying again.", "conflict");
          return;
        }
        handleActionError(error, "GM edit consent could not be updated.");
      }
    });
  }

  async function createCampaignInvite() {
    if (!state.campaign || state.campaign.canManage !== true) {
      return;
    }
    var fingerprint = mutationFingerprint([state.campaign.campaignId, 1440, 1]);
    if (isUnsupportedMutationBlocked("create-invite", fingerprint)) {
      setStatus("The previous invite creation outcome is unconfirmed. Hub cannot safely recover or replay its secret, so no second invite was issued.", "ambiguous");
      return;
    }
    await withBusy(async function () {
      try {
        var invite = await apiRequest(
          apiRoot + "/" + encodeURIComponent(state.campaign.campaignId) + "/invites",
          { method: "POST", body: { expiresInMinutes: 1440, maxUses: 1 } }
        );
        clearUnsupportedMutation("create-invite");
        byId("campaign-invite-secret").hidden = false;
        byId("campaign-invite-link").value = absoluteSameOriginPath(invite.joinPath);
        byId("campaign-invite-code").value = safeText(invite.shortCode, "");
        byId("campaign-copy-status").textContent = "Invitation is visible only in this open page.";
        setStatus("A one-use invitation is ready. Share the private link or short code.", "ready");
      } catch (error) {
        if (error instanceof AmbiguousMutationProblem) {
          markUnsupportedMutationAmbiguous("create-invite", fingerprint);
          setStatus("Invite creation may have completed, but this Hub contract cannot replay the secret safely. No automatic retry was sent.", "ambiguous");
          return;
        }
        clearUnsupportedMutation("create-invite");
        handleActionError(error, "Invitation creation failed.");
      }
    });
  }

  async function copyCampaignInvite() {
    var link = byId("campaign-invite-link").value;
    if (!link || !navigator.clipboard || typeof navigator.clipboard.writeText !== "function") {
      byId("campaign-copy-status").textContent = "Copy is unavailable here. Select the private link manually.";
      return;
    }
    try {
      await navigator.clipboard.writeText(link);
      byId("campaign-copy-status").textContent = "Private link copied. Share it only with the intended player.";
    } catch (error) {
      void error;
      byId("campaign-copy-status").textContent = "Copy was blocked. Select the private link manually.";
    }
  }

  function renderRunChoices() {
    var select = byId("campaign-run-select");
    var runIds = state.campaign && Array.isArray(state.campaign.runIds) ? state.campaign.runIds : [];
    select.innerHTML = runIds.length
      ? runIds.map(function (runId) {
        return "<option value=\"" + escapeAttribute(runId) + "\">" + escapeHtml(runId) + "</option>";
      }).join("")
      : "<option value=\"\">No runs</option>";
    select.disabled = runIds.length === 0;
  }

  async function loadRunsite(runId) {
    if (!state.campaign || !runId) {
      clearRunsite("Choose a run to inspect its Runsite.");
      return;
    }
    try {
      if (state.campaign.canManage === true) {
        try {
          state.runsiteDraft = await apiRequest(runsiteRoute(runId) + "/draft");
        } catch (error) {
          if (!(error instanceof ApiProblem) || error.status !== 404) {
            throw error;
          }
          state.runsiteDraft = emptyRunsiteDraft(runId);
        }
        renderRunsiteDraft();
      } else {
        try {
          state.runsitePlayer = await apiRequest(runsiteRoute(runId));
        } catch (error) {
          if (!(error instanceof ApiProblem) || error.status !== 404) {
            throw error;
          }
          state.runsitePlayer = null;
        }
        renderRunsitePlayer();
      }
    } catch (error) {
      handleActionError(error, "Runsite could not load.");
    }
  }

  function emptyRunsiteDraft(runId) {
    return {
      campaignId: state.campaign.campaignId,
      runId: runId,
      revision: 0,
      title: "",
      summary: "",
      playerSections: [],
      gmNotes: "",
      publishedRevision: null
    };
  }

  function renderRunsiteDraft() {
    var draft = state.runsiteDraft;
    byId("campaign-runsite-player").hidden = true;
    byId("campaign-runsite-form").hidden = false;
    byId("campaign-runsite-title").value = safeText(draft.title, "");
    byId("campaign-runsite-summary").value = safeText(draft.summary, "");
    byId("campaign-runsite-sections").value = (Array.isArray(draft.playerSections) ? draft.playerSections : [])
      .map(function (section) { return safeText(section.heading, "") + " | " + safeText(section.body, ""); })
      .join("\n");
    byId("campaign-runsite-gm-notes").value = safeText(draft.gmNotes, "");
    byId("campaign-runsite-revision").textContent = "Draft revision " + numberOrZero(draft.revision)
      + (draft.publishedRevision == null ? " · not published" : " · published revision " + numberOrZero(draft.publishedRevision));
  }

  function renderRunsitePlayer() {
    byId("campaign-runsite-form").hidden = true;
    byId("campaign-runsite-player").hidden = false;
    var player = state.runsitePlayer;
    if (!player) {
      byId("campaign-runsite-player-title").textContent = "No published Runsite yet";
      byId("campaign-runsite-player-summary").textContent = "The GM is still planning this run. Private notes are never projected here.";
      byId("campaign-runsite-player-sections").innerHTML = "";
      return;
    }
    byId("campaign-runsite-player-title").textContent = safeText(player.title, "Runsite");
    byId("campaign-runsite-player-summary").textContent = safeText(player.summary, "");
    renderRunsiteSections(byId("campaign-runsite-player-sections"), player.sections);
  }

  function renderRunsiteSections(container, sections) {
    var safeSections = Array.isArray(sections) ? sections : [];
    container.innerHTML = safeSections.map(function (section) {
      return "<article class=\"campaign-sheet-section\"><h3>"
        + escapeHtml(safeText(section.heading, "Area")) + "</h3><p>"
        + escapeHtml(safeText(section.body, "")) + "</p></article>";
    }).join("");
  }

  async function saveRunsiteDraft() {
    if (!state.campaign || !state.runsiteDraft || state.campaign.canManage !== true) {
      return;
    }
    var runId = state.runsiteDraft.runId;
    var request = {
      expectedRevision: numberOrZero(state.runsiteDraft.revision),
      title: boundedText(byId("campaign-runsite-title").value, 160),
      summary: boundedText(byId("campaign-runsite-summary").value, 2000),
      playerSections: parseRunsiteSections(byId("campaign-runsite-sections").value),
      gmNotes: boundedText(byId("campaign-runsite-gm-notes").value, 4000)
    };
    if (!request.title) {
      setStatus("Enter a Runsite title before saving.", "error");
      return;
    }
    await withBusy(async function () {
      try {
        state.runsiteDraft = await apiRequest(runsiteRoute(runId) + "/draft", { method: "PUT", body: request });
        renderRunsiteDraft();
        setStatus("Runsite draft saved at revision " + numberOrZero(state.runsiteDraft.revision) + ". Players still see only the last published revision.", "ready");
      } catch (error) {
        if (error instanceof ApiProblem && error.status === 409) {
          await loadRunsite(runId);
          setStatus("The Runsite draft changed elsewhere. The current revision is loaded; review it before saving again.", "conflict");
          return;
        }
        handleActionError(error, "Runsite draft could not be saved.");
      }
    });
  }

  async function publishRunsite() {
    if (!state.campaign || !state.runsiteDraft || state.campaign.canManage !== true) {
      return;
    }
    var runId = state.runsiteDraft.runId;
    var expectedRevision = numberOrZero(state.runsiteDraft.revision);
    if (numberOrZero(state.runsiteDraft.publishedRevision) === expectedRevision && expectedRevision > 0) {
      setStatus("This exact Runsite draft revision is already published.", "ready");
      return;
    }
    var fingerprint = mutationFingerprint([
      state.campaign.campaignId,
      runId,
      expectedRevision,
      safeText(state.runsiteDraft.title, ""),
      safeText(state.runsiteDraft.summary, ""),
      JSON.stringify(Array.isArray(state.runsiteDraft.playerSections) ? state.runsiteDraft.playerSections : [])
    ]);
    if (isUnsupportedMutationBlocked("publish-runsite", fingerprint)) {
      var existing = await reconcileRunsitePublication(runId, state.runsiteDraft);
      if (existing) {
        clearUnsupportedMutation("publish-runsite");
        state.runsiteDraft.publishedRevision = existing.revision;
        renderRunsiteDraft();
        setStatus("Authoritative Runsite state confirms that this draft revision is already published.", "ready");
      } else {
        setStatus("Runsite publication remains unconfirmed. Hub does not accept an idempotency key here, so no second publish request was sent.", "ambiguous");
      }
      return;
    }
    await withBusy(async function () {
      try {
        var published = await apiRequest(runsiteRoute(runId) + "/publish", {
          method: "POST",
          body: { expectedRevision: expectedRevision }
        });
        clearUnsupportedMutation("publish-runsite");
        state.runsiteDraft.publishedRevision = published.revision;
        renderRunsiteDraft();
        setStatus("Player-safe Runsite revision " + numberOrZero(published.revision) + " is published. Private GM notes were not included.", "ready");
      } catch (error) {
        if (error instanceof AmbiguousMutationProblem) {
          var reconciled = await reconcileRunsitePublication(runId, state.runsiteDraft);
          if (reconciled) {
            clearUnsupportedMutation("publish-runsite");
            state.runsiteDraft.publishedRevision = reconciled.revision;
            renderRunsiteDraft();
            setStatus("The publish response was lost, but the player-safe Runsite projection confirms revision " + numberOrZero(reconciled.revision) + ".", "ready");
          } else {
            markUnsupportedMutationAmbiguous("publish-runsite", fingerprint);
            setStatus("Runsite publication may have completed. No blind retry was sent because Hub does not accept an idempotency key for publish.", "ambiguous");
          }
          return;
        }
        clearUnsupportedMutation("publish-runsite");
        if (error instanceof ApiProblem && error.status === 409) {
          await loadRunsite(runId);
          setStatus("The Runsite changed before publication. The current draft is loaded; review it before publishing.", "conflict");
          return;
        }
        handleActionError(error, "Runsite publication failed.");
      }
    });
  }

  function runsiteRoute(runId) {
    return apiRoot + "/" + encodeURIComponent(state.campaign.campaignId)
      + "/runs/" + encodeURIComponent(runId) + "/runsite";
  }

  function parseRunsiteSections(value) {
    return String(value || "").split(/\r?\n/).map(function (line) {
      var separator = line.indexOf("|");
      if (separator < 0) {
        return { heading: boundedText(line, 160), body: "" };
      }
      return {
        heading: boundedText(line.slice(0, separator), 160),
        body: boundedText(line.slice(separator + 1), 1000)
      };
    }).filter(function (section) { return section.heading || section.body; }).slice(0, 20);
  }

  function clearRunsite(message) {
    state.runsiteDraft = null;
    state.runsitePlayer = null;
    byId("campaign-runsite-form").hidden = true;
    byId("campaign-runsite-player").hidden = false;
    byId("campaign-runsite-player-title").textContent = "Runsite unavailable";
    byId("campaign-runsite-player-summary").textContent = message;
    byId("campaign-runsite-player-sections").innerHTML = "";
  }

  function getOrCreateMutationAttempt(kind, fingerprint) {
    var current = state.pendingMutations[kind];
    if (current && current.fingerprint === fingerprint) {
      return current;
    }
    var idempotencyKey = newIdempotencyKey();
    if (!idempotencyKey) {
      return null;
    }
    var created = {
      fingerprint: fingerprint,
      idempotencyKey: idempotencyKey,
      ambiguous: false
    };
    state.pendingMutations[kind] = created;
    return created;
  }

  function clearMutationAttempt(kind) {
    delete state.pendingMutations[kind];
  }

  function mutationFingerprint(parts) {
    return JSON.stringify(Array.isArray(parts) ? parts : []);
  }

  function isUnsupportedMutationBlocked(kind, fingerprint) {
    var current = state.unsupportedAmbiguousMutations[kind];
    return Boolean(current && current.fingerprint === fingerprint && current.ambiguous === true);
  }

  function markUnsupportedMutationAmbiguous(kind, fingerprint) {
    state.unsupportedAmbiguousMutations[kind] = { fingerprint: fingerprint, ambiguous: true };
  }

  function clearUnsupportedMutation(kind) {
    delete state.unsupportedAmbiguousMutations[kind];
  }

  async function reconcileCreatedCampaign(campaignIdsBefore, name, summary) {
    var campaigns = await tryApiRequest(apiRoot);
    if (!Array.isArray(campaigns)) {
      return null;
    }
    state.campaigns = campaigns;
    renderCampaignList();
    var candidates = campaigns.filter(function (campaign) {
      return !campaignIdsBefore.has(campaign.campaignId)
        && safeText(campaign.name, "") === name
        && (!summary || safeText(campaign.summary, "") === summary);
    });
    return candidates.length === 1 ? candidates[0] : null;
  }

  async function reconcileJoinedCampaign(dossierId, campaignIdsBefore) {
    var campaigns = await tryApiRequest(apiRoot);
    if (!Array.isArray(campaigns)) {
      return null;
    }
    state.campaigns = campaigns;
    renderCampaignList();
    var memberships = campaigns.filter(function (campaign) {
      return Array.isArray(campaign.roster) && campaign.roster.some(function (member) {
        return member.dossierId === dossierId;
      });
    });
    var newlyVisible = memberships.filter(function (campaign) {
      return !campaignIdsBefore.has(campaign.campaignId);
    });
    if (newlyVisible.length === 1) {
      return newlyVisible[0];
    }
    return memberships.length === 1 ? memberships[0] : null;
  }

  async function reconcileGmCharacterEdit(dossierId, request) {
    var current = await tryApiRequest(
      apiRoot + "/" + encodeURIComponent(state.campaign.campaignId) + "/sheets/" + encodeURIComponent(dossierId)
    );
    if (!current
      || numberOrZero(current.revision) <= request.expectedRevision
      || safeText(current.runnerHandle, "") !== request.runnerHandle
      || safeText(current.displayName, "") !== request.displayName
      || safeText(current.status, "active") !== request.status) {
      return null;
    }
    await applyReconciledSheet(current);
    return current;
  }

  async function reconcileGmAuthority(dossierId, request) {
    var current = await tryApiRequest(
      apiRoot + "/" + encodeURIComponent(state.campaign.campaignId) + "/sheets/" + encodeURIComponent(dossierId)
    );
    if (!current
      || numberOrZero(current.gmAuthorityBindingRevision) <= request.expectedBindingRevision
      || (current.gmEditAuthorityGranted === true) !== request.grantGmEditAuthority) {
      return null;
    }
    await applyReconciledSheet(current);
    return current;
  }

  async function applyReconciledSheet(sheet) {
    state.sheet = sheet;
    renderSheet();
    var roster = await tryApiRequest(apiRoot + "/" + encodeURIComponent(state.campaign.campaignId) + "/roster");
    if (Array.isArray(roster)) {
      state.roster = roster;
      renderRoster();
    }
  }

  async function reconcileRunsitePublication(runId, draft) {
    var published = await tryApiRequest(runsiteRoute(runId));
    if (!published
      || numberOrZero(published.revision) !== numberOrZero(draft.revision)
      || safeText(published.title, "") !== safeText(draft.title, "")
      || safeText(published.summary, "") !== safeText(draft.summary, "")
      || !runsiteSectionsMatch(published.sections, draft.playerSections)) {
      return null;
    }
    return published;
  }

  function runsiteSectionsMatch(publishedSections, draftSections) {
    var published = Array.isArray(publishedSections) ? publishedSections : [];
    var draft = Array.isArray(draftSections) ? draftSections : [];
    if (published.length !== draft.length) {
      return false;
    }
    return published.every(function (section, index) {
      return safeText(section.heading, "") === safeText(draft[index].heading, "")
        && safeText(section.body, "") === safeText(draft[index].body, "");
    });
  }

  async function tryApiRequest(route) {
    try {
      return await apiRequest(route);
    } catch (error) {
      void error;
      return null;
    }
  }

  async function apiRequest(route, options) {
    var requestOptions = options || {};
    var method = String(requestOptions.method || "GET").toUpperCase();
    var unsafe = method !== "GET" && method !== "HEAD" && method !== "OPTIONS";
    var headers = { accept: "application/json" };
    var body;
    if (unsafe) {
      var antiforgery = await getAntiforgeryToken();
      headers[antiforgery.headerName] = antiforgery.requestToken;
      headers["content-type"] = "application/json";
      body = JSON.stringify(requestOptions.body == null ? {} : requestOptions.body);
      if (new TextEncoder().encode(body).byteLength > maxRequestBytes) {
        throw new ApiProblem(413, "The campaign request exceeds the 64 KiB safety limit.", null);
      }
    }

    var result;
    try {
      result = await boundedFetchText(route, {
        method: method,
        headers: headers,
        body: body,
        credentials: "include",
        cache: "no-store",
        redirect: "error"
      });
    } catch (error) {
      if (error instanceof RequestTransportProblem) {
        if (unsafe && error.requestMayHaveReachedServer) {
          throw new AmbiguousMutationProblem(
            error.timedOut
              ? "The campaign mutation timed out after it may have reached Hub."
              : "The campaign mutation response was lost after it may have reached Hub."
          );
        }
        throw new ApiProblem(503, "Hub could not be reached before the request completed.", null);
      }
      throw error;
    }
    var response = result.response;
    var text = result.text;
    var payload = parseJson(text);
    if (!response.ok) {
      if (unsafe && response.status === 400) {
        state.antiforgery = null;
      }
      var definitiveFailureStatuses = Array.isArray(requestOptions.definitiveFailureStatuses)
        ? requestOptions.definitiveFailureStatuses
        : [];
      if (unsafe
        && response.status >= 500
        && !definitiveFailureStatuses.includes(response.status)) {
        throw new AmbiguousMutationProblem("Hub returned a server failure after the mutation may have committed.");
      }
      throw new ApiProblem(response.status, problemDetail(payload, response.status), payload);
    }
    if (unsafe && payload == null) {
      throw new AmbiguousMutationProblem("Hub returned no valid mutation receipt after the mutation may have committed.");
    }
    return payload;
  }

  async function getAntiforgeryToken() {
    if (state.antiforgery) {
      return state.antiforgery;
    }
    var result;
    try {
      result = await boundedFetchText(antiforgeryRoute, {
        method: "GET",
        headers: { accept: "application/json" },
        credentials: "include",
        cache: "no-store",
        redirect: "error"
      });
    } catch (error) {
      if (error instanceof RequestTransportProblem) {
        throw new ApiProblem(503, "Hub antiforgery protection could not be loaded. No campaign mutation was attempted.", null);
      }
      throw error;
    }
    var response = result.response;
    var payload = parseJson(result.text);
    if (!response.ok) {
      throw new ApiProblem(response.status, problemDetail(payload, response.status), payload);
    }
    var requestToken = payload && typeof payload.requestToken === "string" ? payload.requestToken.trim() : "";
    var headerName = payload && typeof payload.headerName === "string" ? payload.headerName.trim() : "";
    if (!requestToken || requestToken.length > 4096 || !/^[A-Za-z0-9-]{1,80}$/.test(headerName)) {
      throw new ApiProblem(503, "Hub antiforgery protection is unavailable. No campaign mutation was attempted.", payload);
    }
    state.antiforgery = { requestToken: requestToken, headerName: headerName };
    return state.antiforgery;
  }

  async function boundedFetchText(route, options) {
    if (typeof window.AbortController !== "function") {
      throw new RequestTransportProblem(false, false);
    }
    var controller = new window.AbortController();
    var timedOut = false;
    var requestMayHaveReachedServer = false;
    var timer = window.setTimeout(function () {
      timedOut = true;
      controller.abort();
    }, requestTimeoutMs);
    try {
      requestMayHaveReachedServer = true;
      var response = await fetch(route, Object.assign({}, options, { signal: controller.signal }));
      var text = await response.text();
      return { response: response, text: text };
    } catch (error) {
      void error;
      throw new RequestTransportProblem(requestMayHaveReachedServer, timedOut);
    } finally {
      window.clearTimeout(timer);
    }
  }

  async function withBusy(action) {
    if (state.busy) {
      return;
    }
    state.busy = true;
    root.setAttribute("aria-busy", "true");
    try {
      await action();
    } finally {
      state.busy = false;
      root.removeAttribute("aria-busy");
    }
  }

  function takeInviteSecret() {
    var hash = String(window.location.hash || "").replace(/^#/, "");
    var secret = "";
    try {
      secret = new URLSearchParams(hash).get("secret") || "";
    } catch (error) {
      void error;
    }
    if (window.location.hash) {
      window.history.replaceState({}, "", window.location.pathname + window.location.search);
    }
    secret = secret.trim();
    return secret.length > 0 && secret.length <= 4096 ? secret : "";
  }

  function showJoinLinkPosture() {
    byId("campaign-join-title").textContent = "Join with this private invitation";
    byId("campaign-join-secret-copy").hidden = false;
    byId("campaign-code-label").hidden = true;
    byId("campaign-code").hidden = true;
    byId("campaign-code").required = false;
  }

  function replaceCampaignRoute(campaignId) {
    if (!campaignId) {
      return;
    }
    var path = "/mobile/campaigns/" + encodeURIComponent(campaignId);
    if (window.location.pathname !== path) {
      window.history.replaceState({}, "", path);
    }
  }

  function handleTopLevelError(error, fallback) {
    if (error instanceof ApiProblem && (error.status === 401 || error.status === 403)) {
      byId("campaign-auth-required").hidden = false;
      byId("campaign-list-card").hidden = true;
      byId("campaign-create-card").hidden = true;
      byId("campaign-join-card").hidden = true;
      byId("campaign-workspace").hidden = true;
      setStatus("Sign in with the intended Hub account to continue. No invitation was redeemed.", "error");
      return;
    }
    handleActionError(error, fallback);
  }

  function handleActionError(error, fallback) {
    if (error instanceof AmbiguousMutationProblem) {
      setStatus(error.detail || fallback, "ambiguous");
      return;
    }
    if (error instanceof ApiProblem) {
      if (error.status === 401) {
        handleTopLevelError(error, fallback);
        return;
      }
      if (error.status === 403) {
        setStatus("This signed-in account does not have authority for that campaign action.", "error");
        return;
      }
      if (error.status === 429) {
        setStatus("Invitation requests are temporarily limited. Wait for the Hub retry window before trying again.", "error");
        return;
      }
      setStatus(error.detail || fallback, error.status === 409 ? "conflict" : "error");
      return;
    }
    setStatus(fallback, "error");
  }

  function setStatus(message, tone) {
    var status = byId("campaign-status");
    status.textContent = message;
    status.setAttribute("data-tone", tone || "ready");
  }

  function problemDetail(payload, status) {
    if (payload && typeof payload.detail === "string" && payload.detail.trim()) {
      return payload.detail.trim().slice(0, 1000);
    }
    if (payload && typeof payload.title === "string" && payload.title.trim()) {
      return payload.title.trim().slice(0, 1000);
    }
    return "Campaign API request failed with HTTP " + status + ".";
  }

  function parseJson(value) {
    if (!value) {
      return null;
    }
    try {
      return JSON.parse(value);
    } catch (error) {
      void error;
      return null;
    }
  }

  function newIdempotencyKey() {
    return window.crypto && typeof window.crypto.randomUUID === "function"
      ? window.crypto.randomUUID()
      : "";
  }

  function absoluteSameOriginPath(path) {
    if (typeof path !== "string" || !path.startsWith("/") || path.startsWith("//")) {
      return "";
    }
    return window.location.origin + path;
  }

  function normalizeJoinCode(value) {
    var normalized = String(value || "").trim().toUpperCase().replace(/[^A-Z0-9-]/g, "");
    return normalized.slice(0, 32);
  }

  function boundedIdentifier(value) {
    var normalized = String(value || "").trim();
    return normalized.length > 0 && normalized.length <= 160 && /^[A-Za-z0-9._:-]+$/.test(normalized)
      ? normalized
      : "";
  }

  function boundedText(value, maximumLength) {
    return String(value || "").trim().slice(0, maximumLength);
  }

  function safeText(value, fallback) {
    return typeof value === "string" && value.trim() ? value.trim() : fallback;
  }

  function numberOrZero(value) {
    return typeof value === "number" && Number.isSafeInteger(value) && value >= 0 ? value : 0;
  }

  function byId(id) {
    return document.getElementById(id);
  }

  function escapeHtml(value) {
    return String(value == null ? "" : value)
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/\"/g, "&quot;")
      .replace(/'/g, "&#39;");
  }

  function escapeAttribute(value) {
    return escapeHtml(boundedIdentifier(value));
  }

  function ApiProblem(status, detail, payload) {
    this.name = "ApiProblem";
    this.status = status;
    this.detail = detail;
    this.payload = payload;
  }
  ApiProblem.prototype = Object.create(Error.prototype);
  ApiProblem.prototype.constructor = ApiProblem;

  function AmbiguousMutationProblem(detail) {
    this.name = "AmbiguousMutationProblem";
    this.detail = detail;
  }
  AmbiguousMutationProblem.prototype = Object.create(Error.prototype);
  AmbiguousMutationProblem.prototype.constructor = AmbiguousMutationProblem;

  function RequestTransportProblem(requestMayHaveReachedServer, timedOut) {
    this.name = "RequestTransportProblem";
    this.requestMayHaveReachedServer = requestMayHaveReachedServer === true;
    this.timedOut = timedOut === true;
  }
  RequestTransportProblem.prototype = Object.create(Error.prototype);
  RequestTransportProblem.prototype.constructor = RequestTransportProblem;
}());
