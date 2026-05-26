namespace PokoPuzzle.AI
{
    public sealed class HeuristicGameDesignerAgent : IGameDesignerAgent
    {
        public AgentSuggestion Analyze(BoardTelemetry telemetry)
        {
            var boardSize = telemetry.Width * telemetry.Height;
            var chainDensity = boardSize == 0 ? 0f : (float)telemetry.PossibleChains / boardSize;

            if (telemetry.RainbowCleared >= 3)
            {
                return new AgentSuggestion(
                    "Rainbow Heavy",
                    $"{telemetry.RainbowCleared} rainbows cleared.",
                    "Rainbow tiles were heavily used for bridging. Board felt easy.",
                    "Rainbow spawn rate may be too high.",
                    "Lower rainbow probability or increase tile types.",
                    System.Math.Max(8, telemetry.MovesUsed - 2),
                    telemetry.Score + 200,
                    System.Math.Min(6, telemetry.TileTypes + 1));
            }

            if (telemetry.FeverActive)
            {
                return new AgentSuggestion(
                    "Fever",
                    $"Combo {telemetry.Combo} / Fever active. {telemetry.TotalDamageDealt} dmg dealt.",
                    "Fever is running; the board cascade is self-clearing.",
                    "Fever may end before target score is met.",
                    "Keep chain momentum; consider lowering target if Fever doesn't resolve.",
                    telemetry.MovesUsed + 15,
                    telemetry.Score + 800,
                    telemetry.TileTypes);
            }

            if (telemetry.EnemyHp <= 0)
            {
                return new AgentSuggestion(
                    "Boss Down",
                    $"Enemy defeated. {telemetry.TotalDamageDealt} total damage.",
                    "Enemy is gone; player can focus on score target.",
                    "Target score may still be too high without enemy bonus.",
                    "Consider slightly lower target or faster spawns next level.",
                    telemetry.MovesUsed + 12,
                    telemetry.Score + 500,
                    telemetry.TileTypes);
            }

            if (telemetry.Combo >= 5)
            {
                return new AgentSuggestion(
                    "Combo Warm",
                    $"Combo {telemetry.Combo}. Fever threshold at 7.",
                    "Player is chaining well; board readability is good.",
                    "One failed chain could reset momentum.",
                    "Add a small move buffer to reward combo play.",
                    telemetry.MovesUsed + 10,
                    telemetry.Score + 400,
                    telemetry.TileTypes);
            }

            if (telemetry.LongestChain >= 8 || chainDensity > 0.42f)
            {
                return new AgentSuggestion(
                    "Easy",
                    $"{telemetry.PossibleChains} starts / longest {telemetry.LongestChain}.",
                    "Readable opener; add pressure next.",
                    "Too many obvious links.",
                    "Raise target or add 1 tile type.",
                    18,
                    2600,
                    telemetry.TileTypes + 1);
            }

            if (telemetry.LongestChain <= 3 || chainDensity < 0.18f)
            {
                return new AgentSuggestion(
                    "Hard",
                    $"{telemetry.PossibleChains} starts / longest {telemetry.LongestChain}.",
                    "Avoid early dead-board frustration.",
                    "3-chain is hard to read.",
                    "Lower types or add moves.",
                    24,
                    1400,
                    System.Math.Max(3, telemetry.TileTypes - 1));
            }

            return new AgentSuggestion(
                "Normal",
                $"{telemetry.PossibleChains} starts / longest {telemetry.LongestChain}.",
                "Baseline first-level feel.",
                "Clear feedback is still quiet.",
                "Tune clear juice and target score.",
                20,
                2200,
                telemetry.TileTypes);
        }
    }
}
