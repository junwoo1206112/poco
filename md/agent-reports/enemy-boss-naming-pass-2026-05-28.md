# Enemy and Boss Naming Pass - 2026-05-28

## Goal

Rename enemies and bosses so their names support the active boss skill concepts in `GameData.xlsx`.

## Skill Concept Map

- Wave 1: `freeze` -> Frostbind Queen
- Wave 2: `stone` -> Stoneheart Golem
- Wave 3: `colorswap` -> Prism Trickster
- Wave 4: `freeze` -> Blizzard Wyrm
- Wave 5: `colorswap` -> Chroma Overlord

## Regular Enemy Naming Direction

- Freeze family: Frostbinder Imp, Rime Wisp, Icebound Larva
- Stone family: Granite Mite, Crystal Guard, Stonehide Serpent
- Color swap family: Prism Bat, Shade Shifter, Hue Imp, Chroma Wisp

## Files Updated

- `Assets/PokoPuzzle/Data/Excel/GameData.xlsx`
- `Assets/PokoPuzzle/Data/Resources/PokoRegularEnemyDatabase.asset`
- `Assets/PokoPuzzle/Data/Resources/PokoEnemyDatabase.asset`
- `Assets/PokoPuzzle/Editor/ExcelDataGenerator.cs`

## Note

HP and score values were preserved from the current Excel workbook and mirrored into the runtime databases so the name pass does not silently retune combat difficulty.
