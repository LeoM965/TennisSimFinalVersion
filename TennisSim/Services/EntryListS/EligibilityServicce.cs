using TennisSim.Data;
using TennisSim.Models.Entities;
using TennisSim.Services.EntryListS;

public class EligibilityService
{
    private readonly ApplicationDbContext _context;
    private readonly Random _random;

    public EligibilityService(ApplicationDbContext context)
    {
        _context = context;
        _random = new Random();
    }

    public List<EligiblePlayer> GetEligiblePlayers(UserName user, Tournament tournament, int userId)
    {
        List<string> playersInOtherTournaments = GetPlayersInConflictingTournaments(userId, tournament);
        int maxEntries = TournamentConfiguration.GetMaxEntries(tournament.Category);

        List<PlayerWithRanking> playersWithRankings = GetPlayersWithLatestRankings(user.CurrentDate, playersInOtherTournaments);
        List<EligiblePlayer> eligiblePlayers = FilterEligiblePlayers(playersWithRankings, tournament);
        List<EligiblePlayer> selectedPlayers = SelectPlayersRandomly(eligiblePlayers);

        return selectedPlayers
            .OrderBy(p => p.Rank)
            .Take(maxEntries)
            .ToList();
    }

    private List<string> GetPlayersInConflictingTournaments(int userId, Tournament tournament)
    {
        DateTime tournamentWeekStart = tournament.StartDate.Date.AddDays(-(int)tournament.StartDate.DayOfWeek);
        DateTime tournamentWeekEnd = tournamentWeekStart.AddDays(6);

        return _context.UserEntryLists
            .Where(uel => uel.UserNameId == userId)
            .Where(uel => uel.Tournament.StartDate >= tournamentWeekStart &&
                         uel.Tournament.StartDate <= tournamentWeekEnd)
            .SelectMany(uel => uel.EntryList.Select(el => el.PlayerName))
            .Distinct()
            .ToList();
    }

    private List<PlayerWithRanking> GetPlayersWithLatestRankings(DateTime currentDate, List<string> excludedPlayers)
    {
        return _context.Players
            .Where(p => p.Active && !excludedPlayers.Contains(p.Name))
            .Select(p => new PlayerWithRanking
            {
                Name = p.Name,
                LatestRanking = p.Rankings
                    .Where(r => r.Date <= currentDate)
                    .OrderByDescending(r => r.Date)
                    .FirstOrDefault()
            })
            .Where(p => p.LatestRanking != null)
            .ToList();
    }

    private List<EligiblePlayer> FilterEligiblePlayers(List<PlayerWithRanking> players, Tournament tournament)
    {
        return players
            .Select(p => new EligiblePlayer
            {
                Name = p.Name,
                Rank = p.LatestRanking.Rank,
                Points = p.LatestRanking.Points,
                EntryChance = 0.0 // Will be calculated next
            })
            .Where(p => TournamentConfiguration.IsEligible(p.Rank, tournament.Category))
            .Select(p => new EligiblePlayer
            {
                Name = p.Name,
                Rank = p.Rank,
                Points = p.Points,
                EntryChance = TournamentConfiguration.CalculateEntryChance(p.Rank, tournament.Category)
            })
            .ToList();
    }

    private List<EligiblePlayer> SelectPlayersRandomly(List<EligiblePlayer> eligiblePlayers)
    {
        return eligiblePlayers
            .Where(p => _random.NextDouble() <= p.EntryChance)
            .ToList();
    }
}
public class PlayerWithRanking
{
    public string Name { get; set; } = string.Empty;
    public Ranking LatestRanking { get; set; } = null!;
}

public class EligiblePlayer
{
    public string Name { get; set; } = string.Empty;
    public int Rank { get; set; }
    public int Points { get; set; }
    public double EntryChance { get; set; }
}
