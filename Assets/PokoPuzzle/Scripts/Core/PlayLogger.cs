using System.IO;
using System.Text;
using UnityEngine;

namespace PokoPuzzle.Core
{
    public sealed class PlayLogger
    {
        private readonly bool enabled;
        private readonly string logPath;
        private readonly string levelLogPath;

        public PlayLogger(bool enabled, string logPath, string levelLogPath)
        {
            this.enabled = enabled;
            this.logPath = logPath;
            this.levelLogPath = levelLogPath;
        }

        public void WriteSessionStart(string levelId, int width, int height, int tileTypes,
            int moveLimit, int timeLimit, int targetScore, bool useHexGrid)
        {
            if (!enabled) return;

            var line = "{" +
                "\"event\":\"session_start\"," +
                $"\"levelId\":\"{EscapeJson(levelId)}\"," +
                $"\"width\":{width}," +
                $"\"height\":{height}," +
                $"\"tileTypes\":{tileTypes}," +
                "\"roundType\":\"timed_score_attack\"," +
                $"\"moveLimit\":{moveLimit}," +
                $"\"timeLimit\":{timeLimit}," +
                $"\"targetScore\":{targetScore}," +
                $"\"useHexGrid\":{JsonBool(useHexGrid)}" +
                "}";

            WriteTo(logPath, line);
            if (!string.IsNullOrWhiteSpace(levelLogPath) && levelLogPath != logPath)
            {
                WriteTo(levelLogPath, line);
            }
        }

        public void LogMove(bool valid, int chainLength, int gainedScore, int score,
            int movesUsed, int moveLimit, int timeRemaining, int possibleChains, int longestChain)
        {
            if (!enabled) return;

            AppendToBoth(
                "{" +
                "\"event\":\"move\"," +
                $"\"valid\":{JsonBool(valid)}," +
                $"\"chainLength\":{chainLength}," +
                $"\"gainedScore\":{gainedScore}," +
                $"\"score\":{score}," +
                $"\"movesUsed\":{movesUsed}," +
                $"\"moveTarget\":{moveLimit}," +
                $"\"movesOverTarget\":{Mathf.Max(0, movesUsed - moveLimit)}," +
                $"\"timeLeft\":{timeRemaining}," +
                $"\"possibleChains\":{possibleChains}," +
                $"\"longestChain\":{longestChain}" +
                "}");
        }

        public void LogEndState(string result, int score, int movesUsed, int timeRemaining,
            int enemyCount, int targetScore, int moveLimit)
        {
            if (!enabled) return;

            AppendToBoth(
                "{" +
                "\"event\":\"end\"," +
                $"\"result\":\"{EscapeJson(result)}\"," +
                $"\"score\":{score}," +
                $"\"movesUsed\":{movesUsed}," +
                $"\"timeLeft\":{timeRemaining}," +
                $"\"enemyCount\":{enemyCount}," +
                $"\"targetScore\":{targetScore}," +
                $"\"moveLimit\":{moveLimit}" +
                "}");
        }

        public void LogFeverEvent(string state, int comboCount, int timeRemaining)
        {
            if (!enabled) return;

            AppendToBoth(
                "{" +
                "\"event\":\"fever\"," +
                $"\"state\":\"{EscapeJson(state)}\"," +
                $"\"combo\":{comboCount}," +
                $"\"timeLeft\":{timeRemaining}" +
                "}");
        }

        public void LogCombatEvent(string combatEvent, int value1, int value2,
            int comboCount, bool feverActive, int enemyHp, int enemyMaxHp, int timeRemaining)
        {
            if (!enabled) return;

            AppendToBoth(
                "{" +
                "\"event\":\"combat\"," +
                $"\"combatEvent\":\"{EscapeJson(combatEvent)}\"," +
                $"\"value1\":{value1}," +
                $"\"value2\":{value2}," +
                $"\"combo\":{comboCount}," +
                $"\"feverActive\":{JsonBool(feverActive)}," +
                $"\"enemyHp\":{enemyHp}," +
                $"\"enemyMaxHp\":{enemyMaxHp}," +
                $"\"timeLeft\":{timeRemaining}" +
                "}");
        }

        private void WriteTo(string path, string line)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, line + "\n");
        }

        private void AppendToBoth(string line)
        {
            AppendTo(logPath, line);
            if (!string.IsNullOrWhiteSpace(levelLogPath) && levelLogPath != logPath)
            {
                AppendTo(levelLogPath, line);
            }
        }

        private static void AppendTo(string path, string line)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.AppendAllText(path, line + "\n");
        }

        private static string EscapeJson(string value)
        {
            var builder = new StringBuilder();
            foreach (var character in value)
            {
                switch (character)
                {
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        builder.Append(character);
                        break;
                }
            }

            return builder.ToString();
        }

        private static string JsonBool(bool value)
        {
            return value ? "true" : "false";
        }

        public static string BuildLevelPlayLogPath(string levelId, string basePath)
        {
            return $"md/playtest-logs/by-level/{SafeFileSegment(levelId)}-latest.jsonl";
        }

        private static string SafeFileSegment(string value)
        {
            var builder = new StringBuilder();
            foreach (var character in value)
            {
                builder.Append(char.IsLetterOrDigit(character) || character == '_' || character == '-'
                    ? character
                    : '_');
            }

            return builder.Length == 0 ? "unknown" : builder.ToString();
        }
    }
}
