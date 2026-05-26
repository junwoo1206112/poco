## Context

PlayLogAnalysis.Evaluate() runs after every playtest parse and assigns a DifficultyLabel plus three tuning suggestions (MoveLimit, TargetScore, TileTypes). The timeRatio condition (timeLeft / TimeLimit) was just added as a new evaluation branch. However, enemy HP is not part of any suggestion output, so the retune-level and experiment pipeline cannot produce HP-balanced levels. Both regular enemies (Enemy table) and bosses (Boss table) have HP defined in Excel, but no runtime or analysis code currently adjusts them based on playtest data.

The existing pipeline:
1. Play → JSONL log → analyze-playlog → PlayLogAnalysis → suggestion
2. suggestion → retune-level → PokoLevelConfig.asset
3. config asset → apply-level → LineLinkerBoard serialized fields
4. Multiple playtest → compare-level-experiments → promote winner

The gap: steps 1-2 output and consume only MoveLimit/TargetScore/TileTypes. HP is never suggested, stored, or applied.

## Goals / Non-Goals

**Goals:**
- Add `SuggestedRegularEnemyHp` and `SuggestedBossHp` to PlayLogAnalysis output
- Extend AgentSuggestion struct with HP fields
- Extend PokoLevelConfig to store optional HP overrides
- Wire HP suggestions into retune-level output
- Wire HP into experiment variant generation (control: same HP, readability: -20% HP, combo: +20% HP)
- Wire HP into experiment winner comparison report
- Include HP in analyze-playlog markdown and JSON reports

**Non-Goals:**
- Auto-modify Excel GameData.xlsx (HP values stay in Excel as defaults; overrides live in PokoLevelConfig asset)
- Runtime LineLinkerBoard changes (HP override wiring can be a follow-up change)
- Enemy skill adjustments based on HP
- Multi-session HP trend analysis

## Decisions

**Decision 1: HP overrides in PokoLevelConfig vs. Excel modification**
- Chosen: PokoLevelConfig gains optional `regularEnemyHp` and `bossHp` fields (0 = use Excel default)
- Rationale: Keeps Excel as source of truth; PokoLevelConfig as playtest-derived override. This matches how MoveLimit/TargetScore already work (Excel sets baseline, config overrides).
- Alternative rejected: Writing back to Excel is destructive and makes rollback hard.

**Decision 2: HP suggestion formula**
- timeRatio > 0.5 (Easy, too much time): SuggestedBossHp = baseHp × 1.3, SuggestedRegularEnemyHp = baseHp × 1.3
- timeRatio < 0.2 (Hard, barely cleared): SuggestedBossHp = baseHp × 0.8, SuggestedRegularEnemyHp = baseHp × 0.8
- Otherwise: no HP change (0 = use Excel default)
- Rationale: 30% increase/reduction is large enough to affect playtest feel without breaking the board. Base HP comes from PokoEnemyDatabase / PokoRegularEnemyDatabase defaults while round tuning stays in PokoLevelConfig.

**Decision 3: Experiment variant HP deltas**
- Control: no HP change (0)
- Readability: -20% HP on both regular and boss
- Combo: +20% HP on both regular and boss
- Rationale: Symmetric deltas around control match existing MoveLimit/TargetScore variant strategy.

**Decision 4: Winner selection ignores HP by default**
- ChooseExperimentRecommendation() only mentions HP in the comparison report text, but does not use HP as a selection criterion.
- Rationale: HP tuning is too new to have a proven winner heuristic. Future changes can add HP-aware selection.

## Risks / Trade-offs

- [Risk] PokoLevelConfig HP fields are unused at runtime → Mitigation: Accept as intermediate state. The fields exist for the designer loop report; runtime wiring is a separate change.
- [Risk] 30% HP scaling may feel unfair → Mitigation: Formula is conservative (30%), and the values are suggestions, not auto-applied. Designer reviews before promoting.
- [Risk] HP lookup requires PokoEnemyDatabase and PokoRegularEnemyDatabase, which may not load in all CLI contexts → Mitigation: CLI runs inside Unity Editor where AssetDatabase.LoadAssetAtPath works. Guard with null checks and default to 0.

## Portfolio Evidence

- analyze-playlog report will show HP suggestions alongside existing tuning knobs
- experiment comparison report will show HP deltas per variant
- retune-level output will include HP fields in the generated PokoLevelConfig asset
