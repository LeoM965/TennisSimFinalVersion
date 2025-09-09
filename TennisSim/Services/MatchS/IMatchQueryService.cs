using TennisSim.Models.Entities;

namespace TennisSim.Services.Match
{
    public interface IMatchQueryService
    {
        Task<ScheduleMatch?> GetScheduleMatchWithFullDetails(int scheduleMatchId);
        Task<ScheduleMatch?> GetScheduleMatchWithDrawDetails(int id);
        Task EnsurePlayersAttributesLoaded(DrawMatch drawMatch);
    }
}
