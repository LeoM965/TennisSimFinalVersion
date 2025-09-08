using TennisSim.Models;

namespace TennisSim.Services.Match
{
    public interface IMatchService
    {
        Task<MatchViewModel> GetMatchWithPlayersAndAttributes(int matchId);
        Task<MatchSimulationResult> SimulateMatch(int matchId);
    }
}
