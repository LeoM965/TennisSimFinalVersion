namespace TennisSim.Models.DTOs.Match
{
    public sealed record PointDto(
        string P1ScoreDisplay,
        string P2ScoreDisplay,
        bool WinnerIsPlayer1
    );
}
