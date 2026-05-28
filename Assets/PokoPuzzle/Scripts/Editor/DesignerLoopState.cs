#if UNITY_EDITOR
using System;
using System.IO;

namespace PokoPuzzle.Editor
{
    public sealed class DesignerLoopState
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

        private static string ExperimentLogPath(string experimentId, string variant)
        {
            return $"md/playtest-logs/by-experiment/{experimentId}-{variant}.jsonl";
        }

        private static string ExperimentAssetPath(string experimentId, string variant)
        {
            return $"Assets/PokoPuzzle/Data/Experiments/{experimentId}/{variant}.asset";
        }
    }
}
#endif
