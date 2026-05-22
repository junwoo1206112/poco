## Purpose

Defines the AI Game Designer Agent contract: structured telemetry input, tuning suggestion output, portfolio evidence, level config generation, playtest log analysis, LLM review, experiment planning/comparison/promotion, and designer loop status reporting.

## Requirements

### Requirement: Board telemetry input

The AI Game Designer Agent SHALL receive structured board telemetry including board size, tile type count, possible chain count, longest chain estimate, score, and moves used.

#### Scenario: Agent analyzes the board

- **WHEN** the board asks the agent for analysis
- **THEN** the agent receives a `BoardTelemetry` value with the current board and play summary

### Requirement: Tuning suggestion output

The AI Game Designer Agent SHALL output a difficulty label, readable summary, suggested move limit, suggested target score, and suggested tile type count.

#### Scenario: Board has many available links

- **WHEN** the telemetry indicates high chain density or a long available chain
- **THEN** the agent labels the board easy and suggests stricter next-level tuning

#### Scenario: Board has few available links

- **WHEN** the telemetry indicates low chain density or very short chains
- **THEN** the agent labels the board hard and suggests softer next-level tuning

### Requirement: Portfolio evidence

The AI Game Designer Agent SHALL expose its reasoning in a reviewer-readable form through in-game text, Markdown report, JSON report, or another durable artifact.

#### Scenario: Reviewer inspects the project

- **WHEN** a reviewer opens the project documentation
- **THEN** they can find what the AI agent analyzed, what it suggested, and why that matters for puzzle tuning

### Requirement: Level config generation

The AI Game Designer Agent SHALL convert a tuning suggestion into a Unity-readable level configuration artifact.

#### Scenario: Agent creates a next-level proposal

- **WHEN** the CLI asks the agent to generate a level
- **THEN** the project receives a `PokoLevelConfig` asset with board size, layout, tile type count, move limit, target score, and spawn weights

#### Scenario: Reviewer inspects generated level data

- **WHEN** a reviewer opens the generated level report
- **THEN** they can see the source telemetry, agent judgment, generated Unity asset path, and the reason for the suggested tuning

#### Scenario: Agent applies a generated level

- **WHEN** the CLI applies a generated level to the prototype scene
- **THEN** the scene `LineLinkerBoard` references the generated `PokoLevelConfig` and mirrors its board size, layout, tile type count, move limit, and target score

### Requirement: Playtest log analysis

The AI Game Designer Agent SHALL analyze runtime play logs and produce reviewer-readable tuning feedback.

#### Scenario: Runtime records play telemetry

- **WHEN** the player attempts chains during Play mode
- **THEN** the game records session, move, score, chain length, and end-state events to a playtest log

#### Scenario: Agent analyzes playtest telemetry

- **WHEN** the CLI analyzes a playtest log
- **THEN** it outputs a Markdown and JSON report with result, final score, move count, valid move count, invalid short release count, average chain length, and next tuning action

#### Scenario: Agent retunes a level from playtest telemetry

- **WHEN** the CLI retunes a level from a playtest log
- **THEN** it creates a new `PokoLevelConfig` asset whose move limit, target score, tile type count, and spawn weights are based on the playtest result

### Requirement: Optional LLM designer review

The AI Game Designer Agent SHALL expose an optional LLM review workflow that reads the deterministic playtest analysis contract and saves durable review evidence.

#### Scenario: API key is unavailable

- **WHEN** the CLI requests an LLM designer review without `OPENAI_API_KEY`
- **THEN** it saves the Responses request packet and a pending Markdown report without blocking the deterministic tuning loop

#### Scenario: API key is available

- **WHEN** the CLI requests an LLM designer review with a usable API key
- **THEN** it saves the LLM-written Markdown review and raw response JSON under `md/llm-reports/`

### Requirement: Level experiment planning

The AI Game Designer Agent SHALL convert a playtest diagnosis into a small comparison set of level experiment candidates.

#### Scenario: Agent plans a next play pass

- **WHEN** the CLI plans level experiments from a playtest log
- **THEN** it creates control, readability, and combo-focused `PokoLevelConfig` assets for comparison

#### Scenario: Reviewer reads the experiment plan

- **WHEN** the experiment CLI finishes
- **THEN** it saves Markdown and JSON reports with each candidate hypothesis, measurement focus, and generated asset path

### Requirement: Experiment result comparison

The AI Game Designer Agent SHALL preserve level-specific play logs and compare planned experiment candidates after they are played.

#### Scenario: Experiment candidate is played

- **WHEN** Play mode starts with a generated level candidate
- **THEN** the runtime log writes both the latest playtest log and a level-specific latest log path for later comparison

#### Scenario: Agent compares candidate logs

- **WHEN** the CLI compares control, readability, and combo experiment logs
- **THEN** it saves Markdown and JSON comparison reports with candidate metrics and a recommended next variant

### Requirement: Experiment winner promotion

The AI Game Designer Agent SHALL promote the recommended experiment candidate into a durable next baseline and milestone artifact.

#### Scenario: Agent promotes a winner

- **WHEN** the CLI promotes an experiment winner from candidate logs
- **THEN** it creates a promoted `PokoLevelConfig` asset and a promotion report for the chosen variant

#### Scenario: Agent prepares portfolio evidence

- **WHEN** promotion completes
- **THEN** it saves a milestone Markdown note that explains what a reviewer can play, what the AI designer decided, and which evidence supports that decision

### Requirement: Designer loop status

The AI Game Designer Agent SHALL report the current evidence state and next action for the tuning loop.

#### Scenario: Loop is missing play telemetry

- **WHEN** the status CLI finds no latest playtest log
- **THEN** it reports that Play mode telemetry is the next required step

#### Scenario: Loop can advance

- **WHEN** the status CLI finds analysis, experiment, comparison, or promotion evidence
- **THEN** it reports the next CLI or play step and saves Markdown and JSON status artifacts
