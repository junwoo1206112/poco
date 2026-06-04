# Petrified Boss Data Pass - 2026-06-04

## Design Split

| Tile | Match result | Bomb result | Gravity rule |
| --- | --- | --- | --- |
| Frozen | Adjacent clear thaws it back to its animal tile | Red/Blue bombs thaw it | Fixed while frozen |
| Stone | Adjacent clear deals 1 durability damage | Red/Blue bombs destroy it immediately | Fixed blocker |
| Petrified | No damage | Immune to Red/Blue/Rainbow bombs | Falls with gravity and clears at bottom |

## Data Added

### Boss

| Wave | Name | Skill | HP | Defeat Bonus |
| --- | --- | --- | --- | --- |
| 6 | Obsidian Basilisk | Petrify | 760 | 1200 |

### Regular Enemy Set

| EnemyId | Name | Role | HP | Score Bonus |
| --- | --- | --- | --- | --- |
| 11 | Basalt Crawler | Petrified | 85 | 95 |
| 12 | Onyx Gargoyle | Petrified | 110 | 130 |

## Runtime Wiring

- `EnemySkillType.Petrify` maps to Excel `SkillType` value `petrify`.
- `PokoBlockSubtype.Petrified` is non-linkable through the existing `IsLinkable` rule.
- Red and Blue bombs thaw Frozen, destroy Stone immediately, and skip Petrified.
- Rainbow only removes linkable tiles, so Petrified survives even when its color is targeted.
- Stone and Frozen are fixed blockers during gravity.
- Petrified falls with movable pieces and clears for +40 when it reaches the bottom row.
- Column collapse/refill now compacts movable pieces in segments around fixed Stone/Frozen blockers to avoid empty playable cells.
- Boss wave 6 is included in the boss cycle and paired with the two Petrified regular enemies.

## Updated Assets

- `Assets/PokoPuzzle/Data/Excel/GameData.xlsx`
- `Assets/PokoPuzzle/Data/Resources/PokoEnemyDatabase.asset`
- `Assets/PokoPuzzle/Data/Resources/PokoRegularEnemyDatabase.asset`
- `Assets/PokoPuzzle/Data/Resources/PokoEnemySkillDatabase.asset`
