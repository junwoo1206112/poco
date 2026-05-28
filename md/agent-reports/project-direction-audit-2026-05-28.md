# Project Direction Audit - 2026-05-28

## Portfolio Goal

The project is still directionally strong as a Unity casual puzzle portfolio:

- Playable PokoPang-inspired Line-Linker core.
- Deterministic AI game designer loop.
- Excel-to-ScriptableObject data pipeline for tuning.
- Markdown/JSON evidence under `md/` for reviewer-readable proof.

The strongest portfolio sentence is:

> A reviewer can play a compact 4x13 hex Line-Linker board, then inspect how the local designer agent reads play logs and proposes level tuning.

## What Is Aligned

- `LineLinkerBoard` already contains the playable loop: board generation, drag linking, score, timer, collapse/refill, enemy pressure, bombs, fever, and play logging.
- `PokoPuzzleCli` contains the correct portfolio pipeline: validate board, analyze board, generate/apply/retune level, plan/compare/promote experiments, and LLM review packet generation.
- OpenSpec now validates all active specs and changes.
- The Excel data direction is correct: authored workbook -> editor conversion -> ScriptableObject databases -> serialized runtime dependencies.
- The next-action handoff correctly says not to add new systems until readability, telemetry, and tuning proof are stronger.

## Direction Risks

1. Scope creep risk

`poko-core-mechanics` covers irreversible linking, combo, fever, enemy combat, bombs, special blocks, telemetry, and evidence. That is a large milestone for a portfolio prototype. It should be presented as "implemented prototype systems needing play validation", not as a fully balanced production feature set.

2. Evidence mismatch risk

The latest play log is currently only a session start plus one enemy skill event, while the latest analysis report describes an older 7x7 playtest with valid/invalid moves. Regenerate play evidence after a real play session before using the report as portfolio proof.

3. Checklist mismatch risk

`poko-core-mechanics/tasks.md` still shows 0/55 tasks even though code and milestone notes show many of those systems exist. This makes the project look less complete than it is and should be reconciled after Unity play verification.

4. Core-feel mismatch risk

The milestone says irreversible one-stroke commits automatically at 3 linked tiles, but current code still commits on release. Decide whether the intended player feel is:

- PokoPang-like immediate commit at the third tile, or
- Safer drag-preview until release.

This should be a deliberate design choice, because it changes how fast and irreversible the puzzle feels.

5. Rainbow behavior risk

Rainbow tiles were specified as wildcard link tiles, but runtime behavior did not fully match. This audit pass corrected the main linkability, wildcard, score bonus, and logging path.

## Changes Made In This Audit

- Made Rainbow subtype linkable.
- Allowed Rainbow tiles to bridge different tile types during drag.
- Moved Rainbow tap activation to single-tile release, so a Rainbow tile can also start a chain.
- Applied the 1.5x Rainbow score bonus for Rainbow-inclusive chains.
- Logged `rainbow_cleared` events for chain clears and tap clears.
- Fixed the `time-based-hp-tuning` OpenSpec delta header so all active specs validate.

## Recommended Next Work

1. Run one real Unity play session and generate fresh evidence:

```cmd
tools\poko-cli.cmd validate-core-board --scenePath Assets/Scenes/PokoPrototype.unity --reportPath md/cli-reports/core-board-validation.md
tools\poko-cli.cmd analyze-playlog --logPath md/playtest-logs/latest-playtest.jsonl --reportPath md/agent-reports/latest-playtest-analysis.md
tools\poko-cli.cmd designer-loop-status --reportPath md/designer-loop/latest-status.md
```

2. Reconcile `poko-core-mechanics/tasks.md` after play verification.

3. Decide the irreversible link behavior and update either code or spec:

- If immediate commit is the goal, implement commit-on-third-link.
- If release commit is the goal, update the spec/milestone wording to avoid promising a different feel.

4. Capture a short portfolio milestone only after the evidence files match the current scene and play log.

