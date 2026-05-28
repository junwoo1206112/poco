namespace PokoPuzzle.Core.Data
{
    [System.Serializable]
    public sealed class BalanceProfileData
    {
        public string ProfileId;
        public string DisplayName;
        public int MinAvailableChains;
        public float TargetAverageChainLength;
        public float RainbowGaugeMultiplier;
        public float RefillAssistRate;
        public int BlockerBudget;
        public float RegularEnemyHpMultiplier;
        public float BossHpMultiplier;
        public float SkillCooldownMultiplier;
    }
}
