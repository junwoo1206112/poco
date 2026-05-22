## Context

The prototype currently generates a white circle sprite at runtime via `LineLinkerBoard.CreateCircleSprite()` and applies tile type color through `PokoTile.Initialize()`. The board logic uses odd-row offset hex grid with 6-direction adjacency, but the visual sprite is a circle — the shape does not communicate the hex rule set to the player. The previous `Readability Risk` playtest diagnosis flagged this mismatch.

The valid-next-tile hint feature (glowing valid neighbors during drag) is already implemented. A hex visual pass combined with a fresh playtest will measure whether the readability improves.

## Goals / Non-Goals

**Goals:**
- Replace the runtime circle sprite generator with a hex sprite generator.
- Change `PokoTile` collider from `CircleCollider2D` to `PolygonCollider2D` for pixel-accurate hex hit detection.
- Keep all sprite generation at runtime — no external texture or asset dependencies.
- Run a fresh playtest and save the play log.
- Compare the new play log metrics against the previous `Readability Risk` baseline.

**Non-Goals:**
- Do not add polished gradient, outline, or bevel effects — keep the flat color hex style.
- Do not modify the hex grid logic or adjacency rules.
- Do not add PokoPang-protected art, characters, or UI elements.

## Decisions

**Hex sprite over circle.** The hex sprite uses the same `Texture2D` pixel-iteration approach as the current circle. A 96x96 texture draws a hex mask using six vertex positions calculated from a center point and radius. The white mask is tinted by the tile type color at render time via the sprite renderer. Alternative: importing a pre-made hex PNG. Rejected because runtime generation keeps the prototype self-contained and avoids asset pipeline dependency.

**Polygon collider.** `CircleCollider2D` leaves gaps at hex corners and overlaps outside the hex boundary. A `PolygonCollider2D` with six vertices matching the hex sprite silhouette gives accurate click targeting. Alternative: keep `CircleCollider2D` with a tighter radius. Rejected because gameplay feel benefits from accurate hex hit detection.

**Playtest comparison.** The existing `analyze-playlog` CLI reads the same JSONL format already written by `LineLinkerBoard`. No logging changes needed. The comparison is manual (read both MD reports) or via the experiment comparison CLI if the previous log is preserved.

## Risks / Trade-offs

- The runtime hex sprite uses point-sampled pixel edges — may look aliased at extreme zoom levels. Mitigation: the orthographic camera size is clamped to a reasonable range (`6.6f` minimum), so aliasing is minimal.
- The `PolygonCollider2D` requires six vertex positions that match the sprite hex exactly. If offsets drift during maintenance, clicks may miss. Mitigation: both the sprite and collider derive from the same hex radius constant.
- The previous `Readability Risk` baseline log is from an older play session (target score 1400). The current target is 2200. Direct comparison needs to account for different move/score curves.
