# Video Layout Match Pass - 2026-06-01

## Goal

Bring the prototype closer to the referenced PokoPang-style Shorts clip by making the board read as a wider, denser mobile puzzle field while preserving the current Line-Linker controls and special-tile VFX work.

## Source Limitation

The YouTube Shorts URL could not be fetched directly from the local/browser tool chain, so this pass uses the visible requirements gathered during review:

- Blue bomb should feel like nearby tiles are pulled into the bomb center before detonation.
- Red bomb should feel like line-clearing firepower across the board.
- Rainbow tile should target and clear a color with a prism/sweep effect.
- The board should not look like the older narrow 4x13 / 3-4-3-4 prototype or the interim 6-7-6-7 honeycomb silhouette.

QooApp's LINE PokoPang screenshots were also inspected as a reference for the original board feel. The visible gameplay screenshot reads closer to a compact full-width staggered block field than to a tall alternating-row honeycomb board.

## Applied Changes

- Changed the runtime prototype board default to `7x7`, `5` tile types, and `0.72` spacing.
- Updated the saved prototype scene to the same `7x7`, `5` tile type, `0.72` spacing setup.
- Reduced the board camera orthographic size to `5.0` so the board and combat space read closer to the mobile reference framing.
- Reworked hex row sizing so configured board width is respected as full-width staggered rows: a width of `7` now creates `7-7-7-7` rows with alternating half-tile offsets instead of a `6-7-6-7` honeycomb silhouette.
- Updated bomb range logic to use width-aware hex neighbors, so red and blue bomb effects match the wider board instead of the legacy row size.
- Updated CLI help/default scene creation path to advertise the new video-like board defaults.
- Updated OpenSpec line-linker requirements and tests around configurable hex row width.

## Verification

- `dotnet build Assembly-CSharp.csproj` passed.
- `dotnet build Assembly-CSharp-Editor.csproj` passed with three pre-existing `LLMGameDesignerAgent` field warnings.
- `dotnet build Tests.csproj` passed when run after the parallel build file lock cleared.
- `openspec.cmd validate --all` passed: 12 specs, 0 failures.
- `tools\poko-cli.cmd validate-core-board` was attempted, but Unity batchmode refused to open because another Unity instance already had this project open.

## Remaining Visual Check

Open the prototype scene in Unity Play Mode and confirm:

- The board fills the view as a dense `7x7` staggered hex field.
- Blue bomb affected tiles visibly pull inward before the center burst.
- Red bomb reads as six-direction line fire across the wider board.
- Rainbow tile shimmer/zap is visually distinct from both bombs.
