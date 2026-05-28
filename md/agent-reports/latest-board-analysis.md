# Game Designer Agent Board Analysis

## Input

- Layout: `hex`
- Seed: `42`
- Board size: `4x13`
- Tile types: `6`
- Score: `0`
- Moves used: `0`

## Telemetry

- Possible chain starts: `4`
- Longest same-type area: `4`

## Designer Agent Judgment

- Difficulty: `Hard`
- Diagnosis: 4 starts / longest 4.
- Intent: Avoid early dead-board frustration.
- Risk: 3-chain is hard to read.
- Action: Lower types or add moves.

## Suggested Next Level Tuning

- Move limit: `24`
- Target score: `1400`
- Tile types: `5`

### Board Visual

- Layout: `hex` | Size: `4x13` | Seed: `42` | Tile types: `6`

```
  / ▲ \___/ ▲ \___/ ▲ \
  \___/ ◆ \___/ ⬟ \___/ ● \___/ ◆ \___/
  / ◆ \___/ ⬟ \___/ ⬟ \
  \___/ ⬟ \___/ ● \___/ ▲ \___/ ◆ \___/
  / ◆ \___/ ● \___/ ◆ \
  \___/ ▲ \___/ ■ \___/ ● \___/ ▲ \___/
  / ⬟ \___/ ▲ \___/ ◆ \
  \___/ ◆ \___/ ⬟ \___/ ▲ \___/ ● \___/
  / ◆ \___/ ● \___/ ⬟ \
  \___/ ▲ \___/ ▲ \___/ ★ \___/ ● \___/
  / ● \___/ ◆ \___/ ● \
  \___/ ● \___/ ■ \___/ ⬟ \___/ ⬟ \___/
  / ⬟ \___/ ◆ \___/ ● \
```

- `●` = Red
- `◆` = Yellow
- `■` = Green
- `▲` = Blue
- `⬟` = Purple
- `★` = Orange

