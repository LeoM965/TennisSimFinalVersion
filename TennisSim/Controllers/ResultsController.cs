using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TennisSim.Data;
using TennisSim.Models.Entities;

namespace TennisSim.Controllers
{
    public class ResultsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ResultsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int id)
        {
            UserName user = await GetAuthenticatedUserAsync();
            if (user == null)
                return RedirectToAction("EnterUsername", "GameStart");

            Tournament tournament = await GetTournamentWithDrawsAsync(id, user.Id, false);
            if (tournament == null)
                return NotFound();

            ViewData["CurrentDate"] = user.CurrentDate.ToString("d MMMM yyyy");
            return View(tournament);
        }

        public async Task<IActionResult> Details(int id)
        {
            UserName user = await GetAuthenticatedUserAsync();
            if (user == null)
                return RedirectToAction("EnterUsername", "GameStart");

            Tournament tournament = await GetTournamentWithDrawsAsync(id, user.Id, true);
            if (tournament == null)
                return NotFound();

            if (tournament.Schedules != null)
            {
                tournament.Schedules = tournament.Schedules
                    .Where(s => s.Date <= user.CurrentDate)
                    .ToList();
            }

            ViewData["CurrentDate"] = user.CurrentDate.ToString("d MMMM yyyy");
            return View(tournament);
        }

        private async Task<UserName> GetAuthenticatedUserAsync()
        {
            if (!HttpContext.Session.Keys.Contains("Username"))
                return null;

            string username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
                return null;

            return await _context.UserNames.FirstOrDefaultAsync(u => u.Username == username);
        }

        private async Task<Tournament> GetTournamentWithDrawsAsync(int tournamentId, int userId, bool includeAdditionalData)
        {
            IQueryable<Tournament> query = _context.Tournaments
                .Include(t => t.Draws.Where(d => d.UserId == userId))
                .ThenInclude(d => d.DrawMatches)
                .ThenInclude(dm => dm.Player1)
                .ThenInclude(p => p.Nationality)
                .Include(t => t.Draws)
                .ThenInclude(d => d.DrawMatches)
                .ThenInclude(dm => dm.Player2)
                .ThenInclude(p => p.Nationality)
                .Include(t => t.Draws)
                .ThenInclude(d => d.DrawMatches)
                .ThenInclude(dm => dm.Match)
                .Include(t => t.Draws)
                .ThenInclude(d => d.DrawMatches)
                .ThenInclude(dm => dm.Winner);

            if (includeAdditionalData)
            {
                query = query
                    .Include(t => t.PointDistributions)
                    .Include(t => t.Schedules);
            }

            return await query
                .AsSplitQuery()
                .FirstOrDefaultAsync(t => t.Id == tournamentId);
        }
    }
}