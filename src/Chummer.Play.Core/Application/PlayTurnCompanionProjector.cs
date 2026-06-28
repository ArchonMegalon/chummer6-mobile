using Chummer.Play.Core.PlayApi;
using Chummer.Play.Core.Sync;

namespace Chummer.Play.Core.Application;

public sealed record PlayTurnCompanionContext(
    string SessionId,
    PlaySurfaceRole Role,
    EngineSessionEnvelope Session,
    SyncCheckpoint? Checkpoint,
    PlayRuntimeBundleMetadata? RuntimeBundle,
    PlayCachePressureSnapshot CachePressure,
    IReadOnlyList<string> Timeline,
    IReadOnlyList<PlayQuickAction> QuickActions,
    string OwnerRoute,
    int PendingQueueCount
);

public sealed record PlayTurnCompanionState(
    int PhysicalDamage,
    int StunDamage,
    int Edge,
    int AmmoInMagazine,
    int AmmoReserve,
    int Charges,
    string SelectedActionId,
    string? SelectedAnchorId,
    IReadOnlyList<PlayTurnInventoryItemState> Inventory,
    IReadOnlyList<PlayTurnModifierState> Modifiers,
    IReadOnlyList<PlayTurnHistoryEntry> History,
    int Revision
);

public sealed record PlayTurnInventoryItemState(
    string ItemId,
    string Label,
    int Quantity
);

public sealed record PlayTurnModifierState(
    string ModifierId,
    string Label,
    int DiceModifier,
    string Source,
    bool Enabled
);

public sealed record PlayTurnHistoryEntry(
    string EntryId,
    string Title,
    string Detail,
    DateTimeOffset OccurredAtUtc,
    bool Queued,
    bool Manual,
    int? Hits,
    string ActionId
);

public sealed record PlayTurnResolveRequest(
    bool UseManualEntry,
    int? ManualHits,
    bool ManualGlitch
);

public sealed record PlayTurnCompanionProjection(
    string SessionId,
    PlaySurfaceRole Role,
    bool CanMutate,
    string ShellSummary,
    string LocalBoundarySummary,
    string CurrentSceneSummary,
    int PendingQueueCount,
    int LocalRevision,
    PlayTurnTrustSurface Trust,
    PlayTurnNowSurface Now,
    PlayTurnActionSurface Act,
    PlayTurnModifierSurface Adjust,
    PlayTurnResolveSurface Resolve,
    PlayTurnHistorySurface History,
    PlayTurnRunsiteSurface Runsite,
    PlayTurnSyncSurface Sync
);

public sealed record PlayTurnTrustSurface(
    string StatusLabel,
    string Summary,
    string CheckpointLabel,
    string RuntimeLabel,
    string QueueLabel,
    IReadOnlyList<string> Labels
);

public sealed record PlayTurnNowSurface(
    string ActorLabel,
    string WeaponLabel,
    IReadOnlyList<PlayTurnStatCard> StatCards,
    IReadOnlyList<PlayTurnInventoryCard> InventoryCards
);

public sealed record PlayTurnStatCard(
    string MetricId,
    string Label,
    int Value,
    string Detail,
    string Accent
);

public sealed record PlayTurnInventoryCard(
    string ItemId,
    string Label,
    int Quantity,
    string Detail
);

public sealed record PlayTurnActionSurface(
    string SelectedActionId,
    string SelectedActionLabel,
    IReadOnlyList<PlayTurnActionOption> Actions,
    IReadOnlyList<string> ExistingQuickActions
);

public sealed record PlayTurnActionOption(
    string ActionId,
    string Label,
    string Summary,
    int BaseDicePool,
    int AmmoCost,
    bool Selected,
    bool Enabled
);

public sealed record PlayTurnModifierSurface(
    int TotalModifier,
    string Summary,
    IReadOnlyList<PlayTurnModifierOption> Options
);

public sealed record PlayTurnModifierOption(
    string ModifierId,
    string Label,
    int DiceModifier,
    string Source,
    bool Enabled
);

public sealed record PlayTurnResolveSurface(
    string SelectedActionLabel,
    int DicePool,
    string OddsSummary,
    IReadOnlyList<PlayTurnOddsBadge> Odds,
    string ManualEntryHint,
    string LastOutcomeSummary
);

public sealed record PlayTurnOddsBadge(
    string Label,
    string Value
);

public sealed record PlayTurnHistorySurface(
    string Summary,
    IReadOnlyList<PlayTurnHistoryItem> Entries
);

public sealed record PlayTurnHistoryItem(
    string Title,
    string Detail,
    string When,
    bool Queued,
    bool Manual
);

