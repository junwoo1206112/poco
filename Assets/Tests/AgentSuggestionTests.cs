using NUnit.Framework;
using PokoPuzzle.AI;

namespace Tests
{
    public sealed class AgentSuggestionTests
    {
        [Test]
        public void Constructor_SetsProperties()
        {
            var suggestion = new AgentSuggestion(
                "Easy", "Test summary", "Test intent",
                "Low risk", "Increase difficulty",
                15, 2000, 5);

            Assert.AreEqual("Easy", suggestion.DifficultyLabel);
            Assert.AreEqual("Test summary", suggestion.Summary);
            Assert.AreEqual("Test intent", suggestion.DesignIntent);
            Assert.AreEqual("Low risk", suggestion.Risk);
            Assert.AreEqual("Increase difficulty", suggestion.RecommendedAction);
            Assert.AreEqual(15, suggestion.SuggestedMoveLimit);
            Assert.AreEqual(2000, suggestion.SuggestedTargetScore);
            Assert.AreEqual(5, suggestion.SuggestedTileTypes);
        }
    }

    public sealed class BoardTelemetryTests
    {
        [Test]
        public void Constructor_SetsProperties()
        {
            var telemetry = new BoardTelemetry(
                4, 13, 5, 12, 6,
                1500, 15,
                3, true, 80,
                200, 2, 5, 1);

            Assert.AreEqual(4, telemetry.Width);
            Assert.AreEqual(13, telemetry.Height);
            Assert.AreEqual(5, telemetry.TileTypes);
            Assert.AreEqual(12, telemetry.PossibleChains);
            Assert.AreEqual(6, telemetry.LongestChain);
            Assert.AreEqual(1500, telemetry.Score);
            Assert.AreEqual(15, telemetry.MovesUsed);
            Assert.AreEqual(3, telemetry.Combo);
            Assert.IsTrue(telemetry.FeverActive);
            Assert.AreEqual(80, telemetry.EnemyHp);
            Assert.AreEqual(200, telemetry.TotalDamageDealt);
            Assert.AreEqual(2, telemetry.BombsCleared);
            Assert.AreEqual(5, telemetry.SpecialBlocksCleared);
            Assert.AreEqual(1, telemetry.RainbowCleared);
        }
    }
}
