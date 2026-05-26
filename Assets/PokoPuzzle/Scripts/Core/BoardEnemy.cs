using UnityEngine;

namespace PokoPuzzle.Core
{
    public sealed class BoardEnemy
    {
        public int MaxHp { get; private set; }
        public int CurrentHp { get; private set; }
        public bool IsDefeated => CurrentHp <= 0;
        public int DefeatBonus { get; private set; }
        public int Wave { get; private set; }
        public string Name { get; private set; }

        private const int BaseMaxHp = 100;
        private const int BaseDefeatBonus = 500;

        public BoardEnemy(int maxHp = BaseMaxHp, int defeatBonus = BaseDefeatBonus, string name = "Monster", int wave = 1)
        {
            MaxHp = maxHp > 0 ? maxHp : BaseMaxHp;
            CurrentHp = MaxHp;
            DefeatBonus = defeatBonus > 0 ? defeatBonus : BaseDefeatBonus;
            Name = name ?? "Monster";
            Wave = Mathf.Max(1, wave);
        }

        public int ApplyDamage(int damage)
        {
            if (IsDefeated)
            {
                return 0;
            }

            var actualDamage = Mathf.Min(damage, CurrentHp);
            CurrentHp -= actualDamage;
            return actualDamage;
        }

        public void Reset(int? maxHp = null, int? defeatBonus = null, string name = null, int? wave = null)
        {
            Wave = wave.HasValue && wave.Value > 0 ? wave.Value : 1;
            MaxHp = maxHp.HasValue && maxHp.Value > 0 ? maxHp.Value : BaseMaxHp;
            DefeatBonus = defeatBonus.HasValue && defeatBonus.Value > 0 ? defeatBonus.Value : BaseDefeatBonus;
            Name = name ?? "Monster";
            CurrentHp = MaxHp;
        }

        public float HpRatio => MaxHp > 0 ? (float)CurrentHp / MaxHp : 0f;
    }
}
