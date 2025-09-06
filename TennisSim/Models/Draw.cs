namespace TennisSim.Models
{
    public class Draw
    {
        public int Id { get; set; }
        public int TournamentId { get; set; }
        public Tournament Tournament { get; set; } = null!;
        public int DrawSize { get; set; }
        public int SeedCount { get; set; }
        public int ByeCount { get; set; }
        public int UserId { get; set; }
        public UserName User { get; set; } = null!;
        public virtual ICollection<DrawMatch> DrawMatches { get; set; } = new List<DrawMatch>();
        public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
    }
}