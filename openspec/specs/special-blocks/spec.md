## Purpose

Defines Frozen, Stone, and Petrified special block subtypes with distinct behaviors and visual appearance. Rainbow SHALL NOT be a regular block subtype; it is a separate bomb reward type.

## Requirements

### Requirement: Special block types

The system SHALL support Frozen, Stone, and Petrified block subtypes that appear on the board and have distinct behaviors. Rainbow SHALL NOT be a regular block subtype.

#### Scenario: Frozen block thaws

- **WHEN** any tile adjacent to a Frozen block is cleared, or a Red/Blue bomb hits a Frozen block
- **THEN** the Frozen block thaws back into its original animal tile
- **AND** the thaw scores 20 points

#### Scenario: Stone block is destroyed by bombs

- **WHEN** a Red or Blue bomb hits a Stone block
- **THEN** the Stone block is destroyed immediately
- **AND** the clear scores 50 points

#### Scenario: Stone block resists adjacent matching

- **WHEN** any tile adjacent to a Stone block is cleared by a normal chain
- **THEN** the Stone block is not damaged or cleared

#### Scenario: Stone block is destroyed by rainbow

- **WHEN** a Rainbow bomb targets the Stone block's tile type
- **THEN** the Stone block is destroyed immediately
- **AND** the clear scores 50 points

#### Scenario: Petrified block resists normal removal

- **WHEN** a Petrified block is adjacent to a normal clear or inside a Red/Blue/Rainbow bomb effect
- **THEN** the Petrified block is not cleared or damaged

#### Scenario: Petrified block clears at bottom

- **WHEN** gravity moves a Petrified block to the bottom valid row in its column
- **THEN** the Petrified block clears
- **AND** the clear scores 40 points

#### Scenario: Special subtype is created

- **WHEN** a tile is assigned Frozen, Stone, or Petrified
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

#### Scenario: Player sees a Petrified block

- **WHEN** a Petrified block is on the board
- **THEN** it renders with a purple-tinted fill and cracked-gem icon
