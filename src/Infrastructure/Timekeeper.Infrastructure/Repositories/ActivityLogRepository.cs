using Microsoft.EntityFrameworkCore;
using Timekeeper.Domain.Entities;
using Timekeeper.Domain.Enums;
using Timekeeper.Domain.Interfaces;
using Timekeeper.Infrastructure.Persistence;

namespace Timekeeper.Infrastructure.Repositories;

public class ActivityLogRepository : IActivityLogRepository
{
    private readonly TimekeeperDbContext _context;

    public ActivityLogRepository(TimekeeperDbContext context)
    {
        _context = context;
    }

    public async Task<ActivityLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ActivityLogs
            .Include(a => a.TodoItem)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<ActivityLog>> GetByTodoItemIdAsync(Guid todoItemId, CancellationToken cancellationToken = default)
    {
        return await _context.ActivityLogs
            .Where(a => a.TodoItemId == todoItemId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ActivityLog>> GetByLogTypeAsync(LogType logType, CancellationToken cancellationToken = default)
    {
        return await _context.ActivityLogs
            .Include(a => a.TodoItem)
            .Where(a => a.LogType == logType)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ActivityLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.ActivityLogs
            .Include(a => a.TodoItem)
            .Where(a => a.CreatedAt >= startDate && a.CreatedAt <= endDate)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ActivityLog> AddAsync(ActivityLog activityLog, CancellationToken cancellationToken = default)
    {
        _context.ActivityLogs.Add(activityLog);
        await _context.SaveChangesAsync(cancellationToken);
        return activityLog;
    }

    public async Task UpdateAsync(ActivityLog activityLog, CancellationToken cancellationToken = default)
    {
        _context.ActivityLogs.Attach(activityLog);
        _context.Entry(activityLog).State = EntityState.Modified;
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException($"ActivityLog with ID {activityLog.Id} could not be updated because it may have been modified or deleted by another process.");
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var activityLog = await _context.ActivityLogs.FindAsync(new object[] { id }, cancellationToken);
        if (activityLog != null)
        {
            _context.ActivityLogs.Remove(activityLog);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
