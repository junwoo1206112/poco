# HP and Boss UI Tuning - 2026-05-28

## Goal

Make regular enemies and bosses survive longer in the 60-second score-attack loop, and make boss appearances visually clear in the HUD.

## HP Baseline

Regular enemy HP was raised from the old 25-60 range to a 40-85 range.

Boss HP was raised from the old 120-350 range to:

- Frostbind Queen: 240
- Stoneheart Golem: 300
- Prism Trickster: 380
- Blizzard Wyrm: 480
- Chroma Overlord: 650

The values were applied to:

- `Assets/PokoPuzzle/Data/Excel/GameData.xlsx`
- `Assets/PokoPuzzle/Data/Resources/PokoRegularEnemyDatabase.asset`
- `Assets/PokoPuzzle/Data/Resources/PokoEnemyDatabase.asset`
- `Assets/PokoPuzzle/Editor/ExcelDataGenerator.cs`

## UI Change

Boss enemies now show a red `BOSS` badge to the left of the enemy HP bar. The badge includes the boss wave number so a reviewer can immediately distinguish boss combat from regular enemy waves.

## Next Playtest Check

Play one 60-second round and verify:

- First boss is readable as a boss before attacking.
- Stoneheart Golem and Prism Trickster do not disappear too quickly.
- Score can still reach the 10000 target without the round feeling stalled.
