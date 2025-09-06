using TennisSim.Models;

public class ScheduleMatch
{
    public int Id { get; set; }
    public int ScheduleId { get; set; }
    public int DrawMatchId { get; set; }
    public string Court { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public MatchStatus Status { get; set; } = MatchStatus.Scheduled;
    public string Round { get; set; } = string.Empty;
    public Schedule Schedule { get; set; } = null!;
    public DrawMatch DrawMatch { get; set; } = null!;
}

public enum MatchStatus
{
    Scheduled,
    InProgress,
    Completed,
    Postponed,
    Cancelled,
    Walkover
}
