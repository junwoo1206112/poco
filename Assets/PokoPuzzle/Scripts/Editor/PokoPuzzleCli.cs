#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using PokoPuzzle.AI;
using PokoPuzzle.Core;
using PokoPuzzle.Core.Data;

namespace PokoPuzzle.Editor
{
    public static class PokoPuzzleCli
    {
        public static void CreateCoreBoardScene()
        {
            var args = CliArgs.Parse(Environment.GetCommandLineArgs());
            var settings = new PokoPrototypeSceneSettings(
                args.GetString("scenePath", PokoPrototypeSceneSettings.Default.ScenePath),
                args.GetInt("width", PokoPrototypeSceneSettings.Default.Width),
                args.GetInt("height", PokoPrototypeSceneSettings.Default.Height),
                args.GetInt("tileTypes", PokoPrototypeSceneSettings.Default.TileTypes),
                args.GetFloat("spacing", PokoPrototypeSceneSettings.Default.Spacing),
                args.GetLayout("layout", PokoPrototypeSceneSettings.Default.UseHexGrid),
                args.GetTileVisualStyle("tileVisual", PokoPrototypeSceneSettings.Default.TileVisualStyle));

            PokoPrototypeSceneBuilder.CreatePrototypeScene(settings);
            WriteCoreBoardReport(settings, args.GetString("reportPath", "md/cli-reports/core-board-input.md"));
            Debug.Log($"Poko CLI created core board scene: {settings.ScenePath}");
        }

        public static void ValidateCoreBoardScene()
        {
            var args = CliArgs.Parse(Environment.GetCommandLineArgs());
            var scenePath = args.GetString("scenePath", PokoPrototypeSceneSettings.Default.ScenePath);
            var reportPath = args.GetString("reportPath", "md/cli-reports/core-board-validation.md");

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var board = UnityEngine.Object.FindFirstObjectByType<LineLinkerBoard>();
            if (board == null)
            {
                Fail($"No LineLinkerBoard found in {scene.path}");
            }

            var serializedBoard = new SerializedObject(board);
            var width = serializedBoard.FindProperty("width").intValue;
            var height = serializedBoard.FindProperty("height").intValue;
            var tileTypes = serializedBoard.FindProperty("tileTypes").intValue;
            var spacing = serializedBoard.FindProperty("spacing").floatValue;
            var useHexGrid = serializedBoard.FindProperty("useHexGrid").boolValue;
            var tileVisualStyle = (PokoTileVisualStyle)serializedBoard.FindProperty("tileVisualStyle").enumValueIndex;
            var boardCamera = serializedBoard.FindProperty("boardCamera").objectReferenceValue;
            var linkLine = serializedBoard.FindProperty("linkLine").objectReferenceValue;
            var scoreText = serializedBoard.FindProperty("scoreText").objectReferenceValue;
            var agentText = serializedBoard.FindProperty("agentText").objectReferenceValue;
            var feedbackText = serializedBoard.FindProperty("feedbackText").objectReferenceValue;
            var useScreenHud = serializedBoard.FindProperty("useScreenHud").boolValue;
            var enablePlayLog = serializedBoard.FindProperty("enablePlayLog").boolValue;
            var playLogPath = serializedBoard.FindProperty("playLogPath").stringValue;

            if (width < 3 || height < 3)
            {
                Fail($"Board dimensions must be at least 3x3. Current: {width}x{height}");
            }

            if (tileTypes < 3 || tileTypes > 6)
            {
                Fail($"Tile type count must be between 3 and 6. Current: {tileTypes}");
            }

            if (spacing <= 0f)
            {
                Fail($"Tile spacing must be positive. Current: {spacing}");
            }

            if (boardCamera == null || linkLine == null)
            {
                Fail("Board runtime references are incomplete. Camera and link line are required.");
            }

            if (!useScreenHud && (scoreText == null || agentText == null || feedbackText == null))
            {
                Fail("World-space HUD mode requires score text, agent text, and feedback text references.");
            }

            if (enablePlayLog && string.IsNullOrWhiteSpace(playLogPath))
            {
                Fail("Play log path is required when play logging is enabled.");
            }

            ValidateAdjacency();
            WriteValidationReport(scenePath, width, height, tileTypes, spacing, useHexGrid, tileVisualStyle, reportPath);
            Debug.Log($"Poko CLI validated core board scene: {scenePath}");
        }

        public static void AnalyzeBoard()
        {
            var args = CliArgs.Parse(Environment.GetCommandLineArgs());
            var width = args.GetInt("width", PokoPrototypeSceneSettings.Default.Width);
            var height = args.GetInt("height", PokoPrototypeSceneSettings.Default.Height);
            var tileTypes = args.GetInt("tileTypes", PokoPrototypeSceneSettings.Default.TileTypes);
            var useHexGrid = args.GetLayout("layout", PokoPrototypeSceneSettings.Default.UseHexGrid);
            var seed = args.GetInt("seed", 1001);
            var score = args.GetInt("score", 0);
            var movesUsed = args.GetInt("movesUsed", 0);
            var reportPath = args.GetString("reportPath", "md/agent-reports/latest-board-analysis.md");
            var jsonPath = args.GetString("jsonPath", "md/agent-reports/latest-board-analysis.json");

            if (width < 3 || height < 3)
            {
                Fail($"Board dimensions must be at least 3x3. Current: {width}x{height}");
            }

            if (tileTypes < 3 || tileTypes > 6)
            {
                Fail($"Tile type count must be between 3 and 6. Current: {tileTypes}");
            }

            var board = GenerateBoard(width, height, tileTypes, seed);
            var telemetry = new BoardTelemetry(
                width,
                height,
                tileTypes,
                CountPossibleChains(board, tileTypes),
                FindLongestChain(board, tileTypes),
                score,
                movesUsed);

            var suggestion = new HeuristicGameDesignerAgent().Analyze(telemetry);
            WriteAgentMarkdownReport(reportPath, telemetry, suggestion, useHexGrid, seed);
            WriteAgentJsonReport(jsonPath, telemetry, suggestion, useHexGrid, seed);
            Debug.Log($"Poko CLI analyzed board. Report: {reportPath}, JSON: {jsonPath}");
        }

        public static void GenerateLevel()
        {
            var args = CliArgs.Parse(Environment.GetCommandLineArgs());
            var width = args.GetInt("width", PokoPrototypeSceneSettings.Default.Width);
            var height = args.GetInt("height", PokoPrototypeSceneSettings.Default.Height);
            var tileTypes = args.GetInt("tileTypes", PokoPrototypeSceneSettings.Default.TileTypes);
            var useHexGrid = args.GetLayout("layout", PokoPrototypeSceneSettings.Default.UseHexGrid);
            var seed = args.GetInt("seed", 1001);
            var score = args.GetInt("score", 0);
            var movesUsed = args.GetInt("movesUsed", 0);
            var levelId = args.GetString("levelId", "level_001");
            var assetPath = args.GetString("assetPath", $"Assets/PokoPuzzle/Data/Generated/{levelId}.asset");
            var reportPath = args.GetString("reportPath", $"md/level-reports/{levelId}.md");
            var jsonPath = args.GetString("jsonPath", $"md/level-reports/{levelId}.json");
            var balanceProfileId = args.GetString("balanceProfileId", "default");

            var board = GenerateBoard(width, height, tileTypes, seed);
            var telemetry = new BoardTelemetry(
                width,
                height,
                tileTypes,
                CountPossibleChains(board, tileTypes),
                FindLongestChain(board, tileTypes),
                score,
                movesUsed);
            var suggestion = new HeuristicGameDesignerAgent().Analyze(telemetry);
            var nextTileTypes = Mathf.Clamp(suggestion.SuggestedTileTypes, 3, 6);
            var spawnWeights = BuildSpawnWeights(nextTileTypes, suggestion.DifficultyLabel);

            var levelConfig = ScriptableObject.CreateInstance<PokoLevelConfig>();
            levelConfig.Configure(
                levelId,
                width,
                height,
                nextTileTypes,
                useHexGrid,
                suggestion.SuggestedMoveLimit,
                suggestion.SuggestedTargetScore,
                spawnWeights,
                0,
                0,
                balanceProfileId);

            SaveLevelAsset(assetPath, levelConfig);
            WriteGeneratedLevelMarkdown(reportPath, assetPath, telemetry, suggestion, levelConfig, spawnWeights, useHexGrid, seed);
            WriteGeneratedLevelJson(jsonPath, levelId, assetPath, telemetry, suggestion, levelConfig, spawnWeights, useHexGrid, seed);
            Debug.Log($"Poko CLI generated level config: {assetPath}");
        }

        public static void ApplyLevel()
        {
            var args = CliArgs.Parse(Environment.GetCommandLineArgs());
            var scenePath = args.GetString("scenePath", PokoPrototypeSceneSettings.Default.ScenePath);
            var levelId = args.GetString("levelId", "level_001");
            var assetPath = args.GetString("assetPath", $"Assets/PokoPuzzle/Data/Generated/{levelId}.asset");
            var reportPath = args.GetString("reportPath", $"md/level-reports/{levelId}-applied.md");

            var levelConfig = AssetDatabase.LoadAssetAtPath<PokoLevelConfig>(assetPath);
            if (levelConfig == null)
            {
                Fail($"No PokoLevelConfig found at {assetPath}. Run generate-level first.");
            }

            ApplyLevelToScene(scenePath, assetPath, levelConfig);
            WriteAppliedLevelReport(reportPath, scenePath, assetPath, levelConfig);
            Debug.Log($"Poko CLI applied level config '{assetPath}' to scene '{scenePath}'.");
        }

        public static void AnalyzePlayLog()
        {
            var args = CliArgs.Parse(Environment.GetCommandLineArgs());
            var logPath = args.GetString("logPath", "md/playtest-logs/latest-playtest.jsonl");
            var reportPath = args.GetString("reportPath", "md/agent-reports/latest-playtest-analysis.md");
            var jsonPath = args.GetString("jsonPath", "md/agent-reports/latest-playtest-analysis.json");

            if (!File.Exists(logPath))
            {
                Fail($"No play log found at {logPath}. Play the prototype first.");
            }

            var analysis = PlayLogAnalysis.FromFile(logPath);
            WritePlayLogMarkdownReport(reportPath, logPath, analysis);
            WritePlayLogJsonReport(jsonPath, logPath, analysis);
            Debug.Log($"Poko CLI analyzed play log. Report: {reportPath}, JSON: {jsonPath}");
        }

