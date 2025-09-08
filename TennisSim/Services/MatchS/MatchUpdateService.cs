using Microsoft.EntityFrameworkCore;
using TennisSim.Data;
using TennisSim.Models;

namespace TennisSim.Services.Match
{
    public class MatchUpdateService : IMatchUpdateService
    {
        private readonly ApplicationDbContext _context;

        public MatchUpdateService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task UpdateMatchResult(int scheduleMatchId, List<(int p1Score, int p2Score)> sets,
            Player winner, ScheduleMatch scheduleMatch, string detailedScore)
        {
            DrawMatch drawMatch = scheduleMatch.DrawMatch;
            if (drawMatch == null) return;

            drawMatch.WinnerId = winner.Id;
            drawMatch.Winner = winner;

            Models.Match match = drawMatch.Match ?? new Models.Match();
            await UpdateMatchDetails(match, drawMatch, scheduleMatch, winner, detailedScore);

            if (drawMatch.Match == null)
            {
                drawMatch.Match = match;
                _context.Matches.Add(match);
            }

            scheduleMatch.Status = MatchStatus.Completed;
            await _context.SaveChangesAsync();
        }

        public async Task AdvanceWinnerInTournament(DrawMatch currentMatch, int winnerId)
        {
            if (currentMatch.Draw == null) return;

            List<DrawMatch> allMatches = await GetAllDrawMatches(currentMatch.DrawId);
            Player? winner = await _context.Players.FindAsync(winnerId);

            UpdateCurrentMatchWinner(currentMatch, winnerId, winner);

            DrawMatch nextMatch = GetNextMatch(currentMatch, allMatches);
            if (nextMatch != null)
            {
                UpdateNextMatchPlayer(currentMatch, nextMatch);
                await HandleByeMatch(nextMatch);
            }

            await _context.SaveChangesAsync();
        }

        private async Task UpdateMatchDetails(Models.Match match, DrawMatch drawMatch,
            ScheduleMatch scheduleMatch, Player winner, string detailedScore)
        {
            if (drawMatch.Draw?.TournamentId > 0)
            {
                bool tournamentExists = await _context.Tournaments
                    .AnyAsync(t => t.Id == drawMatch.Draw.TournamentId);

                if (tournamentExists)
                {
                    match.DrawId = drawMatch.Draw.TournamentId;
                }
            }

            match.Score = detailedScore;
            match.Status = MatchStatus.Completed;
            match.WinnerId = winner.Id;
            match.Player1Id = drawMatch.Player1Id ?? 0;
            match.Player2Id = drawMatch.Player2Id ?? 0;
            match.Round = scheduleMatch.Round;
            match.DrawId = drawMatch.DrawId;
            match.Date = scheduleMatch.StartTime;
        }

        private async Task<List<DrawMatch>> GetAllDrawMatches(int drawId)
        {
            return await _context.DrawMatches
                .Include(m => m.Player1)
                .Include(m => m.Player2)
                .Include(m => m.Winner)
                .Where(m => m.DrawId == drawId)
                .OrderBy(m => m.Round)
                .ThenBy(m => m.MatchNumber)
                .ToListAsync();
        }

        private static void UpdateCurrentMatchWinner(DrawMatch currentMatch, int winnerId, Player? winner)
        {
            currentMatch.WinnerId = winnerId;
            currentMatch.Winner = winner;
        }

        private async Task HandleByeMatch(DrawMatch nextMatch)
        {
            if (nextMatch.IsBye && nextMatch.Player1Id.HasValue && nextMatch.Player2Id.HasValue)
            {
                nextMatch.WinnerId = nextMatch.Player1Id;
                await AdvanceWinnerInTournament(nextMatch, nextMatch.Player1Id.Value);
            }
        }

        private static DrawMatch GetNextMatch(DrawMatch currentMatch, List<DrawMatch> allMatches)
        {
            return allMatches
               .Where(m => m.Round == currentMatch.Round + 1)
               .OrderBy(m => m.MatchNumber)
               .FirstOrDefault(m => m.MatchNumber == (currentMatch.MatchNumber + 1) / 2);
        }

        private static void UpdateNextMatchPlayer(DrawMatch currentMatch, DrawMatch nextMatch)
        {
            if (currentMatch.MatchNumber % 2 == 1)
            {
                nextMatch.Player1Id = currentMatch.WinnerId;
                nextMatch.Player1 = currentMatch.Winner;
                nextMatch.Player1SeedNumber = GetWinnerSeedNumber(currentMatch);
            }
            else
            {
                nextMatch.Player2Id = currentMatch.WinnerId;
                nextMatch.Player2 = currentMatch.Winner;
                nextMatch.Player2SeedNumber = GetWinnerSeedNumber(currentMatch);
            }
        }

        private static int? GetWinnerSeedNumber(DrawMatch match)
        {
            return match.WinnerId == match.Player1Id
                ? match.Player1SeedNumber
                : match.Player2SeedNumber;
        }
    }
}