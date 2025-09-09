namespace TennisSim.Models.DTOs.Match
{
    public sealed record PlayerDto(
        int Id,
        string Name,
        NationalityDto Nationality
    );
}
