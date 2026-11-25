# Private companion replay identities

The normal private turn companion submits `batchId` and `identifiedEvents`
(`eventId`, `text`) to its existing replay route. IDs are generated once using
browser cryptographic randomness. A pending batch is frozen, retained for an
unconfirmed retry, and limited to 100 events. Actions added during that request
remain queued. A reply settles only IDs from that submitted batch, with a
matching `acknowledgedBatchId`, `retrySafe: true`, and explicit
`acknowledgedEventIds`. Counts alone cannot settle an identified batch.

The server stores the ledger and scoped batch receipts in one atomic value at
the existing ledger key. Replay uses the existing session mutation semaphore,
including resume initialization. The identity scope is the existing trusted
session / role / device route; a device ID is not authentication. Matching
retries return the original identities without advancing the sequence or
appending again. Reusing an ID with different content is rejected. Different
IDs with identical text remain distinct actions. Receipts survive pending-queue
acknowledgement, legacy appends, and service restart.

Server settlement is durable before the POST response. The client settles its
open-tab queue before the separate device-status GET, whose failure is reported
as an observation failure. The existing privacy policy still forbids durable
private browser storage: `persistClientState` remains a no-op, private queues
are discarded on page close/reload, and legacy browser keys are still purged.
This change does not claim recovery of unsubmitted events after a browser
restart. No public bridge, install shell, service worker, or access gate is
opened by this protocol.

Pre-upgrade server ledgers remain readable. Their text entries receive no
invented IDs and are preserved on the first atomic migration to the versioned
ledger wrapper. Older clients may still submit `events: string[]`; replies
explicitly have `retrySafe: false`, no acknowledged identities, and a warning
against automatic retry after a lost reply. Text-only actions cannot be
deduplicated reliably. The new client retains any identityless in-memory
receipts for owner reconciliation instead of assigning fresh IDs to possibly
submitted actions. Malformed or unknown stored replay formats fail closed and
are retained for recovery. Receipts are retained for the ledger's lifetime;
an older server binary cannot safely read the new wrapper, so rollback must
retain the upgraded reader or use an explicitly reviewed recovery procedure.

## Local verification and controller handoff

Executed in the scoped worker:

```sh
node --test --experimental-test-isolation=none tests/mobile_turn_companion_replay.test.cjs
node --check src/Chummer.Play.Web/wwwroot/mobile-turn-companion.js
node --check chummer6-mobile.js
python3 -m pytest -q tests/test_mobile_private_data_lifecycle_contract.py tests/test_pwa_install_identity_contract.py
git diff --check
```

Six JavaScript regressions pass. The two existing Python modules pass. The
Node isolation option exposes individual case output in this worker; the default
`node --test tests/mobile_turn_companion_replay.test.cjs` invocation also passes.
There were no existing JavaScript test files in this checkout; this adds a
Node regression file around the actual private companion entry point.

The added `CompanionReplayChecks` cases and the existing C# ledger checks are
**not executed by this worker**. In the controller's serialized local package
slot, retain its approved package-feed environment and use the existing runner:

```sh
bash scripts/ai/with-package-plane.sh build src/Chummer.Play.RegressionChecks/Chummer.Play.RegressionChecks.csproj --nologo
bash scripts/ai/with-package-plane.sh run --project src/Chummer.Play.RegressionChecks/Chummer.Play.RegressionChecks.csproj --no-build -- --companion-replay-only
bash scripts/ai/with-package-plane.sh run --project src/Chummer.Play.RegressionChecks/Chummer.Play.RegressionChecks.csproj --no-build -- --file-backed-ledger-only
```

The focused C# cases cover file-backed restart/lost reply, distinct same-text
actions, concurrent retry, session/role/device scope separation, observer
rejection, old-client and pre-upgrade ledger behavior, conflicting IDs, atomic
write failure, and checkpoint failure after ledger commit. The normal regression
entry point also includes these cases. Package compilation and these C# checks
remain prerequisites for accepting the server change. No live probes, heavy
builds, publication, or production closure are claimed here.
