using System.Collections.Generic;
using UnityEngine;

namespace PokoPuzzle.Core.Data
{
    [CreateAssetMenu(menuName = "Poko Puzzle/Data/Balance Profile Database", fileName = "PokoBalanceProfileDatabase")]
    public sealed class PokoBalanceProfileDatabase : ScriptableObject, IBalanceProfileProvider
    {
        [SerializeField] private List<BalanceProfileData> profiles = new();

        public void Configure(IEnumerable<BalanceProfileData> source)
        {
            profiles = new List<BalanceProfileData>();
            if (source == null)
            {
                return;
            }

            foreach (var profile in source)
            {
                if (profile == null || string.IsNullOrWhiteSpace(profile.ProfileId))
                {
                    continue;
                }

                profiles.Add(new BalanceProfileData
                {
                    ProfileId = profile.ProfileId,
                    DisplayName = string.IsNullOrWhiteSpace(profile.DisplayName) ? profile.ProfileId : profile.DisplayName,
                    MinAvailableChains = Mathf.Max(0, profile.MinAvailableChains),
                    TargetAverageChainLength = Mathf.Max(0f, profile.TargetAverageChainLength),
                    RainbowSpawnWeight = Mathf.Max(0f, profile.RainbowSpawnWeight),
                    RefillAssistRate = Mathf.Clamp01(profile.RefillAssistRate),
                    BlockerBudget = Mathf.Max(0, profile.BlockerBudget),
                    RegularEnemyHpMultiplier = Mathf.Max(0f, profile.RegularEnemyHpMultiplier),
                    BossHpMultiplier = Mathf.Max(0f, profile.BossHpMultiplier),
                    SkillCooldownMultiplier = Mathf.Max(0f, profile.SkillCooldownMultiplier)
                });
            }
        }

        public BalanceProfileData GetProfile(string profileId)
        {
            foreach (var profile in profiles)
            {
                if (profile.ProfileId == profileId)
                {
                    return profile;
                }
            }

            return profiles.Count > 0
                ? profiles[0]
                : new BalanceProfileData
                {
                    ProfileId = "default",
                    DisplayName = "Default",
                    MinAvailableChains = 3,
                    TargetAverageChainLength = 4f,
                    RainbowSpawnWeight = 1f,
                    RefillAssistRate = 0.15f,
                    BlockerBudget = 2,
                    RegularEnemyHpMultiplier = 1f,
                    BossHpMultiplier = 1f,
                    SkillCooldownMultiplier = 1f
                };
        }

        public IReadOnlyList<BalanceProfileData> GetAllProfiles()
        {
            return profiles;
        }
    }
}