public sealed record PlayTurnRunsiteSurface(
    string Summary,
    string TruthPosture,
    string? SelectedAnchorId,
    IReadOnlyList<PlayTurnAnchorOption> Anchors
);

public sealed record PlayTurnAnchorOption(
    string AnchorId,
    string Label,
    string Summary,
    bool Selected
);

public sealed record PlayTurnSyncSurface(
    string PendingSummary,
    string ReconnectSummary,
    string ClaimedDeviceSummary,
    bool CanReplayLocalQueue,
    bool CanAcknowledgeServerQueue,
    IReadOnlyList<PlayTurnQueueActionOption> QuickActions
);

public sealed record PlayTurnQueueActionOption(
    string ActionId,
    string Label,
    string Summary
);

public static class PlayTurnCompanionProjector
{
    public static PlayTurnCompanionState CreateDefaultState(PlaySurfaceRole role)
    {
        string selectedActionId = GetActionDefinitions(role).First().ActionId;
        string selectedAnchorId = GetAnchorDefinitions().First().AnchorId;

        return new PlayTurnCompanionState(
            PhysicalDamage: 0,
            StunDamage: 0,
            Edge: 2,
            AmmoInMagazine: 12,
            AmmoReserve: 24,
            Charges: 2,
            SelectedActionId: selectedActionId,
            SelectedAnchorId: selectedAnchorId,
            Inventory: [
                new PlayTurnInventoryItemState("stim-patch", "Stim Patch", 1),
                new PlayTurnInventoryItemState("medkit", "Medkit", 1),
                new PlayTurnInventoryItemState("flashbang", "Flashbang", 2)
            ],
            Modifiers: [
                new PlayTurnModifierState("cover", "Cover", 2, "Partial cover", false),
                new PlayTurnModifierState("wound", "Wound", -1, "Current physical condition", false),
                new PlayTurnModifierState("recoil", "Recoil", -2, "Carry-forward burst recoil", false),
                new PlayTurnModifierState("visibility", "Visibility", -2, "Smoke, glare, or darkness", false),
                new PlayTurnModifierState("aim", "Take Aim", 1, "One careful setup beat", false),
                new PlayTurnModifierState("sustained", "Sustained effect", -2, "Bound spell or effect upkeep", false)
            ],
            History: [
                new PlayTurnHistoryEntry(
                    "seeded",
                    "Turn tracker seeded",
                    "Install-local values are editable here and do not replace engine or GM authority.",
                    DateTimeOffset.UtcNow,
                    Queued: false,
                    Manual: false,
                    Hits: null,
                    ActionId: selectedActionId)
            ],
            Revision: 1
        );
    }

    public static PlayTurnCompanionProjection Project(
        PlayTurnCompanionContext context,
        PlayTurnCompanionState state)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(state);

        bool canMutate = context.Role != PlaySurfaceRole.Observer;
        ActionDefinition selectedAction = GetSelectedActionDefinition(context.Role, state.SelectedActionId);
        int modifierTotal = state.Modifiers.Where(static item => item.Enabled).Sum(static item => item.DiceModifier);
        int dicePool = Math.Max(0, selectedAction.BaseDicePool + modifierTotal);
        string latestTimeline = context.Timeline.LastOrDefault() ?? "No local action receipt is pinned yet.";
        string statusLabel = BuildTrustStatusLabel(context);
        string queueLabel = context.PendingQueueCount == 0
            ? "No queued replay-safe mutations are pending."
            : $"{context.PendingQueueCount} queued replay-safe mutation(s) still need reconnect or sync.";
        string checkpointLabel = context.Checkpoint is null
            ? "Checkpoint pending"
            : $"Checkpoint {context.Checkpoint.AppliedThroughSequence} on {context.Checkpoint.SceneRevision}";
        string runtimeLabel = context.RuntimeBundle is null
            ? "Runtime bundle proof pending"
            : $"Bundle {context.RuntimeBundle.BundleTag} · {context.RuntimeBundle.RuntimeFingerprint}";
        string actorLabel = context.Role switch
        {
            PlaySurfaceRole.GameMaster => "GM focus actor",
            PlaySurfaceRole.Observer => "Observer mirror",
            _ => "Claimed player actor"
        };
        string weaponLabel = selectedAction.AmmoCost > 0
            ? $"Active action: {selectedAction.Label} · magazine {state.AmmoInMagazine} · reserve {state.AmmoReserve}"
            : $"Active action: {selectedAction.Label} · queue-safe local tracking";
        string shellSummary = $"{actorLabel} on {context.Session.SceneId} · {latestTimeline}";
        string localBoundarySummary = canMutate
            ? "Install-local turn tracker: adjust these values freely during play, but treat them as mobile shell truth until the wider session confirms them."
            : "Observer mode is read-mostly: this mirror can inspect trust, anchors, and recent deltas, but it must not mutate player or GM state.";
        string currentSceneSummary = $"Scene {context.Session.SceneId} · revision {context.Session.SceneRevision} · runtime {context.Session.RuntimeFingerprint}.";
        string oddsSummary = BuildOddsSummary(dicePool);
        IReadOnlyList<PlayTurnAnchorOption> anchors = GetAnchorDefinitions()
            .Select(anchor => new PlayTurnAnchorOption(
                anchor.AnchorId,
                anchor.Label,
                anchor.Summary,
                string.Equals(anchor.AnchorId, state.SelectedAnchorId, StringComparison.Ordinal)))
            .ToArray();
        string selectedAnchorLabel = anchors.FirstOrDefault(static anchor => anchor.Selected)?.Label ?? "No anchor selected";

