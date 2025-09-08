using TennisSim.Models;
using TennisSim.Services.DrawS;
using TennisSim.Utilities;

namespace TennisSim.Services
{
    public class DrawPlayerAssigner : IDrawPlayerAssigner
    {
        public void AssignSeeds(List<EntryList> entryList, List<DrawMatch> firstRoundMatches,
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

        public void AssignRemainingPlayers(List<EntryList> entryList, List<DrawMatch> firstRoundMatches,
            int seedsCount, Dictionary<string, Player> preloadedPlayers)
        {
            List<Player> remainingPlayers = entryList
                .Skip(seedsCount)
                .Where(entry => preloadedPlayers.ContainsKey(entry.PlayerName))
                .Select(entry => preloadedPlayers[entry.PlayerName])
                .OrderBy(x => Guid.NewGuid())
                .ToList();

            int playerIndex = 0;
            List<DrawMatch> availableMatches = firstRoundMatches.Where(m => m.Player1Id == null).ToList();

            foreach (DrawMatch match in availableMatches)
            {
                if (playerIndex >= remainingPlayers.Count) break;
                match.Player1Id = remainingPlayers[playerIndex++].Id;
            }

            List<DrawMatch> openSlots = firstRoundMatches.Where(m => !m.IsBye && m.Player2Id == null).ToList();

            foreach (DrawMatch match in openSlots)
            {
                if (playerIndex >= remainingPlayers.Count) break;
                match.Player2Id = remainingPlayers[playerIndex++].Id;
            }
        }
    }
}