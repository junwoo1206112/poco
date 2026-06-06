using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using PokoPuzzle.Core;
using UnityEngine;

namespace Tests
{
    public sealed class LineLinkerBoardInputTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        private readonly List<GameObject> createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            for (var index = createdObjects.Count - 1; index >= 0; index--)
            {
                if (createdObjects[index] != null)
                {
                    Object.DestroyImmediate(createdObjects[index]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void BombAtPointer_FindsBombNearVisibleTileEdge()
        {
            var board = CreateBoard(out var camera);
            var bomb = CreateBomb(Vector3.zero);
            SetField(board, "boardCamera", camera);
            GetBombTiles(board).Add(bomb);

            var screenPoint = camera.WorldToScreenPoint(new Vector3(0.5f, 0f, 0f));
            var result = InvokeBombAtPointer(board, screenPoint);

            Assert.AreSame(bomb, result);
        }

        [Test]
        public void BombAtPointer_IgnoresTapOutsideSpecialTileRadius()
        {
            var board = CreateBoard(out var camera);
            var bomb = CreateBomb(Vector3.zero);
            SetField(board, "boardCamera", camera);
            GetBombTiles(board).Add(bomb);

            var screenPoint = camera.WorldToScreenPoint(new Vector3(0.7f, 0f, 0f));
            var result = InvokeBombAtPointer(board, screenPoint);

            Assert.IsNull(result);
        }

        [Test]
        public void TileAtPointer_FindsRegularTileNearVisibleTileEdge()
        {
            var board = CreateBoard(out var camera);
            var tile = CreateTile(Vector3.zero);
            SetField(board, "boardCamera", camera);
            SetField(board, "width", 1);
            SetField(board, "height", 1);
            SetField(board, "tiles", new[,] { { tile } });

            var screenPoint = camera.WorldToScreenPoint(new Vector3(0.48f, 0f, 0f));
            var result = InvokeTileAtPointer(board, screenPoint);

            Assert.AreSame(tile, result);
        }

        [Test]
        public void SkillTargetsCurrentDrag_ReturnsTrueForSelectedTile()
        {
            var board = CreateBoard(out _);
            var tile = CreateTile(Vector3.zero);
            SetField(board, "dragging", true);
            GetSelectedTiles(board).Add(tile);

            var result = InvokeSkillTargetsCurrentDrag(board, new List<PokoTile> { tile });

            Assert.IsTrue(result);
        }

        [Test]
        public void CancelDragChangedBySkill_ClearsCurrentSelection()
        {
            var board = CreateBoard(out _);
            var tile = CreateTile(Vector3.zero);
            SetField(board, "dragging", true);
            SetField(board, "currentPointerTile", tile);
            GetSelectedTiles(board).Add(tile);

            InvokeCancelDragChangedBySkill(board, true);

            Assert.IsFalse((bool)GetField(board, "dragging"));
            Assert.IsNull(GetField(board, "currentPointerTile"));
            Assert.AreEqual(0, GetSelectedTiles(board).Count);
        }

        [Test]
        public void ClearMatchedTiles_ThawsAdjacentFrozenTileAndScores()
        {
            var board = CreateBoard(out _);
            var matchedTile = CreateTile(new Vector3(0f, 1f, 0f));
            var frozenTile = CreateTile(Vector3.zero);
            frozenTile.ConfigureSubtype(PokoBlockSubtype.Frozen);
            SetField(board, "width", 2);
            SetField(board, "height", 2);
            SetField(board, "tiles", new[,] { { frozenTile, matchedTile }, { null, null } });
            GetSelectedTiles(board).Add(matchedTile);

            InvokeClearMatchedTiles(board);

            var tiles = (PokoTile[,])GetField(board, "tiles");
            Assert.AreSame(frozenTile, tiles[0, 0]);
            Assert.IsFalse(frozenTile.IsFrozen);
            Assert.IsTrue(frozenTile.IsLinkable);
            Assert.AreEqual(20, GetField(board, "score"));
        }

        [Test]
        public void CollapseAndRefill_MovesBombsAndLeavesNoEmptyCells()
        {
            var board = CreateBoard(out _);
            var lowerTile = CreateTile(new Vector3(0f, 1f, 0f));
            var bomb = CreateBomb(new Vector3(0f, 2f, 0f));
            var upperTile = CreateTile(new Vector3(0f, 3f, 0f));
            SetField(board, "width", 1);
            SetField(board, "height", 4);
            SetField(board, "useHexGrid", false);
            SetField(board, "spacing", 1f);
            SetField(board, "tileTypes", 1);
            SetField(board, "tiles", new[,] { { null, lowerTile, bomb, upperTile } });
            GetBombTiles(board).Add(bomb);

            InvokeCollapseAndRefill(board);

            var tiles = (PokoTile[,])GetField(board, "tiles");
            for (var row = 0; row < 4; row++)
            {
                Assert.IsNotNull(tiles[0, row], $"Row {row} should be filled");
            }

            Assert.AreEqual(1, bomb.Row);
        }

        [Test]
        public void ResolveBombClear_ClearsStoneButSkipsPetrified()
        {
            var board = CreateBoard(out _);
            var stone = CreateTile(new Vector3(1f, 0f, 0f));
            var petrified = CreateTile(new Vector3(2f, 0f, 0f));
            stone.SetGridPosition(1, 0, stone.transform.position);
            petrified.SetGridPosition(2, 0, petrified.transform.position);
            stone.ConfigureSubtype(PokoBlockSubtype.Stone, 2);
            petrified.ConfigureSubtype(PokoBlockSubtype.Petrified);
            SetField(board, "width", 3);
            SetField(board, "height", 1);
            SetField(board, "useHexGrid", false);
            SetField(board, "tiles", new[,] { { null }, { stone }, { petrified } });

            var routine = InvokeResolveBombClear(board, 0, 0, BombType.Red, Vector3.zero);
            Assert.IsTrue(routine.MoveNext());
            while (routine.MoveNext())
            {
            }

            var tiles = (PokoTile[,])GetField(board, "tiles");
            Assert.IsNull(tiles[1, 0]);
            Assert.AreSame(petrified, tiles[2, 0]);
        }

        [Test]
        public void CollapseAndRefill_ClearsPetrifiedAfterItFallsToBottom()
        {
            var board = CreateBoard(out _);
            var petrified = CreateTile(new Vector3(0f, 0f, 0f));
            petrified.ConfigureSubtype(PokoBlockSubtype.Petrified);
            SetField(board, "width", 1);
            SetField(board, "height", 3);
            SetField(board, "useHexGrid", false);
            SetField(board, "spacing", 1f);
            SetField(board, "tileTypes", 1);
            SetField(board, "tiles", new[,] { { null, petrified, null } });

            InvokeCollapseAndRefill(board);

            var tiles = (PokoTile[,])GetField(board, "tiles");
            for (var row = 0; row < 3; row++)
            {
                Assert.AreNotSame(petrified, tiles[0, row]);
                Assert.IsNotNull(tiles[0, row]);
            }

            Assert.AreEqual(40, GetField(board, "score"));
        }

        [Test]
        public void CollapseAndRefill_ClearsBottomPetrifiedEvenWhenTilesAreAboveIt()
        {
            var board = CreateBoard(out _);
            var petrified = CreateTile(new Vector3(0f, 0f, 0f));
            var upperTile = CreateTile(new Vector3(0f, 1f, 0f));
            petrified.ConfigureSubtype(PokoBlockSubtype.Petrified);
            SetField(board, "width", 1);
            SetField(board, "height", 3);
            SetField(board, "useHexGrid", false);
            SetField(board, "spacing", 1f);
            SetField(board, "tileTypes", 1);
            SetField(board, "tiles", new[,] { { petrified, upperTile, null } });

            InvokeCollapseAndRefill(board);

            var tiles = (PokoTile[,])GetField(board, "tiles");
            for (var row = 0; row < 3; row++)
            {
                Assert.AreNotSame(petrified, tiles[0, row]);
                Assert.IsNotNull(tiles[0, row]);
            }

            Assert.AreEqual(40, GetField(board, "score"));
        }

        [Test]
        public void CollapseAndRefill_FillsHexBoardAfterBottomPetrifiedClears()
        {
            var board = CreateBoard(out _);
            var petrified = CreateTile(new Vector3(0f, 0f, 0f));
            petrified.ConfigureSubtype(PokoBlockSubtype.Petrified);
            SetField(board, "width", 4);
            SetField(board, "height", 9);
            SetField(board, "useHexGrid", true);
            SetField(board, "spacing", 1f);
            SetField(board, "tileTypes", 1);

            var tiles = new PokoTile[4, 9];
            for (var column = 0; column < 4; column++)
            {
                for (var row = 0; row < 9; row++)
                {
                    if (!IsInsideHexTestBoard(column, row))
                    {
                        continue;
                    }

                    var tile = column == 3 && row == 1
                        ? petrified
                        : CreateTile(new Vector3(column, row, 0f));
                    tile.SetGridPosition(column, row, tile.transform.position);
                    tiles[column, row] = tile;
                }
            }

            SetField(board, "tiles", tiles);

            InvokeCollapseAndRefill(board);

            tiles = (PokoTile[,])GetField(board, "tiles");
            for (var column = 0; column < 4; column++)
            {
                for (var row = 0; row < 9; row++)
                {
                    if (IsInsideHexTestBoard(column, row))
                    {
                        Assert.IsNotNull(tiles[column, row], $"Cell {column},{row} should be filled");
                        Assert.AreNotSame(petrified, tiles[column, row]);
                    }
                }
            }

            Assert.AreEqual(40, GetField(board, "score"));
        }

        [Test]
        public void ClearMatchedTiles_SkipsAdjacentStoneAndPetrified()
        {
            var board = CreateBoard(out _);
            var matchedTile = CreateTile(new Vector3(0f, 0f, 0f));
            var stone = CreateTile(new Vector3(1f, 0f, 0f));
            var petrified = CreateTile(new Vector3(2f, 0f, 0f));
            stone.ConfigureSubtype(PokoBlockSubtype.Stone, 2);
            petrified.ConfigureSubtype(PokoBlockSubtype.Petrified);
            SetField(board, "width", 3);
            SetField(board, "height", 1);
            SetField(board, "useHexGrid", false);
            SetField(board, "tiles", new[,] { { matchedTile }, { stone }, { petrified } });
            GetSelectedTiles(board).Add(matchedTile);

            InvokeClearMatchedTiles(board);

            var tiles = (PokoTile[,])GetField(board, "tiles");
            Assert.AreSame(stone, tiles[1, 0]);
            Assert.AreEqual(2, stone.BlockHitPoints);
            Assert.AreSame(petrified, tiles[2, 0]);
        }

        [Test]
        public void ResolveRainbowClear_ClearsMatchingStone()
        {
            var board = CreateBoard(out _);
            var redTile = CreateTile(new Vector3(0f, 0f, 0f));
            var stone = CreateTile(new Vector3(1f, 0f, 0f));
            stone.ConfigureSubtype(PokoBlockSubtype.Stone, 2);
            SetField(board, "width", 2);
            SetField(board, "height", 1);
            SetField(board, "useHexGrid", false);
            SetField(board, "spacing", 1f);
            SetField(board, "tileTypes", 1);
            SetField(board, "tiles", new[,] { { redTile }, { stone } });

            var routine = InvokeResolveRainbowClear(board, PokoTileType.Red, Vector3.zero);
            Assert.IsTrue(routine.MoveNext());
            while (routine.MoveNext())
            {
            }

            var tiles = (PokoTile[,])GetField(board, "tiles");
            Assert.AreNotSame(stone, tiles[0, 0]);
            Assert.AreNotSame(stone, tiles[1, 0]);
            Assert.AreEqual(100, GetField(board, "score"));
        }

        [Test]
        public void VerifyBoardIntegrity_RepairsEmptyPlayableCells()
        {
            var board = CreateBoard(out _);
            var tile = CreateTile(new Vector3(0f, 0f, 0f));
            SetField(board, "width", 2);
            SetField(board, "height", 2);
            SetField(board, "useHexGrid", false);
            SetField(board, "spacing", 1f);
            SetField(board, "tileTypes", 1);
            SetField(board, "tiles", new[,] { { tile, null }, { null, null } });

            InvokeVerifyBoardIntegrity(board);

            var tiles = (PokoTile[,])GetField(board, "tiles");
            for (var column = 0; column < 2; column++)
            {
                for (var row = 0; row < 2; row++)
                {
                    Assert.IsNotNull(tiles[column, row], $"Cell {column},{row} should be repaired");
                }
            }
        }

        [Test]
        public void VerifyBoardIntegrity_ReplacesClearingPlayableCells()
        {
            var board = CreateBoard(out _);
            var clearingTile = CreateTile(new Vector3(0f, 0f, 0f));
            clearingTile.PlayClearAndDestroy();
            SetField(board, "width", 1);
            SetField(board, "height", 1);
            SetField(board, "useHexGrid", false);
            SetField(board, "spacing", 1f);
            SetField(board, "tileTypes", 1);
            SetField(board, "tiles", new[,] { { clearingTile } });

            InvokeVerifyBoardIntegrity(board);

            var tiles = (PokoTile[,])GetField(board, "tiles");
            Assert.IsNotNull(tiles[0, 0]);
            Assert.AreNotSame(clearingTile, tiles[0, 0]);
            Assert.IsFalse(tiles[0, 0].IsClearing);
        }

        [Test]
        public void VerifyBoardIntegrity_SnapsVisuallyMisplacedTile()
        {
            var board = CreateBoard(out _);
            var tile = CreateTile(new Vector3(0f, 5f, 0f));
            tile.SetGridPosition(0, 0, new Vector3(0f, 5f, 0f));
            SetField(board, "width", 1);
            SetField(board, "height", 1);
            SetField(board, "useHexGrid", false);
            SetField(board, "spacing", 1f);
            SetField(board, "tileTypes", 1);
            SetField(board, "tiles", new[,] { { tile } });

            InvokeVerifyBoardIntegrity(board);

            Assert.AreEqual(Vector3.zero, tile.transform.position);
        }

        [Test]
        public void SetGridPosition_CancelsPendingDropAnimation()
        {
            var tile = CreateTile(Vector3.zero);
            tile.AnimateDrop(Vector3.zero, 3f, 0f);

            tile.SetGridPosition(0, 0, Vector3.zero);

            Assert.AreEqual(Vector3.zero, tile.transform.position);
        }

        [Test]
        public void CollapseAndRefill_FillsGapsAroundFixedSpecialBlockers()
        {
            var board = CreateBoard(out _);
            var lowerTile = CreateTile(new Vector3(0f, 1f, 0f));
            var stone = CreateTile(new Vector3(0f, 2f, 0f));
            var upperTile = CreateTile(new Vector3(0f, 4f, 0f));
            stone.ConfigureSubtype(PokoBlockSubtype.Stone, 2);
            SetField(board, "width", 1);
            SetField(board, "height", 5);
            SetField(board, "useHexGrid", false);
            SetField(board, "spacing", 1f);
            SetField(board, "tileTypes", 1);
            SetField(board, "tiles", new[,] { { null, lowerTile, stone, null, upperTile } });

            InvokeCollapseAndRefill(board);

            var tiles = (PokoTile[,])GetField(board, "tiles");
            for (var row = 0; row < 5; row++)
            {
                Assert.IsNotNull(tiles[0, row], $"Row {row} should be filled");
            }

            Assert.AreSame(stone, tiles[0, 2]);
        }

        [Test]
        public void CollapseAndRefill_RepairsHexBoardWithMixedSpecialGaps()
        {
            var board = CreateBoard(out _);
            SetField(board, "width", 4);
            SetField(board, "height", 9);
            SetField(board, "useHexGrid", true);
            SetField(board, "spacing", 1f);
            SetField(board, "tileTypes", 1);

            var tiles = new PokoTile[4, 9];
            for (var column = 0; column < 4; column++)
            {
                for (var row = 0; row < 9; row++)
                {
                    if (!IsInsideHexTestBoard(column, row))
                    {
                        continue;
                    }

                    if ((column == 0 && row == 1) || (column == 2 && row == 5) || (column == 3 && row == 7))
                    {
                        continue;
                    }

                    var tile = CreateTile(new Vector3(column, row, 0f));
                    tile.SetGridPosition(column, row, tile.transform.position);
                    if (column == 1 && row == 3)
                    {
                        tile.ConfigureSubtype(PokoBlockSubtype.Frozen);
                    }
                    else if (column == 2 && row == 4)
                    {
                        tile.ConfigureSubtype(PokoBlockSubtype.Stone, 2);
                    }
                    else if (column == 3 && row == 1)
                    {
                        tile.ConfigureSubtype(PokoBlockSubtype.Petrified);
                    }

                    tiles[column, row] = tile;
                }
            }

            SetField(board, "tiles", tiles);

            InvokeCollapseAndRefill(board);

            tiles = (PokoTile[,])GetField(board, "tiles");
            for (var column = 0; column < 4; column++)
            {
                for (var row = 0; row < 9; row++)
                {
                    if (IsInsideHexTestBoard(column, row))
                    {
                        Assert.IsNotNull(tiles[column, row], $"Cell {column},{row} should be filled");
                        Assert.IsFalse(tiles[column, row].IsClearing, $"Cell {column},{row} should not contain a clearing tile");
                    }
                }
            }
        }

        [Test]
        public void ClearFrozenTilesAdjacentTo_OnlyUsesActualClearedCells()
        {
            var board = CreateBoard(out _);
            var unrelatedFrozen = CreateTile(new Vector3(0f, 1f, 0f));
            var adjacentFrozen = CreateTile(new Vector3(1f, 0f, 0f));
            unrelatedFrozen.ConfigureSubtype(PokoBlockSubtype.Frozen);
            adjacentFrozen.ConfigureSubtype(PokoBlockSubtype.Frozen);
            SetField(board, "width", 3);
            SetField(board, "height", 2);
            SetField(board, "useHexGrid", false);
            SetField(board, "spacing", 1f);
            SetField(board, "tileTypes", 1);
            SetField(board, "tiles", new[,]
            {
                { null, null },
                { adjacentFrozen, null },
                { null, unrelatedFrozen }
            });

            InvokeClearFrozenTilesAdjacentTo(board, new List<Vector2Int> { new(0, 0) });

            Assert.IsTrue(unrelatedFrozen.IsFrozen);
            Assert.IsFalse(adjacentFrozen.IsFrozen);
        }

        private LineLinkerBoard CreateBoard(out Camera camera)
        {
            var cameraObject = CreateObject("Input Test Camera");
            camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.pixelRect = new Rect(0f, 0f, 720f, 720f);

            var boardObject = CreateObject("Input Test Board");
            boardObject.SetActive(false);
            return boardObject.AddComponent<LineLinkerBoard>();
        }

        private PokoTile CreateBomb(Vector3 position)
        {
            var tileObject = CreateObject("Input Test Bomb");
            tileObject.transform.position = position;
            var tile = tileObject.AddComponent<PokoTile>();
            tile.Initialize(0, 0, PokoTileType.Red, null);
            tile.ConfigureBomb(BombType.Red);
            return tile;
        }

        private PokoTile CreateTile(Vector3 position)
        {
            var tileObject = CreateObject("Input Test Tile");
            tileObject.transform.position = position;
            var tile = tileObject.AddComponent<PokoTile>();
            tile.Initialize(0, 0, PokoTileType.Red, null);
            return tile;
        }

        private GameObject CreateObject(string name)
        {
            var gameObject = new GameObject(name);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private static List<PokoTile> GetBombTiles(LineLinkerBoard board)
        {
            return (List<PokoTile>)typeof(LineLinkerBoard)
                .GetField("bombTiles", PrivateInstance)
                .GetValue(board);
        }

        private static List<PokoTile> GetSelectedTiles(LineLinkerBoard board)
        {
            return (List<PokoTile>)typeof(LineLinkerBoard)
                .GetField("selectedTiles", PrivateInstance)
                .GetValue(board);
        }

        private static PokoTile InvokeBombAtPointer(LineLinkerBoard board, Vector2 screenPoint)
        {
            return (PokoTile)typeof(LineLinkerBoard)
                .GetMethod("BombAtPointer", PrivateInstance)
                .Invoke(board, new object[] { screenPoint });
        }

        private static PokoTile InvokeTileAtPointer(LineLinkerBoard board, Vector2 screenPoint)
        {
            return (PokoTile)typeof(LineLinkerBoard)
                .GetMethod("TileAtPointer", PrivateInstance)
                .Invoke(board, new object[] { screenPoint });
        }

        private static bool InvokeSkillTargetsCurrentDrag(LineLinkerBoard board, List<PokoTile> targets)
        {
            return (bool)typeof(LineLinkerBoard)
                .GetMethod("SkillTargetsCurrentDrag", PrivateInstance)
                .Invoke(board, new object[] { targets });
        }

        private static void InvokeCancelDragChangedBySkill(LineLinkerBoard board, bool shouldCancel)
        {
            typeof(LineLinkerBoard)
                .GetMethod("CancelDragChangedBySkill", PrivateInstance)
                .Invoke(board, new object[] { shouldCancel });
        }

        private static int InvokeClearMatchedTiles(LineLinkerBoard board)
        {
            return (int)typeof(LineLinkerBoard)
                .GetMethod("ClearMatchedTiles", PrivateInstance)
                .Invoke(board, new object[0]);
        }

        private static void InvokeCollapseAndRefill(LineLinkerBoard board)
        {
            typeof(LineLinkerBoard)
                .GetMethod("CollapseAndRefill", PrivateInstance)
                .Invoke(board, new object[0]);
        }

        private static IEnumerator InvokeResolveBombClear(LineLinkerBoard board, int column, int row, BombType bombType, Vector3 bombPosition)
        {
            return (IEnumerator)typeof(LineLinkerBoard)
                .GetMethod("ResolveBombClear", PrivateInstance)
                .Invoke(board, new object[] { column, row, bombType, bombPosition });
        }

        private static IEnumerator InvokeResolveRainbowClear(LineLinkerBoard board, PokoTileType targetType, Vector3 bombPosition)
        {
            return (IEnumerator)typeof(LineLinkerBoard)
                .GetMethod("ResolveRainbowClear", PrivateInstance)
                .Invoke(board, new object[] { targetType, bombPosition });
        }

        private static void InvokeVerifyBoardIntegrity(LineLinkerBoard board)
        {
            typeof(LineLinkerBoard)
                .GetMethod("VerifyBoardIntegrity", PrivateInstance)
                .Invoke(board, new object[0]);
        }

        private static void InvokeClearFrozenTilesAdjacentTo(LineLinkerBoard board, IReadOnlyList<Vector2Int> clearedCells)
        {
            typeof(LineLinkerBoard)
                .GetMethod("ClearFrozenTilesAdjacentTo", PrivateInstance)
                .Invoke(board, new object[] { clearedCells });
        }

        private static object GetField(object target, string fieldName)
        {
            return target.GetType().GetField(fieldName, PrivateInstance).GetValue(target);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            target.GetType().GetField(fieldName, PrivateInstance).SetValue(target, value);
        }

        private static bool IsInsideHexTestBoard(int column, int row)
        {
            return row >= 0 && row < 9 && column >= 0 && column < HexGridUtility.RowSize(row, 4);
        }
    }
}
