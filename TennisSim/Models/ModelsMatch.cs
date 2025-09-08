using TennisSim.Models;

public class MatchSimulationResult
{
    public List<(int p1Score, int p2Score)> Sets { get; set; } = new();
    public Player Winner { get; set; }
    public List<SetDetailResult> SetDetails { get; set; } = new();
}

public class SetDetailResult
{
    public int SetNumber { get; set; }
    public List<GameDetailResult> Games { get; set; } = new();
    public int P1Score { get; set; }
    public int P2Score { get; set; }
}

public class GameDetailResult
{
    public int GameNumber { get; set; }
    public List<PointDetailResult> Points { get; set; } = new();
    public bool WinnerIsPlayer1 { get; set; }
}

public class PointDetailResult
{
    public string P1ScoreDisplay { get; set; } = string.Empty;
    public string P2ScoreDisplay { get; set; } = string.Empty;
    public bool WinnerIsPlayer1 { get; set; }
}