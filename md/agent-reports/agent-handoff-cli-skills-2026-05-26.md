# Agent Handoff - CLI and Skills

## Role

Continue this project as a game designer agent plus product engineer. The goal is a Trinode AX-style Unity portfolio that shows a playable PokoPang-inspired Line-Linker puzzle, an AI-assisted designer loop, and a data pipeline that keeps gameplay tuning explicit.

## Required Skill Use

Use `.codex/skills/poko-game-designer/SKILL.md` before changing gameplay scope. It keeps the project focused on a small playable Line-Linker loop, CLI evidence, OpenSpec changes, and portfolio proof.

Use OpenSpec change workflows for behavior changes:

- `openspec/changes/hex-tile-visual-pass/`
- `openspec/changes/poko-core-mechanics/`
- `openspec/changes/rainbow-tile/`
- `openspec/changes/excel-data-di-pipeline/`

Before adding new mechanics, check existing changes and validate the relevant change with `openspec.cmd validate <change-name>`.

## Required CLI Use

Use `tools\poko-cli.cmd` for repeatable project operations. Do not rely only on manual Unity state when a CLI report can be produced.

Recommended commands:

```cmd
tools\poko-cli.cmd create-core-board --layout hex --tileVisual circle-in-hex --width 4 --height 13 --tileTypes 5 --spacing 0.74
tools\poko-cli.cmd validate-core-board --scenePath Assets/Scenes/PokoPrototype.unity --reportPath md/cli-reports/core-board-validation.md
tools\poko-cli.cmd convert-excel-data
tools\poko-cli.cmd analyze-playlog --logPath md/playtest-logs/latest-playtest.jsonl --reportPath md/agent-reports/latest-playtest-analysis.md
tools\poko-cli.cmd designer-loop-status --reportPath md/designer-loop/latest-status.md
```

Run Unity batchmode CLI commands only when the Unity editor is closed. If the editor is open, use the matching Unity menu item instead and then save evidence under `md/`.

## Current Implementation Direction

- Board shape is a 4x13 alternating 3-4-3-4 pointy-top hex board.
- Tiles use `CircleInHex` presentation so the board reads as a connected puzzle grid while each piece remains visible.
- Normal tile identity should stay stable: same type means same color and same shape.
- Collapse/refill must preserve the 3-4-3-4 silhouette after every clear.
- Runtime gameplay should not open `.xlsx` files directly.
- Excel data is authored in `Assets/PokoPuzzle/Data/Excel/GameData.xlsx`.
- `GameData.xlsx` uses `Enemy`, `Boss`, `Skill`, and `BalanceProfile` sheets.
- NPOI belongs in the editor conversion path.
- Runtime uses `PokoEnemyDatabase`, `PokoRegularEnemyDatabase`, `PokoEnemySkillDatabase`, and `PokoBalanceProfileDatabase` through serialized dependency injection.

## Excel Data Pipeline

The intended pattern is:

```text
GameData.xlsx
-> NPOI Editor Converter
-> PokoRegularEnemyDatabase.asset / PokoEnemyDatabase.asset / PokoEnemySkillDatabase.asset / PokoBalanceProfileDatabase.asset
-> LineLinkerBoard serialized DI
-> Runtime gameplay
```

Excel editing rules:

- Add a row to add data.
- Edit cells to change data.
- Delete a row or set `Enabled` to `FALSE`, `0`, `NO`, `OFF`, or `DELETE` to remove data from conversion.
- Convert again after edits.
- Check `md/cli-reports/excel-data-conversion.md` for imported row counts.

## Verification Priorities

1. Unity compiles with no NPOI reference errors.
2. `Tools > Poko Puzzle > Generate Excel Data Files` creates `GameData.xlsx`.
3. `Tools > Poko Puzzle > Convert Excel Data To Assets` creates generated database assets.
4. `LineLinkerBoard` receives `PokoEnemyDatabase`, `PokoRegularEnemyDatabase`, `PokoEnemySkillDatabase`, and `PokoBalanceProfileDatabase`.
5. Play mode shows the 3-4-3-4 hex board without shape/color identity confusion.
6. Clearing tiles preserves the board silhouette.
7. A play log is recorded and analyzed into `md/agent-reports/latest-playtest-analysis.md`.

## Portfolio Evidence Rule

Every milestone should answer:

- What can the reviewer play?
- What does the AI designer analyze?
- Which engineering/data decision is demonstrated?
- Where is the evidence saved?

Save concise evidence under `md/portfolio-milestones/`, `md/agent-reports/`, and `md/cli-reports/`.
