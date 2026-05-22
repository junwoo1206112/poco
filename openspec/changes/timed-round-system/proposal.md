## Why

PokoPang uses a 60-second time limit per round instead of a move limit. Adding a countdown timer makes the prototype match the core PokoPang gameplay loop and gives urgency to the puzzle-solving.

## What Changes

- Add 60-second countdown timer that starts when the board is ready.
- Show remaining time on the HUD alongside score and move count.
- End the round when time reaches 0 (evaluate score vs target score).
- Keep move limit as a secondary constraint (default to a high number like 999 so time is primary).

## Capabilities

### New Capabilities

- `timed-round`: 60-second timed round with HUD countdown and time-based game-over evaluation.

### Modified Capabilities

None.

## Impact

- `LineLinkerBoard.cs` — new `roundTimer` field, `Update()` countdown, HUD display.
- `PokoPrototypeSceneSettings` — no changes needed (time is always 60s).
