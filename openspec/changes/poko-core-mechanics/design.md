## Context

The current prototype has a functional hex grid line-linker with timed rounds, but it's missing the core tension loop that defines PokoPang's gameplay identity. Analysis of PokoPang gameplay footage (NHN Media Day) reveals three interlocking systems absent from the prototype:

1. **Irreversible one-stroke** — chains commit at 3+ with no undo; the current prototype allows back-drag to cancel.
2. **Combo + Fever** — consecutive clears build combo, and at 7 combo Fever triggers neighbor-cascade clears.
3. **Enemy combat** — an enemy entity with HP sits above the board; cleared tiles launch projectiles that deal damage.

Adding these transforms the prototype from a generic match-3 feel into the specific hybrid puzzle-action loop that Trinode's flagship uses. The AI Game Designer Agent also needs combat telemetry to give relevant tuning advice.

## Goals / Non-Goals

**Goals:**
- Irreversible one-stroke: chain commits automatically at 3+ linked tiles; back-drag no longer cancels past 3 tiles.
- Combo counter: consecutive clears within 2.5s increment combo (1→2→3...). Reset on timeout or invalid short chain.
- Fever mode: at 7 combo, every cleared tile also clears its 1-ring hex neighbors for 6s. Score multiplied by 2x during Fever.
- Enemy combat: enemy entity with 100 base HP. Clear launches a projectile dealing `chainLength * 10` damage. Enemy defeated = 5000 bonus score.
- Bomb generation: 7+ chain drops Red Bomb (6-direction line clear on manual trigger or auto after 5s). 10+ chain drops Blue Bomb (3x3 radius clear).
- Special blocks: Frozen (must clear adjacent tile), Stone (falls to bottom row on collapse, auto-clears), Clock (+2s when cleared).
- AI agent extended to track combat telemetry and suggest tuning for combo/enemy parameters.
- Portfolio evidence in `md/portfolio-milestones/poko-core-mechanics.md`.

**Non-Goals:**
- No animal companion system.
- No currency/economy (cherries, blueberries, diamonds).
- No recruitment mechanic.
- No new art assets — all visuals remain runtime-generated placeholders with color-coding.

## Decisions

### Irreversible commit at 3+, not instant-on-any-selection
- **Decision**: Chain commits instantly when `selectedTiles.Count >= 3` on drag continuation. Player can still back-drag up to tile 2 but not below 3.
- **Rationale**: Matches PokoPang where you commit at 3 and the chain fires. Allows brief correction window (tiles 1-2) before commitment.
- **Alternatives considered**: Commit at 2 (too fast, no correction window), commit at 4 (deviates from PokoPang).

### Combo window as timer, not move-based
- **Decision**: 2.5-second real-time window between consecutive clears. Resets on invalid short chain.
- **Rationale**: Aligns with PokoPang's speed-based 60s round. Move-based combo would break the time-pressure feel.
- **Trade-off**: Timer can be interrupted by enemy animations; keep timer running during enemy hit.

### Enemy as HP bar, not a timer-gated entity
- **Decision**: Enemy has fixed HP (100). Each clear fires a projectile. No time-gates or invulnerability phases.
- **Rationale**: Simple, testable, and immediately satisfying. Gate difficulty through HP scaling, not mechanics.
- **Trade-off**: No boss-phase variety. Can be extended later with special enemy abilities (color-swap, freeze).

### Bombs as board-placeable tiles, not inventory items
- **Decision**: Bombs appear as special tiles on the board after long-chain clears. Player can tap to trigger or they auto-detonate after 5s.
- **Rationale**: Keeps focus on the board. Inventory UI is scope creep. Auto-detonate prevents board clogging.
- **Trade-off**: Less strategic depth than inventory bombs, but sufficient for portfolio prototype.

### Special blocks as tile subtypes, not separate objects
- **Decision**: `PokoTile` gains a `BlockSubtype` enum (None, Frozen, Stone, Clock). Visual tint and behavior vary by subtype.
- **Rationale**: Reuses existing tile rendering, collider, and grid system. No new GameObject types needed.
- **Trade-off**: Subtype logic adds branching in tile methods. Mitigate with helper methods.

## Risks / Trade-offs

- [Risk] Irreversible commit feels punishing if adjacency detection is too strict → Mitigation: Pre-commit hint glow stays active, player sees valid targets clearly.
- [Risk] Fever cascade could cause performance spikes on large clears → Mitigation: Limit cascade to 1-ring neighbors only (not recursive). Batch visual updates.
- [Risk] Enemy HP bar placement could overlap board → Mitigation: Use screen-space canvas bar above the board, sized proportionally.
- [Risk] Bombs auto-detonating at 5s might surprise the player → Mitigation: Show a shrinking visual timer on the bomb tile (2s → flash at 1s).

## Portfolio Evidence

After implementation:
1. Play through a full round (clear or fail) confirming irreversible one-stroke, combo counter, fever trigger, enemy damage, and bomb drops.
2. Log file captures combat events (combo, fever, enemy damage, bomb triggers).
3. CLI `analyze-playlog` report includes combat telemetry.
4. CLI `generate-level` uses combat metrics for tuning suggestions.
5. Milestone note at `md/portfolio-milestones/poko-core-mechanics.md` summarizes what was implemented and what a reviewer can play.
