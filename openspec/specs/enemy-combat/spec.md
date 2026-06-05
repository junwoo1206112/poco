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

### Requirement: Weapon damage from bomb clears

The system SHALL deal damage to the enemy when a bomb detonates and clears linkable tiles, treating bomb clears as "weapon clears."

#### Scenario: Red bomb detonation damages enemy

- **WHEN** a Red Bomb detonates and clears N linkable tiles
- **THEN** the enemy SHALL take `N * 10` damage
- **AND** the damage SHALL be presented as a visible damage number toward the enemy
- **AND** the play log SHALL record a `weapon_damage` combat event with the damage amount

#### Scenario: Blue bomb detonation damages enemy

- **WHEN** a Blue Bomb detonates and clears N linkable tiles
- **THEN** the enemy SHALL take `N * 10` damage
- **AND** the damage SHALL be presented as a visible damage number toward the enemy

#### Scenario: Rainbow bomb detonation damages enemy

- **WHEN** a Rainbow Bomb detonates and clears N linkable tiles
- **THEN** the enemy SHALL take `N * 10` damage
- **AND** the damage SHALL be presented as a visible damage number toward the enemy

#### Scenario: Weapon clear can defeat enemy

- **WHEN** weapon damage from a bomb detonation reduces enemy HP to 0
- **THEN** the enemy SHALL be defeated and the defeat bonus SHALL be awarded
- **AND** the next enemy SHALL spawn

### Requirement: Enemy HP scaling for weapon damage

The system SHALL apply a 1.5x HP multiplier to all enemies to compensate for the increased DPS from weapon damage.

#### Scenario: Balance profile applies weapon damage HP multiplier

- **WHEN** a balance profile is active with `RegularEnemyHpMultiplier` of 1.5
- **THEN** regular enemy HP SHALL be multiplied by 1.5

#### Scenario: Boss HP scales with weapon damage multiplier

- **WHEN** a balance profile is active with `BossHpMultiplier` of 1.5
- **THEN** boss enemy HP SHALL be multiplied by 1.5
