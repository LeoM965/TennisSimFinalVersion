﻿using Microsoft.AspNetCore.Mvc;
using TennisSim.Services;
using TennisSim.Models;
using Microsoft.EntityFrameworkCore;
using TennisSim.Data;
using TennisSim.Services.DrawS;

namespace TennisSim.Controllers
{
    public class TournamentsController : Controller
    {
        private readonly ITournamentService _tournamentService;
        private readonly IEntryListService _entryListService;
        private readonly IUserService _userService;
        private readonly IDrawService _drawService;
        private readonly ApplicationDbContext _context;

        public TournamentsController(
            ITournamentService tournamentService,
            IEntryListService entryListService,
            IUserService userService,
            IDrawService drawService,
            ApplicationDbContext context)
        {
            _tournamentService = tournamentService;
            _entryListService = entryListService;
            _userService = userService;
            _drawService = drawService;
            _context = context;
        }

        public IActionResult Index()
        {
            List<Tournament> tournaments = _tournamentService.GetAllTournaments();
            return View(tournaments);
        }

        public ActionResult Details(int id)
        {
            Tournament tournament = _tournamentService.GetTournamentById(id);
            if (tournament == null)
                return NotFound();

            List<PointDistribution> pointDistributions = _tournamentService.GetPointDistributions(tournament.Category);

            Tournament tournamentModel = new Tournament
            {
                Id = tournament.Id,
                Name = tournament.Name,
                Location = tournament.Location,
                Category = tournament.Category,
                StartDate = tournament.StartDate,
                EndDate = tournament.EndDate,
                QualifyingStartDate = tournament.QualifyingStartDate,
                QualifyingEndDate = tournament.QualifyingEndDate,
                Surface = tournament.Surface,
                PointDistributions = pointDistributions
            };

            ViewData["EntryListLink"] = Url.Action("EntryList", new { id = tournament.Id });
            return View(tournamentModel);
        }

        public ActionResult EntryList(int id)
        {
            Tournament tournament = _tournamentService.GetTournamentById(id);
            if (tournament == null)
                return NotFound("Tournament not found");

            string? username = GetCurrentUsername();
            if (username == null)
                return RedirectToAction("EnterUsername", "GameStart");

            UserName? user = _context.UserNames.FirstOrDefault(u => u.Username == username);
            if (user == null)
                return NotFound("User not found");

            if (user.CurrentDate < tournament.StartDate.AddDays(-2))
            {
                ViewData["TournamentName"] = tournament.Name;
                ViewData["EntryListMessage"] = "Entry list will be available closer to the tournament date.";
                ViewData["EntryListLink"] = null;
                return View();
            }

            try
            {
                UserEntryList userEntryList = _entryListService.GetUserEntryList(user.Id, id);
                if (userEntryList == null)
                    return NotFound("Entry list not found");

                List<EntryList> entryList = _context.EntryLists
                    .Where(el => el.UserEntryListId == userEntryList.Id)
                    .ToList();

                if (!entryList.Any())
                    entryList = _entryListService.GenerateEntryList(id, user.Id);

                ViewData["TournamentName"] = tournament.Name;
                return View(entryList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error loading entry list: {ex.Message}");
            }
        }

        public async Task<ActionResult> Schedule(int id, string? username = null, DateTime? date = null)
        {
            try
            {
                Tournament tournament = _tournamentService.GetTournamentById(id);
                if (tournament == null)
                    return NotFound("Tournament not found");

                username = username ?? GetCurrentUsername();
                if (username == null)
                    return RedirectToAction("EnterUsername", "GameStart");

                UserName user = _userService.GetUserByUsername(username);
                DateTime requestedDate = date ?? user.CurrentDate;

                if (requestedDate > user.CurrentDate)
                    return RedirectToAction("Schedule", new { id, username, date = user.CurrentDate });

                UserEntryList userEntryList = _entryListService.GetUserEntryList(user.Id, id);
                if (userEntryList == null || !userEntryList.HasViewedDraw)
                    return RedirectToAction("EntryList", new { id });

                Draw? draw = await GetOrCreateDraw(id, user.Id, tournament);
                if (draw == null)
                    return NotFound("Draw could not be created or retrieved");

                List<Schedule>? schedules = _tournamentService.GetTournamentScheduleForDate(id, draw, requestedDate);
                List<DateTime> availableDates = _tournamentService.GetAvailableDates(id, tournament.StartDate, user.CurrentDate);

                TournamentScheduleViewModel viewModel = new TournamentScheduleViewModel
                {
                    TournamentName = tournament.Name,
                    TournamentId = id,
                    Schedule = schedules,
                    CurrentDate = user.CurrentDate,
                    SelectedDate = requestedDate,
                    AvailableDates = availableDates ?? new List<DateTime>(),
                    HasUnplayedMatches = await _tournamentService.HasUnplayedMatches(id)
                };

                return View(viewModel);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (DbUpdateException dbEx)
            {
                return StatusCode(500, $"Database error: {dbEx.InnerException?.Message ?? dbEx.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        private string? GetCurrentUsername()
        {
            if (!HttpContext.Session.Keys.Contains("Username"))
                return null;

            return HttpContext.Session.GetString("Username");
        }

        private async Task<Draw?> GetOrCreateDraw(int tournamentId, int userId, Tournament tournament)
        {
            Draw? draw = await _context.Draws
                .Include(d => d.DrawMatches)
                    .ThenInclude(m => m.Player1)
                .Include(d => d.DrawMatches)
                    .ThenInclude(m => m.Player2)
                .Include(d => d.DrawMatches)
                    .ThenInclude(m => m.Winner)
                .FirstOrDefaultAsync(d => d.TournamentId == tournamentId && d.UserId == userId);

            if (draw != null)
                return draw;

            Tournament? fullTournament = await _context.Tournaments
                .Include(t => t.UserEntryLists)
                    .ThenInclude(uel => uel.EntryList)
                .FirstOrDefaultAsync(t => t.Id == tournamentId);

            if (fullTournament == null)
                return null;

            List<EntryList> entryList = await GetEntryListForUser(fullTournament, userId);
            if (entryList.Count == 0)
                return null;

            await ValidatePlayersExist(entryList);
            return _drawService.CreateNewDraw(fullTournament, entryList, userId);
        }

        private static async Task<List<EntryList>> GetEntryListForUser(Tournament tournament, int userId)
        {
            UserEntryList? userEntryLists = tournament.UserEntryLists?
                .FirstOrDefault(uel => uel.UserNameId == userId);

            if (userEntryLists?.EntryList != null && userEntryLists.EntryList.Any())
                return userEntryLists.EntryList.OrderBy(e => e.Rank).ToList();

            return tournament.UserEntryLists?
                .Where(uel => uel.EntryList != null)
                .SelectMany(uel => uel.EntryList)
                .OrderBy(e => e.Rank)
                .ToList() ?? new List<EntryList>();
        }

        private async Task ValidatePlayersExist(List<EntryList> entryList)
        {
            List<string> playerNames = entryList.Select(e => e.PlayerName).ToList();
            Dictionary<string, Player> allPlayers = await _context.Players
                .Where(p => playerNames.Contains(p.Name))
                .ToDictionaryAsync(p => p.Name, p => p);

            List<string> missingPlayers = playerNames.Except(allPlayers.Keys).ToList();
            if (missingPlayers.Any())
            {
                string missingPlayersMessage = string.Join(", ", missingPlayers);
                throw new InvalidOperationException($"Missing players in database: {missingPlayersMessage}");
            }
        }
    }
}