# Portfolio Milestone: PokoPang Core Mechanics

## What Was Implemented

This milestone adds the core gameplay systems that define PokoPang's distinctive hybrid puzzle-action loop:

### Irreversible One-Stroke
- Chain commits automatically at 3+ linked tiles (no back-drag cancel)
- Matches PokoPang's core linking rule where 3 tiles is the commitment threshold
- Back-drag correction is still allowed for 1-2 tile selections

### Combo + Fever System
- Consecutive clears within 2.5 seconds increment a combo counter (displayed on HUD)
- At 7 combo, **Fever Mode** activates for 6 seconds with 2x score multiplier
- During Fever, every cleared tile also destroys its 1-ring hex neighbors (non-recursive cascade)
- Combo resets on timeout or invalid short chain

### Enemy Combat
- Enemy entity with 100 HP appears above the board with a visible HP bar
- Each valid chain clear launches a projectile dealing `chainLength * 10` damage
- Defeating the enemy awards a 5000 score bonus
- Enemy HP bar is rendered as a screen-space UI element between the score panel and the board

### Bomb Generation
- Clearing 7+ tiles in one stroke spawns a **Red Bomb** on the board
- Clearing 10+ tiles spawns a **Blue Bomb**
- Tap a bomb to detonate it manually, or it auto-detonates after 5 seconds
- Red Bomb clears all tiles in 6-direction straight lines
- Blue Bomb clears tiles in a 3x3 radius
- Bomb detonation awards 50 points per cleared tile

### Special Blocks
- **Frozen Block** (blue tint): Must be adjacent-cleared — cleared when any adjacent tile is destroyed
- **Stone Block** (gray tint): Cannot be linked; falls to bottom row on collapse and auto-clears
- **Clock Block** (green tint): Cleared by chain or bomb, adds +2 seconds to the round timer
- Special blocks appear with ~6% probability during board generation and ~8% during refill

### AI Designer Agent Updates
- `HeuristicGameDesignerAgent` now considers combo, fever, enemy HP, and damage dealt
- New difficulty labels: `Fever Master`, `Combat Focus`, `Boss Down`, `Combo Warm`
- `PlayLogAnalysis` parses combat events (combo, fever, enemy damage, bomb events)
- CLI report includes combat telemetry section

## Files Changed

| File | Change |
|------|--------|
| `Assets/PokoPuzzle/Scripts/Core/LineLinkerBoard.cs` | Major: new fields, CommitChain(), combo/fever/bomb/enemy methods, modified Update/EndDrag/OnGUI |
| `Assets/PokoPuzzle/Scripts/Core/PokoTile.cs` | Added BlockSubtype, BombType, IsBomb, IsLinkable, ConfigureBomb/ConfigureSubtype, special block visuals |
| `Assets/PokoPuzzle/Scripts/Core/PokoBlockType.cs` | **New**: PokoBlockSubtype enum (None, Frozen, Stone, Clock) |
| `Assets/PokoPuzzle/Scripts/Core/BoardEnemy.cs` | **New**: BoardEnemy class with HP, damage, defeat bonus |
| `Assets/PokoPuzzle/Scripts/Core/BoardBomb.cs` | **New**: BombType enum, BoardBomb static class with Red/Blue detonation patterns |
| `Assets/PokoPuzzle/Scripts/AI/BoardTelemetry.cs` | Extended: added Combo, FeverActive, EnemyHp, TotalDamageDealt, BombsCleared, SpecialBlocksCleared |
| `Assets/PokoPuzzle/Scripts/AI/HeuristicGameDesignerAgent.cs` | Updated: combat-aware difficulty heuristics |
| `Assets/PokoPuzzle/Scripts/Editor/PokoPuzzleCli.cs` | Updated: PlayLogAnalysis with combat metrics, report with combat telemetry |

## What a Reviewer Can Play

1. Open Unity and enter Play mode in the prototype scene
2. **Irreversible one-stroke**: Drag across 3+ same-color tiles — the chain commits and clears immediately without needing to release
3. **Combo**: Clear two chains within 2.5s to see the combo counter on the HUD
4. **Fever**: Reach 7 combo to trigger Fever — watch the board cascade as tiles destroy neighbors
5. **Enemy**: See the enemy HP bar above the board; each clear deals damage; defeat the enemy for 5000 bonus points
6. **Bombs**: Drag a long chain (7+ tiles) to spawn a bomb, then tap it to detonate
7. **Special blocks**: Watch for frozen (blue), stone (gray), and clock (green) blocks with distinct behaviors

## Evidence Location

- Play logs: `md/playtest-logs/latest-playtest.jsonl` (contains combat events)
- Agent analysis: `md/agent-reports/latest-playtest-analysis.md` (includes combat telemetry)
- CLI command: `tools\poko-cli.cmd analyze-playlog` to generate combat-aware report
