using Timekeeper.Domain.Entities;
using Timekeeper.Domain.Enums;
using TaskStatus = Timekeeper.Domain.Enums.TaskStatus;

namespace Timekeeper.Domain.Interfaces;

public interface ITodoItemRepository
{
    Task<TodoItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TodoItem?> GetByIdWithoutNavigationPropertiesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<TodoItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<TodoItem>> GetByStatusAsync(TaskStatus status, CancellationToken cancellationToken = default);
    Task<IEnumerable<TodoItem>> GetByPriorityAsync(Priority priority, CancellationToken cancellationToken = default);
    Task<IEnumerable<TodoItem>> GetByDueDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<TodoItem>> GetByTagsAsync(string tags, CancellationToken cancellationToken = default);
    Task<IEnumerable<TodoItem>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);
    Task<IEnumerable<TodoItem>> GetByTitleAsync(string title, CancellationToken cancellationToken = default);
    Task<IEnumerable<TodoItem>> GetByTitleForDuplicateCheckAsync(string title, CancellationToken cancellationToken = default);
    Task<TodoItem> AddAsync(TodoItem todoItem, CancellationToken cancellationToken = default);
    Task UpdateAsync(TodoItem todoItem, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<TodoItem>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);
}
