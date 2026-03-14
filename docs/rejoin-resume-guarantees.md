# Rejoin, Replay, and Resume Guarantees

This is the public trust note for the `chummer6-mobile` boundary.

## What a rejoin guarantees

- a reconnecting device does not become a second source of truth
- missed events are replayed onto the client from the canonical lineage
- stale lineage is rejected instead of being silently merged

## What a resume guarantees

- resume restores the last durable local client state that still matches the session lineage
- if the stored lineage is stale, the client asks for a safe projection instead of pretending the old cache is still right
- runtime bundle lineage stays attached to the resumed session state

## What replay guarantees

- replay applies accepted events in canonical order
- queued offline events only re-enter the session when cursor and lineage checks still pass
- a device catching up should not mutate the canonical ledger out of order just because it woke up late

## What this boundary does not promise

- offline devices do not get to overwrite newer canonical state
- cross-device continuity does not bypass stale-lineage protection
- the client does not invent semantic session events that belong to core or hub canon
