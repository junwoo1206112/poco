# Poko AI Handoff - 2026-05-22

## Project Goal

Build a Unity 2D PokoPang-style Line-Linker puzzle prototype and an AI Game Designer Agent workflow that can:

1. play a small drag-link puzzle loop
2. record play telemetry
3. analyze that telemetry
4. suggest or generate the next tuning pass through CLI reports and level assets

This is not a generic match-3 project. The prototype should stay focused on:

- hexagonal board logic
- same-type drag linking
- 3+ clear rule
- readable player feedback
- a repeatable designer-agent CLI loop

## Resume First

When continuing from another PC or AI session, read these first:

1. `.codex/skills/poko-game-designer/SKILL.md`
2. `md/trinode-ax-portfolio-plan.md`
3. `openspec/changes/line-linker-mvp-ai-designer/tasks.md`
4. `md/playable-drag-validation.md`
5. this file

## Important Current Truth

### Hex Board

The current runtime rules are hexagonal already:

- default board is odd-row offset
- adjacency uses six directions
- linking accepts only adjacent same-type tiles

The tile art has moved beyond the old circle placeholder. Runtime tile sprites now use generated hex frames/colliders, and the latest visual pass strengthens hex boundaries with bright white grid lines, a thin inner groove, clear-pop shake, camera shake, and hex shard bursts. Keep this original and do not copy protected PokoPang art.

### Play Loop

Implemented:

- runtime 7x7 board generation
- pointer input through Unity Input System APIs
- same-type drag linking
- immediate backtrack by dragging to previous tile
- clear only on 3 or more linked tiles
- short-chain cancel without board mutation
- score gain from chain clears
- collapse and refill
- win/fail state from target score and move limit
- round restart from end HUD
- guaranteed clearable 3-tile path after initial build and refill
- valid next same-type neighbor hint while a drag chain is active

Key files:

- `Assets/PokoPuzzle/Scripts/Core/LineLinkerBoard.cs`
- `Assets/PokoPuzzle/Scripts/Core/PokoTile.cs`
- `Assets/PokoPuzzle/Scripts/Core/HexGridUtility.cs`
- `Assets/PokoPuzzle/Scripts/Core/PokoLevelConfig.cs`

### HUD and Input Diagnosis

Several UI/input-looking issues were already traced:

- Old score and designer labels were world-space `TextMesh` objects and overlapped the board.
- Runtime now hides those legacy text renderers when screen HUD mode is enabled.
- Score and move state are rendered in a screen HUD top lane.
- The short designer summary is rendered in a screen HUD bottom lane.
- If a round clears or fails, drag input intentionally stops because `gameEnded` is set.
- The end HUD now explains that state and exposes `Restart Round`.

The previous "tiles stop clicking after several moves" report was caused by a normal clear at `1420/1400` after eight valid moves. The baseline default target was raised from `1400` to `2200` for the current score curve.

### Unity Game View Note

Unity Game view `Scale 2x` crops the preview around the center. Use the full `1x` Game preview when judging overall HUD layout. The current layout should keep:

- score and feedback above the board
- board in the middle play area
- designer summary below the board

## Designer Agent State

Implemented CLI workflow in `tools/poko-cli.cmd` includes:

- `create-core-board`
- `validate-core-board`
- `analyze-board`
- `generate-level`
- `apply-level`
- `analyze-playlog`
- `retune-level`
- `plan-level-experiments`
- `compare-level-experiments`
- `promote-experiment-winner`
- `designer-loop-status`
- `llm-design-review`

The deterministic no-key designer agent is the baseline. The optional LLM review path should extend the same telemetry contract instead of replacing it.

Latest analysis evidence:

- Runtime play log: `md/playtest-logs/latest-playtest.jsonl`
- Playtest analysis markdown: `md/agent-reports/latest-playtest-analysis.md`
- Playtest analysis JSON: `md/agent-reports/latest-playtest-analysis.json`

The latest unfinished play log had:

- one valid five-link clear
- three short invalid releases
- high invalid rate

The current designer diagnosis is `Readability Risk`. That diagnosis triggered the valid-next-tile hint during drag.

## Verification Already Done

These checks passed during the current session:

```cmd
dotnet build Assembly-CSharp.csproj
dotnet build Assembly-CSharp-Editor.csproj
openspec.cmd validate line-linker-mvp-ai-designer
```

Important Windows/Unity caveat:

`tools\poko-cli.cmd validate-core-board` and `tools\poko-cli.cmd analyze-playlog` launch Unity batchmode. They fail while the same Unity project is already open in the editor with a message like:

```text
It looks like another Unity instance is running with this project open.
```

Do not treat that as a gameplay code failure. Close the project in Unity before running those batchmode CLI commands.

## OpenSpec State

Active change:

- `openspec/changes/line-linker-mvp-ai-designer`

Current task status:

- gameplay and designer-agent implementation tasks are checked through gameplay task `3.7`
- verification task `5.2` is still open
- verification task `5.4` is still open

The two open verification tasks are:

1. run the Unity prototype and confirm no console errors during basic drag-link play
2. run `tools\poko-cli.cmd validate-core-board` after Unity releases the project lock

## Next Actions

### 1. Manual Unity Play Check

Use Unity Play mode with Game view at `1x`.

Check:

1. score HUD stays above board rows
2. designer summary stays below board rows
3. clicking a tile starts a drag chain
4. valid next same-type hex neighbors show hint feedback
5. 3+ chain release clears, scores, collapses, and refills
6. 1-2 chain release leaves board unchanged
7. clear/fail end panel appears and `Restart Round` works
8. Console has no runtime exceptions

If this passes, mark OpenSpec task `5.2` complete.

### 2. Close Unity and Run CLI Validation

After Unity is not holding the project lock:

```cmd
tools\poko-cli.cmd validate-core-board --reportPath md/cli-reports/core-board-validation.md
tools\poko-cli.cmd analyze-playlog --logPath md/playtest-logs/latest-playtest.jsonl --reportPath md/agent-reports/latest-playtest-analysis.md
```

If the board validation passes, mark OpenSpec task `5.4` complete.

### 3. Decide Next Product Slice

Recommended next slice after verification:

1. replace circle placeholder tiles with original hex-shaped tiles so the visual board matches the hex rule set
2. run another playtest with the new valid-next-tile hint
3. compare the next play log against the current `Readability Risk` diagnosis

Avoid adding combat, monetization, or broad match-3 systems before the core Line-Linker presentation and designer-loop evidence read clearly.

## Known Files Worth Reading

- Core runtime: `Assets/PokoPuzzle/Scripts/Core/LineLinkerBoard.cs`
- Tile feedback: `Assets/PokoPuzzle/Scripts/Core/PokoTile.cs`
- Scene builder: `Assets/PokoPuzzle/Scripts/Editor/PokoPrototypeSceneBuilder.cs`
- CLI surface: `Assets/PokoPuzzle/Scripts/Editor/PokoPuzzleCli.cs`
- CLI workflow note: `md/core-board-input-cli.md`
- Play validation note: `md/playable-drag-validation.md`
- Designer loop status note: `md/designer-loop-status-cli.md`

## Suggested Continuation Prompt

Use this prompt in a new AI session:

```text
Read md/poko-ai-handoff-2026-05-22.md, .codex/skills/poko-game-designer/SKILL.md, and openspec/changes/line-linker-mvp-ai-designer/tasks.md first. Continue the Unity Poko Line-Linker project from the handoff. Start with the remaining verification tasks, then move the placeholder circular board presentation to an original hex tile visual pass if verification is clean.
```
