## ADDED Requirements

### Requirement: Rainbow tile telemetry

The AI Game Designer Agent SHALL receive extended board telemetry including the number of Rainbow tiles cleared.

#### Scenario: Agent receives rainbow usage data

- **WHEN** the board asks the agent for analysis after a round with rainbow tiles
- **THEN** the agent receives a `BoardTelemetry` value that includes `RainbowCleared` count

### Requirement: Rainbow-aware suggestions

The AI Game Designer Agent SHALL consider Rainbow tile usage when suggesting difficulty labels and tuning.

#### Scenario: Player frequently uses rainbows

- **WHEN** the play log shows Rainbow tiles were cleared in multiple chains
- **THEN** the agent labels the board as Easy and suggests lowering rainbow spawn probability

#### Scenario: Player never uses rainbows

- **WHEN** the play log shows no Rainbow tiles were ever cleared despite being present on the board
- **THEN** the agent suggests improving rainbow tile visibility or indication

### Requirement: Extended play log events

The system SHALL record Rainbow tile clear events in the play log.

#### Scenario: Runtime records rainbow event

- **WHEN** a chain containing a Rainbow tile is cleared
- **THEN** the play log records a `rainbow_cleared` event with the chain length
