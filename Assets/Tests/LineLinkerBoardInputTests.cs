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
        public void ClearMatchedTiles_DamagesAdjacentStoneButSkipsPetrified()
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
            Assert.AreEqual(1, stone.BlockHitPoints);
            Assert.AreSame(petrified, tiles[2, 0]);
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

        private static object GetField(object target, string fieldName)
        {
            return target.GetType().GetField(fieldName, PrivateInstance).GetValue(target);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            target.GetType().GetField(fieldName, PrivateInstance).SetValue(target, value);
        }
    }
}
