## Why

This project needs a lightweight spec layer so the portfolio does not drift from "PokoPang-style Line-Linker puzzle with AI planning evidence" into a generic match-3 prototype. OpenSpec keeps gameplay, AI-agent behavior, and portfolio evidence aligned before additional implementation work.

## What Changes

- Establish a playable Unity 2D Line-Linker puzzle MVP as a formal capability.
- Establish a local AI Game Designer Agent as a formal capability.
- Require visible gameplay evidence: chain selection, clear/collapse/refill, score, and tuning suggestions.
- Require written portfolio evidence under `md/`.
- Avoid copying protected PokoPang art, characters, names, sounds, or exact UI.

## Capabilities

### New Capabilities

- `line-linker-puzzle`: Defines the playable board, drag-link, clear, collapse, refill, and scoring behavior.
- `ai-game-designer-agent`: Defines the telemetry input, analysis output, and report/evidence behavior for the AI planner.

### Modified Capabilities

- None.

## Impact

- Affected runtime code: `Assets/PokoPuzzle/Scripts/Core/`.
- Affected AI/planning code: `Assets/PokoPuzzle/Scripts/AI/`.
- Affected editor workflow: `Assets/PokoPuzzle/Scripts/Editor/PokoPrototypeSceneBuilder.cs`.
- Affected documentation: `README.md`, `md/`, `.codex/skills/poko-game-designer/SKILL.md`, and `openspec/`.
