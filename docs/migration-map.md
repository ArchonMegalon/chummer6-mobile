# Migration Map

Initial extraction seam from `chummer-presentation` into `chummer6-mobile`:

- `Chummer.Session.Web/Program.cs` -> `src/Chummer.Play.Web/Program.cs`
- `Chummer.Session.Web/BrowserSessionApiClient.cs` -> `src/Chummer.Play.Web/BrowserSessionApiClient.cs`
- `Chummer.Session.Web/BrowserSessionCoachApiClient.cs` -> `src/Chummer.Play.Web/BrowserSessionCoachApiClient.cs`
- `Chummer.Session.Web/BrowserSessionEventLogStore.cs` -> `src/Chummer.Play.Web/BrowserSessionEventLogStore.cs`
- `Chummer.Session.Web/BrowserSessionOfflineCacheService.cs` -> `src/Chummer.Play.Web/BrowserSessionOfflineCacheService.cs`
- `Chummer.Session.Web/BrowserSessionShellProbe.cs` -> `src/Chummer.Play.Web/BrowserSessionShellProbe.cs`

Rules for migration:

- move the play seam without dragging shared workbench abstractions into this repo
- replace old `Chummer.Presentation` project references with package-only dependencies
- preserve local-first event log, runtime bundle, and offline cache ownership here
- keep DTO canon in shared packages instead of introducing repo-local copies
