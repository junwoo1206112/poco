## ADDED Requirements

### Requirement: Combat telemetry input

The AI Game Designer Agent SHALL receive extended board telemetry including combat metrics: combo count, fever active flag, enemy HP, total damage dealt, bombs cleared, and special blocks cleared.

#### Scenario: Agent analyzes board with combat state

- **WHEN** the board asks the agent for analysis
- **THEN** the agent receives a `BoardTelemetry` value that includes `Combo`, `FeverActive`, `EnemyHp`, `TotalDamageDealt`, `BombsCleared`, and `SpecialBlocksCleared`

### Requirement: Combat-aware tuning suggestions

The AI Game Designer Agent SHALL consider combat metrics when suggesting difficulty labels and next-level tuning.

#### Scenario: Player frequently triggers Fever

- **WHEN** the play log shows the player reached Fever (7+ combo) in multiple rounds
- **THEN** the agent labels the board as Easy and suggests increasing enemy HP or reducing combo time window

#### Scenario: Player never reaches Fever

- **WHEN** the play log shows the player never exceeded 3 combo
- **THEN** the agent labels the board as Hard and suggests lowering enemy starting HP or increasing combo time window

#### Scenario: Player defeats enemy early

- **WHEN** the enemy is defeated before 30% of the round time has elapsed
- **THEN** the agent labels the board as Easy and suggests increasing enemy HP

### Requirement: Extended play log events

The system SHALL record combat events (combo increment, fever start/end, enemy damage, bomb placement/detonation, special block clear) in the play log.

#### Scenario: Runtime records combat event

- **WHEN** a combat event occurs (combo change, fever state, enemy damage, bomb event)
- **THEN** the play log records the event with relevant context (combo value, fever active, enemy HP, bomb type)

#### Scenario: Play log analysis includes combat

- **WHEN** the CLI analyzes a play log
- **THEN** the analysis report includes combat metrics: max combo reached, fever triggers, total damage dealt, bombs generated/used, special blocks cleared
