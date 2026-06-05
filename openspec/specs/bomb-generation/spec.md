## Purpose

Defines bomb tile generation on long chains and bomb detonation mechanics.

## Requirements

### Requirement: Bomb tile generation

The system SHALL place a bomb tile on the board when the player clears a chain of 7 or more tiles, with bomb type based on chain length. The bomb SHALL spawn at the chain end position, falling back to a random empty cell if the preferred position is unavailable.

#### Scenario: Player clears 7-9 tiles

- **WHEN** the player clears a chain of 7 to 9 tiles
- **THEN** a Red Bomb tile appears at the last tile position of the chain
- **AND** if that position is unavailable, a random valid empty position on the board is used

#### Scenario: Player clears 10+ tiles

- **WHEN** the player clears a chain of 10 or more tiles
- **THEN** a Blue Bomb tile appears at the last tile position of the chain
- **AND** if that position is unavailable, a random valid empty position on the board is used

### Requirement: Bomb detonation

A bomb tile SHALL detonate either on player tap, or automatically after 5 seconds, affecting tiles in a pattern based on bomb type. Frozen tiles SHALL thaw, Stone tiles SHALL be destroyed, and Petrified tiles SHALL NOT be affected by bomb detonation.

#### Scenario: Player taps a Red Bomb

- **WHEN** the player taps a Red Bomb tile
- **THEN** the bomb detonates, clearing all linkable tiles in its 6-direction straight lines
- **AND** Frozen tiles in the blast area SHALL thaw back to normal animal tiles
- **AND** Stone tiles in the blast area SHALL be destroyed immediately
- **AND** Petrified tiles in the blast area SHALL NOT be cleared or damaged
- **AND** the clear remains effective from edges or corners because the lines continue until the board boundary

#### Scenario: Player taps a Blue Bomb

- **WHEN** the player taps a Blue Bomb tile
- **THEN** the bomb pulls affected nearby hex tiles toward the bomb center before detonation
- **AND** the bomb affects the origin-adjacent 2-ring hex burst area
- **AND** Frozen tiles in the blast area SHALL thaw back to normal animal tiles
- **AND** Stone tiles in the blast area SHALL be destroyed immediately
- **AND** Petrified tiles in the blast area SHALL NOT be cleared or damaged
- **AND** the clear can lose affected area near edges or corners because off-board radius cells do not exist

#### Scenario: Bomb auto-detonates

- **WHEN** a bomb tile has existed on the board for 5 seconds without being tapped
- **THEN** it auto-detonates with the same effect as a manual tap

#### Scenario: Bomb clear scores points

- **WHEN** a bomb detonates and clears linkable tiles or Stone tiles
- **THEN** each cleared tile scores 50 points regardless of chain length

#### Scenario: Bomb clear feeds rainbow gauge

- **WHEN** a bomb detonates and clears linkable tiles
- **THEN** the cleared linkable tile count SHALL contribute to the rainbow gauge fill
