using System.Collections.Generic;

namespace PokoPuzzle.Core.Data
{
    [System.Serializable]
    public sealed class StageData
    {
        public string LevelId;
        public int TargetScore;
        public int TimeLimit;
        public int MoveLimit;
        public int TileTypes;
        public int BossWave;
    }

    public interface IStageDataProvider
    {
        StageData GetStage(string levelId);
        IReadOnlyList<StageData> GetAllStages();
    }
}
