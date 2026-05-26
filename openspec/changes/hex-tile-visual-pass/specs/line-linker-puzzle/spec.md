## ADDED Requirements

### Requirement: Hex-shaped tile presentation

The system SHALL render each tile as a hex-shaped sprite whose visual edges match the odd-row offset hex grid adjacency.

#### Scenario: Board displays hex tiles

- **WHEN** the prototype scene enters Play mode
- **THEN** each tile is rendered as a hex-shaped sprite with flat-color fill matching its tile type color

#### Scenario: Player clicks a hex tile

- **WHEN** the player clicks or drags on a tile
- **THEN** the click hit detection covers the hex silhouette area without significant dead zones outside the hex boundary

### Requirement: Circle-in-hex tile presentation

The system SHALL support a CLI-selected tile visual where each tile renders as a colored circle inside a visible hexagonal frame.

#### Scenario: CLI creates circle-in-hex tiles

- **WHEN** `create-core-board` is run with `--tileVisual circle-in-hex`
- **THEN** the generated prototype scene stores the circle-in-hex tile visual style
- **AND** each runtime tile renders as a colored circle inside a hexagonal frame

#### Scenario: Hex frames join into a puzzle board

- **WHEN** the prototype scene enters Play mode with the default 4x13 circle-in-hex board
- **THEN** adjacent hex frames visually meet as one connected 3-4-3-4 puzzle-board silhouette instead of separated individual tiles

#### Scenario: Row-offset hex grid matches pointy-top geometry

- **WHEN** the board uses alternating 3-4 rows with horizontal row offsets
- **THEN** the tile silhouette, collider, visual placement, and six-direction adjacency use matching pointy-top hex geometry

#### Scenario: Collapse preserves 3-4-3-4 silhouette

- **WHEN** tiles are cleared and the board collapses/refills
- **THEN** tiles only occupy valid cells for each row
- **AND** the alternating 3-4-3-4 board silhouette is preserved

#### Scenario: Core tiles keep stable color-shape identity

- **WHEN** the prototype spawns normal core tiles
- **THEN** each tile type uses one stable color and one stable shape
- **AND** random special block tinting does not create same-shape different-color normal tiles
