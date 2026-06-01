## Purpose

Defines bomb tile generation on long chains and bomb detonation mechanics.

## Requirements

### Requirement: Bomb tile generation

The system SHALL place a bomb tile on the board when the player clears a chain of 7 or more tiles, with bomb type based on chain length.

#### Scenario: Player clears 7-9 tiles

- **WHEN** the player clears a chain of 7 to 9 tiles
- **THEN** a Red Bomb tile appears at a random valid position on the board

#### Scenario: Player clears 10+ tiles

- **WHEN** the player clears a chain of 10 or more tiles
- **THEN** a Blue Bomb tile appears at a random valid position on the board

### Requirement: Bomb detonation

A bomb tile SHALL detonate either on player tap, or automatically after 5 seconds, destroying tiles in a pattern based on bomb type.

#### Scenario: Player taps a Red Bomb

- **WHEN** the player taps a Red Bomb tile
- **THEN** the bomb detonates, clearing all tiles in its 6-direction straight lines (up/down/left/right and two diagonal hex directions)
- **AND** the clear remains effective from edges or corners because the lines continue until the board boundary

#### Scenario: Player taps a Blue Bomb

- **WHEN** the player taps a Blue Bomb tile
- **THEN** the bomb pulls affected nearby hex tiles toward the bomb center before detonation
- **AND** the bomb clears the origin-adjacent 2-ring hex burst area
- **AND** the clear can lose affected area near edges or corners because off-board radius cells do not exist

#### Scenario: Bomb auto-detonates

- **WHEN** a bomb tile has existed on the board for 5 seconds without being tapped
- **THEN** it auto-detonates with the same effect as a manual tap

#### Scenario: Bomb clear scores points

- **WHEN** a bomb detonates and clears tiles
- **THEN** each cleared tile scores 50 points regardless of chain length
