using Chummer.Play.Core.Offline;

namespace Chummer.Play.Web.BrowserState;

// One atomic store value commits the engine ledger and replay receipts together.
// Legacy ledger values are read explicitly; they have no recoverable identities.
internal sealed record ReplayLedgerState(
    int ReplayFormatVersion,
    OfflineLedgerEnvelope Ledger,
    IReadOnlyList<ScopedReplayBatch> ReplayBatches);

internal sealed record ScopedReplayBatch(string Scope, PlayReplayBatch Batch);
