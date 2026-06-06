# Portfolio Milestone: Poko Core Mechanics

## What Was Implemented

This milestone adds the core gameplay systems that support the portfolio loop: playable Line-Linker action, combat pressure, telemetry, and AI designer analysis.

The current round format follows a Poko-style 60-second score attack: the player keeps linking until time expires, and the target score is evaluated at the end of the round. The first playable target is now `10000`, based on the full-round score of `13720`.

### Release-Committed Chain

- Chain previews during drag and commits when the player releases 3+ linked tiles.
- Back-drag correction remains available before release.
- This keeps the prototype readable and easy to verify while preserving the 3+ clear threshold.

### Combo + Fever System

- Consecutive clears within 2.5 seconds increment a combo counter on the HUD.
- At 7 combo, Fever Mode activates for 6 seconds with a 2x score multiplier.
- During Fever, every cleared tile also destroys its 1-ring hex neighbors.
- Combo resets on timeout or invalid short chain.

### Enemy Combat

- Enemy entity appears above the board with a visible HP bar.
- Each valid chain clear deals `chainLength * 10` damage.
- Defeating the enemy awards the configured defeat score bonus.
- Enemy HP is included in play-log combat events.

### Bomb Generation

- Clearing 7+ tiles in one stroke spawns a Red Bomb.
- Clearing 10+ tiles spawns a Blue Bomb.
- Tap a bomb to detonate it manually, or let it auto-detonate after 5 seconds.
- Red Bomb clears 6-direction lines.
- Blue Bomb clears a local radius.
- Bomb detonation awards 50 points per cleared tile.

### Special Blocks

- Frozen Block: cannot be linked; clears when an adjacent tile is destroyed.
- Stone Block: cannot be linked; resists adjacent chain clears, and is destroyed only by Red/Blue bombs or a same-type Rainbow clear.
- Petrified Block: cannot be linked, resists normal/bomb/Rainbow removal, falls with movable tiles, and clears for +40 after settling at the bottom valid row.
- The board repairs empty playable cells after special-block clears and collapse/refill so the staggered hex silhouette stays filled.

### AI Designer Agent Updates

- `HeuristicGameDesignerAgent` considers combo, fever, enemy HP, damage dealt, and rainbow usage.
- `PlayLogAnalysis` parses combat events.
- CLI/Markdown reports include combat telemetry.

## Files Changed

| File | Change |
|------|--------|
| `Assets/PokoPuzzle/Scripts/Core/LineLinkerBoard.cs` | Main gameplay loop: drag/release, score, combo, fever, enemy, bombs, special blocks, play logging |
| `Assets/PokoPuzzle/Scripts/Core/PokoTile.cs` | Block subtype, bomb, rainbow, linkability, special visuals |
| `Assets/PokoPuzzle/Scripts/Core/PokoBlockType.cs` | `PokoBlockSubtype` enum |
| `Assets/PokoPuzzle/Scripts/Core/BoardEnemy.cs` | Enemy HP, damage, defeat bonus |
| `Assets/PokoPuzzle/Scripts/Core/BoardBomb.cs` | Red/Blue bomb detonation patterns |
| `Assets/PokoPuzzle/Scripts/AI/BoardTelemetry.cs` | Combat telemetry fields |
| `Assets/PokoPuzzle/Scripts/AI/HeuristicGameDesignerAgent.cs` | Combat-aware difficulty heuristics |
| `Assets/PokoPuzzle/Scripts/Editor/PokoPuzzleCli.cs` | Play-log analysis and combat telemetry reports |

## What a Reviewer Can Play

1. Open Unity and enter Play mode in the prototype scene.
2. Drag across 3+ same-color tiles, then release to clear, score, damage the enemy, collapse, and refill.
3. Clear multiple chains quickly to see combo/Fever behavior.
4. Watch the enemy HP bar respond to clears.
5. Try long chains to create and detonate bombs.
6. Observe Frozen, Stone, and Petrified special block behavior.

## Evidence Location

- Play logs: `md/playtest-logs/latest-playtest.jsonl`
- Agent analysis: `md/agent-reports/latest-playtest-analysis.md`
- Loop status: `md/designer-loop/latest-status.md`
- CLI command: `tools\poko-cli.cmd analyze-playlog`

## Current Evidence Gap

The latest implementation now aligns the special-block rules with runtime tests. The next evidence pass should be one fresh Unity Play session that captures a full round with special blocks, bomb/Rainbow clears, and a final `end` event for portfolio proof.
