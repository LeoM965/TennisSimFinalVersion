using TennisSim.Models.DTOs.Draw;
using TennisSim.Models.Entities;

public static class DrawMapping
{
    public static DrawDto ToDto(this Draw draw)
    {
        return new DrawDto
        {
            Id = draw.Id,
            TournamentId = draw.TournamentId,
            TournamentName = draw.Tournament?.Name ?? string.Empty,
            TournamentStartDate = draw.Tournament?.StartDate ?? DateTime.MinValue,
            Matches = draw.DrawMatches?.Select(m => m.ToDto()).ToList() ?? new List<DrawMatchDto>()
        };
    }

    public static DrawMatchDto ToDto(this DrawMatch match)
    {
        return new DrawMatchDto
        {
            Id = match.Id,
            Round = match.Round,
            MatchNumber = match.MatchNumber,
            Player1Name = GetPlayerName(match.Player1, match.Player1Id),
            Player2Name = GetPlayerName(match.Player2, match.Player2Id, match.IsBye),
            WinnerName = match.Winner?.Name ?? string.Empty,
            Player1SeedNumber = match.Player1SeedNumber,
            Player2SeedNumber = match.Player2SeedNumber,
            IsBye = match.IsBye
        };
    }

    public static DrawListItemDto ToListItemDto(this Draw draw)
    {
        return new DrawListItemDto
        {
            Id = draw.Id,
            TournamentName = draw.Tournament?.Name ?? string.Empty,
            TournamentStartDate = draw.Tournament?.StartDate ?? DateTime.MinValue,
            Category = draw.Tournament?.Category ?? TournamentCategory.GrandSlam
        };
    }

    private static string GetPlayerName(Player player, int? playerId, bool isBye = false)
    {
        if (isBye && playerId == null)
            return "BYE";
        
        if (player?.Name != null)
            return player.Name;
        
        if (playerId.HasValue)
            return "TBD";
        
        return string.Empty;
    }
}