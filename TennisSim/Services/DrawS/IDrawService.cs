using TennisSim.Models;

namespace TennisSim.Services.DrawS
{
    public interface IDrawService
    {
        Task<Draw> CreateNewDrawAsync(Tournament tournament, List<EntryList> entryList, int userId);
        Draw CreateNewDraw(Tournament tournament, List<EntryList> entryList, int userId);
        Task UpdateUserEntryListsViewStatusAsync(int tournamentId);
        void UpdateUserEntryListsViewStatus(int tournamentId);
    }
}
