## Why

The time-based difficulty analysis (timeLeft/TimeLimit ratio) was added to PlayLogAnalysis.Evaluate() but only suggests MoveLimit/TargetScore/TileTypes adjustments. Enemy and boss HP are the primary difficulty knobs for combat depth, and leaving them untuned means the designer loop produces incomplete level configs.

## What Changes

- `PlayLogAnalysis.Evaluate()` outputs `SuggestedRegularEnemyHp` and `SuggestedBossHp` based on timeRatio
- `AgentSuggestion` struct gains two new HP fields
- `PokoLevelConfig` scriptable object adds enemy HP override fields
- `retune-level` CLI command writes HP suggestions into generated level configs
- `plan-level-experiments` / `compare-level-experiments` consider HP in variant generation and winner selection
- `analyze-playlog` report includes HP suggestions
- Board telemetry is unchanged; only the analysis/retune pipeline expands

## Capabilities

### New Capabilities
- `time-hp-tuning`: Time-based HP recommendation engine for both regular enemies and bosses, integrated into the play-log analysis and level generation pipeline.

### Modified Capabilities
- `ai-game-designer-agent`: Evaluation cascade now includes HP tuning suggestions alongside existing difficulty labels; experiment planning generates HP variants.

## Impact

- `PokoPuzzleCli.cs`: PlayLogAnalysis inner class gains two int fields; Evaluate() sets them; report/retune/experiment commands read them
- `AgentSuggestion.cs`: Two int fields added
- `PokoLevelConfig.cs`: Two int fields added (regularEnemyHp, bossHp)
- `PokoPuzzleCli.cs` experiment builders: BuildExperimentVariants() creates HP-differentiated variants
- `PokoPuzzleCli.cs` winner selection: ChooseExperimentRecommendation() considers HP changes
- No runtime BoardEnemy or LineLinkerBoard changes unless levelConfig HP overrides are wired in
