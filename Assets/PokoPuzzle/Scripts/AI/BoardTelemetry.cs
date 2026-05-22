namespace PokoPuzzle.AI
{
    public readonly struct BoardTelemetry
    {
        public BoardTelemetry(int width, int height, int tileTypes, int possibleChains, int longestChain, int score, int movesUsed)
        {
            Width = width;
            Height = height;
            TileTypes = tileTypes;
            PossibleChains = possibleChains;
            LongestChain = longestChain;
            Score = score;
            MovesUsed = movesUsed;
        }

        public int Width { get; }
        public int Height { get; }
        public int TileTypes { get; }
        public int PossibleChains { get; }
        public int LongestChain { get; }
        public int Score { get; }
        public int MovesUsed { get; }
    }
}
