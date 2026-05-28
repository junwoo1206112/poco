# Design: Rainbow Gauge Bomb

## Overview

Rainbow is implemented as a charged bomb rather than a normal tile. Normal chain clears fill `rainbowGauge`; when it reaches `RainbowGaugeMax`, the board creates a `BombType.Rainbow` in an empty cell. The player detonates that bomb to clear all linkable tiles of the most common color.

## Decisions

### Rainbow as BombType

- **Decision**: Use `BombType.Rainbow`, not `PokoBlockSubtype.Rainbow`.
- **Rationale**: The desired behavior is a charged board-clearing bomb, not a wildcard link tile.

### Player-activated detonation

- **Decision**: Rainbow bombs do not use the normal 5-second bomb auto-detonation timer.
- **Rationale**: A charged rainbow bomb should feel like a player-owned tactical tool.

### Most-common color targeting

- **Decision**: The rainbow bomb removes every linkable tile of the most common tile type.
- **Rationale**: This approximates "remove all same-colored blocks" without requiring the player to choose a color UI yet.

### Gauge overflow

- **Decision**: When a bomb spawns, gauge overflow is preserved. If no empty spawn cell is available, the gauge stays full.
- **Rationale**: The player should not lose gauge progress because the board has not collapsed/refilled yet.

## Risks

- [Risk] Rainbow bomb spawns during the clear-before-collapse window and may visually move during collapse.
  - Mitigation: It is still a board tile and participates in the normal collapse flow.
- [Risk] Most-common auto-targeting may feel less intentional than color selection.
  - Mitigation: Keep this as the prototype baseline; add color selection later only if playtests need more agency.
