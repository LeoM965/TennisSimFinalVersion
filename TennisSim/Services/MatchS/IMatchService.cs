using TennisSim.Models.ViewModels;

namespace TennisSim.Services.Match
{
    public interface IMatchService
    {
        Task<MatchViewModel> GetMatchWithPlayersAndAttributes(int matchId);
        Task<MatchSimulationResult> SimulateMatch(int matchId);
    }
}
