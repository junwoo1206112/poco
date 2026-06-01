# Rainbow Gauge Reference Pass - 2026-06-01

## Reference Read

PokoPang references describe the top-right charge as a Rainbow Gauge. Blocks cleared by tracing or bomb effects increase the Rainbow Gauge, and when it reaches MAX a Rainbow bomb appears. Fever/Star Fever exists as a separate timed or combo-related state, not as the top-right charge meter.

## Project Mismatch

- The HUD showed a large `FEVER` gauge and a small secondary `RAINBOW` gauge.
- `LineLinkerBoard` filled a Fever gauge from cleared blocks and could trigger Fever from that gauge.
- This made the Rainbow bomb reward feel secondary even though the PokoPang reference treats Rainbow Gauge as the visible charge lane.

## Changes

- Removed block-clear charging for Fever.
- Kept Fever as a combo/timed state triggered by combo threshold.
- Promoted Rainbow Gauge to the single top-right charge bar in `BoardHudRenderer`.
- Chain clears and non-rainbow bomb clears now fill Rainbow Gauge.
- When Rainbow Gauge reaches MAX, it resets and creates a `BombType.Rainbow` bomb.
- Updated Rainbow spec and prior UI notes so future agents do not reinterpret the top-right gauge as Fever.

## Compatibility Note

`BalanceProfileData.FeverGaugeMultiplier` still exists as a legacy data-pipeline field. Runtime now uses that value as the Rainbow charge multiplier until the Excel/data schema is renamed in a dedicated data migration pass.
