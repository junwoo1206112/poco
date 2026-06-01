using NUnit.Framework;
using PokoPuzzle.AI;

namespace Tests
{
    public sealed class HeuristicGameDesignerAgentTests
    {
        private HeuristicGameDesignerAgent agent;

        [SetUp]
        public void SetUp()
        {
            agent = new HeuristicGameDesignerAgent();
        }

        [Test]
        public void Analyze_RainbowCleared3OrMore_ReturnsRainbowHeavy()
        {
            var result = agent.Analyze(new BoardTelemetry(4, 13, 5, 10, 5, 2000, 15, rainbowCleared: 3));
            Assert.AreEqual("Rainbow Heavy", result.DifficultyLabel);
        }

        [Test]
        public void Analyze_RainbowCleared5_ReturnsRainbowHeavy()
        {
            var result = agent.Analyze(new BoardTelemetry(4, 13, 5, 10, 5, 2000, 15, rainbowCleared: 5));
            Assert.AreEqual("Rainbow Heavy", result.DifficultyLabel);
        }

        [Test]
        public void Analyze_FeverActive_ReturnsFever()
        {
            var result = agent.Analyze(new BoardTelemetry(4, 13, 5, 10, 5, 2000, 15, feverActive: true));
            Assert.AreEqual("Fever", result.DifficultyLabel);
        }

        [Test]
        public void Analyze_EnemyDefeated_ReturnsBossDown()
        {
            var result = agent.Analyze(new BoardTelemetry(4, 13, 5, 10, 5, 2000, 15, enemyHp: 0, totalDamageDealt: 500));
            Assert.AreEqual("Boss Down", result.DifficultyLabel);
        }

        [Test]
        public void Analyze_HighCombo_ReturnsComboWarm()
        {
            var result = agent.Analyze(new BoardTelemetry(4, 13, 5, 10, 5, 2000, 15, combo: 5));
            Assert.AreEqual("Combo Warm", result.DifficultyLabel);
        }

        [Test]
        public void Analyze_LongChainDensityHigh_ReturnsEasy()
        {
            var telemetry = new BoardTelemetry(4, 13, 5, 26, 8, 3000, 10);
            var result = agent.Analyze(telemetry);
            Assert.AreEqual("Easy", result.DifficultyLabel);
        }

        [Test]
        public void Analyze_ShortChainLowDensity_ReturnsHard()
        {
            var telemetry = new BoardTelemetry(4, 13, 5, 3, 3, 500, 20);
            var result = agent.Analyze(telemetry);
            Assert.AreEqual("Hard", result.DifficultyLabel);
        }

        [Test]
        public void Analyze_BalancedTelemetry_ReturnsNormal()
        {
            var telemetry = new BoardTelemetry(4, 13, 5, 10, 5, 1500, 15);
            var result = agent.Analyze(telemetry);
            Assert.AreEqual("Normal", result.DifficultyLabel);
        }

        [Test]
        public void Analyze_RainbowPriority_OverFever()
        {
            var telemetry = new BoardTelemetry(4, 13, 5, 10, 5, 2000, 15, rainbowCleared: 3, feverActive: true);
            var result = agent.Analyze(telemetry);
            Assert.AreEqual("Rainbow Heavy", result.DifficultyLabel);
        }

        [Test]
        public void Analyze_SuggestedValues_RainbowHeavy()
        {
            var result = agent.Analyze(new BoardTelemetry(4, 13, 5, 10, 5, 2000, 15, rainbowCleared: 4));
            Assert.Greater(result.SuggestedTargetScore, 0);
            Assert.Greater(result.SuggestedMoveLimit, 0);
            Assert.GreaterOrEqual(result.SuggestedTileTypes, 3);
        }

        [Test]
        public void Analyze_SuggestedValues_Normal()
        {
            var result = agent.Analyze(new BoardTelemetry(4, 13, 5, 10, 5, 1500, 15));
            Assert.Greater(result.SuggestedTargetScore, 0);
            Assert.Greater(result.SuggestedMoveLimit, 0);
        }

        [Test]
        public void Analyze_SuggestedValues_Hard()
        {
            var result = agent.Analyze(new BoardTelemetry(4, 13, 5, 3, 3, 500, 20));
            Assert.LessOrEqual(result.SuggestedTileTypes, 5);
            Assert.GreaterOrEqual(result.SuggestedTileTypes, 3);
        }
    }
}
