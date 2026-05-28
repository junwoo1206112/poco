## Purpose

Defines enemy entity with HP bar, damage-on-clear mechanics, and combat telemetry recording.

## Requirements

### Requirement: Enemy entity

The system SHALL render an enemy with an HP bar above the board at round start, and SHALL apply damage when the player clears tiles.

#### Scenario: Round starts with an enemy

- **WHEN** a new round begins
- **THEN** an enemy entity with 100 base HP appears above the board with a visible HP bar

#### Scenario: Player clears a valid chain

- **WHEN** the player clears a chain of 3 or more tiles
- **THEN** a projectile launches toward the enemy dealing `chainLength * 10` damage, and the enemy HP bar updates

#### Scenario: Enemy HP reaches 0

- **WHEN** enemy HP reaches 0
- **THEN** the enemy is defeated, a score bonus of 5000 is awarded, and a visual indicator shows victory

### Requirement: Combat telemetry

The system SHALL record combat events in the play log for later analysis.

#### Scenario: Combat event is logged

- **WHEN** the player damages the enemy or defeats it
- **THEN** the play log records the event with damage dealt, current enemy HP, and combo state

#### Scenario: Round ends with enemy alive

- **WHEN** time runs out or moves are exhausted and the enemy is still alive
- **THEN** the end-state log includes remaining enemy HP
