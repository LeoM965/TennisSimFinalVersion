using Microsoft.EntityFrameworkCore;
using TennisSim.Data;
using TennisSim.Models;
using TennisSim.Models.Entities;
using TennisSim.Services.DrawS;
using TennisSim.Utilities;

namespace TennisSim.Services
{
    public class DrawValidationService : IDrawValidationService
    {
        private readonly ApplicationDbContext _context;

        public DrawValidationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task ValidateInputsAsync(List<EntryList> entryList, int userId)
        {
            var userExists = await _context.UserNames.AnyAsync(u => u.Id == userId);
            if (!userExists)
                throw new InvalidOperationException($"User with ID {userId} does not exist.");

            if (entryList?.Count == 0)
                throw new InvalidOperationException("Entry list is empty.");
        }

        public async Task<Dictionary<string, Player>> ValidateAndGetPlayersAsync(List<string> playerNames)
        {
            var allPlayers = await _context.Players
                .Where(p => playerNames.Contains(p.Name))
                .ToDictionaryAsync(p => p.Name, p => p);

            var missingPlayers = playerNames.Except(allPlayers.Keys).ToHashSet();
            if (missingPlayers.Count > 0)
                throw new InvalidOperationException($"Players not found: {string.Join(", ", missingPlayers)}");

            return allPlayers;
        }

        public (int drawSize, int byeCount, int seedCount) GetDrawConstants(TournamentCategory category) =>
            (DrawConstants.GetDrawSize(category),
             DrawConstants.GetByeCount(category),
             DrawConstants.GetSeedCount(category));
    }

}