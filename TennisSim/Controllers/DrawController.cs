using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TennisSim.Data;
using TennisSim.Models.Entities;
using TennisSim.Services.DrawS;
using TennisSim.Models.ViewModels.Draw;

namespace TennisSim.Controllers
{
    public class DrawController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IDrawService _drawService;

        public DrawController(ApplicationDbContext context, IDrawService drawService)
        {
            _context = context;
            _drawService = drawService;
        }

        public async Task<IActionResult> Index()
        {
            var user = await GetAuthenticatedUserAsync();
            if (user == null)
                return RedirectToAction("EnterUsername", "GameStart");

            var draws = await _context.Draws
                .Include(d => d.Tournament)
                .Where(d => d.Tournament != null && d.UserId == user.Id)
                .OrderByDescending(d => d.Tournament.StartDate)
                .ToListAsync();

            var viewModel = new DrawIndexViewModel
            {
                Draws = draws.ConvertAll(d => d.ToListItemDto())
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Draw(int id)
        {
            var user = await GetAuthenticatedUserAsync();
            if (user == null)
                return RedirectToAction("EnterUsername", "GameStart");

            var draw = await GetDrawWithDetailsAsync(id, user.Id);
            if (draw?.Tournament == null)
                return NotFound("Draw or associated tournament not found");

            var viewModel = new DrawViewModel();

            if (user.CurrentDate < draw.Tournament.StartDate.AddDays(-2))
            {
                viewModel.TournamentName = draw.Tournament.Name;
                viewModel.DrawMessage = "The draw will be available closer to the tournament date.";
                return View(viewModel);
            }

            viewModel.Draw = draw.ToDto();
            return View(viewModel);
        }

        public async Task<IActionResult> GenerateDraw(int tournamentId)
        {
            var user = await GetAuthenticatedUserAsync();
            if (user == null)
                return RedirectToAction("EnterUsername", "GameStart");

            var tournament = await _context.Tournaments
                .Include(t => t.UserEntryLists.Where(uel => uel.UserNameId == user.Id))
                    .ThenInclude(uel => uel.EntryList.OrderBy(e => e.Rank))
                .FirstOrDefaultAsync(t => t.Id == tournamentId);

            if (tournament == null)
                return NotFound("Tournament not found");

            if (user.CurrentDate < tournament.StartDate.AddDays(-2))
            {
                return View("Draw", new DrawViewModel
                {
                    TournamentName = tournament.Name,
                    DrawMessage = "The draw will be available closer to the tournament date."
                });
            }

            var existingDraw = await GetDrawWithDetailsAsync(tournamentId, user.Id, true);
            if (existingDraw != null)
            {
                return View("Draw", new DrawViewModel
                {
                    Draw = existingDraw.ToDto()
                });
            }

            var userEntryList = tournament.UserEntryLists.FirstOrDefault();
            if (userEntryList?.EntryList?.Count == 0)
            {
                TempData["ErrorMessage"] = "You must view the entry list before generating the draw.";
                return RedirectToAction("Details", "Tournament", new { id = tournamentId });
            }

            var entryList = userEntryList.EntryList.ToList();
            var playerNames = entryList.ConvertAll(e => e.PlayerName);

            if (!ValidateEntryList(playerNames, out var errorMessage))
            {
                TempData["ErrorMessage"] = errorMessage;
                return RedirectToAction("Details", "Tournament", new { id = tournamentId });
            }

            try
            {
                _drawService.UpdateUserEntryListsViewStatus(tournamentId);
                var draw = _drawService.CreateNewDraw(tournament, entryList, user.Id);
                return RedirectToAction("Draw", new { id = draw.Id });
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Details", "Tournament", new { id = tournamentId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to generate draw: {ex.Message}";
                return RedirectToAction("Details", "Tournament", new { id = tournamentId });
            }
        }

        private async Task<UserName> GetAuthenticatedUserAsync()
        {
            var username = HttpContext.Session.GetString("Username");
            return string.IsNullOrEmpty(username) ? null :
                await _context.UserNames.FirstOrDefaultAsync(u => u.Username == username);
        }

        private async Task<Draw> GetDrawWithDetailsAsync(int identifier, int userId, bool byTournamentId = false)
        {
            var query = _context.Draws
                .Include(d => d.Tournament)
                .Include(d => d.DrawMatches.OrderBy(m => m.Round).ThenBy(m => m.MatchNumber))
                    .ThenInclude(m => m.Player1)
                .Include(d => d.DrawMatches)
                    .ThenInclude(m => m.Player2)
                .Include(d => d.DrawMatches)
                    .ThenInclude(m => m.Winner);

            return byTournamentId ?
                await query.FirstOrDefaultAsync(d => d.TournamentId == identifier && d.UserId == userId) :
                await query.FirstOrDefaultAsync(d => d.Id == identifier && d.UserId == userId);
        }

        private static bool ValidateEntryList(IReadOnlyCollection<string> playerNames, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (playerNames.Count == 0)
            {
                errorMessage = "Entry list is empty. Please select players for the draw first.";
                return false;
            }

            if (playerNames.Count != playerNames.Distinct().Count())
            {
                errorMessage = "Entry list contains duplicate players. Please fix the entry list first.";
                return false;
            }

            return true;
        }
    }
}