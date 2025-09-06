namespace TennisSim.Models
{
    public class Player
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int YearOfBirth { get; set; }
        public int NationalityId { get; set; }
        public int Ranking { get; set; }
        public bool Active { get; set; } = true;

        public Nationality Nationality { get; set; } = null!;
        public ICollection<PlayerAttribute> Attributes { get; set; } = new List<PlayerAttribute>();
        public ICollection<Match> MatchesAsPlayer1 { get; set; } = new List<Match>();
        public ICollection<Match> MatchesAsPlayer2 { get; set; } = new List<Match>();
        public ICollection<Ranking> Rankings { get; set; } = new List<Ranking>();

        public int LatestPoints => Rankings?.OrderByDescending(r => r.Date).FirstOrDefault()?.Points ?? 0;
    }
}