using System.Collections.Generic;
using UnityEngine;

namespace PokoPuzzle.Core.Data
{
    [CreateAssetMenu(menuName = "Poko Puzzle/Data/Enemy Skill Database", fileName = "PokoEnemySkillDatabase")]
    public sealed class PokoEnemySkillDatabase : ScriptableObject, IEnemySkillProvider
    {
        [SerializeField] private List<EnemySkillEntry> skills = new();

        public void Configure(IEnumerable<EnemySkillEntry> source)
        {
            skills = new List<EnemySkillEntry>();
            if (source == null)
            {
                return;
            }

            foreach (var entry in source)
            {
                if (entry == null || entry.Wave <= 0 || entry.SkillType == EnemySkillType.None)
                {
                    continue;
                }

                skills.Add(new EnemySkillEntry
                {
                    Wave = entry.Wave,
                    SkillType = entry.SkillType,
                    TargetCount = Mathf.Max(1, entry.TargetCount),
                    CooldownSec = Mathf.Max(1f, entry.CooldownSec)
                });
            }
        }

        public IReadOnlyList<EnemySkillEntry> GetAllSkills() => skills;

        public IReadOnlyList<EnemySkillEntry> GetSkillsForWave(int wave)
        {
            var result = new List<EnemySkillEntry>();
            foreach (var entry in skills)
            {
                if (entry.Wave == wave)
                {
                    result.Add(entry);
                }
            }

            return result;
        }
    }
}
