from __future__ import annotations

import copy
import hashlib
import http.client
import json
import os
import socket
import subprocess
import tempfile
import time
from contextlib import closing
from pathlib import Path
from typing import Any, Iterator
from urllib.parse import urlsplit

import pytest
from playwright.sync_api import Browser, BrowserContext, Page, Route, sync_playwright


ROOT = Path(__file__).resolve().parents[1]
WEB_PROJECT = ROOT / "src" / "Chummer.Play.Web" / "Chummer.Play.Web.csproj"

GM_EMAIL = "gm.campaign@girschele.com"
ALICE_EMAIL = "alice.runner@girschele.com"
BOB_EMAIL = "bob.runner@girschele.com"
DEPLETED_EMAIL = "depleted.runner@girschele.com"


def _free_port() -> int:
    with closing(socket.socket(socket.AF_INET, socket.SOCK_STREAM)) as candidate:
        candidate.bind(("127.0.0.1", 0))
        return int(candidate.getsockname()[1])


def _request_health(port: int) -> bool:
    connection = http.client.HTTPConnection("127.0.0.1", port, timeout=3)
    try:
        connection.request("GET", "/health")
        response = connection.getresponse()
        return response.status == 200 and response.read().strip() == b"ok"
    finally:
        connection.close()


@pytest.fixture(scope="module")
def campaign_host() -> Iterator[str]:
    port = _free_port()
    base_url = f"http://127.0.0.1:{port}"
    with tempfile.TemporaryDirectory(prefix="chummer-mobile-campaign-") as state_root:
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
                if process.poll() is not None:
                    raise RuntimeError("Play host exited before campaign tests started")
                try:
                    if _request_health(port):
                        break
                except OSError:
                    pass
                time.sleep(0.2)
            else:
                raise RuntimeError("Play host did not become healthy")
            yield base_url
        finally:
            process.terminate()
            try:
                process.wait(timeout=10)
            except subprocess.TimeoutExpired:
                process.kill()
                process.wait(timeout=5)


