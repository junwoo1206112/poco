## MODIFIED Requirements

### Requirement: Same-type drag linking

The system SHALL allow the player to drag across 6-direction adjacent same-type tiles to build a chain, OR drag through a Rainbow tile which bridges any two types.

#### Scenario: Player links matching adjacent tiles

- **WHEN** the player drags from one tile to a 6-direction adjacent tile of the same type
- **THEN** the second tile is added to the active chain

#### Scenario: Player drags through a Rainbow tile

- **WHEN** the player has a Red tile selected and drags to an adjacent Rainbow tile
- **THEN** the Rainbow tile is added to the chain regardless of type mismatch

#### Scenario: Player drags from a Rainbow tile to a non-matching tile

- **WHEN** the player has a Rainbow tile in the chain and drags to an adjacent non-Rainbow tile of any type
- **THEN** the non-Rainbow tile is added to the chain

#### Scenario: Player backtracks before commitment

- **WHEN** the player has 1-2 tiles in the active chain and drags back to the previous tile
- **THEN** the last tile is removed from the active chain (back-drag still works below 3)

#### Scenario: Rainbow chain commits on release

- **WHEN** the player releases a chain containing 3 or more linked tiles (with or without Rainbow)
- **THEN** the chain commits as a valid clear

### Requirement: Scoring with rainbow bonus

The system SHALL apply a 1.5× score multiplier for chains containing at least one Rainbow tile, before the combo multiplier.

#### Scenario: Rainbow chain scores bonus

- **WHEN** the player clears a chain that includes a Rainbow tile
- **THEN** the base score formula is `(chainLength × chainLength × 10) × 1.5`

#### Scenario: Rainbow chain has combo

- **WHEN** the player clears a Rainbow-inclusive chain with an active combo
- **THEN** the score is `(chainLength × chainLength × 10) × 1.5 × comboMultiplier`
