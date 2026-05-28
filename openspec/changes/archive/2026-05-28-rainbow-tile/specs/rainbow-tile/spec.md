## ADDED Requirements

### Requirement: Rainbow bomb as separate special

The system SHALL support Rainbow as a separate bomb reward, independent from the Fever gauge.

#### Scenario: Rainbow bomb is created

- **WHEN** the player clears a long enough chain for a Rainbow reward
- **THEN** the board creates a `BombType.Rainbow` bomb in an empty cell

#### Scenario: Rainbow bomb waits for player activation

- **WHEN** a Rainbow bomb is on the board
- **THEN** it remains available until the player detonates it instead of auto-detonating on the normal bomb timer

### Requirement: Rainbow bomb detonation

The system SHALL make a Rainbow bomb remove all linkable tiles of the most common color on the board.

#### Scenario: Player detonates rainbow bomb

- **WHEN** the player detonates a Rainbow bomb
- **THEN** the board identifies the most common linkable tile type and removes every linkable tile of that type

#### Scenario: Rainbow bomb scores

- **WHEN** a Rainbow bomb removes tiles
- **THEN** the player gains score based on the removed tile count and the play log records a `rainbow_cleared` event
