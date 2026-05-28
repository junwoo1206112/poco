# Poko Puzzle Effect Direction

Date: 2026-05-28

## Purpose

This document defines the next visual-effect direction for the Poko Puzzle prototype so a future AI agent or developer can implement effects without re-deciding the presentation each time.

The project should feel like a compact line-link puzzle battle. Effects must make three things readable:

- The player is drawing a valid chain.
- Clearing blocks sends power into enemy or boss damage.
- Fever, Rainbow, and boss skills are special moments, not ordinary clears.

Do not copy protected PokoPang art, sounds, characters, exact UI, or exact effect assets. Use the same arcade puzzle-battle readability, but keep the presentation original.

## Current State

Already implemented:

- Selected tiles scale up in `PokoTile.SetSelected`.
- Link hints scale up in `PokoTile.SetLinkHint`.
- Cleared tiles pop, shrink, fade, and then destroy.
- Bomb tiles have distinct generated sprites.
- Rainbow bomb has a generated rainbow sprite.
- Bombs flash near auto-detonation.
- HUD shows score, time, Fever gauge, combo, enemy HP, boss badge, and feedback text.
- Boss skill concepts exist in gameplay: `Freeze`, `Stone`, and `ColorSwap`.
- Boss and enemy names now support those concepts in Excel and runtime DBs.
- Frozen and Stone tiles keep stronger persistent overlays.

Main missing feel:

- Timing and clutter still need a Play mode tuning pass after the implemented effects are reviewed in motion.

## Effect Pillars

### 1. Chain Readability

Goal: The player should understand the chain path before release.

Implementation direction:

- Keep selected tile scale at roughly `1.15-1.18`.
- Add a soft selected outline or additive glow using a child `SpriteRenderer`.
- Add a light pulse to hinted next tiles instead of a static scale only.
- Make the `LineRenderer` slightly brighter and thicker during valid chains of 3 or more.
- For invalid 1-2 chains, avoid harsh failure effects; show a small dull blue feedback pop.

Priority: High

Suggested files:

- `Assets/PokoPuzzle/Scripts/Core/PokoTile.cs`
- `Assets/PokoPuzzle/Scripts/Core/LineLinkerBoard.cs`

### 2. Clear Impact

Goal: Releasing a valid chain should feel satisfying even before score or enemy damage is noticed.

Implementation direction:

- On release, selected tiles should briefly pop larger, fade, then destroy.
- Timing target: `0.08s` pop up, `0.10-0.14s` fade/shrink.
- Spawn small score text near the average position of the cleared chain.
- Floating score text should move upward and fade out in `0.6-0.8s`.
- Keep collapse/refill fast after the clear beat, not before it.

Priority: High

Suggested helper:

- Add `BoardEffectRenderer` as a small runtime component owned by `LineLinkerBoard`.
- Keep it code-generated first: no particle package dependency required.

Suggested files:

- `Assets/PokoPuzzle/Scripts/Core/BoardEffectRenderer.cs`
- `Assets/PokoPuzzle/Scripts/Core/LineLinkerBoard.cs`

### 3. Board-To-Enemy Damage

Goal: The combat layer should be legible: clears produce damage, not just score.

Implementation direction:

- After a valid clear, spawn 1-3 small energy motes from the chain center toward the enemy HP lane.
- Chain length controls intensity:
  - `3-5`: one mote
  - `6-9`: two motes
  - `10+`: three motes and a brighter hit flash
- On hit, pulse the enemy HP bar or boss badge.
- Show damage text near the enemy HP bar: `-30`, `-80`, etc.
- Boss hit should use a red/orange impact; regular enemy hit should use blue-white.

Priority: High

Suggested files:

- `Assets/PokoPuzzle/Scripts/Core/BoardEffectRenderer.cs`
- `Assets/PokoPuzzle/Scripts/Core/BoardHudRenderer.cs`
- `Assets/PokoPuzzle/Scripts/Core/LineLinkerBoard.cs`

### 4. Boss Spawn

Goal: A boss should feel like a phase change.

Implementation direction:

- When `enemy.Wave > 0` spawns, show a short top-lane warning.
- Sequence:
  - `BOSS` badge flashes red/gold for `0.4s`.
  - Boss name appears centered under the top HUD for `1.0s`.
  - HP bar fills from 0 to full over `0.35s`.
- Keep the board playable quickly; do not block input for more than about half a second.

Priority: Medium

Suggested files:

- `Assets/PokoPuzzle/Scripts/Core/BoardHudRenderer.cs`
- `Assets/PokoPuzzle/Scripts/Core/LineLinkerBoard.cs`

### 5. Boss Skill Identity

Goal: Each boss skill should be readable before and after it lands.

Skill mapping:

- Frostbind Queen / Blizzard Wyrm: `Freeze`
- Stoneheart Golem: `Stone`
- Prism Trickster / Chroma Overlord: `ColorSwap`

