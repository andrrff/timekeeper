using Timekeeper.Domain.Entities;
using Timekeeper.Domain.Enums;
using Timekeeper.Application.Reports.Queries.GetCalendarReport;
using TaskStatus = Timekeeper.Domain.Enums.TaskStatus;

namespace Timekeeper.CLI.Services;

public interface ICommandService
{
    Task<TodoItem> CreateTodoAsync(string title, string? description = null, Priority priority = Priority.Medium);
    Task<bool> UpdateTodoAsync(Guid id, string? title = null, string? description = null);
    Task<bool> DeleteTodoAsync(Guid id);
    Task<bool> CompleteTodoAsync(Guid id);
}

public interface ITodoService
{
    Task<IEnumerable<TodoItem>> GetAllTodosAsync();
    Task<TodoItem?> GetTodoByIdAsync(Guid id);
    Task<TodoItem?> GetTodoByIdAsync(int id);
    Task<IEnumerable<TodoItem>> SearchTodosAsync(string searchTerm);
    Task<IEnumerable<TodoItem>> GetTodosByStatusAsync(Timekeeper.Domain.Enums.TaskStatus status);
    Task<IEnumerable<TodoItem>> GetTodosByPriorityAsync(Priority priority);
    
    // CLI specific methods
    Task CreateTodoAsync(string title, string? description, Priority priority, string? category, string? tags, DateTime? dueDate);
    Task ListTodosAsync(Timekeeper.Domain.Enums.TaskStatus? status = null, string? category = null, int limit = 10);
    Task CompleteTodoAsync(int id);
    Task DeleteTodoAsync(int id);
    Task UpdateTodoAsync(int id, string? title, string? description, Priority? priority, string? category, string? tags);
}

public interface ITimeTrackingService
{
    Task StartTimerAsync(int todoId, string? description = null);
    Task StopTimerAsync(int? entryId = null);
    Task AddManualTimeEntryAsync(int todoId, DateTime start, DateTime end, string? description = null);
    Task ListTimeEntriesAsync(DateTime? date = null, int days = 7);
    Task ShowActiveTimersAsync();
    
    // Additional methods needed by TimeTrackingUI
    Task<bool> IsTimerActiveAsync(Guid todoId);
    Task<TimeSpan> GetElapsedTimeAsync(Guid todoId);
    Task<IEnumerable<TimeEntry>> GetAllActiveTimersAsync();
    Task<TimeSpan> GetTotalTimeSpentAsync(Guid todoId);
    Task<TimeSpan> GetRemainingTimeAsync(Guid todoId);
    Task UpdateManualTimeAsync(Guid todoId, DateTime start, DateTime end);
}

public interface IReportService
{
    Task<Dictionary<string, int>> GetTaskSummaryAsync();
    Task<Dictionary<string, double>> GetTimeTrackingSummaryAsync();
    Task<Dictionary<string, int>> GetProductivityTrendsAsync();
    Task<Dictionary<string, int>> GetCategoryAnalysisAsync();
    
    // CLI specific methods
    Task GenerateDailyReportAsync(DateTime date);
    Task GenerateWeeklyReportAsync(DateTime? startDate = null);
    Task GenerateTimeSummaryAsync(int days = 30);
    
    // Calendar reports
    Task GenerateCalendarReportAsync(DateTime startDate, DateTime endDate, CalendarReportType reportType);
    
    // Kanban reports
    Task GenerateKanbanReportAsync(string? categoryFilter = null, Priority? priorityFilter = null, TaskStatus? statusFilter = null);
}

public interface IDevOpsService
{
    Task<bool> TestConnectionAsync();
    Task<IEnumerable<object>> SyncWorkItemsAsync();
    Task<bool> ConfigureIntegrationAsync(string organizationUrl, string pat);
    Task ShowStatusAsync();
}

public interface IGitHubService
{
    Task<bool> TestConnectionAsync(string organizationUrl, string personalAccessToken);
    Task<IEnumerable<object>> GetIssuesAsync(string organizationUrl, string personalAccessToken, string? repositoryName = null);
    Task<object?> GetIssueByIdAsync(string organizationUrl, string personalAccessToken, string repository, int issueNumber);
    Task<bool> UpdateIssueAsync(string organizationUrl, string personalAccessToken, string repository, int issueNumber, object updates);
    Task<IEnumerable<string>> GetRepositoriesAsync(string organizationUrl, string personalAccessToken);
    
    // CLI-specific methods
    Task<bool> ConfigureIntegrationAsync(string organizationUrl, string pat);
}
