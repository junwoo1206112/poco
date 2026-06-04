# Frozen Stone Gap Fix - 2026-06-04

## Folder Audit

- `openspec/specs/special-blocks/spec.md` says Frozen clears when an adjacent tile is cleared and scores 20 points.
- `Assets/PokoPuzzle/Scripts/Core/LineLinkerBoard.cs` owned the actual Frozen, Stone, collapse, refill, and enemy-skill behavior.
- `Assets/PokoPuzzle/Scripts/Core/PokoTile.cs` owned persistent Frozen and Stone visuals.
- `Assets/PokoPuzzle/Scripts/Core/Data/EnemySkillData.cs` maps enemy skills to `Freeze`, `Stone`, and `ColorSwap`.
- Play logs under `md/playtest-logs/` showed repeated `special_block_unlock`, which matched the old implementation but not the current spec.

## Problems Found

- Blizzard/Freeze-created Frozen tiles were being converted back into normal tiles through `ConfigureSubtype(None)`. They were not actually clearing or awarding the Frozen score.
- Frozen behavior was logged as `special_block_unlock`, while the current spec expects a special block clear.
- Column collapse skipped bombs entirely. A bomb in the middle of a column could behave like a fixed obstacle, leaving empty spaces below or around it after clears and refill.
- Stone depended on column compaction to reach the bottom. When other special tiles disrupted compaction, Stone could appear not to fall or clear correctly.

## Fixes

- Frozen tiles now clear through `ClearFrozenTileAt(...)`, award 20 score, increment special-block clear telemetry, and log `special_block_clear`.
- Adjacent clears, bomb clears, and rainbow clears now use the same Frozen clear path.
- Bomb and Rainbow tiles now participate in column compaction like board pieces, so they fall with the column and do not pin empty spaces in place.
- The board still verifies tile coordinates after collapse/refill so `tiles[,]` and `PokoTile.Column/Row` stay aligned.

## Verification

- `dotnet build Assembly-CSharp.csproj`
- `dotnet build Tests.csproj`
- `dotnet test Tests.csproj --no-build`
- `dotnet build Assembly-CSharp-Editor.csproj`
- `openspec.cmd validate --all`
- `git diff --check`
