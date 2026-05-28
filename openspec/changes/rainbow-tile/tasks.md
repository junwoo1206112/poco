## 1. Data Model

- [x] 1.1 Add `Rainbow` to `BombType`.
- [x] 1.2 Remove Rainbow from `PokoBlockSubtype`.
- [x] 1.3 Remove `PokoTile.IsRainbow` regular tile behavior.
- [x] 1.4 Keep `RainbowCleared` telemetry for AI/designer analysis.

## 2. Rainbow Bomb Visual

- [x] 2.1 Generate a multicolor rainbow sprite for `BombType.Rainbow`.
- [x] 2.2 Ensure rainbow bombs do not use red/blue bomb tinting.
- [x] 2.3 Ensure rainbow bombs do not auto-detonate on the normal bomb timer.

## 3. Rainbow Gauge

- [x] 3.1 Add `rainbowGauge` state to `LineLinkerBoard`.
- [x] 3.2 Fill the gauge when blocks are cleared.
- [x] 3.3 Create a rainbow bomb when the gauge reaches the threshold.
- [x] 3.4 Preserve gauge overflow and keep the gauge full if no empty spawn cell exists.
- [x] 3.5 Reset gauge on round restart.

## 4. Rainbow Bomb Detonation

- [x] 4.1 Add `DetonateRainbowBomb()`.
- [x] 4.2 Remove all linkable tiles of the most common color.
- [x] 4.3 Award score from removed tile count.
- [x] 4.4 Log `rainbow_ready` and `rainbow_cleared` combat events.

## 5. HUD

- [x] 5.1 Add rainbow gauge bar to `BoardHudRenderer`.
- [x] 5.2 Keep feedback text from overlapping the gauge bar.

## 6. AI Agent and Telemetry

- [x] 6.1 Keep `RainbowCleared` in `BoardTelemetry`.
- [x] 6.2 Keep rainbow-aware difficulty suggestions based on rainbow bomb usage.
- [x] 6.3 Keep `PlayLogAnalysis` parsing `rainbow_cleared` events.

## 7. Unity Verification

- [ ] 7.1 Open Unity and confirm compilation succeeds.
- [ ] 7.2 Enter Play mode and verify the rainbow gauge fills as blocks are cleared.
- [ ] 7.3 Verify a rainbow bomb appears when the gauge fills.
- [ ] 7.4 Verify tapping the rainbow bomb clears all tiles of one color.
- [ ] 7.5 Verify no console errors during extended play with rainbow bombs.
- [ ] 7.6 Run CLI `analyze-playlog` and confirm rainbow events in report.
