using TennisSim.Models.Entities;

namespace TennisSim.Services.EntryListS
{
    public interface IEntryListService
    {
        UserEntryList GetUserEntryList(int userId, int tournamentId);
        Draw GetTournamentDraw(int tournamentId);
        List<EntryList> GenerateEntryList(int tournamentId, int userId);
    }
}