## 1. Runtime Data Assets

- [x] 1.1 Make enemy, boss, skill, and balance profile data records Unity-serializable.
- [x] 1.2 Add `PokoEnemyDatabase` implementing `IEnemyDataProvider`.
- [x] 1.3 Add `PokoRegularEnemyDatabase` implementing `IRegularEnemyDataProvider`.
- [x] 1.4 Add `PokoEnemySkillDatabase` implementing `IEnemySkillProvider`.
- [x] 1.5 Add `PokoBalanceProfileDatabase` implementing `IBalanceProfileProvider`.

## 2. Editor Excel Conversion

- [x] 2.1 Add an editor menu that converts `.xlsx` data into ScriptableObject databases.
- [x] 2.2 Save converted assets under `Assets/PokoPuzzle/Data/Resources/`.
- [x] 2.3 Keep generated sample Excel authoring workbook under `Assets/PokoPuzzle/Data/Excel/GameData.xlsx`.
- [x] 2.4 Add a CLI command for repeatable Excel conversion.
- [x] 2.5 Use `Enemy`, `Boss`, `Skill`, and `BalanceProfile` sheets in one workbook instead of separate Excel files.
- [x] 2.6 Support Excel add/edit/delete workflow with an `Enabled` column and conversion report.
- [x] 2.7 Keep round tuning in generated `PokoLevelConfig` assets instead of the Excel workbook.

## 3. Dependency Injection

- [x] 3.1 Add serialized database references to `LineLinkerBoard`.
- [x] 3.2 Initialize data providers from injected ScriptableObject databases.
- [x] 3.3 Remove direct runtime `.xlsx` loading from `LineLinkerBoard`.

## 4. Evidence

- [x] 4.1 Save a portfolio note explaining the Excel-to-DI workflow.
- [x] 4.2 Run Unity menu conversion and verify generated assets after closing/reloading the editor if needed.
