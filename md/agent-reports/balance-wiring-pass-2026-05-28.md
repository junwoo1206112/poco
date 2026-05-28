# Balance Wiring Pass - 2026-05-28

## Goal

Make the current tuning model behave consistently in play before doing another playtest pass.

## Changes

- Chain score now applies the same combo or Fever multiplier to the actual score that the HUD feedback reports.
- `PokoLevelConfig` now stores `BalanceProfileId`.
- `LineLinkerBoard` selects the active `BalanceProfile` from the level config or scene field before spawning enemies.
- Boss skill cooldowns now apply `SkillCooldownMultiplier` both to the first skill timer and to each selected skill cooldown.
- CLI generated, retuned, experiment, applied, and promoted level reports now preserve or display the balance profile id.

## Balance Meaning

- `default` remains the prototype baseline.
- `readable` lowers HP pressure and charges Fever faster.
- `pressure` can be used for harder runs with slower Fever and stronger enemies.
- `combo` supports capture-friendly runs where long chains and Fever are more likely to define the round.

## Next Playtest Question

After this pass, the next Play Mode check should verify whether the `10000` target still lands correctly now that combo and Fever multipliers affect the real score.
