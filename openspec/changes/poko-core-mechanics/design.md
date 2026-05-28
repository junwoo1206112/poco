## Context

The prototype now needs to prove the product-engineering loop, not chase unlimited mechanics. The important portfolio claim is that a reviewer can play a compact Line-Linker board, inspect telemetry, and see how a deterministic AI designer agent proposes the next tuning step.

## Goals / Non-Goals

**Goals:**
- Release-committed chain execution: the player previews a chain during drag and clears on release at 3+ tiles.
- Timed score attack: the round lasts 60 seconds; target score is evaluated at time end instead of ending the round immediately.
- Combo counter: consecutive clears within 2.5s increment combo. Reset on timeout or invalid short chain.
- Fever mode: at 7 combo, every cleared tile also clears its 1-ring hex neighbors for 6s. Score is multiplied by 2x during Fever.
- Enemy combat: enemy entity with HP. Clear deals `chainLength * 10` damage. Defeat grants the configured score bonus.
- Bomb generation: 7+ chain drops Red Bomb; 10+ chain drops Blue Bomb.
- Special blocks: Frozen, Stone, and Clock subtypes.
- AI agent extended to track combat telemetry and suggest tuning for combo/enemy parameters.
- Portfolio evidence in `md/portfolio-milestones/poko-core-mechanics.md`.

**Non-Goals:**
- No animal companion system.
- No currency/economy.
- No recruitment mechanic.
- No exact clone of PokoPang input timing.
- No new art assets; all visuals remain runtime-generated placeholders with color-coding.

## Decisions

### Release commit instead of automatic commit at 3

- **Decision**: Keep drag preview and backtracking until release. A 3+ chain commits once in `EndDrag()`.
- **Rationale**: This is easier to understand, easier to verify, and already matches the main `line-linker-puzzle` spec. It reduces input-edge bugs while preserving the 3+ clear threshold.
- **Alternative considered**: Auto-commit as soon as 3 tiles are linked. Deferred because it adds timing ambiguity and is not required to prove the AI designer loop.

### Combo window as timer, not move-based

- **Decision**: 2.5-second real-time window between consecutive clears. Reset on invalid short chain.
- **Rationale**: Aligns with the speed-puzzle feel. Move-based combo would weaken time pressure.

### Enemy as HP bar, not a timer-gated entity

- **Decision**: Enemy has HP and a configured defeat bonus. Each valid clear deals damage.
- **Rationale**: Simple, testable, and immediately legible. Difficulty can be tuned through HP and score targets.

### Bombs as board-placeable tiles, not inventory items

- **Decision**: Bombs appear as special tiles on the board after long-chain clears. Player can tap to trigger or wait for auto-detonation.
- **Rationale**: Keeps focus on the board and avoids inventory UI scope creep.

### Special blocks as tile subtypes, not separate objects

- **Decision**: `PokoTile` owns `PokoBlockSubtype` and related visual/behavior flags.
- **Rationale**: Reuses existing rendering, collider, and grid systems.

## Risks / Trade-offs

- [Risk] Fever cascade could cause broad clears that hide board readability issues. Mitigation: keep cascade non-recursive.
- [Risk] Enemy HP tuning may dominate score tuning. Mitigation: preserve HP suggestions separately in analysis JSON.
- [Risk] Bombs auto-detonating at 5s might surprise the player. Mitigation: keep timer feedback visible.
- [Risk] The current play log lacks an `end` event. Mitigation: require one full play session before promoting portfolio evidence.
- [Risk] Target score may be too low for Fever/bomb throughput. Mitigation: tune target score from full-round logs rather than short partial sessions.

## Portfolio Evidence

After implementation:
1. Play through a full round (clear or fail) confirming release-committed chain clearing, combo counter, fever trigger, enemy damage, and bomb drops.
2. Log file captures combat events.
3. CLI `analyze-playlog` report includes combat telemetry.
4. CLI `generate-level` or `retune-level` uses telemetry for tuning suggestions.
5. Milestone note at `md/portfolio-milestones/poko-core-mechanics.md` summarizes what was implemented and what a reviewer can play.
