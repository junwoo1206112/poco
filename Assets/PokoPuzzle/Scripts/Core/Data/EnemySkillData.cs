using System.Collections.Generic;

namespace PokoPuzzle.Core.Data
{
    public enum EnemySkillType
    {
        None,
        Freeze,
        Stone,
        ColorSwap
    }

    [System.Serializable]
    public sealed class EnemySkillEntry
    {
        public int Wave;
        public EnemySkillType SkillType;
        public int TargetCount;
        public float CooldownSec;
    }

    public interface IEnemySkillProvider
    {
        IReadOnlyList<EnemySkillEntry> GetAllSkills();
        IReadOnlyList<EnemySkillEntry> GetSkillsForWave(int wave);
    }
}
