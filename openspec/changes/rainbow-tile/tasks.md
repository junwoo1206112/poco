## 1. Data Model

- [x] 1.1 Add `Rainbow` value to `PokoBlockSubtype` enum in `PokoBlockType.cs`
- [x] 1.2 Add `IsRainbow` property to `PokoTile` (`BlockSubtype == PokoBlockSubtype.Rainbow`)
- [x] 1.3 Update `IsLinkable` to return true for Rainbow subtype
- [x] 1.4 Add `RainbowCleared` field to `BoardTelemetry`

## 2. Rainbow Tile Visual

- [x] 2.1 Create `CreateRainbowGradient()` static method: generate a 96×96 Texture2D with horizontal rainbow gradient (red→yellow→green→blue→purple-magenta→back-to-red)
- [x] 2.2 Modify `PokoTile.ApplyBombVisual()` pattern or add equivalent `ApplyRainbowVisual()`: override sprite with the gradient texture when `IsRainbow` is true
- [x] 2.3 Ensure rainbow visual persists through selection/hint state changes

## 3. Rainbow Link Logic

- [x] 3.1 Modify `TryAddTileAtPointer()` in `LineLinkerBoard.cs`: when the last selected tile OR the candidate tile is Rainbow, skip the type-matching check (`tile.Type != last.Type`)
- [x] 3.2 Verify back-drag (RemoveLastSelectedTile) still works correctly with Rainbow tiles in the chain
- [x] 3.3 Verify that a chain starting with a Rainbow tile allows the second tile of any type
- [x] 3.4 Verify `RefreshLinkHints()` shows hints from Rainbow tiles to all adjacent linkable tiles

## 4. Rainbow Scoring Bonus and Tap Ability

- [x] 4.1 Modify `ClearMatchedTiles()`: check if any tile in `selectedTiles` is Rainbow, apply ×1.5 multiplier to `gainedScore` before adding to total score
- [x] 4.2 Log `rainbow_cleared` event in play log when a rainbow-inclusive chain is cleared
- [x] 4.3 Add `TryActivateRainbowAtPointer()`: on tap, removes all tiles of the most common type on the board with 50pts per tile
- [x] 4.4 Add `rainbow_tap` combat event logging

## 5. Board Generation and Refill

- [x] 5.1 Update `RandomSubtype()` to include Rainbow in the random roll (equal probability with Frozen, Stone, Clock)
- [ ] 5.2 Verify rainbow tiles appear on initial board build and after refill

## 6. AI Agent and Telemetry

- [x] 6.1 Update `HeuristicGameDesignerAgent` to check `RainbowCleared` in telemetry
- [x] 6.2 Add rainbow-aware difficulty suggestion (high rainbow usage → label Easy, suggest lower spawn rate)
- [x] 6.3 Update `PlayLogAnalysis` in `PokoPuzzleCli.cs` to parse `rainbow_cleared` events

## 7. Unity Verification

- [ ] 7.1 Open Unity and confirm compilation succeeds
- [ ] 7.2 Enter Play mode and verify rainbow tiles appear on the board with gradient visual
- [ ] 7.3 Drag from a Red tile through a Rainbow tile to a Blue tile → chain of 3 accepted
- [ ] 7.4 Verify ×1.5 score bonus on rainbow-inclusive chains
- [ ] 7.5 Verify no console errors during extended play with rainbow tiles
- [ ] 7.6 Run CLI `analyze-playlog` and confirm rainbow events in report