Freeze direction:

- Anticipation: cool blue ring or horizontal sweep across board.
- Impact: target tiles flash pale blue.
- Persistent read: frozen tiles keep icy tint and a crisp outline.
- Feedback text color: blue-white.

Stone direction:

- Anticipation: short gray dust pulse from boss lane.
- Impact: target tiles drop-scale slightly, then return with stone tint.
- Persistent read: stone tiles should look heavier and less saturated.
- Feedback text color: gray.

ColorSwap direction:

- Anticipation: prism shimmer across random target tiles.
- Impact: tile color flips after a brief white/pink flicker.
- Persistent read: no long-term overlay; the changed color is the effect.
- Feedback text color: pink/prism.

Priority: High

Suggested files:

- `Assets/PokoPuzzle/Scripts/Core/BoardEffectRenderer.cs`
- `Assets/PokoPuzzle/Scripts/Core/PokoTile.cs`
- `Assets/PokoPuzzle/Scripts/Core/LineLinkerBoard.cs`

### 6. Fever

Goal: Fever should read as a temporary high-energy state that changes clear feel.

Implementation direction:

- On Fever start:
  - Flash `FEVER!` near top center.
  - Pulse the Fever gauge from orange to gold.
  - Brief warm screen flash behind HUD, not full opaque overlay.
- During Fever:
  - Link line color shifts to gold/orange.
  - Clear pop timing can be slightly faster.
  - Extra cleared neighbor tiles should use a small secondary burst so the cascade is visible.
- On Fever end:
  - Gauge returns to normal.
  - Avoid a negative-feeling fail sound or harsh dim.

Priority: Medium

Suggested files:

- `Assets/PokoPuzzle/Scripts/Core/BoardHudRenderer.cs`
- `Assets/PokoPuzzle/Scripts/Core/LineLinkerBoard.cs`

### 7. Rainbow Bomb

Goal: Rainbow bomb should be the premium clear effect.

Implementation direction:

- On tap:
  - Pause regular visual flow for about `0.12s`.
  - Highlight all tiles of the target color with rainbow edge shimmer.
  - Clear highlighted tiles together with a prism burst.
- Score text should say `Rainbow +N`.
- Avoid removing the board instantly with no pre-read; the player should see why those tiles vanished.

Priority: Medium

Suggested files:

- `Assets/PokoPuzzle/Scripts/Core/BoardEffectRenderer.cs`
- `Assets/PokoPuzzle/Scripts/Core/LineLinkerBoard.cs`
- `Assets/PokoPuzzle/Scripts/Core/PokoTile.cs`

## Recommended Implementation Order

1. Add `BoardEffectRenderer` for code-generated floating text, simple flashes, and energy motes. `Done in first pass`
2. Route valid chain clears through clear pop and floating score. `Done in final follow-up pass`
3. Add damage motes and HP/boss badge pulse. `Done in first follow-up pass`
4. Add boss skill anticipation/impact effects for Freeze, Stone, and ColorSwap. `Done in first follow-up pass with sweep motes and skill pulse`
5. Add Fever start and active-state visual treatment. `Done in final follow-up pass with active gauge glow and line color`
6. Add Rainbow pre-highlight and prism burst. `Done in first follow-up pass with a short target preview delay`

This order is intentional. Steps 1-3 improve every move and make the core loop feel better immediately. Steps 4-6 make special moments more memorable once the base loop reads clearly.

## Technical Notes

- Keep first-pass effects code-generated with `SpriteRenderer`, `TextMesh`, simple `Coroutine`s, and existing Unity primitives.
- Avoid introducing third-party VFX dependencies until the core feel is validated.
- Effects should be optional and fail-safe. If an effect reference is missing, gameplay must still continue.
- Do not delay board logic too much. If a visual delay is needed, keep it below `0.2s` except Rainbow, which can use a slightly longer premium beat.
- Use local helper methods and one new renderer class before adding a broader effect framework.
- Preserve deterministic play logging. Effects should not change score, damage, tile selection, or AI telemetry.

## Acceptance Checklist

- A valid 3+ clear has a visible pop, score float, and board refill.
- A long chain visibly feels stronger than a short chain.
- Enemy damage is readable without inspecting numbers only.
- Boss spawn is distinguishable from regular enemy spawn.
- Freeze, Stone, and ColorSwap are visually distinct before or at impact.
- Fever is readable while active.
- Rainbow bomb has a pre-clear highlight and premium burst.
- The first 60-second playtest remains fast and not visually cluttered.

## Portfolio Framing

This effect pass should be presented as a readability and feedback layer over the existing systems. It supports the portfolio claim that the project is not only implementing puzzle logic, but also exposing gameplay state clearly enough for a product engineer and AI designer agent workflow.
