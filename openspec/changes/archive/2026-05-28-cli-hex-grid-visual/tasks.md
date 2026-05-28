## 1. Hex Grid Renderer

- [x] 1.1 Add `WriteHexGridVisual()` method to `PokoPuzzleCli.cs` that renders an `int[,]` board as an ASCII hex grid with odd-row offset using Unicode tile symbols.
- [x] 1.2 Add tile type symbol legend (● ◆ ■ ▲ ⬟ ★) and board metadata (dimensions, seed) to the visual block.
- [x] 2.1 Inject the hex grid visual into `WriteAgentMarkdownReport()` so `analyze-board` includes it.
- [x] 2.2 Inject the hex grid visual into `WriteGeneratedLevelMarkdown()` so `generate-level` includes it.

## 3. Verification

- [x] 3.1 Open Unity, confirm compilation succeeds.
- [x] 3.2 Run `tools\poko-cli.cmd analyze-board` (close Unity first) and verify the Markdown report contains a readable hex grid.
- [x] 3.3 Confirm the grid correctly renders odd-row offset and five distinct tile symbols.
