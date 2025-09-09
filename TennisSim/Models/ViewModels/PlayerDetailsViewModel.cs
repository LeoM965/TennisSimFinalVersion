using TennisSim.Models.Entities;

namespace TennisSim.Models.ViewModels
{
    public class PlayerDetailsViewModel
    {
        public Player Player { get; set; } = null!;
        public List<Entities.Match> RecentMatches { get; set; } = new List<Entities.Match>();
        public int TotalWins { get; set; }
        public int TotalMatches { get; set; }
        public double WinPercentage { get; set; }
    }


}
