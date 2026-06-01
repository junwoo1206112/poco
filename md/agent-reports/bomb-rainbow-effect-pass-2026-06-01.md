# Bomb and Rainbow Effect Pass - 2026-06-01

## Diagnosis

- Rainbow bomb logic already removed the most common linkable tile color, but the effect was too subtle and the play log stored `value1` as `1`, so the result looked weaker than the actual score change.
- Red and Blue bombs cleared synchronously. Blue bomb especially needed a visible pull-in beat before the burst so the player can read the special tile as an event instead of an instant board mutation.
- Multiple expired bombs could continue detonating in the same `TickBombs` loop after the first bomb started resolving a visual effect.

## Changes

- Added `BoardEffectRenderer.PlayRainbowDetonation` so tapping a Rainbow bomb previews the targeted color, pulls motes from affected tiles into the Rainbow origin, shakes the camera, and then clears/scored tiles.
- Added `BoardEffectRenderer.PlayBombPull` so Red and Blue bombs announce their detonation and pull affected tiles toward the bomb center before clearing.
- Strengthened the Shorts-style special-tile read: Red Bomb now emits expanding warning rings and brighter six-direction zaps, Blue Bomb adds larger shock rings and a spiral pull, and Rainbow adds prism zaps plus target-color pulses before the board mutates.
- Replaced the shared tint-only bomb sprite with separate generated sprites: Red Bomb has a six-direction explosive star, while Blue Bomb has a concentric vortex mark.
- Rechecked web references and adjusted Blue Bomb away from an outward spread read: official LINE material describes the 10+ Super Bomb as clearing surrounding blocks all at once, so the visual now uses contracting rings and inward pull before the central burst.
- Rebalanced Blue Bomb mechanics to an origin-adjacent 2-ring hex burst. This keeps Blue position-sensitive near edges and corners while giving the Super Bomb enough local pull to feel satisfying on the wider 7x9 board.
- Updated Blue Bomb to move the actual affected tile transforms toward the bomb center before clearing, instead of only spawning separate pull motes.
- Rechecked bomb references and adjusted Red Bomb to the 7+ line-clear fantasy: it now fires flame-line segments through each affected direction, shakes harder at peak, and pushes affected tiles outward before clearing.
- Rechecked Rainbow references and strengthened the no-range one-color clear read: all target-color tiles now pulse with rainbow shimmer before removal, the Rainbow origin emits board-wide prism sweeps, and targets dissolve into prism shards.
- Converted normal bomb detonation into a coroutine that collects targets first, plays the pull-in effect, then applies score, special block side effects, gauge fill, collapse, HUD refresh, designer analysis, and end-state checks.
- Updated Rainbow play logging so `rainbow_cleared.value1` now records the number of removed tiles and `value2` records gained score.
- Stopped bomb timer processing after one auto-detonation starts, preventing overlapping same-frame bomb resolutions.

## Design Intent

- Rainbow bomb should feel like a board-wide color clear: tap once, see the chosen color respond, gain visible score, then refill.
- Blue bomb should feel heavier than Red bomb: affected nearby tiles gather into the bomb before the burst.
- Red bomb should read as an overwhelming line-clearing explosion from a 7+ chain, while Blue bomb should read as a heavier 10+ Super Bomb implosion.
- Blue bomb should not read as a wave expanding outward from the origin; it should read as surrounding blocks being gathered and removed in one beat.
- Blue bomb affected tiles should visibly travel into the bomb center so the player can read the local implosion before the board refills.
- Balance contrast: Blue has a compact local radius and loses value near the edge; Red uses board-boundary line clears and therefore feels like the screen-sweeping joker.
- Rainbow bomb should not read as a radius or line effect; it should read as a full-board one-color clear with every matching tile visibly selected before disappearing.
- The implementation keeps the current rules: Rainbow targets the most common linkable color, Red bomb uses 6 hex directions, and Blue bomb uses the origin-adjacent 2-ring hex burst area.

## Verification

- Passed: `dotnet build Assembly-CSharp.csproj`
- Passed with one transient file-copy retry warning and no errors: `dotnet build Tests.csproj`
- Passed with existing editor serialization-field warnings and no errors: `dotnet build Assembly-CSharp-Editor.csproj`
- Passed: `openspec.cmd validate --all`
- Latest pass: `dotnet build Assembly-CSharp.csproj`, `dotnet build Tests.csproj`, `dotnet build Assembly-CSharp-Editor.csproj`, and `openspec.cmd validate --all` all passed. Editor build still reports the pre-existing `LLMGameDesignerAgent.LlmAgentOutput` unused-field warnings.
