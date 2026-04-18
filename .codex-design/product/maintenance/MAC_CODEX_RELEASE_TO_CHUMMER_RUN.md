# Mac Codex Release To chummer.run

Purpose: let a Codex session running on a Mac build a public-ready desktop artifact, prove it, and promote it onto the live `chummer.run` downloads shelf through the authenticated HTTP upload endpoint instead of manual server file copies.

If you want the zero-touch signed-in path, open `https://chummer.run/downloads/release-upload` in the browser first, copy the generated one-liner, and paste that into the Mac shell. The signed-in handoff mints a short-lived upload ticket, always serves the current hosted bootstrap, and the upload response prints the install dispatch URL plus the claim code for the promoted artifact.

## One command

Public bootstrap:

```bash
bash <(curl -fsSL https://chummer.run/artifacts/mac-codex-release-pipeline/bootstrap.sh)
```

Repo-local checkout fallback:

```bash
repo_root="$(git rev-parse --show-toplevel)"
bash "$repo_root/chummer6-hub/scripts/run-mac-release-bootstrap.sh"
```

Do not hardcode `/docker/chummercomplete/.../bootstrap.sh` on the Mac host. That path is for provisioned Linux control environments, not a normal Mac release workstation.

The bootstrap is the public deep link. It now:

1. clones or updates the required repos
2. builds the mac desktop head
3. packages a `.dmg`
4. codesigns, notarizes, staples, and validates it
5. runs startup smoke
6. generates both public release manifests
7. writes `release-evidence/public-promotion.json`
8. uploads the full bundle to `https://chummer.run/api/internal/releases/bundles`
9. verifies the promoted live shelf and prints the resulting `/downloads/install/{artifactId}` handoff URL
10. prints signed-in claim codes when it was launched from the signed-in release-upload handoff

## Minimum environment variables

```bash
export CHUMMER_APP_SIGN_IDENTITY="Developer ID Application: YOUR ORG (TEAMID)"
export CHUMMER_NOTARY_PROFILE="chummer-notary"
export CHUMMER_RELEASE_UPLOAD_TOKEN="..."
```

Optional overrides:

```bash
export CHUMMER_RELEASE_UPLOAD_URL="https://chummer.run/api/internal/releases/bundles"
export CHUMMER_PORTAL_DOWNLOADS_VERIFY_URL="https://chummer.run/downloads/releases.json"
export CHUMMER_RELEASE_CHANNEL="preview"
export CHUMMER_RELEASE_APP="avalonia"
export CHUMMER_RELEASE_RID="osx-arm64"
export CHUMMER_UI_REF="fleet/ui"
export CHUMMER_CORE_REF="fleet/core"
export CHUMMER_HUB_REF="main"
export CHUMMER_UI_KIT_REF="fleet/ui-kit"
export CHUMMER_HUB_REGISTRY_REF="fleet/hub-registry"
export CHUMMER_LEGACY_REF="Docker"
```

## Promotion gate

The upload endpoint may merge platform slices independently, but it only makes an installer public when the bundle includes:

1. the artifact file under `files/`
2. `releases.json`
3. `RELEASE_CHANNEL.generated.json`
4. startup-smoke receipts matching the uploaded digest
5. `release-evidence/public-promotion.json`

For macOS that evidence must prove:

1. `promotionStatus=pass`
2. `startupSmokeStatus=pass`
3. `signingStatus=pass`
4. `notarizationStatus=pass`

For Windows promotion the same endpoint is valid, but the evidence must prove startup smoke and signing before the public shelf can expose the installer.

## Public result

Once the upload succeeds:

1. `https://chummer.run/downloads/releases.json` contains the promoted artifact while preserving the other current shelf entries
2. the direct file URL resolves under `/downloads/files/...`
3. the signed-in claim-code handoff is live at `/downloads/install/{artifactId}`
4. the desktop app also ships `Samples/Legacy/Soma-Career.chum5`, bundled from the legacy Chummer5 test fixtures for a real completed-runner import check
