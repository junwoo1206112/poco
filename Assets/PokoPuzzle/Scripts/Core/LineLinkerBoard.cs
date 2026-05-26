using System.Collections.Generic;
using System.IO;
using System.Text;
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
        private float feedbackClearTime;
        private string levelPlayLogPath;
        private string agentHudText = "Designer analyzing board...";
        private string feedbackMessage = string.Empty;
        private Color feedbackColor = Color.white;

        private int comboCount;
        private float lastClearTime;
        private bool feverActive;
        private float feverTimer;
        private const float ComboWindow = 2.5f;
        private const float FeverDuration = 6f;
        private const int FeverComboThreshold = 7;

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
            FramePlayCamera();
            PrepareHud();
            tileSprites = CreateTileSprites(tileVisualStyle);
            tiles = new PokoTile[width, height];

            if (enemy == null)
            {
                enemy = new BoardEnemy();
            }

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

            timeRemaining -= Time.deltaTime;
            if (timeRemaining <= 0f)
            {
                timeRemaining = 0f;
                gameEnded = true;
                EvaluateEndState();
                RefreshHud();
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

                if (TryActivateRainbowAtPointer(screenPosition))
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
                    ShowFeedback("Frozen - clear adjacent tile", new Color(0.6f, 0.8f, 1f));
                }
                else if (tile.IsStone)
                {
                    ShowFeedback("Stone - falls to bottom", new Color(0.5f, 0.5f, 0.5f));
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
            else if (selectedTiles.Count > 0)
            {
                ShowFeedback("Need 3+ links", new Color(0.85f, 0.92f, 1f));
                ResetCombo();
                LogMove(false, selectedTiles.Count, 0);
            }

            ClearSelection();
            RefreshHud();
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
            ShowFeedback($"Nice! +{gainedScore}", new Color(1f, 0.88f, 0.24f));
            RunDesignerAgent();
            LogMove(true, chainLength, gainedScore);
            EvaluateEndState();
            selectedTiles.Clear();
            hintedTiles.Clear();
            RefreshLine();
            RefreshHud();
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
                ShowFeedback("FEVER!", new Color(1f, 0.4f, 0.1f), 2f);
                LogFeverEvent("start");
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
                LogFeverEvent("end");
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
            LogCombatEvent("enemy_damage", dealt, enemy.CurrentHp);

            if (enemy.IsDefeated)
            {
                score += enemy.DefeatBonus;
                ShowFeedback($"{enemy.Name} Defeated! +{enemy.DefeatBonus}", new Color(0.4f, 1f, 0.55f), 2f);

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

            if (selectedTiles.Contains(tile) || tile.Type != last.Type || !AreAdjacent(last, tile))
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

        private bool TryActivateRainbowAtPointer(Vector2 screenPosition)
        {
            var tile = TileAtPointer(screenPosition);
            if (tile == null || !tile.IsRainbow)
            {
                return false;
            }

            var typeCounts = new int[6];
            var totalRemoved = 0;

            for (var column = 0; column < width; column++)
            {
                for (var row = 0; row < height; row++)
                {
                    var t = tiles[column, row];
                    if (t != null && t.IsLinkable && !t.IsRainbow)
                    {
                        typeCounts[(int)t.Type]++;
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
                return false;
            }

            for (var column = 0; column < width; column++)
            {
                for (var row = 0; row < height; row++)
                {
                    var t = tiles[column, row];
                    if (t != null && t.Type == targetType && t.IsLinkable && !t.IsRainbow)
                    {
                        tiles[column, row] = null;
                        Destroy(t.gameObject);
                        totalRemoved++;
                    }
                }
            }

            var gainedScore = totalRemoved * 50;
            score += gainedScore;
            rainbowTapsCount++;
            ShowFeedback($"Rainbow! Cleared all {targetType} (+{gainedScore})", new Color(0.8f, 0.4f, 1f), 2.5f);

            CollapseAndRefill();
            LogCombatEvent("rainbow_tap", totalRemoved, gainedScore);
            return true;
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
            var tilesToDestroy = new List<PokoTile>(selectedTiles);

            if (feverActive)
            {
                var cascadeNeighbors = new HashSet<PokoTile>();
                foreach (var tile in selectedTiles)
                {
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

                tiles[tile.Column, tile.Row] = null;
                Destroy(tile.gameObject);

                foreach (var frozenPos in HexGridUtility.GetNeighbors(tile.Column, tile.Row, height))
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
            LogCombatEvent("bomb_placed", bt == BombType.Blue ? 2 : 1, chainLength);
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
            LogCombatEvent("bomb_detonate", bombType == BombType.Red ? 1 : 2, affected.Count);
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

            LogCombatEvent("enemy_skill", (int)skill.SkillType, targetCount);
        }

        private void ApplyFreezeSkill(int count)
        {
            var targets = GetRandomLinkableTiles(count);
            foreach (var tile in targets)
            {
                tile.ConfigureSubtype(PokoBlockSubtype.Frozen);
            }

            ShowFeedback("Enemy froze tiles!", new Color(0.5f, 0.8f, 1f), 1.5f);
        }

        private void ApplyStoneSkill(int count)
        {
            var targets = GetRandomLinkableTiles(count);
            foreach (var tile in targets)
            {
                tile.ConfigureSubtype(PokoBlockSubtype.Stone);
            }

            ShowFeedback("Enemy turned tiles to stone!", new Color(0.5f, 0.5f, 0.5f), 1.5f);
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

            ShowFeedback("Enemy swapped tile colors!", new Color(1f, 0.6f, 0.8f), 1.5f);
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
            foreach (var next in HexGridUtility.GetNeighbors(last.Column, last.Row, height))
            {
                var candidate = tiles[next.x, next.y];
                if (candidate == null || !candidate.IsLinkable || candidate.Type != last.Type || selectedTiles.Contains(candidate))
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
                scoreText.text = $"Score {score}\nEnemy {enemySpawnIndex + 1}  {Mathf.CeilToInt(timeRemaining)}s";
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
                    if (nextTile == null || nextTile.Type != startTile.Type || !nextTile.IsLinkable)
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
                if (nextTile != null && nextTile.IsLinkable && nextTile.Type == tile.Type)
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
                        if (second == null || !second.IsLinkable || second.Type != first.Type)
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
                            if (third != null && third.IsLinkable && third.Type == first.Type)
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

                ShowFeedback($"BOSS - {enemy.Name}!", new Color(1f, 0.4f, 0.1f), 2f);
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
                ShowFeedback($"GAME OVER - Score: {score}", new Color(1f, 0.7f, 0.2f), 4f);
                LogEndState("time_up");
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

        private void OnGUI()
        {
            if (!useScreenHud)
            {
                return;
            }

            var scale = Mathf.Clamp(Screen.height / 720f, 0.82f, 1.12f);
            var margin = Mathf.RoundToInt(16f * scale);
            var scoreWidth = Mathf.Min(Screen.width - margin * 2f, 400f * scale);
            var scorePanel = new Rect((Screen.width - scoreWidth) * 0.5f, margin, scoreWidth, 82f * scale);
            var timeDisplay = Mathf.CeilToInt(timeRemaining);
            var comboText = feverActive ? $"FEVER! ({Mathf.CeilToInt(feverTimer)}s)" : comboCount > 0 ? $"Combo x{comboCount}" : "";

            DrawHudPanel(scorePanel);
            GUI.Label(
                new Rect(scorePanel.x + 12f * scale, scorePanel.y + 5f * scale, scorePanel.width - 24f * scale, 26f * scale),
                $"Score {score}" + (string.IsNullOrEmpty(comboText) ? "" : $"  |  {comboText}"),
                CreateHudStyle(feverActive ? 24f : 20f, FontStyle.Bold, feverActive ? new Color(1f, 0.4f, 0.1f) : Color.white, scale, TextAnchor.UpperCenter));
            GUI.Label(
                new Rect(scorePanel.x + 12f * scale, scorePanel.y + 32f * scale, scorePanel.width - 24f * scale, 22f * scale),
                $"Enemy {enemySpawnIndex}  |  Time {timeDisplay}s",
                CreateHudStyle(15f, FontStyle.Normal, new Color(0.86f, 0.93f, 1f), scale, TextAnchor.UpperCenter));

            if (enemy != null)
            {
                var eBarWidth = Mathf.Min(Screen.width - margin * 2f, 260f * scale);
                var eBarHeight = 18f * scale;
                var eBarX = (Screen.width - eBarWidth) * 0.5f;
                var eBarY = scorePanel.yMax + 6f * scale;

                DrawHudPanel(new Rect(eBarX - 4f * scale, eBarY - 2f * scale, eBarWidth + 8f * scale, eBarHeight + 4f * scale));
                var eBarColor = enemy.IsDefeated ? new Color(0.5f, 0.5f, 0.5f, 0.5f) : GUI.color;
                GUI.color = eBarColor;
                GUI.Box(new Rect(eBarX, eBarY, eBarWidth * enemy.HpRatio, eBarHeight), GUIContent.none);
                GUI.color = Color.white;
                GUI.Label(
                    new Rect(eBarX, eBarY, eBarWidth, eBarHeight),
                    enemy.IsDefeated ? $"Defeated!" : $"{enemy.Name}  {enemy.CurrentHp}/{enemy.MaxHp}",
                    CreateHudStyle(13f, FontStyle.Bold, Color.white, scale, TextAnchor.MiddleCenter));
            }

            if (!string.IsNullOrWhiteSpace(agentHudText))
            {
                var agentWidth = Mathf.Min(Screen.width - margin * 2f, 390f * scale);
                var agentPanel = new Rect(
                    (Screen.width - agentWidth) * 0.5f,
                    Screen.height - 80f * scale,
                    agentWidth,
                    68f * scale);

                DrawHudPanel(agentPanel);
                GUI.Label(
                    new Rect(agentPanel.x + 12f * scale, agentPanel.y + 8f * scale, agentPanel.width - 24f * scale, agentPanel.height - 12f * scale),
                    agentHudText,
                    CreateHudStyle(13f, FontStyle.Normal, new Color(0.65f, 0.92f, 1f), scale, TextAnchor.UpperLeft));
            }

            if (!string.IsNullOrWhiteSpace(feedbackMessage) && feedbackClearTime > Time.time)
            {
                var feedbackWidth = Mathf.Min(Screen.width - margin * 2f, 340f * scale);
                var feedbackPanel = new Rect(
                    (Screen.width - feedbackWidth) * 0.5f,
                    scorePanel.yMax + (enemy != null ? 30f * scale : 6f * scale),
                    feedbackWidth,
                    32f * scale);

                DrawHudPanel(feedbackPanel);
                GUI.Label(
                    new Rect(feedbackPanel.x + 8f * scale, feedbackPanel.y + 4f * scale, feedbackPanel.width - 16f * scale, feedbackPanel.height - 8f * scale),
                    feedbackMessage,
                    CreateHudStyle(15f, FontStyle.Bold, feedbackColor, scale, TextAnchor.MiddleCenter));
            }

            if (gameEnded)
            {
                DrawEndPanel(scale);
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
                $"\"timeLimit\":{Mathf.CeilToInt(roundTime)}," +
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
                $"\"enemyCount\":{enemySpawnIndex}," +
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

        private void LogFeverEvent(string state)
        {
            if (!enablePlayLog)
            {
                return;
            }

            AppendPlayLog(
                "{" +
                "\"event\":\"fever\"," +
                $"\"state\":\"{EscapeJson(state)}\"," +
                $"\"combo\":{comboCount}," +
                $"\"timeLeft\":{Mathf.CeilToInt(timeRemaining)}" +
                "}");
        }

        private void LogCombatEvent(string combatEvent, int value1, int value2)
        {
            if (!enablePlayLog)
            {
                return;
            }

            AppendPlayLog(
                "{" +
                "\"event\":\"combat\"," +
                $"\"combatEvent\":\"{EscapeJson(combatEvent)}\"," +
                $"\"value1\":{value1}," +
                $"\"value2\":{value2}," +
                $"\"combo\":{comboCount}," +
                $"\"feverActive\":{JsonBool(feverActive)}," +
                $"\"enemyHp\":{(enemy?.CurrentHp ?? 0)}," +
                $"\"enemyMaxHp\":{(enemy?.MaxHp ?? 0)}," +
                $"\"timeLeft\":{Mathf.CeilToInt(timeRemaining)}" +
                "}");
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

        private static Sprite[] CreateTileSprites(PokoTileVisualStyle visualStyle)
        {
            var count = System.Enum.GetValues(typeof(PokoTileType)).Length;
            var sprites = new Sprite[count];
            for (var index = 0; index < count; index++)
            {
                sprites[index] = CreateShapeSprite((PokoTileType)index, visualStyle);
            }

            return sprites;
        }

        private static Sprite CreateShapeSprite(PokoTileType type, PokoTileVisualStyle visualStyle)
        {
            const int size = 96;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            var outerRadius = size * 0.42f;
            var innerRadius = size * 0.34f;
            var outer = new Vector2[6];
            var inner = new Vector2[6];

            for (var index = 0; index < 6; index++)
            {
                var angle = Mathf.Deg2Rad * (60f * index + 30f);
                var cos = Mathf.Cos(angle);
                var sin = Mathf.Sin(angle);
                outer[index] = new Vector2(center.x + outerRadius * cos, center.y + outerRadius * sin);
                inner[index] = new Vector2(center.x + innerRadius * cos, center.y + innerRadius * sin);
            }

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var point = new Vector2(x, y);
                    var inOuter = IsInsideHex(point, outer);
                    var inInner = IsInsideHex(point, inner);

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

                    var inShape = IsInsideShape(point, center, size, type);
                    var gray = inShape ? 0.95f : 0.50f;
                    texture.SetPixel(x, y, new Color(gray, gray, gray, 1f));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static bool IsInsideShape(Vector2 point, Vector2 center, int size, PokoTileType type)
        {
            var dx = point.x - center.x;
            var dy = point.y - center.y;
            var s = size * 0.30f;
            var lw = size * 0.055f;

            switch (type)
            {
                case PokoTileType.Red:
                {
                    var outer = s;
                    var inner = s - lw;
                    var dist = Mathf.Sqrt(dx * dx + dy * dy);
                    return dist <= outer && dist >= inner;
                }

                case PokoTileType.Yellow:
                {
                    var inOuter = Mathf.Abs(dx) <= s && Mathf.Abs(dy) <= s;
                    var inner2 = s - lw;
                    var inInner = Mathf.Abs(dx) <= inner2 && Mathf.Abs(dy) <= inner2;
                    return inOuter && !inInner;
                }

                case PokoTileType.Green:
                {
                    var h = s * 1.4f;
                    var w = s * 1.1f;
                    var y0 = center.y - h * 0.5f;
                    var relY = (point.y - y0) / h;
                    var halfW = w * (1f - relY);
                    var inOuter = relY >= 0f && relY <= 1f && Mathf.Abs(dx) <= halfW;

                    var y1 = y0 + lw;
                    var h2 = h - lw * 2f;
                    var relY2 = h2 > 0f ? (point.y - y1) / h2 : -1f;
                    var halfW2 = (w - lw) * (1f - relY2);
                    var inInner = relY2 >= 0f && relY2 <= 1f && Mathf.Abs(dx) <= halfW2;

                    return inOuter && !inInner;
                }

                case PokoTileType.Blue:
                {
                    var inOuter = Mathf.Abs(dx) + Mathf.Abs(dy) <= s;
                    var inner2 = s - lw;
                    var inInner = Mathf.Abs(dx) + Mathf.Abs(dy) <= inner2;
                    return inOuter && !inInner;
                }

                case PokoTileType.Purple:
                {
                    var outerR = s;
                    var innerR = s * 0.30f;
                    var angle = Mathf.Atan2(dy, dx);
                    var dist = Mathf.Sqrt(dx * dx + dy * dy);
                    var spike = Mathf.Max(0f, Mathf.Cos(angle * 5f));
                    var maxR = innerR + (outerR - innerR) * spike;
                    var minR = Mathf.Max(0f, maxR - lw);
                    return dist <= maxR && dist >= minR;
                }

                case PokoTileType.Orange:
                {
                    var barW = s * 0.25f;
                    var barL = s * 0.80f;
                    var inOuter = (Mathf.Abs(dx) <= barW && Mathf.Abs(dy) <= barL) || (Mathf.Abs(dy) <= barW && Mathf.Abs(dx) <= barL);
                    var iw = Mathf.Max(0f, barW - lw * 0.5f);
                    var il = Mathf.Max(0f, barL - lw);
                    var inInner = (Mathf.Abs(dx) <= iw && Mathf.Abs(dy) <= il) || (Mathf.Abs(dy) <= iw && Mathf.Abs(dx) <= il);
                    return inOuter && !inInner;
                }

                default:
                {
                    var dist = Mathf.Sqrt(dx * dx + dy * dy);
                    return dist <= s && dist >= s - lw;
                }
            }
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
