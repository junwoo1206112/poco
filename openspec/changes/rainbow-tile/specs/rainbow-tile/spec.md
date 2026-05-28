## ADDED Requirements

### Requirement: Rainbow gauge bomb

The system SHALL fill a rainbow gauge as blocks are cleared and create a Rainbow bomb when the gauge reaches its threshold.

#### Scenario: Gauge fills from clears

- **WHEN** the player clears blocks through normal chain play
- **THEN** the rainbow gauge increases based on the number of cleared blocks

#### Scenario: Rainbow bomb is created

- **WHEN** the rainbow gauge reaches its threshold and an empty board cell is available
- **THEN** the board creates a `BombType.Rainbow` bomb in an empty cell and resets the gauge with any overflow preserved

#### Scenario: Rainbow bomb waits for player activation

- **WHEN** a rainbow bomb is on the board
- **THEN** it remains available until the player detonates it instead of auto-detonating on the normal bomb timer

### Requirement: Rainbow bomb detonation

The system SHALL make a Rainbow bomb remove all linkable tiles of the most common color on the board.

#### Scenario: Player detonates rainbow bomb

- **WHEN** the player detonates a rainbow bomb
- **THEN** the board identifies the most common linkable tile type and removes every linkable tile of that type

#### Scenario: Rainbow bomb scores

- **WHEN** a rainbow bomb removes tiles
- **THEN** the player gains score based on the removed tile count and the play log records a `rainbow_cleared` event

### Requirement: Rainbow bomb visual and HUD

The system SHALL render rainbow bombs with a distinct multicolor visual and show rainbow gauge progress in the HUD.

#### Scenario: Rainbow bomb appears on the board

- **WHEN** a tile is configured as `BombType.Rainbow`
- **THEN** it renders with a multicolor gradient pattern

#### Scenario: Rainbow gauge appears on the HUD

- **WHEN** the screen HUD is enabled
- **THEN** the HUD displays current rainbow gauge progress
