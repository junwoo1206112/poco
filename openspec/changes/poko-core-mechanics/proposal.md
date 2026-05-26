## Why

The current prototype has a working hex board with timed rounds, but the gameplay loop lacks the core tension that makes PokoPang distinctive — enemy combat, combo-driven fever, and irreversible one-stroke commitment. The YouTube Short (NHN Media Day) shows that PokoPang's identity is a hybrid of speed puzzle + action combat. Without these mechanics, the prototype feels like a generic match-3 rather than a PokoPang-style line-linker with combat urgency. Adding these now (before portfolio submission) closes the biggest gap with the target company's game.

## What Changes

- Replace optional back-drag with **irreversible one-stroke** — once 3+ tiles are linked, the chain commits instantly (no undo, matching PokoPang's core rule).
- Add a **combo counter** — consecutive clears within a brief window increment a combo multiplier. Combo resets on delay or failed short release.
- Add **Fever mode** — at 7 combo, Fever triggers: every cleared tile also destroys its 1-ring neighbors, creating cascading chain reactions.
- Add **enemy combat** — an enemy with an HP bar appears at the top. Each cleared chain launches a projectile that damages the enemy. Defeating the enemy grants a score bonus.
- Add **bomb generation** — clearing 7+ tiles in one stroke drops a Red Bomb (clears 6-direction straight line). Clearing 10+ drops a Blue Bomb (clears 3x3 radius).
- Add **special blocks** (frozen, stone, clock) that appear at higher levels for variety.
- Update the **designer agent** to analyze combat telemetry (damage dealt, combos reached, fever triggers) and suggest tuning.

### Non-goals

- No animal companion system (too large for portfolio scope; PokoPang's core is playable without it).
- No currency/economy system (cherries, blueberries, diamonds).
- No original PokoPang art, names, characters, or sounds — all assets remain runtime-generated placeholders.

## Capabilities

### New Capabilities

- `irreversible-one-stroke`: Chain commits at 3+ tiles with no back-drag undo, matching PokoPang's core linking rule.
- `combo-fever`: Consecutive-clear combo counter that escalates into Fever mode at 7 combo with neighbor-cascade clearing.
- `enemy-combat`: Enemy entity with HP bar that receives damage from cleared-tile projectiles; defeat grants score bonus.
- `bomb-generation`: Long-chain bombs — Red Bomb (7+) clears 6-direction line, Blue Bomb (10+) clears 3x3 radius.
- `special-blocks`: Frozen blocks (need adjacent clear), Stone blocks (fall to bottom), Clock blocks (+2s timer).

### Modified Capabilities

- `line-linker-puzzle`: Drag mechanic changes from cancellable to irreversible one-stroke. Scoring includes combo multiplier, enemy damage, and bomb clears.
- `ai-game-designer-agent`: Telemetry and suggestions extended to cover combat metrics (combo, fever, enemy HP, bomb usage).

## Impact

- `Assets/PokoPuzzle/Scripts/Core/LineLinkerBoard.cs` — major changes to drag logic, EndDrag, scoring, new combat/combo/fever state machines.
- `Assets/PokoPuzzle/Scripts/Core/PokoTile.cs` — new field for frozen/stone/clock subtypes, new visual tints.
- New file `Assets/PokoPuzzle/Scripts/Core/BoardEnemy.cs` — enemy entity with HP, projectile spawning.
- New file `Assets/PokoPuzzle/Scripts/Core/BoardBomb.cs` — bomb tile logic and detonation.
- New file `Assets/PokoPuzzle/Scripts/Core/ComboTracker.cs` — combo counter and fever state machine.
- New file `Assets/PokoPuzzle/Scripts/Core/PokoBlockType.cs` — enum for special block types.
- `Assets/PokoPuzzle/Scripts/AI/BoardTelemetry.cs` — new fields for combat metrics.
- `Assets/PokoPuzzle/Scripts/AI/AgentSuggestion.cs` — new suggestion fields for combat tuning.
- `Assets/PokoPuzzle/Scripts/AI/IGameDesignerAgent.cs` — extended interface if needed.
- `Assets/PokoPuzzle/Scripts/AI/HeuristicGameDesignerAgent.cs` — updated heuristics for combat data.
- `md/agent-reports/` — reports will include combat telemetry.
- `md/portfolio-milestones/` — new milestone doc for combat-combo-fever implementation.
