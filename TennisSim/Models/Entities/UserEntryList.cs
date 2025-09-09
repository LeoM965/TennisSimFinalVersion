namespace TennisSim.Models.Entities
{
    public class UserEntryList
    {
        public int Id { get; set; }
        public int UserNameId { get; set; }
        public int TournamentId { get; set; }
        public bool HasViewedDraw { get; set; }
        public UserName User { get; set; } = null!;
        public Tournament Tournament { get; set; } = null!;
        public ICollection<EntryList> EntryList { get; set; } = new List<EntryList>();
    }
}

