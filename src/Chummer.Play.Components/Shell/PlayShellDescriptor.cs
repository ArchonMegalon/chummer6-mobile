using Chummer.Play.Core.Application;

namespace Chummer.Play.Components.Shell;

public sealed record PlayShellDescriptor(
    PlaySurfaceRole Role,
    string ShellName,
    string Summary,
    IReadOnlyList<string> RequiredCapabilities
);
