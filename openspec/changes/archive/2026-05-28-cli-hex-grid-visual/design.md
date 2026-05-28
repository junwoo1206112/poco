## Context

The CLI currently writes structured Markdown reports to `md/` files for all designer-loop commands. These reports contain telemetry tables and agent judgments but no board layout visualization. The reviewer must either open Unity or mentally reconstruct the board from numbers. Adding an ASCII hex grid to the report body makes the board state immediately readable in the terminal or any Markdown viewer.

## Goals / Non-Goals

**Goals:**
- Render a hex grid as an ASCII block using Unicode symbols, odd-row offset layout.
- Each tile type maps to a distinct Unicode symbol for visual differentiation.
- Include the hex grid in `analyze-board` and `generate-level` CLI reports.
- Include board dimensions, seed, and tile type legend in the visual block.

**Non-Goals:**
- Do not add terminal color escape codes — Unicode symbols work in all terminals.
- Do not add interactive or animated terminal output.
- Do not modify the Unity runtime board or tile rendering.
- Do not change the JSON report format — only the Markdown human-readable reports.

## Decisions

**Unicode symbols over terminal colors.** `System.Console` color support varies across Windows Terminal, PowerShell ISE, git-bash, and CI logs. Unicode symbols (● ◆ ■ ▲ ⬟) are universally supported and render identically in all environments. Alternative: ANSI escape codes. Rejected because they produce garbage in non-ANSI terminals and in the `md/` Markdown files.

**StringBuilder-based rendering.** A `char[][]` buffer is built row by row, then joined into a single string. Odd rows are offset by one space character. The buffer approach avoids per-row string concatenation overhead in a tight double loop.

**Report body injection.** The hex grid ASCII block is appended to the report body string in the existing `WriteAgentMarkdownReport()` and `WriteGeneratedLevelMarkdown()` methods. No new CLI commands are needed.

## Risks / Trade-offs

- Unicode symbol alignment depends on monospace font. Most terminals and code editors use monospace fonts by default. Markdown viewers (GitHub, Obsidian) render code-fence blocks in monospace. Risk is low.
- Tile type count (3-6) maps to 3-6 symbols. If someone adds a 7th type, the legend needs a new symbol. Acceptable for the current 5-type default.
