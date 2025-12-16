using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TennisSim.Data;
using TennisSim.Models;
using TennisSim.Models.Entities;
using TennisSim.Models.ViewModels;
using TennisSim.Services;
using TennisSim.Services.Match;

namespace TennisSim.Controllers
{
    public class GameController : Controller
    {
        private readonly IUserService _userService;
        private readonly ApplicationDbContext _context;
        private readonly IMatchService _matchService;
        private readonly RankingService _rankingService;

        public GameController(IUserService userService,
                              ApplicationDbContext context,
                              IMatchService matchService,
                              RankingService rankingService)
        {
            _userService = userService;
            _context = context;
            _matchService = matchService;
            _rankingService = rankingService;
        }

        [HttpGet]
        public IActionResult Start(string username)
        {
            UserName user = _userService.GetUserByUsername(username);
            if (user == null)
                return NotFound("User not found");

            var model = new GameStartViewModel
            {
                Username = user.Username,
                CurrentDate = user.CurrentDate
            };
            return View(model);
        }

        [HttpGet]
        public IActionResult Load() => View();

        [HttpPost]
        public IActionResult Load(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                ModelState.AddModelError("", "Username is required.");
                return View();
            }

            UserName user = _userService.GetUserByUsername(username);
            if (user == null)
            {
                ModelState.AddModelError("", "User not found. Please check the username and try again.");
                return View();
            }

            HttpContext.Session.SetString("Username", username);
            return RedirectToAction("Start", "Game", new { username });
        }

        [HttpPost]
        public async Task<IActionResult> IncrementDay(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "User ID is required." });

            UserName? user = await _context.UserNames.FirstOrDefaultAsync(u => u.Username == userId);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            try
            {
                bool skipAustralianOpen = user.CurrentDate.Month == 1 && user.CurrentDate.Day == 27;

                ValidationResult upcomingValidation = await ValidateUpcomingTournamentsAsync(user, skipAustralianOpen);
                ValidationResult activeValidation = await ValidateActiveTournamentsAsync(user, skipAustralianOpen);

                if (!upcomingValidation.IsValid)
                    return Json(new { success = false, message = upcomingValidation.Message });

                if (!activeValidation.IsValid)
                    return Json(new { success = false, message = activeValidation.Message });

                await SimulateMatchesForDayAsync(user);

                if (user.CurrentDate.DayOfWeek == DayOfWeek.Monday)
                    await _rankingService.UpdateRankingsAsync(user.CurrentDate, user.Id);

                user.CurrentDate = user.CurrentDate.AddDays(1);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    newDate = user.CurrentDate.ToString("MMMM dd, yyyy"),
                    newDay = user.CurrentDate.ToString("dddd")
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "An error occurred while processing the request.",
                    error = ex.Message,
                    inner = ex.InnerException?.Message
                });
            }
        }

        private async Task SimulateMatchesForDayAsync(UserName user)
        {
            var schedulesQuery = _context.Schedules
                .Where(s => s.Date.Date == user.CurrentDate.Date && s.Draw.UserId == user.Id);

            if (user.CurrentDate.Month == 1 && user.CurrentDate.Day == 27)
            {
                schedulesQuery = schedulesQuery
                    .Where(s => !EF.Functions.Like(s.Draw.Tournament.Name, "%Australian Open%"));
            }

            List<int> todayMatches = await schedulesQuery
                .SelectMany(s => s.ScheduledMatches)
                .Where(m => m.Status == MatchStatus.Scheduled)
                .Select(m => m.Id)
                .ToListAsync();

            foreach (int matchId in todayMatches)
            {
                await _matchService.SimulateMatch(matchId);
            }
        }


        private async Task<ValidationResult> ValidateUpcomingTournamentsAsync(UserName user, bool skipAustralianOpen = false)
        {
            DateTime dateRange = user.CurrentDate.AddDays(2).Date;
            
            IQueryable<Tournament> query = _context.Tournaments
                .Where(t => t.StartDate.Date > user.CurrentDate.Date && t.StartDate.Date <= dateRange);

            if (skipAustralianOpen)
                query = query.Where(t => !EF.Functions.Like(t.Name, "%Australian Open%"));

            List<Tournament> upcomingTournaments = await query.AsNoTracking().ToListAsync();

            if (upcomingTournaments.Count == 0)
                return ValidationResult.Valid;

            List<int> tournamentIds = upcomingTournaments.Select(t => t.Id).ToList();
            Dictionary<int, UserEntryList> entryListDict = await _context.UserEntryLists
                .Where(uel => uel.UserNameId == user.Id && tournamentIds.Contains(uel.TournamentId))
                .AsNoTracking()
                .ToDictionaryAsync(uel => uel.TournamentId);

            foreach (Tournament tournament in upcomingTournaments)
            {
                if (!entryListDict.TryGetValue(tournament.Id, out var userEntryList) || !userEntryList.HasViewedDraw)
                    return new ValidationResult(false,
                        $"You must view both the entry list and draw for {tournament.Name} before proceeding.");
            }

            return ValidationResult.Valid;
        }

        private async Task<ValidationResult> ValidateActiveTournamentsAsync(UserName user, bool skipAustralianOpen = false)
        {
            IQueryable<Tournament> query = _context.Tournaments
                .Where(t => t.StartDate.Date <= user.CurrentDate.Date && t.EndDate.Date >= user.CurrentDate.Date);

            if (skipAustralianOpen)
                query = query.Where(t => !EF.Functions.Like(t.Name, "%Australian Open%"));

            List<Tournament> activeTournaments = await query.AsNoTracking().ToListAsync();

            if (activeTournaments.Count == 0)
                return ValidationResult.Valid;

            List<int> tournamentIds = activeTournaments.Select(t => t.Id).ToList();
            Dictionary<int, Draw> drawsDict = await _context.Draws
                .Where(d => tournamentIds.Contains(d.TournamentId) && d.UserId == user.Id)
                .AsNoTracking()
                .ToDictionaryAsync(d => d.TournamentId);

            List<int> userDrawIds = drawsDict.Values.Select(d => d.Id).ToList();
            HashSet<int> scheduledDrawIds = (await _context.Schedules
                .Where(s => s.Date.Date == user.CurrentDate.Date && userDrawIds.Contains(s.DrawId))
                .Select(s => s.DrawId)
                .ToListAsync())
                .ToHashSet();

            foreach (Tournament tournament in activeTournaments)
            {
                if (!drawsDict.TryGetValue(tournament.Id, out var userDraw))
                {
                    return new ValidationResult(false,
                        $"You must create a draw for {tournament.Name} before proceeding.");
                }

                if (!scheduledDrawIds.Contains(userDraw.Id))
                    return new ValidationResult(false,
                        $"You must create a schedule for {tournament.Name} before proceeding.");
            }

            return ValidationResult.Valid;
        }
    }
}
