# Excel Data DI Pipeline

## Goal

Excel is the authoring format for designer-facing enemy, boss, skill, and balance profile data. Runtime gameplay should not open `.xlsx` files directly. Instead, Unity editor tooling converts Excel workbooks into ScriptableObject databases, and the board receives those databases through serialized dependency injection.

## Workflow

1. Run `Tools > Poko Puzzle > Generate Excel Data Files`.
2. Edit `Assets/PokoPuzzle/Data/Excel/GameData.xlsx`.
3. Keep normal unit rows in the `Enemy` sheet, boss rows in the `Boss` sheet, enemy skill rows in the `Skill` sheet, and pressure presets in the `BalanceProfile` sheet.
4. Run `Tools > Poko Puzzle > Convert Excel Data To Assets`.
5. Use the generated databases under `Assets/PokoPuzzle/Data/Resources/`.
6. Inject `PokoEnemyDatabase`, `PokoRegularEnemyDatabase`, `PokoEnemySkillDatabase`, and `PokoBalanceProfileDatabase` into `LineLinkerBoard`.

## Excel Editing Rules

- Add: add a new row under the matching sheet.
- Edit: change cell values in the existing row.
- Delete: remove the row, or set `Enabled` to `FALSE`, `0`, `NO`, `OFF`, or `DELETE`.
- Convert again after editing. The generated ScriptableObject databases are replaced from the current enabled Excel rows.
- Check `md/cli-reports/excel-data-conversion.md` for imported row counts.

CLI alternative:

```cmd
tools\poko-cli.cmd convert-excel-data
```

## Runtime Contract

- `LineLinkerBoard` depends on `IEnemyDataProvider`, `IRegularEnemyDataProvider`, `IEnemySkillProvider`, and `IBalanceProfileProvider`.
- `PokoEnemyDatabase` implements boss wave lookup for HP and defeat bonus from the `Boss` sheet.
- `PokoRegularEnemyDatabase` implements normal enemy lookup for HP and score bonus from the `Enemy` sheet.
- `PokoEnemySkillDatabase` implements enemy skill lookup from the `Skill` sheet.
- `PokoBalanceProfileDatabase` implements pressure preset lookup from the `BalanceProfile` sheet, including `FeverGaugeMultiplier` for Fever charge pacing.
- `PokoLevelConfig` can select a `BalanceProfile` row by profile id, so generated experiment levels can run as `default`, `readable`, `pressure`, or `combo`.
- Round settings such as board size, tile type count, target score, balance profile id, and move limit live in `PokoLevelConfig` and generated level assets rather than the Excel workbook.
- NPOI is used by editor tooling to read `.xlsx`; gameplay uses converted Unity assets.

## Portfolio Value

This shows a product-engineering workflow: designer-readable data, deterministic conversion, explicit dependencies, and runtime-safe gameplay systems. It also keeps the AI designer loop ready for future output into Excel or generated database assets.
