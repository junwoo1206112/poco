# Effect Pass - 2026-05-28

## Goal

Implement the first gameplay-feel pass from `md/poko-effect-direction.md` without adding external art assets.

## Implemented

- Added `BoardEffectRenderer` for code-generated bursts, floating text, energy motes, Fever text, boss spawn text, boss skill flashes, and Rainbow clear bursts.
- Valid chain clears now spawn tile-position bursts and floating score near the chain center.
- Enemy damage now spawns energy motes from the chain center toward the combat lane and shows damage text.
- Boss spawns now show a short boss-name effect in the combat lane.
- Boss skills now trigger distinct effect colors for Freeze, Stone, and ColorSwap.
- Fever start now shows a dedicated visual beat.
- Rainbow bomb clears now show prism-style bursts over removed tiles.
- Hint tiles now pulse while available.
- Link line becomes thicker on valid 3+ chains and shifts to orange during Fever.

## Asset Decision

No new image assets were added. The first pass uses generated sprites and `TextMesh` so the team can judge timing, readability, and clutter before investing in custom VFX art.

## Next Recommended Pass

- Tune effect durations in Play mode if the board feels visually crowded.
- Replace generated burst sprites with authored VFX art only after timing is validated.

## Follow-up Implemented

- Added HP bar and boss badge pulse through `BoardHudRenderer`.
- Added full-screen skill color pulse for boss skills.
- Added short boss-skill sweep motes from the combat lane to target tiles.
- Added Rainbow target preview and a brief delay before Rainbow tiles are removed.

## Final Follow-up Implemented

- Routed normal clears, bomb clears, Rainbow clears, adjacent frozen clears, and bottom stone cleanup through tile pop/fade removal.
- Added persistent Frozen and Stone status overlays so boss skill results remain readable after impact.
- Added boss HP intro fill from 0 to current HP on boss spawn.
- Added active Fever gauge glow and gold/orange pulse while Fever is running.
