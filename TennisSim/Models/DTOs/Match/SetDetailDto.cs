namespace TennisSim.Models.DTOs.Match
{
    public sealed record SetDetailDto(
        int SetNumber,
        IReadOnlyList<GameDto> Games,
        int P1Score,
        int P2Score
    );
}
