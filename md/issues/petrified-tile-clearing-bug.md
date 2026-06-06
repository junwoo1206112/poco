# Petrified Tile Clearing Bug

## Status
**Fixed** - Petrified tiles now clear when they settle on the true bottom row of their column.

## Problem Description
Petrified (석화) tiles are applied to random tiles via the enemy Petrify skill. When these tiles fall to the bottom of the board, they should be cleared automatically, granting +40 score and dealing 1 damage to the enemy. However, Petrified tiles remain on the board indefinitely.

## Root Cause Analysis

### Flow
1. Petrified tiles are created via `ApplyPetrifySkill()` → `tile.ConfigureSubtype(PokoBlockSubtype.Petrified)`
2. `CollapseAndRefill()` calls `CompactColumns(false)` which moves all movable tiles (including Petrified) downward
3. `ClearBottomPetrifiedBlocks()` is called in a while loop to clear Petrified tiles at the bottom
4. **Bug**: `ClearBottomPetrifiedBlocks()` checked `row + 1` as though that direction were below the tile.
5. In this board, lower rows are closer to `row 0`, so a petrified tile on the true bottom row could be rejected when ordinary tiles were above it.

### Key Finding
The `ClearBottomPetrifiedBlocks()` method checked the wrong row direction. `CompactColumns()` writes movable tiles from the lowest valid row upward, so the bottom check must scan toward lower row indices.

### Log Evidence
```
[ClearBottomPetrifiedBlocks] col=1, row=0, tile=PokoTile, IsPetrified=True
[ClearBottomPetrifiedBlocks] col=1, row=0, isAtBottom=False  ← Petrified at row 0 but other tiles below
[ClearBottomPetrifiedBlocks] col=0, row=4, tile=PokoTile, IsPetrified=True
[ClearBottomPetrifiedBlocks] col=0, row=4, isAtBottom=True   ← Petrified at bottom, gets cleared
[ClearBottomPetrifiedBlocks] Cleared petrified at col=0, row=4
```

Some Petrified tiles DO get cleared (when they happen to be at the bottom), but most remain stuck in the middle of columns.

## Current Workarounds Attempted

### Attempt 1: Petrified as Fixed Obstacle
Made Petrified a fixed obstacle (`IsFixedObstacle` returns true for Petrified). This prevented Petrified from moving down at all - tiles stacked on top of Petrified but Petrified never reached the bottom.

### Attempt 2: Petrified as Movable Tile with Bottom Check
Kept Petrified as movable, added bottom-check logic in `ClearBottomPetrifiedBlocks()`. Works only when Petrified happens to be at the true bottom row with no tiles below it.

### Attempt 3: Immediate Clear on Discovery
Modified `ClearBottomPetrifiedBlocks()` to clear any Petrified tile found, regardless of position. This would work but may not match the intended game design (Petrified should fall to bottom first).

## Fix

The runtime now uses `IsPetrifiedAtBottom()` to scan toward `row - 1`, skipping invalid hex rows. A petrified tile clears only when there is no valid row below it, or when a fixed blocker forms the bottom of that compacted segment.

Regression coverage was added for the case where a petrified tile is on row 0 while ordinary tiles are above it.

## Files Involved
- `Assets/PokoPuzzle/Scripts/Core/LineLinkerBoard.cs` - `CollapseAndRefill()`, `ClearBottomPetrifiedBlocks()`, `CompactColumns()`, `IsFixedObstacle()`
- `Assets/PokoPuzzle/Scripts/Core/PokoTile.cs` - `IsPetrified`, `BlockSubtype`

## Related
- Petrified tiles should grant +40 score and deal 1 damage to enemy when cleared
- Petrified tiles are immune to bombs (per game design)
