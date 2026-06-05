using System.Collections;
using System.Collections.Generic;
using PokoPuzzle.AI;
using PokoPuzzle.Core.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PokoPuzzle.Core
{
    public sealed class LineLinkerBoard : MonoBehaviour
    {
        [Header("Board")]
        [SerializeField] private int width = 4;
        [SerializeField] private int height = 9;
        [SerializeField] private int tileTypes = 5;
        [SerializeField] private float spacing = 0.85f;
        [SerializeField] private bool useHexGrid = true;
        [SerializeField] private PokoTileVisualStyle tileVisualStyle = PokoTileVisualStyle.Hex;
        [SerializeField] private int moveLimit = 999;
        [SerializeField] private float roundTime = 60f;
        [SerializeField] private int targetScore = 10000;
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
        private Sprite[] tileSprites;
        private IGameDesignerAgent designerAgent;

        [Header("Data Providers (ScriptableObject DI)")]
        [SerializeField] private PokoEnemyDatabase enemyDatabase;
        [SerializeField] private PokoEnemySkillDatabase skillDatabase;
        [SerializeField] private PokoRegularEnemyDatabase regularEnemyDatabase;
        [SerializeField] private PokoBalanceProfileDatabase balanceProfileDatabase;
        [SerializeField] private string balanceProfileId = "default";

        private float enemySkillTimer;
        private float skillCooldown = 10f;

        private int score;
        private int movesUsed;
        private float timeRemaining;
        private bool dragging;
        private bool gameEnded;
        private bool resolvingVisualEffect;
        private PokoTile currentPointerTile;
        private string agentHudText = "Designer analyzing board...";

        private int comboCount;
        private float lastClearTime;
        private bool feverActive;
        private float feverTimer;
        private const float ComboWindow = 2.5f;
        private const float FeverDuration = 6f;
        private const int FeverComboThreshold = 7;
        private const int TimeBonusSeconds = 2;

        private PlayLogger playLogger;
        private BoardHudRenderer hudRenderer;
        private BoardEffectRenderer effectRenderer;
        private BoardEnemy enemy;
        private int bossMaxHp = 100;
        private int bossDefeatBonus = 500;
        private string bossName = "Monster";
        private int regularEnemyHpOverride;
        private int bossHpOverride;
        private BalanceProfileData activeBalanceProfile;
        private int totalDamageDealt;
        private int bombsClearedCount;
        private int specialBlocksClearedCount;
        private int rainbowClearedCount;
        private int rainbowTapsCount;
        private int enemySpawnIndex;
        private int regularSpawnCount;
        private const float WaveHpMultiplier = 1.25f;

        private static readonly Dictionary<int, int[]> BossRegularThemeMap = new()
        {
            { 1, new[] { 1, 7 } },
            { 2, new[] { 3, 8 } },
            { 3, new[] { 5, 9 } },
            { 4, new[] { 2, 4 } },
            { 5, new[] { 10, 6 } },
            { 6, new[] { 11, 12 } },
        };

        private readonly List<int> bossCycleOrder = new();
        private int bossCyclePtr;
        private int bossGroupStep;
        private int currentGroupBossWave;
        private int[] currentGroupRegulars = new int[2];

        private float rainbowGauge;
        private const float RainbowGaugeMax = 100f;
        private const float RainbowGaugeFillPerBlock = 5f;
        private const float TilePointerRadius = 0.36f;

        private readonly List<PokoTile> bombTiles = new();

        private void Awake()
        {
            if (boardCamera == null)
            {
                boardCamera = Camera.main;
            }

            tiles = new PokoTile[width, height];
            designerAgent = new HeuristicGameDesignerAgent();
            InitDataProviders();
            ApplyLevelConfig();
            hudRenderer = new BoardHudRenderer(boardCamera, scoreText, agentText, feedbackText,
                useScreenHud, width, height, spacing, useHexGrid);
            hudRenderer.FramePlayCamera();
            hudRenderer.PrepareHud();
            hudRenderer.OnRestartRequested = RestartRound;
            effectRenderer = GetComponent<BoardEffectRenderer>();
            if (effectRenderer == null)
            {
                effectRenderer = gameObject.AddComponent<BoardEffectRenderer>();
            }

            effectRenderer.Configure(boardCamera);
            tileSprites = TileSpriteGenerator.CreateTileSprites(tileVisualStyle);

            if (enemy == null)
            {
                enemy = new BoardEnemy();
            }

            BuildBoard();
            timeRemaining = roundTime;
            playLogger = CreatePlayLogger();
            playLogger.WriteSessionStart(
                levelConfig == null ? "prototype" : levelConfig.LevelId,
                width, height, tileTypes, moveLimit, Mathf.CeilToInt(roundTime), targetScore, useHexGrid, balanceProfileId);
            hudRenderer?.RefreshHud(score, enemySpawnIndex, Mathf.CeilToInt(timeRemaining));
            RunDesignerAgent();
        }

        private void Update()
        {
            RefreshTimedFeedback();

            if (gameEnded)
            {
                return;
            }

            if (resolvingVisualEffect)
            {
                return;
            }

            timeRemaining -= Time.deltaTime;
            if (timeRemaining <= 0f)
            {
                timeRemaining = 0f;
                gameEnded = true;
                EvaluateEndState();
                hudRenderer?.RefreshHud(score, enemySpawnIndex, Mathf.CeilToInt(timeRemaining));
                return;
            }

            TickComboTimeout();
            TickFever();
            TickBombs();
            TickEnemySkills();

            if (!TryReadPointer(out var screenPosition, out var isPressed, out var wasPressedThisFrame, out var wasReleasedThisFrame))
            {
                return;
            }

            if (wasPressedThisFrame)
            {
                if (TryDetonateBombAtPointer(screenPosition))
                {
                    return;
                }

                BeginDrag(screenPosition);
            }
            else if (dragging && isPressed)
            {
                ContinueDrag(screenPosition);
            }
            else if (dragging && wasReleasedThisFrame)
            {
                dragging = false;
                EndDrag();
            }
            else if (dragging && !isPressed)
            {
                dragging = false;
                EndDrag();
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
            var sprite = tileSprites != null ? tileSprites[(int)type] : null;
            tile.Initialize(column, row, type, sprite);
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
            var tile = TileAtPointer(screenPosition);
            currentPointerTile = tile;
            if (tile != null && !tile.IsLinkable)
            {
                if (tile.IsBomb)
                {
                    DetonateBomb(tile);
                    dragging = false;
                    return;
                }

                if (tile.IsFrozen)
                {
                    hudRenderer.ShowFeedback("Frozen - clear adjacent tile to break", new Color(0.6f, 0.8f, 1f), 1.5f);
                    dragging = false;
                    return;
                }

                if (tile.IsStone)
                {
                    hudRenderer.ShowFeedback("Stone - falls to bottom", new Color(0.5f, 0.5f, 0.5f), 1.5f);
                    dragging = false;
                    return;
                }

                if (tile.IsPetrified)
                {
                    hudRenderer.ShowFeedback("Petrified - immune to bombs", new Color(0.62f, 0.28f, 0.9f), 1.5f);
                    dragging = false;
                    return;
                }

                dragging = false;
                return;
            }

            TryAddTileAtPointer(screenPosition);
        }

        private void ContinueDrag(Vector2 screenPosition)
        {
            currentPointerTile = TileAtPointer(screenPosition);
            TryAddTileAtPointer(screenPosition);
            RefreshLine();
        }

        private void EndDrag()
        {
            if (selectedTiles.Count >= 3)
            {
                CommitChain();
            }
            else if (selectedTiles.Count > 0)
            {
                hudRenderer.ShowFeedback("Need 3+ links", new Color(0.85f, 0.92f, 1f), 1.5f);
                ResetCombo();
                playLogger?.LogMove(false, selectedTiles.Count, 0, score, movesUsed, moveLimit,
                    Mathf.CeilToInt(timeRemaining), CountPossibleChains(), FindLongestChain());
                ClearSelection();
            }

            hudRenderer?.RefreshHud(score, enemySpawnIndex, Mathf.CeilToInt(timeRemaining));
            currentPointerTile = null;
        }

        private void CommitChain()
        {
            var chainLength = selectedTiles.Count;
            var selectedPositions = CaptureTilePositions(selectedTiles);
            var chainCenter = AveragePosition(selectedPositions);
            var baseScore = ClearMatchedTiles();

            ApplyComboIncrement();
            var comboMultiplier = feverActive ? 2 : Mathf.Max(1, comboCount);
            var gainedScore = baseScore * comboMultiplier;
            score += gainedScore;
            effectRenderer?.PlayChainClear(selectedPositions, chainCenter, gainedScore, feverActive);
            ApplyEnemyDamage(chainLength, chainCenter);
            var chainEnd = selectedTiles.Count > 0
                ? new Vector2Int(selectedTiles[^1].Column, selectedTiles[^1].Row)
                : (Vector2Int?)null;
            var chainTileType = selectedTiles.Count > 0 ? selectedTiles[^1].Type : PokoTileType.Red;
            var createdBomb = TryPlaceBomb(chainLength, chainEnd, chainTileType, out var createdBombType);
            ClearAdjacentFrozenTiles();

            CollapseAndRefill();
            movesUsed++;
            var feedback = createdBomb
                ? $"{createdBombType} Bomb Created! +{gainedScore}"
                : $"Nice! +{gainedScore}";
            hudRenderer.ShowFeedback(feedback, createdBomb ? new Color(1f, 0.55f, 0.16f) : new Color(1f, 0.88f, 0.24f), 1.5f);
            RunDesignerAgent();
            playLogger?.LogMove(true, chainLength, gainedScore, score, movesUsed, moveLimit,
                Mathf.CeilToInt(timeRemaining), CountPossibleChains(), FindLongestChain());
            EvaluateEndState();
            selectedTiles.Clear();
            hintedTiles.Clear();
            RefreshLine();
            hudRenderer?.RefreshHud(score, enemySpawnIndex, Mathf.CeilToInt(timeRemaining));
        }

        private void ApplyComboIncrement()
        {
            if (Time.time - lastClearTime <= ComboWindow && lastClearTime > 0f)
            {
                comboCount++;
            }
            else
            {
                comboCount = 1;
            }

            lastClearTime = Time.time;

            TryStartFever(comboCount >= FeverComboThreshold);
        }

        private void TryStartFever(bool shouldStart)
        {
            if (!shouldStart || feverActive)
            {
                return;
            }

            feverActive = true;
            feverTimer = FeverDuration;
            hudRenderer.ShowFeedback("FEVER!", new Color(1f, 0.4f, 0.1f), 2f);
            effectRenderer?.PlayFeverStart();
            playLogger?.LogFeverEvent("start", comboCount, Mathf.CeilToInt(timeRemaining));
        }

        private void ResetCombo()
        {
            comboCount = 0;
            lastClearTime = 0f;
        }

        private void TickComboTimeout()
        {
            if (comboCount > 0 && lastClearTime > 0f && Time.time - lastClearTime > ComboWindow)
            {
                ResetCombo();
            }
        }

        private void TickFever()
        {
            if (!feverActive)
            {
                return;
            }

            feverTimer -= Time.deltaTime;
            if (feverTimer <= 0f)
            {
                feverActive = false;
                feverTimer = 0f;
                playLogger?.LogFeverEvent("end", comboCount, Mathf.CeilToInt(timeRemaining));
            }
        }

        private void ApplyEnemyDamage(int chainLength, Vector3 sourcePosition)
        {
            if (enemy == null || enemy.IsDefeated)
            {
                return;
            }

            var damage = chainLength * 10;
            var dealt = enemy.ApplyDamage(damage);
            totalDamageDealt += dealt;
            effectRenderer?.PlayDamage(sourcePosition, dealt, enemy.Wave > 0);
            hudRenderer?.PlayDamagePulse(enemy.Wave > 0);
            playLogger?.LogCombatEvent("enemy_damage", dealt, enemy.CurrentHp,
                comboCount, feverActive, enemy.CurrentHp, enemy.MaxHp, Mathf.CeilToInt(timeRemaining));

            if (enemy.IsDefeated)
            {
                score += enemy.DefeatBonus;
                hudRenderer.ShowFeedback($"{enemy.Name} Defeated! +{enemy.DefeatBonus}", new Color(0.4f, 1f, 0.55f), 2f);

                if (!gameEnded)
                {
                    SpawnNextEnemy();
                }
            }
        }

        private void ApplyWeaponDamage(int clearedCount, Vector3 sourcePosition)
        {
            if (enemy == null || enemy.IsDefeated || clearedCount <= 0)
            {
                return;
            }

            var damage = clearedCount * 10;
            var dealt = enemy.ApplyDamage(damage);
            totalDamageDealt += dealt;
            effectRenderer?.PlayDamage(sourcePosition, dealt, enemy.Wave > 0);
            hudRenderer?.PlayDamagePulse(enemy.Wave > 0);
            playLogger?.LogCombatEvent("weapon_damage", dealt, enemy.CurrentHp,
                comboCount, feverActive, enemy.CurrentHp, enemy.MaxHp, Mathf.CeilToInt(timeRemaining));

            if (enemy.IsDefeated)
            {
                score += enemy.DefeatBonus;
                hudRenderer.ShowFeedback($"{enemy.Name} Defeated! +{enemy.DefeatBonus}", new Color(0.4f, 1f, 0.55f), 2f);

                if (!gameEnded)
                {
                    SpawnNextEnemy();
                }
            }
        }

        private void TryAddTileAtPointer(Vector2 screenPosition)
        {
            var tile = TileAtPointer(screenPosition);
            if (tile == null || !tile.IsLinkable)
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
                if (selectedTiles.Count < 3)
                {
                    RemoveLastSelectedTile();
                }

                return;
            }

            if (selectedTiles.Contains(tile) || !CanLinkTiles(last, tile) || !AreAdjacent(last, tile))
            {
                return;
            }

            AddSelectedTile(tile);
        }

        private bool TryDetonateBombAtPointer(Vector2 screenPosition)
        {
            if (boardCamera == null)
            {
                return false;
            }

            var bombTile = BombAtPointer(screenPosition);

            if (bombTile == null)
            {
                return false;
            }

            DetonateBomb(bombTile);
            return true;
        }

        private PokoTile BombAtPointer(Vector2 screenPosition)
        {
            if (boardCamera == null)
            {
                return null;
            }

            var worldPosition = boardCamera.ScreenToWorldPoint(screenPosition);
            var worldPoint = new Vector2(worldPosition.x, worldPosition.y);

            Physics2D.SyncTransforms();
            PokoTile closest = null;
            var minDist = float.MaxValue;
            var hits = Physics2D.OverlapCircleAll(worldPoint, TilePointerRadius);
            for (var index = 0; index < hits.Length; index++)
            {
                var tile = hits[index].GetComponent<PokoTile>();
                if (tile == null || !tile.gameObject.activeInHierarchy || !tile.IsBomb)
                {
                    continue;
                }

                var dist = DistanceToTileSqr(worldPoint, tile);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = tile;
                }
            }

            var boardClosest = ClosestBoardTile(worldPoint, TilePointerRadius);
            if (boardClosest != null && boardClosest.IsBomb)
            {
                var dist = DistanceToTileSqr(worldPoint, boardClosest);
                if (dist < minDist)
                {
                    closest = boardClosest;
                }
            }

            return closest;
        }

        private PokoTile TileAtPointer(Vector2 screenPosition)
        {
            if (boardCamera == null)
            {
                return null;
            }

            var worldPosition = boardCamera.ScreenToWorldPoint(screenPosition);
            var worldPoint = new Vector2(worldPosition.x, worldPosition.y);

            Physics2D.SyncTransforms();
            PokoTile closest = null;
            var minDist = float.MaxValue;
            var hits = Physics2D.OverlapCircleAll(worldPoint, TilePointerRadius);
            for (var index = 0; index < hits.Length; index++)
            {
                var tile = hits[index].GetComponent<PokoTile>();
                if (tile == null || !tile.gameObject.activeInHierarchy)
                {
                    continue;
                }

                var dist = DistanceToTileSqr(worldPoint, tile);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = tile;
                }
            }

            var boardClosest = ClosestBoardTile(worldPoint, TilePointerRadius);
            if (boardClosest != null)
            {
                var dist = DistanceToTileSqr(worldPoint, boardClosest);
                if (dist < minDist)
                {
                    closest = boardClosest;
                }
            }

            return closest;
        }

        private PokoTile ClosestBoardTile(Vector2 worldPoint, float radius)
        {
            if (tiles == null)
            {
                return null;
            }

            var radiusSqr = radius * radius;
            PokoTile closest = null;
            var minDist = float.MaxValue;
            for (var column = 0; column < width; column++)
            {
                for (var row = 0; row < height; row++)
                {
                    var tile = tiles[column, row];
                    if (tile == null || !tile.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    var dist = DistanceToTileSqr(worldPoint, tile);
                    if (dist <= radiusSqr && dist < minDist)
                    {
                        minDist = dist;
                        closest = tile;
                    }
                }
            }

            return closest;
        }

        private float DistanceToTileSqr(Vector2 worldPoint, PokoTile tile)
        {
            var tilePos = tile.transform.position;
            return (worldPoint - new Vector2(tilePos.x, tilePos.y)).sqrMagnitude;
        }

        private void AddSelectedTile(PokoTile tile)
        {
            if (tile == null || !tile.IsLinkable)
            {
                return;
            }

            PruneSelectedTiles();
            ClearLinkHints();
            selectedTiles.Add(tile);
            tile.SetSelected(true);
            RefreshLine();
            RefreshLinkHints();
            RefreshChainRewardFeedback();
        }

        private void RemoveLastSelectedTile()
        {
            PruneSelectedTiles();
            if (selectedTiles.Count == 0)
            {
                RefreshLine();
                return;
            }

            ClearLinkHints();
            var last = selectedTiles[^1];
            selectedTiles.RemoveAt(selectedTiles.Count - 1);
            if (last != null)
            {
                last.SetSelected(false);
            }

            RefreshLine();
            RefreshLinkHints();
        }

        private int ClearMatchedTiles()
        {
            if (tiles == null)
            {
                return 0;
            }

            var chainLength = selectedTiles.Count;
            var gainedScore = chainLength * chainLength * 10;
            var tilesToDestroy = new List<PokoTile>(selectedTiles);

            if (feverActive)
            {
                var cascadeNeighbors = new HashSet<PokoTile>();
                foreach (var tile in selectedTiles)
                {
                    if (tile == null)
                    {
                        continue;
                    }

                    foreach (var neighborPos in HexGridUtility.GetNeighbors(tile.Column, tile.Row, width, height))
                    {
                        var neighbor = tiles[neighborPos.x, neighborPos.y];
                        if (neighbor != null && !tilesToDestroy.Contains(neighbor) && neighbor.IsLinkable)
                        {
                            cascadeNeighbors.Add(neighbor);
                        }
                    }
                }

                tilesToDestroy.AddRange(cascadeNeighbors);
            }

            foreach (var tile in tilesToDestroy)
            {
                if (tile == null)
                {
                    continue;
                }

                var tileColumn = -1;
                var tileRow = -1;
                if (tiles != null)
                {
                    for (var c = 0; c < width && tileColumn == -1; c++)
                    {
                        for (var r = 0; r < height; r++)
                        {
                            if (tiles[c, r] == tile)
                            {
                                tileColumn = c;
                                tileRow = r;
                                break;
                            }
                        }
                    }
                }

                if (tileColumn == -1)
                {
                    for (var c = 0; c < width && tileColumn == -1; c++)
                    {
                        for (var r = 0; r < height; r++)
                        {
                            if (ReferenceEquals(tiles[c, r], tile))
                            {
                                tileColumn = c;
                                tileRow = r;
                                break;
                            }
                        }
                    }

                    if (tileColumn != -1)
                    {
                        tiles[tileColumn, tileRow] = null;
                    }

                    tile.PlayClearAndDestroy();
                    continue;
                }

                tiles[tileColumn, tileRow] = null;
                tile.PlayClearAndDestroy();

                foreach (var frozenPos in HexGridUtility.GetNeighbors(tileColumn, tileRow, width, height))
                {
                    ClearFrozenTileAt(frozenPos.x, frozenPos.y);
                    HitStoneTileAt(frozenPos.x, frozenPos.y);
                }
            }

            FillRainbowGauge(tilesToDestroy.Count);
            return gainedScore;
        }

        private void ClearAdjacentFrozenTiles()
        {
            var changed = true;
            while (changed)
            {
                changed = false;
                for (var column = 0; column < width; column++)
                {
                    for (var row = 0; row < height; row++)
                    {
                        var tile = tiles[column, row];
                        if (tile == null || !tile.IsFrozen)
                        {
                            continue;
                        }

                        foreach (var neighborPos in HexGridUtility.GetNeighbors(column, row, width, height))
                        {
                            if (tiles[neighborPos.x, neighborPos.y] == null)
                            {
                                ClearFrozenTileAt(column, row);
                                changed = true;
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void ClearFrozenTileAt(int column, int row)
        {
            if (!IsInside(column, row))
            {
                return;
            }

            var tile = tiles[column, row];
            if (tile == null || !tile.IsFrozen)
            {
                return;
            }

            tile.ConfigureSubtype(PokoBlockSubtype.None);
            score += 20;
            specialBlocksClearedCount++;
            playLogger?.LogCombatEvent("special_block_clear", 1, 1,
                comboCount, feverActive, enemy?.CurrentHp ?? 0, enemy?.MaxHp ?? 0, Mathf.CeilToInt(timeRemaining));
            hudRenderer?.ShowFeedback("Frozen Clear! +20", new Color(0.6f, 0.8f, 1f), 1.2f);

            if (enemy != null && !enemy.IsDefeated)
            {
                var dealt = enemy.ApplyDamage(1);
                totalDamageDealt += dealt;
                effectRenderer?.PlayDamage(tile.transform.position, dealt, enemy.Wave > 0);
                hudRenderer?.PlayDamagePulse(enemy.Wave > 0);
                playLogger?.LogCombatEvent("enemy_damage", dealt, enemy.CurrentHp,
                    comboCount, feverActive, enemy.CurrentHp, enemy.MaxHp, Mathf.CeilToInt(timeRemaining));

                if (enemy.IsDefeated)
                {
                    score += enemy.DefeatBonus;
                    hudRenderer.ShowFeedback($"{enemy.Name} Defeated! +{enemy.DefeatBonus}", new Color(0.4f, 1f, 0.55f), 2f);

                    if (!gameEnded)
                    {
                        SpawnNextEnemy();
                    }
                }
            }
        }

        private bool HitStoneTileAt(int column, int row)
        {
            if (!IsInside(column, row))
            {
                return false;
            }

            var tile = tiles[column, row];
            if (tile == null || !tile.IsStone)
            {
                return false;
            }

            if (!tile.DamageStone())
            {
                return false;
            }

            tiles[column, row] = null;
            tile.PlayClearAndDestroy();
            score += 30;
            specialBlocksClearedCount++;
            playLogger?.LogCombatEvent("special_block_clear", 2, 1,
                comboCount, feverActive, enemy?.CurrentHp ?? 0, enemy?.MaxHp ?? 0, Mathf.CeilToInt(timeRemaining));
            hudRenderer?.ShowFeedback("Stone Clear! +30", new Color(0.5f, 0.5f, 0.5f), 1.2f);

            if (enemy != null && !enemy.IsDefeated)
            {
                var dealt = enemy.ApplyDamage(1);
                totalDamageDealt += dealt;
                effectRenderer?.PlayDamage(tile.transform.position, dealt, enemy.Wave > 0);
                hudRenderer?.PlayDamagePulse(enemy.Wave > 0);
                playLogger?.LogCombatEvent("enemy_damage", dealt, enemy.CurrentHp,
                    comboCount, feverActive, enemy.CurrentHp, enemy.MaxHp, Mathf.CeilToInt(timeRemaining));

                if (enemy.IsDefeated)
                {
                    score += enemy.DefeatBonus;
                    hudRenderer.ShowFeedback($"{enemy.Name} Defeated! +{enemy.DefeatBonus}", new Color(0.4f, 1f, 0.55f), 2f);

                    if (!gameEnded)
                    {
                        SpawnNextEnemy();
                    }
                }
            }

            return true;
        }

        private bool ClearStoneTileAt(int column, int row, int scoreValue)
        {
            if (!IsInside(column, row))
            {
                return false;
            }

            var tile = tiles[column, row];
            if (tile == null || !tile.IsStone)
            {
                return false;
            }

            tiles[column, row] = null;
            tile.PlayClearAndDestroy();
            score += scoreValue;
            specialBlocksClearedCount++;
            playLogger?.LogCombatEvent("special_block_clear", 2, 1,
                comboCount, feverActive, enemy?.CurrentHp ?? 0, enemy?.MaxHp ?? 0, Mathf.CeilToInt(timeRemaining));
            hudRenderer?.ShowFeedback($"Stone Clear! +{scoreValue}", new Color(0.5f, 0.5f, 0.5f), 1.2f);

            if (enemy != null && !enemy.IsDefeated)
            {
                var dealt = enemy.ApplyDamage(1);
                totalDamageDealt += dealt;
                effectRenderer?.PlayDamage(tile.transform.position, dealt, enemy.Wave > 0);
                hudRenderer?.PlayDamagePulse(enemy.Wave > 0);
                playLogger?.LogCombatEvent("enemy_damage", dealt, enemy.CurrentHp,
                    comboCount, feverActive, enemy.CurrentHp, enemy.MaxHp, Mathf.CeilToInt(timeRemaining));

                if (enemy.IsDefeated)
                {
                    score += enemy.DefeatBonus;
                    hudRenderer.ShowFeedback($"{enemy.Name} Defeated! +{enemy.DefeatBonus}", new Color(0.4f, 1f, 0.55f), 2f);

                    if (!gameEnded)
                    {
                        SpawnNextEnemy();
                    }
                }
            }

            return true;
        }

        private bool TryPlaceBomb(int chainLength, Vector2Int? preferredPosition, PokoTileType chainTileType, out BombType bombType)
        {
            bombType = BombType.Red;
            if (chainLength < 7)
            {
                return false;
            }

            Vector2Int pos;
            var usedFallback = false;
            if (preferredPosition.HasValue && IsInsideBoard(preferredPosition.Value.x, preferredPosition.Value.y)
                && tiles[preferredPosition.Value.x, preferredPosition.Value.y] == null)
            {
                pos = preferredPosition.Value;
            }
            else
            {
                var emptyPositions = new List<Vector2Int>();
                for (var column = 0; column < width; column++)
                {
                    for (var row = 0; row < height; row++)
                    {
                        if (IsInsideBoard(column, row) && tiles[column, row] == null)
                        {
                            emptyPositions.Add(new Vector2Int(column, row));
                        }
                    }
                }

                if (emptyPositions.Count == 0)
                {
                    return false;
                }

                pos = emptyPositions[Random.Range(0, emptyPositions.Count)];
                usedFallback = true;
            }

            var bombTile = CreateTile(pos.x, pos.y, chainTileType);
            var bt = chainLength >= 10 ? BombType.Blue : BombType.Red;
            bombType = bt;
            bombTile.ConfigureBomb(bt);
            tiles[pos.x, pos.y] = bombTile;
            bombTiles.Add(bombTile);
            playLogger?.LogCombatEvent("bomb_placed", bt == BombType.Rainbow ? 3 : bt == BombType.Blue ? 2 : 1, chainLength,
                comboCount, feverActive, enemy?.CurrentHp ?? 0, enemy?.MaxHp ?? 0, Mathf.CeilToInt(timeRemaining));
            if (usedFallback)
            {
                playLogger?.LogCombatEvent("bomb_fallback_placement", pos.x, pos.y,
                    comboCount, feverActive, enemy?.CurrentHp ?? 0, enemy?.MaxHp ?? 0, Mathf.CeilToInt(timeRemaining));
            }
            return true;
        }

        private void FillRainbowGauge(int blocksCleared)
        {
            var gaugeMultiplier = activeBalanceProfile != null
                ? Mathf.Max(0f, activeBalanceProfile.FeverGaugeMultiplier)
                : 1f;
            rainbowGauge += blocksCleared * RainbowGaugeFillPerBlock * gaugeMultiplier;
            if (rainbowGauge >= RainbowGaugeMax)
            {
                rainbowGauge = 0f;
                SpawnRainbowBomb();
            }
        }

        private void SpawnRainbowBomb()
        {
            for (var column = 0; column < width; column++)
            {
                for (var row = 0; row < height; row++)
                {
                    if (!IsInsideBoard(column, row) || tiles[column, row] != null)
                    {
                        continue;
                    }

                    var bombTile = CreateTile(column, row, PokoTileType.Red);
                    bombTile.ConfigureBomb(BombType.Rainbow);
                    tiles[column, row] = bombTile;
                    bombTiles.Add(bombTile);
                    hudRenderer.ShowFeedback("Rainbow Bomb Charged!", new Color(0.8f, 0.4f, 1f), 2f);
                    effectRenderer?.PlayBossSpawn("Rainbow");
                    return;
                }
            }
        }

        private void TickBombs()
        {
            for (var index = bombTiles.Count - 1; index >= 0; index--)
            {
                var bomb = bombTiles[index];
                if (bomb == null)
                {
                    bombTiles.RemoveAt(index);
                    continue;
                }

                if (bomb.TickBombTimer(Time.deltaTime))
                {
                    DetonateBomb(bomb);
                    return;
                }
            }
        }

        private void DetonateBomb(PokoTile bombTile)
        {
            if (bombTile == null || tiles == null)
            {
                return;
            }

            var column = -1;
            var row = -1;
            for (var c = 0; c < width && column == -1; c++)
            {
                for (var r = 0; r < height; r++)
                {
                    if (tiles[c, r] == bombTile)
                    {
                        column = c;
                        row = r;
                        break;
                    }
                }
            }

            if (column == -1)
            {
                bombTiles.Remove(bombTile);
                return;
            }

            bombTile.SetGridPosition(column, row, GridToWorld(column, row));
            var bombType = bombTile.BombType;
            var bombPosition = bombTile.transform.position;

            bombTiles.Remove(bombTile);
            tiles[column, row] = null;
            bombTile.PlayClearAndDestroy(0.14f);

            if (bombType == BombType.Rainbow)
            {
                DetonateRainbowBomb(bombPosition);
                return;
            }

            StartCoroutine(ResolveBombClear(column, row, bombType, bombPosition));
        }

        private IEnumerator ResolveBombClear(int column, int row, BombType bombType, Vector3 bombPosition)
        {
            resolvingVisualEffect = true;
            var affected = new List<Vector2Int>(BoardBomb.GetAffectedPositions(column, row, width, height, bombType));
            var targets = new List<PokoTile>();
            var targetPositions = new List<Vector3>();
            foreach (var pos in affected)
            {
                var tile = tiles[pos.x, pos.y];
                if (tile == null || tile.IsBomb)
                {
                    continue;
                }

                if (tile.IsFrozen)
                {
                    ClearFrozenTileAt(pos.x, pos.y);
                    continue;
                }

                if (tile.IsPetrified)
                {
                    continue;
                }

                if (tile.IsStone)
                {
                    if (ClearStoneTileAt(pos.x, pos.y, 50))
                    {
                        bombsClearedCount++;
                    }
                    continue;
                }

                if (!tile.IsLinkable)
                {
                    continue;
                }

                targets.Add(tile);
                targetPositions.Add(tile.transform.position);
            }

            var estimatedScore = targets.Count * 50;
            effectRenderer?.PlayBombPull(bombPosition, targetPositions, bombType, estimatedScore);
            if (bombType == BombType.Blue)
            {
                for (var index = 0; index < targets.Count; index++)
                {
                    targets[index]?.PlayPullToward(bombPosition, 0.38f, index * 0.01f);
                }
            }
            else if (bombType == BombType.Red)
            {
                for (var index = 0; index < targets.Count; index++)
                {
                    var distance = Vector3.Distance(targets[index].transform.position, bombPosition);
                    targets[index]?.PlayBlastAwayFrom(bombPosition, 0.26f, Mathf.Min(0.12f, distance * 0.025f));
                }
            }

            yield return new WaitForSeconds(bombType == BombType.Blue ? 0.46f : 0.38f);

            var clearedCount = 0;
            foreach (var tile in targets)
            {
                if (tile == null)
                {
                    continue;
                }

                var tileColumn = -1;
                var tileRow = -1;
                if (tiles != null)
                {
                    for (var c = 0; c < width && tileColumn == -1; c++)
                    {
                        for (var r = 0; r < height; r++)
                        {
                            if (tiles[c, r] == tile)
                            {
                                tileColumn = c;
                                tileRow = r;
                                break;
                            }
                        }
                    }
                }

                if (tileColumn == -1 || !tile.IsLinkable)
                {
                    if (tileColumn == -1)
                    {
                        for (var c = 0; c < width && tileColumn == -1; c++)
                        {
                            for (var r = 0; r < height; r++)
                            {
                                if (ReferenceEquals(tiles[c, r], tile))
                                {
                                    tileColumn = c;
                                    tileRow = r;
                                    break;
                                }
                            }
                        }

                        if (tileColumn != -1)
                        {
                            tiles[tileColumn, tileRow] = null;
                        }

                        tile.PlayClearAndDestroy();
                        clearedCount++;
                    }
                    continue;
                }

                bombsClearedCount++;
                clearedCount++;
                score += 50;
                tiles[tileColumn, tileRow] = null;
                tile.PlayClearAndDestroy();
            }

            ClearAdjacentFrozenTiles();
            FillRainbowGauge(clearedCount);
            ApplyWeaponDamage(clearedCount, bombPosition);
            CollapseAndRefill();
            hudRenderer.ShowFeedback($"{bombType} Bomb! +{clearedCount * 50}", bombType == BombType.Blue ? new Color(0.22f, 0.62f, 1f) : new Color(1f, 0.35f, 0.18f), 1.6f);
            playLogger?.LogCombatEvent("bomb_detonate", bombType == BombType.Red ? 1 : 2, clearedCount,
                comboCount, feverActive, enemy?.CurrentHp ?? 0, enemy?.MaxHp ?? 0, Mathf.CeilToInt(timeRemaining));
            hudRenderer?.RefreshHud(score, enemySpawnIndex, Mathf.CeilToInt(timeRemaining));
            RunDesignerAgent();
            EvaluateEndState();
            resolvingVisualEffect = false;
        }

        private void DetonateRainbowBomb(Vector3 bombPosition)
        {
            var typeCounts = new int[6];

            for (var col = 0; col < width; col++)
            {
                for (var r = 0; r < height; r++)
                {
                    var tile = tiles[col, r];
                    if (tile != null && tile.IsLinkable)
                    {
                        typeCounts[(int)tile.Type]++;
                    }
                }
            }

            var maxCount = 0;
            var targetType = PokoTileType.Red;
            for (var index = 0; index < typeCounts.Length; index++)
            {
                if (typeCounts[index] > maxCount)
                {
                    maxCount = typeCounts[index];
                    targetType = (PokoTileType)index;
                }
            }

            if (maxCount == 0)
            {
                CollapseAndRefill();
                return;
            }

            StartCoroutine(ResolveRainbowClear(targetType, bombPosition));
        }

        private IEnumerator ResolveRainbowClear(PokoTileType targetType, Vector3 bombPosition)
        {
            resolvingVisualEffect = true;
            var clearedPositions = new List<Vector3>();
            var targets = new List<PokoTile>();
            for (var col = 0; col < width; col++)
            {
                for (var r = 0; r < height; r++)
                {
                    var tile = tiles[col, r];
                    if (tile != null && tile.Type == targetType && tile.IsLinkable)
                    {
                        clearedPositions.Add(tile.transform.position);
                        targets.Add(tile);
                    }
                }
            }

            var estimatedScore = targets.Count * 50;
            effectRenderer?.PlayRainbowPreview(clearedPositions, AveragePosition(clearedPositions));
            effectRenderer?.PlayRainbowDetonation(bombPosition, clearedPositions, targetType, estimatedScore);
            var targetColor = targetType.ToColor();
            for (var index = 0; index < targets.Count; index++)
            {
                targets[index]?.PlayRainbowTargetPulse(targetColor, 0.5f, index * 0.004f);
            }

            yield return new WaitForSeconds(0.56f);

            var totalRemoved = 0;
            foreach (var tile in targets)
            {
                if (tile == null)
                {
                    continue;
                }

                var tileColumn = -1;
                var tileRow = -1;
                if (tiles != null)
                {
                    for (var c = 0; c < width && tileColumn == -1; c++)
                    {
                        for (var r = 0; r < height; r++)
                        {
                            if (tiles[c, r] == tile)
                            {
                                tileColumn = c;
                                tileRow = r;
                                break;
                            }
                        }
                    }
                }

                if (tileColumn == -1)
                {
                    for (var c = 0; c < width && tileColumn == -1; c++)
                    {
                        for (var r = 0; r < height; r++)
                        {
                            if (ReferenceEquals(tiles[c, r], tile))
                            {
                                tileColumn = c;
                                tileRow = r;
                                break;
                            }
                        }
                    }

                    if (tileColumn != -1)
                    {
                        tiles[tileColumn, tileRow] = null;
                    }

                    tile.PlayClearAndDestroy();
                    totalRemoved++;
                    continue;
                }

                tiles[tileColumn, tileRow] = null;
                tile.PlayClearAndDestroy();
                totalRemoved++;
            }

            var gainedScore = totalRemoved * 50;
            score += gainedScore;
            rainbowClearedCount++;
            rainbowTapsCount++;
            hudRenderer.ShowFeedback($"Rainbow! Cleared all {targetType} (+{gainedScore})", new Color(0.8f, 0.4f, 1f), 2.5f);
            effectRenderer?.PlayRainbowClear(clearedPositions, AveragePosition(clearedPositions), gainedScore);

            ClearAdjacentFrozenTiles();
            ApplyWeaponDamage(totalRemoved, bombPosition);
            CollapseAndRefill();
            playLogger?.LogCombatEvent("rainbow_cleared", totalRemoved, gainedScore,
                comboCount, feverActive, enemy?.CurrentHp ?? 0, enemy?.MaxHp ?? 0, Mathf.CeilToInt(timeRemaining));
            hudRenderer?.RefreshHud(score, enemySpawnIndex, Mathf.CeilToInt(timeRemaining));
            RunDesignerAgent();
            EvaluateEndState();
            resolvingVisualEffect = false;
        }

        private void TickEnemySkills()
        {
            if (gameEnded || enemy == null || enemy.IsDefeated || skillDatabase == null)
            {
                return;
            }

            enemySkillTimer -= Time.deltaTime;
            if (enemySkillTimer > 0f)
            {
                return;
            }

            var wave = enemy.Wave;
            if (wave <= 0)
            {
                enemySkillTimer = 8f;
                return;
            }

            var skills = skillDatabase.GetSkillsForWave(wave);
            if (skills.Count == 0)
            {
                enemySkillTimer = 10f;
                return;
            }

            var skill = skills[Random.Range(0, skills.Count)];
            var targetCount = skill.TargetCount;
            var cooldownMultiplier = activeBalanceProfile != null
                ? Mathf.Max(0.01f, activeBalanceProfile.SkillCooldownMultiplier)
                : 1f;
            skillCooldown = skill.CooldownSec * cooldownMultiplier;
            enemySkillTimer = skillCooldown;

            switch (skill.SkillType)
            {
                case EnemySkillType.Freeze:
                    ApplyFreezeSkill(targetCount);
                    break;
                case EnemySkillType.Stone:
                    ApplyStoneSkill(targetCount);
                    break;
                case EnemySkillType.Petrify:
                    ApplyPetrifySkill(targetCount);
                    break;
                case EnemySkillType.ColorSwap:
                    ApplyColorSwapSkill(targetCount);
                    break;
            }

            playLogger?.LogCombatEvent("enemy_skill", (int)skill.SkillType, targetCount,
                comboCount, feverActive, enemy.CurrentHp, enemy.MaxHp, Mathf.CeilToInt(timeRemaining));
        }

        private void ApplyFreezeSkill(int count)
        {
            var targets = GetRandomLinkableTiles(count);
            var shouldCancelDrag = SkillTargetsCurrentDrag(targets);
            hudRenderer?.PlaySkillPulse(new Color(0.55f, 0.85f, 1f, 0.95f));
            effectRenderer?.PlayBossSkill(EnemySkillType.Freeze, targets);
            foreach (var tile in targets)
            {
                tile.ConfigureSubtype(PokoBlockSubtype.Frozen);
            }

            CancelDragChangedBySkill(shouldCancelDrag);
            hudRenderer.ShowFeedback("Enemy froze tiles!", new Color(0.5f, 0.8f, 1f), 1.5f);
        }

        private void ApplyStoneSkill(int count)
        {
            var targets = GetRandomLinkableTiles(count);
            var shouldCancelDrag = SkillTargetsCurrentDrag(targets);
            hudRenderer?.PlaySkillPulse(new Color(0.62f, 0.62f, 0.62f, 0.95f));
            effectRenderer?.PlayBossSkill(EnemySkillType.Stone, targets);
            foreach (var tile in targets)
            {
                tile.ConfigureSubtype(PokoBlockSubtype.Stone, Random.Range(2, 4));
            }

            CancelDragChangedBySkill(shouldCancelDrag);
            hudRenderer.ShowFeedback("Enemy turned tiles to stone!", new Color(0.5f, 0.5f, 0.5f), 1.5f);
        }

        private void ApplyPetrifySkill(int count)
        {
            var targets = GetRandomLinkableTiles(count);
            var shouldCancelDrag = SkillTargetsCurrentDrag(targets);
            hudRenderer?.PlaySkillPulse(new Color(0.54f, 0.18f, 0.82f, 0.95f));
            effectRenderer?.PlayBossSkill(EnemySkillType.Petrify, targets);
            foreach (var tile in targets)
            {
                tile.ConfigureSubtype(PokoBlockSubtype.Petrified);
            }

            CancelDragChangedBySkill(shouldCancelDrag);
            hudRenderer.ShowFeedback("Enemy petrified tiles!", new Color(0.62f, 0.28f, 0.9f), 1.5f);
        }

        private void ApplyColorSwapSkill(int count)
        {
            var targets = GetRandomLinkableTiles(count);
            var shouldCancelDrag = SkillTargetsCurrentDrag(targets);
            hudRenderer?.PlaySkillPulse(new Color(1f, 0.45f, 0.95f, 0.95f));
            effectRenderer?.PlayBossSkill(EnemySkillType.ColorSwap, targets);
            foreach (var tile in targets)
            {
                var newType = RandomType();
                while (newType == tile.Type)
                {
                    newType = RandomType();
                }

                tile.SetTypeWithSprite(newType, tileSprites[(int)newType]);
            }

            CancelDragChangedBySkill(shouldCancelDrag);
            hudRenderer.ShowFeedback("Enemy swapped tile colors!", new Color(1f, 0.6f, 0.8f), 1.5f);
        }

        private bool SkillTargetsCurrentDrag(IReadOnlyList<PokoTile> targets)
        {
            if (!dragging || targets == null || targets.Count == 0)
            {
                return false;
            }

            for (var index = 0; index < targets.Count; index++)
            {
                var tile = targets[index];
                if (tile != null && (tile == currentPointerTile || selectedTiles.Contains(tile)))
                {
                    return true;
                }
            }

            return false;
        }

        private void CancelDragChangedBySkill(bool shouldCancel)
        {
            if (!shouldCancel)
            {
                return;
            }

            dragging = false;
            ClearSelection();
            currentPointerTile = null;
        }

        private List<PokoTile> GetRandomLinkableTiles(int count)
        {
            var linkable = new List<PokoTile>();
            for (var column = 0; column < width; column++)
            {
                for (var row = 0; row < height; row++)
                {
                    var tile = tiles[column, row];
                    if (tile != null && tile.IsLinkable)
                    {
                        linkable.Add(tile);
                    }
                }
            }

            var result = new List<PokoTile>();
            while (result.Count < count && linkable.Count > 0)
            {
                var index = Random.Range(0, linkable.Count);
                result.Add(linkable[index]);
                linkable.RemoveAt(index);
            }

            return result;
        }

        private void CollapseAndRefill()
        {
            PurgeDestroyedTiles();
            CompactColumns(false);

            while (ClearBottomPetrifiedBlocks())
            {
                CompactColumns(false);
            }

            CompactColumns(false);
            CompactColumns(true);
            VerifyBoardIntegrity();
            EnsurePlayableChain();
        }

        private void PurgeDestroyedTiles()
        {
            for (var column = 0; column < width; column++)
            {
                for (var row = 0; row < height; row++)
                {
                    var tile = tiles[column, row];
                    if (!ReferenceEquals(tile, null) && tile == null)
                    {
                        tiles[column, row] = null;
                    }
                }
            }
        }

        private void VerifyBoardIntegrity()
        {
            for (var column = 0; column < width; column++)
            {
                for (var row = 0; row < height; row++)
                {
                    var tile = tiles[column, row];
                    if (tile == null)
                    {
                        continue;
                    }

                    if (tile.Column != column || tile.Row != row)
                    {
                        tile.SetGridPosition(column, row, GridToWorld(column, row));
                    }
                }
            }
        }

        private void CompactColumns(bool refill)
        {
            for (var column = 0; column < width; column++)
            {
                var validRows = GetValidRowsInColumn(column);
                var segmentStart = 0;

                while (segmentStart < validRows.Count)
                {
                    var row = validRows[segmentStart];
                    var tile = tiles[column, row];
                    if (IsFixedObstacle(tile))
                    {
                        var targetPos = GridToWorld(column, row);
                        if (tile.Column != column || tile.Row != row)
                        {
                            tile.SetGridPosition(column, row, targetPos);
                        }

                        segmentStart++;
                        continue;
                    }

                    var segmentRows = new List<int>();
                    while (segmentStart < validRows.Count)
                    {
                        row = validRows[segmentStart];
                        tile = tiles[column, row];
                        if (IsFixedObstacle(tile))
                        {
                            break;
                        }

                        segmentRows.Add(row);
                        segmentStart++;
                    }

                    CompactColumnSegment(column, segmentRows, refill);
                }
            }
        }

        private void CompactColumnSegment(int column, IReadOnlyList<int> segmentRows, bool refill)
        {
            var movableTiles = new List<PokoTile>();
            foreach (var row in segmentRows)
            {
                var tile = tiles[column, row];
                if (tile != null && !tile.IsClearing)
                {
                    movableTiles.Add(tile);
                }

                tiles[column, row] = null;
            }

            var writeIndex = 0;
            foreach (var tile in movableTiles)
            {
                var writeRow = segmentRows[writeIndex];
                var targetPos = GridToWorld(column, writeRow);

                if (tile.Row != writeRow || tile.Column != column)
                {
                    var previousRow = tile.Row;
                    tile.SetGridPosition(column, writeRow, targetPos);
                    var dropHeight = Mathf.Max(0f, previousRow - writeRow) * spacing * HexGridUtility.VerticalSpacingRatio;
                    tile.AnimateDrop(targetPos, dropHeight, 0f);
                }
                else
                {
                    tile.SetGridPosition(column, writeRow, targetPos);
                }

                tiles[column, writeRow] = tile;
                writeIndex++;
            }

            if (!refill)
            {
                return;
            }

            for (var index = writeIndex; index < segmentRows.Count; index++)
            {
                var row = segmentRows[index];
                var dropTile = CreateTile(column, row, RandomType());
                tiles[column, row] = dropTile;
                var dropHeight = (height - row) * spacing * HexGridUtility.VerticalSpacingRatio * 0.3f;
                dropTile.AnimateDrop(GridToWorld(column, row), Mathf.Max(0.3f, dropHeight), index * 0.015f);
            }
        }

        private bool IsFixedObstacle(PokoTile tile)
        {
            return tile != null && (tile.IsFrozen || tile.IsStone);
        }

        private bool ClearBottomPetrifiedBlocks()
        {
            var clearedAny = false;
            Debug.Log("[ClearBottomPetrifiedBlocks] START");
            for (var column = 0; column < width; column++)
            {
                for (var row = 0; row < height; row++)
                {
                    if (!IsInsideBoard(column, row))
                    {
                        continue;
                    }

                    var tile = tiles[column, row];
                    Debug.Log($"[ClearBottomPetrifiedBlocks] col={column}, row={row}, tile={tile?.GetType().Name ?? "null"}, IsPetrified={tile?.IsPetrified}");
                    if (tile == null || !tile.IsPetrified)
                    {
                        continue;
                    }

                    var isAtBottom = true;
                    for (var belowRow = row + 1; belowRow < height; belowRow++)
                    {
                        if (!IsInsideBoard(column, belowRow))
                        {
                            continue;
                        }

                        var belowTile = tiles[column, belowRow];
                        if (belowTile != null && IsFixedObstacle(belowTile))
                        {
                            break;
                        }
                        else if (belowTile != null)
                        {
                            isAtBottom = false;
                            break;
                        }
                    }

                    Debug.Log($"[ClearBottomPetrifiedBlocks] col={column}, row={row}, isAtBottom={isAtBottom}");

                    if (!isAtBottom)
                    {
                        continue;
                    }

                    tiles[column, row] = null;
                    tile.PlayClearAndDestroy();
                    score += 40;
                    specialBlocksClearedCount++;
                    playLogger?.LogCombatEvent("special_block_clear", 3, 1,
                        comboCount, feverActive, enemy?.CurrentHp ?? 0, enemy?.MaxHp ?? 0, Mathf.CeilToInt(timeRemaining));
                    hudRenderer?.ShowFeedback("Petrified Clear! +40", new Color(0.62f, 0.28f, 0.9f), 1.2f);
                    clearedAny = true;
                    Debug.Log($"[ClearBottomPetrifiedBlocks] Cleared petrified at col={column}, row={row}");

                    if (enemy != null && !enemy.IsDefeated)
                    {
                        var dealt = enemy.ApplyDamage(1);
                        totalDamageDealt += dealt;
                        effectRenderer?.PlayDamage(tile.transform.position, dealt, enemy.Wave > 0);
                        hudRenderer?.PlayDamagePulse(enemy.Wave > 0);
                        playLogger?.LogCombatEvent("enemy_damage", dealt, enemy.CurrentHp,
                            comboCount, feverActive, enemy.CurrentHp, enemy.MaxHp, Mathf.CeilToInt(timeRemaining));

                        if (enemy.IsDefeated)
                        {
                            score += enemy.DefeatBonus;
                            hudRenderer.ShowFeedback($"{enemy.Name} Defeated! +{enemy.DefeatBonus}", new Color(0.4f, 1f, 0.55f), 2f);

                            if (!gameEnded)
                            {
                                SpawnNextEnemy();
                            }
                        }
                    }
                }
            }

            return clearedAny;
        }

        private List<int> GetValidRowsInColumn(int column)
        {
            var rows = new List<int>();
            for (var row = 0; row < height; row++)
            {
                if (IsInsideBoard(column, row))
                {
                    rows.Add(row);
                }
            }

            return rows;
        }

        private void ClearSelection()
        {
            PruneSelectedTiles();
            ClearLinkHints();

            foreach (var tile in selectedTiles)
            {
                if (tile != null)
                {
                    tile.SetSelected(false);
                }
            }

            selectedTiles.Clear();
            currentPointerTile = null;
            RefreshLine();
        }

        private void RefreshLinkHints()
        {
            PruneSelectedTiles();
            if (selectedTiles.Count == 0)
            {
                return;
            }

            var last = selectedTiles[^1];
            if (last == null || tiles == null)
            {
                return;
            }

            foreach (var next in HexGridUtility.GetNeighbors(last.Column, last.Row, width, height))
            {
                var candidate = tiles[next.x, next.y];
                if (candidate == null || !candidate.IsLinkable || !CanLinkTiles(last, candidate) || selectedTiles.Contains(candidate))
                {
                    continue;
                }

                candidate.SetLinkHint(true);
                hintedTiles.Add(candidate);
            }
        }

        private void RefreshChainRewardFeedback()
        {
            if (selectedTiles.Count >= 10)
            {
                hudRenderer.ShowFeedback("Blue Bomb Ready", new Color(0.35f, 0.65f, 1f), 0.7f);
            }
            else if (selectedTiles.Count >= 7)
            {
                hudRenderer.ShowFeedback("Red Bomb Ready", new Color(1f, 0.42f, 0.18f), 0.7f);
            }
        }

        private void ClearLinkHints()
        {
            for (var index = hintedTiles.Count - 1; index >= 0; index--)
            {
                var tile = hintedTiles[index];
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

            PruneSelectedTiles();
            var activeColor = feverActive ? new Color(1f, 0.55f, 0.08f) : Color.white;
            var lineWidth = selectedTiles.Count >= 3 ? 0.15f : 0.11f;
            linkLine.startColor = activeColor;
            linkLine.endColor = activeColor;
            linkLine.startWidth = lineWidth;
            linkLine.endWidth = lineWidth;
            linkLine.positionCount = selectedTiles.Count;
            for (var index = 0; index < selectedTiles.Count; index++)
            {
                var tile = selectedTiles[index];
                if (tile != null)
                {
                    linkLine.SetPosition(index, tile.transform.position + Vector3.back * 0.1f);
                }
            }
        }

        private void PruneSelectedTiles()
        {
            for (var index = selectedTiles.Count - 1; index >= 0; index--)
            {
                var tile = selectedTiles[index];
                if (tile == null || !tile.IsLinkable)
                {
                    selectedTiles.RemoveAt(index);
                }
            }
        }

        private static List<Vector3> CaptureTilePositions(IReadOnlyList<PokoTile> source)
        {
            var positions = new List<Vector3>();
            if (source == null)
            {
                return positions;
            }

            foreach (var tile in source)
            {
                if (tile != null && tile.IsLinkable)
                {
                    positions.Add(tile.transform.position);
                }
            }

            return positions;
        }

        private static Vector3 AveragePosition(IReadOnlyList<Vector3> positions)
        {
            if (positions == null || positions.Count == 0)
            {
                return Vector3.zero;
            }

            var sum = Vector3.zero;
            foreach (var position in positions)
            {
                sum += position;
            }

            return sum / positions.Count;
        }

        private void RunDesignerAgent()
        {
            if (designerAgent == null)
            {
                return;
            }

            var telemetry = new BoardTelemetry(
                width, height, tileTypes, CountPossibleChains(), FindLongestChain(),
                score, movesUsed,
                comboCount, feverActive, enemy?.CurrentHp ?? 0,
                totalDamageDealt, bombsClearedCount, specialBlocksClearedCount, rainbowClearedCount);
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
            if (startTile == null || !startTile.IsLinkable)
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

                foreach (var next in HexGridUtility.GetNeighbors(current.x, current.y, width, height))
                {
                    if (visited[next.x, next.y])
                    {
                        continue;
                    }

                    var nextTile = tiles[next.x, next.y];
                    if (nextTile == null || !CanLinkTiles(startTile, nextTile) || !nextTile.IsLinkable)
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
            if (tile == null || !tile.IsLinkable)
            {
                return 0;
            }

            var count = 0;

            foreach (var next in HexGridUtility.GetNeighbors(column, row, width, height))
            {
                var nextTile = tiles[next.x, next.y];
                if (nextTile != null && nextTile.IsLinkable && CanLinkTiles(tile, nextTile))
                {
                    count++;
                }
            }

            return count;
        }

        private void EnsurePlayableChain()
        {
            if (FindLongestChain() >= 7 || (TryCreateChain(7) && FindLongestChain() >= 7))
            {
                return;
            }

            if (FindLongestChain() >= 3)
            {
                return;
            }

            if (!TryFindThreeTilePath(out var first, out var second, out var third))
            {
                return;
            }

            var assistedType = first.Type;
            second.SetTypeWithSprite(assistedType, tileSprites[(int)assistedType]);
            third.SetTypeWithSprite(assistedType, tileSprites[(int)assistedType]);
        }

        private bool TryCreateChain(int targetLength)
        {
            var needed = targetLength - 1;

            for (var column = 0; column < width; column++)
            {
                for (var row = 0; row < height; row++)
                {
                    if (!IsInsideBoard(column, row))
                    {
                        continue;
                    }

                    var center = tiles[column, row];
                    if (center == null || !center.IsLinkable || center.IsBomb)
                    {
                        continue;
                    }

                    var visited = new bool[width, height];
                    var queue = new Queue<(Vector2Int Pos, int Dist)>();
                    queue.Enqueue((new Vector2Int(column, row), 0));
                    visited[column, row] = true;
                    var candidates = new List<PokoTile>();

                    while (queue.Count > 0)
                    {
                        var current = queue.Dequeue();

                        if (current.Dist > 0)
                        {
                            var tile = tiles[current.Pos.x, current.Pos.y];
                            if (tile != null && tile.IsLinkable && !tile.IsBomb)
                            {
                                candidates.Add(tile);
                            }
                        }

                        if (current.Dist >= 2)
                        {
                            continue;
                        }

                        foreach (var neighbor in HexGridUtility.GetNeighbors(current.Pos.x, current.Pos.y, width, height))
                        {
                            if (!visited[neighbor.x, neighbor.y])
                            {
                                visited[neighbor.x, neighbor.y] = true;
                                queue.Enqueue((neighbor, current.Dist + 1));
                            }
                        }
                    }

                    if (candidates.Count < needed)
                    {
                        continue;
                    }

                    var targetType = center.Type;
                    for (var index = 0; index < needed; index++)
                    {
                        candidates[index].SetTypeWithSprite(targetType, tileSprites[(int)targetType]);
                    }

                    if (EstimateFloodSize(column, row) >= targetLength)
                    {
                        return true;
                    }
                }
            }

            return false;
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

                    foreach (var secondPosition in HexGridUtility.GetNeighbors(column, row, width, height))
                    {
                        second = tiles[secondPosition.x, secondPosition.y];
                        if (second == null || !second.IsLinkable || !CanLinkTiles(first, second))
                        {
                            continue;
                        }

                        foreach (var thirdPosition in HexGridUtility.GetNeighbors(secondPosition.x, secondPosition.y, width, height))
                        {
                            if (thirdPosition.x == column && thirdPosition.y == row)
                            {
                                continue;
                            }

                            third = tiles[thirdPosition.x, thirdPosition.y];
                            if (third != null && third.IsLinkable && CanLinkTiles(second, third))
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

        private static bool CanLinkTiles(PokoTile a, PokoTile b)
        {
            return a.Type == b.Type;
        }

        private bool IsInside(int column, int row)
        {
            return IsInsideBoard(column, row);
        }

        private bool IsInsideBoard(int column, int row)
        {
            if (column < 0 || row < 0 || row >= height)
            {
                return false;
            }

            var maxCol = useHexGrid ? HexGridUtility.RowSize(row, width) : width;
            return column < maxCol;
        }

        private Vector3 GridToWorld(int column, int row)
        {
            return HexGridUtility.ToWorld(column, row, width, height, spacing, useHexGrid);
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
            if (levelConfig != null)
            {
                width = levelConfig.Width;
                height = levelConfig.Height;
                tileTypes = levelConfig.TileTypes;
                useHexGrid = levelConfig.UseHexGrid;
                moveLimit = levelConfig.MoveLimit;
                targetScore = levelConfig.TargetScore;
                regularEnemyHpOverride = levelConfig.RegularEnemyHp;
                bossHpOverride = levelConfig.BossHp;
                balanceProfileId = levelConfig.BalanceProfileId;
            }

            SelectActiveBalanceProfile();
            ApplyBossDefaults();
        }

        private void InitDataProviders()
        {
            if (enemyDatabase == null)
            {
                enemyDatabase = Resources.Load<PokoEnemyDatabase>("PokoEnemyDatabase");
            }

            if (skillDatabase == null)
            {
                skillDatabase = Resources.Load<PokoEnemySkillDatabase>("PokoEnemySkillDatabase");
            }

            if (regularEnemyDatabase == null)
            {
                regularEnemyDatabase = Resources.Load<PokoRegularEnemyDatabase>("PokoRegularEnemyDatabase");
            }

            if (balanceProfileDatabase == null)
            {
                balanceProfileDatabase = Resources.Load<PokoBalanceProfileDatabase>("PokoBalanceProfileDatabase");
            }

            if (skillDatabase != null)
            {
                Debug.Log($"[LineLinkerBoard] Loaded skills for {skillDatabase.GetAllSkills().Count} entries");
            }

            if (balanceProfileDatabase != null)
            {
                Debug.Log($"[LineLinkerBoard] Loaded {balanceProfileDatabase.GetAllProfiles().Count} balance profiles");
            }

            ResetEnemySkills();
        }

        private void SelectActiveBalanceProfile()
        {
            if (balanceProfileDatabase == null)
            {
                activeBalanceProfile = null;
                return;
            }

            activeBalanceProfile = balanceProfileDatabase.GetProfile(balanceProfileId);
            balanceProfileId = activeBalanceProfile?.ProfileId ?? "default";
            Debug.Log($"[LineLinkerBoard] Active balance profile: {balanceProfileId}");
        }

        private void ApplyBossDefaults()
        {
            enemySpawnIndex = 0;
            regularSpawnCount = 0;
            ShuffleBossCycle();
            bossCyclePtr = 0;
            bossGroupStep = 0;

            SpawnNextEnemy();
        }

        private void SpawnNextEnemy()
        {
            var multiplier = Mathf.Pow(WaveHpMultiplier, regularSpawnCount / 10);

            if (bossGroupStep == 2)
            {
                var wave = currentGroupBossWave;
                if (enemyDatabase != null)
                {
                    var bossData = enemyDatabase.GetWave(wave);
                    var hp = ResolveEnemyHp(bossData?.Hp ?? 100, true, multiplier);
                    enemy = new BoardEnemy(hp, bossData?.DefeatBonus ?? 500, bossData?.Name ?? $"Boss {wave}", wave, bossData?.PortraitPath ?? string.Empty);
                }
                else
                {
                    enemy = new BoardEnemy(ResolveEnemyHp(bossMaxHp, true, multiplier), bossDefeatBonus, bossName, wave);
                }

                hudRenderer.ShowFeedback($"BOSS - {enemy.Name}!", new Color(1f, 0.4f, 0.1f), 2f);
                effectRenderer?.PlayBossSpawn(enemy.Name);
                hudRenderer?.PlayBossPulse();
                hudRenderer?.ShowPortraitIntro(enemy);
                Debug.Log($"[LineLinkerBoard] Boss spawned: {enemy.Name} (HP {enemy.MaxHp}, spawn {enemySpawnIndex})");

                bossCyclePtr++;
                if (bossCyclePtr >= bossCycleOrder.Count)
                {
                    ShuffleBossCycle();
                    bossCyclePtr = 0;
                }
                bossGroupStep = 0;
            }
            else
            {
                if (bossGroupStep == 0)
                {
                    currentGroupBossWave = bossCycleOrder[bossCyclePtr];
                    currentGroupRegulars = PickRegularsForBoss(currentGroupBossWave);
                }

                var enemyId = currentGroupRegulars[bossGroupStep];
                if (regularEnemyDatabase != null)
                {
                    var regData = regularEnemyDatabase.GetEnemy(enemyId);
                    var hp = ResolveEnemyHp(regData.Hp, false, multiplier);
                    enemy = new BoardEnemy(hp, regData.ScoreBonus, regData.Name, 0, regData.PortraitPath ?? string.Empty);
                }
                else
                {
                    enemy = new BoardEnemy(30, 50, "Monster", 0);
                }

                Debug.Log($"[LineLinkerBoard] Enemy spawned: {enemy.Name} (HP {enemy.MaxHp}, spawn {enemySpawnIndex})");
                hudRenderer?.ShowPortraitIntro(enemy);
                bossGroupStep++;
                regularSpawnCount++;
            }

            enemySpawnIndex++;
            ResetEnemySkills();
        }

        private void ShuffleBossCycle()
        {
            bossCycleOrder.Clear();
            for (var i = 1; i <= 6; i++)
            {
                bossCycleOrder.Add(i);
            }

            for (var i = bossCycleOrder.Count - 1; i > 0; i--)
            {
                var j = Random.Range(0, i + 1);
                (bossCycleOrder[i], bossCycleOrder[j]) = (bossCycleOrder[j], bossCycleOrder[i]);
            }
        }

        private static int[] PickRegularsForBoss(int bossWave)
        {
            return BossRegularThemeMap.TryGetValue(bossWave, out var pool) && pool.Length >= 2
                ? new[] { pool[0], pool[1] }
                : new[] { 1, 2 };
        }

        private int ResolveEnemyHp(int baseHp, bool isBoss, float waveMultiplier)
        {
            var overrideHp = isBoss ? bossHpOverride : regularEnemyHpOverride;
            var sourceHp = overrideHp > 0 ? overrideHp : baseHp;
            var profileMultiplier = 1f;
            if (activeBalanceProfile != null)
            {
                profileMultiplier = isBoss ? activeBalanceProfile.BossHpMultiplier : activeBalanceProfile.RegularEnemyHpMultiplier;
            }

            return Mathf.Max(1, Mathf.CeilToInt(sourceHp * Mathf.Max(0.01f, waveMultiplier) * Mathf.Max(0.01f, profileMultiplier)));
        }

        private void ResetEnemySkills()
        {
            var cooldownMultiplier = activeBalanceProfile != null
                ? Mathf.Max(0.01f, activeBalanceProfile.SkillCooldownMultiplier)
                : 1f;
            enemySkillTimer = 8f * cooldownMultiplier;
            skillCooldown = 10f * cooldownMultiplier;
        }

        private void EvaluateEndState()
        {
            if (timeRemaining <= 0f)
            {
                gameEnded = true;
                var feedback = $"TIME UP - Score: {score}";
                var color = new Color(1f, 0.7f, 0.2f);
                hudRenderer.ShowFeedback(feedback, color, 4f);
                playLogger?.LogEndState("time_up", score, movesUsed, Mathf.CeilToInt(timeRemaining),
                    enemySpawnIndex, targetScore, moveLimit);
            }
        }

        private void RefreshTimedFeedback()
        {
            if (gameEnded)
            {
                return;
            }

            hudRenderer?.RefreshTimedFeedback();
        }

        private void RestartRound()
        {
            ClearSelection();
            DestroyBoardTiles();
            score = 0;
            movesUsed = 0;
            timeRemaining = roundTime;
            gameEnded = false;
            comboCount = 0;
            lastClearTime = 0f;
            feverActive = false;
            feverTimer = 0f;
            totalDamageDealt = 0;
            bombsClearedCount = 0;
            specialBlocksClearedCount = 0;
            rainbowClearedCount = 0;
            rainbowTapsCount = 0;
            rainbowGauge = 0f;
            bombTiles.Clear();
            enemySpawnIndex = 0;
            regularSpawnCount = 0;
            ShuffleBossCycle();
            bossCyclePtr = 0;
            bossGroupStep = 0;
            SpawnNextEnemy();
            ResetEnemySkills();
            tiles = new PokoTile[width, height];
            BuildBoard();
            playLogger = CreatePlayLogger();
            hudRenderer?.RefreshHud(score, enemySpawnIndex, Mathf.CeilToInt(timeRemaining));
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

        private void OnGUI()
        {
            hudRenderer?.OnGUI(score, Mathf.CeilToInt(timeRemaining), comboCount, feverActive,
                Mathf.CeilToInt(feverTimer), gameEnded, roundTime,
                enemy, enemySpawnIndex, rainbowGauge, RainbowGaugeMax);
        }

        private PlayLogger CreatePlayLogger()
        {
            var levelId = levelConfig == null ? "prototype" : levelConfig.LevelId;
            var levelPath = PlayLogger.BuildLevelPlayLogPath(levelId, playLogPath);
            return new PlayLogger(enablePlayLog, playLogPath, levelPath);
        }
    }
}
