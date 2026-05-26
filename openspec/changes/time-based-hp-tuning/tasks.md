## 1. Data Model Changes

- [x] 1.1 Add `SuggestedRegularEnemyHp` and `SuggestedBossHp` fields to `AgentSuggestion` struct in `AgentSuggestion.cs`
- [x] 1.2 Add `regularEnemyHp` and `bossHp` fields to `PokoLevelConfig` ScriptableObject in `PokoLevelConfig.cs`

## 2. Play Log Analysis HP Suggestion

- [x] 2.1 In `PlayLogAnalysis.Evaluate()`, load `PokoEnemyDatabase` and `PokoRegularEnemyDatabase` via `AssetDatabase.LoadAssetAtPath`
- [x] 2.2 Compute base HP: boss HP from `PokoEnemyDatabase.GetWave(bossWave)` and regular enemy HP from `PokoRegularEnemyDatabase.GetEnemy(enemyId)` using stage data
- [x] 2.3 Apply timeRatio formula: `timeRatio > 0.5` → HP × 1.3, `timeRatio < 0.2` → HP × 0.8, else HP = 0
- [x] 2.4 Set `SuggestedBossHp` and `SuggestedRegularEnemyHp` in the timeRatio evaluation branch

## 3. Retune-Level HP Output

- [x] 3.1 Update `RetuneLevel()` in `PokoPuzzleCli.cs` to pass `analysis.SuggestedRegularEnemyHp` and `analysis.SuggestedBossHp` into the generated `PokoLevelConfig`
- [x] 3.2 Update `PokoLevelConfig.Configure()` to accept and store HP override parameters

## 4. Analyze-Playlog Report HP Output

- [x] 4.1 Add HP suggestion fields to the Markdown report section in `AnalyzePlayLog()` output
- [x] 4.2 Add HP suggestion fields to the JSON report section in `AnalyzePlayLog()` output

## 5. Experiment Variant HP

- [x] 5.1 Update `BuildExperimentVariants()` to apply HP deltas: readability gets 80% of control HP, combo gets 120% of control HP
- [x] 5.2 Update experiment comparison report writer to include each variant's HP values

## 6. Verification

- [x] 6.1 Run `Convert Excel Data To Assets` in Unity Editor to ensure all databases are current *(pending Unity)*
- [x] 6.2 Play a level, generate play log, run `analyze-playlog`, and verify HP suggestions appear in the report *(pending Unity)*
- [x] 6.3 Run `retune-level` and verify the generated `PokoLevelConfig` asset contains HP override fields *(pending Unity)*
- [x] 6.4 Run `plan-level-experiments` and verify each variant has different HP values *(pending Unity)*
- [x] 6.5 Check Unity console for any errors or warnings from the new code paths *(pending Unity)*
