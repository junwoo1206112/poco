## MODIFIED Requirements

### Requirement: Tuning suggestion output

The AI Game Designer Agent SHALL output a difficulty label, readable summary, suggested move limit, suggested target score, suggested tile type count, suggested regular enemy HP, and suggested boss HP.

#### Scenario: Board has many available links

- **WHEN** the telemetry indicates high chain density or a long available chain
- **THEN** the agent labels the board easy and suggests stricter next-level tuning, including higher enemy HP values

#### Scenario: Board has few available links

- **WHEN** the telemetry indicates low chain density or very short chains
- **THEN** the agent labels the board hard and suggests softer next-level tuning, including lower enemy HP values

### Requirement: Level config generation

The AI Game Designer Agent SHALL convert a tuning suggestion into a Unity-readable level configuration artifact that optionally includes enemy HP overrides and a balance profile id.

#### Scenario: Agent creates a next-level proposal

- **WHEN** the CLI asks the agent to generate a level
- **THEN** the project receives a `PokoLevelConfig` asset with board size, layout, tile type count, move limit, target score, spawn weights, balance profile id, optional regular enemy HP, and optional boss HP

#### Scenario: Agent applies a generated level

- **WHEN** the CLI applies a generated level to the prototype scene
- **THEN** the scene `LineLinkerBoard` references the generated `PokoLevelConfig` and mirrors its board size, layout, tile type count, move limit, target score, and balance profile id

### Requirement: Playtest log analysis

The AI Game Designer Agent SHALL analyze runtime play logs and produce reviewer-readable tuning feedback including suggested enemy HP adjustments.

#### Scenario: Agent analyzes playtest telemetry

- **WHEN** the CLI analyzes a playtest log
- **THEN** it outputs a Markdown and JSON report with result, final score, move count, valid move count, invalid short release count, average chain length, suggested regular enemy HP, suggested boss HP, and next tuning action

#### Scenario: Agent retunes a level from playtest telemetry

- **WHEN** the CLI retunes a level from a playtest log
- **THEN** it creates a new `PokoLevelConfig` asset whose move limit, target score, tile type count, spawn weights, balance profile id, regular enemy HP, and boss HP are based on the playtest result

### Requirement: Level experiment planning

The AI Game Designer Agent SHALL convert a playtest diagnosis into a small comparison set of level experiment candidates with differentiated HP values and balance profiles.

#### Scenario: Agent plans a next play pass

- **WHEN** the CLI plans level experiments from a playtest log
- **THEN** it creates control, readability, and combo-focused `PokoLevelConfig` assets where each variant has distinct regular enemy HP, boss HP, and balance profile values

### Requirement: Experiment result comparison

The AI Game Designer Agent SHALL preserve level-specific play logs and compare planned experiment candidates including HP metrics after they are played.

#### Scenario: Agent compares candidate logs

- **WHEN** the CLI compares control, readability, and combo experiment logs
- **THEN** it saves Markdown and JSON comparison reports with candidate metrics including regular enemy HP, boss HP, and a recommended next variant
