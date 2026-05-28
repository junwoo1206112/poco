# Change: Rainbow Gauge Bomb

## Why

The prototype should follow the PokoPang-style rainbow bomb pattern: clearing blocks fills a gauge, and a charged rainbow bomb removes all tiles of one color. The previous wildcard-tile direction made Rainbow behave like a regular board subtype, which no longer matches the intended product feel.

## What Changes

- Add a Rainbow bomb type driven by a board clear gauge.
- Remove Rainbow as a regular `PokoBlockSubtype`.
- Fill the gauge from cleared block count.
- Spawn a `BombType.Rainbow` bomb when the gauge reaches its threshold.
- Detonate the Rainbow bomb by player action, removing all linkable tiles of the most common color.
- Show the gauge in the HUD and log rainbow events for AI designer analysis.

## Impact

- `special-blocks`: Rainbow is no longer a subtype; Frozen, Stone, and Clock remain subtype-driven.
- `line-linker-puzzle`: Rainbow is no longer a wildcard link tile; it is a detonatable bomb.
- `rainbow-tile`: Reframed as a rainbow gauge bomb capability.
- `ai-game-designer-agent`: Rainbow usage remains tracked through `rainbow_cleared` events.
