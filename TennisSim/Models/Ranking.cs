using TennisSim.Models;

public class Ranking
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public DateTime Date { get; set; }
    public int Rank { get; set; }
    public int Points { get; set; }
    public int UserId { get; set; }

    public UserName User { get; set; } = null!;
}