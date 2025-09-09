namespace TennisSim.Models.ViewModels
{
    public class MatchDetailsViewModel
    {
        public List<Match> HeadToHeadMatches { get; set; } = new List<Match>();
        public int Player1Id { get; set; }
        public int Player2Id { get; set; }
        public string Player1Name { get; set; } = string.Empty;
        public string Player2Name { get; set; } = string.Empty;
        public int Player1Wins { get; set; }
        public int Player2Wins { get; set; }
        public Dictionary<string, int> RoundStats { get; set; } = new Dictionary<string, int>();
        public string TournamentName { get; set; } = string.Empty;
        public Nationality Player1Nationality { get; set; } = null!;
        public Nationality Player2Nationality { get; set; } = null!;
        public int UserId { get; set; }
    }

}
