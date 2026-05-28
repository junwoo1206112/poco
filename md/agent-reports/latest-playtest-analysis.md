# Game Designer Agent Playtest Analysis

## Input

- Play log: `md/playtest-logs/latest-playtest.jsonl`
- Level id: `prototype`
- Board size: `4x13`
- Tile types: `5`
- Move limit: `20`
- Target score: `10000`

## Play Telemetry

- Result: `unfinished`
- Final score: `3195`
- Moves used: `12`
- Valid moves: `12`
- Invalid short releases: `5`
- Average chain length: `3.83`
- Average score per valid move: `252.50`

## Combat Telemetry

- Max combo: `0`
- Fever triggers: `1`
- Total damage dealt: `415`
- Bombs generated: `0`
- Bombs detonated: `0`
- Special blocks cleared: `0`
- Rainbow bombs cleared: `0`

## Designer Agent Judgment

- Difficulty: `Combat Focus`
- Diagnosis: Player dealt 415 damage to enemy.
- Risk: Damage output may carry the round regardless of score.
- Action: Consider increasing enemy HP or adding special blocks.
- Suggested regular enemy HP: `0`
- Suggested boss HP: `0`
