using System.Collections.Generic;
using UnityEngine;

namespace PokoPuzzle.Core.Data
{
    [CreateAssetMenu(menuName = "Poko Puzzle/Data/Boss Database", fileName = "PokoEnemyDatabase")]
    public sealed class PokoEnemyDatabase : ScriptableObject, IEnemyDataProvider
    {
        [SerializeField] private List<EnemyWaveData> waves = new();

        public void Configure(IEnumerable<EnemyWaveData> source)
        {
            waves = new List<EnemyWaveData>();
            if (source == null)
            {
                return;
            }

            foreach (var wave in source)
            {
                if (wave == null || wave.Wave <= 0)
                {
                    continue;
                }

                waves.Add(new EnemyWaveData
                {
                    Wave = wave.Wave,
                    Name = string.IsNullOrWhiteSpace(wave.Name) ? "Monster" : wave.Name,
                    Hp = Mathf.Max(1, wave.Hp),
                    DefeatBonus = Mathf.Max(0, wave.DefeatBonus),
                    PortraitPath = wave.PortraitPath ?? string.Empty,
                    BackgroundPath = wave.BackgroundPath ?? string.Empty
                });
            }
        }

        public IReadOnlyList<EnemyWaveData> GetAllWaves()
        {
            return waves;
        }

        public EnemyWaveData GetWave(int wave)
        {
            foreach (var enemyWave in waves)
            {
                if (enemyWave.Wave == wave)
                {
                    return enemyWave;
                }
            }

            return waves.Count > 0
                ? waves[^1]
                : new EnemyWaveData { Wave = wave, Hp = 100, DefeatBonus = 500, PortraitPath = string.Empty, BackgroundPath = string.Empty };
        }
    }
}
