## 1. Data Model Changes

- [x] 1.1 Add `PokoBlockSubtype` enum to `PokoBlockType.cs` (None, Frozen, Stone, Clock)
- [x] 1.2 Add `BlockSubtype` field to `PokoTile` with visual tint methods
- [x] 1.3 Add combat fields to `BoardTelemetry` (Combo, FeverActive, EnemyHp, TotalDamageDealt, BombsCleared, SpecialBlocksCleared)
- [x] 1.4 Add combat fields to `AgentSuggestion` if tuning suggestions reference combo/enemy parameters
- [x] 1.5 Keep chain commitment single-shot through `EndDrag()` and `CommitChain()`

## 2. Release-Committed Chain Execution

- [x] 2.1 Keep chain preview active during drag and commit 3+ chains on release
- [x] 2.2 Preserve back-drag removal before release
- [x] 2.3 Ensure `EndDrag()` commits valid chains once and rejects short chains
- [x] 2.4 Update link hints and line refresh to work with release-committed flow

## 3. Combo Counter and Fever

- [x] 3.1 Add `comboCount` field and `lastClearTime` field to `LineLinkerBoard`
- [x] 3.2 Add `feverActive`, `feverTimer` fields and `feverDuration` constant (6s)
- [x] 3.3 Implement combo logic on valid clear: check if within 2.5s of last clear, then increment or reset combo
- [x] 3.4 Implement combo reset on invalid short chain
- [x] 3.5 Implement combo timeout in `Update()`: if `lastClearTime + 2.5s < Time.time`, reset combo
- [x] 3.6 Implement Fever trigger at combo == 7: set `feverActive`, `feverTimer = 6f`, score multiplier = 2x
- [x] 3.7 Implement Fever neighbor-cascade: when clearing tiles during Fever, also destroy all 1-ring neighbors (non-recursive)
- [x] 3.8 Implement Fever expiry in `Update()`: decrement `feverTimer`, clear active at 0
- [x] 3.9 Add combo and fever visual indicators to HUD (OnGUI)
- [x] 3.10 Add combo multiplier to scoring formula: `(N*N*10) * (feverActive ? 2 : Mathf.Max(1, comboCount))`

## 4. Enemy Combat

- [x] 4.1 Create `BoardEnemy.cs` with HP, maxHP, damage method, and data-driven defeat bonus
- [x] 4.2 Add enemy instance to `LineLinkerBoard` with HP bar rendering (screen-space or world-space)
- [x] 4.3 Implement damage on valid chain clear: deal `chainLength * 10` damage
- [x] 4.4 Implement enemy defeat: award bonus score and visual indicator
- [x] 4.5 Add enemy HP bar to HUD panel

## 5. Bomb Generation

- [x] 5.1 Create `BoardBomb.cs` with bomb type (Red/Blue), timer, detonation method
- [x] 5.2 After chain clear, if `chainLength >= 7`, place Red Bomb at random board position
- [x] 5.3 After chain clear, if `chainLength >= 10`, place Blue Bomb (overrides Red)
- [x] 5.4 Implement bomb tap detection: if tapped tile is a bomb, detonate instead of linking
- [x] 5.5 Implement Red Bomb detonation: clear all tiles in 6-direction straight lines
- [x] 5.6 Implement Blue Bomb detonation: clear all tiles in 3x3 radius
- [x] 5.7 Implement 5-second auto-detonation timer in `Update()`
- [x] 5.8 Add bomb visual and shrinking/timer feedback

## 6. Special Blocks

- [x] 6.1 Add `BlockSubtype` field to `PokoTile`, initialize random subtype during board generation
- [x] 6.2 Implement Frozen block behavior: mark as unlinkable; when adjacent tile cleared, destroy frozen block
- [x] 6.3 Implement Stone block behavior: unlinkable; on collapse, fall to bottom of column; auto-clear on bottom row
- [x] 6.4 Implement Clock block behavior: when cleared (by chain or bomb), add +2s to round time
- [x] 6.5 Add visual tint for each subtype on the tile sprite
- [x] 6.6 Log special block clears as explicit play-log events

## 7. Combat Telemetry and Logging

- [x] 7.1 Add combat event logging: fever start/end, enemy damage, bomb place/detonate, rainbow events, enemy skill
- [x] 7.2 Extend play log JSON events with combat fields
- [x] 7.3 Extend `PlayLogAnalysis` to parse and summarize combat metrics
- [x] 7.4 Extend `HeuristicGameDesignerAgent.Analyze()` to use combat telemetry for tuning
- [x] 7.5 Extend CLI analysis reports to include combat metrics
- [x] 7.6 Update `BoardTelemetry` construction to include combat fields

## 8. Unity Verification

- [x] 8.1 Open Unity, confirm compilation succeeds with no errors
- [x] 8.2 Enter Play mode and verify release-committed 3+ chain behavior
- [x] 8.3 Verify combo counter increments on consecutive clears within 2.5s
- [x] 8.4 Verify Fever triggers at 7 combo with neighbor-cascade effect
- [x] 8.5 Verify enemy appears with HP bar and takes damage from clears
- [x] 8.6 Capture one full 60-second score-attack round with an `end` event
- [x] 8.7 Verify bombs drop from 7+/10+ chains and detonate on tap or after 5s
- [x] 8.8 Verify special blocks (frozen, stone, clock) behave correctly
- [x] 8.9 Verify no console errors during extended play
- [x] 8.10 Run `tools\poko-cli.cmd analyze-playlog` and confirm combat metrics in report

## 9. Portfolio Evidence

- [x] 9.1 Write portfolio milestone document `md/portfolio-milestones/poko-core-mechanics.md`
- [x] 9.2 Capture full-round play log showing combat events
- [x] 9.3 Capture after-fix play log showing `clear` when target score is reached by time end
