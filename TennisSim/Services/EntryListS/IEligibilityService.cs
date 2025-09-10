using TennisSim.Models.Entities;

namespace TennisSim.Services.EntryListS
{
    public interface IEligibilityService
    {
        Task<List<EligiblePlayer>> GetEligiblePlayersAsync(UserName user, Tournament tournament, int userId);
    }
}
