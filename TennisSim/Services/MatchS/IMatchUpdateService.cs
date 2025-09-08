using TennisSim.Models;

namespace TennisSim.Services.Match
{
    public interface IMatchUpdateService
    {
        Task UpdateMatchResult(int scheduleMatchId, List<(int p1Score, int p2Score)> sets,
            Player winner, ScheduleMatch scheduleMatch, string detailedScore);
        Task AdvanceWinnerInTournament(DrawMatch currentMatch, int winnerId);
    }
}
