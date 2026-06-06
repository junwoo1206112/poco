# Enemy Rotation Backgrounds

## Summary

Add original background assets and runtime background transitions that follow the current regular enemy and boss rotation from Excel data.

## Portfolio Value

This makes the playable prototype read more like a themed casual puzzle stage while preserving the data-driven Excel pipeline. Reviewers can see that enemy/boss content, visual assets, and runtime presentation are coordinated through one editable data source.

## Scope

- Add `BackgroundPath` data for regular enemies and boss waves.
- Generate original placeholder background PNGs for each Excel enemy and boss.
- Switch the board background whenever the active regular enemy or boss changes.
- Fade between backgrounds so the rotation feels intentional in Play mode.

## Non-Goals

- No copied PokoPang art, names, or exact scene compositions.
- No broad stage selection framework or biome progression UI.
- No changes to combat balance, HP, scoring, or enemy skill behavior.