        public static void RetuneLevel()
        {
            var args = CliArgs.Parse(Environment.GetCommandLineArgs());
            var logPath = args.GetString("logPath", "md/playtest-logs/latest-playtest.jsonl");
            var levelId = args.GetString("levelId", "level_002");
            var assetPath = args.GetString("assetPath", $"Assets/PokoPuzzle/Data/Generated/{levelId}.asset");
            var reportPath = args.GetString("reportPath", $"md/level-reports/{levelId}-retune.md");
            var jsonPath = args.GetString("jsonPath", $"md/level-reports/{levelId}-retune.json");
            var balanceProfileId = args.GetString("balanceProfileId", "default");

            if (!File.Exists(logPath))
            {
                Fail($"No play log found at {logPath}. Play the prototype first.");
            }

            var analysis = PlayLogAnalysis.FromFile(logPath);
            var spawnWeights = BuildSpawnWeights(analysis.SuggestedTileTypes, analysis.DifficultyLabel);
            var levelConfig = ScriptableObject.CreateInstance<PokoLevelConfig>();
            levelConfig.Configure(
                levelId,
                analysis.Width,
                analysis.Height,
                analysis.SuggestedTileTypes,
                true,
                analysis.SuggestedMoveLimit,
                analysis.SuggestedTargetScore,
                spawnWeights,
                analysis.SuggestedRegularEnemyHp,
                analysis.SuggestedBossHp,
                balanceProfileId);

            SaveLevelAsset(assetPath, levelConfig);
            WriteRetunedLevelMarkdown(reportPath, logPath, assetPath, analysis, levelConfig, spawnWeights);
            WriteRetunedLevelJson(jsonPath, logPath, assetPath, analysis, levelConfig, spawnWeights);
            Debug.Log($"Poko CLI retuned level from play log: {assetPath}");
        }

        public static void PlanLevelExperiments()
        {
            var args = CliArgs.Parse(Environment.GetCommandLineArgs());
            var logPath = args.GetString("logPath", "md/playtest-logs/latest-playtest.jsonl");
            var experimentId = args.GetString("experimentId", "exp_001");
            var assetRoot = args.GetString("assetRoot", "Assets/PokoPuzzle/Data/Generated/Experiments");
            var reportPath = args.GetString("reportPath", $"md/experiment-reports/{experimentId}.md");
            var jsonPath = args.GetString("jsonPath", $"md/experiment-reports/{experimentId}.json");

            if (!File.Exists(logPath))
            {
                Fail($"No play log found at {logPath}. Play the prototype first.");
            }

            var analysis = PlayLogAnalysis.FromFile(logPath);
            var variants = BuildExperimentVariants(experimentId, assetRoot, analysis);
            foreach (var variant in variants)
            {
                SaveLevelAsset(variant.AssetPath, variant.LevelConfig);
            }

            WriteExperimentMarkdownReport(reportPath, experimentId, logPath, analysis, variants);
            WriteExperimentJsonReport(jsonPath, experimentId, logPath, analysis, variants);
            Debug.Log($"Poko CLI planned level experiments. Report: {reportPath}, JSON: {jsonPath}");
        }

        public static void CompareLevelExperiments()
        {
            var args = CliArgs.Parse(Environment.GetCommandLineArgs());
            var experimentId = args.GetString("experimentId", "exp_001");
            var controlLog = args.GetString("controlLog", ExperimentLogPath(experimentId, "control"));
            var readabilityLog = args.GetString("readabilityLog", ExperimentLogPath(experimentId, "readability"));
            var comboLog = args.GetString("comboLog", ExperimentLogPath(experimentId, "combo"));
            var reportPath = args.GetString("reportPath", $"md/experiment-reports/{experimentId}-comparison.md");
            var jsonPath = args.GetString("jsonPath", $"md/experiment-reports/{experimentId}-comparison.json");

            RequirePlayLog(controlLog, "control");
            RequirePlayLog(readabilityLog, "readability");
            RequirePlayLog(comboLog, "combo");

            var results = new[]
            {
                new ExperimentResult("control", "Baseline retune", controlLog, PlayLogAnalysis.FromFile(controlLog)),
                new ExperimentResult("readability", "Hex path readability", readabilityLog, PlayLogAnalysis.FromFile(readabilityLog)),
                new ExperimentResult("combo", "Combo showcase", comboLog, PlayLogAnalysis.FromFile(comboLog))
            };
            var recommendation = ChooseExperimentRecommendation(results);

            WriteExperimentComparisonMarkdown(reportPath, experimentId, results, recommendation);
            WriteExperimentComparisonJson(jsonPath, experimentId, results, recommendation);
            Debug.Log($"Poko CLI compared experiment results. Report: {reportPath}, JSON: {jsonPath}");
        }

        public static void PromoteExperimentWinner()
        {
            var args = CliArgs.Parse(Environment.GetCommandLineArgs());
            var experimentId = args.GetString("experimentId", "exp_001");
            var levelId = args.GetString("levelId", $"{experimentId}_winner");
            var assetPath = args.GetString("assetPath", $"Assets/PokoPuzzle/Data/Generated/Promoted/{levelId}.asset");
            var reportPath = args.GetString("reportPath", $"md/experiment-reports/{experimentId}-promotion.md");
            var milestonePath = args.GetString("milestonePath", $"md/portfolio-milestones/{experimentId}-designer-loop.md");
            var applyScene = args.GetBool("applyScene", false);
            var scenePath = args.GetString("scenePath", PokoPrototypeSceneSettings.Default.ScenePath);
            var results = LoadExperimentResults(experimentId, args);
            var recommendation = ChooseExperimentRecommendation(results);
            var sourceAssetPath = ExperimentAssetPath(experimentId, recommendation.VariantName);
            var sourceLevel = AssetDatabase.LoadAssetAtPath<PokoLevelConfig>(sourceAssetPath);
            if (sourceLevel == null)
            {
                Fail($"No recommended experiment asset found at {sourceAssetPath}. Run plan-level-experiments first.");
            }

            var promotedLevel = ScriptableObject.CreateInstance<PokoLevelConfig>();
            promotedLevel.Configure(levelId, sourceLevel.Width, sourceLevel.Height, sourceLevel.TileTypes, sourceLevel.UseHexGrid, sourceLevel.MoveLimit, sourceLevel.TargetScore, sourceLevel.SpawnWeights, sourceLevel.RegularEnemyHp, sourceLevel.BossHp, sourceLevel.BalanceProfileId);
            SaveLevelAsset(assetPath, promotedLevel);
            WritePromotionReport(reportPath, experimentId, sourceAssetPath, assetPath, promotedLevel, results, recommendation, applyScene, scenePath);
            WritePortfolioMilestone(milestonePath, experimentId, assetPath, promotedLevel, results, recommendation);

            if (applyScene)
            {
                ApplyLevelToScene(scenePath, assetPath, promotedLevel);
            }

            Debug.Log($"Poko CLI promoted experiment winner: {assetPath}");
        }

        public static void DesignerLoopStatus()
        {
            var args = CliArgs.Parse(Environment.GetCommandLineArgs());
            var experimentId = args.GetString("experimentId", "exp_001");
            var latestLog = args.GetString("logPath", "md/playtest-logs/latest-playtest.jsonl");
            var playtestAnalysis = args.GetString("analysisPath", "md/agent-reports/latest-playtest-analysis.json");
            var experimentPlan = args.GetString("planPath", $"md/experiment-reports/{experimentId}.json");
            var comparison = args.GetString("comparisonPath", $"md/experiment-reports/{experimentId}-comparison.json");
            var promotion = args.GetString("promotionPath", $"md/experiment-reports/{experimentId}-promotion.md");
            var reportPath = args.GetString("reportPath", "md/designer-loop/latest-status.md");
            var jsonPath = args.GetString("jsonPath", "md/designer-loop/latest-status.json");
            var status = DesignerLoopState.Inspect(experimentId, latestLog, playtestAnalysis, experimentPlan, comparison, promotion);

            WriteDesignerLoopStatusMarkdown(reportPath, status);
            WriteDesignerLoopStatusJson(jsonPath, status);
            Debug.Log($"Poko CLI wrote designer loop status: {reportPath}");
        }

        public static void LlmDesignReview()
        {
            var args = CliArgs.Parse(Environment.GetCommandLineArgs());
            var inputPath = args.GetString("inputPath", "md/agent-reports/latest-playtest-analysis.json");
            var model = args.GetString("model", "gpt-4o-mini");
            var requestPath = args.GetString("requestPath", "md/llm-reports/latest-designer-request.json");
            var reportPath = args.GetString("reportPath", "md/llm-reports/latest-designer-review.md");
            var rawResponsePath = args.GetString("rawResponsePath", "md/llm-reports/latest-designer-response.json");
            var baseUrl = args.GetString("baseUrl", Environment.GetEnvironmentVariable("OPENAI_BASE_URL") ?? "https://api.openai.com");
            baseUrl = baseUrl.TrimEnd('/');

            if (!File.Exists(inputPath))
            {
                Fail($"No designer input JSON found at {inputPath}. Run analyze-playlog first.");
            }

            var sourceJson = File.ReadAllText(inputPath);
            var requestBody = BuildLlmDesignerRequest(model, sourceJson);
            WriteReport(requestPath, requestBody);

            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                WritePendingLlmReport(reportPath, model, inputPath, requestPath);
                Debug.Log($"Poko CLI saved LLM designer request packet without API call: {requestPath}");
                return;
            }

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            using var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            var url = $"{baseUrl}/v1/responses";
            var response = client.PostAsync(url, content).GetAwaiter().GetResult();
            var responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            WriteReport(rawResponsePath, responseBody);

            if (!response.IsSuccessStatusCode)
            {
                Fail($"OpenAI Responses API request failed: {(int)response.StatusCode}. Raw response saved to {rawResponsePath}");
            }

