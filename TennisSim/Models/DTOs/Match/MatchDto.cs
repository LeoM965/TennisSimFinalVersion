namespace TennisSim.Models.DTOs.Match
{
    public sealed record MatchDto(
        int Id,
        PlayerDto Player1,
        PlayerDto Player2,
        PlayerDto Winner,
        DateTime Date,
        string Round,
        TournamentDto Tournament
    );
}
