## MODIFIED Requirements

### Requirement: Same-type drag linking

The system SHALL allow the player to drag across 6-direction adjacent same-type linkable tiles to build a chain.

#### Scenario: Player links matching adjacent tiles

- **WHEN** the player drags from one tile to a 6-direction adjacent tile of the same type
- **THEN** the second tile is added to the active chain

#### Scenario: Player cannot link through rainbow as a normal tile

- **WHEN** a rainbow bomb exists on the board
- **THEN** it is not added to a drag chain as a wildcard tile

#### Scenario: Player detonates rainbow bomb

- **WHEN** the player taps or starts dragging on a rainbow bomb
- **THEN** the rainbow bomb detonates and removes every linkable tile of the most common color
