using MediatR;
using Timekeeper.Domain.Interfaces;
using Timekeeper.Domain.Enums;
using TaskStatus = Timekeeper.Domain.Enums.TaskStatus;

namespace Timekeeper.Application.Reports.Queries.GetKanbanBoard;

public class GetKanbanBoardQueryHandler : IRequestHandler<GetKanbanBoardQuery, KanbanBoardResult>
{
    private readonly ITodoItemRepository _todoItemRepository;
    private readonly ITimeEntryRepository _timeEntryRepository;

    public GetKanbanBoardQueryHandler(ITodoItemRepository todoItemRepository, ITimeEntryRepository timeEntryRepository)
    {
        _todoItemRepository = todoItemRepository;
        _timeEntryRepository = timeEntryRepository;
    }

    public async Task<KanbanBoardResult> Handle(GetKanbanBoardQuery request, CancellationToken cancellationToken)
    {
        // Get all todos
        var allTodos = await _todoItemRepository.GetAllAsync(cancellationToken);
        var todoList = allTodos.ToList();

        // Apply filters
        if (!string.IsNullOrEmpty(request.CategoryFilter))
        {
            todoList = todoList.Where(t => t.Category?.Equals(request.CategoryFilter, StringComparison.OrdinalIgnoreCase) == true).ToList();
        }

        if (request.PriorityFilter.HasValue)
        {
            todoList = todoList.Where(t => t.Priority == request.PriorityFilter.Value).ToList();
        }

        if (!request.IncludeCompleted)
        {
            todoList = todoList.Where(t => t.Status != TaskStatus.Completed && t.Status != TaskStatus.Cancelled).ToList();
        }

        // Get active timers for all todos
        var activeTimers = await _timeEntryRepository.GetActiveTimeEntriesAsync(cancellationToken);
        var activeTimerTodoIds = activeTimers.Select(t => t.TodoItemId).ToHashSet();

        // Create Kanban board
        var result = new KanbanBoardResult
        {
            CategoryFilter = request.CategoryFilter,
            PriorityFilter = request.PriorityFilter
        };

        // Define columns
        var columns = new List<KanbanColumn>
        {
            new() { Name = "📋 Backlog", Status = TaskStatus.Pending, Icon = "📋", Color = "blue" },
            new() { Name = "🔄 In Progress", Status = TaskStatus.InProgress, Icon = "🔄", Color = "yellow" },
            new() { Name = "⏸️ On Hold", Status = TaskStatus.OnHold, Icon = "⏸️", Color = "orange" },
            new() { Name = "✅ Completed", Status = TaskStatus.Completed, Icon = "✅", Color = "green" },
            new() { Name = "❌ Cancelled", Status = TaskStatus.Cancelled, Icon = "❌", Color = "red" }
        };

        // Populate cards for each column
        foreach (var column in columns)
        {
            var columnTodos = todoList.Where(t => t.Status == column.Status).ToList();
            
            foreach (var todo in columnTodos)
            {
                var card = new KanbanCard
                {
                    Id = todo.Id,
                    Title = todo.Title,
                    Description = todo.Description,
                    Priority = todo.Priority,
                    Category = todo.Category,
                    Tags = todo.Tags,
                    DueDate = todo.DueDate,
                    CreatedAt = todo.CreatedAt,
                    UpdatedAt = todo.UpdatedAt,
                    EstimatedHours = todo.EstimatedTimeMinutes / 60.0,
                    ActualHours = todo.ActualTimeMinutes / 60.0,
                    ProviderWorkItemId = todo.DevOpsWorkItemId,
                    ProviderUrl = todo.DevOpsUrl,
                    HasActiveTimer = activeTimerTodoIds.Contains(todo.Id),
                    IsOverdue = todo.DueDate.HasValue && todo.DueDate.Value.Date < DateTime.Today && todo.Status != TaskStatus.Completed,
                    DaysUntilDue = todo.DueDate.HasValue ? (todo.DueDate.Value.Date - DateTime.Today).Days : 0
                };

                column.Cards.Add(card);
            }

            // Sort cards within each column
            column.Cards = column.Cards.OrderBy(c => c.Priority)
                                     .ThenBy(c => c.DueDate ?? DateTime.MaxValue)
                                     .ThenByDescending(c => c.CreatedAt)
                                     .ToList();
        }

        result.Columns = columns;

        // Calculate statistics
        result.Statistics = CalculateStatistics(todoList, activeTimerTodoIds);

        return result;
    }

    private KanbanStatistics CalculateStatistics(List<Domain.Entities.TodoItem> todos, HashSet<Guid> activeTimerTodoIds)
    {
        var stats = new KanbanStatistics
        {
            TotalCards = todos.Count,
            PendingCards = todos.Count(t => t.Status == TaskStatus.Pending),
            InProgressCards = todos.Count(t => t.Status == TaskStatus.InProgress),
            CompletedCards = todos.Count(t => t.Status == TaskStatus.Completed),
            OnHoldCards = todos.Count(t => t.Status == TaskStatus.OnHold),
            CancelledCards = todos.Count(t => t.Status == TaskStatus.Cancelled),
            OverdueCards = todos.Count(t => t.DueDate.HasValue && t.DueDate.Value.Date < DateTime.Today && t.Status != TaskStatus.Completed),
            CardsWithActiveTimers = todos.Count(t => activeTimerTodoIds.Contains(t.Id)),
            TotalEstimatedHours = todos.Sum(t => t.EstimatedTimeMinutes) / 60.0,
            TotalActualHours = todos.Sum(t => t.ActualTimeMinutes) / 60.0
        };

        stats.CompletionPercentage = stats.TotalCards > 0 ? (double)stats.CompletedCards / stats.TotalCards * 100 : 0;

        // Group by priority
        stats.CardsByPriority = todos.GroupBy(t => t.Priority)
                                    .ToDictionary(g => g.Key, g => g.Count());

        // Group by category
        stats.CardsByCategory = todos.Where(t => !string.IsNullOrEmpty(t.Category))
                                    .GroupBy(t => t.Category!)
                                    .ToDictionary(g => g.Key, g => g.Count());

        return stats;
    }
}
