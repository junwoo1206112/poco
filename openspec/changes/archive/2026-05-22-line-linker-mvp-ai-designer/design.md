## Context

The project is a Unity 6000.3 2D prototype for a Trinode AX portfolio. The current prototype already contains runtime board creation, colored circular tiles, drag linking, clearing, collapse/refill, score text, and a heuristic AI designer label.

The next work should keep the prototype small but intentionally documented. The portfolio value comes from the combination of a playable casual puzzle and an AI-assisted design/tuning loop.

## Goals / Non-Goals

**Goals:**

- Keep the puzzle playable in one generated prototype scene.
- Use Unity Input System APIs for pointer input.
- Keep Line-Linker rules simple and testable.
- Shape the AI designer as a contract: telemetry in, tuning suggestion out, readable evidence saved or displayed.
- Use OpenSpec changes to plan feature increments before broad code edits.

**Non-Goals:**

- Do not build a full commercial puzzle framework yet.
- Do not clone PokoPang assets, exact UI, characters, sounds, or names.
- Do not add networked AI dependencies until the local deterministic agent contract is stable.

## Decisions

- Use OpenSpec `spec-driven` profile for this project.
  - Rationale: the workflow is light enough for a solo portfolio but still creates proposal, design, specs, and tasks.
  - Alternative: keep plans only in chat or ad-hoc Markdown. Rejected because the portfolio needs durable planning evidence.

- Keep puzzle rules in runtime C# scripts first.
  - Rationale: the prototype must be playable quickly in Unity and easy to inspect.
  - Alternative: build editor-heavy tools first. Rejected because tool polish matters less than visible puzzle feel at this stage.

- Keep AI designer local and heuristic initially.
  - Rationale: it proves the agent-shaped interface without API keys, latency, or dependency risk.
  - Alternative: start with an LLM agent immediately. Deferred until telemetry and report formats are clear.

- Add an optional LLM design review after the deterministic telemetry loop is stable.
  - Rationale: the project now has playtest JSON, generated level configs, and no-key retuning, so an LLM can review the same contract without becoming the only path.
  - Alternative: replace the heuristic agent with an API-only agent. Rejected because portfolio evidence and local iteration should still work without credentials.

- Plan small level experiment sets after a diagnosis.
  - Rationale: a designer agent should state what to compare next, so a control candidate plus focused variants make the tuning loop easier to playtest and explain.
  - Alternative: keep generating only one retuned level. Deferred as the fallback path when comparison is unnecessary.

- Preserve level-specific latest play logs for experiment candidates.
  - Rationale: applying each candidate should not erase the evidence needed to compare the pack later.
  - Alternative: ask the user to manually rename `latest-playtest.jsonl` after every run. Rejected because the CLI loop should be repeatable.

- Promote the recommended candidate into a named baseline asset and milestone note.
  - Rationale: comparison should lead to a playable next state and durable reviewer evidence, not only another report.
  - Alternative: leave the chosen candidate inside the experiment folder. Rejected because promoted levels should be clear handoff points.

- Add one status entrypoint for the designer loop.
  - Rationale: the project now has separate analyze, plan, compare, and promote steps, so the agent should explain what evidence is missing before the next step.
  - Alternative: keep the workflow only in docs. Rejected because status should be generated from the current workspace artifacts.

- Store rationale and reports in `md/`.
  - Rationale: hiring reviewers can read the workflow even without running Unity.
  - Alternative: only show in-game text. Rejected because portfolio evidence should be durable.

## Risks / Trade-offs

- Prototype may feel too simple -> Mitigation: prioritize visible chain feedback, quick clear/collapse/refill, and readable scoring before adding more systems.
- AI designer may look like a label instead of an agent -> Mitigation: formalize telemetry, suggestions, and generated reports as the next implementation step.
- OpenSpec may add process overhead -> Mitigation: use it only for meaningful gameplay/AI changes, not tiny typo fixes.

## Migration Plan

1. Keep the existing prototype code.
2. Add OpenSpec artifacts for the current MVP and next steps.
3. Validate OpenSpec status before implementing larger gameplay or AI-agent changes.
4. Archive completed OpenSpec changes after the specs represent shipped behavior.

## Open Questions

- Should level data be ScriptableObject-first or JSON-first for the portfolio?
- Should the first AI report be generated inside Unity, from CLI, or both?
- Should the first polished visual pass use generated bitmap assets or simple vector-like Unity sprites?
