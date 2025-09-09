namespace TennisSim.Models.DTOs.Match
{
    public sealed record GameDto(
       int GameNumber,
       bool WinnerIsPlayer1,
       IReadOnlyList<PointDto> Points
   );
}
