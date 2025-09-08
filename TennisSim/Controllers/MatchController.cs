using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using TennisSim.Data;
using TennisSim.Models;
using TennisSim.Services;
using TennisSim.Services.Match;

namespace TennisSim.Controllers
{
    public class MatchController : Controller
    {
        private readonly IMatchService _matchService;
        private readonly ApplicationDbContext _context;

        public MatchController(IMatchService matchService, ApplicationDbContext context)
        {
            _matchService = matchService;
            _context = context;
        }

        public async Task<IActionResult> MatchDetails(int drawMatchId)
        {
            UserName user = await GetAuthenticatedUserAsync();
            if (user == null)
                return RedirectToAction("EnterUsername", "GameStart");

            DrawMatch? drawMatch = await GetDrawMatchWithDetails(drawMatchId);
            if (drawMatch == null)
                return NotFound();

            List<Match> headToHeadMatches = await GetHeadToHeadMatches(
                drawMatch.Player1Id, drawMatch.Player2Id, user.Id);

            MatchDetailsViewModel viewModel = CreateMatchDetailsViewModel(drawMatch, headToHeadMatches);
            ViewData["CurrentDate"] = user.CurrentDate.ToString("d MMMM yyyy");

            return View(viewModel);
        }

        public async Task<IActionResult> SimulateMatch(int matchId)
        {
            MatchViewModel match = await _matchService.GetMatchWithPlayersAndAttributes(matchId);
            if (match == null)
                return NotFound();

            return View(match);
        }

        [HttpPost]
        public async Task<IActionResult> RunSimulation([Required] int matchId)
        {
            if (matchId <= 0)
                return BadRequest(CreateErrorResponse("Invalid match ID", "Match ID must be a positive number"));

            try
            {
                MatchSimulationResult result = await _matchService.SimulateMatch(matchId);
                if (result == null)
                    return StatusCode(500, CreateErrorResponse("Match simulation failed", "Unable to simulate match"));

                return Ok(CreateSimulationResponse(result));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(CreateErrorResponse("Invalid match data", ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(500, CreateErrorResponse("Match simulation failed", ex.Message));
            }
            catch (Exception)
            {
                return StatusCode(500, CreateErrorResponse("Internal server error",
                    "An unexpected error occurred during match simulation"));
            }
        }

        private async Task<UserName?> GetAuthenticatedUserAsync()
        {
            string? username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
                return null;

            return await _context.UserNames.FirstOrDefaultAsync(u => u.Username == username);
        }

        private async Task<DrawMatch?> GetDrawMatchWithDetails(int drawMatchId)
        {
            return await _context.DrawMatches
                .Include(d => d.Player1)
                    .ThenInclude(p => p.Nationality)
                .Include(d => d.Player2)
                    .ThenInclude(p => p.Nationality)
                .Include(d => d.Draw.Tournament)
                .FirstOrDefaultAsync(d => d.Id == drawMatchId);
        }

        private async Task<List<Match>> GetHeadToHeadMatches(int? player1Id, int? player2Id, int userId)
        {
            return await _context.Matches
                .Where(m =>
                    ((m.Player1Id == player1Id && m.Player2Id == player2Id) ||
                    (m.Player1Id == player2Id && m.Player2Id == player1Id)) &&
                    m.Draw.UserId == userId)
                .OrderByDescending(m => m.Date)
                .Include(m => m.Player1)
                .Include(m => m.Player2)
                .Include(m => m.Winner)
                .Include(m => m.Draw)
                    .ThenInclude(d => d.Tournament)
                .AsSplitQuery()
                .ToListAsync();
        }

        private static MatchDetailsViewModel CreateMatchDetailsViewModel(DrawMatch drawMatch, List<Match> headToHeadMatches)
        {
            int? player1Id = drawMatch.Player1Id;
            int? player2Id = drawMatch.Player2Id;

            return new MatchDetailsViewModel
            {
                HeadToHeadMatches = headToHeadMatches,
                Player1Id = drawMatch.Player1.Id,
                Player2Id = drawMatch.Player2.Id,
                Player1Name = drawMatch.Player1.Name,
                Player2Name = drawMatch.Player2.Name,
                Player1Nationality = drawMatch.Player1.Nationality,
                Player2Nationality = drawMatch.Player2.Nationality,
                Player1Wins = headToHeadMatches.Count(m => m.WinnerId == player1Id),
                Player2Wins = headToHeadMatches.Count(m => m.WinnerId == player2Id),
                RoundStats = headToHeadMatches
                    .GroupBy(m => m.Round)
                    .ToDictionary(g => g.Key, g => g.Count()),
                TournamentName = drawMatch.Draw.Tournament.Name,
            };
        }

        private static object CreateErrorResponse(string error, string details)
        {
            return new { error, details };
        }

        private static object CreateSimulationResponse(MatchSimulationResult result)
        {
            return new
            {
                winner = new { id = result.Winner.Id, name = result.Winner.Name },
                sets = result.Sets.Select(s => new { p1Score = s.p1Score, p2Score = s.p2Score }),
                setDetails = result.SetDetails.Select(sd => new
                {
                    setNumber = sd.SetNumber,
                    games = sd.Games.Select(g => new
                    {
                        gameNumber = g.GameNumber,
                        winnerIsPlayer1 = g.WinnerIsPlayer1,
                        points = g.Points.Select(p => new
                        {
                            p1Score = p.P1ScoreDisplay,
                            p2Score = p.P2ScoreDisplay,
                            winnerIsPlayer1 = p.WinnerIsPlayer1
                        })
                    }),
                    p1Score = sd.P1Score,
                    p2Score = sd.P2Score
                })
            };
        }
    }
}