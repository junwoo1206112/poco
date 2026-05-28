## Purpose

Defines Frozen, Stone, and Clock special block subtypes with distinct behaviors and visual appearance. Rainbow SHALL NOT be a regular block subtype — it is a separate bomb reward type.

## Requirements

### Requirement: Special block types

The system SHALL support Frozen, Stone, and Clock block subtypes that appear on the board and have distinct behaviors. Rainbow SHALL NOT be a regular block subtype.

#### Scenario: Frozen block clears

- **WHEN** any tile adjacent to a Frozen block is cleared
- **THEN** the Frozen block is also cleared and scores 20 points

#### Scenario: Stone block falls

- **WHEN** tiles beneath a Stone block are cleared
- **THEN** the Stone block falls to the lowest empty row in its column, and if it reaches the bottom row it auto-clears scoring 30 points

#### Scenario: Clock block cleared

- **WHEN** a Clock block is cleared (by chain or bomb)
- **THEN** 2 seconds are added to the remaining round time

#### Scenario: Special subtype is created

- **WHEN** a tile is assigned Frozen, Stone, or Clock
- **THEN** its `BlockSubtype` reflects that special state and its tile behavior follows the subtype rules

#### Scenario: Rainbow is not a regular subtype

- **WHEN** a rainbow effect is created
- **THEN** it is represented as a separate `BombType.Rainbow` bomb reward, not as `PokoBlockSubtype.Rainbow`

### Requirement: Special block visual distinction

Each special block subtype SHALL have a distinct visual appearance so the player can identify it.

#### Scenario: Player sees a Frozen block

- **WHEN** a Frozen block is on the board
- **THEN** it renders with a blue-tinted border and ice crystal icon

#### Scenario: Player sees a Stone block

- **WHEN** a Stone block is on the board
- **THEN** it renders with a gray-tinted fill and rock icon

#### Scenario: Player sees a Clock block

- **WHEN** a Clock block is on the board
- **THEN** it renders with a green-tinted fill and clock icon
