#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace PokoPuzzle.Editor
{
    public sealed class PlayLogAnalysis
    {
        public string LevelId { get; private set; } = "unknown";
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int TileTypes { get; private set; }
        public int MoveLimit { get; private set; }
        public int TimeLimit { get; private set; }
        public int TargetScore { get; private set; }
        public string Result { get; private set; } = "unfinished";
        public int FinalScore { get; private set; }
        public int MovesUsed { get; private set; }
        public int ValidMoves { get; private set; }
        public int InvalidMoves { get; private set; }
        public float AverageChainLength { get; private set; }
        public float AverageScorePerValidMove { get; private set; }
        public string DifficultyLabel { get; private set; } = "Normal";
        public string Diagnosis { get; private set; } = "Playtest data is balanced enough for the next pass.";
        public string Risk { get; private set; } = "No strong risk detected.";
        public string Action { get; private set; } = "Continue with visual polish and one more playtest.";
        public int SuggestedMoveLimit { get; private set; }
        public int SuggestedTargetScore { get; private set; }
        public int SuggestedTileTypes { get; private set; }
        public int MaxCombo { get; private set; }
        public int FeverTriggers { get; private set; }
        public int TotalDamageDealt { get; private set; }
        public int BombsGenerated { get; private set; }
        public int BombsDetonated { get; private set; }
        public int SpecialBlocksCleared { get; private set; }
        public int RainbowCleared { get; private set; }
        public int TimeLeft { get; private set; }
        public int SuggestedRegularEnemyHp { get; private set; }
        public int SuggestedBossHp { get; private set; }

        public static PlayLogAnalysis FromFile(string logPath)
        {
            var analysis = new PlayLogAnalysis();
            var totalChainLength = 0;
            var totalGainedScore = 0;

            foreach (var line in File.ReadAllLines(logPath))
            {
                if (line.Contains("\"event\":\"session_start\""))
                {
                    analysis.LevelId = GetString(line, "levelId", analysis.LevelId);
                    analysis.Width = GetInt(line, "width", analysis.Width);
                    analysis.Height = GetInt(line, "height", analysis.Height);
                    analysis.TileTypes = GetInt(line, "tileTypes", analysis.TileTypes);
                    analysis.MoveLimit = GetInt(line, "moveLimit", analysis.MoveLimit);
                    analysis.TimeLimit = GetInt(line, "timeLimit", analysis.TimeLimit);
                    analysis.TargetScore = GetInt(line, "targetScore", analysis.TargetScore);
                }
                else if (line.Contains("\"event\":\"move\""))
                {
                    var valid = GetBool(line, "valid");
                    var chainLength = GetInt(line, "chainLength", 0);
                    var gainedScore = GetInt(line, "gainedScore", 0);
                    analysis.FinalScore = GetInt(line, "score", analysis.FinalScore);
                    analysis.MovesUsed = GetInt(line, "movesUsed", analysis.MovesUsed);

                    if (valid)
                    {
                        analysis.ValidMoves++;
                        totalChainLength += chainLength;
                        totalGainedScore += gainedScore;
                    }
                    else
                    {
                        analysis.InvalidMoves++;
                    }
                }
                else if (line.Contains("\"event\":\"end\""))
                {
                    analysis.Result = GetString(line, "result", analysis.Result);
                    analysis.FinalScore = GetInt(line, "score", analysis.FinalScore);
                    analysis.MovesUsed = GetInt(line, "movesUsed", analysis.MovesUsed);
                    analysis.TimeLeft = GetInt(line, "timeLeft", 0);
                }
                else if (line.Contains("\"event\":\"combat\""))
                {
                    var combatEvent = GetString(line, "combatEvent", "");
                    switch (combatEvent)
                    {
                        case "enemy_damage":
                            analysis.TotalDamageDealt += GetInt(line, "value1", 0);
                            break;
                        case "bomb_placed":
                            analysis.BombsGenerated++;
                            break;
                        case "bomb_detonate":
                            analysis.BombsDetonated++;
                            break;
                        case "rainbow_cleared":
                            analysis.RainbowCleared++;
                            break;
                    }
                }
                else if (line.Contains("\"event\":\"fever\""))
                {
                    if (string.Equals(GetString(line, "state", ""), "start", StringComparison.OrdinalIgnoreCase))
                    {
                        analysis.FeverTriggers++;
                    }
                }
                else if (line.Contains("\"combat\""))
                {
                    var combo = GetInt(line, "combo", 0);
                    if (combo > analysis.MaxCombo)
                    {
                        analysis.MaxCombo = combo;
                    }
                }
            }

            analysis.AverageChainLength = analysis.ValidMoves == 0 ? 0f : (float)totalChainLength / analysis.ValidMoves;
            analysis.AverageScorePerValidMove = analysis.ValidMoves == 0 ? 0f : (float)totalGainedScore / analysis.ValidMoves;
            analysis.SuggestedMoveLimit = analysis.MoveLimit;
            analysis.SuggestedTargetScore = analysis.TargetScore;
            analysis.SuggestedTileTypes = analysis.TileTypes;
            analysis.Evaluate();
            return analysis;
        }

        private void Evaluate()
        {
            var fail = string.Equals(Result, "fail", StringComparison.OrdinalIgnoreCase);
            var clear = string.Equals(Result, "clear", StringComparison.OrdinalIgnoreCase);
            var timeUp = string.Equals(Result, "time_up", StringComparison.OrdinalIgnoreCase);
            var invalidRate = ValidMoves + InvalidMoves == 0 ? 0f : (float)InvalidMoves / (ValidMoves + InvalidMoves);

            if (FeverTriggers >= 2)
            {
                DifficultyLabel = "Fever Master";
                Diagnosis = $"Player triggered Fever {FeverTriggers} times (max combo {MaxCombo}).";
                Risk = "Board may be too easy if Fever chains clear everything.";
                Action = "Raise target score, add tile types, or increase enemy HP.";
                SuggestedMoveLimit = Mathf.Max(8, MoveLimit - 2);
                SuggestedTargetScore = TargetScore + 500;
                SuggestedTileTypes = Mathf.Min(6, TileTypes + 1);
                return;
            }

            if (TotalDamageDealt >= 200)
            {
                DifficultyLabel = "Combat Focus";
                Diagnosis = $"Player dealt {TotalDamageDealt} damage to enemy.";
                Risk = "Damage output may carry the round regardless of score.";
                Action = "Consider increasing enemy HP or adding special blocks.";
                SuggestedMoveLimit = MoveLimit;
                SuggestedTargetScore = TargetScore + 300;
                SuggestedTileTypes = TileTypes;
                return;
            }

            if (timeUp && FinalScore >= TargetScore * 2 && TargetScore > 0)
            {
                DifficultyLabel = "Easy";
                Diagnosis = $"Player scored {FinalScore} ({FinalScore * 100f / TargetScore:F0}% of target) with full time used. Level is too easy.";
                Risk = "Players earn too many points too quickly for the time limit.";
                Action = "Increase time limit or raise target score for the next pass.";
                SuggestedMoveLimit = MoveLimit;
                SuggestedTargetScore = TargetScore + 500;
                SuggestedTileTypes = Mathf.Min(6, TileTypes + 1);
                return;
            }

            if (timeUp && AverageChainLength < 3.0f && FinalScore < TargetScore * 0.75f && TargetScore > 0)
            {
                DifficultyLabel = "Hard";
                Diagnosis = $"Player scored only {FinalScore} with short chains (avg {AverageChainLength:F2}). Level is too hard.";
                Risk = "The board may not surface readable 3+ paths often enough within the time limit.";
                Action = "Lower tile types or increase time limit for the next pass.";
                SuggestedMoveLimit = MoveLimit;
                SuggestedTargetScore = Mathf.Max(600, TargetScore - 200);
                SuggestedTileTypes = Mathf.Max(3, TileTypes - 1);
                return;
            }

            if (invalidRate > 0.28f)
            {
                DifficultyLabel = "Readability Risk";
                Diagnosis = "Many short invalid releases happened during play.";
                Risk = "Players may not understand which hex neighbors are valid.";
                Action = "Improve selected-chain feedback and consider hinting valid next tiles.";
                SuggestedMoveLimit = MoveLimit + 2;
                SuggestedTargetScore = TargetScore;
                SuggestedTileTypes = TileTypes;
                return;
            }

            if (clear && TimeLeft > 0)
            {
                if (TimeLimit > 0)
                {
                    var timeRatio = (float)TimeLeft / TimeLimit;

                    if (timeRatio > 0.5f)
                    {
                        DifficultyLabel = "Easy";
                        Diagnosis = $"Player cleared with {TimeLeft}s left ({timeRatio * 100f:F0}% of {TimeLimit}s). Level is too easy.";
                        Risk = "Excess time means the time limit is too generous for the current board.";
                        Action = $"Reduce time limit to {Mathf.CeilToInt(TimeLimit * 0.7f)}s or raise target score.";
                        SuggestedMoveLimit = Mathf.Max(8, MoveLimit - 2);
                        SuggestedTargetScore = TargetScore + 300;
                        SuggestedTileTypes = Mathf.Min(6, TileTypes + 1);
                        return;
                    }

                    if (timeRatio < 0.2f)
                    {
                        DifficultyLabel = "Hard";
                        Diagnosis = $"Player barely cleared with only {TimeLeft}s left ({timeRatio * 100f:F0}% of {TimeLimit}s). High time pressure.";
                        Risk = "Time limit creates stressful gameplay that may feel unfair.";
                        Action = $"Increase time limit to {Mathf.CeilToInt(TimeLimit * 1.2f)}s for a more comfortable pace.";
                        SuggestedMoveLimit = MoveLimit + 2;
                        SuggestedTargetScore = Mathf.Max(600, TargetScore - 200);
                        SuggestedTileTypes = Mathf.Max(3, TileTypes - 1);
                        return;
                    }
                }
            }

            DifficultyLabel = "Normal";
            Diagnosis = "Playtest telemetry suggests a workable level baseline.";
            Risk = "Moment-to-moment juice may still be too quiet for portfolio capture.";
            Action = "Keep the level values and polish clear/combo feedback.";
            SuggestedMoveLimit = MoveLimit;
            SuggestedTargetScore = TargetScore + 100;
            SuggestedTileTypes = TileTypes;
        }

        private static int GetInt(string line, string key, int fallback)
        {
            var raw = GetRaw(line, key);
            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : fallback;
        }

        private static bool GetBool(string line, string key)
        {
            return string.Equals(GetRaw(line, key), "true", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetString(string line, string key, string fallback)
        {
            var token = $"\"{key}\":\"";
            var start = line.IndexOf(token, StringComparison.Ordinal);
            if (start < 0)
            {
                return fallback;
            }

            start += token.Length;
            var end = line.IndexOf('"', start);
            return end < 0 ? fallback : line.Substring(start, end - start);
        }

        private static string GetRaw(string line, string key)
        {
            var token = $"\"{key}\":";
            var start = line.IndexOf(token, StringComparison.Ordinal);
            if (start < 0)
            {
                return string.Empty;
            }

            start += token.Length;
            var end = start;
            while (end < line.Length && line[end] != ',' && line[end] != '}')
            {
                end++;
            }

            return line.Substring(start, end - start).Trim().Trim('"');
        }
    }
}
#endif
