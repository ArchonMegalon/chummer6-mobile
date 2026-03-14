# Sync Model

`chummer6-mobile` uses a local-first sync model:

- queue local session events first
- persist scene id, scene revision, and runtime fingerprint with every sync checkpoint
- replay locally while offline
- reconcile with canonical server projections through a narrow `/api/play/*` surface
- reject stale scene revisions instead of silently overwriting state

Hard rule: no absolute tracker overwrite paths.

Dedicated play API ownership:

- `/api/play/bootstrap` remains the role-aware shell bootstrap entry point
- follow-on `/api/play/projection`, `/api/play/reconnect`, and `/api/play/sync` work stays in `Chummer.Play.Web`
- `BrowserSessionOfflineQueueService` is the local-first sync owner for pending-event enqueue and replay acknowledgement so route handlers do not bypass stale/provenance rules
- browser cache and replay checkpoints must feed those routes without bypassing stale protection or provenance capture
