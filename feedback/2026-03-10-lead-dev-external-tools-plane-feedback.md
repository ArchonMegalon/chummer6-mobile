# Lead-dev feedback: play external-tools boundary

Date: 2026-03-10

Play may render upstream projections that refer to external outputs, but it must never own vendor keys, vendor SDKs, or direct provider orchestration.

Preserve the client-local ledger boundary and keep all third-party access server-side or worker-side.
