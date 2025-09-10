using Microsoft.EntityFrameworkCore;
using TennisSim.Data;
using TennisSim.Models.Entities;

namespace TennisSim.Services.EntryListS
{
    public class EntryListService : IEntryListService
    {
        private readonly ApplicationDbContext _context;
        private readonly EligibilityService _eligibilityService;

        public EntryListService(ApplicationDbContext context, EligibilityService eligibilityService)
        {
            _context = context;
            _eligibilityService = eligibilityService;
        }

        public UserEntryList GetUserEntryList(int userId, int tournamentId)
        {
            UserName user = _context.UserNames.FirstOrDefault(u => u.Id == userId)
                ?? throw new InvalidOperationException($"User with ID {userId} not found.");

            Tournament tournament = _context.Tournaments.FirstOrDefault(t => t.Id == tournamentId)
                ?? throw new InvalidOperationException($"Tournament with ID {tournamentId} not found.");

            UserEntryList entryList = _context.UserEntryLists
                .Include(uel => uel.EntryList)
                .FirstOrDefault(uel => uel.UserNameId == userId && uel.TournamentId == tournamentId);

            if (entryList == null)
            {
                GenerateEntryList(tournamentId, userId);
                entryList = _context.UserEntryLists
                    .Include(uel => uel.EntryList)
                    .FirstOrDefault(uel => uel.UserNameId == userId && uel.TournamentId == tournamentId)
                    ?? throw new InvalidOperationException("Failed to generate or retrieve entry list.");
            }

            return entryList;
        }

        public Draw GetTournamentDraw(int tournamentId)
        {
            return _context.Draws
                .Include(d => d.DrawMatches)
                    .ThenInclude(dm => dm.Player1)
                .Include(d => d.DrawMatches)
                    .ThenInclude(dm => dm.Player2)
                .FirstOrDefault(d => d.TournamentId == tournamentId)
                ?? throw new KeyNotFoundException($"Draw not found for tournament {tournamentId}");
        }

        public List<EntryList> GenerateEntryList(int tournamentId, int userId)
        {
            Tournament tournament = _context.Tournaments.Find(tournamentId);
            UserName user = _context.UserNames.Find(userId);

            if (tournament == null || user == null)
                return new List<EntryList>();

            DateTime pastRankingDate = tournament.StartDate.AddDays(-7);
            List<EligiblePlayer> eligiblePlayers = _eligibilityService.GetEligiblePlayers(user, tournament, userId, pastRankingDate);
            UserEntryList userEntryList = CreateUserEntryList(userId, tournamentId, user, tournament);
            List<EntryList> entryList = CreateEntryListEntries(eligiblePlayers, userEntryList);

            _context.EntryLists.AddRange(entryList);
            _context.SaveChanges();

            return entryList;
        }

        private UserEntryList CreateUserEntryList(int userId, int tournamentId, UserName user, Tournament tournament)
        {
            UserEntryList userEntryList = new UserEntryList
            {
                UserNameId = userId,
                TournamentId = tournamentId,
                User = user,
                Tournament = tournament,
                EntryList = new List<EntryList>()
            };

            _context.UserEntryLists.Add(userEntryList);
            _context.SaveChanges();

            return userEntryList;
        }

        private List<EntryList> CreateEntryListEntries(List<EligiblePlayer> eligiblePlayers, UserEntryList userEntryList)
        {
            return eligiblePlayers.Select(p => new Models.Entities.EntryList
            {
                PlayerName = p.Name,
                Rank = p.Rank,
                Points = p.Points,
                UserEntryListId = userEntryList.Id,
                UserEntryList = userEntryList
            }).ToList();
        }

        public async Task GenerateEntryListsForUpcomingTournamentsAsync(UserName user)
        {
            DateTime dateRange = user.CurrentDate.AddDays(2).Date;
            List<Tournament> upcomingTournaments = await _context.Tournaments
                .Where(t => t.StartDate.Date > user.CurrentDate.Date && t.StartDate.Date <= dateRange)
                .ToListAsync();

            foreach (Tournament tournament in upcomingTournaments)
            {
                UserEntryList existingEntryList = await _context.UserEntryLists
                    .Include(uel => uel.EntryList)
                    .FirstOrDefaultAsync(uel => uel.UserNameId == user.Id && uel.TournamentId == tournament.Id);

                if (existingEntryList == null)
                {
                    GenerateEntryList(tournament.Id, user.Id);
                }
                else
                {
                    await RefreshEntryListPoints(existingEntryList, user, tournament, user.Id);
                }
            }
        }

        private async Task RefreshEntryListPoints(UserEntryList userEntryList, UserName user, Tournament tournament, int userId)
        {
            DateTime pastRankingDate = tournament.StartDate.AddDays(-7);
            List<EligiblePlayer> eligiblePlayers = _eligibilityService.GetEligiblePlayers(user, tournament, userId, pastRankingDate);

            foreach (EntryList entryItem in userEntryList.EntryList)
            {
                EligiblePlayer updatedPlayer = eligiblePlayers.FirstOrDefault(ep => ep.Name == entryItem.PlayerName);
                if (updatedPlayer != null)
                {
                    entryItem.Points = updatedPlayer.Points;
                    entryItem.Rank = updatedPlayer.Rank;
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}