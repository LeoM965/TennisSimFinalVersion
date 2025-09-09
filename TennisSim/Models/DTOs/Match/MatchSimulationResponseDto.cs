namespace TennisSim.Models.DTOs.Match
{
    public sealed record MatchSimulationResponseDto(
        WinnerDto Winner,
        IReadOnlyList<SetScoreDto> Sets,
        IReadOnlyList<SetDetailDto> SetDetails
    );
}
