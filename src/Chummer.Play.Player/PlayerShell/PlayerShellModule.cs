using Chummer.Play.Components.Shell;
using Chummer.Play.Core.Application;

namespace Chummer.Play.Player.PlayerShell;

public static class PlayerShellModule
{
    public static PlayShellDescriptor CreateDescriptor() =>
        new(
            PlaySurfaceRole.Player,
            "Player Shell",
            "Narrow play-mode surface for trackers, quick actions, notes, and grounded coaching.",
            new[] { "play.session.read", "play.session.sync", "play.notes.write" }
        );
}
