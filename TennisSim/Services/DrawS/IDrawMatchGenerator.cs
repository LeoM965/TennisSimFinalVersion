using TennisSim.Models;

namespace TennisSim.Services.DrawS
{

    public interface IDrawMatchGenerator
    {
        List<DrawMatch> GenerateDrawMatches(int drawId, List<EntryList> entryList, int drawSize, int byeCount, int seedCount, Dictionary<string, Player> preloadedPlayers);
        void ProcessByeMatches(List<DrawMatch> matches);
        DrawMatch GetNextMatch(DrawMatch currentMatch, List<DrawMatch> allMatches);
    }
}
