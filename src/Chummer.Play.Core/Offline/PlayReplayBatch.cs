namespace Chummer.Play.Core.Offline;

// Transport identities belong to this queue, not to the engine event grammar.
public sealed record PlayReplayEvent(string EventId, string Text);

public sealed record PlayReplayBatch(string BatchId, IReadOnlyList<PlayReplayEvent> Events);
