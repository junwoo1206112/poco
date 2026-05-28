## Why

The current prototype uses a circular placeholder sprite for tiles, but the board logic already uses odd-row offset hex grid with 6-direction adjacency. The visual shape does not match the rule set, which contributes to the `Readability Risk` diagnosis from the previous playtest analysis. Now that valid-next-tile hinting is implemented, the board is ready for a hex visual pass followed by a fresh playtest to measure whether readability improves.

## What Changes

- Replace runtime-generated circle placeholder sprite with a runtime-generated hex-shaped tile sprite.
- Make the hex sprite respect each tile's `PokoTileType` color while keeping a distinct hex silhouette.
- Run a fresh playtest session with the new hex visuals and enabled valid-next-tile hints.
- Compare the new play log against the previous `Readability Risk` baseline to measure improvement.

## Capabilities

### New Capabilities

None. This change is an implementation-quality pass within existing capabilities.

### Modified Capabilities

- `line-linker-puzzle`: Visual tile presentation — the board SHALL render tiles as hex-shaped sprites instead of circles so the visual form matches the hex grid adjacency rule.

## Impact

- `Assets/PokoPuzzle/Scripts/Core/LineLinkerBoard.cs` — `CreateCircleSprite` replaced with `CreateHexSprite`.
- `md/playtest-logs/latest-playtest.jsonl` — new play log from the fresh playtest.
- `md/agent-reports/` — new playtest analysis and optional comparison report.

No new assets, textures, or external art dependencies. The hex sprite is generated at runtime the same way the circle sprite was.
