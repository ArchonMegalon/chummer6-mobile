# Standalone mobile package bootstrap

`bash scripts/ai/verify.sh` keeps the full local product suite: all six managed
projects, the regression executable and the browser checks. In the default
`slice` mode, a checkout without explicit package inputs builds genuine public
owner projects using `eng/package-plane.lock.json`. Published NuGet artifacts
are not a prerequisite for this route.

The source bootstrap requires Git, Python 3, SDK **10.0.103**, access to the
three public GitHub repositories below, and NuGet.org for SDK dependencies.
The remaining default checks also need ripgrep, PyYAML and Playwright with its
matching browser. An unavailable source, mismatched digest or failed product
check fails verification. There is no default scaffold fallback.

| Package | Public owner | Exact source commit |
| --- | --- | --- |
| Chummer.Engine.Contracts | [Core](https://github.com/ArchonMegalon/chummer6-core/tree/5160e78a60bcefd952e8720aae6032a127c3a755) | `5160e78a60bcefd952e8720aae6032a127c3a755` |
| Chummer.Campaign.Contracts | [Hub](https://github.com/ArchonMegalon/chummer6-hub/tree/4f7c98ad4407e860ed68b88d0fc6d15ba2bd9b2e) | `4f7c98ad4407e860ed68b88d0fc6d15ba2bd9b2e` |
| Chummer.Control.Contracts | Hub | `4f7c98ad4407e860ed68b88d0fc6d15ba2bd9b2e` |
| Chummer.Play.Contracts | Hub | `4f7c98ad4407e860ed68b88d0fc6d15ba2bd9b2e` |
| Chummer.Ui.Kit | [UI Kit](https://github.com/ArchonMegalon/chummer6-ui-kit/tree/20028e1f0984521450e6af7d891341ec8bdce150) | `20028e1f0984521450e6af7d891341ec8bdce150` |

The lock records actual owner project paths and their SHA-256 digests.
Hub's selected source declares the same Engine source candidate as its default
dependency. Campaign depends on Engine; Control, Play and UI Kit have no
package dependencies. Control remains a bootstrap inventory entry, not a new
mobile product reference.

Source checkout uses a filtered fetch and includes every locked project for
each owner, plus root and ancestor build metadata. Unrelated applications and
media are not checked out; commit, origin and project integrity checks remain.
The private checkout preserves Git blob bytes without platform EOL conversion,
so project digests do not depend on workstation or repository line-ending rules.

All five local packages and the four product consumer pins use
`0.0.0-packageplane.mobile.20261110.1`. This is a new, unpublished bootstrap
recipe version, not a claim that an owner published that version. It avoids
reusing the old `0.1.0-preview` scaffold identity or relabeling an existing
release. Owner assembly names, package identities, licenses and source remain
unchanged. No DTO source is copied into mobile.

The implementation adapts Core's public owner-source bootstrap: fetch each
exact commit into an isolated workspace, verify the checkout and project
digest, then pack the actual projects serially. Campaign restores the Engine
package produced earlier in the same staged feed. NuGet source mapping reserves
`Chummer.*` for that feed; public NuGet.org supplies only other dependencies.
Owner compatibility-tree references and additional restore feeds are disabled.

The helper canonicalizes package ZIP metadata and validates each nuspec's ID,
version, repository commit, license, internal dependency graph and expected
assembly. Only after all five packages validate does it promote a feed and
write `chummer-owner-contracts.inventory.json`, containing measured package
digests and sizes, the source-lock digest and bootstrap-recipe digest. This
local inventory is not a signed publication or release attestation.

The default feed is under `.artifacts/nuget-local/source-<lock-sha256>`; source
checkouts and temporary restore state are under `.tmp/ai/package-plane`.
`CHUMMER_PACKAGE_PLANE_LOCAL_FEED` relocates the feed parent. The package helper
holds its existing lock through preparation and the consumer command. It
reuses a feed only after inventory validation. Corrupt inventories fail closed.
Restore-capable calls remove only mismatched cached versions of these five
packages. Skipped restores require intact inventory, cache and matching
target provenance; they never build missing dependencies.

`CHUMMER_PUBLISHED_FEED_SOURCES` still selects an explicit feed, and all five
`CHUMMER_PACKAGE_PLANE_*_PROJECT` inputs still select explicit development
owners. Partial owner inputs fail rather than being mixed with automatic
sources. Version overrides that disagree with the source lock are rejected in
the automatic route. Only explicitly selected `scaffold` mode permits the
legacy empty scaffolds, which cannot establish product compatibility.

Integration and release verification retain their explicit external inputs
and same-run no-stub provenance requirements. The automatic source route is
limited to `slice`; success there remains ineligible for release evidence.
Focused fixture tests exercise control flow and tamper rejection without
compiling. Actual compatibility requires the unchanged full default verifier
with real packages; fixture success alone does not establish it.
