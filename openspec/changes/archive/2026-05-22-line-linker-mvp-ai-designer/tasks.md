## 1. OpenSpec Setup

- [x] 1.1 Initialize OpenSpec structure in the Unity project.
- [x] 1.2 Configure OpenSpec project context for the Line-Linker puzzle and AI designer agent.
- [x] 1.3 Add OpenSpec proposal, design, specs, and tasks for the MVP direction.

## 2. Current Prototype Alignment

- [x] 2.1 Implement runtime board generation.
- [x] 2.2 Implement same-type drag linking.
- [x] 2.3 Implement chain clear, score, collapse, and refill.
- [x] 2.4 Replace legacy `UnityEngine.Input` pointer reads with Unity Input System APIs.
- [x] 2.5 Add odd-row offset hex grid placement and 6-direction adjacency validation.

## 3. Next Gameplay Work

- [x] 3.1 Add CLI creation and validation for the Core Board & Input prototype.
- [x] 3.2 Add level data for target score, move limit, tile type count, and spawn weights.
- [x] 3.3 Add clearer visual feedback for selected chains and successful clears.
- [x] 3.4 Add win/fail state based on score target and move limit.
- [x] 3.5 Guarantee a clearable three-tile path after board generation and refill.
- [x] 3.6 Keep score UI readable and expose round restart when win/fail locks drag input.
- [x] 3.7 Hint valid next same-type neighbor tiles during an active drag chain.

## 4. Next AI Designer Work

- [x] 4.1 Save AI designer analysis to `md/agent-reports/` as Markdown.
- [x] 4.2 Add a JSON export path for level tuning suggestions.
- [x] 4.3 Add a CLI-friendly report workflow that can be run after play-test data is collected.
- [x] 4.4 Add a CLI workflow that applies generated level tuning to the prototype scene.
- [x] 4.5 Add runtime play logs and a CLI workflow that analyzes playtest telemetry.
- [x] 4.6 Add a CLI workflow that retunes the next level from playtest telemetry.
- [x] 4.7 Add an optional LLM designer review CLI that reads playtest analysis JSON and stores request/report evidence.
- [x] 4.8 Add a CLI workflow that turns playtest diagnosis into control, readability, and combo level experiments.
- [x] 4.9 Preserve level-specific play logs and compare experiment candidate results with a CLI report.
- [x] 4.10 Promote the recommended experiment winner and save a portfolio milestone note.
- [x] 4.11 Add a designer-loop status CLI that reports the current evidence gap and next action.

## 5. Verification

- [x] 5.1 Validate OpenSpec artifacts with `openspec.cmd validate line-linker-mvp-ai-designer`.
- [x] 5.2 Run the Unity prototype and confirm no console errors during basic drag-link play.
- [x] 5.3 Update README and portfolio notes after the next playable milestone.
- [x] 5.4 Run `tools\poko-cli.cmd validate-core-board` after Unity recompiles editor scripts.
