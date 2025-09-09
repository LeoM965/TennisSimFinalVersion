namespace TennisSim.Models.DTOs.Draw
{
    public class DrawMatchDto
    {
        public int Id { get; set; }
        public int Round { get; set; }
        public int MatchNumber { get; set; }
        public string Player1Name { get; set; }
        public string Player2Name { get; set; }
        public string WinnerName { get; set; }
        public int? Player1SeedNumber { get; set; }
        public int? Player2SeedNumber { get; set; }
        public bool IsBye { get; set; }
    }
}