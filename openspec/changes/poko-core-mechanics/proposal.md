## Why

The current prototype has a working hex Line-Linker board. The portfolio needs to show a focused product-engineering loop: playable puzzle action, combat pressure, telemetry, and an AI designer agent that turns play evidence into tuning suggestions. The project should stay close to the PokoPang-inspired feel without copying every exact timing rule or over-expanding into a broad match-3 framework.

## What Changes

- Keep **release-committed chain execution**: players can preview and backtrack during drag, then a 3+ chain commits once on release.
- Add a **combo counter**: consecutive clears within a brief window increment a combo multiplier. Combo resets on delay or failed short release.
- Add **Fever mode**: at 7 combo, Fever triggers; every cleared tile also destroys its 1-ring neighbors, creating cascading chain reactions.
- Add **enemy combat**: an enemy with an HP bar appears at the top. Each cleared chain damages the enemy. Defeating the enemy grants a configured score bonus.
- Add **bomb generation**: clearing 7+ tiles in one stroke drops a Red Bomb; clearing 10+ drops a Blue Bomb.
- Add **special blocks** (frozen, stone, clock) that appear at higher levels for variety.
- Update the **designer agent** to analyze combat telemetry (damage dealt, combos reached, fever triggers) and suggest tuning.

### Non-goals

- No animal companion system.
- No currency/economy system.
- No original PokoPang art, names, characters, or sounds; all assets remain runtime-generated placeholders.
- No exact clone of PokoPang input timing. This prototype keeps release-to-clear because it is readable, stable, and easier to verify.

## Capabilities

### New Capabilities

- `irreversible-one-stroke`: For this prototype, this change is scoped as release-committed one-stroke chain execution.
- `combo-fever`: Consecutive-clear combo counter that escalates into Fever mode at 7 combo with neighbor-cascade clearing.
- `enemy-combat`: Enemy entity with HP bar that receives damage from cleared chains; defeat grants score bonus.
- `bomb-generation`: Long-chain bombs: Red Bomb (7+) clears 6-direction line, Blue Bomb (10+) clears 3x3 radius.
- `special-blocks`: Frozen blocks (need adjacent clear), Stone blocks (fall to bottom), Clock blocks (+2s timer).

### Modified Capabilities

- `line-linker-puzzle`: Scoring includes combo multiplier, enemy damage, bomb clears, and special block effects while keeping the established release-to-clear drag interaction.
- `ai-game-designer-agent`: Telemetry and suggestions extended to cover combat metrics (combo, fever, enemy HP, bomb usage).

## Impact

- `Assets/PokoPuzzle/Scripts/Core/LineLinkerBoard.cs`: drag release, scoring, combat, combo, fever, bomb, special-block, and logging paths.
- `Assets/PokoPuzzle/Scripts/Core/PokoTile.cs`: subtype, bomb, rainbow, and visual behavior.
- `Assets/PokoPuzzle/Scripts/Core/BoardEnemy.cs`: enemy HP/damage/defeat bonus model.
- `Assets/PokoPuzzle/Scripts/Core/BoardBomb.cs`: bomb detonation patterns.
- `Assets/PokoPuzzle/Scripts/Core/PokoBlockType.cs`: special block subtype enum.
- `Assets/PokoPuzzle/Scripts/AI/BoardTelemetry.cs`: combat metrics.
- `Assets/PokoPuzzle/Scripts/AI/HeuristicGameDesignerAgent.cs`: combat-aware heuristics.
- `Assets/PokoPuzzle/Scripts/Editor/PokoPuzzleCli.cs`: play-log analysis, reports, level retuning, experiments.
- `md/agent-reports/` and `md/portfolio-milestones/`: reviewer-readable evidence.
