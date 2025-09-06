namespace TennisSim.Models
{
    public class MatchViewModel
    {
        public Tournament Tournament { get; set; } = null!;
        public Match Match { get; set; } = null!;
        public IEnumerable<PlayerAttribute> Player1Attributes { get; set; } = new List<PlayerAttribute>();
        public IEnumerable<PlayerAttribute> Player2Attributes { get; set; } = new List<PlayerAttribute>();
    }

}
