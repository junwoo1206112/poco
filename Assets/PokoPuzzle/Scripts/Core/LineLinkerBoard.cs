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
        [SerializeField] private int height = 13;
        [SerializeField] private int tileTypes = 5;
        [SerializeField] private float spacing = 0.74f;
        [SerializeField] private bool useHexGrid = true;
        [SerializeField] private PokoTileVisualStyle tileVisualStyle = PokoTileVisualStyle.CircleInHex;
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
        private Sprite[] tileSprites;
        private IGameDesignerAgent designerAgent;

        [Header("Data Providers (ScriptableObject DI)")]
        [SerializeField] private PokoEnemyDatabase enemyDatabase;
        [SerializeField] private PokoEnemySkillDatabase skillDatabase;
        [SerializeField] private PokoRegularEnemyDatabase regularEnemyDatabase;
        [SerializeField] private PokoBalanceProfileDatabase balanceProfileDatabase;

        private float enemySkillTimer;
        private float skillCooldown = 10f;

        private int score;
        private int movesUsed;
        private float timeRemaining;
        private bool dragging;
        private bool gameEnded;
        private string agentHudText = "Designer analyzing board...";

        private int comboCount;
        private float lastClearTime;
        private bool feverActive;
        private float feverTimer;
        private const float ComboWindow = 2.5f;
        private const float FeverDuration = 6f;
        private const int FeverComboThreshold = 7;

        private PlayLogger playLogger;
        private BoardHudRenderer hudRenderer;
        private BoardEnemy enemy;
        private int bossWave = 1;
        private int bossMaxHp = 100;
        private int bossDefeatBonus = 500;
        private string bossName = "Monster";
        private int totalDamageDealt;
        private int bombsClearedCount;
        private int specialBlocksClearedCount;
        private int rainbowClearedCount;
        private int rainbowTapsCount;
        private int enemySpawnIndex;
        private const int BossInterval = 5;
        private const float WaveHpMultiplier = 1.25f;

        private readonly List<PokoTile> bombTiles = new();

        private void Awake()
        {
            if (boardCamera == null)
            {
                boardCamera = Camera.main;
            }

            designerAgent = new HeuristicGameDesignerAgent();
            InitDataProviders();
            ApplyLevelConfig();
            hudRenderer = new BoardHudRenderer(boardCamera, scoreText, agentText, feedbackText,
                useScreenHud, width, height, spacing, useHexGrid);
            hudRenderer.FramePlayCamera();
            hudRenderer.PrepareHud();
            hudRenderer.OnRestartRequested = RestartRound;
            tileSprites = TileSpriteGenerator.CreateTileSprites(tileVisualStyle);
            tiles = new PokoTile[width, height];

            if (enemy == null)
            {
                enemy = new BoardEnemy();
            }

            BuildBoard();
            timeRemaining = roundTime;
            playLogger = CreatePlayLogger();
            playLogger.WriteSessionStart(
                levelConfig == null ? "prototype" : levelConfig.LevelId,
                width, height, tileTypes, moveLimit, Mathf.CeilToInt(roundTime), targetScore, useHexGrid);
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
            if (tile != null && !tile.IsLinkable)
            {
                if (tile.IsBomb)
                {
                    DetonateBomb(tile);
                }
                else if (tile.IsFrozen)
                {
                    hudRenderer.ShowFeedback("Frozen - clear adjacent tile", new Color(0.6f, 0.8f, 1f));
                }
                else if (tile.IsStone)
                {
                    hudRenderer.ShowFeedback("Stone - falls to bottom", new Color(0.5f, 0.5f, 0.5f));
                }
                else
                {
                    dragging = false;
                    return;
                }
            }

            TryAddTileAtPointer(screenPosition);
        }

        private void ContinueDrag(Vector2 screenPosition)
        {
            TryAddTileAtPointer(screenPosition);
        }

        private void EndDrag()
        {
            if (selectedTiles.Count >= 3)
            {
                CommitChain();
            }
            else if (selectedTiles.Count == 1 && selectedTiles[0].IsRainbow)
            {
                var rainbowTile = selectedTiles[0];
                ClearSelection();
                ActivateRainbowTile(rainbowTile);
            }
            else if (selectedTiles.Count > 0)
            {
                hudRenderer.ShowFeedback("Need 3+ links", new Color(0.85f, 0.92f, 1f));
                ResetCombo();
                playLogger?.LogMove(false, selectedTiles.Count, 0, score, movesUsed, moveLimit,
                    Mathf.CeilToInt(timeRemaining), CountPossibleChains(), FindLongestChain());
                ClearSelection();
            }

            hudRenderer?.RefreshHud(score, enemySpawnIndex, Mathf.CeilToInt(timeRemaining));
        }

        private void CommitChain()
        {
            var chainLength = selectedTiles.Count;
            var comboMultiplier = feverActive ? 2 : Mathf.Max(1, comboCount);
            var gainedScore = ClearMatchedTiles() * comboMultiplier;

            ApplyComboIncrement();
            ApplyEnemyDamage(chainLength);
            TryPlaceBomb(chainLength);
            ClearAdjacentFrozenTiles();

            CollapseAndRefill();
            movesUsed++;
            hudRenderer.ShowFeedback($"Nice! +{gainedScore}", new Color(1f, 0.88f, 0.24f));
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

            if (!feverActive && comboCount >= FeverComboThreshold)
            {
                feverActive = true;
                feverTimer = FeverDuration;
                hudRenderer.ShowFeedback("FEVER!", new Color(1f, 0.4f, 0.1f), 2f);
                playLogger?.LogFeverEvent("start", comboCount, Mathf.CeilToInt(timeRemaining));
            }
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

        private void ApplyEnemyDamage(int chainLength)
        {
            if (enemy == null || enemy.IsDefeated)
            {
                return;
            }

            var damage = chainLength * 10;
            var dealt = enemy.ApplyDamage(damage);
            totalDamageDealt += dealt;
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
            var tile = TileAtPointer(screenPosition);
            if (tile == null || !tile.IsBomb)
            {
                return false;
            }

            DetonateBomb(tile);
            return true;
        }

        private void ActivateRainbowTile(PokoTile rainbowTile)
        {
            if (rainbowTile == null || !rainbowTile.IsRainbow)
            {
                return;
            }

            var typeCounts = new int[6];
            var totalRemoved = 0;

            for (var column = 0; column < width; column++)
            {
                for (var row = 0; row < height; row++)
                {
                    var tile = tiles[column, row];
                    if (tile != null && tile.IsLinkable && !tile.IsRainbow)
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
                return;
            }

            tiles[rainbowTile.Column, rainbowTile.Row] = null;
            Destroy(rainbowTile.gameObject);
            rainbowClearedCount++;
            totalRemoved++;

            for (var column = 0; column < width; column++)
            {
                for (var row = 0; row < height; row++)
                {
                    var tile = tiles[column, row];
                    if (tile != null && tile.Type == targetType && tile.IsLinkable && !tile.IsRainbow)
                    {
                        tiles[column, row] = null;
                        Destroy(tile.gameObject);
                        totalRemoved++;
                    }
                }
            }

            var gainedScore = totalRemoved * 50;
            score += gainedScore;
            rainbowTapsCount++;
            hudRenderer.ShowFeedback($"Rainbow! Cleared all {targetType} (+{gainedScore})", new Color(0.8f, 0.4f, 1f), 2.5f);

            CollapseAndRefill();
            playLogger?.LogCombatEvent("rainbow_tap", totalRemoved, gainedScore,
                comboCount, feverActive, enemy?.CurrentHp ?? 0, enemy?.MaxHp ?? 0, Mathf.CeilToInt(timeRemaining));
            playLogger?.LogCombatEvent("rainbow_cleared", 1, gainedScore,
                comboCount, feverActive, enemy?.CurrentHp ?? 0, enemy?.MaxHp ?? 0, Mathf.CeilToInt(timeRemaining));
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
            var hasRainbow = false;
            foreach (var tile in selectedTiles)
            {
                if (tile == null)
                {
                    continue;
                }

                if (tile.IsRainbow)
                {
                    hasRainbow = true;
                    break;
                }
            }

            var gainedScore = chainLength * chainLength * 10;
            if (hasRainbow)
            {
                gainedScore = Mathf.RoundToInt(gainedScore * 1.5f);
            }

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

                    foreach (var neighborPos in HexGridUtility.GetNeighbors(tile.Column, tile.Row, height))
                    {
                        var neighbor = tiles[neighborPos.x, neighborPos.y];
                        if (neighbor != null && !tilesToDestroy.Contains(neighbor) && !neighbor.IsBomb)
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

                var tileColumn = tile.Column;
                var tileRow = tile.Row;

                if (tile.IsClock)
                {
                    timeRemaining += 2f;
                    specialBlocksClearedCount++;
                }
                else if (tile.IsFrozen || tile.IsStone)
                {
                    specialBlocksClearedCount++;
                }

                if (tile.IsRainbow)
                {
                    rainbowClearedCount++;
                }

                tiles[tileColumn, tileRow] = null;
                Destroy(tile.gameObject);

                foreach (var frozenPos in HexGridUtility.GetNeighbors(tileColumn, tileRow, height))
                {
                    var frozenTile = tiles[frozenPos.x, frozenPos.y];
                    if (frozenTile != null && frozenTile.IsFrozen)
                    {
                        specialBlocksClearedCount++;
                        tiles[frozenPos.x, frozenPos.y] = null;
                        Destroy(frozenTile.gameObject);
                    }
                }
            }

            score += gainedScore;
            if (hasRainbow)
            {
                playLogger?.LogCombatEvent("rainbow_cleared", rainbowClearedCount, gainedScore,
                    comboCount, feverActive, enemy?.CurrentHp ?? 0, enemy?.MaxHp ?? 0, Mathf.CeilToInt(timeRemaining));
            }

            return gainedScore;
        }

        private void ClearAdjacentFrozenTiles()
        {
            for (var column = 0; column < width; column++)
            {
                for (var row = 0; row < height; row++)
                {
                    var tile = tiles[column, row];
                    if (tile == null || !tile.IsFrozen)
                    {
                        continue;
                    }

                    foreach (var neighborPos in HexGridUtility.GetNeighbors(column, row, height))
                    {
                        if (tiles[neighborPos.x, neighborPos.y] == null)
                        {
                            specialBlocksClearedCount++;
                            tiles[column, row] = null;
                            Destroy(tile.gameObject);
                            break;
                        }
                    }
                }
            }
        }

        private void TryPlaceBomb(int chainLength)
        {
            if (chainLength < 7)
            {
                return;
            }

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
                return;
            }

            var pos = emptyPositions[Random.Range(0, emptyPositions.Count)];
            var bombTile = CreateTile(pos.x, pos.y, PokoTileType.Red);
            var bt = chainLength >= 10 ? BombType.Blue : BombType.Red;
            bombTile.ConfigureBomb(bt);
            tiles[pos.x, pos.y] = bombTile;
            bombTiles.Add(bombTile);
            playLogger?.LogCombatEvent("bomb_placed", bt == BombType.Blue ? 2 : 1, chainLength,
                comboCount, feverActive, enemy?.CurrentHp ?? 0, enemy?.MaxHp ?? 0, Mathf.CeilToInt(timeRemaining));
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
                }
            }
        }

        private void DetonateBomb(PokoTile bombTile)
        {
            if (bombTile == null || tiles[bombTile.Column, bombTile.Row] != bombTile)
            {
                return;
            }

            var column = bombTile.Column;
            var row = bombTile.Row;
            var bombType = bombTile.BombType;

            bombTiles.Remove(bombTile);
            tiles[column, row] = null;
            Destroy(bombTile.gameObject);

            var affected = new List<Vector2Int>(BoardBomb.GetAffectedPositions(column, row, height, bombType));
            foreach (var pos in affected)
            {
                var tile = tiles[pos.x, pos.y];
                if (tile == null || tile.IsBomb)
                {
                    continue;
                }

                if (tile.IsClock)
                {
                    timeRemaining += 2f;
                }

                if (tile.IsStone || tile.IsFrozen || tile.IsClock)
                {
                    specialBlocksClearedCount++;
                }

                bombsClearedCount++;
                score += 50;
                tiles[pos.x, pos.y] = null;
                Destroy(tile.gameObject);
            }

            ClearAdjacentFrozenTiles();
            CollapseAndRefill();
            playLogger?.LogCombatEvent("bomb_detonate", bombType == BombType.Red ? 1 : 2, affected.Count,
                comboCount, feverActive, enemy?.CurrentHp ?? 0, enemy?.MaxHp ?? 0, Mathf.CeilToInt(timeRemaining));
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
            var skills = skillDatabase.GetSkillsForWave(wave);
            if (skills.Count == 0)
            {
                enemySkillTimer = 10f;
                return;
            }

            var skill = skills[Random.Range(0, skills.Count)];
            var targetCount = skill.TargetCount;
            skillCooldown = skill.CooldownSec;
            enemySkillTimer = skillCooldown;

            switch (skill.SkillType)
            {
                case EnemySkillType.Freeze:
                    ApplyFreezeSkill(targetCount);
                    break;
                case EnemySkillType.Stone:
                    ApplyStoneSkill(targetCount);
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
            foreach (var tile in targets)
            {
                tile.ConfigureSubtype(PokoBlockSubtype.Frozen);
            }

            hudRenderer.ShowFeedback("Enemy froze tiles!", new Color(0.5f, 0.8f, 1f), 1.5f);
        }

        private void ApplyStoneSkill(int count)
        {
            var targets = GetRandomLinkableTiles(count);
            foreach (var tile in targets)
            {
                tile.ConfigureSubtype(PokoBlockSubtype.Stone);
            }

            hudRenderer.ShowFeedback("Enemy turned tiles to stone!", new Color(0.5f, 0.5f, 0.5f), 1.5f);
        }

        private void ApplyColorSwapSkill(int count)
        {
            var targets = GetRandomLinkableTiles(count);
            foreach (var tile in targets)
            {
                var newType = RandomType();
                while (newType == tile.Type)
                {
                    newType = RandomType();
                }

                tile.SetTypeWithSprite(newType, tileSprites[(int)newType]);
            }

            hudRenderer.ShowFeedback("Enemy swapped tile colors!", new Color(1f, 0.6f, 0.8f), 1.5f);
        }

        private List<PokoTile> GetRandomLinkableTiles(int count)
        {
            var linkable = new List<PokoTile>();
            for (var column = 0; column < width; column++)
            {
                for (var row = 0; row < height; row++)
                {
                    var tile = tiles[column, row];
                    if (tile != null && tile.IsLinkable && !tile.IsRainbow)
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
            for (var column = 0; column < width; column++)
            {
                var validRows = GetValidRowsInColumn(column);
                var writeIndex = 0;

                foreach (var row in validRows)
                {
                    var tile = tiles[column, row];
                    if (tile == null)
                    {
                        continue;
                    }

                    var writeRow = validRows[writeIndex];
                    tiles[column, writeRow] = tile;
                    tile.SetGridPosition(column, writeRow, GridToWorld(column, writeRow));

                    if (writeRow != row)
                    {
                        tiles[column, row] = null;
                    }

                    writeIndex++;
                }

                for (var index = writeIndex; index < validRows.Count; index++)
                {
                    var row = validRows[index];
                    tiles[column, row] = CreateTile(column, row, RandomType());
                }
            }

            for (var column = 0; column < width; column++)
            {
                var bottom = tiles[column, 0];
                if (bottom != null && bottom.IsStone)
                {
                    tiles[column, 0] = null;
                    Destroy(bottom.gameObject);
                    specialBlocksClearedCount++;
                }
            }

            EnsurePlayableChain();
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
            if (last == null || tiles == null)
            {
                return;
            }

            foreach (var next in HexGridUtility.GetNeighbors(last.Column, last.Row, height))
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

                foreach (var next in HexGridUtility.GetNeighbors(current.x, current.y, height))
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

            foreach (var next in HexGridUtility.GetNeighbors(column, row, height))
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
            if (FindLongestChain() >= 3 || !TryFindThreeTilePath(out var first, out var second, out var third))
            {
                return;
            }

            var assistedType = first.Type;
            second.SetTypeWithSprite(assistedType, tileSprites[(int)assistedType]);
            third.SetTypeWithSprite(assistedType, tileSprites[(int)assistedType]);
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
                        if (second == null || !second.IsLinkable || !CanLinkTiles(first, second))
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
            return a.IsRainbow || b.IsRainbow || a.Type == b.Type;
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

        private static PokoBlockSubtype RandomSubtype(float probability)
        {
            if (Random.value > probability)
            {
                return PokoBlockSubtype.None;
            }

            var roll = Random.Range(0, 3);
            return roll switch
            {
                0 => PokoBlockSubtype.Frozen,
                1 => PokoBlockSubtype.Stone,
                _ => PokoBlockSubtype.Clock
            };
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
            }

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

        private void ApplyBossDefaults()
        {
            bossWave = 1;
            enemySpawnIndex = 0;

            if (enemyDatabase != null)
            {
                var bossData = enemyDatabase.GetWave(bossWave);
                bossMaxHp = bossData?.Hp ?? 100;
                bossDefeatBonus = bossData?.DefeatBonus ?? 500;
                bossName = bossData?.Name ?? $"Boss {bossWave}";
            }

            SpawnNextEnemy();
        }

        private void SpawnNextEnemy()
        {
            var isBoss = enemySpawnIndex > 0 && enemySpawnIndex % BossInterval == 0;
            var cycleIndex = enemySpawnIndex / 10;
            var enemyId = (enemySpawnIndex % 10) + 1;
            var multiplier = Mathf.Pow(WaveHpMultiplier, cycleIndex);

            if (isBoss)
            {
                var wave = (enemySpawnIndex / BossInterval) % 5 + 1;
                if (enemyDatabase != null)
                {
                    var bossData = enemyDatabase.GetWave(wave);
                    var hp = bossData != null ? Mathf.CeilToInt(bossData.Hp * multiplier) : 100;
                    enemy = new BoardEnemy(hp, bossData?.DefeatBonus ?? 500, bossData?.Name ?? $"Boss {wave}", wave);
                }
                else
                {
                    enemy = new BoardEnemy(Mathf.CeilToInt(bossMaxHp * multiplier), bossDefeatBonus, bossName, bossWave);
                }

                hudRenderer.ShowFeedback($"BOSS - {enemy.Name}!", new Color(1f, 0.4f, 0.1f), 2f);
                Debug.Log($"[LineLinkerBoard] Boss spawned: {enemy.Name} (HP {enemy.MaxHp}, spawn {enemySpawnIndex})");
            }
            else
            {
                if (regularEnemyDatabase != null)
                {
                    var regData = regularEnemyDatabase.GetEnemy(enemyId);
                    var hp = Mathf.CeilToInt(regData.Hp * multiplier);
                    enemy = new BoardEnemy(hp, regData.ScoreBonus, regData.Name, 0);
                }
                else
                {
                    enemy = new BoardEnemy(30, 50, "Monster", 0);
                }

                Debug.Log($"[LineLinkerBoard] Enemy spawned: {enemy.Name} (HP {enemy.MaxHp}, spawn {enemySpawnIndex})");
            }

            enemySpawnIndex++;
            ResetEnemySkills();
        }

        private void ResetEnemySkills()
        {
            enemySkillTimer = 8f;
            skillCooldown = 10f;
        }

        private void EvaluateEndState()
        {
            if (timeRemaining <= 0f)
            {
                gameEnded = true;
                hudRenderer.ShowFeedback($"GAME OVER - Score: {score}", new Color(1f, 0.7f, 0.2f), 4f);
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
            bombTiles.Clear();
            enemySpawnIndex = 0;
            enemy = new BoardEnemy(30, 50, "Monster", 0);
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
                Mathf.CeilToInt(feverTimer), agentHudText, gameEnded, targetScore,
                enemy, enemySpawnIndex);
        }

        private PlayLogger CreatePlayLogger()
        {
            var levelId = levelConfig == null ? "prototype" : levelConfig.LevelId;
            var levelPath = PlayLogger.BuildLevelPlayLogPath(levelId, playLogPath);
            return new PlayLogger(enablePlayLog, playLogPath, levelPath);
        }
    }
}
