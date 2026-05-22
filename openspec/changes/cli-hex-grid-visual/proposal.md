## Why

The existing CLI reports (`analyze-board`, `generate-level`) output telemetry tables and agent judgments as Markdown, but they do not show the board layout itself. Adding a terminal hex grid visualization makes the board state readable at a glance during the designer loop — the AI agent's analysis becomes verifiable without Unity Play mode.

## What Changes

- Add a hex grid ASCII renderer that outputs the board with odd-row offset layout.
- Render each tile type as a distinct Unicode symbol (e.g., ● ▲ ■ ◆ ⬟) instead of colored output (terminal color support is inconsistent across Windows/PowerShell/git-bash).
- Include the hex grid in `analyze-board` and `generate-level` CLI reports.
- Include board dimensions, tile type legend, and seed in the visual output.

## Capabilities

### New Capabilities

- `cli-board-visual`: Terminal-readable hex grid visualization of the puzzle board for CLI reports.

### Modified Capabilities

None. This is a pure addition to CLI output — no existing requirement or behavior changes.

## Impact

- `Assets/PokoPuzzle/Scripts/Editor/PokoPuzzleCli.cs` — new `WriteHexGridVisual()` method and calls from `AnalyzeBoard()` / `GenerateLevel()`.
- Report files in `md/cli-reports/` and `md/agent-reports/` will include a hex grid ASCII block.
- No external NuGet or npm dependencies. Unicode symbols are built into the .NET console.
