## Purpose

Defines bomb tile spawn position rules — chain end placement with random fallback.

## Requirements

### Requirement: Bomb spawns at chain end position

The system SHALL place bomb tiles at the last tile position of the chain that created them, falling back to a random empty position only when the chain end is unavailable.

#### Scenario: Red bomb spawns at chain end

- **WHEN** the player clears a chain of 7 to 9 tiles
- **THEN** the Red Bomb SHALL be placed at the grid position of the last tile in the chain
- **AND** if that position is occupied or outside the board, the bomb SHALL be placed at a random empty position

#### Scenario: Blue bomb spawns at chain end

- **WHEN** the player clears a chain of 10 or more tiles
- **THEN** the Blue Bomb SHALL be placed at the grid position of the last tile in the chain
- **AND** if that position is occupied or outside the board, the bomb SHALL be placed at a random empty position

#### Scenario: Chain end position is occupied after clear

- **WHEN** the chain end position is not empty after tile clearing (e.g., due to simultaneous effects)
- **THEN** the bomb SHALL fall back to a random empty cell on the board
- **AND** the fallback placement SHALL be logged in the play log
