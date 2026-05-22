## Purpose

Defines the playable Line-Linker puzzle board: runtime generation, hex grid, drag linking, chain clear/collapse/refill, scoring, HUD, win/fail states, and input handling for the Poko Engine CLI Puzzle Framework prototype.

## Requirements

### Requirement: Runtime board generation

The system SHALL generate a hexagonal Line-Linker puzzle board at runtime with configurable width, height, tile type count, tile spacing, and layout.

#### Scenario: Prototype scene starts

- **WHEN** the prototype scene enters Play mode
- **THEN** the board contains a visible odd-row offset hex grid of colored tiles sized according to the configured dimensions

### Requirement: Hexagonal adjacency

The system SHALL use odd-row offset hex coordinates for default board placement and input adjacency.

#### Scenario: Even-row hex neighbors are checked

- **WHEN** a tile is on an even row
- **THEN** only its six valid hex neighbors are considered adjacent

#### Scenario: Odd-row hex neighbors are checked

- **WHEN** a tile is on an odd row
- **THEN** only its six valid hex neighbors are considered adjacent

### Requirement: Same-type drag linking

The system SHALL allow the player to drag across 6-direction adjacent same-type tiles to build a chain.

#### Scenario: Player links matching adjacent tiles

- **WHEN** the player drags from one tile to a 6-direction adjacent tile of the same type
- **THEN** the second tile is added to the active chain and the visible link path updates

#### Scenario: Player drags over a non-matching tile

- **WHEN** the player drags from one tile to an adjacent tile of a different type
- **THEN** the non-matching tile is not added to the active chain

#### Scenario: Player backtracks to previous tile

- **WHEN** the player drags from the current tile back to the immediately previous tile in the active chain
- **THEN** the current last tile is removed from the active chain

#### Scenario: Player reads valid next tiles

- **WHEN** a same-type chain is active
- **THEN** adjacent same-type tiles that can extend the current chain are hinted near the current chain end

### Requirement: Playable board availability

The system SHALL keep at least one same-type three-tile path available after initial board generation and refill.

#### Scenario: Random generation produces no clearable chain

- **WHEN** the generated or refilled board has no same-type connected area of three or more tiles
- **THEN** the board adjusts a neighboring three-tile path into a valid clearable chain

### Requirement: Chain clear and refill

The system SHALL clear chains of three or more linked tiles, update score, collapse remaining tiles, and refill empty cells.

#### Scenario: Player releases a valid chain

- **WHEN** the player releases a chain containing three or more tiles
- **THEN** those tiles are removed, score increases, remaining tiles collapse, and new tiles fill the board

#### Scenario: Player releases an invalid chain

- **WHEN** the player releases a chain containing fewer than three tiles
- **THEN** no tiles are removed and the chain selection is cleared

### Requirement: Clear feedback

The system SHALL show immediate readable feedback for successful clears and invalid short chains.

#### Scenario: Player clears a valid chain

- **WHEN** the player releases a chain containing three or more tiles
- **THEN** the game shows the score gained from that chain

#### Scenario: Player releases a short chain

- **WHEN** the player releases a chain containing one or two tiles
- **THEN** the game shows that at least three linked tiles are required

### Requirement: Prototype play HUD

The system SHALL keep score, move count, round state, and round-end recovery readable during prototype drag play.

#### Scenario: Player reads the live score

- **WHEN** the prototype is played in the Unity Game view
- **THEN** the score and remaining move count are shown as screen-space HUD information instead of overlapping the tile path

#### Scenario: Round reaches a clear or failed state

- **WHEN** drag input stops because the round reached a clear or failed state
- **THEN** the player sees the round-end state and can restart the prototype round from the HUD

### Requirement: Win and fail state

The system SHALL evaluate the level state using target score and move limit.

#### Scenario: Player reaches target score

- **WHEN** the score reaches or exceeds the target score
- **THEN** the board enters a level clear state and no longer accepts drag input

#### Scenario: Player runs out of moves

- **WHEN** the player has used all moves without reaching the target score
- **THEN** the board enters a failed state and no longer accepts drag input

### Requirement: Input System compatibility

The system SHALL use Unity Input System APIs for pointer input.

#### Scenario: Project uses Input System package

- **WHEN** the project active input handling is set to Input System package
- **THEN** the puzzle accepts mouse or touch pointer input without using `UnityEngine.Input`
