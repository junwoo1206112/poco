# Design: Separate Rainbow Bomb and Fever Gauge

## Overview

Rainbow is treated as a separate bomb reward, while the top-right charge meter is Fever. Normal clears fill the Fever gauge; when full, Fever mode starts. Rainbow bombs are created separately from long chains and remove all linkable tiles of the most common color when detonated.

## Decisions

### Fever Gauge

- **Decision**: Use the HUD charge meter for Fever, not Rainbow.
- **Rationale**: This better matches the Poko-style reading of a charge bar leading into Fever Time.

### Rainbow as BombType

- **Decision**: Keep Rainbow as `BombType.Rainbow`, not `PokoBlockSubtype.Rainbow`.
- **Rationale**: Rainbow is a separate board-clearing tool, not a normal link tile.

### Rainbow Generation

- **Decision**: Create Rainbow bombs from long-chain rewards rather than Fever gauge completion.
- **Rationale**: This keeps Fever pacing and Rainbow utility independent.

### Most-common Color Targeting

- **Decision**: The Rainbow bomb removes every linkable tile of the most common tile type.
- **Rationale**: This approximates "remove all same-colored blocks" without a color-selection UI.
