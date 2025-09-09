using TennisSim.Models.Entities;

namespace TennisSim.Services.Match
{
    public class MatchSimulationService
    {
        private readonly Random _random;

        public MatchSimulationService()
        {
            _random = new Random();
        }
        public MatchSimulation.MatchResult SimulateMatch(PlayerAttribute player1Attributes, PlayerAttribute player2Attributes)
        {
            if (player1Attributes == null)
                throw new ArgumentNullException(nameof(player1Attributes));
            if (player2Attributes == null)
                throw new ArgumentNullException(nameof(player2Attributes));

            MatchSimulation simulation = new(_random);

            PlayerAttribute[] p1AttributesList = new[] { player1Attributes };
            PlayerAttribute[] p2AttributesList = new[] { player2Attributes };

            return simulation.SimulateMatch(p1AttributesList, p2AttributesList);
        }

        public MatchSimulation.MatchResult SimulateMatch(IEnumerable<PlayerAttribute> player1Attributes, IEnumerable<PlayerAttribute> player2Attributes)
        {
            if (player1Attributes == null)
                throw new ArgumentNullException(nameof(player1Attributes));
            if (player2Attributes == null)
                throw new ArgumentNullException(nameof(player2Attributes));

            MatchSimulation simulation = new(_random);
            return simulation.SimulateMatch(player1Attributes, player2Attributes);
        }

        public static List<(int P1Score, int P2Score)> ExtractSetScores(List<MatchSimulation.SetResult> sets)
        {
            return sets.Select(s => (s.P1Score, s.P2Score)).ToList();
        }

        public static List<SetDetailResult> ConvertToSetDetails(List<MatchSimulation.SetResult> sets)
        {
            return sets.Select((set, index) => new SetDetailResult
            {
                SetNumber = index + 1,
                Games = ConvertToGameDetails(set.Games),
                P1Score = set.P1Score,
                P2Score = set.P2Score
            }).ToList();
        }

        private static List<GameDetailResult> ConvertToGameDetails(List<MatchSimulation.GameResult> games)
        {
            return games.Select((game, gameIndex) => new GameDetailResult
            {
                GameNumber = gameIndex + 1,
                Points = ConvertToPointDetails(game.Points),
                WinnerIsPlayer1 = game.WinnerIsPlayer1
            }).ToList();
        }

        private static List<PointDetailResult> ConvertToPointDetails(List<MatchSimulation.PointResult> points)
        {
            return points.Select(point => new PointDetailResult
            {
                P1ScoreDisplay = point.P1ScoreDisplay,
                P2ScoreDisplay = point.P2ScoreDisplay,
                WinnerIsPlayer1 = point.WinnerIsPlayer1
            }).ToList();
        }
    }
}