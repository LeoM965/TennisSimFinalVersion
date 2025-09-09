using TennisSim.Models;
using TennisSim.Models.Entities;
using TennisSim.Services.DrawS;
using TennisSim.Utilities;

namespace TennisSim.Services
{

    public class DrawMatchGenerator : IDrawMatchGenerator
    {
        private readonly IDrawPlayerAssigner _playerAssigner;

        public DrawMatchGenerator(IDrawPlayerAssigner playerAssigner)
        {
            _playerAssigner = playerAssigner;
        }

        public List<DrawMatch> GenerateDrawMatches(int drawId, List<EntryList> entryList, int drawSize,
            int byeCount, int seedCount, Dictionary<string, Player> preloadedPlayers)
        {
            var roundCount = (int)Math.Log2(drawSize);
            var matches = new List<DrawMatch>(drawSize - 1);

            for (var round = 1; round <= roundCount; round++)
            {
                var matchesInRound = drawSize / (int)Math.Pow(2, round);
                for (var i = 1; i <= matchesInRound; i++)
                {
                    matches.Add(new DrawMatch
                    {
                        DrawId = drawId,
                        Round = round,
                        MatchNumber = i
                    });
                }
            }

            var firstRoundMatches = matches.Where(m => m.Round == 1).ToList();
            var seedsCount = Math.Min(seedCount, entryList.Count);

            _playerAssigner.AssignSeeds(entryList, firstRoundMatches, seedsCount, byeCount, preloadedPlayers);
            _playerAssigner.AssignRemainingPlayers(entryList, firstRoundMatches, seedsCount, preloadedPlayers);

            return matches;
        }

        public void ProcessByeMatches(List<DrawMatch> matches)
        {
            var byeMatches = matches
                .Where(m => m.IsBye && m.Player1Id.HasValue)
                .ToList();

            foreach (var byeMatch in byeMatches)
            {
                var nextMatch = GetNextMatch(byeMatch, matches);
                var isOddMatch = byeMatch.MatchNumber % 2 != 0;

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
            var nextRoundMatchNumber = (currentMatch.MatchNumber + 1) / 2;
            return allMatches.First(m =>
                m.Round == currentMatch.Round + 1 &&
                m.MatchNumber == nextRoundMatchNumber);
        }
    }

    
}