using TennisSim.Models.DTOs.Match;
using TennisSim.Models.Entities;

public static class MatchMapping
{
    public static MatchDto ToDto(this Match match)
    {
        ArgumentNullException.ThrowIfNull(match);

        return new MatchDto(
            match.Id,
            match.Player1.ToDto(),
            match.Player2.ToDto(),
            match.Winner.ToDto(),
            match.Date,
            match.Round ?? string.Empty,
            match.Draw?.Tournament?.ToDto() ?? new TournamentDto(0, string.Empty)
        );
    }

    public static PlayerDto ToDto(this Player player)
    {
        ArgumentNullException.ThrowIfNull(player);

        return new PlayerDto(
            player.Id,
            player.Name ?? string.Empty,
            player.Nationality?.ToDto() ?? new NationalityDto(0, string.Empty, string.Empty)
        );
    }

    public static NationalityDto ToDto(this Nationality nationality)
    {
        ArgumentNullException.ThrowIfNull(nationality);

        return new NationalityDto(
            nationality.Id,
            nationality.Name ?? string.Empty,
            nationality.Code ?? string.Empty
        );
    }

    public static TournamentDto ToDto(this Tournament tournament)
    {
        ArgumentNullException.ThrowIfNull(tournament);

        return new TournamentDto(
            tournament.Id,
            tournament.Name ?? string.Empty
        );
    }

    public static MatchSimulationResponseDto ToDto(this MatchSimulationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(result.Winner);

        var sets = result.Sets?.Select(ToSetScoreDto).ToArray() ?? Array.Empty<SetScoreDto>();
        var setDetails = result.SetDetails?.Select(ToDto).ToArray() ?? Array.Empty<SetDetailDto>();

        return new MatchSimulationResponseDto(
            new WinnerDto(result.Winner.Id, result.Winner.Name ?? string.Empty),
            sets,
            setDetails
        );
    }

    public static SetDetailDto ToDto(this SetDetailResult setDetail)
    {
        ArgumentNullException.ThrowIfNull(setDetail);

        var games = setDetail.Games?.Select(ToDto).ToArray() ?? Array.Empty<GameDto>();

        return new SetDetailDto(
            setDetail.SetNumber,
            games,
            setDetail.P1Score,
            setDetail.P2Score
        );
    }

    public static GameDto ToDto(this GameDetailResult game)
    {
        ArgumentNullException.ThrowIfNull(game);

        var points = game.Points?.Select(ToDto).ToArray() ?? Array.Empty<PointDto>();

        return new GameDto(
            game.GameNumber,
            game.WinnerIsPlayer1,
            points
        );
    }

    public static PointDto ToDto(this PointDetailResult point)
    {
        ArgumentNullException.ThrowIfNull(point);

        return new PointDto(
            point.P1ScoreDisplay ?? string.Empty,
            point.P2ScoreDisplay ?? string.Empty,
            point.WinnerIsPlayer1
        );
    }

    private static SetScoreDto ToSetScoreDto((int p1Score, int p2Score) set)
    {
        return new SetScoreDto(set.p1Score, set.p2Score);
    }
}