using Timekeeper.Domain.Entities;

namespace Timekeeper.Domain.Interfaces;

public interface ITimeEntryRepository
{
    Task<TimeEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<TimeEntry>> GetByTodoItemIdAsync(Guid todoItemId, CancellationToken cancellationToken = default);
    Task<List<TimeEntry>> GetActiveTimeEntriesAsync(CancellationToken cancellationToken = default);
    Task<List<TimeEntry>> GetTimeEntriesInDateRangeAsync(DateTime startDate, DateTime endDate, Guid? todoItemId = null, CancellationToken cancellationToken = default);
    Task<TimeEntry> CreateAsync(TimeEntry timeEntry, CancellationToken cancellationToken = default);
    Task UpdateAsync(TimeEntry timeEntry, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
