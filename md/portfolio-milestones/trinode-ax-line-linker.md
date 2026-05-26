# Trinode AX Portfolio Milestone - Poko Line Linker

## Portfolio Claim

This prototype demonstrates a Unity 2D casual puzzle built with an AI-agent-assisted production loop. The focus is not only making a playable Line-Linker board, but also proving that gameplay state, player behavior, and tuning decisions can be captured as repeatable evidence.

## Reviewer Playable

- A narrow mobile-style hex Line-Linker board.
- Circle-in-hex tile presentation for clearer adjacency reading.
- Drag linking through same-type adjacent tiles.
- Backtracking by dragging to the previous tile.
- Clear, score, collapse, and refill for chains of 3 or more.
- Screen-space HUD for score, time, moves, and designer feedback.

## AI Designer Role

The AI designer role is currently a deterministic local designer agent plus CLI workflow. Its job is to read board/play telemetry and produce practical next tuning actions.

Inputs:

- Board size and layout.
- Tile type count.
- Possible chain count.
- Longest same-type chain estimate.
- Score, moves used, and play log outcomes.
- Invalid short releases and average chain length after play.

Outputs:

- Difficulty/readability label.
- Design diagnosis.
- Risk statement.
- Suggested next action.
- Generated or retuned level settings through CLI.

## System Decisions

- The board uses a 3-4-3-4 alternating hex shape to better match a PokoPang-style vertical mobile puzzle board.
- The tile visual moved from full hex fill to circle-in-hex so the player reads both "piece" and "hex frame" at the same time.
- The default tile spacing is tuned so adjacent pointy-top hex frames read as a joined 3-4-3-4 puzzle board instead of isolated pieces.
- The designer loop is evidence-first: play logs and Markdown/JSON reports under `md/` are treated as portfolio artifacts, not temporary debug output.
- CLI commands create, validate, analyze, generate, apply, and retune puzzle state so the workflow is repeatable.

## Evidence Map

- Scene: `Assets/Scenes/PokoPrototype.unity`
- Runtime board: `Assets/PokoPuzzle/Scripts/Core/LineLinkerBoard.cs`
- Tile visual style: `Assets/PokoPuzzle/Scripts/Core/PokoTileVisualStyle.cs`
- CLI entrypoint: `tools/poko-cli.cmd`
- CLI implementation: `Assets/PokoPuzzle/Scripts/Editor/PokoPuzzleCli.cs`
- OpenSpec change: `openspec/changes/hex-tile-visual-pass/`
- Latest play log: `md/playtest-logs/latest-playtest.jsonl`
- Designer reports: `md/agent-reports/`

## Current Gap

The implementation has the right portfolio direction, but the evidence set is not complete yet. The latest play log currently contains only a session start for the 4x13 prototype, so the next step is a real playthrough that records valid moves, invalid short releases, and an end state.

## Next Validation Pass

1. Open `Assets/Scenes/PokoPrototype.unity`.
2. Confirm the board displays circle-in-hex tiles.
3. Play for at least 60 seconds or until the round ends.
4. Confirm `md/playtest-logs/latest-playtest.jsonl` contains session, move, and end events.
5. Close Unity and run `tools\poko-cli.cmd analyze-playlog`.
6. Save the resulting diagnosis as portfolio evidence.

## Resume Sentence

I built a Unity 2D Line-Linker puzzle and an AI designer-agent workflow that turns board state and play logs into tuning decisions. The project focuses on the system behind casual puzzle iteration: playable rules, telemetry, analysis, and repeatable CLI-driven retuning.
