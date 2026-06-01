#if UNITY_EDITOR
using System;
using System.Net.Http;
using System.Text;
using UnityEngine;
using PokoPuzzle.AI;

namespace PokoPuzzle.Editor
{
    public sealed class LLMGameDesignerAgent : IGameDesignerAgent
    {
        private readonly string apiKey;
        private readonly string model;
        private readonly IGameDesignerAgent fallback;
        private readonly HttpClient client;

        private readonly string baseUrl;

        public LLMGameDesignerAgent(string apiKey = null, string model = "gpt-4o-mini", string baseUrl = null)
        {
            this.apiKey = apiKey ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            this.model = model;
            this.baseUrl = (baseUrl ?? Environment.GetEnvironmentVariable("OPENAI_BASE_URL") ?? "https://api.openai.com").TrimEnd('/');
            fallback = new HeuristicGameDesignerAgent();
            client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(15);
        }

        public AgentSuggestion Analyze(BoardTelemetry telemetry)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Debug.Log("[LLMGameDesignerAgent] No API key set, falling back to heuristic.");
                return fallback.Analyze(telemetry);
            }

            try
            {
                var result = CallLlm(telemetry);
                if (result.HasValue)
                {
                    return result.Value;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LLMGameDesignerAgent] LLM call failed: {ex.Message}");
            }

            return fallback.Analyze(telemetry);
        }

        private AgentSuggestion? CallLlm(BoardTelemetry t)
        {
            var prompt = new StringBuilder();
            prompt.Append("You are a game designer agent for a line-linker puzzle game. ");
            prompt.Append("Analyze this board telemetry and return a JSON object with these fields: ");
            prompt.Append("difficultyLabel (one word label), summary (one Korean sentence), ");
            prompt.Append("designIntent (one Korean sentence), risk (one Korean sentence), ");
            prompt.Append("recommendedAction (one Korean sentence), ");
            prompt.Append("suggestedMoveLimit (integer), suggestedTargetScore (integer), ");
            prompt.Append("suggestedTileTypes (integer 3-6). ");
            prompt.Append($"Board: {t.Width}x{t.Height}, types {t.TileTypes}, ");
            prompt.Append($"chains {t.PossibleChains}/{t.LongestChain}, ");
            prompt.Append($"score {t.Score}, moves {t.MovesUsed}, ");
            prompt.Append($"combo {t.Combo}, fever {t.FeverActive}, ");
            prompt.Append($"enemy HP {t.EnemyHp}, damage {t.TotalDamageDealt}, ");
            prompt.Append($"bombs {t.BombsCleared}, rainbow {t.RainbowCleared}.");

            var escapedPrompt = prompt.ToString()
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");

            var json =
                "{\n" +
                $"  \"model\": \"{model}\",\n" +
                "  \"messages\": [\n" +
                "    {\"role\": \"system\", \"content\": \"You are a game designer. Respond in JSON.\"},\n" +
                "    {\"role\": \"user\", \"content\": \"" + escapedPrompt + "\"}\n" +
                "  ],\n" +
                "  \"response_format\": {\"type\": \"json_object\"},\n" +
                "  \"temperature\": 0.7,\n" +
                "  \"max_tokens\": 500\n" +
                "}\n";

            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var url = $"{baseUrl}/v1/chat/completions";
            var response = client.PostAsync(url, content).GetAwaiter().GetResult();
            var responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            if (!response.IsSuccessStatusCode)
            {
                Debug.LogWarning($"[LLMGameDesignerAgent] API error {(int)response.StatusCode}: {responseBody}");
                return null;
            }

            return ParseResponse(responseBody, t);
        }

        private static AgentSuggestion? ParseResponse(string responseBody, BoardTelemetry t)
        {
            var envelope = JsonUtility.FromJson<ChatCompletionEnvelope>(responseBody);
            if (envelope?.choices == null || envelope.choices.Length == 0)
            {
                return null;
            }

            var text = envelope.choices[0].message.content;
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var parsed = JsonUtility.FromJson<LlmAgentOutput>(text);
            if (parsed == null)
            {
                return null;
            }

            return new AgentSuggestion(
                parsed.difficultyLabel ?? "LLM Analyzed",
                parsed.summary ?? (text.Length > 80 ? text[..80] : text),
                parsed.designIntent ?? "See summary",
                parsed.risk ?? "See recommended action",
                parsed.recommendedAction ?? "Review telemetry",
                parsed.suggestedMoveLimit > 0 ? parsed.suggestedMoveLimit : t.MovesUsed + 10,
                parsed.suggestedTargetScore > 0 ? parsed.suggestedTargetScore : t.Score + 500,
                parsed.suggestedTileTypes > 0
                    ? Mathf.Clamp(parsed.suggestedTileTypes, 3, 6)
                    : t.TileTypes
            );
        }

        [Serializable]
        private sealed class ChatCompletionEnvelope
        {
            public Choice[] choices = Array.Empty<Choice>();
        }

        [Serializable]
        private sealed class Choice
        {
            public Message message = new();
        }

        [Serializable]
        private sealed class Message
        {
            public string content = string.Empty;
        }

        [Serializable]
        private sealed class LlmAgentOutput
        {
            public string difficultyLabel = string.Empty;
            public string summary = string.Empty;
            public string designIntent = string.Empty;
            public string risk = string.Empty;
            public string recommendedAction = string.Empty;
            public int suggestedMoveLimit;
            public int suggestedTargetScore;
            public int suggestedTileTypes;
        }
    }
}
#endif
