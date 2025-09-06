using Microsoft.EntityFrameworkCore;
using TennisSim.Data;
using TennisSim.Models;
using TennisSim.Utilities;

namespace TennisSim.Services
{
    public class DrawService : IDrawService
    {
        private readonly ApplicationDbContext _context;

        public DrawService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Draw> CreateNewDrawAsync(Tournament tournament, List<EntryList> entryList, int userId)
        {
            await ValidateInputsAsync(entryList, userId);

            int drawSize = DrawConstants.GetDrawSize(tournament.Category);
            int byeCount = DrawConstants.GetByeCount(tournament.Category);
            int seedCount = DrawConstants.GetSeedCount(tournament.Category);

            List<string> playerNames = entryList.ConvertAll(e => e.PlayerName);
            Dictionary<string, Player> allPlayers = await ValidateAndGetPlayersAsync(playerNames);

            Draw draw = new()
            {
                TournamentId = tournament.Id,
                UserId = userId,
                DrawSize = drawSize,
                ByeCount = byeCount,
                SeedCount = seedCount
            };

            _context.Draws.Add(draw);
            await _context.SaveChangesAsync();

            List<DrawMatch> matches = GenerateDrawMatches(draw.Id, entryList, drawSize, byeCount, seedCount, allPlayers);
            _context.DrawMatches.AddRange(matches);
            ProcessByeMatches(matches);
            await _context.SaveChangesAsync();

            return await GetDrawWithIncludesAsync(draw.Id);
        }

        public Draw CreateNewDraw(Tournament tournament, List<EntryList> entryList, int userId) =>
            CreateNewDrawAsync(tournament, entryList, userId).GetAwaiter().GetResult();

        public async Task UpdateUserEntryListsViewStatusAsync(int tournamentId) =>
            await _context.UserEntryLists
                .Where(uel => uel.TournamentId == tournamentId && !uel.HasViewedDraw)
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.HasViewedDraw, true));

        public void UpdateUserEntryListsViewStatus(int tournamentId) =>
            UpdateUserEntryListsViewStatusAsync(tournamentId).GetAwaiter().GetResult();

        public void ProcessByeMatches(List<DrawMatch> matches)
        {
            foreach (DrawMatch byeMatch in matches.Where(m => m.IsBye && m.Player1Id.HasValue))
            {
                DrawMatch nextMatch = GetNextMatch(byeMatch, matches);

                if (byeMatch.MatchNumber % 2 != 0)
                {
                    nextMatch.Player1Id = byeMatch.Player1Id;
                    nextMatch.Player1SeedNumber = byeMatch.Player1SeedNumber;
                }
                else
                {
                    nextMatch.Player2Id = byeMatch.Player1Id;
                    nextMatch.Player2SeedNumber = byeMatch.Player1SeedNumber;
                }
            }
        }

        public DrawMatch GetNextMatch(DrawMatch currentMatch, List<DrawMatch> allMatches)
        {
            int nextRoundMatchNumber = (currentMatch.MatchNumber + 1) / 2;
            return allMatches.First(m =>
                m.Round == currentMatch.Round + 1 &&
                m.MatchNumber == nextRoundMatchNumber);
        }

        public List<DrawMatch> GenerateDrawMatches(int drawId, List<EntryList> entryList, int drawSize, int byeCount,
            int seedCount, Dictionary<string, Player> preloadedPlayers)
        {
            int roundCount = (int)Math.Log2(drawSize);
            List<DrawMatch> matches = new(drawSize - 1);

            for (int round = 1; round <= roundCount; round++)
            {
                int matchesInRound = drawSize / (int)Math.Pow(2, round);
                for (int i = 1; i <= matchesInRound; i++)
                {
                    matches.Add(new DrawMatch
                    {
                        DrawId = drawId,
                        Round = round,
                        MatchNumber = i
                    });
                }
            }

            List<DrawMatch> firstRoundMatches = matches.Where(m => m.Round == 1).ToList();
            int seedsCount = Math.Min(seedCount, entryList.Count);

            AssignSeeds(entryList, firstRoundMatches, seedsCount, byeCount, preloadedPlayers);
            AssignRemainingPlayers(entryList, firstRoundMatches, seedsCount, preloadedPlayers);

            return matches;
        }

        private async Task<Draw> GetDrawWithIncludesAsync(int drawId) =>
            await _context.Draws
                .Include(d => d.Tournament)
                .Include(d => d.DrawMatches.OrderBy(m => m.Round).ThenBy(m => m.MatchNumber))
                    .ThenInclude(m => m.Player1)
                .Include(d => d.DrawMatches)
                    .ThenInclude(m => m.Player2)
                .Include(d => d.DrawMatches)
                    .ThenInclude(m => m.Winner)
                .FirstAsync(d => d.Id == drawId);

        private async Task ValidateInputsAsync(List<EntryList> entryList, int userId)
        {
            bool userExists = await _context.UserNames.AnyAsync(u => u.Id == userId);
            if (!userExists)
                throw new InvalidOperationException($"User with ID {userId} does not exist.");

            if (entryList?.Count == 0)
                throw new InvalidOperationException("Entry list is empty.");
        }

        private async Task<Dictionary<string, Player>> ValidateAndGetPlayersAsync(List<string> playerNames)
        {
            Dictionary<string, Player> allPlayers = await _context.Players
                .Where(p => playerNames.Contains(p.Name))
                .ToDictionaryAsync(p => p.Name, p => p);

            HashSet<string> missingPlayers = playerNames.Except(allPlayers.Keys).ToHashSet();
            if (missingPlayers.Count > 0)
                throw new InvalidOperationException($"Players not found: {string.Join(", ", missingPlayers)}");

            return allPlayers;
        }

        private static void AssignSeeds(List<EntryList> entryList, List<DrawMatch> firstRoundMatches,
            int seedsCount, int byeCount, Dictionary<string, Player> preloadedPlayers)
        {
            for (int i = 0; i < seedsCount; i++)
            {
                int position = DrawConstants.GetSeedPosition(i + 1, firstRoundMatches.Count);

                if (position < firstRoundMatches.Count &&
                    preloadedPlayers.TryGetValue(entryList[i].PlayerName, out Player player))
                {
                    DrawMatch match = firstRoundMatches[position];
                    match.Player1Id = player.Id;
                    match.Player1SeedNumber = i + 1;

                    if (i < byeCount)
                    {
                        match.IsBye = true;
                        match.WinnerId = player.Id;
                    }
                }
            }
        }

        private static void AssignRemainingPlayers(List<EntryList> entryList, List<DrawMatch> firstRoundMatches,
            int seedsCount, Dictionary<string, Player> preloadedPlayers)
        {
            List<Player> remainingPlayers = entryList
                .Skip(seedsCount)
                .Where(entry => preloadedPlayers.ContainsKey(entry.PlayerName))
                .Select(entry => preloadedPlayers[entry.PlayerName])
                .OrderBy(x => Guid.NewGuid())
                .ToList();

            int playerIndex = 0;

            foreach (DrawMatch match in firstRoundMatches.Where(m => m.Player1Id == null))
            {
                if (playerIndex >= remainingPlayers.Count) break;
                match.Player1Id = remainingPlayers[playerIndex++].Id;
            }

            foreach (DrawMatch match in firstRoundMatches.Where(m => !m.IsBye && m.Player2Id == null))
            {
                if (playerIndex >= remainingPlayers.Count) break;
                match.Player2Id = remainingPlayers[playerIndex++].Id;
            }
        }
    }
}