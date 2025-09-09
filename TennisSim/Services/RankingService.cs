using Microsoft.EntityFrameworkCore;
using TennisSim.Data;
using TennisSim.Models.Entities;

namespace TennisSim.Services
{
    public class RankingService
    {
        private const double DecayPercentage = 0.02;
        private readonly ApplicationDbContext _context;

        public RankingService(ApplicationDbContext context)
        {
            _context = context;
        }

        private static int CalculateTournamentPoints(
            List<Models.Entities.Match> tournamentMatches,
            int playerId,
            List<PointDistribution> pointDistributions)
        {
            List<Models.Entities.Match> playerMatches = tournamentMatches
                .Where(m => m.Player1Id == playerId || m.Player2Id == playerId)
                .OrderBy(m => GetRoundOrder(m.Round))
                .ToList();

            if (playerMatches.Count == 0) return 0;

            Tournament tournament = tournamentMatches.First().Draw.Tournament;
            Models.Entities.Match? finalMatch = tournamentMatches.FirstOrDefault(m => m.Round == "Final");

            if (finalMatch?.WinnerId == playerId)
            {
                return GetPointsForRound(pointDistributions, tournament.Category.ToString(), "Winner");
            }

            if (finalMatch != null && (finalMatch.Player1Id == playerId || finalMatch.Player2Id == playerId))
            {
                return GetPointsForRound(pointDistributions, tournament.Category.ToString(), "Final");
            }

            Models.Entities.Match? lastMatch = playerMatches.LastOrDefault(m => m.WinnerId != playerId);
            if (lastMatch != null)
            {
                return GetPointsForRound(pointDistributions, tournament.Category.ToString(), lastMatch.Round);
            }

            Models.Entities.Match? lastWonMatch = playerMatches.LastOrDefault(m => m.WinnerId == playerId);
            if (lastWonMatch != null)
            {
                string nextRound = GetNextRound(lastWonMatch.Round);
                return GetPointsForRound(pointDistributions, tournament.Category.ToString(), nextRound);
            }

            return 0;
        }

        private static int GetPointsForRound(List<PointDistribution> pointDistributions, string category, string round)
        {
            return pointDistributions
                .Where(pd => pd.Category.ToString() == category && pd.Round == round)
                .Select(pd => pd.Points)
                .FirstOrDefault();
        }

        private static int GetRoundOrder(string round)
        {
            return round switch
            {
                "Round 1" => 1,
                "Round 2" => 2,
                "Round 3" => 3,
                "Round of 16" => 4,
                "Quarter Final" => 5,
                "Semi Final" => 6,
                "Final" => 7,
                _ => 0
            };
        }

        private static string GetNextRound(string currentRound)
        {
            return currentRound switch
            {
                "Round 1" => "Round 2",
                "Round 2" => "Round 3",
                "Round 3" => "Round of 16",
                "Round of 16" => "Quarter Final",
                "Quarter Final" => "Semi Final",
                "Semi Final" => "Final",
                "Final" => "Winner",
                _ => currentRound
            };
        }

