# Chummer Mobile Play Split Guide

Date: 2026-03-09

## Executive summary

Split mobile play mode into `chummer-play` now.

The split is viable because the session/mobile shell is already a distinct seam:
- mobile and browser play flows are narrower than the desktop/browser workbench
- local-first session ledger handling belongs in a dedicated repo
- the backend and engine authority split already exists in the Chummer design docs

## Required rules

- Canonize contracts before broad feature migration.
- Consume `Chummer.Contracts`, `Chummer.Play.Contracts`, and `Chummer.Ui.Kit` as packages only.
- Do not copy shared contracts into this repo.
- Do not add rules math, XML parsing, provider calls, or hidden canon writes here.

## Initial repo responsibilities

- player and GM play shells
- offline event-log storage and sync/replay
- runtime bundle consumption
- play-scoped Spider, coach, and delivery surfaces
- installable PWA/mobile hardening

## Immediate task focus

1. Establish package-only dependency boundaries.
2. Extract the session-web/mobile host seam.
3. Define the repo architecture and role shells.
4. Align on canonical engine session contracts plus a narrow play API contract family.
5. Build local-first storage and replay.
