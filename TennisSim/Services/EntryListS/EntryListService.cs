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

            List<EligiblePlayer> eligiblePlayers = _eligibilityService.GetEligiblePlayers(user, tournament, userId);
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
    }
}