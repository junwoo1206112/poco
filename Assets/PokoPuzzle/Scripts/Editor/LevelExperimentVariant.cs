#if UNITY_EDITOR
using PokoPuzzle.Core;

namespace PokoPuzzle.Editor
{
    public sealed class LevelExperimentVariant
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
}
#endif
