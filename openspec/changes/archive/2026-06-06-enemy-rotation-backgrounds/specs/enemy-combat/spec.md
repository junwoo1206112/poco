## ADDED Requirements

### Requirement: Enemy rotation backgrounds

The system SHALL associate each Excel regular enemy and boss wave with a background Resources path and SHALL update the playable board background when that enemy becomes active.

#### Scenario: Regular enemy spawns

- **WHEN** the rotation spawns a regular enemy from the Excel-backed regular enemy database
- **THEN** the board background SHALL transition to the enemy row's `BackgroundPath` asset

#### Scenario: Boss spawns

- **WHEN** the rotation spawns a boss from the Excel-backed boss database
- **THEN** the board background SHALL transition to the boss row's `BackgroundPath` asset

#### Scenario: Background asset is missing

- **WHEN** a background Resources path is blank or cannot be loaded
- **THEN** gameplay SHALL continue without blocking enemy spawn or player input
