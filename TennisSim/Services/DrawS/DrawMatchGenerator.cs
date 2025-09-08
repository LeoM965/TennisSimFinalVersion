using TennisSim.Models;
using TennisSim.Services.DrawS;

namespace TennisSim.Services
{
    public class DrawMatchGenerator : IDrawMatchGenerator
    {
        private readonly IDrawPlayerAssigner _playerAssigner;

        public DrawMatchGenerator(IDrawPlayerAssigner playerAssigner)
        {
            _playerAssigner = playerAssigner;
        }

        public List<DrawMatch> GenerateDrawMatches(int drawId, List<EntryList> entryList, int drawSize, int byeCount,
            int seedCount, Dictionary<string, Player> preloadedPlayers)
        {
            int roundCount = (int)Math.Log2(drawSize);
            List<DrawMatch> matches = new List<DrawMatch>(drawSize - 1);

            for (int round = 1; round <= roundCount; round++)
            {
                int matchesInRound = drawSize / (int)Math.Pow(2, round);
                for (int i = 1; i <= matchesInRound; i++)
                {
                    matches.Add(new DrawMatch
                    {
                        DrawId = drawId,
                        Round = round,
                        MatchNumber = i
                    });
                }
            }

            List<DrawMatch> firstRoundMatches = matches.Where(m => m.Round == 1).ToList();
            int seedsCount = Math.Min(seedCount, entryList.Count);

            _playerAssigner.AssignSeeds(entryList, firstRoundMatches, seedsCount, byeCount, preloadedPlayers);
            _playerAssigner.AssignRemainingPlayers(entryList, firstRoundMatches, seedsCount, preloadedPlayers);

            return matches;
        }

        public void ProcessByeMatches(List<DrawMatch> matches)
        {
            List<DrawMatch> byeMatches = matches.Where(m => m.IsBye && m.Player1Id.HasValue).ToList();

            foreach (DrawMatch byeMatch in byeMatches)
            {
                DrawMatch nextMatch = GetNextMatch(byeMatch, matches);
                bool isOddMatch = byeMatch.MatchNumber % 2 != 0;

                if (isOddMatch)
                {
                    nextMatch.Player1Id = byeMatch.Player1Id;
                    nextMatch.Player1SeedNumber = byeMatch.Player1SeedNumber;
                }
                else
                {
                    nextMatch.Player2Id = byeMatch.Player1Id;
                    nextMatch.Player2SeedNumber = byeMatch.Player1SeedNumber;
                }
            }
        }

        public DrawMatch GetNextMatch(DrawMatch currentMatch, List<DrawMatch> allMatches)
        {
            int nextRoundMatchNumber = (currentMatch.MatchNumber + 1) / 2;
            return allMatches.First(m =>
                m.Round == currentMatch.Round + 1 &&
                m.MatchNumber == nextRoundMatchNumber);
        }
    }
}