using Chummer.Play.Components.Shell;
using Chummer.Play.Core.Application;

namespace Chummer.Play.Gm.TacticalShell;

public static class GmTacticalShellModule
{
    public static PlayShellDescriptor CreateDescriptor() =>
        new(
            PlaySurfaceRole.GameMaster,
            "GM Tactical Shell",
            "Tactical cards, approvals, reveal controls, and stale-protected session actions for live play.",
            new[] { "play.session.read", "play.session.sync", "play.spider.cards", "play.gm.actions" }
        );
}