class FakeCampaignHub:
    def __init__(self) -> None:
        self.campaign_id = "campaign-night-shift"
        self.run_id = "run-dockyard"
        self.created = False
        self.invite_counter = 0
        self.invites: dict[str, dict[str, str]] = {}
        self.members: dict[str, dict[str, Any]] = {}
        self.draft: dict[str, Any] | None = None
        self.published: dict[str, Any] | None = None
        self.force_edit_conflict_once = True
        self.unsafe_requests: list[dict[str, str]] = []
        self.drop_after_commit_once: set[str] = set()
        self.drop_read_once: set[tuple[str, str]] = set()
        self.idempotent_responses: dict[
            tuple[str, str, str, str], tuple[str, int, dict[str, Any]]
        ] = {}
        self.command_keys: dict[str, list[str]] = {
            "create": [],
            "invite": [],
            "join": [],
            "edit": [],
            "consent": [],
            "draft": [],
            "publish": [],
        }
        self.commit_counts = {
            "create": 0,
            "invite": 0,
            "join": 0,
            "edit": 0,
            "consent": 0,
            "draft": 0,
            "publish": 0,
        }
        self.eligible = {
            GM_EMAIL: [],
            ALICE_EMAIL: [self._eligible_character("dossier-alice", "character-alice", "Neon Fox", "Alice Voss")],
            BOB_EMAIL: [self._eligible_character("dossier-bob", "character-bob", "Chrome Finch", "Bob Vale")],
            DEPLETED_EMAIL: [],
        }
        self.sheets = {
            "dossier-alice": self._sheet("dossier-alice", "Neon Fox", "Alice Voss"),
            "dossier-bob": self._sheet("dossier-bob", "Chrome Finch", "Bob Vale"),
        }

    @staticmethod
    def _eligible_character(dossier_id: str, character_id: str, handle: str, name: str) -> dict[str, Any]:
        return {
            "dossierId": dossier_id,
            "authorityKind": "canonical-core",
            "authoritativeCharacterId": character_id,
            "runnerHandle": handle,
            "displayName": name,
            "status": "active",
            "currentRevision": 1,
            "updatedAtUtc": "2026-07-21T00:00:00Z",
        }

    def _sheet(self, dossier_id: str, handle: str, name: str) -> dict[str, Any]:
        return {
            "campaignId": self.campaign_id,
            "dossierId": dossier_id,
            "runnerHandle": handle,
            "displayName": name,
            "status": "active",
            "role": "player",
            "canManage": False,
            "gmEditAuthorityGranted": False,
            "gmAuthorityBindingRevision": 1,
            "revision": 1,
            "ruleEnvironmentFingerprint": "sr5-fixture-20260721",
            "sections": [
                {
                    "projectionId": f"public-{dossier_id}",
                    "kind": "runner-summary",
                    "label": "Public runner summary",
                    "summary": f"Player-safe sheet for {handle}.",
                    "audience": "campaign",
                    "trustBand": "canonical",
                    "nextSafeAction": "Ask the player before changing owner-controlled details.",
                }
            ],
            "updatedAtUtc": "2026-07-21T00:00:00Z",
        }

    def install(self, context: BrowserContext) -> None:
        context.route("**/api/v1/**", self._route)

    def _route(self, route: Route) -> None:
        request = route.request
        parsed = urlsplit(request.url)
        path = parsed.path.rstrip("/") or "/"
        method = request.method.upper()
        identity = request.headers.get("x-test-identity", "")
        if identity not in self.eligible:
            self._problem(route, 401, "Fixture identity is not authenticated.")
            return

        if path == "/api/v1/antiforgery" and method == "GET":
            self._json(
                route,
                200,
                {"requestToken": f"csrf-{identity}", "headerName": "X-CSRF-TOKEN"},
                extra_headers={
                    "cache-control": "private, no-store",
                    "referrer-policy": "no-referrer",
                    "set-cookie": f"ChummerCsrfPair=csrf-{identity}; Path=/; SameSite=Strict",
                },
            )
            return

        if method not in {"GET", "HEAD", "OPTIONS"}:
            if not self._validate_mutation(request, identity):
                self._problem(route, 400, "Antiforgery token and paired cookie are required.")
                return

        read_key = (identity, path)
        if method == "GET" and read_key in self.drop_read_once:
            self.drop_read_once.remove(read_key)
            route.abort("connectionreset")
            return

        body = self._body(request)
        segments = [segment for segment in path.split("/") if segment]

        if path == "/api/v1/campaigns/eligible-characters" and method == "GET":
            self._json(route, 200, copy.deepcopy(self.eligible[identity]))
            return
        if path == "/api/v1/campaigns" and method == "GET":
            visible = self.created and (identity == GM_EMAIL or self._dossier_for(identity) in self.members)
            self._json(route, 200, [self._campaign(identity)] if visible else [])
            return
        if path == "/api/v1/campaigns" and method == "POST":
            self._create_campaign(route, identity, body)
            return
        if path == "/api/v1/campaigns/join-code/redeem" and method == "POST":
            invite = next((item for item in self.invites.values() if item["code"] == body.get("code")), None)
            self._redeem(route, identity, body, invite)
            return
        if len(segments) == 6 and segments[:4] == ["api", "v1", "campaigns", "invites"] and segments[5] == "redeem" and method == "POST":
            invite = self.invites.get(segments[4])
            if invite is None or invite["secret"] != body.get("secret"):
                invite = None
            self._redeem(route, identity, body, invite)
            return

        if len(segments) < 4 or segments[:3] != ["api", "v1", "campaigns"]:
            self._problem(route, 404, "Unknown fixture route.")
            return
        campaign_id = segments[3]
        if campaign_id != self.campaign_id or not self.created:
            self._problem(route, 404, "Campaign not found.")
            return
        if not self._has_access(identity):
            self._problem(route, 403, "Campaign access is required.")
            return

        if len(segments) == 4 and method == "GET":
            self._json(route, 200, self._campaign(identity))
            return
        if len(segments) == 5 and segments[4] == "roster" and method == "GET":
            self._json(route, 200, self._roster())
            return
        if len(segments) == 5 and segments[4] == "invites" and method == "POST":
            self._create_invite(route, identity, body)
            return
        if len(segments) >= 6 and segments[4] == "sheets":
            dossier_id = segments[5]
            if dossier_id not in self.members:
                self._problem(route, 404, "Crew sheet not found.")
                return
            if len(segments) == 6 and method == "GET":
                payload = copy.deepcopy(self.sheets[dossier_id])
                payload["canManage"] = identity == GM_EMAIL
                self._json(route, 200, payload)
                return
            if len(segments) == 6 and method == "PUT":
                self._edit_sheet(route, identity, dossier_id, body)
                return
            if len(segments) == 7 and segments[6] == "gm-authority" and method == "PUT":
                self._update_consent(route, identity, dossier_id, body)
                return
        if len(segments) >= 7 and segments[4] == "runs" and segments[6] == "runsite":
            run_id = segments[5]
            if run_id != self.run_id:
                self._problem(route, 404, "Run not found.")
                return
            if len(segments) == 8 and segments[7] == "draft" and method == "GET":
                if identity != GM_EMAIL:
                    self._problem(route, 403, "GM access is required.")
                elif self.draft is None:
                    self._problem(route, 404, "Draft not found.")
                else:
                    self._json(route, 200, copy.deepcopy(self.draft))
                return
            if len(segments) == 8 and segments[7] == "draft" and method == "PUT":
                self._save_draft(route, identity, body)
                return
            if len(segments) == 8 and segments[7] == "publish" and method == "POST":
                self._publish(route, identity, body)
                return
            if len(segments) == 7 and method == "GET":
                if self.published is None:
                    self._problem(route, 404, "Published Runsite not found.")
                else:
                    self._json(route, 200, copy.deepcopy(self.published))
                return

        self._problem(route, 404, "Unknown fixture route.")

    def _validate_mutation(self, request: Any, identity: str) -> bool:
        body = (request.post_data or "").encode("utf-8")
        headers = request.headers
        try:
            payload = json.loads(request.post_data or "{}")
        except json.JSONDecodeError:
            payload = {}
        self.unsafe_requests.append(
            {
                "identity": identity,
                "path": urlsplit(request.url).path,
                "token": headers.get("x-csrf-token", ""),
                "cookie": headers.get("cookie", ""),
                "body_bytes": str(len(body)),
                "idempotency_key": str(payload.get("idempotencyKey", "")) if isinstance(payload, dict) else "",
            }
        )
        return (
            len(body) <= 64 * 1024
            and headers.get("x-csrf-token") == f"csrf-{identity}"
            and f"ChummerCsrfPair=csrf-{identity}" in headers.get("cookie", "")
        )

    @staticmethod
    def _body(request: Any) -> dict[str, Any]:
        try:
            parsed = json.loads(request.post_data or "{}")
        except json.JSONDecodeError:
            return {}
        return parsed if isinstance(parsed, dict) else {}

    def _campaign(self, identity: str) -> dict[str, Any]:
        return {
            "campaignId": self.campaign_id,
            "groupId": "group-night-shift",
            "name": "Vienna Night Shift",
            "summary": "A deterministic multi-user fixture campaign.",
            "visibility": "campaign",
            "role": "game-master" if identity == GM_EMAIL else "player",
            "canManage": identity == GM_EMAIL,
            "crewId": "crew-night-shift",
            "runIds": [self.run_id],
            "roster": self._roster(),
            "createdAtUtc": "2026-07-21T00:00:00Z",
            "updatedAtUtc": "2026-07-21T00:00:00Z",
        }

    def _roster(self) -> list[dict[str, Any]]:
        rows = []
        for dossier_id, member in self.members.items():
            sheet = self.sheets[dossier_id]
            rows.append(
                {
                    "dossierId": dossier_id,
                    "authorityKind": "canonical-core",
                    "authoritativeCharacterId": member["characterId"],
                    "runnerHandle": sheet["runnerHandle"],
                    "displayName": sheet["displayName"],
                    "status": sheet["status"],
                    "role": "player",
                    "revision": sheet["revision"],
                    "gmEditAuthorityGranted": sheet["gmEditAuthorityGranted"],
                    "gmAuthorityBindingRevision": sheet["gmAuthorityBindingRevision"],
                    "joinedAtUtc": "2026-07-21T00:00:00Z",
                    "updatedAtUtc": "2026-07-21T00:00:00Z",
                }
            )
        return rows

    def _create_campaign(self, route: Route, identity: str, body: dict[str, Any]) -> None:
        if identity != GM_EMAIL:
            self._problem(route, 403, "Only the GM fixture can create this campaign.")
            return
        idempotency_key = str(body.get("idempotencyKey", ""))
        self.command_keys["create"].append(idempotency_key)
        request_digest = self._command_digest(body)
        if self._idempotent_replay(
            route, identity, "create-campaign", "campaign-root", idempotency_key, request_digest
        ):
            return
        self.created = True
        payload = self._campaign(identity)
        self._store_idempotent_response(
            identity, "create-campaign", "campaign-root", idempotency_key, request_digest, 201, payload
        )
        self.commit_counts["create"] += 1
        self._json_or_drop(route, 201, payload, f"create:{identity}")

    def _create_invite(self, route: Route, identity: str, body: dict[str, Any]) -> None:
        if identity != GM_EMAIL:
            self._problem(route, 403, "Only a GM can create invitations.")
            return
        idempotency_key = str(body.get("idempotencyKey", ""))
        self.command_keys["invite"].append(idempotency_key)
        request_digest = self._command_digest(body)
        if self._idempotent_replay(
            route, identity, "create-invite", self.campaign_id, idempotency_key, request_digest
        ):
            return
        self.invite_counter += 1
        invite_id = f"invite-{self.invite_counter}"
        secret = f"link-secret-{self.invite_counter}"
        code = f"JOIN-{self.invite_counter:04d}"
        self.invites[invite_id] = {"secret": secret, "code": code}
        payload = {
            "inviteId": invite_id,
            "campaignId": self.campaign_id,
            "joinPath": f"/join/campaign/{invite_id}#secret={secret}",
            "linkSecret": secret,
            "shortCode": code,
            "expiresAtUtc": "2026-07-22T00:00:00Z",
            "maxUses": 1,
            "createdAtUtc": "2026-07-21T00:00:00Z",
        }
        self._store_idempotent_response(
            identity, "create-invite", self.campaign_id, idempotency_key, request_digest, 201, payload
        )
        self.commit_counts["invite"] += 1
        self._json_or_drop(route, 201, payload, f"invite:{self.campaign_id}")

    def _redeem(self, route: Route, identity: str, body: dict[str, Any], invite: dict[str, str] | None) -> None:
        dossier_id = self._dossier_for(identity)
        if invite is None or dossier_id is None or body.get("dossierId") != dossier_id:
            self._problem(route, 404, "Invitation is invalid or not available to this character.")
            return
        idempotency_key = str(body.get("idempotencyKey", ""))
        self.command_keys["join"].append(idempotency_key)
        request_digest = self._command_digest(body)
        replay = self._idempotent_replay(
            route, identity, "join", self.campaign_id, idempotency_key, request_digest
        )
        if replay:
            return
        if body.get("expectedCharacterRevision") != self.sheets[dossier_id]["revision"]:
            self._problem(route, 409, "Character revision changed.")
            return
        already_joined = dossier_id in self.members
        self.members[dossier_id] = {
            "identity": identity,
            "characterId": body.get("authoritativeCharacterId"),
        }
        self.sheets[dossier_id]["gmEditAuthorityGranted"] = body.get("grantGmEditAuthority") is True
        payload = {
            "campaignId": self.campaign_id,
            "dossierId": dossier_id,
            "crewId": "crew-night-shift",
            "role": "player",
            "binding": {
                "bindingId": f"binding-{dossier_id}",
                "campaignId": self.campaign_id,
                "dossierId": dossier_id,
                "authorityKind": "canonical-core",
                "authoritativeCharacterId": body.get("authoritativeCharacterId"),
                "bindingRevision": 1,
                "currentRevision": self.sheets[dossier_id]["revision"],
                "gmAuthorityRole": "delegated" if body.get("grantGmEditAuthority") else "none",
                "grantedAtUtc": "2026-07-21T00:00:00Z",
            },
            "alreadyJoined": already_joined,
            "joinedAtUtc": "2026-07-21T00:00:00Z",
        }
        self._store_idempotent_response(
            identity, "join", self.campaign_id, idempotency_key, request_digest, 200, payload
        )
        self.commit_counts["join"] += 1
        self._json_or_drop(route, 200, payload, f"join:{identity}")

    def _edit_sheet(self, route: Route, identity: str, dossier_id: str, body: dict[str, Any]) -> None:
        sheet = self.sheets[dossier_id]
        if identity != GM_EMAIL or not sheet["gmEditAuthorityGranted"]:
            self._problem(route, 403, "GM edit consent is required.")
            return
        if body.get("sections") is not None or body.get("status") != sheet["status"]:
            self._problem(route, 400, "Only canonical delegated profile fields may change.")
            return
        idempotency_key = str(body.get("idempotencyKey", ""))
        self.command_keys["edit"].append(idempotency_key)
        request_digest = self._command_digest(body)
        scope = f"{self.campaign_id}/{dossier_id}"
        if self._idempotent_replay(route, identity, "edit-sheet", scope, idempotency_key, request_digest):
            return
        if self.force_edit_conflict_once:
            self.force_edit_conflict_once = False
            sheet["revision"] += 1
            self._problem(route, 409, "Owner revision advanced before the GM edit.")
            return
        if body.get("expectedRevision") != sheet["revision"]:
            self._problem(route, 409, "Character revision changed.")
            return
        previous = sheet["revision"]
        sheet["runnerHandle"] = str(body.get("runnerHandle", ""))
        sheet["displayName"] = str(body.get("displayName", ""))
        sheet["revision"] += 1
        payload = {
            "receiptId": "gm-edit-receipt",
            "campaignId": self.campaign_id,
            "dossierId": dossier_id,
            "previousRevision": previous,
            "revision": sheet["revision"],
            "currentRevision": sheet["revision"],
            "idempotencyKey": idempotency_key,
            "reason": body.get("reason"),
            "editedByUserId": "gm-fixture",
            "beforeSha256": "a" * 64,
            "afterSha256": "b" * 64,
            "editedAtUtc": "2026-07-21T00:00:00Z",
        }
        self._store_idempotent_response(
            identity, "edit-sheet", scope, idempotency_key, request_digest, 200, payload
        )
        self.commit_counts["edit"] += 1
        self._json_or_drop(route, 200, payload, f"edit:{dossier_id}")

    def _update_consent(self, route: Route, identity: str, dossier_id: str, body: dict[str, Any]) -> None:
        if self._dossier_for(identity) != dossier_id:
            self._problem(route, 403, "Only the character owner can update consent.")
            return
        sheet = self.sheets[dossier_id]
        idempotency_key = str(body.get("idempotencyKey", ""))
        self.command_keys["consent"].append(idempotency_key)
        request_digest = self._command_digest(body)
        scope = f"{self.campaign_id}/{dossier_id}"
        if self._idempotent_replay(route, identity, "update-consent", scope, idempotency_key, request_digest):
            return
        if body.get("expectedBindingRevision") != sheet["gmAuthorityBindingRevision"]:
            self._problem(route, 409, "Consent binding changed.")
            return
        previous = sheet["gmAuthorityBindingRevision"]
        sheet["gmAuthorityBindingRevision"] += 1
        sheet["gmEditAuthorityGranted"] = body.get("grantGmEditAuthority") is True
        payload = {
            "receiptId": "consent-receipt",
            "campaignId": self.campaign_id,
            "dossierId": dossier_id,
            "previousBindingRevision": previous,
            "bindingRevision": sheet["gmAuthorityBindingRevision"],
            "currentCharacterRevision": sheet["revision"],
            "gmEditAuthorityGranted": sheet["gmEditAuthorityGranted"],
            "changed": True,
            "idempotencyKey": idempotency_key,
            "reason": body.get("reason"),
            "changedAtUtc": "2026-07-21T00:00:00Z",
        }
        self._store_idempotent_response(
            identity, "update-consent", scope, idempotency_key, request_digest, 200, payload
        )
        self.commit_counts["consent"] += 1
        self._json_or_drop(route, 200, payload, f"consent:{dossier_id}")

    def _save_draft(self, route: Route, identity: str, body: dict[str, Any]) -> None:
        if identity != GM_EMAIL:
            self._problem(route, 403, "GM access is required.")
            return
        idempotency_key = str(body.get("idempotencyKey", ""))
        self.command_keys["draft"].append(idempotency_key)
        request_digest = self._command_digest(body)
        scope = f"{self.campaign_id}/{self.run_id}"
        if self._idempotent_replay(
            route, identity, "save-runsite-draft", scope, idempotency_key, request_digest
        ):
            return
        current_revision = 0 if self.draft is None else int(self.draft["revision"])
        if body.get("expectedRevision") != current_revision:
            self._problem(route, 409, "Runsite draft changed.")
            return
        self.draft = {
            "campaignId": self.campaign_id,
            "runId": self.run_id,
            "revision": current_revision + 1,
            "title": body.get("title"),
            "summary": body.get("summary"),
            "playerSections": body.get("playerSections", []),
            "gmNotes": body.get("gmNotes"),
            "publishedRevision": None if self.published is None else self.published["revision"],
            "updatedAtUtc": "2026-07-21T00:00:00Z",
            "publishedAtUtc": None,
        }
        payload = copy.deepcopy(self.draft)
        self._store_idempotent_response(
            identity, "save-runsite-draft", scope, idempotency_key, request_digest, 200, payload
        )
        self.commit_counts["draft"] += 1
        self._json_or_drop(route, 200, payload, f"draft:{self.run_id}")

    def _publish(self, route: Route, identity: str, body: dict[str, Any]) -> None:
        if identity != GM_EMAIL or self.draft is None:
            self._problem(route, 403, "A GM draft is required.")
            return
        idempotency_key = str(body.get("idempotencyKey", ""))
        self.command_keys["publish"].append(idempotency_key)
        request_digest = self._command_digest(body)
        scope = f"{self.campaign_id}/{self.run_id}"
        if self._idempotent_replay(
            route, identity, "publish-runsite", scope, idempotency_key, request_digest
        ):
            return
        if body.get("expectedRevision") != self.draft["revision"]:
            self._problem(route, 409, "Runsite draft changed.")
            return
        self.published = {
            "campaignId": self.campaign_id,
            "runId": self.run_id,
            "revision": self.draft["revision"],
            "title": self.draft["title"],
            "summary": self.draft["summary"],
            "sections": copy.deepcopy(self.draft["playerSections"]),
            "publishedAtUtc": "2026-07-21T00:00:00Z",
        }
        self._store_idempotent_response(
            identity, "publish-runsite", scope, idempotency_key, request_digest, 200, self.published
        )
        self.commit_counts["publish"] += 1
        self._json_or_drop(route, 200, copy.deepcopy(self.published), f"publish:{self.run_id}")

    def _idempotent_replay(
        self,
        route: Route,
        identity: str,
        operation: str,
        scope: str,
        idempotency_key: str,
        request_digest: str,
    ) -> bool:
        if not idempotency_key:
            self._problem(route, 400, "Idempotency key is required.")
            return True
        existing = self.idempotent_responses.get((identity, operation, scope, idempotency_key))
        if existing is None:
            return False
        existing_digest, status, payload = existing
        if existing_digest != request_digest:
            self._problem(route, 409, "Idempotency key was reused with a different command.")
            return True
        self._json(route, status, copy.deepcopy(payload))
        return True

    @staticmethod
    def _command_digest(body: dict[str, Any]) -> str:
        command = {key: value for key, value in body.items() if key != "idempotencyKey"}
        canonical = json.dumps(command, sort_keys=True, separators=(",", ":")).encode("utf-8")
        return hashlib.sha256(canonical).hexdigest()

    def _store_idempotent_response(
        self,
        identity: str,
        operation: str,
        scope: str,
        idempotency_key: str,
        request_digest: str,
        status: int,
        payload: dict[str, Any],
    ) -> None:
        self.idempotent_responses[(identity, operation, scope, idempotency_key)] = (
            request_digest,
            status,
            copy.deepcopy(payload),
        )

    def _json_or_drop(self, route: Route, status: int, payload: Any, drop_key: str) -> None:
        if drop_key in self.drop_after_commit_once:
            self.drop_after_commit_once.remove(drop_key)
            route.abort("connectionreset")
            return
        self._json(route, status, payload)

    def _has_access(self, identity: str) -> bool:
        return identity == GM_EMAIL or self._dossier_for(identity) in self.members

    def _dossier_for(self, identity: str) -> str | None:
        characters = self.eligible.get(identity, [])
        return characters[0]["dossierId"] if characters else None

    @staticmethod
    def _json(
        route: Route,
        status: int,
        payload: Any,
        *,
        extra_headers: dict[str, str] | None = None,
    ) -> None:
        headers = {"cache-control": "private, no-store", **(extra_headers or {})}
        route.fulfill(
            status=status,
            content_type="application/json; charset=utf-8",
            headers=headers,
            body=json.dumps(payload),
        )

    @classmethod
    def _problem(cls, route: Route, status: int, detail: str) -> None:
        cls._json(route, status, {"title": "Fixture request failed", "status": status, "detail": detail})


