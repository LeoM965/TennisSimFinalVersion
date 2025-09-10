using Microsoft.EntityFrameworkCore;
using TennisSim.Data;
using TennisSim.Models;
using TennisSim.Models.Entities;
using TennisSim.Services.DrawS;

namespace TennisSim.Services
{
    public class DrawService : IDrawService
    {
        private readonly ApplicationDbContext _context;
        private readonly IDrawValidationService _validationService;
        private readonly IDrawMatchGenerator _matchGenerator;

        public DrawService(ApplicationDbContext context, IDrawValidationService validationService,
            IDrawMatchGenerator matchGenerator)
        {
            _context = context;
            _validationService = validationService;
            _matchGenerator = matchGenerator;
        }

        public async Task<Draw> CreateNewDrawAsync(Tournament tournament, List<EntryList> entryList, int userId)
        {
            Console.WriteLine($"[DrawService] Attempting to create draw with UserId: {userId}");

            List<UserName> allUsers = await _context.UserNames.ToListAsync();
            Console.WriteLine($"[DrawService] All users in database: {string.Join(", ", allUsers.Select(u => $"ID:{u.Id},Username:{u.Username}"))}");

            bool userExists = await _context.UserNames.AnyAsync(u => u.Id == userId);
            Console.WriteLine($"[DrawService] User with ID {userId} exists: {userExists}");

            if (!userExists)
            {
                throw new ArgumentException($"User with ID {userId} does not exist.");
            }

            await _validationService.ValidateInputsAsync(entryList, userId);

            (int drawSize, int byeCount, int seedCount) = _validationService.GetDrawConstants(tournament.Category);
            List<string> playerNames = entryList.ConvertAll(e => e.PlayerName);
            Dictionary<string, Player> allPlayers = await _validationService.ValidateAndGetPlayersAsync(playerNames);

            Draw draw = new Draw
            {
                TournamentId = tournament.Id,
                UserId = userId,
                DrawSize = drawSize,
                ByeCount = byeCount,
                SeedCount = seedCount
            };

            _context.Draws.Add(draw);
            await _context.SaveChangesAsync();

            List<DrawMatch> matches = _matchGenerator.GenerateDrawMatches(draw.Id, entryList, drawSize, byeCount,
                seedCount, allPlayers);
            _context.DrawMatches.AddRange(matches);
            _matchGenerator.ProcessByeMatches(matches);
            await _context.SaveChangesAsync();

            return await GetDrawWithIncludesAsync(draw.Id);
        }

        public Draw CreateNewDraw(Tournament tournament, List<EntryList> entryList, int userId)
        {
            return CreateNewDrawAsync(tournament, entryList, userId).GetAwaiter().GetResult();
        }

        public async Task UpdateUserEntryListsViewStatusAsync(int tournamentId)
        {
            await _context.UserEntryLists
                .Where(uel => uel.TournamentId == tournamentId && !uel.HasViewedDraw)
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.HasViewedDraw, true));
        }

        public void UpdateUserEntryListsViewStatus(int tournamentId)
        {
            UpdateUserEntryListsViewStatusAsync(tournamentId).GetAwaiter().GetResult();
        }

        private async Task<Draw> GetDrawWithIncludesAsync(int drawId)
        {
            return await _context.Draws
                .Include(d => d.Tournament)
                .Include(d => d.DrawMatches.OrderBy(m => m.Round).ThenBy(m => m.MatchNumber))
                    .ThenInclude(m => m.Player1)
                .Include(d => d.DrawMatches)
                    .ThenInclude(m => m.Player2)
                .Include(d => d.DrawMatches)
                    .ThenInclude(m => m.Winner)
                .FirstAsync(d => d.Id == drawId);
        }
    }
}