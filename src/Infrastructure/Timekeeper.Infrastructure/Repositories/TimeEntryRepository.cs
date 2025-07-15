using Microsoft.EntityFrameworkCore;
using Timekeeper.Domain.Entities;
using Timekeeper.Domain.Interfaces;
using Timekeeper.Infrastructure.Persistence;

namespace Timekeeper.Infrastructure.Repositories;

public class TimeEntryRepository : ITimeEntryRepository
{
    private readonly TimekeeperDbContext _context;

    public TimeEntryRepository(TimekeeperDbContext context)
    {
        _context = context;
    }

    public async Task<TimeEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TimeEntries
            .Include(t => t.TodoItem)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<List<TimeEntry>> GetByTodoItemIdAsync(Guid todoItemId, CancellationToken cancellationToken = default)
    {
        return await _context.TimeEntries
            .Where(t => t.TodoItemId == todoItemId)
            .OrderByDescending(t => t.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<TimeEntry>> GetActiveTimeEntriesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TimeEntries
            .Include(t => t.TodoItem)
            .Where(t => t.IsActive)
            .OrderByDescending(t => t.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<TimeEntry>> GetTimeEntriesInDateRangeAsync(DateTime startDate, DateTime endDate, Guid? todoItemId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.TimeEntries
            .Include(t => t.TodoItem)
            .Where(t => t.StartTime >= startDate && t.StartTime <= endDate);

        if (todoItemId.HasValue)
        {
            query = query.Where(t => t.TodoItemId == todoItemId.Value);
        }

        return await query
            .OrderByDescending(t => t.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<TimeEntry> CreateAsync(TimeEntry timeEntry, CancellationToken cancellationToken = default)
    {
        _context.TimeEntries.Add(timeEntry);
        await _context.SaveChangesAsync(cancellationToken);
        return timeEntry;
    }

    public async Task UpdateAsync(TimeEntry timeEntry, CancellationToken cancellationToken = default)
    {
        var existingEntity = await _context.TimeEntries.FindAsync(new object[] { timeEntry.Id }, cancellationToken);
        if (existingEntity == null)
        {
            throw new InvalidOperationException($"TimeEntry with ID {timeEntry.Id} not found.");
        }

        // Update the properties of the tracked entity
        existingEntity.TodoItemId = timeEntry.TodoItemId;
        existingEntity.StartTime = timeEntry.StartTime;
        existingEntity.EndTime = timeEntry.EndTime;
        existingEntity.DurationMinutes = timeEntry.DurationMinutes;
        existingEntity.Description = timeEntry.Description;
        existingEntity.IsActive = timeEntry.IsActive;
        existingEntity.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException($"TimeEntry with ID {timeEntry.Id} could not be updated because it may have been modified or deleted by another process.");
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var timeEntry = await _context.TimeEntries.FindAsync(new object[] { id }, cancellationToken);
        if (timeEntry != null)
        {
            _context.TimeEntries.Remove(timeEntry);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        return false;
    }
}
