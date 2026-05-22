# Level Experiment Planning CLI

## What This Stage Does

The earlier designer loop answers one tuning question:

1. Read a play log.
2. Diagnose the level.
3. Generate one next level.

The experiment planning stage answers a more designer-like question:

> Which next level variants should be compared, and what should each one prove?

## Command

```cmd
tools\poko-cli.cmd plan-level-experiments --experimentId exp_001
```

The command reads:

- `md/playtest-logs/latest-playtest.jsonl`

It writes:

- three generated `PokoLevelConfig` assets under `Assets/PokoPuzzle/Data/Generated/Experiments/`
- a Markdown plan under `md/experiment-reports/`
- a JSON plan under `md/experiment-reports/`

## Candidate Set

### Control

The control candidate uses the deterministic retune recommendation. It is the comparison anchor.

### Readability

The readability candidate reduces color noise and adds a small move buffer. It tests whether the player understands valid hex links and reaches a valid 3+ clear more reliably.

### Combo

The combo candidate favors readable same-color paths while raising the score challenge. It tests whether longer links and score feedback make the portfolio play capture feel satisfying.

## How To Use It

1. Generate the experiment pack.
2. Apply one asset with `apply-level`.
3. Play the candidate in Unity.
4. Play the other candidates so their level-specific logs are saved.
5. Compare the candidate logs:

```cmd
tools\poko-cli.cmd compare-level-experiments --experimentId exp_001
```

Runtime logging keeps `md/playtest-logs/latest-playtest.jsonl` and also writes level-specific latest logs such as:

- `md/playtest-logs/by-level/exp_001_control-latest.jsonl`
- `md/playtest-logs/by-level/exp_001_readability-latest.jsonl`
- `md/playtest-logs/by-level/exp_001_combo-latest.jsonl`

Promote the comparison winner into the next baseline:

```cmd
tools\poko-cli.cmd promote-experiment-winner --experimentId exp_001 --applyScene true
```

Promotion creates a durable promoted `PokoLevelConfig`, a promotion report, and a portfolio milestone note.

Check the current loop stage at any point:

```cmd
tools\poko-cli.cmd designer-loop-status --experimentId exp_001
```

The status report says which evidence exists and which Play mode or CLI action should happen next.

This makes the Game Designer Agent visibly responsible for both tuning suggestions and the next design test.
