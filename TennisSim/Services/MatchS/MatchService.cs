using Microsoft.EntityFrameworkCore.Storage;
using TennisSim.Data;
using TennisSim.Models.Entities;
using TennisSim.Models.ViewModels;

namespace TennisSim.Services.Match
{
    public class MatchService : IMatchService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMatchQueryService _queryService;
        private readonly IMatchUpdateService _updateService;
        private readonly MatchSimulationService _simulationService;

        public MatchService(ApplicationDbContext context,
            IMatchQueryService queryService,
            IMatchUpdateService updateService,
            MatchSimulationService simulationService)
        {
            _context = context;
            _queryService = queryService;
            _updateService = updateService;
            _simulationService = simulationService;
        }

        public async Task<MatchViewModel> GetMatchWithPlayersAndAttributes(int scheduleMatchId)
        {
            try
            {
                ScheduleMatch? scheduleMatch = await _queryService.GetScheduleMatchWithFullDetails(scheduleMatchId);

                if (scheduleMatch?.DrawMatch == null)
                    throw new InvalidOperationException($"ScheduleMatch with ID {scheduleMatchId} not found or has no DrawMatch.");

                DrawMatch drawMatch = scheduleMatch.DrawMatch;
                await _queryService.EnsurePlayersAttributesLoaded(drawMatch);

                if (drawMatch.Player1?.Attributes == null || drawMatch.Player2?.Attributes == null)
                    throw new InvalidOperationException("Player attributes are missing.");

                TennisSim.Models.Match match = drawMatch.Match ?? CreateNewMatch(scheduleMatch, drawMatch);

                return new MatchViewModel
                {
                    Match = match,
                    Player1Attributes = drawMatch.Player1.Attributes,
                    Player2Attributes = drawMatch.Player2.Attributes,
                    Tournament = drawMatch.Draw?.Tournament ?? new Tournament()
                };
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving match data", ex);
            }
        }

        public async Task<MatchSimulationResult> SimulateMatch(int id)
        {
            try
            {
                MatchViewModel matchVM = await GetMatchWithPlayersAndAttributes(id);

                MatchSimulation.MatchResult result = _simulationService.SimulateMatch(
                    matchVM.Player1Attributes, matchVM.Player2Attributes);

                ScheduleMatch scheduleMatch = await _queryService.GetScheduleMatchWithDrawDetails(id);
                if (scheduleMatch?.DrawMatch == null)
                    throw new ArgumentException("Schedule match or draw match not found");

                Player winner = result.WinnerIsPlayer1 ? matchVM.Match.Player1 : matchVM.Match.Player2;
                List<SetDetailResult> setDetails = MatchSimulationService.ConvertToSetDetails(result.Sets);
                List<(int P1Score, int P2Score)> setsTuples = MatchSimulationService.ExtractSetScores(result.Sets);
                string detailedScore = MatchScoreService.GenerateDetailedScore(result.Sets);

                await UpdateMatchAndAdvanceWinner(id, setsTuples, winner, scheduleMatch, detailedScore);

                return new MatchSimulationResult
                {
                    Sets = setsTuples,
                    Winner = winner,
                    SetDetails = setDetails
                };
            }
            catch (Exception ex)
            {
                throw new Exception("Error during match simulation", ex);
            }
        }

        private async Task UpdateMatchAndAdvanceWinner(int scheduleMatchId, List<(int p1Score, int p2Score)> sets,
            Player winner, ScheduleMatch scheduleMatch, string detailedScore)
        {
            await using IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _updateService.UpdateMatchResult(scheduleMatchId, sets, winner, scheduleMatch, detailedScore);
                await _updateService.AdvanceWinnerInTournament(scheduleMatch.DrawMatch, winner.Id);
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private static TennisSim.Models.Match CreateNewMatch(ScheduleMatch scheduleMatch, DrawMatch drawMatch)
        {
            return new TennisSim.Models.Match
            {
                Id = scheduleMatch.Id,
                Player1 = drawMatch.Player1,
                Player2 = drawMatch.Player2,
                Player1Id = drawMatch.Player1Id ?? 0,
                Player2Id = drawMatch.Player2Id ?? 0,
                Status = MatchStatus.Scheduled,
                Date = scheduleMatch.StartTime,
                Round = scheduleMatch.Round,
                DrawId = drawMatch.DrawId
            };
        }
    }
}