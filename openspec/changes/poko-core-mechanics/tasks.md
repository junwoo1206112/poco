## 1. Data Model Changes

- [ ] 1.1 Add `BlockSubtype` enum to new `PokoBlockType.cs` (None, Frozen, Stone, Clock)
- [ ] 1.2 Add `BlockSubtype` field to `PokoTile` with visual tint methods
- [ ] 1.3 Add combat fields to `BoardTelemetry` (Combo, FeverActive, EnemyHp, TotalDamageDealt, BombsCleared, SpecialBlocksCleared)
- [ ] 1.4 Add combat fields to `AgentSuggestion` if tuning suggestions reference combo/enemy parameters
- [ ] 1.5 Add `chainCommitted` state tracking to `LineLinkerBoard`

## 2. Irreversible One-Stroke

- [ ] 2.1 Modify `ContinueDrag()`: when `selectedTiles.Count >= 3`, commit chain immediately (fire clear/score/collapse/refill)
- [ ] 2.2 Modify `RemoveLastSelectedTile()`: prevent back-drag removal when `selectedTiles.Count >= 3`
- [ ] 2.3 Ensure `EndDrag()` still works when release happens after auto-commit (guard against double-clear)
- [ ] 2.4 Update link hints and line refresh to work with auto-commit flow

## 3. Combo Counter and Fever

- [ ] 3.1 Add `comboCount` field and `lastClearTime` field to `LineLinkerBoard`
- [ ] 3.2 Add `feverActive`, `feverTimer` fields and `feverDuration` constant (6s)
- [ ] 3.3 Implement combo logic in `EndDrag()`: on valid clear, check if within 2.5s of last clear → increment or reset combo
- [ ] 3.4 Implement combo reset on invalid short chain
- [ ] 3.5 Implement combo timeout in `Update()`: if `lastClearTime + 2.5s < Time.time`, reset combo
- [ ] 3.6 Implement Fever trigger at combo == 7: set `feverActive`, `feverTimer = 6f`, score multiplier = 2x
- [ ] 3.7 Implement Fever neighbor-cascade: when clearing tiles during Fever, also destroy all 1-ring neighbors (non-recursive)
- [ ] 3.8 Implement Fever expiry in `Update()`: decrement `feverTimer`, clear active at 0
- [ ] 3.9 Add combo and fever visual indicators to HUD (OnGUI)
- [ ] 3.10 Add combo multiplier to scoring formula: `(N*N*10) * (feverActive ? 2 : Mathf.Max(1, comboCount))`

## 4. Enemy Combat

- [ ] 4.1 Create new `BoardEnemy.cs` with HP (100), maxHP, damage method, defeat bonus (5000)
- [ ] 4.2 Add enemy instance to `LineLinkerBoard` with HP bar rendering (screen-space or world-space)
- [ ] 4.3 Implement projectile launch on valid chain clear: deal `chainLength * 10` damage
- [ ] 4.4 Implement enemy defeat: award bonus score, visual indicator
- [ ] 4.5 Add enemy HP bar to HUD panel

## 5. Bomb Generation

- [ ] 5.1 Create new `BoardBomb.cs` with bomb type (Red/Blue), timer, detonation method
- [ ] 5.2 After chain clear, if `chainLength >= 7`, place Red Bomb at random board position
- [ ] 5.3 After chain clear, if `chainLength >= 10`, place Blue Bomb (overrides Red)
- [ ] 5.4 Implement bomb tap detection in `TryAddTileAtPointer`: if tapped tile is a bomb, detonate instead of linking
- [ ] 5.5 Implement Red Bomb detonation: clear all tiles in 6-direction straight lines
- [ ] 5.6 Implement Blue Bomb detonation: clear all tiles in 3x3 radius
- [ ] 5.7 Implement 5-second auto-detonation timer in `Update()`
- [ ] 5.8 Add bomb visual (shrunken circle with colored glow) and shrinking timer indicator

## 6. Special Blocks

- [ ] 6.1 Add `BlockSubtype` field to `PokoTile`, initialize random subtype during board generation (configurable probability)
- [ ] 6.2 Implement Frozen block behavior: mark as unlinkable; when adjacent tile cleared, destroy frozen block
- [ ] 6.3 Implement Stone block behavior: unlinkable; on collapse, fall to bottom of column; auto-clear on bottom row
- [ ] 6.4 Implement Clock block behavior: when cleared (by chain or bomb), add +2s to round time
- [ ] 6.5 Add visual tint for each subtype on the white tile sprite
- [ ] 6.6 Log special block clears in play log

## 7. Combat Telemetry and Logging

- [ ] 7.1 Add combat event logging: combo increment, fever start/end, enemy damage, bomb place/detonate, special block clear
- [ ] 7.2 Extend play log JSON events with combat fields
- [ ] 7.3 Extend `PlayLogAnalysis` to parse and summarize combat metrics
- [ ] 7.4 Extend `HeuristicGameDesignerAgent.Analyze()` to use combat telemetry for tuning
- [ ] 7.5 Extend CLI analysis reports to include combat metrics
- [ ] 7.6 Update `BoardTelemetry` construction to include combat fields

## 8. Unity Verification

- [ ] 8.1 Open Unity, confirm compilation succeeds with no errors
- [ ] 8.2 Enter Play mode and verify irreversible one-stroke: drag to 3 tiles → chain commits automatically
- [ ] 8.3 Verify combo counter increments on consecutive clears within 2.5s
- [ ] 8.4 Verify Fever triggers at 7 combo with neighbor-cascade effect
- [ ] 8.5 Verify enemy appears with HP bar and takes damage from clears
- [ ] 8.6 Verify bombs drop from 7+/10+ chains and detonate on tap or after 5s
- [ ] 8.7 Verify special blocks (frozen, stone, clock) behave correctly
- [ ] 8.8 Verify no console errors during extended play
- [ ] 8.9 Run `tools\poko-cli.cmd analyze-playlog` and confirm combat metrics in report

## 9. Portfolio Evidence

- [ ] 9.1 Write portfolio milestone document `md/portfolio-milestones/poko-core-mechanics.md`
- [ ] 9.2 Capture before/after play log comparison showing combat events