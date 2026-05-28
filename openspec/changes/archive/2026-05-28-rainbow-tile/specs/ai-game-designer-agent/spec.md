## MODIFIED Requirements

### Requirement: Rainbow bomb telemetry

The AI Game Designer Agent SHALL receive extended board telemetry including the number of Rainbow bombs cleared.

#### Scenario: Agent receives rainbow bomb usage data

- **WHEN** the board asks the agent for analysis after a round with Rainbow bomb detonations
- **THEN** the agent receives a `BoardTelemetry` value that includes `RainbowCleared` count

### Requirement: Rainbow event logging

The system SHALL record Rainbow bomb detonation events in the play log.

#### Scenario: Runtime records rainbow clear event

- **WHEN** a Rainbow bomb is detonated
- **THEN** the play log records a `rainbow_cleared` event with the gained score
