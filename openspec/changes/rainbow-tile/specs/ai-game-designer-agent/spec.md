## MODIFIED Requirements

### Requirement: Rainbow bomb telemetry

The AI Game Designer Agent SHALL receive extended board telemetry including the number of Rainbow bombs cleared.

#### Scenario: Agent receives rainbow bomb usage data

- **WHEN** the board asks the agent for analysis after a round with rainbow bomb detonations
- **THEN** the agent receives a `BoardTelemetry` value that includes `RainbowCleared` count

### Requirement: Rainbow-aware suggestions

The AI Game Designer Agent SHALL consider Rainbow bomb usage when suggesting difficulty labels and tuning.

#### Scenario: Player frequently uses rainbow bombs

- **WHEN** the play log shows Rainbow bombs were cleared multiple times
- **THEN** the agent labels the board as Easy and suggests lowering rainbow gauge charge speed

#### Scenario: Player never uses rainbow bombs

- **WHEN** the play log shows no Rainbow bombs were ever cleared
- **THEN** the agent can suggest improving rainbow bomb visibility, gauge placement, or charge pacing

### Requirement: Rainbow event logging

The system SHALL record Rainbow bomb events in the play log.

#### Scenario: Runtime records rainbow ready event

- **WHEN** the rainbow gauge creates a Rainbow bomb
- **THEN** the play log records a `rainbow_ready` event

#### Scenario: Runtime records rainbow clear event

- **WHEN** a Rainbow bomb is detonated
- **THEN** the play log records a `rainbow_cleared` event with the gained score
