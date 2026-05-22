---
name: poko-game-designer
description: Use this in the Unity Poko Puzzle project when building a PokoPang-style Line-Linker puzzle and AI game designer agent. Keep gameplay scope, CLI workflow, OpenSpec changes, and portfolio evidence aligned.
---

# Poko Game Designer

## Workflow

1. Read `md/trinode-ax-portfolio-plan.md` before changing gameplay scope.
2. Prefer a small playable Line-Linker loop over a broad match-3 framework.
3. Keep Unity scripts focused and easy to verify locally.
4. Save design decisions, CLI evidence, and AI designer outputs under `md/`.
5. Check `openspec/changes/` before adding a new feature area. If the feature changes behavior or requirements, update or create an OpenSpec change first.
6. Use `md/core-board-input-cli.md` for the Core Board & Input CLI workflow.
7. Use `tools\poko-cli.cmd analyze-board` when the work needs a designer-agent balance read.

## CLI Standards

- Inspect the current code and docs first with `rg`, `rg --files`, and `Get-Content`.
- For Unity console errors, search `Editor.log` for the exact class, line, and exception name.
- Keep repeatable Unity creation or validation work behind `tools/poko-cli.cmd`.
- Store CLI output reports in `md/cli-reports/`.
- Store designer-agent analysis in `md/agent-reports/`.
- Prefer the smallest playable slice that proves the portfolio point.

## Feature CLI Map

- Core Board & Input: `tools\poko-cli.cmd create-core-board --layout hex`
- Core Board validation: `tools\poko-cli.cmd validate-core-board`
- Designer Agent board read: `tools\poko-cli.cmd analyze-board --layout hex --width 7 --height 7 --tileTypes 5`
- Designer Agent level output: `tools\poko-cli.cmd generate-level --levelId level_001 --layout hex`
- Apply generated level: `tools\poko-cli.cmd apply-level --levelId level_001`
- Designer Agent playtest read: `tools\poko-cli.cmd analyze-playlog`
- Designer Agent retune output: `tools\poko-cli.cmd retune-level --levelId level_002`
- Designer Agent experiment plan: `tools\poko-cli.cmd plan-level-experiments --experimentId exp_001`
- Designer Agent experiment comparison: `tools\poko-cli.cmd compare-level-experiments --experimentId exp_001`
- Designer Agent winner promotion: `tools\poko-cli.cmd promote-experiment-winner --experimentId exp_001 --applyScene true`
- Designer Agent loop status: `tools\poko-cli.cmd designer-loop-status --experimentId exp_001`
- LLM designer review packet/report: `tools\poko-cli.cmd llm-design-review`

## OpenSpec Standards

- Use `openspec.cmd` on Windows.
- A feature change should include `proposal.md`, `design.md` when needed, `specs/<capability>/spec.md`, and `tasks.md`.
- Requirements use `### Requirement:` and scenarios use `#### Scenario:`.
- Run `openspec.cmd validate <change-name>` before considering a spec change complete.
- Small bug fixes or documentation-only edits can skip OpenSpec when they do not change behavior.

## Gameplay Rules

- Board: start with a 7x7 board.
- Layout: default board uses an odd-row offset hexagonal grid.
- Adjacency: only six hex-direction neighbors are valid.
- Linking: player drags through adjacent same-type tiles.
- Backtracking: dragging back to the immediately previous tile removes the last selected tile.
- Clear rule: a chain clears only when the player releases 3 or more linked tiles.
- Invalid release: chains of 1 or 2 tiles do not change the board.
- Feedback priority: visible connection line, scoring popup or score text, fast collapse/refill.
- Do not copy protected PokoPang art, names, characters, sounds, or exact UI.

## AI Designer Agent Contract

The first implementation should be deterministic and local.

- Input: level settings, board distribution, possible-chain statistics, and play summary.
- Output: difficulty label, design hint, suggested move limit, target score, tile type count, or spawn-weight suggestion.
- Reports should be human-readable Markdown under `md/agent-reports/` when possible.
- JSON tuning suggestions should be saved beside the Markdown report when CLI is used.
- Generated level settings should become `PokoLevelConfig` assets under `Assets/PokoPuzzle/Data/Generated/`.
- Generated level settings should be applied to the prototype scene before judging gameplay feel.
- Runtime play logs should be analyzed before the next tuning pass.
- Runtime play logs should preserve a level-specific latest file so experiment candidates can be compared after play.
- Playtest analysis should feed a new generated level asset so the agent forms a closed tuning loop.
- Playtest analysis should also produce a small experiment set when the next design question needs comparison instead of one answer.
- Experiment comparison should promote a playable next baseline and save reviewer-readable portfolio milestone evidence.
- Loop status should explain the current evidence gap and next CLI or Play mode step before another tuning pass.
- The optional LLM review step should read the same JSON analysis contract and save its request, report, and raw response under `md/llm-reports/`.

The deterministic agent remains the no-key baseline. The LLM review layer extends that baseline without replacing the telemetry contract.

## Portfolio Criteria

Each completed milestone should answer:

- What can a reviewer play?
- What does the AI designer analyze or suggest?
- Which Unity or CLI engineering decision is demonstrated?
- Where is the evidence saved?