            WriteLlmDesignerReview(reportPath, model, inputPath, ExtractResponseText(responseBody), rawResponsePath);
            Debug.Log($"Poko CLI saved LLM designer review: {reportPath}");
        }

        public static void CompareAgentStrategies()
        {
            var args = CliArgs.Parse(Environment.GetCommandLineArgs());
            var logPath = args.GetString("logPath", "md/playtest-logs/latest-playtest.jsonl");
            var reportPath = args.GetString("reportPath", "md/agent-reports/agent-strategy-comparison.md");

            if (!File.Exists(logPath))
            {
                Fail($"No play log found at {logPath}. Play the prototype first.");
            }

            var analysis = PlayLogAnalysis.FromFile(logPath);
            var telemetry = TelemetryFromAnalysis(analysis);
            var heuristicResult = new HeuristicGameDesignerAgent().Analyze(telemetry);
            var llmResult = new LLMGameDesignerAgent().Analyze(telemetry);

            var body =
                "# AI Game Designer Strategy Comparison\n\n" +
                "## Input Telemetry\n\n" +
                $"- Board: `{analysis.Width}x{analysis.Height}`, Tile types: `{analysis.TileTypes}`\n" +
                $"- Score: `{analysis.FinalScore}`, Moves: `{analysis.MovesUsed}`, Valid: `{analysis.ValidMoves}`, Invalid: `{analysis.InvalidMoves}`\n" +
                $"- Avg chain: `{analysis.AverageChainLength:F2}`, Max combo: `{analysis.MaxCombo}`, Fever triggers: `{analysis.FeverTriggers}`\n" +
                $"- Damage dealt: `{analysis.TotalDamageDealt}`, Bombs: `{analysis.BombsGenerated}`, Rainbow: `{analysis.RainbowCleared}`\n\n" +
                "## Heuristic Agent Result\n\n" +
                $"- **Difficulty**: `{heuristicResult.DifficultyLabel}`\n" +
                $"- **Summary**: {heuristicResult.Summary}\n" +
                $"- **Design Intent**: {heuristicResult.DesignIntent}\n" +
                $"- **Risk**: {heuristicResult.Risk}\n" +
                $"- **Recommended Action**: {heuristicResult.RecommendedAction}\n" +
                $"- Suggested move limit: `{heuristicResult.SuggestedMoveLimit}`, target score: `{heuristicResult.SuggestedTargetScore}`, tile types: `{heuristicResult.SuggestedTileTypes}`\n\n" +
                "## LLM Agent Result\n\n" +
                $"- **Difficulty**: `{llmResult.DifficultyLabel}`\n" +
                $"- **Summary**: {llmResult.Summary}\n" +
                $"- **Design Intent**: {llmResult.DesignIntent}\n" +
                $"- **Risk**: {llmResult.Risk}\n" +
                $"- **Recommended Action**: {llmResult.RecommendedAction}\n" +
                $"- Suggested move limit: `{llmResult.SuggestedMoveLimit}`, target score: `{llmResult.SuggestedTargetScore}`, tile types: `{llmResult.SuggestedTileTypes}`\n\n" +
                "## Comparison\n\n" +
                $"| Dimension | Heuristic | LLM |\n" +
                $"|---|---|---|\n" +
                $"| Difficulty | {heuristicResult.DifficultyLabel} | {llmResult.DifficultyLabel} |\n" +
                $"| Action | {heuristicResult.RecommendedAction} | {llmResult.RecommendedAction} |\n" +
                $"| Move limit | {heuristicResult.SuggestedMoveLimit} | {llmResult.SuggestedMoveLimit} |\n" +
                $"| Target score | {heuristicResult.SuggestedTargetScore} | {llmResult.SuggestedTargetScore} |\n" +
                $"| Tile types | {heuristicResult.SuggestedTileTypes} | {llmResult.SuggestedTileTypes} |\n\n" +
                "## Portfolio Note\n\n" +
                "This comparison demonstrates two AI game designer strategies on the same play log. " +
                "The heuristic agent uses deterministic rules; the LLM agent uses `gpt-4o-mini` with natural language analysis. " +
                "Both implement the same `IGameDesignerAgent` interface, making them swappable at runtime.\n";

            WriteReport(reportPath, body);
            Debug.Log($"Poko CLI compared agent strategies. Report: {reportPath}");
        }

        public static void ConvertExcelData()
        {
            ExcelDataGenerator.ConvertAll();
            Debug.Log("Poko CLI converted Excel data into ScriptableObject databases.");
        }

        private static BoardTelemetry TelemetryFromAnalysis(PlayLogAnalysis analysis)
        {
            return new BoardTelemetry(
                analysis.Width,
                analysis.Height,
                analysis.TileTypes,
                0,
                0,
                analysis.FinalScore,
                analysis.MovesUsed,
                analysis.MaxCombo,
                analysis.FeverTriggers > 0,
                100,
                analysis.TotalDamageDealt,
                analysis.BombsDetonated,
                analysis.SpecialBlocksCleared,
                analysis.RainbowCleared);
        }

        private static void WriteCoreBoardReport(PokoPrototypeSceneSettings settings, string reportPath)
        {
            var body =
                "# Core Board & Input CLI Creation Report\n\n" +
                "## Result\n\n" +
                $"- Scene path: `{settings.ScenePath}`\n" +
                $"- Board size: `{settings.Width}x{settings.Height}`\n" +
                $"- Tile types: `{settings.TileTypes}`\n" +
                $"- Tile spacing: `{settings.Spacing.ToString(CultureInfo.InvariantCulture)}`\n" +
                $"- Layout: `{LayoutName(settings.UseHexGrid)}`\n" +
                $"- Tile visual: `{TileVisualName(settings.TileVisualStyle)}`\n" +
                $"- Neighbor directions: `{HexGridUtility.GetNeighborCount()}`\n\n" +
                "## Poko-style Criteria\n\n" +
                "- The default board uses an odd-row offset hex grid.\n" +
                "- Hex linking accepts only 6-direction neighbors.\n" +
                "- Drag through same-type tiles to build a chain.\n" +
                "- Dragging back to the previous tile removes the last tile from the active chain.\n" +
                "- Clear, score, collapse, and refill happen for chains of 3 or more.\n" +
                "- Mouse and touch input use Unity Input System APIs.\n" +
                "- The prototype validates mechanics with original placeholder tiles instead of copied PokoPang art or UI.\n";

            WriteReport(reportPath, body);
        }

        private static void WriteValidationReport(string scenePath, int width, int height, int tileTypes, float spacing, bool useHexGrid, PokoTileVisualStyle tileVisualStyle, string reportPath)
        {
            var body =
                "# Core Board & Input CLI Validation Report\n\n" +
                "## Result\n\n" +
                "- Status: `PASS`\n" +
                $"- Scene path: `{scenePath}`\n" +
                $"- Board size: `{width}x{height}`\n" +
                $"- Tile types: `{tileTypes}`\n" +
                $"- Tile spacing: `{spacing.ToString(CultureInfo.InvariantCulture)}`\n" +
                $"- Layout: `{LayoutName(useHexGrid)}`\n" +
                $"- Tile visual: `{TileVisualName(tileVisualStyle)}`\n" +
                $"- Neighbor directions: `{HexGridUtility.GetNeighborCount()}`\n\n" +
                "## Checks\n\n" +
                "- `LineLinkerBoard` component exists.\n" +
                "- Board dimensions satisfy the minimum size.\n" +
                "- Tile type count is within range.\n" +
                "- Layout adjacency rules pass deterministic validation.\n" +
                "- Camera, link line, score text, feedback text, and AI designer text references are connected.\n";

            WriteReport(reportPath, body);
        }

        private static string WriteHexGridVisual(int width, int height, int tileTypes, int seed, bool useHexGrid)
        {
            var board = GenerateBoard(width, height, tileTypes, seed);
            var symbols = new[] { "●", "◆", "■", "▲", "⬟", "★" };
            var typeNames = new[] { "Red", "Yellow", "Green", "Blue", "Purple", "Orange" };
            var result = new StringBuilder();
            result.AppendLine();
            result.AppendLine("### Board Visual");
            result.AppendLine();
            result.AppendLine($"- Layout: `{LayoutName(useHexGrid)}` | Size: `{width}x{height}` | Seed: `{seed}` | Tile types: `{tileTypes}`");
            result.AppendLine();
            result.AppendLine("```");

            if (useHexGrid)
            {
                for (var row = height - 1; row >= 0; row--)
                {
                    var tileCount = HexGridUtility.RowSize(row, width);
                    var isEven = (row & 1) == 0;

                    result.Append(isEven ? "  / " : "  \\___/ ");

                    for (var col = 0; col < tileCount; col++)
                    {
                        var sym = symbols[board[col, row] % symbols.Length];
                        if (isEven)
                        {
                            result.Append(col < tileCount - 1 ? $"{sym} \\___/ " : $"{sym} \\");
                        }
                        else
                        {
                            result.Append(col < tileCount - 1 ? $"{sym} \\___/ " : $"{sym} \\___/");
                        }
                    }

                    result.AppendLine();
                }
            }
            else
            {
                for (var row = height - 1; row >= 0; row--)
                {
                    for (var column = 0; column < width; column++)
                    {
                        var type = board[column, row];
                        var symbol = type >= 0 && type < symbols.Length ? symbols[type] : "?";
                        result.Append(symbol);
                        result.Append(' ');
                    }

                    result.AppendLine();
                }
            }

            result.AppendLine("```");
            result.AppendLine();

            for (var index = 0; index < tileTypes && index < symbols.Length; index++)
            {
                result.AppendLine($"- `{symbols[index]}` = {typeNames[index]}");
            }

            result.AppendLine();
            return result.ToString();
        }

