## ADDED Requirements

### Requirement: 60-second countdown timer

The system SHALL run a 60-second countdown timer that starts when the board is ready and ends the round when time reaches 0.

#### Scenario: Timer counts down during play

- **WHEN** the board is generated and the round starts
- **THEN** the timer begins counting down from 60 seconds

#### Scenario: Time runs out

- **WHEN** the timer reaches 0
- **THEN** the round ends, the board evaluates score against target score, and no more input is accepted

### Requirement: Timer HUD display

The system SHALL show the remaining time on the HUD.

#### Scenario: Player reads time

- **WHEN** the prototype is played
- **THEN** remaining time in seconds is shown on the HUD alongside score
