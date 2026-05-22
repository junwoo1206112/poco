# LLM Designer Review CLI

## Why This Exists

The project already has a deterministic Game Designer Agent loop:

1. `analyze-playlog` turns runtime telemetry into Markdown and JSON.
2. `retune-level` converts that analysis into the next `PokoLevelConfig`.

The optional LLM layer should extend that loop, not replace it. It reads the same playtest analysis JSON so the AI designer role stays grounded in gameplay evidence.

## Command

```cmd
tools\poko-cli.cmd llm-design-review
```

Optional arguments:

```cmd
tools\poko-cli.cmd llm-design-review --inputPath md/agent-reports/latest-playtest-analysis.json --model gpt-5.4-mini --reportPath md/llm-reports/latest-designer-review.md
```

## Saved Evidence

- Request packet: `md/llm-reports/latest-designer-request.json`
- Markdown designer review: `md/llm-reports/latest-designer-review.md`
- Raw API response when a request is sent: `md/llm-reports/latest-designer-response.json`

## No-Key Behavior

If `OPENAI_API_KEY` is not available to the Unity batchmode process, the command still saves:

- the Responses request packet
- a pending Markdown report that explains the skipped API call

That keeps local iteration deterministic and gives a reviewer visible evidence of the LLM integration path.

## Planner Role

The LLM review should answer planning questions, not act as a random content generator:

- What did the telemetry say about player feel?
- Which tuning risk should be tested next?
- Should move limit, target score, tile type count, or feedback clarity change first?
- What evidence should be captured for the portfolio milestone?
