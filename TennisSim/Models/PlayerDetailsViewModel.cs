namespace TennisSim.Models
{
    public class PlayerDetailsViewModel
    {
        public Player Player { get; set; } = null!;
        public List<Match> RecentMatches { get; set; } = new List<Match>();
        public int TotalWins { get; set; }
        public int TotalMatches { get; set; }
        public double WinPercentage { get; set; }
    }


}
