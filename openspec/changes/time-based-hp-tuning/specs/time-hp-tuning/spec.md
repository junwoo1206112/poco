## ADDED Requirements

### Requirement: HP suggestion from play log analysis

The system SHALL compute suggested regular enemy HP and boss HP from the timeLeft/TimeLimit ratio of a cleared play session.

#### Scenario: Clear with excess time

- **WHEN** `PlayLogAnalysis.Evaluate()` detects `Result` is `"clear"` and `timeLeft / TimeLimit > 0.5`
- **THEN** `SuggestedBossHp` SHALL be `baseBossHp × 1.3` and `SuggestedRegularEnemyHp` SHALL be `baseRegularEnemyHp × 1.3`

#### Scenario: Clear with tight time

- **WHEN** `PlayLogAnalysis.Evaluate()` detects `Result` is `"clear"` and `timeLeft / TimeLimit < 0.2`
- **THEN** `SuggestedBossHp` SHALL be `baseBossHp × 0.8` and `SuggestedRegularEnemyHp` SHALL be `baseRegularEnemyHp × 0.8`

#### Scenario: Normal clear time

- **WHEN** `PlayLogAnalysis.Evaluate()` detects `Result` is `"clear"` and `timeRatio` is between 0.2 and 0.5 (inclusive)
- **THEN** `SuggestedBossHp` and `SuggestedRegularEnemyHp` SHALL be 0 (no suggested change)

#### Scenario: Failed session

- **WHEN** `Result` is not `"clear"`
- **THEN** `SuggestedBossHp` and `SuggestedRegularEnemyHp` SHALL be 0

### Requirement: AgentSuggestion HP fields

The AgentSuggestion struct SHALL include `SuggestedRegularEnemyHp` and `SuggestedBossHp` fields.

#### Scenario: Agent creates a suggestion

- **WHEN** any code creates an `AgentSuggestion` value
- **THEN** `SuggestedRegularEnemyHp` and `SuggestedBossHp` SHALL be present with a default of 0

### Requirement: PokoLevelConfig HP overrides

`PokoLevelConfig` SHALL support optional regular enemy HP and boss HP override fields.

#### Scenario: Level config is generated

- **WHEN** the retune or experiment pipeline creates a `PokoLevelConfig`
- **THEN** `regularEnemyHp` and `bossHp` fields SHALL be present, defaulting to 0 (use Excel baseline)

### Requirement: HP in experiment variants

Experiment variant generation SHALL apply HP deltas relative to the control variant.

#### Scenario: Readability variant

- **WHEN** `BuildExperimentVariants()` creates the readability variant
- **THEN** the readability variant SHALL set `regularEnemyHp` and `bossHp` to 80% of the control suggested values

#### Scenario: Combo variant

- **WHEN** `BuildExperimentVariants()` creates the combo variant
- **THEN** the combo variant SHALL set `regularEnemyHp` and `bossHp` to 120% of the control suggested values

### Requirement: HP in experiment comparison report

The experiment comparison report SHALL include each variant's HP values.

#### Scenario: Comparison report is generated

- **WHEN** `CompareLevelExperiments()` writes the comparison report
- **THEN** the report SHALL include `regularEnemyHp` and `bossHp` for each candidate variant
