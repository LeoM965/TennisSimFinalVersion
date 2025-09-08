namespace TennisSim.Services.Match
{
    public static class MatchScoreService
    {
        public static string GenerateDetailedScore(List<MatchSimulation.SetResult> sets)
        {
            return string.Join(", ", sets.Select(FormatSetScore));
        }

        private static string FormatSetScore(MatchSimulation.SetResult set)
        {
            string gameScores = string.Join(" | ", set.Games.Select(FormatGameScore));
            return $"{set.P1Score}-{set.P2Score} {gameScores}";
        }

        private static string FormatGameScore(MatchSimulation.GameResult game)
        {
            string pointScores = string.Join(" ", game.Points.Select(FormatPointScore));
            return $"[{pointScores}]";
        }

        private static string FormatPointScore(MatchSimulation.PointResult point)
        {
            return $"{point.P1ScoreDisplay}-{point.P2ScoreDisplay}";
        }
    }
}
