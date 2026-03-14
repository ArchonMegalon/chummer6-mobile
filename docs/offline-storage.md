# Offline Storage

Initial storage rules for `chummer6-mobile`:

- keep a local event ledger per session
- keep runtime bundle fingerprints and sync checkpoints locally
- cache play-safe media with bounded lifecycle rules
- preserve enough state for reconnect + replay after offline use

The storage layer belongs to `chummer6-mobile`, not `chummer6-hub`.

Executable ownership in this repo:

- `BrowserSessionEventLogStore` owns the local event ledger by session and scene lineage
- `BrowserSessionOfflineCacheService` owns runtime bundle cache metadata, resume checkpoints, and bounded media-cache lifecycle rules
- `BrowserSessionOfflineQueueService` owns local-first pending-event enqueue plus sync/replay acknowledgement orchestration for `/api/play/quick-action` and `/api/play/sync`
- `wwwroot/service-worker.js` enforces installable cache policy with shell/API/media cache partitioning, media TTL+entry-cap pruning, and quota-triggered backpressure cleanup
- follow-on queue work must preserve provenance, stale protection, and replay-safe resume semantics across both seams
