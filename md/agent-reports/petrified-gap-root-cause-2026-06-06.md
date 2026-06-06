# Petrified Gap Root Cause - 2026-06-06

## Root Causes

The intermittent empty-cell issue was not only caused by the final refill pass failing outright. Two cross-checked paths could make the board appear to have holes after Petrified or other special clears.

`ClearAdjacentFrozenTiles()` scanned the whole board and thawed any Frozen tile that had any neighboring `null` cell. During Petrified bottom clearing, temporary `null` cells are expected before compaction/refill completes. That meant a transient gap from a Petrified clear could incorrectly trigger unrelated Frozen tiles, changing fixed-obstacle segmentation while the board was still resolving.

This made the collapse/refill sequence sensitive to timing and special-tile layout. In play, that could appear as a hole after Petrified disappeared.

The second issue was visual-state drift from overlapping drop animations. `CollapseAndRefill()` can run multiple compaction passes in one resolve cycle. A tile could receive `SetGridPosition()` for its new logical cell and then still have an older `AnimateDrop()` coroutine finish later, writing the transform back to an obsolete target. In that case the `tiles[column,row]` array was filled, but the tile's visible world position no longer matched its grid cell, making a real tile look like a blank cell.

## Fix

- Replaced the global null-neighbor scan with `ClearFrozenTilesAdjacentTo(clearedCells)`.
- Bomb, Rainbow, and Petrified clear paths now pass only the grid cells they actually cleared.
- Normal chain clears already thaw Frozen blocks from the exact cleared tile position and no longer call the global scan.
- Each tile now tracks its active drop coroutine. Starting a new drop, clearing a tile, or snapping it to a grid cell cancels the previous drop animation first.
- `VerifyBoardIntegrity()` now repairs null, clearing, or inactive playable cells and also snaps any filled cell whose visual transform does not match its board coordinates.
- The board runs integrity repair after collapse/refill and again after playable-chain assistance.

## Verification

- `dotnet build "Poko Engine CLI Puzzle Framework.sln"` passed.
- Focused edit-mode test filter for Petrified clear, empty-cell repair, visual snap repair, drop-cancel behavior, mixed special gaps, and exact Frozen adjacency passed.
- `npx.cmd --yes @fission-ai/openspec@latest validate --all` passed: 15 passed, 0 failed.
- `tools\poko-cli.cmd validate-core-board` completed with exit code 0 after closing the Unity editor that had the project open.