def _new_context(browser: Browser, identity: str) -> BrowserContext:
    assert identity.endswith("@girschele.com")
    return browser.new_context(
        viewport={"width": 390, "height": 844},
        extra_http_headers={"X-Test-Identity": identity},
        service_workers="block",
    )


def _send_json_mutation(
    page: Page,
    path: str,
    method: str,
    identity: str,
    body: dict[str, Any],
) -> int:
    return int(
        page.evaluate(
            """
            async ({ path, method, requestToken, body }) => {
              const response = await fetch(path, {
                method,
                credentials: "include",
                cache: "no-store",
                redirect: "error",
                headers: {
                  accept: "application/json",
                  "content-type": "application/json",
                  "X-CSRF-TOKEN": requestToken
                },
                body: JSON.stringify(body)
              });
              await response.text();
              return response.status;
            }
            """,
            {
                "path": path,
                "method": method,
                "requestToken": f"csrf-{identity}",
                "body": body,
            },
        )
    )


def _open_campaign(page: Page, base_url: str, campaign_id: str) -> None:
    page.goto(f"{base_url}/mobile/campaigns/{campaign_id}", wait_until="domcontentloaded")
    page.locator("#campaign-workspace:not([hidden])").wait_for()
    page.locator("#campaign-roster .campaign-member-card").first.wait_for()


