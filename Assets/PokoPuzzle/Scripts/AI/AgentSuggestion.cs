namespace PokoPuzzle.AI
{
    public readonly struct AgentSuggestion
    {
        public AgentSuggestion(
            string difficultyLabel,
            string summary,
            string designIntent,
            string risk,
            string recommendedAction,
            int suggestedMoveLimit,
            int suggestedTargetScore,
            int suggestedTileTypes,
            int suggestedRegularEnemyHp = 0,
            int suggestedBossHp = 0)
        {
            DifficultyLabel = difficultyLabel;
            Summary = summary;
            DesignIntent = designIntent;
            Risk = risk;
            RecommendedAction = recommendedAction;
            SuggestedMoveLimit = suggestedMoveLimit;
            SuggestedTargetScore = suggestedTargetScore;
            SuggestedTileTypes = suggestedTileTypes;
            SuggestedRegularEnemyHp = suggestedRegularEnemyHp;
            SuggestedBossHp = suggestedBossHp;
        }

        public string DifficultyLabel { get; }
        public string Summary { get; }
        public string DesignIntent { get; }
        public string Risk { get; }
        public string RecommendedAction { get; }
        public int SuggestedMoveLimit { get; }
        public int SuggestedTargetScore { get; }
        public int SuggestedTileTypes { get; }
        public int SuggestedRegularEnemyHp { get; }
        public int SuggestedBossHp { get; }
    }
}
