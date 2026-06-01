using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PokoPuzzle.Core;
using UnityEngine;

namespace Tests
{
    public sealed class HexGridUtilityTests
    {
        [Test]
        public void RowSize_EvenRow_Returns3()
        {
            Assert.AreEqual(3, HexGridUtility.RowSize(0));
            Assert.AreEqual(3, HexGridUtility.RowSize(2));
            Assert.AreEqual(3, HexGridUtility.RowSize(4));
        }

        [Test]
        public void RowSize_OddRow_Returns4()
        {
            Assert.AreEqual(4, HexGridUtility.RowSize(1));
            Assert.AreEqual(4, HexGridUtility.RowSize(3));
            Assert.AreEqual(4, HexGridUtility.RowSize(5));
        }

        [Test]
        public void RowSize_ConfiguredWidth_ReturnsFullWidthRows()
        {
            Assert.AreEqual(7, HexGridUtility.RowSize(0, 7));
            Assert.AreEqual(7, HexGridUtility.RowSize(1, 7));
        }

        [Test]
        public void GetNeighbors_ConfiguredWidth_UsesWiderRows()
        {
            var neighbors = HexGridUtility.GetNeighbors(5, 4, 7, 9).ToList();
            Assert.IsTrue(neighbors.All(n => n.x >= 0 && n.x < HexGridUtility.RowSize(n.y, 7)));
            Assert.Greater(neighbors.Count, 0);
        }

        [Test]
        public void GetNeighborCount_Always6()
        {
            Assert.AreEqual(6, HexGridUtility.GetNeighborCount());
        }

        [Test]
        public void AreAdjacent_SamePosition_ReturnsFalse()
        {
            Assert.IsFalse(HexGridUtility.AreAdjacent(0, 0, 0, 0));
            Assert.IsFalse(HexGridUtility.AreAdjacent(1, 2, 1, 2));
        }

        [Test]
        public void AreAdjacent_Orthogonal_ReturnsTrue()
        {
            Assert.IsTrue(HexGridUtility.AreAdjacent(0, 0, 0, 1));
            Assert.IsTrue(HexGridUtility.AreAdjacent(0, 0, 1, 0));
        }

        [Test]
        public void AreAdjacent_Diagonal_ReturnsFalse()
        {
            Assert.IsFalse(HexGridUtility.AreAdjacent(0, 0, 1, 1));
        }

        [Test]
        public void GetNeighbors_CenterOfEvenRow_Returns6Neighbors()
        {
            var neighbors = HexGridUtility.GetNeighbors(1, 2, 13).ToList();
            Assert.AreEqual(6, neighbors.Count);
        }

        [Test]
        public void GetNeighbors_CenterOfOddRow_Returns6Neighbors()
        {
            var neighbors = HexGridUtility.GetNeighbors(1, 1, 13).ToList();
            Assert.AreEqual(6, neighbors.Count);
        }

        [Test]
        public void GetNeighbors_TopEdge_ClampsByHeight()
        {
            var neighbors = HexGridUtility.GetNeighbors(0, 0, 13).ToList();
            Assert.IsTrue(neighbors.All(n => n.y >= 0));
        }

        [Test]
        public void GetNeighbors_BottomEdge_ClampsByHeight()
        {
            var neighbors = HexGridUtility.GetNeighbors(0, 12, 13).ToList();
            Assert.IsTrue(neighbors.All(n => n.y < 13));
        }

        [Test]
        public void ToWorld_SquareGrid_ReturnsCenteredPosition()
        {
            var pos = HexGridUtility.ToWorld(0, 0, 4, 13, 1f, false);
            Assert.IsTrue(pos.x < 0f);
            Assert.IsTrue(pos.y < 0f);
        }

        [Test]
        public void ToWorld_HexGrid_ReturnsPosition()
        {
            var pos = HexGridUtility.ToWorld(0, 0, 4, 13, 1f, true);
            Assert.IsNotNull(pos);
        }

        [Test]
        public void GetNeighbors_AllReturnedPositionsAreInsideBoard()
        {
            const int height = 13;
            for (var column = 0; column < 4; column++)
            {
                for (var row = 0; row < height; row++)
                {
                    var maxCol = (row & 1) == 0 ? 3 : 4;
                    if (column >= maxCol) continue;

                    foreach (var neighbor in HexGridUtility.GetNeighbors(column, row, height))
                    {
                        Assert.IsTrue(neighbor.y >= 0 && neighbor.y < height, $"Neighbor y={neighbor.y} out of range for ({column},{row})");
                        Assert.IsTrue(neighbor.x >= 0, $"Neighbor x={neighbor.x} negative for ({column},{row})");
                        var neighborMaxCol = (neighbor.y & 1) == 0 ? 3 : 4;
                        Assert.IsTrue(neighbor.x < neighborMaxCol, $"Neighbor x={neighbor.x} >= maxCol={neighborMaxCol} for ({column},{row})");
                    }
                }
            }
        }

        [Test]
        public void GetNeighbors_NoDuplicatePositions()
        {
            const int height = 13;
            for (var column = 0; column < 4; column++)
            {
                for (var row = 0; row < height; row++)
                {
                    var maxCol = (row & 1) == 0 ? 3 : 4;
                    if (column >= maxCol) continue;

                    var neighbors = HexGridUtility.GetNeighbors(column, row, height).ToList();
                    Assert.AreEqual(neighbors.Distinct().Count(), neighbors.Count, $"Duplicates found for ({column},{row})");
                }
            }
        }

        [Test]
        public void AreAdjacent_MutualCheck()
        {
            const int height = 13;
            for (var column = 0; column < 4; column++)
            {
                for (var row = 0; row < height; row++)
                {
                    var maxCol = (row & 1) == 0 ? 3 : 4;
                    if (column >= maxCol) continue;

                    foreach (var neighbor in HexGridUtility.GetNeighbors(column, row, height))
                    {
                        Assert.IsTrue(
                            HexGridUtility.AreAdjacent(column, row, neighbor.x, neighbor.y),
                            $"({column},{row}) should be adjacent to ({neighbor.x},{neighbor.y})");
                        Assert.IsTrue(
                            HexGridUtility.AreAdjacent(neighbor.x, neighbor.y, column, row),
                            $"({neighbor.x},{neighbor.y}) should be adjacent to ({column},{row})");
                    }
                }
            }
        }

        [Test]
        public void ToWorld_EvenRowOddRow_XOffsetDiffers()
        {
            var evenPos = HexGridUtility.ToWorld(0, 0, 4, 13, 1f, true);
            var oddPos = HexGridUtility.ToWorld(0, 1, 4, 13, 1f, true);
            Assert.AreNotEqual(evenPos.x, oddPos.x);
        }

        [Test]
        public void ToWorld_SpacingAffectsPosition()
        {
            var small = HexGridUtility.ToWorld(0, 0, 4, 13, 0.5f, true);
            var large = HexGridUtility.ToWorld(0, 0, 4, 13, 1f, true);
            Assert.IsTrue(Mathf.Abs(large.x) > Mathf.Abs(small.x));
            Assert.IsTrue(Mathf.Abs(large.y) > Mathf.Abs(small.y));
        }
    }
}
