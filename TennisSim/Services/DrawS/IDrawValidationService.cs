using TennisSim.Models;
using TennisSim.Models.Entities;

namespace TennisSim.Services.DrawS
{
    public interface IDrawValidationService
    {
        Task ValidateInputsAsync(List<EntryList> entryList, int userId);
        Task<Dictionary<string, Player>> ValidateAndGetPlayersAsync(List<string> playerNames);
        (int drawSize, int byeCount, int seedCount) GetDrawConstants(TournamentCategory category);
    }
}