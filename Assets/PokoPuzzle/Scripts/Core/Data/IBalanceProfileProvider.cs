using System.Collections.Generic;

namespace PokoPuzzle.Core.Data
{
    public interface IBalanceProfileProvider
    {
        BalanceProfileData GetProfile(string profileId);
        IReadOnlyList<BalanceProfileData> GetAllProfiles();
    }
}
