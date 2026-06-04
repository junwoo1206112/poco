# Special Tile Rule Audit - 2026-06-04

## Confirmed Rule Set

| Tile | Link | Adjacent chain clear | Red/Blue bomb | Rainbow bomb | Gravity |
| --- | --- | --- | --- | --- | --- |
| Frozen | No | Thaws to original animal tile, +20 | Thaws to original animal tile, +20 | Not targeted directly | Fixed while frozen |
| Stone | No | Loses durability, clears at 0 HP | Destroyed immediately, +50 | Not targeted directly | Fixed blocker |
| Petrified | No | No damage | No damage | No damage | Falls with movable tiles, clears at bottom for +40 |
| Rainbow Bomb | Tap special | N/A | N/A | Clears linkable tiles of one color | Falls as a bomb tile |

## Problems Found

- Stone bomb handling had been changed to durability damage. This made bombs feel like they failed to break Stone.
- Petrified had been treated as a fixed blocker. That prevented the intended bottom-row clear condition from ever happening.
- Main `special-blocks` OpenSpec still described legacy Stone bottom-clear and Clock requirements, while runtime no longer had a Clock subtype.

## Fixes Applied

- Red/Blue bomb hits now destroy Stone immediately through a dedicated clear path.
- Petrified is no longer treated as a fixed obstacle during column compaction.
- `CollapseAndRefill()` now clears Petrified when it reaches the bottom valid row, scores +40, then compacts again before refill.
- OpenSpec change docs and main `special-blocks` spec now match the runtime rule set.
- Edit-mode tests now cover Stone bomb destruction and Petrified bottom-row clearing.

## Remaining Play Mode Verification

- Confirm Stone disappears visually when a Red/Blue bomb includes it.
- Confirm Petrified survives bomb effects, then falls and clears after reaching the bottom row.
- Confirm Frozen thaws back into a usable animal tile.
- Confirm no empty playable cells remain after fixed Stone/Frozen blockers segment a column.
