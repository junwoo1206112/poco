using System.Collections.Generic;
using System.IO;
using System.Text;
using PokoPuzzle.AI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PokoPuzzle.Core
{
    public sealed class LineLinkerBoard : MonoBehaviour
    {
        [Header("Board")]
        [SerializeField] private int width = 4;
        [SerializeField] private int height = 13;
        [SerializeField] private int tileTypes = 5;
        [SerializeField] private float spacing = 0.84f;
        [SerializeField] private bool useHexGrid = true;
        [SerializeField] private int moveLimit = 999;
        [SerializeField] private float roundTime = 60f;
        [SerializeField] private int targetScore = 2200;
        [SerializeField] private PokoLevelConfig levelConfig;
        [SerializeField] private bool enablePlayLog = true;
        [SerializeField] private string playLogPath = "md/playtest-logs/latest-playtest.jsonl";

        [Header("Runtime")]
        [SerializeField] private Camera boardCamera;
        [SerializeField] private LineRenderer linkLine;
        [SerializeField] private TextMesh scoreText;
        [SerializeField] private TextMesh agentText;
        [SerializeField] private TextMesh feedbackText;
        [SerializeField] private bool useScreenHud = true;

        private readonly List<PokoTile> selectedTiles = new();
        private readonly List<PokoTile> hintedTiles = new();
        private PokoTile[,] tiles;
        private Sprite tileSprite;
        private IGameDesignerAgent designerAgent;
        private int score;
        private int movesUsed;
        private float timeRemaining;
        private bool dragging;
        private bool gameEnded;
        private float feedbackClearTime;
        private string levelPlayLogPath;
        private string agentHudText = "Designer analyzing board...";
        private string feedbackMessage = string.Empty;
        private Color feedbackColor = Color.white;

        private void Awake()
        {
            if (boardCamera == null)
            {
                boardCamera = Camera.main;
            }

            designerAgent = new HeuristicGameDesignerAgent();
            ApplyLevelConfig();
            FramePlayCamera();
            PrepareHud();
            tileSprite = CreateHexSprite();
            tiles = new PokoTile[width, height];
            BuildBoard();
            timeRemaining = roundTime;
            StartPlayLog();
            RefreshHud();
            RunDesignerAgent();
        }

        private void Update()
        {
            RefreshTimedFeedback();

            if (gameEnded)
            {
                return;
            }

            if (!gameEnded)
            {
                timeRemaining -= Time.deltaTime;
                if (timeRemaining <= 0f)
                {
                    timeRemaining = 0f;
                    gameEnded = true;
                    EvaluateEndState();
                    RefreshHud();
                    return;
                }
            }

            if (!TryReadPointer(out var screenPosition, out var isPressed, out var wasPressedThisFrame, out var wasReleasedThisFrame))
            {
                return;
            }

            if (wasPressedThisFrame)
            {
                BeginDrag(screenPosition);
            }
            else if (dragging && isPressed)
            {
                ContinueDrag(screenPosition);
            }
            else if (dragging && wasReleasedThisFrame)
            {
                EndDrag();
            }
        }

        private void OnGUI()
        {
            if (!useScreenHud)
            {
                return;
            }

            var scale = Mathf.Clamp(Screen.height / 720f, 0.82f, 1.12f);
            var margin = Mathf.RoundToInt(16f * scale);
            var scoreWidth = Mathf.Min(Screen.width - margin * 2f, 318f * scale);
            var scorePanel = new Rect((Screen.width - scoreWidth) * 0.5f, margin, scoreWidth, 68f * scale);
            var movesLeft = Mathf.Max(0, moveLimit - movesUsed);
            var timeDisplay = Mathf.CeilToInt(timeRemaining);
            var state = score >= targetScore ? "Level Clear" : timeRemaining <= 0f ? "Failed" : "Playing";

            DrawHudPanel(scorePanel);
            GUI.Label(
                new Rect(scorePanel.x + 12f * scale, scorePanel.y + 7f * scale, scorePanel.width - 24f * scale, 28f * scale),
                $"Score {score}/{targetScore}",
                CreateHudStyle(22f, FontStyle.Bold, Color.white, scale, TextAnchor.UpperCenter));
            GUI.Label(
                new Rect(scorePanel.x + 12f * scale, scorePanel.y + 37f * scale, scorePanel.width - 24f * scale, 22f * scale),
                $"Time {timeDisplay}s  |  Moves {movesLeft}  |  {state}",
                CreateHudStyle(15f, FontStyle.Normal, new Color(0.86f, 0.93f, 1f), scale, TextAnchor.UpperCenter));

            if (!string.IsNullOrWhiteSpace(agentHudText))
            {
                var agentWidth = Mathf.Min(Screen.width - margin * 2f, 390f * scale);
                var agentPanel = new Rect(
                    (Screen.width - agentWidth) * 0.5f,
                    Screen.height - margin - 50f * scale,
                    agentWidth,
                    50f * scale);
                DrawHudPanel(agentPanel);
                GUI.Label(
                    new Rect(agentPanel.x + 12f * scale, agentPanel.y + 8f * scale, agentPanel.width - 24f * scale, agentPanel.height - 12f * scale),
                    agentHudText,
                    CreateHudStyle(12f, FontStyle.Normal, new Color(0.82f, 0.92f, 1f), scale, TextAnchor.UpperCenter));
            }

            if (!string.IsNullOrWhiteSpace(feedbackMessage))
            {
                GUI.Label(
                    new Rect(margin, scorePanel.yMax + 8f * scale, Screen.width - margin * 2f, 44f * scale),
                    feedbackMessage,
                    CreateHudStyle(26f, FontStyle.Bold, feedbackColor, scale, TextAnchor.UpperCenter));
            }

            if (gameEnded)
            {
                DrawEndPanel(scale);
            }
        }

        private void BuildBoard()
        {
            for (var column = 0; column < width; column++)
            {
                for (var row = 0; row < height; row++)
                {
                    if (IsInsideBoard(column, row))
                    {
                        tiles[column, row] = CreateTile(column, row, RandomType());
                    }
                }
            }

            EnsurePlayableChain();
        }

        private PokoTile CreateTile(int column, int row, PokoTileType type)
        {
            var tileObject = new GameObject($"Tile_{column}_{row}");
            tileObject.transform.SetParent(transform);
            tileObject.transform.position = GridToWorld(column, row);

            var tile = tileObject.AddComponent<PokoTile>();
            tile.Initialize(column, row, type, tileSprite);
            return tile;
        }

        private bool TryReadPointer(out Vector2 screenPosition, out bool isPressed, out bool wasPressedThisFrame, out bool wasReleasedThisFrame)
        {
            if (Touchscreen.current != null)
            {
                var touch = Touchscreen.current.primaryTouch;
                if (!touch.press.isPressed && !touch.press.wasPressedThisFrame && !touch.press.wasReleasedThisFrame)
                {
                    return ReadMousePointer(out screenPosition, out isPressed, out wasPressedThisFrame, out wasReleasedThisFrame);
                }

                screenPosition = touch.position.ReadValue();
                isPressed = touch.press.isPressed;
                wasPressedThisFrame = touch.press.wasPressedThisFrame;
                wasReleasedThisFrame = touch.press.wasReleasedThisFrame;
                return true;
            }

            return ReadMousePointer(out screenPosition, out isPressed, out wasPressedThisFrame, out wasReleasedThisFrame);
        }

        private static bool ReadMousePointer(out Vector2 screenPosition, out bool isPressed, out bool wasPressedThisFrame, out bool wasReleasedThisFrame)
        {
            if (Mouse.current != null)
            {
                screenPosition = Mouse.current.position.ReadValue();
                isPressed = Mouse.current.leftButton.isPressed;
                wasPressedThisFrame = Mouse.current.leftButton.wasPressedThisFrame;
                wasReleasedThisFrame = Mouse.current.leftButton.wasReleasedThisFrame;
                return true;
            }

            screenPosition = default;
            isPressed = false;
            wasPressedThisFrame = false;
            wasReleasedThisFrame = false;
            return false;
        }

        private void BeginDrag(Vector2 screenPosition)
        {
            if (gameEnded)
            {
                return;
            }

            ClearSelection();
            dragging = true;
            TryAddTileAtPointer(screenPosition);
        }

        private void ContinueDrag(Vector2 screenPosition)
        {
            TryAddTileAtPointer(screenPosition);
        }

        private void EndDrag()
        {
            dragging = false;

            if (selectedTiles.Count >= 3)
            {
                var chainLength = selectedTiles.Count;
                var gainedScore = ClearMatchedTiles();
                CollapseAndRefill();
                movesUsed++;
                ShowFeedback($"Nice! +{gainedScore}", new Color(1f, 0.88f, 0.24f));
                RunDesignerAgent();
                LogMove(true, chainLength, gainedScore);
                EvaluateEndState();
            }
            else if (selectedTiles.Count > 0)
            {
                ShowFeedback("Need 3+ links", new Color(0.85f, 0.92f, 1f));
                LogMove(false, selectedTiles.Count, 0);
            }

            ClearSelection();
            RefreshHud();
        }

        private void TryAddTileAtPointer(Vector2 screenPosition)
        {
            var tile = TileAtPointer(screenPosition);
            if (tile == null)
            {
                return;
            }

            if (selectedTiles.Count == 0)
            {
                AddSelectedTile(tile);
                return;
            }

            var last = selectedTiles[^1];
            if (tile == last)
            {
                return;
            }

            if (selectedTiles.Count >= 2 && tile == selectedTiles[^2])
            {
                RemoveLastSelectedTile();
                return;
            }

            if (selectedTiles.Contains(tile) || tile.Type != last.Type || !AreAdjacent(last, tile))
            {
                return;
            }

            AddSelectedTile(tile);
        }

        private PokoTile TileAtPointer(Vector2 screenPosition)
        {
            if (boardCamera == null)
            {
                return null;
            }

            var worldPosition = boardCamera.ScreenToWorldPoint(screenPosition);
            var hit = Physics2D.OverlapPoint(worldPosition);
            return hit == null ? null : hit.GetComponent<PokoTile>();
        }

        private void AddSelectedTile(PokoTile tile)
        {
            ClearLinkHints();
            selectedTiles.Add(tile);
            tile.SetSelected(true);
            RefreshLine();
            RefreshLinkHints();
        }

        private void RemoveLastSelectedTile()
        {
            ClearLinkHints();
            var last = selectedTiles[^1];
            selectedTiles.RemoveAt(selectedTiles.Count - 1);
            last.SetSelected(false);
            RefreshLine();
            RefreshLinkHints();
        }

        private int ClearMatchedTiles()
        {
            var chainLength = selectedTiles.Count;
            var gainedScore = chainLength * chainLength * 10;
            score += gainedScore;

            foreach (var tile in selectedTiles)
            {
                tiles[tile.Column, tile.Row] = null;
                Destroy(tile.gameObject);
            }

            return gainedScore;
        }

        private void CollapseAndRefill()
        {
            for (var column = 0; column < width; column++)
            {
                var writeRow = 0;

                for (var row = 0; row < height; row++)
                {
                    var tile = tiles[column, row];
                    if (tile == null)
                    {
                        continue;
                    }

                    tiles[column, writeRow] = tile;
                    tile.SetGridPosition(column, writeRow, GridToWorld(column, writeRow));

                    if (writeRow != row)
                    {
                        tiles[column, row] = null;
                    }

                    writeRow++;
                }

                for (var row = writeRow; row < height; row++)
                {
                    if (IsInsideBoard(column, row))
                    {
                        tiles[column, row] = CreateTile(column, row, RandomType());
                    }
                }
            }

            EnsurePlayableChain();
        }

        private void ClearSelection()
        {
            ClearLinkHints();

            foreach (var tile in selectedTiles)
            {
                if (tile != null)
                {
                    tile.SetSelected(false);
                }
            }

            selectedTiles.Clear();
            RefreshLine();
        }

        private void RefreshLinkHints()
        {
            if (selectedTiles.Count == 0)
            {
                return;
            }

            var last = selectedTiles[^1];
            foreach (var next in HexGridUtility.GetNeighbors(last.Column, last.Row, height))
            {
                var candidate = tiles[next.x, next.y];
                if (candidate == null || candidate.Type != last.Type || selectedTiles.Contains(candidate))
                {
                    continue;
                }

                candidate.SetLinkHint(true);
                hintedTiles.Add(candidate);
            }
        }

        private void ClearLinkHints()
        {
            foreach (var tile in hintedTiles)
            {
                if (tile != null)
                {
                    tile.SetLinkHint(false);
                }
            }

            hintedTiles.Clear();
        }

        private void RefreshLine()
        {
            if (linkLine == null)
            {
                return;
            }

            linkLine.positionCount = selectedTiles.Count;
            for (var index = 0; index < selectedTiles.Count; index++)
            {
                linkLine.SetPosition(index, selectedTiles[index].transform.position + Vector3.back * 0.1f);
            }
        }

        private void RefreshHud()
        {
            if (scoreText != null)
            {
                var movesLeft = Mathf.Max(0, moveLimit - movesUsed);
                var timeDisplay = $"Time {Mathf.CeilToInt(timeRemaining)}s";
                var state = score >= targetScore ? "Level Clear" : timeRemaining <= 0f ? "Failed" : "Playing";
                scoreText.text = $"Score {score}/{targetScore}\nMoves {movesLeft}\n{timeDisplay}\n{state}";
            }
        }

        private void RunDesignerAgent()
        {
            if (designerAgent == null)
            {
                return;
            }

            var telemetry = new BoardTelemetry(width, height, tileTypes, CountPossibleChains(), FindLongestChain(), score, movesUsed);
            var suggestion = designerAgent.Analyze(telemetry);
            agentHudText = $"Designer {suggestion.DifficultyLabel}: {suggestion.Summary}\nNext: {suggestion.RecommendedAction}";

            if (agentText != null)
            {
                agentText.text =
                    $"Designer Agent: {suggestion.DifficultyLabel}\n" +
                    $"{suggestion.Summary}\n" +
                    $"Intent: {suggestion.DesignIntent}\n" +
                    $"Risk: {suggestion.Risk}\n" +
                    $"Action: {suggestion.RecommendedAction}\n" +
                    $"Tune: {suggestion.SuggestedMoveLimit} moves / {suggestion.SuggestedTargetScore} score / {suggestion.SuggestedTileTypes} types";
            }
        }

        private int CountPossibleChains()
        {
            var count = 0;
            for (var column = 0; column < width; column++)
            {
                for (var row = 0; row < height; row++)
                {
                    if (tiles[column, row] == null)
                    {
                        continue;
                    }

                    count += CountSameNeighbors(column, row) >= 2 ? 1 : 0;
                }
            }

            return count;
        }

        private int FindLongestChain()
        {
            var longest = 1;
            for (var column = 0; column < width; column++)
            {
                for (var row = 0; row < height; row++)
                {
                    longest = Mathf.Max(longest, EstimateFloodSize(column, row));
                }
            }

            return longest;
        }

        private int EstimateFloodSize(int startColumn, int startRow)
        {
            var startTile = tiles[startColumn, startRow];
            if (startTile == null)
            {
                return 0;
            }

            var visited = new bool[width, height];
            var stack = new Stack<Vector2Int>();
            stack.Push(new Vector2Int(startColumn, startRow));
            visited[startColumn, startRow] = true;
            var count = 0;

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                count++;

                foreach (var next in HexGridUtility.GetNeighbors(current.x, current.y, height))
                {
                    if (visited[next.x, next.y])
                    {
                        continue;
                    }

                    var nextTile = tiles[next.x, next.y];
                    if (nextTile == null || nextTile.Type != startTile.Type)
                    {
                        continue;
                    }

                    visited[next.x, next.y] = true;
                    stack.Push(next);
                }
            }

            return count;
        }

        private int CountSameNeighbors(int column, int row)
        {
            var tile = tiles[column, row];
            var count = 0;

            foreach (var next in HexGridUtility.GetNeighbors(column, row, height))
            {
                if (tiles[next.x, next.y]?.Type == tile.Type)
                {
                    count++;
                }
            }

            return count;
        }

        private void EnsurePlayableChain()
        {
            if (FindLongestChain() >= 3 || !TryFindThreeTilePath(out var first, out var second, out var third))
            {
                return;
            }

            var assistedType = first.Type;
            second.SetType(assistedType);
            third.SetType(assistedType);
        }

        private bool TryFindThreeTilePath(out PokoTile first, out PokoTile second, out PokoTile third)
        {
            for (var column = 0; column < width; column++)
            {
                for (var row = 0; row < height; row++)
                {
                    if (!IsInsideBoard(column, row))
                    {
                        continue;
                    }

                    first = tiles[column, row];
                    if (first == null)
                    {
                        continue;
                    }

                    foreach (var secondPosition in HexGridUtility.GetNeighbors(column, row, height))
                    {
                        second = tiles[secondPosition.x, secondPosition.y];
                        if (second == null)
                        {
                            continue;
                        }

                        foreach (var thirdPosition in HexGridUtility.GetNeighbors(secondPosition.x, secondPosition.y, height))
                        {
                            if (thirdPosition.x == column && thirdPosition.y == row)
                            {
                                continue;
                            }

                            third = tiles[thirdPosition.x, thirdPosition.y];
                            if (third != null)
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            first = null;
            second = null;
            third = null;
            return false;
        }

        private bool AreAdjacent(PokoTile a, PokoTile b)
        {
            return HexGridUtility.AreAdjacent(a.Column, a.Row, b.Column, b.Row);
        }

        private bool IsInside(int column, int row)
        {
            return column >= 0 && column < width && row >= 0 && row < height;
        }

        private bool IsInsideBoard(int column, int row)
        {
            if (column < 0 || row < 0 || row >= height)
            {
                return false;
            }

            var maxCol = useHexGrid ? HexGridUtility.RowSize(row) : width;
            return column < maxCol;
        }

        private Vector3 GridToWorld(int column, int row)
        {
            return HexGridUtility.ToWorld(column, row, width, height, spacing, useHexGrid);
        }

        private void FramePlayCamera()
        {
            if (boardCamera == null || !boardCamera.orthographic)
            {
                return;
            }

            var boardHalfHeight = Mathf.Max(1f, (height - 1) * spacing * (useHexGrid ? HexGridUtility.VerticalSpacingRatio : 1f) * 0.5f);
            var boardHalfWidth = Mathf.Max(1f, width * spacing * 0.5f);
            var verticalSize = boardHalfHeight + 2.25f;
            var horizontalSize = boardHalfWidth / Mathf.Max(0.5f, boardCamera.aspect) + 1.5f;
            boardCamera.orthographicSize = Mathf.Max(boardCamera.orthographicSize, 6.6f, verticalSize, horizontalSize);

            var cameraPosition = boardCamera.transform.position;
            boardCamera.transform.position = new Vector3(cameraPosition.x, 0.72f, cameraPosition.z);
        }

        private void PrepareHud()
        {
            SetLegacyHudVisible(!useScreenHud);

            var boardHalfHeight = Mathf.Max(1f, (height - 1) * spacing * (useHexGrid ? HexGridUtility.VerticalSpacingRatio : 1f) * 0.5f);
            var boardHalfWidth = Mathf.Max(1f, width * spacing * 0.5f);
            var left = -boardHalfWidth + 0.1f;
            var top = boardHalfHeight + 0.85f;
            var bottom = -boardHalfHeight - 0.55f;

            if (scoreText != null)
            {
                scoreText.transform.position = new Vector3(left, top, 0f);
            }

            if (agentText != null)
            {
                agentText.transform.position = new Vector3(left, bottom, 0f);
            }

            if (feedbackText != null)
            {
                feedbackText.transform.position = new Vector3(0f, top + 0.15f, 0f);
            }
        }

        private void SetLegacyHudVisible(bool visible)
        {
            SetTextMeshVisible(scoreText, visible);
            SetTextMeshVisible(agentText, visible);
            SetTextMeshVisible(feedbackText, visible);
        }

        private static void SetTextMeshVisible(TextMesh textMesh, bool visible)
        {
            if (textMesh != null && textMesh.TryGetComponent<MeshRenderer>(out var renderer))
            {
                renderer.enabled = visible;
            }
        }

        private PokoTileType RandomType()
        {
            var maxTypes = Mathf.Clamp(tileTypes, 1, 6);
            var weights = levelConfig == null ? null : levelConfig.SpawnWeights;
            if (weights == null || weights.Length < maxTypes)
            {
                return (PokoTileType)Random.Range(0, maxTypes);
            }

            var totalWeight = 0;
            for (var index = 0; index < maxTypes; index++)
            {
                totalWeight += Mathf.Max(1, weights[index]);
            }

            var roll = Random.Range(0, totalWeight);
            for (var index = 0; index < maxTypes; index++)
            {
                roll -= Mathf.Max(1, weights[index]);
                if (roll < 0)
                {
                    return (PokoTileType)index;
                }
            }

            return (PokoTileType)(maxTypes - 1);
        }

        private void ApplyLevelConfig()
        {
            if (levelConfig == null)
            {
                return;
            }

            width = levelConfig.Width;
            height = levelConfig.Height;
            tileTypes = levelConfig.TileTypes;
            useHexGrid = levelConfig.UseHexGrid;
            moveLimit = levelConfig.MoveLimit;
            targetScore = levelConfig.TargetScore;
        }

        private void EvaluateEndState()
        {
            if (score >= targetScore)
            {
                gameEnded = true;
                ShowFeedback("LEVEL CLEAR", new Color(0.40f, 1f, 0.55f), 3.5f);
                LogEndState("clear");
                return;
            }

            if (timeRemaining <= 0f)
            {
                gameEnded = true;
                ShowFeedback("TIME'S UP", new Color(1f, 0.32f, 0.32f), 3.5f);
                LogEndState("fail");
                return;
            }

            if (movesUsed >= moveLimit)
            {
                gameEnded = true;
                ShowFeedback("FAILED - RETUNE LEVEL", new Color(1f, 0.32f, 0.32f), 3.5f);
                LogEndState("fail");
            }
        }

        private void ShowFeedback(string message, Color color, float duration = 1.25f)
        {
            feedbackMessage = message;
            feedbackColor = color;
            feedbackClearTime = Time.time + duration;

            if (feedbackText != null)
            {
                feedbackText.text = message;
                feedbackText.color = color;
            }
        }

        private void RefreshTimedFeedback()
        {
            if (feedbackClearTime <= 0f || Time.time < feedbackClearTime || gameEnded)
            {
                return;
            }

            feedbackMessage = string.Empty;
            if (feedbackText != null)
            {
                feedbackText.text = string.Empty;
            }

            feedbackClearTime = 0f;
        }

        private void RestartRound()
        {
            ClearSelection();
            DestroyBoardTiles();
            score = 0;
            movesUsed = 0;
            timeRemaining = roundTime;
            gameEnded = false;
            feedbackClearTime = 0f;
            feedbackMessage = string.Empty;
            tiles = new PokoTile[width, height];
            BuildBoard();
            StartPlayLog();
            RefreshHud();
            RunDesignerAgent();
        }

        private void DestroyBoardTiles()
        {
            if (tiles == null)
            {
                return;
            }

            foreach (var tile in tiles)
            {
                if (tile == null)
                {
                    continue;
                }

                tile.gameObject.SetActive(false);
                Destroy(tile.gameObject);
            }
        }

        private static GUIStyle CreateHudStyle(float fontSize, FontStyle fontStyle, Color color, float scale, TextAnchor alignment = TextAnchor.UpperLeft)
        {
            return new GUIStyle(GUI.skin.label)
            {
                alignment = alignment,
                fontSize = Mathf.Max(11, Mathf.RoundToInt(fontSize * scale)),
                fontStyle = fontStyle,
                normal = { textColor = color },
                wordWrap = true,
                richText = false
            };
        }

        private static void DrawHudPanel(Rect rect)
        {
            var previousColor = GUI.color;
            GUI.color = new Color(0.03f, 0.05f, 0.09f, 0.86f);
            GUI.Box(rect, GUIContent.none);
            GUI.color = previousColor;
        }

        private void DrawEndPanel(float scale)
        {
            var panelWidth = Mathf.Min(Screen.width - 28f * scale, 360f * scale);
            var panelHeight = 176f * scale;
            var panel = new Rect(
                (Screen.width - panelWidth) * 0.5f,
                (Screen.height - panelHeight) * 0.5f,
                panelWidth,
                panelHeight);

            DrawHudPanel(panel);
            GUI.Label(
                new Rect(panel.x + 18f * scale, panel.y + 18f * scale, panel.width - 36f * scale, 40f * scale),
                score >= targetScore ? "LEVEL CLEAR" : "LEVEL FAILED",
                CreateHudStyle(28f, FontStyle.Bold, score >= targetScore ? new Color(0.40f, 1f, 0.55f) : new Color(1f, 0.42f, 0.42f), scale, TextAnchor.UpperCenter));
            GUI.Label(
                new Rect(panel.x + 18f * scale, panel.y + 66f * scale, panel.width - 36f * scale, 34f * scale),
                "Drag input pauses when a round ends.",
                CreateHudStyle(15f, FontStyle.Normal, Color.white, scale, TextAnchor.UpperCenter));

            if (GUI.Button(
                    new Rect(panel.x + 72f * scale, panel.y + 116f * scale, panel.width - 144f * scale, 38f * scale),
                    "Restart Round",
                    CreateButtonStyle(scale)))
            {
                RestartRound();
            }
        }

        private static GUIStyle CreateButtonStyle(float scale)
        {
            return new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Max(12, Mathf.RoundToInt(16f * scale)),
                fontStyle = FontStyle.Bold,
                richText = false
            };
        }

        private void StartPlayLog()
        {
            if (!enablePlayLog)
            {
                return;
            }

            levelPlayLogPath = BuildLevelPlayLogPath();
            var sessionLine =
                "{" +
                "\"event\":\"session_start\"," +
                $"\"levelId\":\"{EscapeJson(levelConfig == null ? "prototype" : levelConfig.LevelId)}\"," +
                $"\"width\":{width}," +
                $"\"height\":{height}," +
                $"\"tileTypes\":{tileTypes}," +
                $"\"moveLimit\":{moveLimit}," +
                $"\"targetScore\":{targetScore}," +
                $"\"useHexGrid\":{JsonBool(useHexGrid)}" +
                "}";

            WritePlayLog(playLogPath, sessionLine);
            if (!string.IsNullOrWhiteSpace(levelPlayLogPath) && levelPlayLogPath != playLogPath)
            {
                WritePlayLog(levelPlayLogPath, sessionLine);
            }
        }

        private void LogMove(bool valid, int chainLength, int gainedScore)
        {
            if (!enablePlayLog)
            {
                return;
            }

            AppendPlayLog(
                "{" +
                "\"event\":\"move\"," +
                $"\"valid\":{JsonBool(valid)}," +
                $"\"chainLength\":{chainLength}," +
                $"\"gainedScore\":{gainedScore}," +
                $"\"score\":{score}," +
                $"\"movesUsed\":{movesUsed}," +
                $"\"movesLeft\":{Mathf.Max(0, moveLimit - movesUsed)}," +
                $"\"timeLeft\":{Mathf.CeilToInt(timeRemaining)}," +
                $"\"possibleChains\":{CountPossibleChains()}," +
                $"\"longestChain\":{FindLongestChain()}" +
                "}");
        }

        private void LogEndState(string result)
        {
            if (!enablePlayLog)
            {
                return;
            }

            AppendPlayLog(
                "{" +
                "\"event\":\"end\"," +
                $"\"result\":\"{EscapeJson(result)}\"," +
                $"\"score\":{score}," +
                $"\"movesUsed\":{movesUsed}," +
                $"\"timeLeft\":{Mathf.CeilToInt(timeRemaining)}," +
                $"\"targetScore\":{targetScore}," +
                $"\"moveLimit\":{moveLimit}" +
                "}");
        }

        private void AppendPlayLog(string line)
        {
            AppendPlayLog(playLogPath, line);
            if (!string.IsNullOrWhiteSpace(levelPlayLogPath) && levelPlayLogPath != playLogPath)
            {
                AppendPlayLog(levelPlayLogPath, line);
            }
        }

        private static void WritePlayLog(string path, string line)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, line + "\n");
        }

        private static void AppendPlayLog(string path, string line)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.AppendAllText(path, line + "\n");
        }

        private string BuildLevelPlayLogPath()
        {
            var levelId = levelConfig == null ? "prototype" : levelConfig.LevelId;
            return $"md/playtest-logs/by-level/{SafeFileSegment(levelId)}-latest.jsonl";
        }

        private static string SafeFileSegment(string value)
        {
            var builder = new StringBuilder();
            foreach (var character in value)
            {
                builder.Append(char.IsLetterOrDigit(character) || character == '_' || character == '-'
                    ? character
                    : '_');
            }

            return builder.Length == 0 ? "unknown" : builder.ToString();
        }

        private static string JsonBool(bool value)
        {
            return value ? "true" : "false";
        }

        private static string EscapeJson(string value)
        {
            var builder = new StringBuilder();
            foreach (var character in value)
            {
                switch (character)
                {
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        builder.Append(character);
                        break;
                }
            }

            return builder.ToString();
        }

        private static Sprite CreateHexSprite()
        {
            const int size = 96;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            var outerRadius = size * 0.42f;
            var innerRadius = size * 0.34f;
            var highlightRadius = size * 0.28f;
            var outer = new Vector2[6];
            var inner = new Vector2[6];
            var highlight = new Vector2[6];

            for (var index = 0; index < 6; index++)
            {
                var angle = Mathf.Deg2Rad * (60f * index);
                var cos = Mathf.Cos(angle);
                var sin = Mathf.Sin(angle);
                outer[index] = new Vector2(center.x + outerRadius * cos, center.y + outerRadius * sin);
                inner[index] = new Vector2(center.x + innerRadius * cos, center.y + innerRadius * sin);
                highlight[index] = new Vector2(center.x + highlightRadius * cos, center.y + highlightRadius * sin);
            }

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var point = new Vector2(x, y);
                    var inOuter = IsInsideHex(point, outer);
                    var inInner = IsInsideHex(point, inner);
                    var inHighlight = IsInsideHex(point, highlight);

                    if (!inOuter)
                    {
                        texture.SetPixel(x, y, new Color(0f, 0f, 0f, 0f));
                        continue;
                    }

                    if (!inInner)
                    {
                        texture.SetPixel(x, y, new Color(0.35f, 0.35f, 0.35f, 1f));
                        continue;
                    }

                    var alpha = inHighlight ? 1f : 0.85f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static bool IsInsideHex(Vector2 point, Vector2[] vertices)
        {
            for (var index = 0; index < 6; index++)
            {
                var next = (index + 1) % 6;
                var edge = vertices[next] - vertices[index];
                var toPoint = point - vertices[index];
                var cross = edge.x * toPoint.y - edge.y * toPoint.x;
                if (cross < 0f)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
