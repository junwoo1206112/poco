using System.Collections.Generic;

namespace PokoPuzzle.Core.Data
{
    [System.Serializable]
    public sealed class EnemyWaveData
    {
        public int Wave;
        public string Name;
        public int Hp;
        public int DefeatBonus;
    }

    public interface IEnemyDataProvider
    {
        IReadOnlyList<EnemyWaveData> GetAllWaves();
        EnemyWaveData GetWave(int wave);
    }
}
