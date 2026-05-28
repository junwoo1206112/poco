## Purpose

Defines combo counter and Fever mode mechanics for consecutive chain clears.

## Requirements

### Requirement: Combo counter

The system SHALL track consecutive valid clears within a 2.5-second window as a combo count, and reset the combo on timeout or invalid short chain.

#### Scenario: Player clears two chains in quick succession

- **WHEN** the player clears a valid chain and clears another valid chain within 2.5 seconds
- **THEN** the combo counter increments (1→2) and the HUD shows the current combo count

#### Scenario: Player waits too long between clears

- **WHEN** more than 2.5 seconds pass after a clear without another clear
- **THEN** the combo counter resets to 0

#### Scenario: Player makes an invalid short chain

- **WHEN** the player releases 1-2 tiles
- **THEN** the combo counter resets to 0

### Requirement: Fever mode

The system SHALL trigger Fever mode when the combo counter reaches 7, and during Fever every cleared tile also destroys its 1-ring hex neighbors.

#### Scenario: Combo reaches 7

- **WHEN** the combo counter reaches 7 during Fever-inactive state
- **THEN** Fever mode activates for 6 seconds, score is doubled, and a visual indicator shows Fever is active

#### Scenario: Tile cleared during Fever

- **WHEN** a tile is cleared during Fever mode
- **THEN** each of its 1-ring hex neighbors (all 6 directions, if a valid tile exists) is also destroyed without scoring additional points

#### Scenario: Fever expires

- **WHEN** 6 seconds have elapsed since Fever activation
- **THEN** Fever mode ends, score multiplier returns to 1x, and the visual indicator clears
