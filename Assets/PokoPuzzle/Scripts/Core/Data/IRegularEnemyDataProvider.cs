using System.Collections.Generic;

namespace PokoPuzzle.Core.Data
{
    [System.Serializable]
    public sealed class RegularEnemyData
    {
        public int EnemyId;
        public string Name;
        public int Hp;
        public int ScoreBonus;
        public string Role;
        public string PortraitPath;
    }

    public interface IRegularEnemyDataProvider
    {
        RegularEnemyData GetEnemy(int enemyId);
        IReadOnlyList<RegularEnemyData> GetAllEnemies();
    }
}
