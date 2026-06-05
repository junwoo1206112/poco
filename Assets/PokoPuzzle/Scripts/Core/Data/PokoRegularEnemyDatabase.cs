using System.Collections.Generic;
using UnityEngine;

namespace PokoPuzzle.Core.Data
{
    [CreateAssetMenu(menuName = "Poko Puzzle/Data/Regular Enemy Database", fileName = "PokoRegularEnemyDatabase")]
    public sealed class PokoRegularEnemyDatabase : ScriptableObject, IRegularEnemyDataProvider
    {
        [SerializeField] private List<RegularEnemyData> enemies = new();

        public void Configure(IEnumerable<RegularEnemyData> source)
        {
            enemies = new List<RegularEnemyData>();
            if (source == null)
            {
                return;
            }

            foreach (var enemy in source)
            {
                if (enemy == null || enemy.EnemyId <= 0)
                {
                    continue;
                }

                enemies.Add(new RegularEnemyData
                {
                    EnemyId = enemy.EnemyId,
                    Name = string.IsNullOrWhiteSpace(enemy.Name) ? "Monster" : enemy.Name,
                    Hp = Mathf.Max(1, enemy.Hp),
                    ScoreBonus = Mathf.Max(0, enemy.ScoreBonus),
                    Role = string.IsNullOrWhiteSpace(enemy.Role) ? "Normal" : enemy.Role,
                    PortraitPath = enemy.PortraitPath ?? string.Empty
                });
            }
        }

        public RegularEnemyData GetEnemy(int enemyId)
        {
            foreach (var enemy in enemies)
            {
                if (enemy.EnemyId == enemyId)
                {
                    return enemy;
                }
            }

            return enemies.Count > 0
                ? enemies[0]
                : new RegularEnemyData { EnemyId = enemyId, Name = "Monster", Hp = 30, ScoreBonus = 50, Role = "Normal", PortraitPath = string.Empty };
        }

        public IReadOnlyList<RegularEnemyData> GetAllEnemies()
        {
            return enemies;
        }
    }
}
