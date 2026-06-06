## Purpose

Defines the playable Line-Linker puzzle board: runtime generation, hex grid, drag linking, chain clear/collapse/refill, scoring with combo multiplier, HUD, win/fail states, hex-shaped tile visuals, and input handling for the Poko Engine CLI Puzzle Framework prototype.

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

The system SHALL allow the player to drag across 6-direction adjacent same-type linkable tiles to build a preview chain. A chain SHALL commit when the player releases with 3 or more linked tiles, preserving readable drag preview behavior for the prototype.

#### Scenario: Player links matching adjacent tiles

- **WHEN** the player drags from one tile to a 6-direction adjacent tile of the same type
- **THEN** the second tile is added to the active chain and the visible link path updates

#### Scenario: Player drags over a non-matching tile

- **WHEN** the player drags from one tile to an adjacent tile of a different type
- **THEN** the non-matching tile is not added to the active chain

#### Scenario: Player backtracks before commitment

- **WHEN** the player has 1-2 tiles in the active chain and drags back to the previous tile
- **THEN** the last tile is removed from the active chain

#### Scenario: Chain commits on release

- **WHEN** the player releases an active chain with 3 or more linked tiles
- **THEN** the chain commits irreversibly and executes clear, score, collapse, and refill

#### Scenario: Player reads valid next tiles

- **WHEN** a same-type chain is active
- **THEN** adjacent same-type tiles that can extend the current chain are hinted near the current chain end

#### Scenario: Player cannot link through rainbow as a normal tile

- **WHEN** a rainbow bomb exists on the board
- **THEN** it is not added to a drag chain as a wildcard tile

#### Scenario: Player detonates rainbow bomb

- **WHEN** the player taps or starts dragging on a rainbow bomb
- **THEN** the rainbow bomb detonates and removes every linkable tile plus same-type Stone tile of the targeted color

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

### Requirement: Input System compatibility

The system SHALL use Unity Input System APIs for pointer input.

#### Scenario: Project uses Input System package

- **WHEN** the project active input handling is set to Input System package
- **THEN** the puzzle accepts mouse or touch pointer input without using `UnityEngine.Input`

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

- **WHEN** the prototype scene enters Play mode with the default 7x7 circle-in-hex board
- **THEN** adjacent hex frames visually meet as one connected PokoPang-like staggered puzzle-board silhouette instead of separated individual tiles

#### Scenario: Row-offset hex grid matches pointy-top geometry

- **WHEN** the board uses full-width rows with alternating horizontal row offsets
- **THEN** the tile silhouette, collider, visual placement, and six-direction adjacency use matching pointy-top hex geometry

#### Scenario: Collapse preserves staggered board silhouette

- **WHEN** tiles are cleared and the board collapses/refills
- **THEN** tiles only occupy valid cells for each row
- **AND** the full-width staggered board silhouette is preserved

#### Scenario: Core tiles keep stable color-shape identity

- **WHEN** the prototype spawns normal core tiles
- **THEN** each tile type uses one stable color and one stable shape
- **AND** random special block tinting does not create same-shape different-color normal tiles
