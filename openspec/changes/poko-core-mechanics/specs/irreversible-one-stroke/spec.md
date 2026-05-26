## ADDED Requirements

### Requirement: Irreversible chain commitment

The system SHALL commit and execute a chain automatically when the linked tile count reaches 3 or more, with no undo or back-drag cancellation below 3 tiles.

#### Scenario: Chain reaches 3 tiles during drag

- **WHEN** the player is dragging and the active chain reaches 3 linked tiles
- **THEN** the chain commits immediately — tiles 1-2 correction is still allowed, but once at 3+ the chain fires (clear, score, collapse, refill) without requiring finger-lift

#### Scenario: Player backtracks before 3 tiles

- **WHEN** the player has linked 1 or 2 tiles and drags back to the previous tile
- **THEN** the last tile is removed from the active chain (standard back-drag behavior)

#### Scenario: Chain exceeds 3 tiles

- **WHEN** the player extends beyond 3 tiles before the commit fires
- **THEN** the chain fires with all linked tiles at 3+ (the chain does not fire at every increment, only once selection stabilizes or on release)
