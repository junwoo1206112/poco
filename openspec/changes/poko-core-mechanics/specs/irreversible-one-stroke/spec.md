## ADDED Requirements

### Requirement: Release-committed chain execution

The system SHALL let the player preview and correct a chain during drag, then commit and execute the chain when the player releases three or more linked tiles.

#### Scenario: Player releases a valid chain

- **WHEN** the player releases a chain containing three or more linked tiles
- **THEN** the chain commits once, clears the selected tiles, applies score/combat effects, collapses the board, and refills empty spaces

#### Scenario: Player backtracks before release

- **WHEN** the player drags back to the immediately previous tile before releasing
- **THEN** the last tile is removed from the active chain

#### Scenario: Player releases a short chain

- **WHEN** the player releases a chain containing one or two tiles
- **THEN** the board remains unchanged and the player receives invalid-chain feedback
