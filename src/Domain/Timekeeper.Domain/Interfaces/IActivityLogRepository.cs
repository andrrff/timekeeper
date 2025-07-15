using Timekeeper.Domain.Entities;
using Timekeeper.Domain.Enums;

namespace Timekeeper.Domain.Interfaces;

public interface IActivityLogRepository
{
    Task<ActivityLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ActivityLog>> GetByTodoItemIdAsync(Guid todoItemId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ActivityLog>> GetByLogTypeAsync(LogType logType, CancellationToken cancellationToken = default);
    Task<IEnumerable<ActivityLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<ActivityLog> AddAsync(ActivityLog activityLog, CancellationToken cancellationToken = default);
    Task UpdateAsync(ActivityLog activityLog, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