def test_multi_identity_campaign_join_sheet_edit_and_runsite_journey(campaign_host: str) -> None:
    hub = FakeCampaignHub()
    hub.drop_after_commit_once.update(
        {
            f"create:{GM_EMAIL}",
            f"invite:{hub.campaign_id}",
            f"join:{ALICE_EMAIL}",
            "edit:dossier-alice",
            "consent:dossier-alice",
            f"draft:{hub.run_id}",
            f"publish:{hub.run_id}",
        }
    )
    with sync_playwright() as playwright:
        browser = playwright.chromium.launch(headless=True)
        contexts: list[BrowserContext] = []
        try:
            gm_context = _new_context(browser, GM_EMAIL)
            contexts.append(gm_context)
            hub.install(gm_context)
            gm = gm_context.new_page()
            gm_document = gm.goto(f"{campaign_host}/mobile/campaigns", wait_until="domcontentloaded")
            assert gm_document is not None
            document_headers = gm_document.headers
            assert "no-store" in document_headers["cache-control"]
            assert document_headers["referrer-policy"] == "no-referrer"
            assert "connect-src 'self'" in document_headers["content-security-policy"]
            gm.locator("#campaign-create-form").wait_for()
            gm.locator("#campaign-name").fill("Vienna Night Shift")
            gm.locator("#campaign-summary").fill("A deterministic multi-user fixture campaign.")
            hub.drop_read_once.add((GM_EMAIL, "/api/v1/campaigns"))
            gm.locator("#campaign-create-form button[type=submit]").click()
            gm.wait_for_function(
                "() => document.querySelector('#campaign-status').textContent.includes('creation outcome is not confirmed')"
            )
            gm.locator("#campaign-create-form button[type=submit]").click()
            gm.locator("#campaign-workspace:not([hidden])").wait_for()
            assert gm.locator("#campaign-role").inner_text() == "Game Master"
            create_keys = [
                item["idempotency_key"]
                for item in hub.unsafe_requests
                if item["identity"] == GM_EMAIL and item["path"] == "/api/v1/campaigns"
            ]
            assert len(create_keys) == 2
            assert create_keys[0] == create_keys[1]
            assert hub.commit_counts["create"] == 1
            assert (
                _send_json_mutation(
                    gm,
                    "/api/v1/campaigns",
                    "POST",
                    GM_EMAIL,
                    {
                        "name": "Different campaign",
                        "summary": "A deterministic multi-user fixture campaign.",
                        "visibility": "campaign",
                        "initialRunTitle": "First Run",
                        "idempotencyKey": create_keys[0],
                    },
                )
                == 409
            )
            assert hub.commit_counts["create"] == 1

            gm.locator("#campaign-create-invite").click()
            gm.wait_for_function(
                "() => document.querySelector('#campaign-status').textContent.includes('invitation outcome is not confirmed')"
            )
            gm.locator("#campaign-create-invite").click()
            gm.locator("#campaign-invite-secret:not([hidden])").wait_for()
            alice_link = gm.locator("#campaign-invite-link").input_value()
            assert alice_link.startswith(f"{campaign_host}/join/campaign/invite-1#secret=")
            invite_path = f"/api/v1/campaigns/{hub.campaign_id}/invites"
            first_invite_keys = [
                item["idempotency_key"]
                for item in hub.unsafe_requests
                if item["identity"] == GM_EMAIL and item["path"] == invite_path
            ]
            assert len(first_invite_keys) == 2
            assert first_invite_keys[0] == first_invite_keys[1]
            assert hub.commit_counts["invite"] == 1
            assert (
                _send_json_mutation(
                    gm,
                    invite_path,
                    "POST",
                    GM_EMAIL,
                    {
                        "expiresInMinutes": 1440,
                        "maxUses": 2,
                        "idempotencyKey": first_invite_keys[0],
                    },
                )
                == 409
            )
            assert hub.commit_counts["invite"] == 1

            gm.locator("#campaign-create-invite").click()
            gm.locator("#campaign-invite-code").wait_for()
            gm.wait_for_function("() => document.querySelector('#campaign-invite-code').value === 'JOIN-0002'")
            bob_code = gm.locator("#campaign-invite-code").input_value()
            assert hub.commit_counts["invite"] == 2

            alice_context = _new_context(browser, ALICE_EMAIL)
            contexts.append(alice_context)
            hub.install(alice_context)
            alice = alice_context.new_page()
            alice.goto(alice_link, wait_until="domcontentloaded")
            alice.locator("#campaign-join-secret-copy:not([hidden])").wait_for()
            assert "#" not in alice.url
            alice.locator("#campaign-grant-gm").check()
            hub.drop_read_once.add((ALICE_EMAIL, "/api/v1/campaigns"))
            alice.locator("#campaign-join-submit").click()
            alice.wait_for_function("() => document.querySelector('#campaign-status').textContent.includes('outcome is not confirmed')")
            alice.locator("#campaign-join-submit").click()
            alice.locator("#campaign-workspace:not([hidden])").wait_for()
            assert alice.locator("#campaign-role").inner_text() == "player"
            alice_join_keys = [
                item["idempotency_key"]
                for item in hub.unsafe_requests
                if item["identity"] == ALICE_EMAIL and item["path"].endswith("/redeem")
            ]
            assert len(alice_join_keys) == 2
            assert alice_join_keys[0] == alice_join_keys[1]
            assert hub.commit_counts["join"] == 1

            bob_context = _new_context(browser, BOB_EMAIL)
            contexts.append(bob_context)
            hub.install(bob_context)
            bob = bob_context.new_page()
            bob.goto(f"{campaign_host}/mobile/campaigns", wait_until="domcontentloaded")
            bob.locator("#campaign-code").fill(bob_code)
            bob.locator("#campaign-join-submit").click()
            bob.locator("#campaign-workspace:not([hidden])").wait_for()

            depleted_context = _new_context(browser, DEPLETED_EMAIL)
            contexts.append(depleted_context)
            hub.install(depleted_context)
            depleted = depleted_context.new_page()
            depleted.goto(f"{campaign_host}/mobile/campaigns", wait_until="domcontentloaded")
            depleted.locator("#campaign-no-characters:not([hidden])").wait_for()
            assert "account is still usable" in depleted.locator("#campaign-no-characters").inner_text()
            assert depleted.locator("#campaign-join-submit").is_disabled()

            _open_campaign(alice, campaign_host, hub.campaign_id)
            assert alice.locator("#campaign-roster .campaign-member-card").count() == 2
            alice.locator("#campaign-roster .campaign-member-card", has_text="Chrome Finch").locator("button").click()
            alice.locator("#campaign-sheet-card:not([hidden])").wait_for()
            assert "Player-safe sheet for Chrome Finch" in alice.locator("#campaign-sheet-sections").inner_text()
            assert alice.locator("#campaign-gm-edit-form").is_hidden()

            _open_campaign(bob, campaign_host, hub.campaign_id)
            bob.locator("#campaign-roster .campaign-member-card", has_text="Neon Fox").locator("button").click()
            bob.locator("#campaign-sheet-card:not([hidden])").wait_for()
            assert "Player-safe sheet for Neon Fox" in bob.locator("#campaign-sheet-sections").inner_text()

            _open_campaign(gm, campaign_host, hub.campaign_id)
            gm.locator("#campaign-roster .campaign-member-card", has_text="Neon Fox").locator("button").click()
            gm.locator("#campaign-gm-edit-form:not([hidden])").wait_for()
            gm.locator("#campaign-sheet-display-name").fill("Alice Voss · updated")
            gm.locator("#campaign-gm-edit-form button[type=submit]").click()
            gm.wait_for_function("() => document.querySelector('#campaign-status').textContent.includes('changed elsewhere')")
            assert "revision 2" in gm.locator("#campaign-sheet-revision").inner_text().lower()

            gm.locator("#campaign-sheet-display-name").fill("Alice Voss · updated")
            gm.locator("#campaign-gm-edit-form button[type=submit]").click()
            gm.wait_for_function("() => document.querySelector('#campaign-status').textContent.includes('canonical sheet confirms')")
            assert hub.sheets["dossier-alice"]["displayName"] == "Alice Voss · updated"
            assert hub.commit_counts["edit"] == 1

            _open_campaign(alice, campaign_host, hub.campaign_id)
            alice.locator("#campaign-roster .campaign-member-card", has_text="Neon Fox").locator("button").click()
            alice.locator("#campaign-consent-form:not([hidden])").wait_for()
            alice.locator("#campaign-consent-toggle").uncheck()
            alice.locator("#campaign-consent-form button[type=submit]").click()
            alice.wait_for_function("() => document.querySelector('#campaign-status').textContent.includes('authoritative binding confirms')")
            assert hub.commit_counts["consent"] == 1
            assert hub.sheets["dossier-alice"]["gmEditAuthorityGranted"] is False

            _open_campaign(gm, campaign_host, hub.campaign_id)
            gm.locator("#campaign-roster .campaign-member-card", has_text="Neon Fox").locator("button").click()
            gm.wait_for_function("() => document.querySelector('#campaign-sheet-name').textContent.includes('Neon Fox')")
            assert gm.locator("#campaign-gm-edit-form").is_hidden()
            assert "not granted" in gm.locator("#campaign-gm-edit-boundary").inner_text()

            gm.locator("#campaign-roster .campaign-member-card", has_text="Chrome Finch").locator("button").click()
            gm.wait_for_function("() => document.querySelector('#campaign-sheet-name').textContent.includes('Chrome Finch')")
            assert gm.locator("#campaign-gm-edit-form").is_hidden()
            assert "not granted" in gm.locator("#campaign-gm-edit-boundary").inner_text()

            gm.locator("#campaign-runsite-title").fill("Dockyard relay")
            gm.locator("#campaign-runsite-summary").fill("Meet at the south crane after midnight.")
            gm.locator("#campaign-runsite-sections").fill("South crane | Public rendezvous point\nWarehouse 3 | Avoid the lit entrance")
            gm.locator("#campaign-runsite-gm-notes").fill("SECRET: opposition waits in the north office")
            draft_path = f"/api/v1/campaigns/{hub.campaign_id}/runs/{hub.run_id}/runsite/draft"
            hub.drop_read_once.add((GM_EMAIL, draft_path))
            gm.locator("#campaign-runsite-form button[type=submit]").click()
            gm.wait_for_function(
                "() => document.querySelector('#campaign-status').textContent.includes('draft outcome is not confirmed')"
            )
            gm.locator("#campaign-runsite-form button[type=submit]").click()
            gm.wait_for_function("() => document.querySelector('#campaign-status').textContent.includes('Runsite draft saved')")
            draft_keys = [
                item["idempotency_key"]
                for item in hub.unsafe_requests
                if item["identity"] == GM_EMAIL and item["path"] == draft_path
            ]
            assert len(draft_keys) == 2
            assert draft_keys[0] == draft_keys[1]
            assert hub.commit_counts["draft"] == 1
            assert (
                _send_json_mutation(
                    gm,
                    draft_path,
                    "PUT",
                    GM_EMAIL,
                    {
                        "expectedRevision": 0,
                        "title": "Dockyard relay",
                        "summary": "Changed after the command committed.",
                        "playerSections": [
                            {"heading": "South crane", "body": "Public rendezvous point"},
                            {"heading": "Warehouse 3", "body": "Avoid the lit entrance"},
                        ],
                        "gmNotes": "SECRET: opposition waits in the north office",
                        "idempotencyKey": draft_keys[0],
                    },
                )
                == 409
            )
            assert hub.commit_counts["draft"] == 1
            published_path = f"/api/v1/campaigns/{hub.campaign_id}/runs/{hub.run_id}/runsite"
            hub.drop_read_once.add((GM_EMAIL, published_path))
            gm.locator("#campaign-runsite-publish").click()
            gm.wait_for_function(
                "() => document.querySelector('#campaign-status').textContent.includes('publication outcome is not confirmed')"
            )
            gm.locator("#campaign-runsite-publish").click()
            gm.wait_for_function("() => document.querySelector('#campaign-status').textContent.includes('is published')")
            publish_mutation_path = f"{published_path}/publish"
            publish_keys = [
                item["idempotency_key"]
                for item in hub.unsafe_requests
                if item["identity"] == GM_EMAIL and item["path"] == publish_mutation_path
            ]
            assert len(publish_keys) == 2
            assert publish_keys[0] == publish_keys[1]
            assert hub.commit_counts["publish"] == 1
            assert (
                _send_json_mutation(
                    gm,
                    publish_mutation_path,
                    "POST",
                    GM_EMAIL,
                    {
                        "expectedRevision": int(hub.draft["revision"]) + 1,
                        "idempotencyKey": publish_keys[0],
                    },
                )
                == 409
            )
            assert hub.commit_counts["publish"] == 1

            _open_campaign(alice, campaign_host, hub.campaign_id)
            alice.locator("#campaign-runsite-player:not([hidden])").wait_for()
            assert alice.locator("#campaign-runsite-player-title").inner_text() == "Dockyard relay"
            assert "Public rendezvous point" in alice.locator("#campaign-runsite-player-sections").inner_text()
            assert "opposition waits" not in alice.locator("body").inner_text()

            alice.locator("#campaign-roster .campaign-member-card", has_text="Neon Fox").locator("button").click()
            alice.wait_for_function("() => document.querySelector('#campaign-sheet-status').textContent.length > 0")
            assert "Alice Voss · updated" in alice.locator("#campaign-sheet-card").inner_text()
            assert alice.evaluate("() => localStorage.length") == 0

            assert hub.unsafe_requests
            assert {item["identity"] for item in hub.unsafe_requests} >= {GM_EMAIL, ALICE_EMAIL, BOB_EMAIL}
            assert all(item["token"] == f"csrf-{item['identity']}" for item in hub.unsafe_requests)
            assert all(f"ChummerCsrfPair=csrf-{item['identity']}" in item["cookie"] for item in hub.unsafe_requests)
            assert all(int(item["body_bytes"]) <= 64 * 1024 for item in hub.unsafe_requests)
            assert all("link-secret" not in json.dumps(item) for item in hub.unsafe_requests)
            assert gm.evaluate("() => localStorage.length") == 0
        finally:
            for context in reversed(contexts):
                context.close()
            browser.close()
