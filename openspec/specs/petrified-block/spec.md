## Purpose

Defines the Petrified block type with unique immunity rules and bottom-row clear behavior.

## Requirements

### Requirement: Petrified block type

The system SHALL support a Petrified block subtype that cannot be linked, cannot be destroyed by adjacent matching, cannot be destroyed by Red or Blue bombs, cannot be destroyed by Rainbow bombs, and clears only after reaching the bottom row by gravity.

#### Scenario: Petrified block is not linkable

- **WHEN** a tile has `PokoBlockSubtype.Petrified`
- **THEN** the tile SHALL NOT be included in a chain drag selection
- **AND** `IsLinkable` SHALL return false for that tile

#### Scenario: Adjacent matching cannot damage petrified block

- **WHEN** a normal chain clears beside a Petrified block
- **THEN** the Petrified block SHALL remain on the board
- **AND** it SHALL NOT lose durability

#### Scenario: Bomb cannot destroy petrified block

- **WHEN** a Red Bomb or Blue Bomb detonates and its affected area includes a Petrified block
- **THEN** the Petrified block SHALL NOT be cleared
- **AND** the bomb effect SHALL skip the Petrified block without damage

#### Scenario: Rainbow bomb cannot destroy petrified block

- **WHEN** a Rainbow Bomb detonates and targets a color that includes Petrified blocks of that color
- **THEN** the Petrified blocks SHALL NOT be cleared
- **AND** only linkable tiles and same-type Stone tiles of the target color SHALL be removed

#### Scenario: Petrified block clears at bottom row

- **WHEN** gravity causes a Petrified block to reach the bottom valid row in its column
- **THEN** the Petrified block SHALL be cleared
- **AND** the clear SHALL score 40 points
- **AND** remaining movable tiles SHALL compact/refill after the clear

### Requirement: Petrified block visual distinction

The Petrified block SHALL have a distinct visual appearance that differentiates it from Frozen and Stone blocks.

#### Scenario: Player sees a Petrified block

- **WHEN** a Petrified block is on the board
- **THEN** it SHALL render with a purple-tinted fill and a cracked-gem or chain icon
- **AND** the visual SHALL be clearly distinguishable from the gray Stone block and blue Frozen block
