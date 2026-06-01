# Time Bonus Tile Reference Pass - 2026-06-01

## Reference Check

- I could not confirm a dedicated PokoPang board tile that directly adds time.
- A PokoPang strategy article documents time-extension as an animal skill: Papaoru's `Time 5` plus Double Attack becomes 10 time items, extending play time by 20 seconds.
- Design interpretation: keep this project away from a one-to-one clone and treat `Clock` as a Poko-style Time Bonus tile that borrows the time-extension fantasy without copying a specific original tile.

Reference used: https://pokopan.hajihaji-lemon.com/?p=13642

## Changes

- Updated Clock special block visuals from generic green tint to a gold time-bonus tile with a clock face and plus badge.
- Added `BoardEffectRenderer.PlayTimeBonus` for `TIME +2s`, gold flash, and ring feedback.
- Centralized Clock clear handling through `LineLinkerBoard.ApplyTimeBonus`, so chain clears, bomb clears, and Rainbow clears all show the same time bonus feedback.
- Updated `special-blocks` spec so Clock now requires visible `TIME +2s` feedback and a gold clock-plus icon.

## Design Intent

- The tile should read as a helpful bonus, not an enemy status block.
- The reward stays small at +2 seconds because it can appear as a board subtype and can also be swept by bombs or Rainbow clears.
- This keeps the timed score-attack pressure intact while giving the player a readable lucky pickup moment.
