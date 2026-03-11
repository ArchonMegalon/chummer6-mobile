using Chummer.Play.Core.Offline;
using Chummer.Play.Core.PlayApi;
using Chummer.Play.Core.Sync;

namespace Chummer.Play.Web;

public static class SessionLineage
{
    public static bool IsStoredLineageAligned(
        EngineSessionEnvelope session,
        SyncCheckpoint? checkpoint,
        OfflineLedgerEnvelope? ledger
    )
    {
        ArgumentNullException.ThrowIfNull(session);

        if (checkpoint is not null && !IsCheckpointAligned(checkpoint, session))
        {
            return false;
        }

        if (ledger is not null
            && !IsLedgerAligned(
                ledger,
                session.SessionId,
                session.SceneId,
                session.SceneRevision,
                session.RuntimeFingerprint
            ))
        {
            return false;
        }

        return true;
    }

    public static bool IsCheckpointAligned(SyncCheckpoint checkpoint, EngineSessionEnvelope session)
    {
        ArgumentNullException.ThrowIfNull(checkpoint);
        ArgumentNullException.ThrowIfNull(session);

        return StringComparer.Ordinal.Equals(checkpoint.SessionId, session.SessionId)
            && StringComparer.Ordinal.Equals(checkpoint.SceneId, session.SceneId)
            && StringComparer.Ordinal.Equals(checkpoint.SceneRevision, session.SceneRevision)
            && StringComparer.Ordinal.Equals(checkpoint.ProjectionFingerprint, session.RuntimeFingerprint);
    }

    public static bool IsSessionAligned(EngineSessionEnvelope left, EngineSessionEnvelope right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        return StringComparer.Ordinal.Equals(left.SessionId, right.SessionId)
            && StringComparer.Ordinal.Equals(left.SceneId, right.SceneId)
            && StringComparer.Ordinal.Equals(left.SceneRevision, right.SceneRevision)
            && StringComparer.Ordinal.Equals(left.RuntimeFingerprint, right.RuntimeFingerprint);
    }

    public static bool IsLedgerAligned(
        OfflineLedgerEnvelope ledger,
        string sessionId,
        string sceneId,
        string sceneRevision,
        string runtimeFingerprint
    )
    {
        ArgumentNullException.ThrowIfNull(ledger);

        return StringComparer.Ordinal.Equals(ledger.SessionId, sessionId)
            && StringComparer.Ordinal.Equals(ledger.SceneId, sceneId)
            && StringComparer.Ordinal.Equals(ledger.SceneRevision, sceneRevision)
            && StringComparer.Ordinal.Equals(ledger.RuntimeFingerprint, runtimeFingerprint);
    }
}
