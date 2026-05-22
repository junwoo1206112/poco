namespace PokoPuzzle.AI
{
    public sealed class HeuristicGameDesignerAgent : IGameDesignerAgent
    {
        public AgentSuggestion Analyze(BoardTelemetry telemetry)
        {
            var boardSize = telemetry.Width * telemetry.Height;
            var chainDensity = boardSize == 0 ? 0f : (float)telemetry.PossibleChains / boardSize;

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
