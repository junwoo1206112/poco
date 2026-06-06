# Excel Data Conversion Report

## Source

- Workbook: `Assets/PokoPuzzle/Data/Excel/GameData.xlsx`
- Enemy sheet: `Enemy`
- Boss sheet: `Boss`
- Skill sheet: `Skill`
- Balance profile sheet: `BalanceProfile`

## Result

- Imported regular enemies: `12`
- Imported boss waves: `6`
- Imported enemy skills: `6`
- Imported balance profiles: `4`
- Disabled rows are skipped when `Enabled` is `FALSE`, `0`, `NO`, `OFF`, or `DELETE`.
- Enemy and boss background transitions load from each row's `BackgroundPath` Resources key.
- Deleted Excel rows disappear from the generated databases on the next conversion.
