namespace TennisSim.Models.DTOs.Draw
{
    public class DrawDto
    {
        public int Id { get; set; }
        public int TournamentId { get; set; }
        public string TournamentName { get; set; }
        public DateTime TournamentStartDate { get; set; }
        public List<DrawMatchDto> Matches { get; set; } = new List<DrawMatchDto>();
    }
}
