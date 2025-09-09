using TennisSim.Models;
using TennisSim.Models.Entities;

namespace TennisSim.Services.DrawS
{
    public interface IDrawPlayerAssigner
    {
        void AssignSeeds(List<EntryList> entryList, List<DrawMatch> firstRoundMatches, int seedsCount, int byeCount, Dictionary<string, Player> preloadedPlayers);
        void AssignRemainingPlayers(List<EntryList> entryList, List<DrawMatch> firstRoundMatches, int seedsCount, Dictionary<string, Player> preloadedPlayers);
    }
}