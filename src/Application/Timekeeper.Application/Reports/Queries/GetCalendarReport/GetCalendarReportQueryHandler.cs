using MediatR;
using Timekeeper.Domain.Interfaces;

namespace Timekeeper.Application.Reports.Queries.GetCalendarReport;

public class GetCalendarReportQueryHandler : IRequestHandler<GetCalendarReportQuery, CalendarReportResult>
{
    private readonly ITimeEntryRepository _timeEntryRepository;
    private readonly ITodoItemRepository _todoItemRepository;

    public GetCalendarReportQueryHandler(ITimeEntryRepository timeEntryRepository, ITodoItemRepository todoItemRepository)
    {
        _timeEntryRepository = timeEntryRepository;
        _todoItemRepository = todoItemRepository;
    }

    public async Task<CalendarReportResult> Handle(GetCalendarReportQuery request, CancellationToken cancellationToken)
    {
        // Get all time entries in the date range
        var timeEntries = await _timeEntryRepository.GetTimeEntriesInDateRangeAsync(
            request.StartDate, request.EndDate, null, cancellationToken);

        // Get all todos (we'll filter in memory)
        var allTodos = await _todoItemRepository.GetAllAsync(cancellationToken);
        var todos = allTodos.Where(t => t.CreatedAt.Date >= request.StartDate.Date && 
                                       t.CreatedAt.Date <= request.EndDate.Date).ToList();

        var result = new CalendarReportResult
        {
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            ReportType = request.ReportType
        };

        // Generate calendar days
        var currentDate = request.StartDate.Date;
        while (currentDate <= request.EndDate.Date)
        {
            var dayEntries = timeEntries.Where(te => te.StartTime.Date == currentDate).ToList();
            var dayTodos = todos.Where(t => t.CreatedAt.Date == currentDate).ToList();

            var calendarDay = new CalendarDay
            {
                Date = currentDate,
                IsWeekend = currentDate.DayOfWeek == DayOfWeek.Saturday || currentDate.DayOfWeek == DayOfWeek.Sunday,
                IsToday = currentDate == DateTime.Today,
                CompletedTasks = dayTodos.Count(t => t.Status == Domain.Enums.TaskStatus.Completed),
                Activities = new List<CalendarActivity>()
            };

            // Process time entries for the day
            foreach (var te in dayEntries)
            {
                // Find the related todo item
                var relatedTodo = allTodos.FirstOrDefault(t => t.Id == te.TodoItemId);
                
                var activity = new CalendarActivity
                {
                    TimeEntryId = te.Id,
                    TodoItemId = te.TodoItemId,
                    TodoTitle = relatedTodo?.Title ?? "Unknown Task",
                    TodoCategory = relatedTodo?.Category,
                    Description = te.Description,
                    StartTime = te.StartTime,
                    EndTime = te.EndTime,
                    DurationHours = te.DurationMinutes / 60.0,
                    IsActive = te.IsActive,
                    ProviderWorkItemId = relatedTodo?.DevOpsWorkItemId,
                    Tags = relatedTodo?.Tags
                };

                calendarDay.Activities.Add(activity);
            }

            calendarDay.TotalHours = calendarDay.Activities.Sum(a => a.DurationHours);
            result.Days.Add(calendarDay);

            currentDate = currentDate.AddDays(1);
        }

        // Calculate summary
        result.Summary = CalculateSummary(result.Days);

        return result;
    }

    private CalendarSummary CalculateSummary(List<CalendarDay> days)
    {
        var summary = new CalendarSummary();

        // Basic totals
        summary.TotalHours = days.Sum(d => d.TotalHours);
        summary.CompletedTasks = days.Sum(d => d.CompletedTasks);
        summary.ActiveTimers = days.SelectMany(d => d.Activities).Count(a => a.IsActive);

        // Hours by category
        var allActivities = days.SelectMany(d => d.Activities).ToList();
        summary.HoursByCategory = allActivities
            .Where(a => !string.IsNullOrEmpty(a.TodoCategory))
            .GroupBy(a => a.TodoCategory!)
            .ToDictionary(g => g.Key, g => g.Sum(a => a.DurationHours));

        // Hours by day
        summary.HoursByDay = days.ToDictionary(d => d.Date, d => d.TotalHours);

        // Average hours per day (excluding days with no activities)
        var workingDays = days.Where(d => d.TotalHours > 0).ToList();
        summary.AverageHoursPerDay = workingDays.Any() ? workingDays.Average(d => d.TotalHours) : 0;

        // Most productive day
        var mostProductiveDay = days.OrderByDescending(d => d.TotalHours).FirstOrDefault();
        if (mostProductiveDay != null && mostProductiveDay.TotalHours > 0)
        {
            summary.MostProductiveDay = mostProductiveDay.Date;
            summary.MostProductiveDayHours = mostProductiveDay.TotalHours;
        }

        // Count unique tasks
        summary.TotalTasks = allActivities.Select(a => a.TodoItemId).Distinct().Count();

        return summary;
    }
}
