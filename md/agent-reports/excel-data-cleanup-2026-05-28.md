# Excel Data Cleanup - 2026-05-28

## Decision

The project should keep one designer-authored workbook:

- `Assets/PokoPuzzle/Data/Excel/GameData.xlsx`

That workbook owns four sheets:

- `Enemy`
- `Boss`
- `Skill`
- `BalanceProfile`

Runtime gameplay should use converted ScriptableObject databases:

- `PokoRegularEnemyDatabase.asset`
- `PokoEnemyDatabase.asset`
- `PokoEnemySkillDatabase.asset`
- `PokoBalanceProfileDatabase.asset`

## Removed

- `GameData_tmp.xlsx`
- `GameData.xlsx.bak`
- `PokoStageDatabase.asset`
- `IStageDataProvider.cs`
- `PokoStageDatabase.cs`

## Why

`GameData.xlsx.bak` was an older workbook with `Stage`, `Enemy`, `Boss`, and `Skill` sheets. The current pipeline no longer stores round tuning in Excel, so keeping that backup made the source of truth ambiguous.

`GameData_tmp.xlsx` matched the newer sheet shape but was not referenced by importer, generator, CLI, docs, or runtime code.

`PokoStageDatabase` and `IStageDataProvider` were leftovers from the intermediate Stage-based workflow. Current runtime board data comes from enemy, boss, skill, and balance profile databases, while round settings live in `PokoLevelConfig` or the prototype scene.

## Current Workflow

1. Edit `GameData.xlsx`.
2. Run `Tools > Poko Puzzle > Convert Excel Data To Assets`.
3. Verify converted databases under `Assets/PokoPuzzle/Data/Resources/`.
4. Keep score, time, board size, tile count, and playtest retune values in `PokoLevelConfig` or AI-generated level assets rather than Excel.
