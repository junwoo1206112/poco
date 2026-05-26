# Excel Data Conversion Report

## Source

- Workbook: `Assets/PokoPuzzle/Data/Excel/GameData.xlsx`
- Enemy sheet: `Enemy`
- Boss sheet: `Boss`
- Skill sheet: `Skill`
- Balance profile sheet: `BalanceProfile`

## Result

- Imported regular enemies: `10`
- Imported boss waves: `5`
- Imported enemy skills: `5`
- Imported balance profiles: `4`
- Disabled rows are skipped when `Enabled` is `FALSE`, `0`, `NO`, `OFF`, or `DELETE`.
- Deleted Excel rows disappear from the generated databases on the next conversion.
