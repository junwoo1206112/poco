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
                spawnWeights);

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
                analysis.SuggestedBossHp);

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
            promotedLevel.Configure(levelId, sourceLevel.Width, sourceLevel.Height, sourceLevel.TileTypes, sourceLevel.UseHexGrid, sourceLevel.MoveLimit, sourceLevel.TargetScore, sourceLevel.SpawnWeights);
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
            var model = args.GetString("model", "gpt-5.4-mini");
            var requestPath = args.GetString("requestPath", "md/llm-reports/latest-designer-request.json");
            var reportPath = args.GetString("reportPath", "md/llm-reports/latest-designer-review.md");
            var rawResponsePath = args.GetString("rawResponsePath", "md/llm-reports/latest-designer-response.json");

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
            var response = client.PostAsync("https://api.openai.com/v1/responses", content).GetAwaiter().GetResult();
            var responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            WriteReport(rawResponsePath, responseBody);

            if (!response.IsSuccessStatusCode)
            {
                Fail($"OpenAI Responses API request failed: {(int)response.StatusCode}. Raw response saved to {rawResponsePath}");
            }

            WriteLlmDesignerReview(reportPath, model, inputPath, ExtractResponseText(responseBody), rawResponsePath);
            Debug.Log($"Poko CLI saved LLM designer review: {reportPath}");
        }

        public static void ConvertExcelData()
        {
            ExcelDataGenerator.ConvertAll();
            Debug.Log("Poko CLI converted Excel data into ScriptableObject databases.");
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
                    var tileCount = HexGridUtility.RowSize(row);
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
                 $"- Rainbow tiles cleared: `{analysis.RainbowCleared}`\n\n" +
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
                    controlBossHp),
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
                    readabilityBossHp),
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
                    comboBossHp)
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
            int bossHp = 0)
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
                bossHp);

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

                foreach (var next in HexGridUtility.GetNeighbors(current.x, current.y, height))
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
            var height = board.GetLength(1);
            var targetType = board[column, row];
            var count = 0;

            foreach (var next in HexGridUtility.GetNeighbors(column, row, height))
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

        [Serializable]
        private sealed class ResponsesEnvelope
        {
            public ResponsesOutputItem[] output = Array.Empty<ResponsesOutputItem>();
        }

        [Serializable]
        private sealed class ResponsesOutputItem
        {
            public ResponsesContentItem[] content = Array.Empty<ResponsesContentItem>();
        }

        [Serializable]
        private sealed class ResponsesContentItem
        {
            public string type = string.Empty;
            public string text = string.Empty;
        }

        private sealed class LevelExperimentVariant
        {
            public string Name { get; }
            public string Focus { get; }
            public string Hypothesis { get; }
            public string Metric { get; }
            public string AssetPath { get; }
            public PokoLevelConfig LevelConfig { get; }
            public int[] SpawnWeights { get; }

            public LevelExperimentVariant(
                string name,
                string focus,
                string hypothesis,
                string metric,
                string assetPath,
                PokoLevelConfig levelConfig,
                int[] spawnWeights)
            {
                Name = name;
                Focus = focus;
                Hypothesis = hypothesis;
                Metric = metric;
                AssetPath = assetPath;
                LevelConfig = levelConfig;
                SpawnWeights = spawnWeights;
            }
        }

        private sealed class ExperimentResult
        {
            public string Name { get; }
            public string Focus { get; }
            public string LogPath { get; }
            public PlayLogAnalysis Analysis { get; }

            public ExperimentResult(string name, string focus, string logPath, PlayLogAnalysis analysis)
            {
                Name = name;
                Focus = focus;
                LogPath = logPath;
                Analysis = analysis;
            }
        }

        private sealed class ExperimentRecommendation
        {
            public string VariantName { get; }
            public string Reason { get; }
            public string NextStep { get; }

            public ExperimentRecommendation(string variantName, string reason, string nextStep)
            {
                VariantName = variantName;
                Reason = reason;
                NextStep = nextStep;
            }
        }

        private sealed class DesignerLoopState
        {
            public string Stage { get; private set; } = string.Empty;
            public string ExperimentId { get; private set; } = string.Empty;
            public string NextAction { get; private set; } = string.Empty;
            public string LatestLogPath { get; private set; } = string.Empty;
            public string AnalysisPath { get; private set; } = string.Empty;
            public string PlanPath { get; private set; } = string.Empty;
            public string ComparisonPath { get; private set; } = string.Empty;
            public string PromotionPath { get; private set; } = string.Empty;
            public string ControlLogPath { get; private set; } = string.Empty;
            public string ReadabilityLogPath { get; private set; } = string.Empty;
            public string ComboLogPath { get; private set; } = string.Empty;
            public bool HasLatestLog { get; private set; }
            public bool HasAnalysis { get; private set; }
            public bool HasPlan { get; private set; }
            public bool HasComparison { get; private set; }
            public bool HasPromotion { get; private set; }
            public bool HasControlLog { get; private set; }
            public bool HasReadabilityLog { get; private set; }
            public bool HasComboLog { get; private set; }
            public string[] Commands { get; private set; } = Array.Empty<string>();

            public static DesignerLoopState Inspect(
                string experimentId,
                string latestLog,
                string analysisPath,
                string planPath,
                string comparisonPath,
                string promotionPath)
            {
                var state = new DesignerLoopState
                {
                    ExperimentId = experimentId,
                    LatestLogPath = latestLog,
                    AnalysisPath = analysisPath,
                    PlanPath = planPath,
                    ComparisonPath = comparisonPath,
                    PromotionPath = promotionPath,
                    ControlLogPath = ExperimentLogPath(experimentId, "control"),
                    ReadabilityLogPath = ExperimentLogPath(experimentId, "readability"),
                    ComboLogPath = ExperimentLogPath(experimentId, "combo")
                };

                state.HasLatestLog = File.Exists(state.LatestLogPath);
                state.HasAnalysis = File.Exists(state.AnalysisPath);
                state.HasPlan = File.Exists(state.PlanPath);
                state.HasComparison = File.Exists(state.ComparisonPath);
                state.HasPromotion = File.Exists(state.PromotionPath);
                state.HasControlLog = File.Exists(state.ControlLogPath);
                state.HasReadabilityLog = File.Exists(state.ReadabilityLogPath);
                state.HasComboLog = File.Exists(state.ComboLogPath);
                state.Evaluate();
                return state;
            }

            private void Evaluate()
            {
                if (!HasLatestLog)
                {
                    Stage = "Need Playtest Log";
                    NextAction = "Play the prototype once so the designer loop has telemetry to analyze.";
                    Commands = new[] { "Play the Unity prototype until md/playtest-logs/latest-playtest.jsonl exists." };
                    return;
                }

                if (!HasAnalysis)
                {
                    Stage = "Ready To Analyze";
                    NextAction = "Convert the latest play log into designer analysis evidence.";
                    Commands = new[] { "tools\\poko-cli.cmd analyze-playlog" };
                    return;
                }

                if (!HasPlan)
                {
                    Stage = "Ready To Plan Experiments";
                    NextAction = "Generate control, readability, and combo candidates for the next play pass.";
                    Commands = new[] { $"tools\\poko-cli.cmd plan-level-experiments --experimentId {ExperimentId}" };
                    return;
                }

                if (!HasControlLog || !HasReadabilityLog || !HasComboLog)
                {
                    Stage = "Need Candidate Plays";
                    NextAction = "Apply and play every planned experiment candidate so level-specific logs exist.";
                    Commands = new[]
                    {
                        $"tools\\poko-cli.cmd apply-level --assetPath {ExperimentAssetPath(ExperimentId, "control")}",
                        $"tools\\poko-cli.cmd apply-level --assetPath {ExperimentAssetPath(ExperimentId, "readability")}",
                        $"tools\\poko-cli.cmd apply-level --assetPath {ExperimentAssetPath(ExperimentId, "combo")}"
                    };
                    return;
                }

                if (!HasComparison)
                {
                    Stage = "Ready To Compare";
                    NextAction = "Compare candidate logs and let the designer agent recommend a next baseline.";
                    Commands = new[] { $"tools\\poko-cli.cmd compare-level-experiments --experimentId {ExperimentId}" };
                    return;
                }

                if (!HasPromotion)
                {
                    Stage = "Ready To Promote";
                    NextAction = "Promote the recommended candidate and save portfolio milestone evidence.";
                    Commands = new[] { $"tools\\poko-cli.cmd promote-experiment-winner --experimentId {ExperimentId} --applyScene true" };
                    return;
                }

                Stage = "Loop Complete";
                NextAction = "Capture the promoted level and start the next playtest cycle from the new baseline.";
                Commands = new[] { "Play the promoted Unity level and capture portfolio evidence." };
            }
        }

        private readonly struct CliArgs
        {
            private readonly string[] args;

            private CliArgs(string[] args)
            {
                this.args = args;
            }

            public static CliArgs Parse(string[] args)
            {
                return new CliArgs(args);
            }

            public string GetString(string name, string fallback)
            {
                var token = $"--{name}";
                for (var index = 0; index < args.Length - 1; index++)
                {
                    if (string.Equals(args[index], token, StringComparison.OrdinalIgnoreCase))
                    {
                        return args[index + 1];
                    }
                }

                return fallback;
            }

            public bool GetBool(string name, bool fallback)
            {
                var value = GetString(name, fallback ? "true" : "false");
                if (bool.TryParse(value, out var parsed))
                {
                    return parsed;
                }

                if (string.Equals(value, "1", StringComparison.Ordinal))
                {
                    return true;
                }

                if (string.Equals(value, "0", StringComparison.Ordinal))
                {
                    return false;
                }

                return fallback;
            }

            public int GetInt(string name, int fallback)
            {
                return int.TryParse(GetString(name, string.Empty), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                    ? value
                    : fallback;
            }

            public float GetFloat(string name, float fallback)
            {
                return float.TryParse(GetString(name, string.Empty), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                    ? value
                    : fallback;
            }

            public bool GetLayout(string name, bool fallback)
            {
                var value = GetString(name, fallback ? "hex" : "square");
                if (string.Equals(value, "hex", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (string.Equals(value, "square", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                throw new InvalidOperationException($"Unsupported layout '{value}'. Use 'hex' or 'square'.");
            }

            public PokoTileVisualStyle GetTileVisualStyle(string name, PokoTileVisualStyle fallback)
            {
                var value = GetString(name, TileVisualName(fallback));
                if (string.Equals(value, "hex", StringComparison.OrdinalIgnoreCase))
                {
                    return PokoTileVisualStyle.Hex;
                }

                if (string.Equals(value, "circle-in-hex", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "circleInHex", StringComparison.OrdinalIgnoreCase))
                {
                    return PokoTileVisualStyle.CircleInHex;
                }

                throw new InvalidOperationException($"Unsupported tile visual '{value}'. Use 'hex' or 'circle-in-hex'.");
            }
        }

        private sealed class PlayLogAnalysis
        {
            public string LevelId { get; private set; } = "unknown";
            public int Width { get; private set; }
            public int Height { get; private set; }
            public int TileTypes { get; private set; }
            public int MoveLimit { get; private set; }
            public int TimeLimit { get; private set; }
            public int TargetScore { get; private set; }
            public string Result { get; private set; } = "unfinished";
            public int FinalScore { get; private set; }
            public int MovesUsed { get; private set; }
            public int ValidMoves { get; private set; }
            public int InvalidMoves { get; private set; }
            public float AverageChainLength { get; private set; }
            public float AverageScorePerValidMove { get; private set; }
            public string DifficultyLabel { get; private set; } = "Normal";
            public string Diagnosis { get; private set; } = "Playtest data is balanced enough for the next pass.";
            public string Risk { get; private set; } = "No strong risk detected.";
            public string Action { get; private set; } = "Continue with visual polish and one more playtest.";
            public int SuggestedMoveLimit { get; private set; }
            public int SuggestedTargetScore { get; private set; }
            public int SuggestedTileTypes { get; private set; }
            public int MaxCombo { get; private set; }
            public int FeverTriggers { get; private set; }
            public int TotalDamageDealt { get; private set; }
            public int BombsGenerated { get; private set; }
            public int BombsDetonated { get; private set; }
            public int SpecialBlocksCleared { get; private set; }
            public int RainbowCleared { get; private set; }
            public int TimeLeft { get; private set; }
            public int SuggestedRegularEnemyHp { get; private set; }
            public int SuggestedBossHp { get; private set; }

            public static PlayLogAnalysis FromFile(string logPath)
            {
                var analysis = new PlayLogAnalysis();
                var totalChainLength = 0;
                var totalGainedScore = 0;

                foreach (var line in File.ReadAllLines(logPath))
                {
                    if (line.Contains("\"event\":\"session_start\""))
                    {
                        analysis.LevelId = GetString(line, "levelId", analysis.LevelId);
                        analysis.Width = GetInt(line, "width", analysis.Width);
                        analysis.Height = GetInt(line, "height", analysis.Height);
                        analysis.TileTypes = GetInt(line, "tileTypes", analysis.TileTypes);
                        analysis.MoveLimit = GetInt(line, "moveLimit", analysis.MoveLimit);
                        analysis.TimeLimit = GetInt(line, "timeLimit", analysis.TimeLimit);
                        analysis.TargetScore = GetInt(line, "targetScore", analysis.TargetScore);
                    }
                    else if (line.Contains("\"event\":\"move\""))
                    {
                        var valid = GetBool(line, "valid");
                        var chainLength = GetInt(line, "chainLength", 0);
                        var gainedScore = GetInt(line, "gainedScore", 0);
                        analysis.FinalScore = GetInt(line, "score", analysis.FinalScore);
                        analysis.MovesUsed = GetInt(line, "movesUsed", analysis.MovesUsed);

                        if (valid)
                        {
                            analysis.ValidMoves++;
                            totalChainLength += chainLength;
                            totalGainedScore += gainedScore;
                        }
                        else
                        {
                            analysis.InvalidMoves++;
                        }
                    }
                    else if (line.Contains("\"event\":\"end\""))
                    {
                        analysis.Result = GetString(line, "result", analysis.Result);
                        analysis.FinalScore = GetInt(line, "score", analysis.FinalScore);
                        analysis.MovesUsed = GetInt(line, "movesUsed", analysis.MovesUsed);
                        analysis.TimeLeft = GetInt(line, "timeLeft", 0);
                    }
                    else if (line.Contains("\"event\":\"combat\""))
                    {
                        var combatEvent = GetString(line, "combatEvent", "");
                        switch (combatEvent)
                        {
                            case "enemy_damage":
                                analysis.TotalDamageDealt += GetInt(line, "value1", 0);
                                break;
                            case "bomb_placed":
                                analysis.BombsGenerated++;
                                break;
                            case "bomb_detonate":
                                analysis.BombsDetonated++;
                                break;
                            case "rainbow_cleared":
                                analysis.RainbowCleared++;
                                break;
                        }
                    }
                    else if (line.Contains("\"event\":\"fever\""))
                    {
                        if (string.Equals(GetString(line, "state", ""), "start", StringComparison.OrdinalIgnoreCase))
                        {
                            analysis.FeverTriggers++;
                        }
                    }
                    else if (line.Contains("\"combat\""))
                    {
                        var combo = GetInt(line, "combo", 0);
                        if (combo > analysis.MaxCombo)
                        {
                            analysis.MaxCombo = combo;
                        }
                    }
                }

                analysis.AverageChainLength = analysis.ValidMoves == 0 ? 0f : (float)totalChainLength / analysis.ValidMoves;
                analysis.AverageScorePerValidMove = analysis.ValidMoves == 0 ? 0f : (float)totalGainedScore / analysis.ValidMoves;
                analysis.SuggestedMoveLimit = analysis.MoveLimit;
                analysis.SuggestedTargetScore = analysis.TargetScore;
                analysis.SuggestedTileTypes = analysis.TileTypes;
                analysis.Evaluate();
                return analysis;
            }

            private void Evaluate()
            {
                var fail = string.Equals(Result, "fail", StringComparison.OrdinalIgnoreCase);
                var clear = string.Equals(Result, "clear", StringComparison.OrdinalIgnoreCase);
                var timeUp = string.Equals(Result, "time_up", StringComparison.OrdinalIgnoreCase);
                var invalidRate = ValidMoves + InvalidMoves == 0 ? 0f : (float)InvalidMoves / (ValidMoves + InvalidMoves);

                if (FeverTriggers >= 2)
                {
                    DifficultyLabel = "Fever Master";
                    Diagnosis = $"Player triggered Fever {FeverTriggers} times (max combo {MaxCombo}).";
                    Risk = "Board may be too easy if Fever chains clear everything.";
                    Action = "Raise target score, add tile types, or increase enemy HP.";
                    SuggestedMoveLimit = Mathf.Max(8, MoveLimit - 2);
                    SuggestedTargetScore = TargetScore + 500;
                    SuggestedTileTypes = Mathf.Min(6, TileTypes + 1);
                    return;
                }

                if (TotalDamageDealt >= 200)
                {
                    DifficultyLabel = "Combat Focus";
                    Diagnosis = $"Player dealt {TotalDamageDealt} damage to enemy.";
                    Risk = "Damage output may carry the round regardless of score.";
                    Action = "Consider increasing enemy HP or adding special blocks.";
                    SuggestedMoveLimit = MoveLimit;
                    SuggestedTargetScore = TargetScore + 300;
                    SuggestedTileTypes = TileTypes;
                    return;
                }

                if (timeUp && FinalScore >= TargetScore * 2 && TargetScore > 0)
                {
                    DifficultyLabel = "Easy";
                    Diagnosis = $"Player scored {FinalScore} ({FinalScore * 100f / TargetScore:F0}% of target) with full time used. Level is too easy.";
                    Risk = "Players earn too many points too quickly for the time limit.";
                    Action = "Increase time limit or raise target score for the next pass.";
                    SuggestedMoveLimit = MoveLimit;
                    SuggestedTargetScore = TargetScore + 500;
                    SuggestedTileTypes = Mathf.Min(6, TileTypes + 1);
                    return;
                }

                if (timeUp && AverageChainLength < 3.0f && FinalScore < TargetScore * 0.75f && TargetScore > 0)
                {
                    DifficultyLabel = "Hard";
                    Diagnosis = $"Player scored only {FinalScore} with short chains (avg {AverageChainLength:F2}). Level is too hard.";
                    Risk = "The board may not surface readable 3+ paths often enough within the time limit.";
                    Action = "Lower tile types or increase time limit for the next pass.";
                    SuggestedMoveLimit = MoveLimit;
                    SuggestedTargetScore = Mathf.Max(600, TargetScore - 200);
                    SuggestedTileTypes = Mathf.Max(3, TileTypes - 1);
                    return;
                }

                if (invalidRate > 0.28f)
                {
                    DifficultyLabel = "Readability Risk";
                    Diagnosis = "Many short invalid releases happened during play.";
                    Risk = "Players may not understand which hex neighbors are valid.";
                    Action = "Improve selected-chain feedback and consider hinting valid next tiles.";
                    SuggestedMoveLimit = MoveLimit + 2;
                    SuggestedTargetScore = TargetScore;
                    SuggestedTileTypes = TileTypes;
                    return;
                }

                if (clear && TimeLeft > 0)
                {
                    var enemyDb = AssetDatabase.LoadAssetAtPath<PokoEnemyDatabase>("Assets/PokoPuzzle/Data/Resources/PokoEnemyDatabase.asset");
                    var regEnemyDb = AssetDatabase.LoadAssetAtPath<PokoRegularEnemyDatabase>("Assets/PokoPuzzle/Data/Resources/PokoRegularEnemyDatabase.asset");
                    if (TimeLimit > 0)
                    {
                        var timeRatio = (float)TimeLeft / TimeLimit;

                        var baseBossHp = 0;
                        if (enemyDb != null)
                        {
                            var bossData = enemyDb.GetWave(1);
                            if (bossData != null) baseBossHp = bossData.Hp;
                        }

                        var baseRegularEnemyHp = 0;
                        if (regEnemyDb != null)
                        {
                            var regularEnemyData = regEnemyDb.GetEnemy(1);
                            if (regularEnemyData != null) baseRegularEnemyHp = regularEnemyData.Hp;
                        }

                        if (timeRatio > 0.5f)
                        {
                            DifficultyLabel = "Easy";
                            Diagnosis = $"Player cleared with {TimeLeft}s left ({timeRatio * 100f:F0}% of {TimeLimit}s). Level is too easy.";
                            Risk = "Excess time means the time limit is too generous for the current board.";
                            Action = $"Reduce time limit to {Mathf.CeilToInt(TimeLimit * 0.7f)}s or raise target score.";
                            SuggestedMoveLimit = Mathf.Max(8, MoveLimit - 2);
                            SuggestedTargetScore = TargetScore + 300;
                            SuggestedTileTypes = Mathf.Min(6, TileTypes + 1);
                            if (baseRegularEnemyHp > 0) SuggestedRegularEnemyHp = Mathf.CeilToInt(baseRegularEnemyHp * 1.3f);
                            if (baseBossHp > 0) SuggestedBossHp = Mathf.CeilToInt(baseBossHp * 1.3f);
                            return;
                        }

                        if (timeRatio < 0.2f)
                        {
                            DifficultyLabel = "Hard";
                            Diagnosis = $"Player barely cleared with only {TimeLeft}s left ({timeRatio * 100f:F0}% of {TimeLimit}s). High time pressure.";
                            Risk = "Time limit creates stressful gameplay that may feel unfair.";
                            Action = $"Increase time limit to {Mathf.CeilToInt(TimeLimit * 1.2f)}s for a more comfortable pace.";
                            SuggestedMoveLimit = MoveLimit + 2;
                            SuggestedTargetScore = Mathf.Max(600, TargetScore - 200);
                            SuggestedTileTypes = Mathf.Max(3, TileTypes - 1);
                            if (baseRegularEnemyHp > 0) SuggestedRegularEnemyHp = Mathf.CeilToInt(baseRegularEnemyHp * 0.8f);
                            if (baseBossHp > 0) SuggestedBossHp = Mathf.CeilToInt(baseBossHp * 0.8f);
                            return;
                        }
                    }
                }

                DifficultyLabel = "Normal";
                Diagnosis = "Playtest telemetry suggests a workable level baseline.";
                Risk = "Moment-to-moment juice may still be too quiet for portfolio capture.";
                Action = "Keep the level values and polish clear/combo feedback.";
                SuggestedMoveLimit = MoveLimit;
                SuggestedTargetScore = TargetScore + 100;
                SuggestedTileTypes = TileTypes;
            }

            private static int GetInt(string line, string key, int fallback)
            {
                var raw = GetRaw(line, key);
                return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : fallback;
            }

            private static bool GetBool(string line, string key)
            {
                return string.Equals(GetRaw(line, key), "true", StringComparison.OrdinalIgnoreCase);
            }

            private static string GetString(string line, string key, string fallback)
            {
                var token = $"\"{key}\":\"";
                var start = line.IndexOf(token, StringComparison.Ordinal);
                if (start < 0)
                {
                    return fallback;
                }

                start += token.Length;
                var end = line.IndexOf('"', start);
                return end < 0 ? fallback : line.Substring(start, end - start);
            }

            private static string GetRaw(string line, string key)
            {
                var token = $"\"{key}\":";
                var start = line.IndexOf(token, StringComparison.Ordinal);
                if (start < 0)
                {
                    return string.Empty;
                }

                start += token.Length;
                var end = start;
                while (end < line.Length && line[end] != ',' && line[end] != '}')
                {
                    end++;
                }

                return line.Substring(start, end - start).Trim().Trim('"');
            }
        }
    }
}
#endif
