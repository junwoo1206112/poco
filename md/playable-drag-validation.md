# Playable Drag Validation

## Current Truth

The puzzle drag path is implemented in `LineLinkerBoard`:

1. pointer press begins a chain
2. dragging over a six-direction adjacent same-type tile extends it
3. dragging back to the previous tile removes the last step
4. releasing a chain of three or more clears, scores, collapses, and refills

The board now guarantees at least one connected same-color three-tile path after initial generation and refill, so a playable drag route should always exist.

## Why The Current Game View Looked Too Large

- The shared screenshot used Unity Game view `Scale 2x`, which zooms the editor preview.
- The saved prototype scene also had a smaller orthographic camera size than the scene builder default.
- Runtime now enforces a minimum camera framing size for the board and HUD, and the saved scene default is aligned to that framing.
- The old score and designer labels were world-space `TextMesh` objects, so moving them closer to the board still let them overlap tiles when the Game view was zoomed or a round ended.
- Runtime now hides those legacy text renderers during play and draws a compact screen-space score panel, feedback, designer summary, and round-end restart panel.
- Unity Game view `Scale 2x` crops the render around its center. Use the full `1x` preview to judge HUD layout.
- In the full preview, score and feedback live in the top HUD lane and the designer summary lives in the bottom HUD lane so neither panel covers board rows.

## Input Lock Root Cause

The play log captured a normal round clear at `1420/1400` after eight valid moves. The board then set `gameEnded`, and that state intentionally stopped drag input.

That behavior was correct for the win/fail rule, but the old overlapping HUD made the stopped input look like a pointer bug. The prototype now:

- raises the default baseline target from `1400` to `2200` to fit the current chain-squared score curve better
- shows a centered end panel when drag input is paused
- lets the player restart the round from that panel
- keeps the detailed designer analysis in reports while the play HUD shows a short designer summary

## Readability Pass

The latest unfinished play log had one valid five-link clear and three short invalid releases. The designer rule path classifies that as `Readability Risk`, so active chains now hint the same-type adjacent neighbors that can extend the current hex path.

## Manual Play Check

In Unity Play mode:

1. Click a tile.
2. Drag only through neighboring tiles of the same color.
3. Release after three or more linked tiles.
4. Confirm score changes, tiles clear, collapse/refill happens, and `md/playtest-logs/latest-playtest.jsonl` is created.

If a drag crosses a different color or a non-neighbor hex tile, the chain should stop extending. A one or two tile release should show the short-chain feedback and keep the board unchanged.

## Verification Snapshot

Checked on 2026-05-22:

- `dotnet build Assembly-CSharp.csproj --no-restore` passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passed when run sequentially.
- `openspec.cmd validate line-linker-mvp-ai-designer` passed.
- `tools\poko-cli.cmd validate-core-board` reached Unity batchmode, but Unity refused to open the project while the same project was already open in the editor.

The next evidence step is still a live Play mode drag check in the open Unity editor, followed by the CLI scene validation after the editor releases the project lock.