        private static void WriteAgentMarkdownReport(string reportPath, BoardTelemetry telemetry, AgentSuggestion suggestion, bool useHexGrid, int seed)
        {
            var body =
                "# Game Designer Agent Board Analysis\n\n" +
                "## Input\n\n" +
                $"- Layout: `{LayoutName(useHexGrid)}`\n" +
                $"- Seed: `{seed}`\n" +
                $"- Board size: `{telemetry.Width}x{telemetry.Height}`\n" +
                $"- Tile types: `{telemetry.TileTypes}`\n" +
                $"- Score: `{telemetry.Score}`\n" +
                $"- Moves used: `{telemetry.MovesUsed}`\n\n" +
                "## Telemetry\n\n" +
                $"- Possible chain starts: `{telemetry.PossibleChains}`\n" +
                $"- Longest same-type area: `{telemetry.LongestChain}`\n\n" +
                "## Designer Agent Judgment\n\n" +
                $"- Difficulty: `{suggestion.DifficultyLabel}`\n" +
                $"- Diagnosis: {suggestion.Summary}\n" +
                $"- Intent: {suggestion.DesignIntent}\n" +
                $"- Risk: {suggestion.Risk}\n" +
                $"- Action: {suggestion.RecommendedAction}\n\n" +
                "## Suggested Next Level Tuning\n\n" +
                $"- Move limit: `{suggestion.SuggestedMoveLimit}`\n" +
                $"- Target score: `{suggestion.SuggestedTargetScore}`\n" +
                 $"- Tile types: `{suggestion.SuggestedTileTypes}`\n" +
                 WriteHexGridVisual(telemetry.Width, telemetry.Height, telemetry.TileTypes, seed, useHexGrid);

            WriteReport(reportPath, body);
        }

        private static void WriteAgentJsonReport(string jsonPath, BoardTelemetry telemetry, AgentSuggestion suggestion, bool useHexGrid, int seed)
        {
            var body =
                "{\n" +
                $"  \"layout\": \"{LayoutName(useHexGrid)}\",\n" +
                $"  \"seed\": {seed},\n" +
                $"  \"width\": {telemetry.Width},\n" +
                $"  \"height\": {telemetry.Height},\n" +
                $"  \"tileTypes\": {telemetry.TileTypes},\n" +
                $"  \"possibleChains\": {telemetry.PossibleChains},\n" +
                $"  \"longestChain\": {telemetry.LongestChain},\n" +
                $"  \"score\": {telemetry.Score},\n" +
                $"  \"movesUsed\": {telemetry.MovesUsed},\n" +
                "  \"suggestion\": {\n" +
                $"    \"difficulty\": \"{EscapeJson(suggestion.DifficultyLabel)}\",\n" +
                $"    \"diagnosis\": \"{EscapeJson(suggestion.Summary)}\",\n" +
                $"    \"intent\": \"{EscapeJson(suggestion.DesignIntent)}\",\n" +
                $"    \"risk\": \"{EscapeJson(suggestion.Risk)}\",\n" +
                $"    \"action\": \"{EscapeJson(suggestion.RecommendedAction)}\",\n" +
                $"    \"moveLimit\": {suggestion.SuggestedMoveLimit},\n" +
                $"    \"targetScore\": {suggestion.SuggestedTargetScore},\n" +
                $"    \"tileTypes\": {suggestion.SuggestedTileTypes}\n" +
                "  }\n" +
                "}\n";

            WriteReport(jsonPath, body);
        }

        private static void WriteGeneratedLevelMarkdown(
            string reportPath,
            string assetPath,
            BoardTelemetry telemetry,
            AgentSuggestion suggestion,
            PokoLevelConfig levelConfig,
            int[] spawnWeights,
            bool useHexGrid,
            int seed)
        {
            var body =
                "# Generated Level Config\n\n" +
                "## Source Analysis\n\n" +
                $"- Layout: `{LayoutName(useHexGrid)}`\n" +
                $"- Seed: `{seed}`\n" +
                $"- Possible chain starts: `{telemetry.PossibleChains}`\n" +
                $"- Longest same-type area: `{telemetry.LongestChain}`\n" +
                $"- Agent difficulty: `{suggestion.DifficultyLabel}`\n" +
                $"- Agent action: {suggestion.RecommendedAction}\n\n" +
                "## Generated Unity Asset\n\n" +
                $"- Asset path: `{assetPath}`\n" +
                $"- Level id: `{levelConfig.LevelId}`\n" +
                $"- Board size: `{levelConfig.Width}x{levelConfig.Height}`\n" +
                $"- Tile types: `{levelConfig.TileTypes}`\n" +
                $"- Move limit: `{levelConfig.MoveLimit}`\n" +
                $"- Target score: `{levelConfig.TargetScore}`\n" +
                $"- Balance profile: `{levelConfig.BalanceProfileId}`\n" +
                 $"- Spawn weights: `{FormatWeights(spawnWeights)}`\n\n" +
                 WriteHexGridVisual(telemetry.Width, telemetry.Height, telemetry.TileTypes, seed, useHexGrid) +
                 "## Why This Level Was Generated\n\n" +
                 $"{suggestion.DesignIntent} {suggestion.Risk} {suggestion.RecommendedAction}\n";

            WriteReport(reportPath, body);
        }

        private static void WriteGeneratedLevelJson(
            string jsonPath,
            string levelId,
            string assetPath,
            BoardTelemetry telemetry,
            AgentSuggestion suggestion,
            PokoLevelConfig levelConfig,
            int[] spawnWeights,
            bool useHexGrid,
            int seed)
        {
            var body =
                "{\n" +
                $"  \"levelId\": \"{EscapeJson(levelId)}\",\n" +
                $"  \"assetPath\": \"{EscapeJson(assetPath)}\",\n" +
                $"  \"layout\": \"{LayoutName(useHexGrid)}\",\n" +
                $"  \"seed\": {seed},\n" +
                $"  \"width\": {levelConfig.Width},\n" +
                $"  \"height\": {levelConfig.Height},\n" +
                $"  \"tileTypes\": {levelConfig.TileTypes},\n" +
                $"  \"moveLimit\": {levelConfig.MoveLimit},\n" +
                $"  \"targetScore\": {levelConfig.TargetScore},\n" +
                $"  \"balanceProfileId\": \"{EscapeJson(levelConfig.BalanceProfileId)}\",\n" +
                $"  \"spawnWeights\": [{FormatWeights(spawnWeights)}],\n" +
                "  \"sourceTelemetry\": {\n" +
                $"    \"possibleChains\": {telemetry.PossibleChains},\n" +
                $"    \"longestChain\": {telemetry.LongestChain},\n" +
                $"    \"difficulty\": \"{EscapeJson(suggestion.DifficultyLabel)}\",\n" +
                $"    \"action\": \"{EscapeJson(suggestion.RecommendedAction)}\"\n" +
                "  }\n" +
                "}\n";

            WriteReport(jsonPath, body);
        }

        private static void WriteAppliedLevelReport(string reportPath, string scenePath, string assetPath, PokoLevelConfig levelConfig)
        {
            var body =
                "# Applied Level Config\n\n" +
                "## Result\n\n" +
                "- Status: `PASS`\n" +
                $"- Scene path: `{scenePath}`\n" +
                $"- Level asset: `{assetPath}`\n\n" +
                "## Applied Values\n\n" +
                $"- Level id: `{levelConfig.LevelId}`\n" +
                $"- Board size: `{levelConfig.Width}x{levelConfig.Height}`\n" +
                $"- Layout: `{LayoutName(levelConfig.UseHexGrid)}`\n" +
                $"- Tile types: `{levelConfig.TileTypes}`\n" +
                $"- Move limit: `{levelConfig.MoveLimit}`\n" +
                $"- Target score: `{levelConfig.TargetScore}`\n" +
                $"- Balance profile: `{levelConfig.BalanceProfileId}`\n" +
                $"- Spawn weights: `{FormatWeights(levelConfig.SpawnWeights)}`\n\n" +
                "## Why This Matters\n\n" +
                "The designer agent output is now connected to the playable Unity scene, so the next Play session uses generated level tuning instead of only showing a report.\n";

            WriteReport(reportPath, body);
        }

        private static void ApplyLevelToScene(string scenePath, string assetPath, PokoLevelConfig levelConfig)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var board = UnityEngine.Object.FindFirstObjectByType<LineLinkerBoard>();
            if (board == null)
            {
                Fail($"No LineLinkerBoard found in {scene.path}");
            }

