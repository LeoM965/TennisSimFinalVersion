using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TennisSim.Data;
using TennisSim.Models.Entities;
using TennisSim.Models.ViewModels;
using TennisSim.Services;

namespace TennisSim.Controllers
{
    public class PlayerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly RankingService _rankingService;

        public PlayerController(ApplicationDbContext context, RankingService rankingService)
        {
            _context = context;
            _rankingService = rankingService;
        }

        public async Task<IActionResult> Index()
        {
            UserName? user = await GetAuthenticatedUserAsync();
            if (user == null)
                return RedirectToAction("EnterUsername", "GameStart");

            await _rankingService.UpdateRankingsAsync(user.CurrentDate, user.Id);

            List<Player> players = await GetPlayersWithLatestRankings(user.Id);
            ViewData["CurrentDate"] = user.CurrentDate.ToString("d MMMM yyyy");

            return View(players);
        }

        public async Task<IActionResult> Details(int id)
        {
            UserName? user = await GetAuthenticatedUserAsync();
            if (user == null)
                return RedirectToAction("EnterUsername", "GameStart");

            await _rankingService.UpdateRankingsAsync(user.CurrentDate, user.Id);

            Player? player = await GetPlayerWithDetails(id, user.Id);
            if (player == null)
                return NotFound();

            List<Match> matches = await GetPlayerMatches(id, user);
            await AssignHistoricalRankingsToMatches(matches, user.Id);

            PlayerDetailsViewModel viewModel = CreatePlayerDetailsViewModel(player, matches, id);

            ViewData["CurrentDate"] = user.CurrentDate.ToString("d MMMM yyyy");
            ViewData["Username"] = user.Username;

            return View(viewModel);
        }

        private async Task<UserName?> GetAuthenticatedUserAsync()
        {
            if (!HttpContext.Session.Keys.Contains("Username"))
                return null;

            string? username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
                return null;

            return await _context.UserNames
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        private async Task<List<Player>> GetPlayersWithLatestRankings(int userId)
        {
            List<Ranking> userRankings = await _context.Rankings
                .Where(r => r.UserId == userId)
                .ToListAsync();

            DateTime latestRankingDate = userRankings
                .OrderByDescending(r => r.Date)
                .Select(r => r.Date)
                .FirstOrDefault();

            List<Player> players = await _context.Players
                .AsNoTracking()
                .Include(p => p.Nationality)
                .Include(p => p.Rankings.Where(r => r.UserId == userId))
                .ToListAsync();

            AssignLatestRankingsToPlayers(players, userRankings, latestRankingDate);

            return players.OrderBy(p => p.Ranking == 0 ? int.MaxValue : p.Ranking).ToList();
        }

        private static void AssignLatestRankingsToPlayers(List<Player> players, List<Ranking> userRankings, DateTime latestRankingDate)
        {
            Dictionary<int, Ranking> rankingDict = userRankings
                .Where(r => r.Date == latestRankingDate)
                .ToDictionary(r => r.PlayerId);

            foreach (Player player in players)
            {
                if (rankingDict.TryGetValue(player.Id, out var ranking))
                {
                    player.Ranking = ranking.Rank;
                }
            }
        }

        private async Task<Player?> GetPlayerWithDetails(int playerId, int userId)
        {
            return await _context.Players
                .Include(p => p.Nationality)
                .Include(p => p.Attributes)
                .Include(p => p.Rankings.Where(r => r.UserId == userId))
                .FirstOrDefaultAsync(p => p.Id == playerId);
        }

        private async Task<List<Match>> GetPlayerMatches(int playerId, UserName user)
        {
            List<int> userDrawIds = await _context.Draws
                .Where(d => d.UserId == user.Id)
                .Select(d => d.Id)
                .ToListAsync();

            DateTime currentDateOnly = user.CurrentDate.Date;

            return await _context.Matches
                .Include(m => m.Draw)
                    .ThenInclude(d => d.Tournament)
                .Include(m => m.Player1)
                    .ThenInclude(p => p.Nationality)
                .Include(m => m.Player2)
                    .ThenInclude(p => p.Nationality)
                .Where(m => (m.Player1Id == playerId || m.Player2Id == playerId) &&
                           userDrawIds.Contains(m.DrawId) &&
                           m.Date.Date <= currentDateOnly)
                .OrderByDescending(m => m.Date)
                .ToListAsync();
        }

        private async Task AssignHistoricalRankingsToMatches(List<Match> matches, int userId)
        {
            List<int> playerIds = matches
                .SelectMany(m => new[] { m.Player1Id, m.Player2Id })
                .Distinct()
                .ToList();

            List<Ranking> rankingsForPlayers = await _context.Rankings
                .Where(r => playerIds.Contains(r.PlayerId) && r.UserId == userId)
                .ToListAsync();

            foreach (Match match in matches)
            {
                AssignPlayerRankingForMatch(match.Player1, match.Player1Id, rankingsForPlayers, match.Date);
                AssignPlayerRankingForMatch(match.Player2, match.Player2Id, rankingsForPlayers, match.Date);
            }
        }

        private static void AssignPlayerRankingForMatch(Player? player, int playerId, List<Ranking> allRankings, DateTime matchDate)
        {
            if (player == null) return;

            List<Ranking> playerRankings = allRankings
                .Where(r => r.PlayerId == playerId)
                .ToList();

            player.Rankings = playerRankings;

            Ranking? ranking = playerRankings
                .Where(r => r.Date.Date <= matchDate.Date)
                .OrderByDescending(r => r.Date)
                .FirstOrDefault();

            if (ranking != null)
            {
                player.Ranking = ranking.Rank;
            }
        }

        private static PlayerDetailsViewModel CreatePlayerDetailsViewModel(Player player, List<Match> matches, int playerId)
        {
            int totalMatches = matches.Count;
            int totalWins = matches.Count(m => m.WinnerId == playerId);
            double winPercentage = totalMatches > 0 ? (double)totalWins / totalMatches * 100 : 0;

            return new PlayerDetailsViewModel
            {
                Player = player,
                RecentMatches = matches,
                TotalMatches = totalMatches,
                TotalWins = totalWins,
                WinPercentage = winPercentage
            };
        }
    }
}