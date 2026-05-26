## Context

The current prototype has 6 typed tile colors (Red, Yellow, Green, Blue, Purple, Orange) plus special blocks (Frozen, Stone, Clock) and Bombs. All linking logic requires identical types between adjacent tiles. There is no wildcard mechanic.

PokoPang's rainbow tile acts as a wildcard: it has no color of its own and can bridge chains between any two types. This creates strategic depth — players can use rainbow tiles to extend chains across color boundaries.

The rainbow tile is visually distinct with a multicolor gradient pattern (no single tint).

## Goals / Non-Goals

**Goals:**
- Rainbow block subtype added to `PokoBlockSubtype` enum.
- Rainbow tiles are linkable and can bridge any two tile types in a chain.
- When a rainbow tile is the first selected tile, it "inherits" the type of the next linked tile.
- When a rainbow tile is in the middle or end of a chain, it acts as a bridge to the next tile regardless of type.
- Rainbow tiles render with a runtime-generated rainbow gradient texture.
- Rainbow clear awards base score ×1.5 bonus multiplier.
- Rainbow tile usage tracked in BoardTelemetry.

**Non-Goals:**
- No new art assets (gradient is runtime-generated).
- No changes to bomb, frozen, stone, or clock behavior.
- Rainbow tiles do not appear as a regular tile color in RandomType() — they only appear via RandomSubtype or manual placement.

## Decisions

### Rainbow tile is a BlockSubtype, not a PokoTileType
- **Decision**: Rainbow uses `PokoBlockSubtype.Rainbow` (new enum value), keeping the tile's `PokoTileType` as a visible color for rendering.
- **Rationale**: Rainbow tiles should display as rainbow-patterned, not as one of the 6 colors. Using BlockSubtype for the wildcard behavior is consistent with how Frozen/Stone/Clock work.
- **Trade-off**: The tile's `Type` property still holds a PokoTileType value (used for render fallback), but it's semantically meaningless for rainbow tiles.

### Wildcard bridge behavior, not type inheritance
- **Decision**: Rainbow tiles allow chain extension from any type to any type. When a rainbow is in the chain, the "last type" check in TryAddTileAtPointer is bypassed.
- **Rationale**: Simpler implementation than type inheritance. The player experiences rainbow as "I can always drag through it."
- **Alternatives considered**: Type inheritance would require tracking "effective type" through the chain, adding complexity.

### Rainbow generated at refill, not at build
- **Decision**: Rainbow tiles appear during board generation and refill via `RandomSubtype()` with ~6% probability (same as other special blocks), not as a replaceable tile color.
- **Rationale**: Rainbow is an occasional strategic bonus, not a core tile type. Low probability keeps it special.

### Visual: Gradient overlay on standard hex
- **Decision**: Rainbow tiles render with a horizontal multicolor gradient generated as a runtime Texture2D, overlaid on the standard hex border.
- **Rationale**: Instantly recognizable as "special" without needing art assets.

### Scoring: ×1.5 multiplier for rainbow-inclusive chains
- **Decision**: When a chain contains at least one rainbow tile, the base score is multiplied by 1.5 (before combo multiplier).
- **Rationale**: Rewards players for incorporating rainbows into chains. Not a flat bonus to avoid exploiting.

## Risks / Trade-offs

- [Risk] Rainbow link bypass could feel unintuitive if the player expects a rainbow to have a specific color → Mitigation: Clear visual distinction (rainbow gradient) signals "wildcard."
- [Risk] ×1.5 multiplier could make rainbow-heavy boards too easy → Mitigation: Low spawn probability (6%). AI agent can detect overuse and suggest lowering it.
- [Risk] Rainbow tiles conflict with IsLinkable logic for special blocks → Mitigation: IsLinkable returns true for Rainbow (same as Clock), and TryAddTileAtPointer handles the type-bridge logic.

## Portfolio Evidence

- Play logs will include `rainbow_cleared` events.
- CLI `analyze-playlog` report shows rainbow usage count.
- AI designer agent can suggest tuning rainbow probability based on play data.
