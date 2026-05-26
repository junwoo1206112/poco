## ADDED Requirements

### Requirement: Excel-authored gameplay data conversion

The system SHALL support one Excel `.xlsx` workbook for enemy, boss, enemy skill, and balance profile data, and convert that data into runtime-safe Unity assets.

#### Scenario: Convert enemy boss skill and profile Excel workbook

- **WHEN** the editor conversion menu is run
- **THEN** the project reads `GameData.xlsx`
- **AND** normal unit data comes from the `Enemy` sheet
- **AND** boss data comes from the `Boss` sheet
- **AND** enemy skill data comes from the `Skill` sheet
- **AND** balance profile data comes from the `BalanceProfile` sheet
- **AND** writes `PokoRegularEnemyDatabase`, `PokoEnemyDatabase`, `PokoEnemySkillDatabase`, and `PokoBalanceProfileDatabase` assets under `Assets/PokoPuzzle/Data/Resources/`

#### Scenario: Add edit and delete rows in Excel

- **WHEN** a designer adds or edits enabled rows in `GameData.xlsx`
- **THEN** the next conversion reflects those rows in the generated databases
- **WHEN** a designer deletes a row or sets `Enabled` to `FALSE`, `0`, `NO`, `OFF`, or `DELETE`
- **THEN** the next conversion excludes that row from the generated databases

### Requirement: Runtime uses injected data providers

The runtime board SHALL receive gameplay data through injected provider assets instead of opening `.xlsx` files during play.

#### Scenario: Board initializes from databases

- **GIVEN** the prototype scene has enemy, regular enemy, skill, and balance profile database references
- **WHEN** the board starts
- **THEN** round settings are applied from `PokoLevelConfig` or serialized board fields
- **AND** boss HP, normal enemy HP, defeat bonus, and skills are applied from the injected providers
- **AND** balance profile presets are available as converted Unity assets for designer-agent tuning
- **AND** no runtime spreadsheet file read is required
