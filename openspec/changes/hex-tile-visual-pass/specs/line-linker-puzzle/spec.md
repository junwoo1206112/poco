## ADDED Requirements

### Requirement: Hex-shaped tile presentation

The system SHALL render each tile as a hex-shaped sprite whose visual edges match the odd-row offset hex grid adjacency.

#### Scenario: Board displays hex tiles

- **WHEN** the prototype scene enters Play mode
- **THEN** each tile is rendered as a hex-shaped sprite with flat-color fill matching its tile type color

#### Scenario: Player clicks a hex tile

- **WHEN** the player clicks or drags on a tile
- **THEN** the click hit detection covers the hex silhouette area without significant dead zones outside the hex boundary
