## MODIFIED Requirements

### Requirement: Same-type drag linking

The system SHALL allow the player to drag across 6-direction adjacent same-type tiles to build a chain. A chain SHALL commit irreversibly once it reaches 3 or more linked tiles, matching PokoPang one-stroke behavior.

#### Scenario: Player links matching adjacent tiles

- **WHEN** the player drags from one tile to a 6-direction adjacent tile of the same type
- **THEN** the second tile is added to the active chain and the visible link path updates

#### Scenario: Player drags over a non-matching tile

- **WHEN** the player drags from one tile to an adjacent tile of a different type
- **THEN** the non-matching tile is not added to the active chain

#### Scenario: Player backtracks before commitment

- **WHEN** the player has 1-2 tiles in the active chain and drags back to the previous tile
- **THEN** the last tile is removed from the active chain

#### Scenario: Chain commits at 3

- **WHEN** the active chain reaches 3 linked tiles
- **THEN** the chain commits irreversibly and executes (clear, score, collapse, refill) without requiring finger-lift

#### Scenario: Player reads valid next tiles

- **WHEN** a same-type chain is active
- **THEN** adjacent same-type tiles that can extend the current chain are hinted near the current chain end

### Requirement: Scoring with combo multiplier

The system SHALL score cleared chains using a base formula multiplied by the current combo multiplier.

#### Scenario: Player scores a base clear

- **WHEN** the player clears a chain of N tiles with no combo
- **THEN** score increases by `N * N * 10`

#### Scenario: Player scores with combo

- **WHEN** the player clears a chain with combo C active
- **THEN** score increases by `(N * N * 10) * C`

#### Scenario: Player scores during Fever

- **WHEN** the player clears a chain during Fever mode
- **THEN** score increases by `(N * N * 10) * 2` (Fever multiplier overrides combo multiplier)

### Requirement: Win and fail state

The system SHALL evaluate the level state using target score, move limit, and round timer, and SHALL also check enemy HP as an optional win condition.

#### Scenario: Player reaches target score

- **WHEN** the score reaches or exceeds the target score
- **THEN** the board enters a level clear state and no longer accepts drag input

#### Scenario: Player runs out of moves

- **WHEN** the player has used all moves without reaching the target score
- **THEN** the board enters a failed state and no longer accepts drag input

#### Scenario: Player defeats the enemy

- **WHEN** the enemy HP reaches 0
- **THEN** a 5000 score bonus is awarded (this does not end the round; the player can continue playing)
