# Designer Agent Next Actions - 2026-05-26

## Current Read

The project is directionally aligned with a Trinode AX-style portfolio: Unity casual puzzle client work plus AI-assisted design iteration. The strongest signal is the loop from playable board to play telemetry to designer diagnosis.

## Design Priority

Do not add new puzzle systems yet. The next design work should prove that the existing core loop is readable, measurable, and tunable.

## Immediate Checks

- Verify the current prototype scene uses `CircleInHex` tile visuals.
- Verify adjacent pointy-top hex frames visually join into one 3-4-3-4 puzzle-board silhouette.
- Verify each normal tile type keeps a stable color-shape identity.
- Verify the 4x13 board keeps the 3-4-3-4 alternating row pattern.
- Verify the player can find and complete at least one 3+ chain without confusion.
- Verify timer, score, collapse, refill, and feedback all work in one recorded play session.

## Success Criteria

- A reviewer can understand valid hex adjacency within the first few seconds.
- A play log includes at least one valid clear and one round end event.
- The designer report explains whether readability improved or still fails.
- The portfolio milestone document can point to concrete scene, code, log, and report evidence.

## Stop Conditions

- If players still click isolated tiles repeatedly, prioritize stronger valid-neighbor hints before adding new mechanics.
- If circle-in-hex tiles still read as full hexes, adjust sprite proportions before tuning score.
- If the play log does not record end events, fix telemetry before producing another portfolio report.

## Recommended Next Command Sequence

Use `.codex/skills/poko-game-designer/SKILL.md` and the project CLI before expanding gameplay scope. The detailed handoff is in `md/agent-reports/agent-handoff-cli-skills-2026-05-26.md`.

```cmd
tools\poko-cli.cmd validate-core-board --scenePath Assets/Scenes/PokoPrototype.unity --reportPath md/cli-reports/core-board-validation.md
tools\poko-cli.cmd convert-excel-data
tools\poko-cli.cmd analyze-playlog --logPath md/playtest-logs/latest-playtest.jsonl --reportPath md/agent-reports/latest-playtest-analysis.md
tools\poko-cli.cmd designer-loop-status --reportPath md/designer-loop/latest-status.md
```

Run these after a real Unity play session and after closing the Unity editor, because Unity batchmode cannot open the same project while the editor already has it open.
