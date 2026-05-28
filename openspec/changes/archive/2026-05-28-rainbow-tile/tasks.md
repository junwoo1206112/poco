## 1. Data Model

- [x] 1.1 Add `Rainbow` to `BombType`.
- [x] 1.2 Remove Rainbow from `PokoBlockSubtype`.
- [x] 1.3 Remove `PokoTile.IsRainbow` regular tile behavior.
- [x] 1.4 Keep `RainbowCleared` telemetry for AI/designer analysis.

## 2. Fever Gauge Split

- [x] 2.1 Rename charge pacing to Fever gauge.
- [x] 2.2 Fill Fever gauge from block clears.
- [x] 2.3 Activate Fever when gauge reaches the threshold.
- [x] 2.4 Rename balance profile field to `FeverGaugeMultiplier`.

## 3. Rainbow Bomb

- [x] 3.1 Keep Rainbow as `BombType.Rainbow`.
- [x] 3.2 Create Rainbow bombs from long-chain rewards.
- [x] 3.3 Prevent Rainbow bombs from auto-detonating on the normal bomb timer.
- [x] 3.4 Detonate Rainbow bombs by player action.
- [x] 3.5 Remove all linkable tiles of the most common color.
- [x] 3.6 Log `rainbow_cleared` combat events.

## 4. HUD

- [x] 4.1 Show the top-right charge meter as `FEVER`.
- [x] 4.2 Keep Rainbow bomb feedback separate from Fever charge UI.

## 5. Unity Verification

- [x] 5.1 Open Unity and confirm compilation succeeds.
- [x] 5.2 Verify the Fever gauge fills as blocks are cleared.
- [x] 5.3 Verify full Fever gauge starts Fever mode.
- [x] 5.4 Verify a long chain creates a Rainbow bomb.
- [x] 5.5 Verify tapping the Rainbow bomb clears all tiles of one color.
