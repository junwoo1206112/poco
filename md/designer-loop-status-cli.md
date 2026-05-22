# Designer Loop Status CLI

## What This Stage Does

The Game Designer Agent now has separate commands for analysis, experiment planning, comparison, and promotion. This status command inspects the current workspace evidence and explains which step is available next.

## Command

```cmd
tools\poko-cli.cmd designer-loop-status --experimentId exp_001
```

## Status Stages

- `Need Playtest Log`: Unity Play mode telemetry does not exist yet.
- `Ready To Analyze`: a latest play log exists but no analysis JSON exists.
- `Ready To Plan Experiments`: analysis exists but no experiment plan exists.
- `Need Candidate Plays`: the experiment pack exists but candidate play logs are incomplete.
- `Ready To Compare`: control, readability, and combo logs are ready.
- `Ready To Promote`: comparison exists and the winner can be promoted.
- `Loop Complete`: promotion evidence exists and the next cycle can start.

## Saved Evidence

- Markdown: `md/designer-loop/latest-status.md`
- JSON: `md/designer-loop/latest-status.json`

This keeps the orchestration role explicit: the agent reports the current design-loop stage before the next playtest or CLI pass.
