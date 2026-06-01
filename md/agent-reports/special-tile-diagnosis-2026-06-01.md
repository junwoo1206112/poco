# Special Tile Diagnosis - 2026-06-01

## Problem

Play feedback suggested that bomb, frozen, and stone tiles were not behaving consistently.

## Findings

- Red bomb pattern used fixed coordinate offsets instead of row-parity-aware hex directions, so its "six directions" did not match the odd-row offset board.
- Blue bomb tests asserted it should be larger than Red, but the intended readable board effect is a compact center-plus-neighbors burst.
- Fever and bomb clears could directly remove Frozen or Stone tiles. This conflicted with the intended special-block rules: Frozen should unlock from adjacent tile removal, and Stone should fall until it reaches the bottom.
- Stone blocks that reached the bottom were removed without awarding the 30-point score and without a second collapse/refill pass, leaving a visible empty gap risk.
- Play log analysis did not count `special_block_clear`, so reports could say zero special blocks even when runtime logs contained special clears.

## Fixes

- `BoardBomb` now follows `HexGridUtility.TryGetDirectionalNeighbor` for parity-aware six-direction Red bomb lines.
- Blue bomb now uses a one-ring hex burst: origin plus six neighboring cells.
- Bomb and Fever clears now clear linkable tiles only, letting Frozen and Stone use their own rules.
- Frozen now unlocks into a normal linkable tile after a neighboring tile is removed instead of being cleared immediately.
- Stone blocks now collapse downward, auto-clear at the bottom, award 30 points, and trigger another collapse before refill.
- Non-linkable Frozen/Stone taps now show feedback without leaving the board in a dragging state.
- Play log analysis now counts `special_block_clear` and records max combo from combat events.

## Verification

```text
dotnet build Assembly-CSharp.csproj
dotnet build Assembly-CSharp-Editor.csproj
dotnet build Tests.csproj
openspec.cmd validate --all
```

All checks passed. `Assembly-CSharp-Editor.csproj` still reports existing `LLMGameDesignerAgent.LlmAgentOutput` unassigned-field warnings unrelated to this fix.
