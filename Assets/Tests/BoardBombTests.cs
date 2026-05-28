using System.Linq;
using NUnit.Framework;
using PokoPuzzle.Core;
using UnityEngine;

namespace Tests
{
    public sealed class BoardBombTests
    {
        [Test]
        public void RedBomb_IncludesOrigin()
        {
            var positions = BoardBomb.GetAffectedPositions(1, 6, 13, BombType.Red).ToList();
            Assert.Contains(new Vector2Int(1, 6), positions);
        }

        [Test]
        public void RedBomb_ShootsIn6Directions()
        {
            var positions = BoardBomb.GetAffectedPositions(1, 6, 13, BombType.Red).ToList();
            Assert.Greater(positions.Count, 1);
        }

        [Test]
        public void BlueBomb_IncludesOrigin()
        {
            var positions = BoardBomb.GetAffectedPositions(1, 6, 13, BombType.Blue).ToList();
            Assert.Contains(new Vector2Int(1, 6), positions);
        }

        [Test]
        public void BlueBomb_RadiusArea()
        {
            var positions = BoardBomb.GetAffectedPositions(1, 6, 13, BombType.Blue).ToList();
            Assert.Greater(positions.Count, 1);
        }

        [Test]
        public void BlueBomb_LargerThanRedBomb()
        {
            var red = BoardBomb.GetAffectedPositions(1, 6, 13, BombType.Red).Count();
            var blue = BoardBomb.GetAffectedPositions(1, 6, 13, BombType.Blue).Count();
            Assert.Greater(blue, red);
        }

        [Test]
        public void RedBomb_DoesNotExceedBoardBounds()
        {
            var positions = BoardBomb.GetAffectedPositions(0, 0, 13, BombType.Red).ToList();
            foreach (var pos in positions)
            {
                Assert.IsTrue(pos.y >= 0 && pos.y < 13, $"Position ({pos.x},{pos.y}) y out of range");
                Assert.IsTrue(pos.x >= 0, $"Position ({pos.x},{pos.y}) x negative");
                var maxCol = (pos.y & 1) == 0 ? 3 : 4;
                Assert.IsTrue(pos.x < maxCol, $"Position ({pos.x},{pos.y}) x >= maxCol={maxCol}");
            }
        }

        [Test]
        public void BlueBomb_DoesNotExceedBoardBounds()
        {
            var positions = BoardBomb.GetAffectedPositions(0, 0, 13, BombType.Blue).ToList();
            foreach (var pos in positions)
            {
                Assert.IsTrue(pos.y >= 0 && pos.y < 13, $"Position ({pos.x},{pos.y}) y out of range");
                Assert.IsTrue(pos.x >= 0, $"Position ({pos.x},{pos.y}) x negative");
                var maxCol = (pos.y & 1) == 0 ? 3 : 4;
                Assert.IsTrue(pos.x < maxCol, $"Position ({pos.x},{pos.y}) x >= maxCol={maxCol}");
            }
        }

        [Test]
        public void RedBomb_CenterBoard_AffectsPositionsInAllDirections()
        {
            var positions = BoardBomb.GetAffectedPositions(1, 6, 13, BombType.Red).ToList();
            Assert.GreaterOrEqual(positions.Count, 7);
        }

        [Test]
        public void RainbowBomb_UsesBoardDetonationLogic()
        {
            var positions = BoardBomb.GetAffectedPositions(1, 6, 13, BombType.Rainbow).ToList();
            Assert.AreEqual(0, positions.Count);
        }
    }
}