            var serializedBoard = new SerializedObject(board);
            serializedBoard.FindProperty("levelConfig").objectReferenceValue = levelConfig;
            serializedBoard.FindProperty("width").intValue = levelConfig.Width;
            serializedBoard.FindProperty("height").intValue = levelConfig.Height;
            serializedBoard.FindProperty("tileTypes").intValue = levelConfig.TileTypes;
            serializedBoard.FindProperty("useHexGrid").boolValue = levelConfig.UseHexGrid;
            serializedBoard.FindProperty("moveLimit").intValue = levelConfig.MoveLimit;
            serializedBoard.FindProperty("targetScore").intValue = levelConfig.TargetScore;
            serializedBoard.FindProperty("balanceProfileId").stringValue = levelConfig.BalanceProfileId;
            serializedBoard.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"Poko CLI connected level asset '{assetPath}' to scene '{scenePath}'.");
        }

        private static void WritePlayLogMarkdownReport(string reportPath, string logPath, PlayLogAnalysis analysis)
        {
            var body =
                "# Game Designer Agent Playtest Analysis\n\n" +
                "## Input\n\n" +
                $"- Play log: `{logPath}`\n" +
                $"- Level id: `{analysis.LevelId}`\n" +
                $"- Board size: `{analysis.Width}x{analysis.Height}`\n" +
                $"- Tile types: `{analysis.TileTypes}`\n" +
                $"- Move limit: `{analysis.MoveLimit}`\n" +
                $"- Target score: `{analysis.TargetScore}`\n\n" +
                "## Play Telemetry\n\n" +
                $"- Result: `{analysis.Result}`\n" +
                $"- Final score: `{analysis.FinalScore}`\n" +
                $"- Moves used: `{analysis.MovesUsed}`\n" +
                $"- Valid moves: `{analysis.ValidMoves}`\n" +
                $"- Invalid short releases: `{analysis.InvalidMoves}`\n" +
                $"- Average chain length: `{analysis.AverageChainLength.ToString("0.00", CultureInfo.InvariantCulture)}`\n" +
                 $"- Average score per valid move: `{analysis.AverageScorePerValidMove.ToString("0.00", CultureInfo.InvariantCulture)}`\n\n" +
                 "## Combat Telemetry\n\n" +
                 $"- Max combo: `{analysis.MaxCombo}`\n" +
                 $"- Fever triggers: `{analysis.FeverTriggers}`\n" +
                 $"- Total damage dealt: `{analysis.TotalDamageDealt}`\n" +
                 $"- Bombs generated: `{analysis.BombsGenerated}`\n" +
                 $"- Bombs detonated: `{analysis.BombsDetonated}`\n" +
                 $"- Special blocks cleared: `{analysis.SpecialBlocksCleared}`\n" +
                 $"- Rainbow bombs cleared: `{analysis.RainbowCleared}`\n\n" +
                 "## Designer Agent Judgment\n\n" +
                $"- Difficulty: `{analysis.DifficultyLabel}`\n" +
                $"- Diagnosis: {analysis.Diagnosis}\n" +
                $"- Risk: {analysis.Risk}\n" +
                $"- Action: {analysis.Action}\n" +
                $"- Suggested regular enemy HP: `{analysis.SuggestedRegularEnemyHp}`\n" +
                $"- Suggested boss HP: `{analysis.SuggestedBossHp}`\n";

            WriteReport(reportPath, body);
        }

        private static void WritePlayLogJsonReport(string jsonPath, string logPath, PlayLogAnalysis analysis)
        {
            var body =
                "{\n" +
                $"  \"logPath\": \"{EscapeJson(logPath)}\",\n" +
                $"  \"levelId\": \"{EscapeJson(analysis.LevelId)}\",\n" +
                $"  \"width\": {analysis.Width},\n" +
                $"  \"height\": {analysis.Height},\n" +
                $"  \"tileTypes\": {analysis.TileTypes},\n" +
                $"  \"moveLimit\": {analysis.MoveLimit},\n" +
                $"  \"targetScore\": {analysis.TargetScore},\n" +
                $"  \"result\": \"{EscapeJson(analysis.Result)}\",\n" +
                $"  \"finalScore\": {analysis.FinalScore},\n" +
                $"  \"movesUsed\": {analysis.MovesUsed},\n" +
                $"  \"validMoves\": {analysis.ValidMoves},\n" +
                $"  \"invalidMoves\": {analysis.InvalidMoves},\n" +
                $"  \"averageChainLength\": {analysis.AverageChainLength.ToString("0.00", CultureInfo.InvariantCulture)},\n" +
                 $"  \"averageScorePerValidMove\": {analysis.AverageScorePerValidMove.ToString("0.00", CultureInfo.InvariantCulture)},\n" +
                 $"  \"maxCombo\": {analysis.MaxCombo},\n" +
                 $"  \"feverTriggers\": {analysis.FeverTriggers},\n" +
                 $"  \"totalDamageDealt\": {analysis.TotalDamageDealt},\n" +
                 $"  \"bombsGenerated\": {analysis.BombsGenerated},\n" +
                 $"  \"bombsDetonated\": {analysis.BombsDetonated},\n" +
                 $"  \"specialBlocksCleared\": {analysis.SpecialBlocksCleared},\n" +
                 $"  \"rainbowCleared\": {analysis.RainbowCleared},\n" +
                 "  \"suggestion\": {\n" +
                $"    \"difficulty\": \"{EscapeJson(analysis.DifficultyLabel)}\",\n" +
                $"    \"diagnosis\": \"{EscapeJson(analysis.Diagnosis)}\",\n" +
                $"    \"risk\": \"{EscapeJson(analysis.Risk)}\",\n" +
                $"    \"action\": \"{EscapeJson(analysis.Action)}\",\n" +
                $"    \"suggestedRegularEnemyHp\": {analysis.SuggestedRegularEnemyHp},\n" +
                $"    \"suggestedBossHp\": {analysis.SuggestedBossHp}\n" +
                "  }\n" +
                "}\n";

            WriteReport(jsonPath, body);
        }

        private static void WriteRetunedLevelMarkdown(string reportPath, string logPath, string assetPath, PlayLogAnalysis analysis, PokoLevelConfig levelConfig, int[] spawnWeights)
        {
            var body =
                "# Retuned Level From Playtest\n\n" +
                "## Source\n\n" +
                $"- Play log: `{logPath}`\n" +
                $"- Source result: `{analysis.Result}`\n" +
                $"- Source difficulty: `{analysis.DifficultyLabel}`\n" +
                $"- Source diagnosis: {analysis.Diagnosis}\n\n" +
                "## Generated Next Level\n\n" +
                $"- Asset path: `{assetPath}`\n" +
                $"- Level id: `{levelConfig.LevelId}`\n" +
                $"- Board size: `{levelConfig.Width}x{levelConfig.Height}`\n" +
                $"- Tile types: `{levelConfig.TileTypes}`\n" +
                $"- Move limit: `{levelConfig.MoveLimit}`\n" +
                $"- Target score: `{levelConfig.TargetScore}`\n" +
                $"- Regular enemy HP: `{levelConfig.RegularEnemyHp}`\n" +
                $"- Boss HP: `{levelConfig.BossHp}`\n" +
                $"- Balance profile: `{levelConfig.BalanceProfileId}`\n" +
                $"- Spawn weights: `{FormatWeights(spawnWeights)}`\n\n" +
                "## Agent Reasoning\n\n" +
                $"{analysis.Action}\n";

            WriteReport(reportPath, body);
        }

        private static void WriteRetunedLevelJson(string jsonPath, string logPath, string assetPath, PlayLogAnalysis analysis, PokoLevelConfig levelConfig, int[] spawnWeights)
        {
            var body =
                "{\n" +
                $"  \"logPath\": \"{EscapeJson(logPath)}\",\n" +
                $"  \"assetPath\": \"{EscapeJson(assetPath)}\",\n" +
                $"  \"sourceDifficulty\": \"{EscapeJson(analysis.DifficultyLabel)}\",\n" +
                $"  \"sourceResult\": \"{EscapeJson(analysis.Result)}\",\n" +
                $"  \"levelId\": \"{EscapeJson(levelConfig.LevelId)}\",\n" +
                $"  \"width\": {levelConfig.Width},\n" +
                $"  \"height\": {levelConfig.Height},\n" +
                $"  \"tileTypes\": {levelConfig.TileTypes},\n" +
                $"  \"moveLimit\": {levelConfig.MoveLimit},\n" +
                $"  \"targetScore\": {levelConfig.TargetScore},\n" +
                $"  \"regularEnemyHp\": {levelConfig.RegularEnemyHp},\n" +
                $"  \"bossHp\": {levelConfig.BossHp},\n" +
                $"  \"balanceProfileId\": \"{EscapeJson(levelConfig.BalanceProfileId)}\",\n" +
                $"  \"spawnWeights\": [{FormatWeights(spawnWeights)}],\n" +
                $"  \"reason\": \"{EscapeJson(analysis.Action)}\"\n" +
                "}\n";

            WriteReport(jsonPath, body);
        }

        private static LevelExperimentVariant[] BuildExperimentVariants(string experimentId, string assetRoot, PlayLogAnalysis analysis)
        {
            var controlTileTypes = Mathf.Clamp(analysis.SuggestedTileTypes, 3, 6);
            var readabilityTileTypes = Mathf.Max(3, controlTileTypes - 1);
            var comboTileTypes = Mathf.Max(3, controlTileTypes - 1);

            var controlRegHp = analysis.SuggestedRegularEnemyHp;
            var controlBossHp = analysis.SuggestedBossHp;
            var readabilityRegHp = controlRegHp > 0 ? Mathf.CeilToInt(controlRegHp * 0.8f) : 0;
            var readabilityBossHp = controlBossHp > 0 ? Mathf.CeilToInt(controlBossHp * 0.8f) : 0;
            var comboRegHp = controlRegHp > 0 ? Mathf.CeilToInt(controlRegHp * 1.2f) : 0;
            var comboBossHp = controlBossHp > 0 ? Mathf.CeilToInt(controlBossHp * 1.2f) : 0;

            return new[]
            {
                CreateExperimentVariant(
                    experimentId,
                    assetRoot,
                    "control",
                    "Baseline retune",
                    "Use the deterministic playtest recommendation as the comparison anchor.",
                    "Clear rate, final score, and average chain length.",
                    analysis,
                    controlTileTypes,
                    analysis.SuggestedMoveLimit,
                    analysis.SuggestedTargetScore,
                    BuildSpawnWeights(controlTileTypes, analysis.DifficultyLabel),
                    controlRegHp,
                    controlBossHp,
                    "default"),
                CreateExperimentVariant(
                    experimentId,
                    assetRoot,
                    "readability",
                    "Hex path readability",
                    "Reduce color noise and add a small move buffer to test whether players understand valid 3+ links faster.",
                    "Invalid short releases and time-to-first valid clear.",
                    analysis,
                    readabilityTileTypes,
                    analysis.SuggestedMoveLimit + 2,
                    Mathf.Max(600, analysis.SuggestedTargetScore - 100),
                    BuildAssistedSpawnWeights(readabilityTileTypes),
                    readabilityRegHp,
                    readabilityBossHp,
                    "readable"),
                CreateExperimentVariant(
                    experimentId,
                    assetRoot,
                    "combo",
                    "Combo showcase",
                    "Favor readable same-color paths while raising the score goal to test whether chain satisfaction carries a portfolio capture.",
                    "Average chain length, average score per move, and clear feedback readability.",
                    analysis,
                    comboTileTypes,
                    Mathf.Max(8, analysis.SuggestedMoveLimit - 1),
                    analysis.SuggestedTargetScore + 300,
                    BuildAssistedSpawnWeights(comboTileTypes),
                    comboRegHp,
                    comboBossHp,
                    "combo")
            };
        }

        private static LevelExperimentVariant CreateExperimentVariant(
            string experimentId,
            string assetRoot,
            string suffix,
            string focus,
            string hypothesis,
            string metric,
            PlayLogAnalysis analysis,
            int tileTypes,
            int moveLimit,
            int targetScore,
            int[] spawnWeights,
            int regularEnemyHp = 0,
            int bossHp = 0,
            string balanceProfileId = "default")
        {
            var levelId = $"{experimentId}_{suffix}";
            var assetPath = $"{assetRoot}/{levelId}.asset";
            var levelConfig = ScriptableObject.CreateInstance<PokoLevelConfig>();
            levelConfig.Configure(
                levelId,
                analysis.Width,
                analysis.Height,
                tileTypes,
                true,
                moveLimit,
                targetScore,
                spawnWeights,
                regularEnemyHp,
                bossHp,
                balanceProfileId);

            return new LevelExperimentVariant(suffix, focus, hypothesis, metric, assetPath, levelConfig, spawnWeights);
        }

        private static void WriteExperimentMarkdownReport(
            string reportPath,
            string experimentId,
            string logPath,
            PlayLogAnalysis analysis,
            LevelExperimentVariant[] variants)
        {
            var body = new StringBuilder();
            body.AppendLine("# Game Designer Agent Level Experiment Plan");
            body.AppendLine();
            body.AppendLine("## Source");
            body.AppendLine();
            body.AppendLine($"- Experiment id: `{experimentId}`");
            body.AppendLine($"- Play log: `{logPath}`");
            body.AppendLine($"- Source level: `{analysis.LevelId}`");
            body.AppendLine($"- Source result: `{analysis.Result}`");
            body.AppendLine($"- Designer diagnosis: {analysis.Diagnosis}");
            body.AppendLine($"- Designer next action: {analysis.Action}");
            body.AppendLine();
            body.AppendLine("## Why This Step Exists");
            body.AppendLine();
            body.AppendLine("A game designer should compare hypotheses, not only emit one tuned level. These candidates turn the playtest read into a small experiment set for the next Unity play pass.");
            body.AppendLine();
            body.AppendLine("## Candidate Levels");
            body.AppendLine();

            foreach (var variant in variants)
            {
                body.AppendLine($"### {variant.Focus}");
                body.AppendLine();
                body.AppendLine($"- Variant: `{variant.Name}`");
                body.AppendLine($"- Asset: `{variant.AssetPath}`");
                body.AppendLine($"- Level id: `{variant.LevelConfig.LevelId}`");
                body.AppendLine($"- Tile types: `{variant.LevelConfig.TileTypes}`");
                body.AppendLine($"- Move limit: `{variant.LevelConfig.MoveLimit}`");
                body.AppendLine($"- Target score: `{variant.LevelConfig.TargetScore}`");
                body.AppendLine($"- Regular enemy HP: `{variant.LevelConfig.RegularEnemyHp}`");
                body.AppendLine($"- Boss HP: `{variant.LevelConfig.BossHp}`");
                body.AppendLine($"- Balance profile: `{variant.LevelConfig.BalanceProfileId}`");
                body.AppendLine($"- Spawn weights: `{FormatWeights(variant.SpawnWeights)}`");
                body.AppendLine($"- Hypothesis: {variant.Hypothesis}");
                body.AppendLine($"- Measure: {variant.Metric}");
                body.AppendLine();
            }

            body.AppendLine("## Next Play Pass");
            body.AppendLine();
            body.AppendLine("Apply one candidate with `tools\\poko-cli.cmd apply-level --assetPath <asset>` and compare the new play log against this plan.");
            WriteReport(reportPath, body.ToString());
        }

        private static void WriteExperimentJsonReport(
            string jsonPath,
            string experimentId,
            string logPath,
            PlayLogAnalysis analysis,
            LevelExperimentVariant[] variants)
        {
            var body = new StringBuilder();
            body.AppendLine("{");
            body.AppendLine($"  \"experimentId\": \"{EscapeJson(experimentId)}\",");
            body.AppendLine($"  \"logPath\": \"{EscapeJson(logPath)}\",");
            body.AppendLine($"  \"sourceLevelId\": \"{EscapeJson(analysis.LevelId)}\",");
            body.AppendLine($"  \"sourceDifficulty\": \"{EscapeJson(analysis.DifficultyLabel)}\",");
            body.AppendLine($"  \"sourceAction\": \"{EscapeJson(analysis.Action)}\",");
            body.AppendLine("  \"variants\": [");

            for (var index = 0; index < variants.Length; index++)
            {
                var variant = variants[index];
                body.AppendLine("    {");
                body.AppendLine($"      \"name\": \"{EscapeJson(variant.Name)}\",");
                body.AppendLine($"      \"focus\": \"{EscapeJson(variant.Focus)}\",");
                body.AppendLine($"      \"hypothesis\": \"{EscapeJson(variant.Hypothesis)}\",");
                body.AppendLine($"      \"metric\": \"{EscapeJson(variant.Metric)}\",");
                body.AppendLine($"      \"assetPath\": \"{EscapeJson(variant.AssetPath)}\",");
                body.AppendLine($"      \"levelId\": \"{EscapeJson(variant.LevelConfig.LevelId)}\",");
                body.AppendLine($"      \"tileTypes\": {variant.LevelConfig.TileTypes},");
                body.AppendLine($"      \"moveLimit\": {variant.LevelConfig.MoveLimit},");
                body.AppendLine($"      \"targetScore\": {variant.LevelConfig.TargetScore},");
                body.AppendLine($"      \"regularEnemyHp\": {variant.LevelConfig.RegularEnemyHp},");
                body.AppendLine($"      \"bossHp\": {variant.LevelConfig.BossHp},");
                body.AppendLine($"      \"balanceProfileId\": \"{EscapeJson(variant.LevelConfig.BalanceProfileId)}\",");
                body.AppendLine($"      \"spawnWeights\": [{FormatWeights(variant.SpawnWeights)}]");
                body.Append("    }");
                body.AppendLine(index == variants.Length - 1 ? string.Empty : ",");
            }

            body.AppendLine("  ]");
            body.AppendLine("}");
            WriteReport(jsonPath, body.ToString());
        }

        private static void WriteExperimentComparisonMarkdown(
            string reportPath,
            string experimentId,
            ExperimentResult[] results,
            ExperimentRecommendation recommendation)
        {
            var body = new StringBuilder();
            body.AppendLine("# Game Designer Agent Experiment Comparison");
            body.AppendLine();
            body.AppendLine("## Experiment");
            body.AppendLine();
            body.AppendLine($"- Experiment id: `{experimentId}`");
            body.AppendLine($"- Recommended variant: `{recommendation.VariantName}`");
            body.AppendLine($"- Recommendation: {recommendation.Reason}");
            body.AppendLine();
            body.AppendLine("## Candidate Results");
            body.AppendLine();

            foreach (var result in results)
            {
                body.AppendLine($"### {result.Focus}");
                body.AppendLine();
                body.AppendLine($"- Variant: `{result.Name}`");
                body.AppendLine($"- Play log: `{result.LogPath}`");
                body.AppendLine($"- Result: `{result.Analysis.Result}`");
                body.AppendLine($"- Final score: `{result.Analysis.FinalScore}`");
                body.AppendLine($"- Moves used: `{result.Analysis.MovesUsed}`");
                body.AppendLine($"- Valid moves: `{result.Analysis.ValidMoves}`");
                body.AppendLine($"- Invalid short releases: `{result.Analysis.InvalidMoves}`");
                body.AppendLine($"- Average chain length: `{result.Analysis.AverageChainLength.ToString("0.00", CultureInfo.InvariantCulture)}`");
                body.AppendLine($"- Average score per valid move: `{result.Analysis.AverageScorePerValidMove.ToString("0.00", CultureInfo.InvariantCulture)}`");
                body.AppendLine($"- Regular enemy HP: `{result.Analysis.SuggestedRegularEnemyHp}`");
                body.AppendLine($"- Boss HP: `{result.Analysis.SuggestedBossHp}`");
                body.AppendLine($"- Designer read: {result.Analysis.Diagnosis}");
                body.AppendLine();
            }

            body.AppendLine("## Next Decision");
            body.AppendLine();
            body.AppendLine(recommendation.NextStep);
            WriteReport(reportPath, body.ToString());
        }

        private static void WriteExperimentComparisonJson(
            string jsonPath,
            string experimentId,
            ExperimentResult[] results,
            ExperimentRecommendation recommendation)
        {
            var body = new StringBuilder();
            body.AppendLine("{");
            body.AppendLine($"  \"experimentId\": \"{EscapeJson(experimentId)}\",");
            body.AppendLine("  \"recommendation\": {");
            body.AppendLine($"    \"variant\": \"{EscapeJson(recommendation.VariantName)}\",");
            body.AppendLine($"    \"reason\": \"{EscapeJson(recommendation.Reason)}\",");
            body.AppendLine($"    \"nextStep\": \"{EscapeJson(recommendation.NextStep)}\"");
            body.AppendLine("  },");
            body.AppendLine("  \"results\": [");

            for (var index = 0; index < results.Length; index++)
            {
                var result = results[index];
                body.AppendLine("    {");
                body.AppendLine($"      \"variant\": \"{EscapeJson(result.Name)}\",");
                body.AppendLine($"      \"focus\": \"{EscapeJson(result.Focus)}\",");
                body.AppendLine($"      \"logPath\": \"{EscapeJson(result.LogPath)}\",");
                body.AppendLine($"      \"result\": \"{EscapeJson(result.Analysis.Result)}\",");
                body.AppendLine($"      \"finalScore\": {result.Analysis.FinalScore},");
                body.AppendLine($"      \"movesUsed\": {result.Analysis.MovesUsed},");
                body.AppendLine($"      \"validMoves\": {result.Analysis.ValidMoves},");
                body.AppendLine($"      \"invalidMoves\": {result.Analysis.InvalidMoves},");
                body.AppendLine($"      \"averageChainLength\": {result.Analysis.AverageChainLength.ToString("0.00", CultureInfo.InvariantCulture)},");
                body.AppendLine($"      \"averageScorePerValidMove\": {result.Analysis.AverageScorePerValidMove.ToString("0.00", CultureInfo.InvariantCulture)},");
                body.AppendLine($"      \"suggestedRegularEnemyHp\": {result.Analysis.SuggestedRegularEnemyHp},");
                body.AppendLine($"      \"suggestedBossHp\": {result.Analysis.SuggestedBossHp}");
                body.Append("    }");
                body.AppendLine(index == results.Length - 1 ? string.Empty : ",");
            }

            body.AppendLine("  ]");
            body.AppendLine("}");
            WriteReport(jsonPath, body.ToString());
        }

        private static void WritePromotionReport(
            string reportPath,
            string experimentId,
            string sourceAssetPath,
            string assetPath,
            PokoLevelConfig promotedLevel,
            ExperimentResult[] results,
            ExperimentRecommendation recommendation,
            bool applyScene,
            string scenePath)
        {
            var body = new StringBuilder();
            body.AppendLine("# Promoted Experiment Winner");
            body.AppendLine();
            body.AppendLine("## Decision");
            body.AppendLine();
            body.AppendLine($"- Experiment id: `{experimentId}`");
            body.AppendLine($"- Promoted variant: `{recommendation.VariantName}`");
            body.AppendLine($"- Reason: {recommendation.Reason}");
            body.AppendLine($"- Source asset: `{sourceAssetPath}`");
            body.AppendLine($"- Promoted asset: `{assetPath}`");
            body.AppendLine($"- Scene applied: `{(applyScene ? "YES" : "NO")}`");
            if (applyScene)
            {
                body.AppendLine($"- Scene path: `{scenePath}`");
            }

            body.AppendLine();
            body.AppendLine("## Promoted Level Values");
            body.AppendLine();
            body.AppendLine($"- Level id: `{promotedLevel.LevelId}`");
            body.AppendLine($"- Board size: `{promotedLevel.Width}x{promotedLevel.Height}`");
            body.AppendLine($"- Tile types: `{promotedLevel.TileTypes}`");
            body.AppendLine($"- Move limit: `{promotedLevel.MoveLimit}`");
            body.AppendLine($"- Target score: `{promotedLevel.TargetScore}`");
            body.AppendLine($"- Balance profile: `{promotedLevel.BalanceProfileId}`");
            body.AppendLine($"- Spawn weights: `{FormatWeights(promotedLevel.SpawnWeights)}`");
            body.AppendLine();
            body.AppendLine("## Supporting Experiment Results");
            body.AppendLine();
            AppendExperimentResultSummary(body, results);
            WriteReport(reportPath, body.ToString());
        }

        private static void WritePortfolioMilestone(
            string milestonePath,
            string experimentId,
            string assetPath,
            PokoLevelConfig promotedLevel,
            ExperimentResult[] results,
            ExperimentRecommendation recommendation)
        {
            var body = new StringBuilder();
            body.AppendLine("# Portfolio Milestone: AI Designer Experiment Loop");
            body.AppendLine();
            body.AppendLine("## Milestone");
            body.AppendLine();
            body.AppendLine($"- Experiment: `{experimentId}`");
            body.AppendLine($"- Promoted variant: `{recommendation.VariantName}`");
            body.AppendLine($"- Playable level asset: `{assetPath}`");
            body.AppendLine();
            body.AppendLine("## What A Reviewer Can Play");
            body.AppendLine();
            body.AppendLine($"A promoted Line-Linker level with `{promotedLevel.TileTypes}` tile types, `{promotedLevel.MoveLimit}` moves, and a `{promotedLevel.TargetScore}` score target.");
            body.AppendLine();
            body.AppendLine("## What The AI Designer Did");
            body.AppendLine();
            body.AppendLine("The designer loop analyzed play telemetry, planned control/readability/combo candidates, compared level-specific play logs, and promoted the strongest next baseline.");
            body.AppendLine();
            body.AppendLine($"Recommendation: {recommendation.Reason}");
            body.AppendLine();
            body.AppendLine("## Evidence");
            body.AppendLine();
            AppendExperimentResultSummary(body, results);
            body.AppendLine("## Next Capture");
            body.AppendLine();
            body.AppendLine(recommendation.NextStep);
            WriteReport(milestonePath, body.ToString());
        }

        private static void WriteDesignerLoopStatusMarkdown(string reportPath, DesignerLoopState status)
        {
            var body = new StringBuilder();
            body.AppendLine("# Game Designer Agent Loop Status");
            body.AppendLine();
            body.AppendLine("## Current Stage");
            body.AppendLine();
            body.AppendLine($"- Stage: `{status.Stage}`");
            body.AppendLine($"- Experiment id: `{status.ExperimentId}`");
            body.AppendLine($"- Next action: {status.NextAction}");
            body.AppendLine();
            body.AppendLine("## Evidence Check");
            body.AppendLine();
            body.AppendLine($"- Latest play log: `{StatusMark(status.HasLatestLog)}` `{status.LatestLogPath}`");
            body.AppendLine($"- Playtest analysis JSON: `{StatusMark(status.HasAnalysis)}` `{status.AnalysisPath}`");
            body.AppendLine($"- Experiment plan JSON: `{StatusMark(status.HasPlan)}` `{status.PlanPath}`");
            body.AppendLine($"- Control candidate log: `{StatusMark(status.HasControlLog)}` `{status.ControlLogPath}`");
            body.AppendLine($"- Readability candidate log: `{StatusMark(status.HasReadabilityLog)}` `{status.ReadabilityLogPath}`");
            body.AppendLine($"- Combo candidate log: `{StatusMark(status.HasComboLog)}` `{status.ComboLogPath}`");
            body.AppendLine($"- Experiment comparison JSON: `{StatusMark(status.HasComparison)}` `{status.ComparisonPath}`");
            body.AppendLine($"- Promotion report: `{StatusMark(status.HasPromotion)}` `{status.PromotionPath}`");
            body.AppendLine();
            body.AppendLine("## Suggested Commands");
            body.AppendLine();
            foreach (var command in status.Commands)
            {
                body.AppendLine($"- `{command}`");
            }

            body.AppendLine();
            body.AppendLine("## Why This Matters");
            body.AppendLine();
            body.AppendLine("This status report lets the designer agent explain where the tuning loop is blocked before another implementation or playtest step begins.");
            WriteReport(reportPath, body.ToString());
        }

        private static void WriteDesignerLoopStatusJson(string jsonPath, DesignerLoopState status)
        {
            var body = new StringBuilder();
            body.AppendLine("{");
            body.AppendLine($"  \"stage\": \"{EscapeJson(status.Stage)}\",");
            body.AppendLine($"  \"experimentId\": \"{EscapeJson(status.ExperimentId)}\",");
            body.AppendLine($"  \"nextAction\": \"{EscapeJson(status.NextAction)}\",");
            body.AppendLine("  \"evidence\": {");
            body.AppendLine($"    \"latestLog\": {JsonBoolean(status.HasLatestLog)},");
            body.AppendLine($"    \"analysis\": {JsonBoolean(status.HasAnalysis)},");
            body.AppendLine($"    \"plan\": {JsonBoolean(status.HasPlan)},");
            body.AppendLine($"    \"controlLog\": {JsonBoolean(status.HasControlLog)},");
            body.AppendLine($"    \"readabilityLog\": {JsonBoolean(status.HasReadabilityLog)},");
            body.AppendLine($"    \"comboLog\": {JsonBoolean(status.HasComboLog)},");
            body.AppendLine($"    \"comparison\": {JsonBoolean(status.HasComparison)},");
            body.AppendLine($"    \"promotion\": {JsonBoolean(status.HasPromotion)}");
            body.AppendLine("  },");
            body.AppendLine("  \"commands\": [");
            for (var index = 0; index < status.Commands.Length; index++)
            {
                body.Append($"    \"{EscapeJson(status.Commands[index])}\"");
                body.AppendLine(index == status.Commands.Length - 1 ? string.Empty : ",");
            }

            body.AppendLine("  ]");
            body.AppendLine("}");
            WriteReport(jsonPath, body.ToString());
        }

        private static string StatusMark(bool available)
        {
            return available ? "READY" : "MISSING";
        }

        private static string JsonBoolean(bool value)
        {
            return value ? "true" : "false";
        }

        private static void AppendExperimentResultSummary(StringBuilder body, ExperimentResult[] results)
        {
            foreach (var result in results)
            {
                body.AppendLine($"- `{result.Name}`: result `{result.Analysis.Result}`, score `{result.Analysis.FinalScore}`, invalid short releases `{result.Analysis.InvalidMoves}`, average chain `{result.Analysis.AverageChainLength.ToString("0.00", CultureInfo.InvariantCulture)}`");
            }

            body.AppendLine();
        }

        private static ExperimentRecommendation ChooseExperimentRecommendation(ExperimentResult[] results)
        {
            var readability = FindExperimentResult(results, "readability");
            var combo = FindExperimentResult(results, "combo");
            var control = FindExperimentResult(results, "control");

            if (readability.Analysis.InvalidMoves < control.Analysis.InvalidMoves &&
                readability.Analysis.AverageChainLength >= control.Analysis.AverageChainLength)
            {
                return new ExperimentRecommendation(
                    readability.Name,
                    "Readability reduced short invalid releases without losing chain quality.",
                    "Promote the readability candidate for the next scene apply pass, then test whether extra feedback polish can preserve the improvement with stricter score pressure.");
            }

            if (combo.Analysis.AverageChainLength > control.Analysis.AverageChainLength &&
                combo.Analysis.AverageScorePerValidMove >= control.Analysis.AverageScorePerValidMove)
            {
                return new ExperimentRecommendation(
                    combo.Name,
                    "Combo candidate improved chain and score feel metrics against the baseline.",
                    "Use the combo candidate for the portfolio capture pass and review whether its raised target score still leaves a clear win arc.");
            }

            return new ExperimentRecommendation(
                control.Name,
                "Focused variants did not beat the baseline metrics strongly enough yet.",
                "Keep the control candidate as the next playable baseline and collect another play log before widening the experiment set.");
        }

        private static ExperimentResult FindExperimentResult(ExperimentResult[] results, string name)
        {
            foreach (var result in results)
            {
                if (string.Equals(result.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return result;
                }
            }

            throw new InvalidOperationException($"Experiment result '{name}' is missing.");
        }

        private static ExperimentResult[] LoadExperimentResults(string experimentId, CliArgs args)
        {
            var controlLog = args.GetString("controlLog", ExperimentLogPath(experimentId, "control"));
            var readabilityLog = args.GetString("readabilityLog", ExperimentLogPath(experimentId, "readability"));
            var comboLog = args.GetString("comboLog", ExperimentLogPath(experimentId, "combo"));

            RequirePlayLog(controlLog, "control");
            RequirePlayLog(readabilityLog, "readability");
            RequirePlayLog(comboLog, "combo");

            return new[]
            {
                new ExperimentResult("control", "Baseline retune", controlLog, PlayLogAnalysis.FromFile(controlLog)),
                new ExperimentResult("readability", "Hex path readability", readabilityLog, PlayLogAnalysis.FromFile(readabilityLog)),
                new ExperimentResult("combo", "Combo showcase", comboLog, PlayLogAnalysis.FromFile(comboLog))
            };
        }

        private static string ExperimentLogPath(string experimentId, string variant)
        {
            return $"md/playtest-logs/by-level/{experimentId}_{variant}-latest.jsonl";
        }

        private static string ExperimentAssetPath(string experimentId, string variant)
        {
            return $"Assets/PokoPuzzle/Data/Generated/Experiments/{experimentId}_{variant}.asset";
        }

        private static void RequirePlayLog(string logPath, string variant)
        {
            if (!File.Exists(logPath))
            {
                Fail($"No {variant} experiment play log found at {logPath}. Apply and play that candidate first.");
            }
        }

        private static string BuildLlmDesignerRequest(string model, string sourceJson)
        {
            var instructions =
                "You are the Game Designer Agent for a bright mobile line-link puzzle. " +
                "Review telemetry, explain the player-facing feel, identify risk, and propose a next tuning pass. " +
                "Do not copy protected PokoPang art, names, characters, UI, or content. " +
                "Return concise Korean Markdown with sections: Diagnosis, Player Feel, Risk, Next Level Tuning, Portfolio Note.";
            var input =
                "Review this puzzle telemetry JSON and produce a designer report. " +
                "Keep tuning suggestions compatible with width, height, tileTypes, moveLimit, targetScore, and spawnWeights.\\n\\n" +
                sourceJson;

            return
                "{\n" +
                $"  \"model\": \"{EscapeJson(model)}\",\n" +
                $"  \"instructions\": \"{EscapeJson(instructions)}\",\n" +
                $"  \"input\": \"{EscapeJson(input)}\"\n" +
                "}\n";
        }

        private static void WritePendingLlmReport(string reportPath, string model, string inputPath, string requestPath)
        {
            var body =
                "# Pending LLM Designer Review\n\n" +
                "## Status\n\n" +
                "- API call: `SKIPPED`\n" +
                "- Reason: `OPENAI_API_KEY` is not set.\n" +
                $"- Model: `{model}`\n" +
                $"- Input JSON: `{inputPath}`\n" +
                $"- Saved Responses request: `{requestPath}`\n\n" +
                "## Next Runtime Step\n\n" +
                "Set `OPENAI_API_KEY` for the Unity batchmode process and rerun `tools\\poko-cli.cmd llm-design-review` to save the LLM-written designer report and raw response.\n";

            WriteReport(reportPath, body);
        }

        private static void WriteLlmDesignerReview(string reportPath, string model, string inputPath, string outputText, string rawResponsePath)
        {
            var body =
                "# LLM Game Designer Review\n\n" +
                "## Source\n\n" +
                $"- Model: `{model}`\n" +
                $"- Input JSON: `{inputPath}`\n" +
                $"- Raw Responses API output: `{rawResponsePath}`\n\n" +
                "## Agent Review\n\n" +
                outputText +
                "\n";

            WriteReport(reportPath, body);
        }

        private static string ExtractResponseText(string responseBody)
        {
            var response = JsonUtility.FromJson<ResponsesEnvelope>(responseBody);
            if (response == null || response.output == null)
            {
                return "The response did not contain an extractable output text block. Inspect the raw response JSON.";
            }

            var builder = new StringBuilder();
            foreach (var item in response.output)
            {
                if (item == null || item.content == null)
                {
                    continue;
                }

                foreach (var content in item.content)
                {
                    if (content == null ||
                        !string.Equals(content.type, "output_text", StringComparison.OrdinalIgnoreCase) ||
                        string.IsNullOrWhiteSpace(content.text))
                    {
                        continue;
                    }

                    if (builder.Length > 0)
                    {
                        builder.AppendLine();
                        builder.AppendLine();
                    }

                    builder.Append(content.text.Trim());
                }
            }

            return builder.Length == 0
                ? "The response did not contain an extractable output text block. Inspect the raw response JSON."
                : builder.ToString();
        }

        private static void SaveLevelAsset(string assetPath, PokoLevelConfig levelConfig)
        {
            var directory = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var existing = AssetDatabase.LoadAssetAtPath<PokoLevelConfig>(assetPath);
            if (existing != null)
            {
                EditorUtility.CopySerialized(levelConfig, existing);
                EditorUtility.SetDirty(existing);
            }
            else
            {
                AssetDatabase.CreateAsset(levelConfig, assetPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static int[] BuildSpawnWeights(int tileTypes, string difficultyLabel)
        {
            var weights = new int[tileTypes];
            for (var index = 0; index < tileTypes; index++)
            {
                weights[index] = 100;
            }

            if (string.Equals(difficultyLabel, "Hard", StringComparison.OrdinalIgnoreCase))
            {
                weights[0] = 125;
                weights[1] = 125;
            }
            else if (string.Equals(difficultyLabel, "Easy", StringComparison.OrdinalIgnoreCase))
            {
                weights[tileTypes - 1] = 115;
            }

            return weights;
        }

        private static int[] BuildAssistedSpawnWeights(int tileTypes)
        {
            var weights = BuildSpawnWeights(tileTypes, "Normal");
            if (tileTypes > 0)
            {
                weights[0] = 125;
            }

            if (tileTypes > 1)
            {
                weights[1] = 125;
            }

            return weights;
        }

        private static string FormatWeights(int[] weights)
        {
            var builder = new StringBuilder();
            for (var index = 0; index < weights.Length; index++)
            {
                if (index > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(weights[index].ToString(CultureInfo.InvariantCulture));
            }

            return builder.ToString();
        }

        private static int[,] GenerateBoard(int width, int height, int tileTypes, int seed)
        {
            var random = new System.Random(seed);
            var board = new int[width, height];
            for (var column = 0; column < width; column++)
            {
                for (var row = 0; row < height; row++)
                {
                    board[column, row] = random.Next(0, tileTypes);
                }
            }

            return board;
        }

        private static int CountPossibleChains(int[,] board, int tileTypes)
        {
            var width = board.GetLength(0);
            var height = board.GetLength(1);
            var count = 0;
            for (var column = 0; column < width; column++)
            {
                for (var row = 0; row < height; row++)
                {
                    if (CountSameNeighbors(board, column, row) >= 2)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static int FindLongestChain(int[,] board, int tileTypes)
        {
            var width = board.GetLength(0);
            var height = board.GetLength(1);
            var longest = 0;
            for (var column = 0; column < width; column++)
            {
                for (var row = 0; row < height; row++)
                {
                    longest = Mathf.Max(longest, EstimateFloodSize(board, column, row));
                }
            }

            return longest;
        }

        private static int EstimateFloodSize(int[,] board, int startColumn, int startRow)
        {
            var width = board.GetLength(0);
            var height = board.GetLength(1);
            var targetType = board[startColumn, startRow];
            var visited = new bool[width, height];
            var stack = new System.Collections.Generic.Stack<Vector2Int>();
            stack.Push(new Vector2Int(startColumn, startRow));
            visited[startColumn, startRow] = true;
            var count = 0;

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                count++;

                foreach (var next in HexGridUtility.GetNeighbors(current.x, current.y, width, height))
                {
                    if (visited[next.x, next.y] || board[next.x, next.y] != targetType)
                    {
                        continue;
                    }

                    visited[next.x, next.y] = true;
                    stack.Push(next);
                }
            }

            return count;
        }

        private static int CountSameNeighbors(int[,] board, int column, int row)
        {
            var width = board.GetLength(0);
            var height = board.GetLength(1);
            var targetType = board[column, row];
            var count = 0;

            foreach (var next in HexGridUtility.GetNeighbors(column, row, width, height))
            {
                if (board[next.x, next.y] == targetType)
                {
                    count++;
                }
            }

            return count;
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

        private static void ValidateAdjacency()
        {
            if (!HexGridUtility.AreAdjacent(1, 2, 2, 2) ||
                !HexGridUtility.AreAdjacent(1, 2, 0, 2) ||
                !HexGridUtility.AreAdjacent(1, 2, 1, 3) ||
                !HexGridUtility.AreAdjacent(1, 2, 2, 3) ||
                !HexGridUtility.AreAdjacent(1, 2, 1, 1) ||
                !HexGridUtility.AreAdjacent(1, 2, 2, 1) ||
                HexGridUtility.AreAdjacent(1, 2, 0, 1))
            {
                Fail("Even-row (3-tile) hex adjacency validation failed.");
            }

            if (!HexGridUtility.AreAdjacent(2, 3, 3, 3) ||
                !HexGridUtility.AreAdjacent(2, 3, 1, 3) ||
                !HexGridUtility.AreAdjacent(2, 3, 2, 4) ||
                !HexGridUtility.AreAdjacent(2, 3, 1, 4) ||
                !HexGridUtility.AreAdjacent(2, 3, 2, 2) ||
                !HexGridUtility.AreAdjacent(2, 3, 1, 2) ||
                HexGridUtility.AreAdjacent(2, 3, 3, 4))
            {
                Fail("Odd-row (4-tile) hex adjacency validation failed.");
            }
        }

        private static string LayoutName(bool useHexGrid)
        {
            return useHexGrid ? "hex" : "square";
        }

        private static string TileVisualName(PokoTileVisualStyle visualStyle)
        {
            return visualStyle == PokoTileVisualStyle.CircleInHex ? "circle-in-hex" : "hex";
        }

        private static void WriteReport(string reportPath, string body)
        {
            var directory = Path.GetDirectoryName(reportPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(reportPath, body);
            AssetDatabase.Refresh();
        }

        private static void Fail(string message)
        {
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }

    }
}
#endif
