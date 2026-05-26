using System.Collections.Generic;
using UnityEngine;

namespace PokoPuzzle.Core.Data
{
    [CreateAssetMenu(menuName = "Poko Puzzle/Data/Stage Database", fileName = "PokoStageDatabase")]
    public sealed class PokoStageDatabase : ScriptableObject, IStageDataProvider
    {
        [SerializeField] private List<StageData> stages = new();

        public void Configure(IEnumerable<StageData> source)
        {
            stages = new List<StageData>();
            if (source == null)
            {
                return;
            }

            foreach (var stage in source)
            {
                if (stage == null || string.IsNullOrWhiteSpace(stage.LevelId))
                {
                    continue;
                }

                stages.Add(new StageData
                {
                    LevelId = stage.LevelId,
                    TargetScore = Mathf.Max(100, stage.TargetScore),
                    TimeLimit = Mathf.Max(1, stage.TimeLimit),
                    MoveLimit = Mathf.Max(1, stage.MoveLimit),
                    TileTypes = Mathf.Clamp(stage.TileTypes, 3, 6),
                    BossWave = Mathf.Max(1, stage.BossWave)
                });
            }
        }

        public StageData GetStage(string levelId)
        {
            foreach (var stage in stages)
            {
                if (stage.LevelId == levelId)
                {
                    return stage;
                }
            }

            return stages.Count > 0
                ? stages[0]
                : new StageData { LevelId = levelId, TargetScore = 2200, TimeLimit = 60, MoveLimit = 20, TileTypes = 5, BossWave = 1 };
        }

        public IReadOnlyList<StageData> GetAllStages()
        {
            return stages;
        }
    }
}