        return new PlayTurnCompanionProjection(
            context.SessionId,
            context.Role,
            canMutate,
            shellSummary,
            localBoundarySummary,
            currentSceneSummary,
            context.PendingQueueCount,
            state.Revision,
            new PlayTurnTrustSurface(
                statusLabel,
                BuildTrustSummary(context, latestTimeline),
                checkpointLabel,
                runtimeLabel,
                queueLabel,
                BuildTrustLabels(context, latestTimeline)),
            new PlayTurnNowSurface(
                actorLabel,
                weaponLabel,
                [
                    new PlayTurnStatCard("physical", "Physical", state.PhysicalDamage, "Damage marked locally on this shell.", "critical"),
                    new PlayTurnStatCard("stun", "Stun", state.StunDamage, "Stun carry-forward for the next exchange.", "warning"),
                    new PlayTurnStatCard("edge", "Edge", state.Edge, "Spend-now currency for quick calls.", "accent"),
                    new PlayTurnStatCard("ammo", "Magazine", state.AmmoInMagazine, "Active firing posture.", "cool"),
                    new PlayTurnStatCard("reserve", "Reserve", state.AmmoReserve, "Fallback rounds or spare clips.", "muted"),
                    new PlayTurnStatCard("charges", "Charges", state.Charges, "Consumable charges or spell upkeep slots.", "accent")
                ],
                state.Inventory.Select(item => new PlayTurnInventoryCard(item.ItemId, item.Label, item.Quantity, "Mission-critical inventory only.")).ToArray()),
            new PlayTurnActionSurface(
                selectedAction.ActionId,
                selectedAction.Label,
                GetActionDefinitions(context.Role)
                    .Select(action => new PlayTurnActionOption(
                        action.ActionId,
                        action.Label,
                        action.Summary,
                        action.BaseDicePool,
                        action.AmmoCost,
                        string.Equals(action.ActionId, selectedAction.ActionId, StringComparison.Ordinal),
                        canMutate))
                    .ToArray(),
                context.QuickActions.Select(action => action.Label).ToArray()),
            new PlayTurnModifierSurface(
                modifierTotal,
                BuildModifierSummary(modifierTotal, state.Modifiers),
                state.Modifiers.Select(item => new PlayTurnModifierOption(item.ModifierId, item.Label, item.DiceModifier, item.Source, item.Enabled)).ToArray()),
            new PlayTurnResolveSurface(
                selectedAction.Label,
                dicePool,
                oddsSummary,
                BuildOddsBadges(dicePool),
                "Physical dice are first-class here: enter the hit count you rolled at the table, or use the digital resolver for a bounded local receipt.",
                state.History.FirstOrDefault()?.Detail ?? "No local resolution receipt is recorded yet."),
            new PlayTurnHistorySurface(
                BuildHistorySummary(state.History, context.PendingQueueCount),
                state.History.Take(8).Select(item => new PlayTurnHistoryItem(
                    item.Title,
                    item.Detail,
                    item.OccurredAtUtc.ToString("HH:mm 'UTC'"),
                    item.Queued,
                    item.Manual)).ToArray()),
            new PlayTurnRunsiteSurface(
                $"RUNSITE anchor: {selectedAnchorLabel}. Keep spatial orientation visible without turning this shell into exact tactical-position truth.",
                "RUNSITE stays orientation-only here: room, zone, and hotspot anchors are inspectable context, not token authority.",
                state.SelectedAnchorId,
                anchors),
            new PlayTurnSyncSurface(
                BuildPendingSummary(context),
                BuildReconnectSummary(context),
                BuildClaimedDeviceSummary(context),
                canMutate,
                canMutate && context.PendingQueueCount > 0,
                context.QuickActions.Select(action => new PlayTurnQueueActionOption(action.ActionId, action.Label, BuildQuickActionSummary(action.RequiredCapability))).ToArray())
        );
    }

    public static PlayTurnCompanionState SelectAction(
        PlayTurnCompanionState state,
        PlaySurfaceRole role,
        string actionId)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(actionId);

        ActionDefinition selected = GetActionDefinitions(role).FirstOrDefault(action => string.Equals(action.ActionId, actionId, StringComparison.Ordinal))
            ?? GetActionDefinitions(role).First();
        if (string.Equals(selected.ActionId, state.SelectedActionId, StringComparison.Ordinal))
        {
            return state;
        }

        return state with
        {
            SelectedActionId = selected.ActionId,
            Revision = state.Revision + 1
        };
    }

    public static PlayTurnCompanionState SelectAnchor(
        PlayTurnCompanionState state,
        string? anchorId)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (string.IsNullOrWhiteSpace(anchorId))
        {
            return state;
        }

        AnchorDefinition selected = GetAnchorDefinitions().FirstOrDefault(anchor => string.Equals(anchor.AnchorId, anchorId, StringComparison.Ordinal))
            ?? GetAnchorDefinitions().First();
        if (string.Equals(selected.AnchorId, state.SelectedAnchorId, StringComparison.Ordinal))
        {
            return state;
        }

        return state with
        {
            SelectedAnchorId = selected.AnchorId,
            Revision = state.Revision + 1
        };
    }

    public static PlayTurnCompanionState ToggleModifier(
        PlayTurnCompanionState state,
        string modifierId,
        bool enabled)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(modifierId);

        PlayTurnModifierState[] modifiers = state.Modifiers
            .Select(item => string.Equals(item.ModifierId, modifierId, StringComparison.Ordinal) ? item with { Enabled = enabled } : item)
            .ToArray();
        PlayTurnModifierState? changed = modifiers.FirstOrDefault(item => string.Equals(item.ModifierId, modifierId, StringComparison.Ordinal));
        return AppendHistory(
            state with
            {
                Modifiers = modifiers,
                Revision = state.Revision + 1
            },
            changed is null
                ? "Modifier unchanged"
                : $"{changed.Label} {(enabled ? "enabled" : "disabled")}",
            changed is null
                ? "No modifier delta was applied."
                : $"{changed.Label} now contributes {FormatSignedModifier(changed.DiceModifier)} dice from {changed.Source}.",
            queued: false,
            manual: false,
            hits: null,
            actionId: state.SelectedActionId
        );
    }

    public static PlayTurnCompanionState AdjustMetric(
        PlayTurnCompanionState state,
        string metricId,
        int delta)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(metricId);
        if (delta == 0)
        {
            return state;
        }

        PlayTurnCompanionState updated = metricId switch
        {
            "physical" => state with { PhysicalDamage = Math.Max(0, state.PhysicalDamage + delta), Revision = state.Revision + 1 },
            "stun" => state with { StunDamage = Math.Max(0, state.StunDamage + delta), Revision = state.Revision + 1 },
            "edge" => state with { Edge = Math.Max(0, state.Edge + delta), Revision = state.Revision + 1 },
            "ammo" => state with { AmmoInMagazine = Math.Max(0, state.AmmoInMagazine + delta), Revision = state.Revision + 1 },
            "reserve" => state with { AmmoReserve = Math.Max(0, state.AmmoReserve + delta), Revision = state.Revision + 1 },
            "charges" => state with { Charges = Math.Max(0, state.Charges + delta), Revision = state.Revision + 1 },
            _ when metricId.StartsWith("inventory:", StringComparison.Ordinal) => AdjustInventory(state, metricId["inventory:".Length..], delta),
            _ => state
        };

        string title = metricId.StartsWith("inventory:", StringComparison.Ordinal)
            ? "Inventory adjusted"
            : $"{HumanizeMetric(metricId)} adjusted";
        string detail = metricId.StartsWith("inventory:", StringComparison.Ordinal)
            ? BuildInventoryAdjustmentDetail(updated, metricId["inventory:".Length..], delta)
            : $"{HumanizeMetric(metricId)} changed by {delta:+#;-#;0}.";
        return AppendHistory(updated, title, detail, queued: false, manual: false, hits: null, actionId: state.SelectedActionId);
    }

    public static PlayTurnCompanionState ResolveAction(
        PlayTurnCompanionContext context,
        PlayTurnCompanionState state,
        PlayTurnResolveRequest request)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(request);

        ActionDefinition action = GetSelectedActionDefinition(context.Role, state.SelectedActionId);
        int modifierTotal = state.Modifiers.Where(static item => item.Enabled).Sum(static item => item.DiceModifier);
        int dicePool = Math.Max(0, action.BaseDicePool + modifierTotal);
        int hits;
        bool glitch;
        string mode;

        if (request.UseManualEntry)
        {
            hits = Math.Max(0, request.ManualHits ?? 0);
            glitch = request.ManualGlitch;
            mode = "manual";
        }
        else
        {
            RollResult roll = RollDicePool(dicePool);
            hits = roll.Hits;
            glitch = roll.Glitch;
            mode = "digital";
        }

        PlayTurnCompanionState updated = ApplyActionDelta(state, action);
        string deltaSummary = BuildResolveDeltaSummary(state, updated, action);
        string receipt = $"{action.Label}: {hits} hit(s) on {dicePool} dice via {mode} entry. {deltaSummary}{(glitch ? " Glitch posture flagged." : string.Empty)}";

        return AppendHistory(
            updated with { Revision = updated.Revision + 1 },
            $"{action.Label} resolved",
            receipt,
            queued: context.PendingQueueCount > 0,
            manual: request.UseManualEntry,
            hits: hits,
            actionId: action.ActionId
        );
    }

    private static PlayTurnCompanionState ApplyActionDelta(PlayTurnCompanionState state, ActionDefinition action)
    {
        PlayTurnCompanionState updated = state;

        if (action.AmmoCost > 0)
        {
            updated = updated with
            {
                AmmoInMagazine = Math.Max(0, updated.AmmoInMagazine - action.AmmoCost)
            };
        }

        if (action.ReloadsMagazine)
        {
            int magazineCapacity = 12;
            int needed = Math.Max(0, magazineCapacity - updated.AmmoInMagazine);
            int transferred = Math.Min(needed, updated.AmmoReserve);
            updated = updated with
            {
                AmmoInMagazine = updated.AmmoInMagazine + transferred,
                AmmoReserve = updated.AmmoReserve - transferred
            };
        }

        if (action.ConsumesCharge)
        {
            updated = updated with { Charges = Math.Max(0, updated.Charges - 1) };
        }

        if (action.ConsumesInventoryItemId is not null)
        {
            updated = AdjustInventory(updated, action.ConsumesInventoryItemId, -1);
        }

        return updated;
    }

    private static PlayTurnCompanionState AdjustInventory(
        PlayTurnCompanionState state,
        string itemId,
        int delta)
    {
        PlayTurnInventoryItemState[] inventory = state.Inventory
            .Select(item => string.Equals(item.ItemId, itemId, StringComparison.Ordinal)
                ? item with { Quantity = Math.Max(0, item.Quantity + delta) }
                : item)
            .ToArray();
        return state with
        {
            Inventory = inventory,
            Revision = state.Revision + 1
        };
    }

    private static PlayTurnCompanionState AppendHistory(
        PlayTurnCompanionState state,
        string title,
        string detail,
        bool queued,
        bool manual,
        int? hits,
        string actionId)
    {
        PlayTurnHistoryEntry entry = new(
            Guid.NewGuid().ToString("N"),
            title,
            detail,
            DateTimeOffset.UtcNow,
            queued,
            manual,
            hits,
            actionId
        );
        PlayTurnHistoryEntry[] history = [entry, .. state.History.Take(11)];
        return state with { History = history };
    }

    private static string BuildResolveDeltaSummary(
        PlayTurnCompanionState before,
        PlayTurnCompanionState after,
        ActionDefinition action)
    {
        List<string> deltas = [];
        if (before.AmmoInMagazine != after.AmmoInMagazine)
        {
            deltas.Add($"Magazine {before.AmmoInMagazine} -> {after.AmmoInMagazine}");
        }

        if (before.AmmoReserve != after.AmmoReserve)
        {
            deltas.Add($"Reserve {before.AmmoReserve} -> {after.AmmoReserve}");
        }

        if (before.Charges != after.Charges)
        {
            deltas.Add($"Charges {before.Charges} -> {after.Charges}");
        }

        PlayTurnInventoryItemState? consumedItem = action.ConsumesInventoryItemId is null
            ? null
            : after.Inventory.FirstOrDefault(item => string.Equals(item.ItemId, action.ConsumesInventoryItemId, StringComparison.Ordinal));
        if (consumedItem is not null)
        {
            deltas.Add($"{consumedItem.Label} now {consumedItem.Quantity}");
        }

        if (deltas.Count == 0)
        {
            return "No automatic local counter change was applied.";
        }

        return string.Join(" · ", deltas);
    }

    private static string BuildTrustStatusLabel(PlayTurnCompanionContext context)
    {
        if (context.Role == PlaySurfaceRole.Observer)
        {
            return "Observer mirror";
        }

        if (context.RuntimeBundle is null || context.Checkpoint is null)
        {
            return "Reconnect before trust";
        }

        if (context.CachePressure.BackpressureActive)
        {
            return "Cache pressure watch";
        }

        if (context.PendingQueueCount > 0)
        {
            return "Queued locally";
        }

        return "Grounded local tracker";
    }

    private static string BuildTrustSummary(PlayTurnCompanionContext context, string latestTimeline)
    {
        string runtime = context.RuntimeBundle is null
            ? "bundle proof is still pending"
            : $"bundle {context.RuntimeBundle.BundleTag} is cached";
        string checkpoint = context.Checkpoint is null
            ? "no local checkpoint is pinned yet"
            : $"checkpoint {context.Checkpoint.AppliedThroughSequence} is pinned";
        return $"Latest table cue: {latestTimeline}. Trust posture: {runtime}, {checkpoint}, and {context.PendingQueueCount} queued replay-safe mutation(s).";
    }

    private static string[] BuildTrustLabels(PlayTurnCompanionContext context, string latestTimeline)
    {
        List<string> labels =
        [
            $"Scene packet: {context.Session.SceneId} · {context.Session.SceneRevision}.",
            $"Owner route: {context.OwnerRoute}.",
            $"Latest cue: {latestTimeline}."
        ];

        labels.Add(context.RuntimeBundle is null
            ? "Runtime proof: reconnect once to seed a grounded local bundle."
            : $"Runtime proof: {context.RuntimeBundle.BundleTag} validated at {context.RuntimeBundle.LastValidatedAtUtc:yyyy-MM-dd HH:mm} UTC.");

        labels.Add(context.Checkpoint is null
            ? "Checkpoint: no local continuity anchor is pinned yet."
            : $"Checkpoint: {context.Checkpoint.AppliedThroughSequence} on {context.Checkpoint.SceneRevision}.");

        labels.Add(context.CachePressure.BackpressureActive
            ? $"Cache pressure: eviction already touched {context.CachePressure.EvictedEntryCount} session(s)."
            : $"Cache pressure: {context.CachePressure.RuntimeBundleCount}/{context.CachePressure.RuntimeBundleQuota} bundles pinned.");

        return labels.ToArray();
    }

    private static string BuildModifierSummary(int modifierTotal, IReadOnlyList<PlayTurnModifierState> modifiers)
    {
        int activeCount = modifiers.Count(static item => item.Enabled);
        if (activeCount == 0)
        {
            return "No modifier is active; the resolver stays on the clean base pool.";
        }

        return $"{activeCount} modifier(s) active for a total of {FormatSignedModifier(modifierTotal)} dice.";
    }

    private static string BuildPendingSummary(PlayTurnCompanionContext context)
        => context.PendingQueueCount == 0
            ? "Server replay queue is empty. Device-local receipts can stay local until you are ready to replay them."
            : $"{context.PendingQueueCount} replay-safe event(s) are pending on the server queue. Acknowledge sync only after the owner lane confirms the handoff.";

    private static string BuildReconnectSummary(PlayTurnCompanionContext context)
    {
        if (context.Role == PlaySurfaceRole.Observer)
        {
            return "Observer mirror stays read-mostly: inspect trust and continuity only, and leave replay or queue ownership to the player or GM lane.";
        }

        if (context.RuntimeBundle is null || context.Checkpoint is null)
        {
            return "Reconnect once before you replay local receipts so the claimed-device lane has a grounded runtime proof and continuity checkpoint.";
        }

        return "When the device reconnects, replay local receipts into the server queue first, then acknowledge sync after the owner route or GM confirms the accepted handoff.";
    }

    private static string BuildClaimedDeviceSummary(PlayTurnCompanionContext context)
        => $"Claimed-device route: {context.OwnerRoute}. Keep this install-local shell as the bounded turn companion, and treat replay acknowledgement as a deliberate handoff instead of background sync magic.";

    private static string BuildQuickActionSummary(string requiredCapability)
        => requiredCapability switch
        {
            "play.gm.actions" => "Replay-safe GM coordination action for the current scene handoff.",
            "play.spider.cards" => "Publish a bounded GM evidence card after the owner lane reconnects.",
            _ => "Replay-safe player action that should follow the current owner route and sync posture."
        };

    private static string BuildOddsSummary(int dicePool)
    {
        if (dicePool <= 0)
        {
            return "Dice pool is zero; adjust modifiers or choose a different action before resolving.";
        }

        int onePlus = ToPercent(ProbabilityAtLeastHits(dicePool, 1));
        int twoPlus = ToPercent(ProbabilityAtLeastHits(dicePool, 2));
        int threePlus = ToPercent(ProbabilityAtLeastHits(dicePool, 3));
        int glitch = ToPercent(ProbabilityOfGlitch(dicePool));
        return $"At {dicePool} dice: {onePlus}% for 1+ hit, {twoPlus}% for 2+, {threePlus}% for 3+, and {glitch}% glitch risk.";
    }

    private static IReadOnlyList<PlayTurnOddsBadge> BuildOddsBadges(int dicePool)
    {
        if (dicePool <= 0)
        {
            return [
                new PlayTurnOddsBadge("1+ hits", "0%"),
                new PlayTurnOddsBadge("2+ hits", "0%"),
                new PlayTurnOddsBadge("3+ hits", "0%"),
                new PlayTurnOddsBadge("Glitch", "0%")
            ];
        }

        return [
            new PlayTurnOddsBadge("1+ hits", $"{ToPercent(ProbabilityAtLeastHits(dicePool, 1))}%"),
            new PlayTurnOddsBadge("2+ hits", $"{ToPercent(ProbabilityAtLeastHits(dicePool, 2))}%"),
            new PlayTurnOddsBadge("3+ hits", $"{ToPercent(ProbabilityAtLeastHits(dicePool, 3))}%"),
            new PlayTurnOddsBadge("Glitch", $"{ToPercent(ProbabilityOfGlitch(dicePool))}%")
        ];
    }

    private static string BuildHistorySummary(IReadOnlyList<PlayTurnHistoryEntry> history, int pendingQueueCount)
    {
        PlayTurnHistoryEntry? latest = history.FirstOrDefault();
        if (latest is null)
        {
            return pendingQueueCount == 0
                ? "No local history is recorded yet."
                : $"{pendingQueueCount} queued replay-safe mutation(s) are pending without a local turn receipt yet.";
        }

        return pendingQueueCount == 0
            ? $"Latest local receipt: {latest.Title}."
            : $"Latest local receipt: {latest.Title}. {pendingQueueCount} queued replay-safe mutation(s) still need sync.";
    }

    private static string BuildInventoryAdjustmentDetail(PlayTurnCompanionState state, string itemId, int delta)
    {
        PlayTurnInventoryItemState? item = state.Inventory.FirstOrDefault(current => string.Equals(current.ItemId, itemId, StringComparison.Ordinal));
        if (item is null)
        {
            return $"Inventory delta {delta:+#;-#;0} was ignored because the item is not present on this shell.";
        }

        return $"{item.Label} changed by {delta:+#;-#;0} and is now {item.Quantity}.";
    }

    private static string HumanizeMetric(string metricId)
        => metricId switch
        {
            "physical" => "Physical",
            "stun" => "Stun",
            "edge" => "Edge",
            "ammo" => "Magazine",
            "reserve" => "Reserve",
            "charges" => "Charges",
            _ => metricId
        };

    private static ActionDefinition GetSelectedActionDefinition(PlaySurfaceRole role, string selectedActionId)
        => GetActionDefinitions(role).FirstOrDefault(action => string.Equals(action.ActionId, selectedActionId, StringComparison.Ordinal))
           ?? GetActionDefinitions(role).First();

    private static IReadOnlyList<ActionDefinition> GetActionDefinitions(PlaySurfaceRole role)
        => role switch
        {
            PlaySurfaceRole.GameMaster => [
                new ActionDefinition("advance-initiative", "Advance Initiative", "Keep the turn order moving without opening a bigger GM bench.", 8, 0, false, false, null),
                new ActionDefinition("attack", "Resolve Attack", "Bounded attack resolution for the current focused actor.", 12, 3, false, false, null),
                new ActionDefinition("defend", "Take Defense", "Preview the defensive pool before the table commits.", 9, 0, false, false, null),
                new ActionDefinition("soak", "Soak Damage", "Quick soak receipt without leaving the scene lane.", 10, 0, false, false, null),
                new ActionDefinition("reveal-threat", "Reveal Threat", "Post the next bounded threat cue for the table.", 9, 0, false, false, null),
                new ActionDefinition("reload", "Reload", "Refill the active weapon from local reserve tracking.", 6, 0, true, false, null)
            ],
            PlaySurfaceRole.Observer => [
                new ActionDefinition("review", "Review Turn", "Observer mirror only. No mutation leaves this shell.", 0, 0, false, false, null),
                new ActionDefinition("anchor", "Inspect Anchor", "Observer mirror only. Keep room and route context visible.", 0, 0, false, false, null)
            ],
            _ => [
                new ActionDefinition("attack", "Resolve Attack", "Primary attack rail with a bounded burst-cost assumption.", 12, 3, false, false, null),
                new ActionDefinition("defend", "Take Defense", "Preview the defensive pool before you burn a scarce interrupt.", 9, 0, false, false, null),
                new ActionDefinition("soak", "Soak Damage", "Check the immediate damage soak lane.", 10, 0, false, false, null),
                new ActionDefinition("reload", "Reload", "Move rounds from reserve into the current magazine.", 6, 0, true, false, null),
                new ActionDefinition("use-consumable", "Use Consumable", "Spend a mission-critical item without opening stash management.", 7, 0, false, false, "stim-patch"),
                new ActionDefinition("cast-or-sustain", "Cast / Sustain", "Bounded spell or effect resolution with upkeep cost tracking.", 11, 0, false, true, null)
            ]
        };

    private static IReadOnlyList<AnchorDefinition> GetAnchorDefinitions()
        => [
            new AnchorDefinition("front-door", "Front Door", "Primary ingress and public-facing entry."),
            new AnchorDefinition("service-hall", "Service Hall", "Maintenance path and fallback route."),
            new AnchorDefinition("server-room", "Server Room", "High-value objective space with constrained sightlines."),
            new AnchorDefinition("fire-stairs", "Fire Stairs", "Emergency egress and fallback regroup point.")
        ];

    private static string FormatSignedModifier(int modifier)
        => modifier.ToString("+#;-#;0");

    private static int ToPercent(double value)
        => (int)Math.Round(value * 100d, MidpointRounding.AwayFromZero);

    private static double ProbabilityAtLeastHits(int dicePool, int minimumHits)
    {
        double total = 0d;
        for (int hits = minimumHits; hits <= dicePool; hits++)
        {
            total += BinomialProbability(dicePool, hits, 1d / 3d);
        }

        return total;
    }

    private static double ProbabilityOfGlitch(int dicePool)
    {
        double total = 0d;
        int onesThreshold = (int)Math.Ceiling(dicePool / 2d);
        for (int hits = 0; hits <= dicePool; hits++)
        {
            for (int ones = onesThreshold; ones <= dicePool - hits; ones++)
            {
                int neutral = dicePool - hits - ones;
                total += MultinomialProbability(
                    hits,
                    ones,
                    neutral,
                    hitProbability: 1d / 3d,
                    oneProbability: 1d / 6d,
                    neutralProbability: 1d / 2d);
            }
        }

        return total;
    }

    private static double BinomialProbability(int count, int successes, double probability)
        => Combination(count, successes) * Math.Pow(probability, successes) * Math.Pow(1d - probability, count - successes);

    private static double MultinomialProbability(
        int hits,
        int ones,
        int neutral,
        double hitProbability,
        double oneProbability,
        double neutralProbability)
    {
        int total = hits + ones + neutral;
        double coefficient = Factorial(total) / (Factorial(hits) * Factorial(ones) * Factorial(neutral));
        return coefficient
               * Math.Pow(hitProbability, hits)
               * Math.Pow(oneProbability, ones)
               * Math.Pow(neutralProbability, neutral);
    }

    private static double Combination(int count, int picks)
        => Factorial(count) / (Factorial(picks) * Factorial(count - picks));

    private static double Factorial(int value)
    {
        double result = 1d;
        for (int current = 2; current <= value; current++)
        {
            result *= current;
        }

        return result;
    }

    private static RollResult RollDicePool(int dicePool)
    {
        int hits = 0;
        int ones = 0;
        for (int index = 0; index < dicePool; index++)
        {
            int die = Random.Shared.Next(1, 7);
            if (die >= 5)
            {
                hits++;
            }
            else if (die == 1)
            {
                ones++;
            }
        }

        bool glitch = ones >= (int)Math.Ceiling(dicePool / 2d);
        return new RollResult(hits, glitch);
    }

    private sealed record ActionDefinition(
        string ActionId,
        string Label,
        string Summary,
        int BaseDicePool,
        int AmmoCost,
        bool ReloadsMagazine,
        bool ConsumesCharge,
        string? ConsumesInventoryItemId
    );

    private sealed record AnchorDefinition(
        string AnchorId,
        string Label,
        string Summary
    );

    private sealed record RollResult(
        int Hits,
        bool Glitch
    );
}
