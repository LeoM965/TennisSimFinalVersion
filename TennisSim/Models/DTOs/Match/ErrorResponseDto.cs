namespace TennisSim.Models.DTOs.Match
{
    public sealed record ErrorResponseDto(
       string Error,
       string Details
   );
}
