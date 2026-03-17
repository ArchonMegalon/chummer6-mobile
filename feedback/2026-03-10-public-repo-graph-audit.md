# Lead-dev feedback: play package canon and mirror coverage

Public audit status: `red/yellow`

Main issues:

* package naming still drifts between README text and build/package usage
* `.codex-design` mirror coverage is still expected to be obvious and current
* the repo is no longer docs-only and now needs to become a real package consumer

Required next steps:

1. Freeze on `Chummer.Engine.Contracts`, `Chummer.Play.Contracts`, and `Chummer.Ui.Kit`.
2. Remove legacy `Chummer.Contracts` language from README and related docs.
3. Keep pushing from scaffold behavior toward real play API, ledger, and sync seams.
