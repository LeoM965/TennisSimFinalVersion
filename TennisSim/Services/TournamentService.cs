using Microsoft.EntityFrameworkCore;
using TennisSim.Data;
using TennisSim.Models.Entities;

namespace TennisSim.Services
{
    public class TournamentService : ITournamentService
    {
        private readonly ApplicationDbContext _context;

        public TournamentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Tournament> GetAllTournaments()
        {
            return _context.Tournaments
                .OrderBy(t => t.StartDate)
                .ToList();
        }

        public Tournament GetTournamentById(int id)
        {
            Tournament? tournament = _context.Tournaments
                .Include(t => t.PointDistributions)
                .FirstOrDefault(t => t.Id == id);

            return tournament ?? throw new KeyNotFoundException($"Tournament with ID {id} not found.");
        }

        public List<PointDistribution> GetPointDistributions(TournamentCategory category)
        {
            return _context.PointDistributions
                .Where(pd => pd.Category == category)
                .ToList();
        }

        public List<Schedule> GenerateTournamentSchedule(int tournamentId, Draw draw)
        {
            Tournament? tournament = _context.Tournaments.FirstOrDefault(t => t.Id == tournamentId);
            if (tournament == null)
                return new List<Schedule>();

            List<Schedule> existingSchedules = _context.Schedules
                .Where(s => s.DrawId == draw.Id)
                .ToList();

            ScheduleGenerator scheduleGenerator = new ScheduleGenerator(tournament);
            List<Schedule> schedules = scheduleGenerator.GenerateSchedule(draw);

            List<Schedule> newSchedules = schedules
                .Where(s => !existingSchedules.Any(es => es.Date == s.Date))
                .ToList();

            if (newSchedules.Any())
            {
                _context.Schedules.AddRange(newSchedules);
                _context.SaveChanges();
            }

            return schedules;
        }

        public async Task<bool> HasUnplayedMatches(int tournamentId)
        {
            return await _context.Schedules
                .Where(s => s.Draw.TournamentId == tournamentId)
                .SelectMany(s => s.ScheduledMatches)
                .AnyAsync(m => m.Status == MatchStatus.Scheduled);
        }

        public async Task<ScheduleMatch> GetNextUnplayedMatch(int tournamentId)
        {
            ScheduleMatch? match = await _context.Schedules
                .Where(s => s.Draw.TournamentId == tournamentId)
                .SelectMany(s => s.ScheduledMatches)
                .Include(m => m.DrawMatch)
                    .ThenInclude(dm => dm.Player1)
                .Include(m => m.DrawMatch)
                    .ThenInclude(dm => dm.Player2)
                .Where(m => m.Status == MatchStatus.Scheduled)
                .OrderBy(m => m.StartTime)
                .FirstOrDefaultAsync();

            return match ?? throw new InvalidOperationException("No unplayed matches found for the specified tournament.");
        }

        public List<DateTime> GetAvailableDates(int tournamentId, DateTime startDate, DateTime currentDate)
        {
            List<DateTime> dates = new List<DateTime>();
            DateTime date = startDate.Date;

            while (date <= currentDate.Date)
            {
                dates.Add(date);
                date = date.AddDays(1);
            }

            return dates;
        }

        public List<Schedule> GetTournamentScheduleForDate(int tournamentId, Draw draw, DateTime requestedDate)
        {
            Tournament? tournament = _context.Tournaments.FirstOrDefault(t => t.Id == tournamentId);
            if (tournament == null)
                return new List<Schedule>();

            DateTime dateOnly = requestedDate.Date;
            List<Schedule> existingSchedule = GetExistingScheduleForDate(draw.Id, dateOnly);

            if (existingSchedule.Any())
                return existingSchedule;

            return GenerateAndSaveScheduleForDate(tournament, draw, dateOnly);
        }

        private List<Schedule> GetExistingScheduleForDate(int drawId, DateTime date)
        {
            return _context.Schedules
                .Where(s => s.DrawId == drawId && s.Date.Date == date)
                .Include(s => s.ScheduledMatches)
                    .ThenInclude(m => m.DrawMatch)
                        .ThenInclude(dm => dm.Match)
                .Include(s => s.ScheduledMatches)
                    .ThenInclude(m => m.DrawMatch)
                        .ThenInclude(dm => dm.Player1)
                .Include(s => s.ScheduledMatches)
                    .ThenInclude(m => m.DrawMatch)
                        .ThenInclude(dm => dm.Player2)
                .ToList();
        }

        private List<Schedule> GenerateAndSaveScheduleForDate(Tournament tournament, Draw draw, DateTime date)
        {
            ScheduleGenerator scheduleGenerator = new ScheduleGenerator(tournament);
            List<Schedule> allSchedules = scheduleGenerator.GenerateSchedule(draw);

            List<Schedule> requestedDateSchedule = allSchedules
                .Where(s => s.Date.Date == date)
                .ToList();

            if (requestedDateSchedule.Any())
            {
                _context.Schedules.AddRange(requestedDateSchedule);
                _context.SaveChanges();
            }

            return requestedDateSchedule;
        }
    }
}