        public async Task UpdateRankingsAsync(DateTime targetDate, int userId)
        {
            if (!await _context.Rankings.AnyAsync(r => r.UserId == userId))
            {
                await CreateInitialRankingsAsync(targetDate, userId);
                return;
            }

            if (targetDate.DayOfWeek != DayOfWeek.Monday) return;

            DateTime thisMonday = targetDate.Date;
            DateTime lastMonday = thisMonday.AddDays(-7);

            if (await _context.Rankings.AnyAsync(r => r.Date == thisMonday && r.UserId == userId)) return;

            List<int> userDrawIds = await _context.Draws
                .Where(d => d.UserId == userId)
                .Select(d => d.Id)
                .ToListAsync();

            List<Models.Entities.Match> lastWeekMatches = await _context.Matches
                .Include(m => m.Draw)
                    .ThenInclude(d => d.Tournament)
                .Where(m => m.Date >= lastMonday && m.Date < thisMonday && userDrawIds.Contains(m.DrawId))
                .ToListAsync();

            List<PointDistribution> pointDistributions = await _context.PointDistributions
                .OrderBy(pd => pd.Category)
                .ThenBy(pd => pd.Round)
                .ToListAsync();

            List<Ranking> newRankings = new List<Ranking>();

            DateTime? latestPreviousDateNullable = await _context.Rankings
                .Where(r => r.UserId == userId && r.Date < thisMonday)
                .Select(r => r.Date)
                .MaxAsync();

            if (!latestPreviousDateNullable.HasValue) return;

            DateTime latestPreviousDate = latestPreviousDateNullable.Value;

            List<Ranking> previousRankings = await _context.Rankings
                .Where(r => r.UserId == userId && r.Date == latestPreviousDate)
                .ToListAsync();

            List<int> rankedPlayerIds = previousRankings.Select(r => r.PlayerId).ToList();
            List<int> allPlayers = await _context.Players.Select(p => p.Id).ToListAsync();
            List<int> unrankedPlayerIds = allPlayers.Except(rankedPlayerIds).ToList();

            foreach (int playerId in unrankedPlayerIds)
            {
                previousRankings.Add(new Ranking
                {
                    PlayerId = playerId,
                    Points = 0,
                    Date = latestPreviousDate,
                    UserId = userId,
                    Rank = 0
                });
            }

            Dictionary<int, List<Models.Entities.Match>> tournaments = lastWeekMatches
                .GroupBy(m => m.Draw.TournamentId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (Ranking prevRank in previousRankings)
            {
                int decayedPoints = (int)Math.Round(prevRank.Points * (1 - DecayPercentage));
                int maxTournamentPoints = 0;

                foreach (KeyValuePair<int, List<Models.Entities.Match>> tournament in tournaments)
                {
                    int tournamentPoints = CalculateTournamentPoints(
                        tournament.Value,
                        prevRank.PlayerId,
                        pointDistributions
                    );
                    maxTournamentPoints = Math.Max(maxTournamentPoints, tournamentPoints);
                }

                newRankings.Add(new Ranking
                {
                    PlayerId = prevRank.PlayerId,
                    Points = decayedPoints + maxTournamentPoints,
                    Date = thisMonday,
                    UserId = userId
                });
            }

            List<Ranking> sortedRankings = newRankings
                .OrderByDescending(r => r.Points)
                .ToList();

            for (int i = 0; i < sortedRankings.Count; i++)
            {
                sortedRankings[i].Rank = i + 1;
            }

            await _context.Rankings.AddRangeAsync(sortedRankings);
            await _context.SaveChangesAsync();
        }

        private async Task CreateInitialRankingsAsync(DateTime targetDate, int userId)
        {
            DateTime targetDateOnly = targetDate.Date;

            if (await _context.Rankings.AnyAsync(r => r.Date == targetDateOnly && r.UserId == userId))
                return;

            List<Ranking> newRankings = new List<Ranking>();
            DateTime? latestSystemDateNullable = null;

            try
            {
                latestSystemDateNullable = await _context.Rankings
                    .Where(r => r.UserId == 0)
                    .Select(r => r.Date)
                    .MaxAsync();
            }
            catch
            {
                latestSystemDateNullable = null;
            }

            if (latestSystemDateNullable.HasValue)
            {
                DateTime latestSystemDate = latestSystemDateNullable.Value;
                List<Ranking> systemRankings = await _context.Rankings
                    .Where(r => r.UserId == 0 && r.Date == latestSystemDate)
                    .ToListAsync();

                if (systemRankings.Count > 0)
                {
                    foreach (Ranking systemRank in systemRankings)
                    {
                        newRankings.Add(new Ranking
                        {
                            PlayerId = systemRank.PlayerId,
                            Points = systemRank.Points,
                            Date = targetDateOnly,
                            UserId = userId,
                            Rank = systemRank.Rank
                        });
                    }

                    List<int> rankedPlayerIds = systemRankings.Select(r => r.PlayerId).ToList();
                    List<int> allPlayers = await _context.Players.Select(p => p.Id).ToListAsync();
                    List<int> unrankedPlayerIds = allPlayers.Except(rankedPlayerIds).ToList();

                    foreach (int playerId in unrankedPlayerIds)
                    {
                        newRankings.Add(new Ranking
                        {
                            PlayerId = playerId,
                            Points = 0,
                            Date = targetDateOnly,
                            UserId = userId,
                            Rank = 0
                        });
                    }
                }
                else
                {
                    await CreateDefaultRankingsAsync(targetDateOnly, userId, newRankings);
                }
            }
            else
            {
                await CreateDefaultRankingsAsync(targetDateOnly, userId, newRankings);
            }

            List<Ranking> sortedRankings = newRankings
                .OrderByDescending(r => r.Points)
                .ToList();

            for (int i = 0; i < sortedRankings.Count; i++)
            {
                sortedRankings[i].Rank = i + 1;
            }

            await _context.Rankings.AddRangeAsync(sortedRankings);
            await _context.SaveChangesAsync();
        }

        private async Task CreateDefaultRankingsAsync(DateTime rankingDate, int userId, List<Ranking> newRankings)
        {
            List<Player> allPlayers = await _context.Players.ToListAsync();

            foreach (Player player in allPlayers)
            {
                newRankings.Add(new Ranking
                {
                    PlayerId = player.Id,
                    Points = 1000,
                    Date = rankingDate,
                    UserId = userId,
                    Rank = 0
                });
            }
        }
    }
}