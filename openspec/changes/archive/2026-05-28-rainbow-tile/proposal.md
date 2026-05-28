# Change: Rainbow Bomb and Fever Gauge Split

## Why

The previous prototype treated the top-right charge meter as a Rainbow gauge. A more Poko-style interpretation is that the charge meter represents Fever Time, while Rainbow remains a separate special item or bomb.

## What Changes

- Rename the charge pacing from Rainbow gauge to Fever gauge.
- Filling the gauge activates Fever mode.
- Keep Rainbow as a separate `BombType.Rainbow`.
- Create Rainbow bombs as long-chain rewards.
- Keep Rainbow bomb detonation as same-color full-board clearing.

## Impact

- `combo-fever`: Fever can now be triggered by combo threshold or full Fever gauge.
- `rainbow-tile`: Rainbow is a separate bomb, not the charge meter.
- `ai-game-designer-agent`: Rainbow usage remains tracked through `rainbow_cleared`; Fever remains tracked through fever events.
