namespace PokoPuzzle.AI
{
    public readonly struct BoardTelemetry
    {
        public BoardTelemetry(
            int width, int height, int tileTypes, int possibleChains, int longestChain,
            int score, int movesUsed,
            int combo = 0, bool feverActive = false, int enemyHp = 100,
            int totalDamageDealt = 0, int bombsCleared = 0, int specialBlocksCleared = 0,
            int rainbowCleared = 0)
        {
            Width = width;
            Height = height;
            TileTypes = tileTypes;
            PossibleChains = possibleChains;
            LongestChain = longestChain;
            Score = score;
            MovesUsed = movesUsed;
            Combo = combo;
            FeverActive = feverActive;
            EnemyHp = enemyHp;
            TotalDamageDealt = totalDamageDealt;
            BombsCleared = bombsCleared;
            SpecialBlocksCleared = specialBlocksCleared;
            RainbowCleared = rainbowCleared;
        }

        public int Width { get; }
        public int Height { get; }
        public int TileTypes { get; }
        public int PossibleChains { get; }
        public int LongestChain { get; }
        public int Score { get; }
        public int MovesUsed { get; }
        public int Combo { get; }
        public bool FeverActive { get; }
        public int EnemyHp { get; }
        public int TotalDamageDealt { get; }
        public int BombsCleared { get; }
        public int SpecialBlocksCleared { get; }
        public int RainbowCleared { get; }
    }
}
