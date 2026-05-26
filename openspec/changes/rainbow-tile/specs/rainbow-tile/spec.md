## ADDED Requirements

### Requirement: Rainbow tile as wildcard

The system SHALL support a Rainbow tile subtype that acts as a wildcard, allowing chain linking between any adjacent tile types.

#### Scenario: Rainbow tile bridges two different types

- **WHEN** the player drags from a Red tile to an adjacent Rainbow tile, then to a Blue tile
- **THEN** the chain includes all three tiles (Red, Rainbow, Blue) as a valid chain of length 3

#### Scenario: Rainbow tile starts a chain

- **WHEN** the player starts a drag on a Rainbow tile and drags to an adjacent Green tile
- **THEN** the Rainbow tile is added as the first tile, and the Green tile is added as the second tile

#### Scenario: Rainbow tile is in the middle of a chain

- **WHEN** the player has a chain of Yellow → Rainbow and drags to a Purple tile adjacent to the Rainbow
- **THEN** the Purple tile is added to the chain regardless of Yellow and Purple being different types

### Requirement: Rainbow tile visual

The system SHALL render Rainbow tiles with a distinct multicolor gradient pattern so players can instantly identify them.

#### Scenario: Rainbow tile appears on the board

- **WHEN** a tile is configured as Rainbow subtype
- **THEN** it renders with a horizontal rainbow gradient (red → yellow → green → blue → purple) and no single-color tint

#### Scenario: Rainbow tile is selected or hinted

- **WHEN** a Rainbow tile is selected or hinted during drag
- **THEN** its visual changes (scale/overlay) similar to normal tiles but the rainbow gradient remains visible

### Requirement: Rainbow tile scoring bonus

The system SHALL apply a 1.5x score multiplier when a cleared chain contains at least one Rainbow tile.

#### Scenario: Chain with rainbow scores more

- **WHEN** the player clears a chain of 4 tiles that includes a Rainbow tile
- **THEN** the base score is `(4 × 4 × 10) × 1.5 = 240` before combo multiplier

#### Scenario: Chain without rainbow scores normally

- **WHEN** the player clears a chain of 4 tiles with no Rainbow tile
- **THEN** the base score is `(4 × 4 × 10) = 160` before combo multiplier
