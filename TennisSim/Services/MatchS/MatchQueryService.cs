using Microsoft.EntityFrameworkCore;
using TennisSim.Data;
using TennisSim.Models.Entities;
using TennisSim.Services.Match;

public class MatchQueryService : IMatchQueryService
{
    private readonly ApplicationDbContext _context;

    public MatchQueryService(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<ScheduleMatch?> GetScheduleMatchWithFullDetails(int scheduleMatchId)
    {
        return await _context.ScheduleMatches
            .Include(sm => sm.DrawMatch)
                .ThenInclude(dm => dm.Player1)
                    .ThenInclude(p => p.Attributes)
            .Include(sm => sm.DrawMatch)
                .ThenInclude(dm => dm.Player2)
                    .ThenInclude(p => p.Attributes)
            .Include(sm => sm.DrawMatch)
                .ThenInclude(dm => dm.Match)
            .Include(sm => sm.DrawMatch)
                .ThenInclude(dm => dm.Draw)
                    .ThenInclude(d => d.Tournament)
            .AsNoTracking()
            .FirstOrDefaultAsync(sm => sm.Id == scheduleMatchId);
    }

    public async Task<ScheduleMatch?> GetScheduleMatchWithDrawDetails(int id)
    {
        ScheduleMatch? scheduleMatch = await _context.ScheduleMatches
            .Include(sm => sm.DrawMatch)
                .ThenInclude(dm => dm.Draw)
                    .ThenInclude(d => d.DrawMatches)
            .Include(sm => sm.DrawMatch)
                .ThenInclude(dm => dm.Match)
            .FirstOrDefaultAsync(sm => sm.Id == id);

        if (scheduleMatch == null)
            throw new InvalidOperationException($"ScheduleMatch with ID {id} not found.");

        return scheduleMatch;
    }

    public async Task EnsurePlayersAttributesLoaded(DrawMatch drawMatch)
    {
        List<Task> loadTasks = new();

        if (drawMatch.Player1?.Attributes == null)
        {
            loadTasks.Add(_context.Entry(drawMatch.Player1)
                .Collection(p => p.Attributes)
                .LoadAsync());
        }

        if (drawMatch.Player2?.Attributes == null)
        {
            loadTasks.Add(_context.Entry(drawMatch.Player2)
                .Collection(p => p.Attributes)
                .LoadAsync());
        }

        if (loadTasks.Count > 0)
            await Task.WhenAll(loadTasks);
    }
}