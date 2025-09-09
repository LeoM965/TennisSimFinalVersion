using TennisSim.Models.Entities;

namespace TennisSim.Models.DTOs.Draw
{
    public class DrawListItemDto
    {
        public int Id { get; set; }
        public string TournamentName { get; set; }
        public DateTime TournamentStartDate { get; set; }
        public TournamentCategory Category { get; set; }
    }
}
