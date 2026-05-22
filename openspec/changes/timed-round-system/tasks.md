## 1. Timer Implementation

- [x] 1.1 Add `roundTime` (60f) and `timeRemaining` fields to `LineLinkerBoard`.
- [x] 1.2 Change default `moveLimit` to 999 (time is primary constraint).
- [x] 1.3 Add timer countdown in `Update()` — when time reaches 0, end the round.
- [x] 1.4 Update `RefreshHud()` and `OnGUI()` to show remaining time.
- [x] 1.5 Update `EvaluateEndState()` to check time-based end before move limit.
- [x] 1.6 Reset timer in `RestartRound()`.
- [x] 1.7 Include `timeLeft` in play log move and end events.

## 2. Verification

- [ ] 2.1 Open Unity, compile, confirm no errors.
- [ ] 2.2 Play and verify 60-second countdown displays and game ends on timeout.
