# Petrified Tile Clearing Bug

## Status
**Open** - Petrified tiles do not clear when reaching the bottom of the board.

## Problem Description
Petrified (석화) tiles are applied to random tiles via the enemy Petrify skill. When these tiles fall to the bottom of the board, they should be cleared automatically, granting +40 score and dealing 1 damage to the enemy. However, Petrified tiles remain on the board indefinitely.

## Root Cause Analysis

### Flow
1. Petrified tiles are created via `ApplyPetrifySkill()` → `tile.ConfigureSubtype(PokoBlockSubtype.Petrified)`
2. `CollapseAndRefill()` calls `CompactColumns(false)` which moves all movable tiles (including Petrified) downward
3. `ClearBottomPetrifiedBlocks()` is called in a while loop to clear Petrified tiles at the bottom
4. **Bug**: Petrified tiles are treated as movable tiles in `CompactColumns()`, so they get mixed into segments with other tiles
5. When a Petrified tile is in the middle of a segment (not at the bottom row), it never reaches the bottom because other tiles are below it

### Key Finding
The `ClearBottomPetrifiedBlocks()` method checks if a Petrified tile is at the bottom by scanning rows below it. If any movable tile exists below the Petrified tile, `isAtBottom = false` and the tile is not cleared. Since Petrified is a movable tile itself, it gets placed in the middle of segments during compaction, preventing it from ever reaching the true bottom.

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

## Design Decision Needed

**Question**: Should Petrified tiles:
1. **Fall to bottom then clear** (current intended behavior, but buggy)
2. **Clear immediately when Petrify skill is applied** (simpler, no falling)
3. **Clear after N turns** (timer-based)
4. **Clear when any adjacent tile is matched** (reactive)

## Recommended Fix

Option 1 (fall to bottom then clear) requires Petrified to be treated as a fixed obstacle during compaction BUT still be checked for clearing after compaction settles. The fix should:

1. Keep Petrified as a fixed obstacle in `IsFixedObstacle()`
2. After `CompactColumns()` settles, check ALL Petrified tiles (not just bottom row)
3. Clear Petrified tiles that cannot fall further (no empty space below them)

```csharp
// In ClearBottomPetrifiedBlocks():
// Instead of checking only the bottom row, check if there's any empty space below
var canFallFurther = false;
for (var belowRow = row + 1; belowRow < height; belowRow++)
{
    if (!IsInsideBoard(column, belowRow)) continue;
    if (tiles[column, belowRow] == null) { canFallFurther = true; break; }
}
if (!canFallFurther) { /* clear the petrified tile */ }
```

## Files Involved
- `Assets/PokoPuzzle/Scripts/Core/LineLinkerBoard.cs` - `CollapseAndRefill()`, `ClearBottomPetrifiedBlocks()`, `CompactColumns()`, `IsFixedObstacle()`
- `Assets/PokoPuzzle/Scripts/Core/PokoTile.cs` - `IsPetrified`, `BlockSubtype`

## Related
- Petrified tiles should grant +40 score and deal 1 damage to enemy when cleared
- Petrified tiles are immune to bombs (per game design)
