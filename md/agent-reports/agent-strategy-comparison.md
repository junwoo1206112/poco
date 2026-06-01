# AI Game Designer Strategy Comparison

## Input Telemetry

- Board: `4x13`, Tile types: `5`
- Score: `3780`, Moves: `10`, Valid: `10`, Invalid: `1`
- Avg chain: `4.20`, Max combo: `0`, Fever triggers: `1`
- Damage dealt: `400`, Bombs: `0`, Rainbow: `0`

## Heuristic Agent Result

- **Difficulty**: `Fever`
- **Summary**: Combo 0 / Fever active. 400 dmg dealt.
- **Design Intent**: Fever is running; the board cascade is self-clearing.
- **Risk**: Fever may end before target score is met.
- **Recommended Action**: Keep chain momentum; consider lowering target if Fever doesn't resolve.
- Suggested move limit: `25`, target score: `4580`, tile types: `5`

## LLM Agent Result

- **Difficulty**: `Fever`
- **Summary**: Combo 0 / Fever active. 400 dmg dealt.
- **Design Intent**: Fever is running; the board cascade is self-clearing.
- **Risk**: Fever may end before target score is met.
- **Recommended Action**: Keep chain momentum; consider lowering target if Fever doesn't resolve.
- Suggested move limit: `25`, target score: `4580`, tile types: `5`

## Comparison

| Dimension | Heuristic | LLM |
|---|---|---|
| Difficulty | Fever | Fever |
| Action | Keep chain momentum; consider lowering target if Fever doesn't resolve. | Keep chain momentum; consider lowering target if Fever doesn't resolve. |
| Move limit | 25 | 25 |
| Target score | 4580 | 4580 |
| Tile types | 5 | 5 |

## Portfolio Note

This comparison demonstrates two AI game designer strategies on the same play log. The heuristic agent uses deterministic rules; the LLM agent uses `gpt-4o-mini` with natural language analysis. Both implement the same `IGameDesignerAgent` interface, making them swappable at runtime.
