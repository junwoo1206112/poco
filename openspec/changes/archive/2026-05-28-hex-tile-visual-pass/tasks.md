## 1. Hex Sprite Implementation

- [x] 1.1 Replace `CreateCircleSprite()` in `LineLinkerBoard.cs` with `CreateHexSprite()` that draws a hex-shaped white mask on a runtime Texture2D.
- [x] 1.2 Replace `CircleCollider2D` with `PolygonCollider2D` on `PokoTile` and set six vertices matching the hex sprite silhouette.
- [x] 1.3 Add `--tileVisual circle-in-hex` scene creation support for a colored circle inside a visible hexagonal frame.

## 2. Hex Board Shape (PokoPang 3-4 pattern)

- [x] 2.1 `IsInsideBoard()` — even rows=3 tiles, odd rows=4 tiles (width).
- [x] 2.2 Modify `BuildBoard()` to skip outside hex board shape.
- [x] 2.3 Modify `CollapseAndRefill()` to skip refill outside hex board shape.
- [x] 2.4 Add `IsInsideBoard` guard to `TryFindThreeTilePath()`.
- [x] 2.5 Change default to 4×13 (narrow tall PokoPang-style board).
- [x] 2.6 CLI hex art: 3-line style with variable row widths (3→4→3→4...).
- [x] 2.7 Refactor `HexGridUtility`: new 3-tile / 4-tile neighbor offsets matching Python code.
- [x] 2.8 Add `HexGridUtility.RowSize(int row)` — returns 3 for even, 4 for odd.
- [x] 2.9 Update all `GetNeighbors` callers to use new 3-param signature (col, row, height).
- [x] 2.10 Update `AreAdjacent` — remove `useHexGrid` param, use new offsets.
- [x] 2.11 Update CLI validation tests for new 3-4-3-4 adjacency rules.
- [x] 2.12 Remove `useHexGrid` from CLI `CountSameNeighbors`, `FindLongestChain`, `CountPossibleChains`.
- [x] 2.13 Fix `ToWorld` centering: per-row centering using `RowSize(row)` instead of global `width`.
- [x] 2.14 Odd-R offset changed to -0.5 (odd rows shift left instead of right).
- [x] 2.15 Tune default spacing to `0.74` so adjacent pointy-top hex frames visually join into one puzzle board.
- [x] 2.16 Keep vertical spacing ratio at `0.8660254` so the 3-4-3-4 row-offset board interlocks as pointy-top hex tiling.
- [x] 2.17 Rotate runtime sprite and collider vertices by 30 degrees so visuals match row-offset pointy-top hex geometry.
- [x] 2.18 Fix collapse/refill to drop only through valid cells in each column so the 3-4-3-4 silhouette survives after clears.
- [x] 2.19 Disable random special subtype tinting in the core prototype so each normal tile type keeps one color-shape identity.

## 3. Unity Verification

- [x] 3.1 Open Unity, confirm compilation succeeds with no errors.
- [x] 3.2 Enter Play mode and verify tiles render as hex shapes with visible dark border.
- [x] 3.3 Verify the board has a 3434 alternating-row pattern (3 tiles on even rows, 4 on odd rows, 13 rows tall).
- [x] 3.4 Verify drag linking, chain clear, collapse/refill, scoring, and valid-next-tile hints still work.
- [x] 3.5 Verify no console errors during play.

## 4. Playtest

- [x] 4.1 Play a round through to clear or fail state so the play log records move events.
- [x] 4.2 Confirm `md/playtest-logs/latest-playtest.jsonl` contains session, move, and end events.

## 5. Analysis

- [x] 5.1 Run `tools\poko-cli.cmd analyze-playlog` (close Unity first) to generate new playtest analysis.
- [x] 5.2 Compare new analysis metrics (invalid short releases, average chain length) against the previous `Readability Risk` baseline.
- [x] 5.3 Save comparison notes to `md/agent-reports/hex-tile-comparison.md`.
