## Why

PokoPang's rainbow (wild) tile is a core strategic element that lets players connect any color, enabling longer chains and more complex combos. The current prototype has no wild-tile mechanic — every tile is rigidly typed, making boards feel more constrained than PokoPang. Adding a rainbow tile closes this gap before portfolio submission.

## What Changes

- Add a new `Rainbow` block subtype that acts as a wildcard tile.
- Rainbow tiles can be linked with any neighboring tile regardless of type, and any neighboring tile can be linked to a rainbow tile.
- Rainbow tiles have no type of their own — they inherit the type of the first tile in the chain when selected, or act as a wildcard bridge when in the middle of a chain.
- Rainbow tiles have a distinct visual (gradient/rainbow pattern) so players instantly recognize them.
- When a rainbow tile is cleared in a chain, its points are calculated using the chain length formula with a bonus multiplier (e.g., ×1.5).
- The AI Game Designer Agent is updated to track rainbow tile usage in telemetry.

### Non-goals
- No new art assets — the rainbow pattern is generated as a runtime texture.
- No changes to bomb or special block behavior.

## Capabilities

### New Capabilities
- `rainbow-tile`: A wildcard tile subtype that can be linked with any adjacent tile type, visually distinct with a rainbow gradient.

### Modified Capabilities
- `line-linker-puzzle`: Drag linking logic must accept rainbow tiles as valid links with any adjacent type. Scoring includes a rainbow bonus.
- `ai-game-designer-agent`: BoardTelemetry extended with rainbow tile usage counter.
- `special-blocks`: Rainbow joins Frozen, Stone, Clock as a new PokoBlockSubtype value.

## Impact

- `Assets/PokoPuzzle/Scripts/Core/PokoTile.cs` — new IsRainbow property, rainbow visual rendering (gradient texture)
- `Assets/PokoPuzzle/Scripts/Core/PokoBlockType.cs` — add Rainbow to PokoBlockSubtype enum
- `Assets/PokoPuzzle/Scripts/Core/LineLinkerBoard.cs` — TryAddTileAtPointer modified to accept Rainbow as a valid bridge tile. Scoring adds rainbow bonus.
- `Assets/PokoPuzzle/Scripts/AI/BoardTelemetry.cs` — extend with rainbow tile cleared count
- `Assets/PokoPuzzle/Scripts/AI/HeuristicGameDesignerAgent.cs` — consider rainbow usage in suggestions
