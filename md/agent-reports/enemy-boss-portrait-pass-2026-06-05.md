# Enemy Boss Portrait Pass - 2026-06-05

## Asset Source

- Generated one 6x3 portrait sheet with the built-in image generation tool.
- Saved the source sheet at `Assets/PokoPuzzle/Art/EnemyPortraits/enemy_boss_portrait_sheet.png`.
- Cropped it into 18 transparent PNG portraits under `Assets/PokoPuzzle/Art/EnemyPortraits/Resources/EnemyPortraits/`.

## Regular Enemy Portraits

| EnemyId | Name | Resource Path |
| --- | --- | --- |
| 1 | Frostbinder Imp | `EnemyPortraits/frostbinder_imp` |
| 2 | Rime Wisp | `EnemyPortraits/rime_wisp` |
| 3 | Granite Mite | `EnemyPortraits/granite_mite` |
| 4 | Crystal Guard | `EnemyPortraits/crystal_guard` |
| 5 | Prism Bat | `EnemyPortraits/prism_bat` |
| 6 | Chroma Shifter | `EnemyPortraits/chroma_shifter` |
| 7 | Icebound Larva | `EnemyPortraits/icebound_larva` |
| 8 | Stonehide Serpent | `EnemyPortraits/stonehide_serpent` |
| 9 | Hue Imp | `EnemyPortraits/hue_imp` |
| 10 | Chroma Wisp | `EnemyPortraits/chroma_wisp` |
| 11 | Basalt Crawler | `EnemyPortraits/basalt_crawler` |
| 12 | Onyx Gargoyle | `EnemyPortraits/onyx_gargoyle` |

## Boss Portraits

| Wave | Name | Resource Path |
| --- | --- | --- |
| 1 | Frostbind Queen | `EnemyPortraits/frostbind_queen` |
| 2 | Stoneheart Golem | `EnemyPortraits/stoneheart_golem` |
| 3 | Prism Trickster | `EnemyPortraits/prism_trickster` |
| 4 | Blizzard Wyrm | `EnemyPortraits/blizzard_wyrm` |
| 5 | Chroma Overlord | `EnemyPortraits/chroma_overlord` |
| 6 | Obsidian Basilisk | `EnemyPortraits/obsidian_basilisk` |

## Runtime Wiring

- Added `PortraitPath` to regular enemy and boss data contracts.
- Added `PortraitPath` columns to `GameData.xlsx` and the Excel template generator.
- Updated generated ScriptableObject databases with portrait resource paths.
- `BoardEnemy` now carries the current portrait path.
- `BoardHudRenderer` loads the portrait with `Resources.Load<Texture2D>()` and draws it in the enemy/boss HP badge.

## Validation

- `dotnet build Assembly-CSharp.csproj` passed.
- `dotnet build Assembly-CSharp-Editor.csproj` passed with the existing LLM DTO warnings.
- `dotnet build Tests.csproj` passed after rerunning outside the parallel build lock.
- `dotnet test Tests.csproj --no-build` passed.
- `openspec.cmd validate --all` passed.
