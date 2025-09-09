namespace TennisSim.Models.Entities
{
    public class Match
    {
        public int Id { get; set; }
        public int Player1Id { get; set; }
        public int Player2Id { get; set; }
        public int DrawId { get; set; }
        public DateTime Date { get; set; }
        public int WinnerId { get; set; }
        public string Score { get; set; } = string.Empty;
        public string Round { get; set; } = string.Empty;
        public MatchStatus Status { get; set; } = MatchStatus.Scheduled;
        public Player Player1 { get; set; } = null!;
        public Player Player2 { get; set; } = null!;
        public Draw Draw { get; set; } = null!;
        public Player Winner { get; set; } = null!;
    }
}