## ADDED Requirements

### Requirement: Hex grid ASCII visualization

The system SHALL render the puzzle board as an ASCII hex grid with odd-row offset layout in CLI Markdown reports.

#### Scenario: analyze-board includes hex grid

- **WHEN** the `analyze-board` CLI command writes its Markdown report
- **THEN** the report includes a code-fenced ASCII hex grid showing tile positions, a tile type legend, board dimensions, and seed

#### Scenario: generate-level includes hex grid

- **WHEN** the `generate-level` CLI command writes its Markdown report
- **THEN** the report includes a code-fenced ASCII hex grid showing tile positions

### Requirement: Tile type symbol mapping

The system SHALL map each PokoTileType to a distinct Unicode symbol in the ASCII grid.

#### Scenario: Five tile types render distinctly

- **WHEN** the grid uses the default 5 tile types
- **THEN** each of the five types appears as a different Unicode symbol (● ◆ ■ ▲ ⬟)

#### Scenario: Legend explains symbol mapping

- **WHEN** the grid is rendered
- **THEN** a legend line maps each symbol to its tile type name
