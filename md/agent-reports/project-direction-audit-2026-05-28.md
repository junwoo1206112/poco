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

4. Core-feel decision

The project previously had a mismatch between some OpenSpec/milestone text, which promised automatic commit at 3 linked tiles, and the current playable code, which commits on release.

Decision: keep release-committed chain execution for this portfolio slice. This preserves readable drag preview, matches the main line-linker spec, avoids extra input-edge bugs, and keeps attention on the designer-agent loop.

5. Rainbow behavior update

Rainbow moved from wildcard link tiles to the PokoPang-style rainbow bomb model: block clears fill a gauge, the gauge creates a Rainbow bomb, and detonating that bomb removes all linkable tiles of the most common color.

## Changes Made In This Audit

- Removed Rainbow as a regular block subtype.
- Added gauge-driven `BombType.Rainbow` behavior.
- Added Rainbow bomb detonation for same-color full-board clearing.
- Added rainbow gauge HUD and `rainbow_ready` / `rainbow_cleared` play-log events.
- Fixed the `time-based-hp-tuning` OpenSpec delta header so all active specs validate.

## Recommended Next Work

1. Run one real Unity play session and generate fresh evidence:

```cmd
tools\poko-cli.cmd validate-core-board --scenePath Assets/Scenes/PokoPrototype.unity --reportPath md/cli-reports/core-board-validation.md
tools\poko-cli.cmd analyze-playlog --logPath md/playtest-logs/latest-playtest.jsonl --reportPath md/agent-reports/latest-playtest-analysis.md
tools\poko-cli.cmd designer-loop-status --reportPath md/designer-loop/latest-status.md
```

2. Reconcile `poko-core-mechanics/tasks.md` after play verification.

3. Track the Unity-generated `.meta` files that correspond to tracked scripts/tests, or intentionally remove the source files and their metas together if those files are obsolete.

4. Capture a short portfolio milestone only after the evidence files include one full-round play log with an `end` event.
