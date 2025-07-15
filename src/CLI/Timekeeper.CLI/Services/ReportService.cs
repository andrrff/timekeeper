using MediatR;
using Spectre.Console;
using Timekeeper.Application.TimeEntries.Queries;
using Timekeeper.Application.TodoItems.Queries;
using Timekeeper.Application.Reports.Queries.GetCalendarReport;
using Timekeeper.Application.Reports.Queries.GetKanbanBoard;
using Timekeeper.Domain.Enums;
using TaskStatus = Timekeeper.Domain.Enums.TaskStatus;

namespace Timekeeper.CLI.Services;

public class ReportService : IReportService
{
    private readonly IMediator _mediator;

    public ReportService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<Dictionary<string, int>> GetTaskSummaryAsync()
    {
        var todos = await _mediator.Send(new GetAllTodoItemsQuery());
        var todoList = todos.ToList();

        return new Dictionary<string, int>
        {
            ["Total"] = todoList.Count,
            ["Completed"] = todoList.Count(t => t.Status == Timekeeper.Domain.Enums.TaskStatus.Completed),
            ["InProgress"] = todoList.Count(t => t.Status == Timekeeper.Domain.Enums.TaskStatus.InProgress),
            ["Pending"] = todoList.Count(t => t.Status == Timekeeper.Domain.Enums.TaskStatus.Pending),
            ["OnHold"] = todoList.Count(t => t.Status == Timekeeper.Domain.Enums.TaskStatus.OnHold),
            ["Cancelled"] = todoList.Count(t => t.Status == Timekeeper.Domain.Enums.TaskStatus.Cancelled)
        };
    }

    public async Task<Dictionary<string, double>> GetTimeTrackingSummaryAsync()
    {
        var todos = await _mediator.Send(new GetAllTodoItemsQuery());
        var todoList = todos.ToList();

        return new Dictionary<string, double>
        {
            ["TotalEstimated"] = todoList.Sum(t => t.EstimatedTimeMinutes) / 60.0,
            ["TotalActual"] = todoList.Sum(t => t.ActualTimeMinutes) / 60.0,
            ["AverageEstimated"] = todoList.Any() ? todoList.Average(t => t.EstimatedTimeMinutes) / 60.0 : 0,
            ["AverageActual"] = todoList.Any() ? todoList.Average(t => t.ActualTimeMinutes) / 60.0 : 0
        };
    }

    public async Task<Dictionary<string, int>> GetProductivityTrendsAsync()
    {
        var todos = await _mediator.Send(new GetAllTodoItemsQuery());
        var todoList = todos.ToList();

        var last7Days = Enumerable.Range(0, 7)
            .Select(i => DateTime.Today.AddDays(-i))
            .ToList();

        var trends = new Dictionary<string, int>();
        foreach (var date in last7Days)
        {
            var dayName = date.ToString("ddd");
            var completedCount = todoList.Count(t => 
                t.Status == Timekeeper.Domain.Enums.TaskStatus.Completed && 
                t.UpdatedAt?.Date == date);
            trends[dayName] = completedCount;
        }

        return trends;
    }

    public async Task<Dictionary<string, int>> GetCategoryAnalysisAsync()
    {
        var todos = await _mediator.Send(new GetAllTodoItemsQuery());
        var todoList = todos.ToList();

        return todoList
            .Where(t => !string.IsNullOrWhiteSpace(t.Category))
            .GroupBy(t => t.Category!)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    // CLI specific implementations
    public async Task GenerateDailyReportAsync(DateTime date)
    {
        var panel = new Panel($"[bold blue]Daily Report for {date:yyyy-MM-dd}[/]")
        {
            Border = BoxBorder.Rounded
        };
        AnsiConsole.Write(panel);

        // Get todos for the day
        var allTodos = await _mediator.Send(new GetAllTodoItemsQuery());
        var todosForDay = allTodos.Where(t => t.CreatedAt.Date == date.Date);

        // Get time entries for the day
        var allTimeEntries = await _mediator.Send(new GetTimeEntriesInDateRangeQuery(date, date.AddDays(1)));
        var timeEntriesForDay = allTimeEntries.Where(te => te.StartTime.Date == date.Date);

        // Create summary table
        var summaryTable = new Table();
        summaryTable.AddColumn("Metric");
        summaryTable.AddColumn("Value");

        summaryTable.AddRow("📋 Todos Created", todosForDay.Count().ToString());
        summaryTable.AddRow("✅ Todos Completed", todosForDay.Count(t => t.Status == Timekeeper.Domain.Enums.TaskStatus.Completed).ToString());
        summaryTable.AddRow("⏱️ Time Entries", timeEntriesForDay.Count().ToString());

        var totalTime = timeEntriesForDay
            .Where(te => te.EndTime.HasValue)
            .Sum(te => (te.EndTime!.Value - te.StartTime).TotalHours);
        summaryTable.AddRow("🕒 Total Time", $"{totalTime:F2} hours");

        AnsiConsole.Write(summaryTable);

        // Time entries detail
        if (timeEntriesForDay.Any())
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold yellow]Time Entries:[/]");
            
            var timeTable = new Table();
            timeTable.AddColumn("Todo");
            timeTable.AddColumn("Description");
            timeTable.AddColumn("Duration");

            foreach (var entry in timeEntriesForDay.OrderBy(te => te.StartTime))
            {
                var todo = allTodos.FirstOrDefault(t => t.Id == entry.TodoItemId);
                var duration = entry.EndTime.HasValue 
                    ? (entry.EndTime.Value - entry.StartTime).ToString(@"hh\:mm")
                    : "Running";

                timeTable.AddRow(
                    todo?.Title ?? "Unknown",
                    entry.Description ?? "-",
                    duration
                );
            }

            AnsiConsole.Write(timeTable);
        }
    }

    public async Task GenerateWeeklyReportAsync(DateTime? startDate = null)
    {
        var weekStart = startDate ?? DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
        var weekEnd = weekStart.AddDays(7);

        var panel = new Panel($"[bold blue]Weekly Report ({weekStart:yyyy-MM-dd} to {weekEnd.AddDays(-1):yyyy-MM-dd})[/]")
        {
            Border = BoxBorder.Rounded
        };
        AnsiConsole.Write(panel);

        // Get data for the week
        var allTodos = await _mediator.Send(new GetAllTodoItemsQuery());
        var weekTodos = allTodos.Where(t => t.CreatedAt >= weekStart && t.CreatedAt < weekEnd);

        var allTimeEntries = await _mediator.Send(new GetTimeEntriesInDateRangeQuery(weekStart, weekEnd));
        var weekTimeEntries = allTimeEntries.Where(te => te.StartTime >= weekStart && te.StartTime < weekEnd);

        // Daily breakdown
        var dailyTable = new Table();
        dailyTable.AddColumn("Day");
        dailyTable.AddColumn("Todos Created");
        dailyTable.AddColumn("Todos Completed");
        dailyTable.AddColumn("Time Tracked");

        for (int i = 0; i < 7; i++)
        {
            var day = weekStart.AddDays(i);
            var dayTodos = weekTodos.Where(t => t.CreatedAt.Date == day.Date);
            var dayTimeEntries = weekTimeEntries.Where(te => te.StartTime.Date == day.Date);
            
            var totalDayTime = dayTimeEntries
                .Where(te => te.EndTime.HasValue)
                .Sum(te => (te.EndTime!.Value - te.StartTime).TotalHours);

            dailyTable.AddRow(
                day.ToString("ddd dd/MM"),
                dayTodos.Count().ToString(),
                dayTodos.Count(t => t.Status == Timekeeper.Domain.Enums.TaskStatus.Completed).ToString(),
                $"{totalDayTime:F1}h"
            );
        }

        AnsiConsole.Write(dailyTable);

        // Week summary
        AnsiConsole.WriteLine();
        var totalWeekTime = weekTimeEntries
            .Where(te => te.EndTime.HasValue)
            .Sum(te => (te.EndTime!.Value - te.StartTime).TotalHours);

        var summaryTable = new Table();
        summaryTable.AddColumn("Week Summary");
        summaryTable.AddColumn("Value");

        summaryTable.AddRow("📋 Total Todos Created", weekTodos.Count().ToString());
        summaryTable.AddRow("✅ Total Todos Completed", weekTodos.Count(t => t.Status == Timekeeper.Domain.Enums.TaskStatus.Completed).ToString());
        summaryTable.AddRow("⏱️ Total Time Tracked", $"{totalWeekTime:F2} hours");
        summaryTable.AddRow("📊 Daily Average", $"{totalWeekTime / 7:F2} hours");

        AnsiConsole.Write(summaryTable);
    }

    public async Task GenerateTimeSummaryAsync(int days = 30)
    {
        var startDate = DateTime.Today.AddDays(-days);
        var endDate = DateTime.Today.AddDays(1);

        var panel = new Panel($"[bold blue]Time Summary (Last {days} days)[/]")
        {
            Border = BoxBorder.Rounded
        };
        AnsiConsole.Write(panel);

        var allTimeEntries = await _mediator.Send(new GetTimeEntriesInDateRangeQuery(startDate, endDate));
        var periodEntries = allTimeEntries.Where(te => te.StartTime >= startDate && te.StartTime < endDate);

        var allTodos = await _mediator.Send(new GetAllTodoItemsQuery());

        // Group by todo
        var todoSummary = periodEntries
            .Where(te => te.EndTime.HasValue)
            .GroupBy(te => te.TodoItemId)
            .Select(g => new
            {
                TodoId = g.Key,
                Todo = allTodos.FirstOrDefault(t => t.Id == g.Key),
                TotalTime = g.Sum(te => (te.EndTime!.Value - te.StartTime).TotalHours),
                EntryCount = g.Count()
            })
            .OrderByDescending(x => x.TotalTime)
            .Take(10);

        var todoTable = new Table();
        todoTable.AddColumn("Todo");
        todoTable.AddColumn("Category");
        todoTable.AddColumn("Time Spent");
        todoTable.AddColumn("Sessions");

        foreach (var item in todoSummary)
        {
            todoTable.AddRow(
                item.Todo?.Title ?? "Unknown",
                item.Todo?.Category ?? "-",
                $"{item.TotalTime:F2}h",
                item.EntryCount.ToString()
            );
        }

        AnsiConsole.Write(todoTable);

        // Category summary
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold yellow]Time by Category:[/]");

        var categorySummary = todoSummary
            .GroupBy(x => x.Todo?.Category ?? "Uncategorized")
            .Select(g => new
            {
                Category = g.Key,
                TotalTime = g.Sum(x => x.TotalTime)
            })
            .OrderByDescending(x => x.TotalTime);

        var categoryTable = new Table();
        categoryTable.AddColumn("Category");
        categoryTable.AddColumn("Time Spent");
        categoryTable.AddColumn("Percentage");

        var totalCategoryTime = categorySummary.Sum(x => x.TotalTime);

        foreach (var category in categorySummary)
        {
            var percentage = totalCategoryTime > 0 ? (category.TotalTime / totalCategoryTime) * 100 : 0;
            categoryTable.AddRow(
                category.Category,
                $"{category.TotalTime:F2}h",
                $"{percentage:F1}%"
            );
        }

        AnsiConsole.Write(categoryTable);

        // Overall summary
        AnsiConsole.WriteLine();
        var totalTime = periodEntries
            .Where(te => te.EndTime.HasValue)
            .Sum(te => (te.EndTime!.Value - te.StartTime).TotalHours);

        var overallTable = new Table();
        overallTable.AddColumn("Overall Summary");
        overallTable.AddColumn("Value");

        overallTable.AddRow("📊 Total Time Tracked", $"{totalTime:F2} hours");
        overallTable.AddRow("📅 Daily Average", $"{totalTime / days:F2} hours");
        overallTable.AddRow("⏱️ Total Sessions", periodEntries.Count().ToString());

        AnsiConsole.Write(overallTable);
    }

    public async Task GenerateCalendarReportAsync(DateTime startDate, DateTime endDate, CalendarReportType reportType)
    {
        var calendarData = await _mediator.Send(new GetCalendarReportQuery(startDate, endDate, reportType));
        
        var reportTitle = reportType switch
        {
            CalendarReportType.Daily => $"Daily Calendar - {startDate:yyyy-MM-dd}",
            CalendarReportType.Weekly => $"Weekly Calendar - {GetWeekRange(startDate, endDate)}",
            CalendarReportType.Monthly => $"Monthly Calendar - {startDate:MMMM yyyy}",
            _ => "Calendar Report"
        };

        var panel = new Panel($"[bold blue]{reportTitle}[/]")
        {
            Border = BoxBorder.Rounded
        };
        AnsiConsole.Write(panel);

        if (reportType == CalendarReportType.Daily)
        {
            await GenerateDayViewAsync(calendarData);
        }
        else if (reportType == CalendarReportType.Weekly)
        {
            await GenerateWeekViewAsync(calendarData);
        }
        else if (reportType == CalendarReportType.Monthly)
        {
            await GenerateMonthViewAsync(calendarData);
        }

        // Show summary
        AnsiConsole.WriteLine();
        DisplayCalendarSummary(calendarData.Summary);
    }

    private async Task GenerateDayViewAsync(CalendarReportResult calendarData)
    {
        var day = calendarData.Days.FirstOrDefault();
        if (day == null)
        {
            AnsiConsole.MarkupLine("[yellow]No data available for the selected day.[/]");
            return;
        }

        AnsiConsole.MarkupLine($"[bold]{day.Date:dddd, MMMM dd, yyyy}[/]");
        
        if (!day.Activities.Any())
        {
            AnsiConsole.MarkupLine("[dim]No activities recorded for this day.[/]");
            return;
        }

        // Create timeline view
        var timelineTable = new Table();
        timelineTable.AddColumn("[bold]Time[/]");
        timelineTable.AddColumn("[bold]Duration[/]");
        timelineTable.AddColumn("[bold]Task[/]");
        timelineTable.AddColumn("[bold]Category[/]");
        timelineTable.AddColumn("[bold]Description[/]");

        foreach (var activity in day.Activities.OrderBy(a => a.StartTime))
        {
            var endTimeText = activity.EndTime?.ToString("HH:mm") ?? "Active";
            var durationText = activity.IsActive ? "🔴 Active" : $"{activity.DurationHours:F2}h";
            
            var taskTitle = activity.TodoTitle.Length > 30 
                ? activity.TodoTitle.Substring(0, 27) + "..." 
                : activity.TodoTitle;

            var categoryDisplay = string.IsNullOrEmpty(activity.TodoCategory) 
                ? "[dim]None[/]" 
                : $"[green]{activity.TodoCategory}[/]";

            var descriptionDisplay = string.IsNullOrEmpty(activity.Description) 
                ? "[dim]-[/]" 
                : (activity.Description.Length > 40 
                    ? activity.Description.Substring(0, 37) + "..."
                    : activity.Description);

            var timeDisplay = activity.IsActive 
                ? $"[red]{activity.StartTime:HH:mm} - Active[/]"
                : $"{activity.StartTime:HH:mm} - {endTimeText}";

            timelineTable.AddRow(
                timeDisplay,
                durationText,
                taskTitle,
                categoryDisplay,
                descriptionDisplay
            );
        }

        AnsiConsole.Write(timelineTable);

        // Day summary
        AnsiConsole.WriteLine();
        var daySummaryTable = new Table();
        daySummaryTable.AddColumn("Metric");
        daySummaryTable.AddColumn("Value");

        daySummaryTable.AddRow("⏱️ Total Time", $"{day.TotalHours:F2} hours");
        daySummaryTable.AddRow("📋 Activities", day.Activities.Count.ToString());
        daySummaryTable.AddRow("✅ Tasks Completed", day.CompletedTasks.ToString());
        daySummaryTable.AddRow("🔴 Active Timers", day.Activities.Count(a => a.IsActive).ToString());

        AnsiConsole.Write(daySummaryTable);
    }

    private async Task GenerateWeekViewAsync(CalendarReportResult calendarData)
    {
        // Weekly grid view
        var weekTable = new Table();
        weekTable.AddColumn("[bold]Day[/]");
        weekTable.AddColumn("[bold]Date[/]");
        weekTable.AddColumn("[bold]Hours[/]");
        weekTable.AddColumn("[bold]Tasks[/]");
        weekTable.AddColumn("[bold]Main Activities[/]");

        foreach (var day in calendarData.Days.OrderBy(d => d.Date))
        {
            var dayName = day.Date.ToString("dddd");
            var dateText = day.Date.ToString("MM/dd");
            
            if (day.IsToday)
            {
                dayName = $"[bold yellow]{dayName}[/]";
                dateText = $"[bold yellow]{dateText}[/]";
            }
            else if (day.IsWeekend)
            {
                dayName = $"[dim]{dayName}[/]";
                dateText = $"[dim]{dateText}[/]";
            }

            var hoursText = day.TotalHours > 0 
                ? $"{day.TotalHours:F1}h" 
                : "[dim]0h[/]";

            var tasksText = day.Activities.Any() 
                ? day.Activities.Select(a => a.TodoItemId).Distinct().Count().ToString()
                : "[dim]0[/]";

            var mainActivities = day.Activities
                .GroupBy(a => a.TodoCategory ?? "Other")
                .OrderByDescending(g => g.Sum(a => a.DurationHours))
                .Take(2)
                .Select(g => g.Key);

            var activitiesText = mainActivities.Any() 
                ? string.Join(", ", mainActivities)
                : "[dim]None[/]";

            weekTable.AddRow(dayName, dateText, hoursText, tasksText, activitiesText);
        }

        AnsiConsole.Write(weekTable);

        // Weekly chart
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Daily Hours Visualization:[/]");
        
        var maxHours = calendarData.Days.Max(d => d.TotalHours);
        var chartWidth = 50;

        foreach (var day in calendarData.Days.OrderBy(d => d.Date))
        {
            var barLength = maxHours > 0 ? (int)((day.TotalHours / maxHours) * chartWidth) : 0;
            var bar = new string('█', barLength);
            var dayLabel = day.Date.ToString("ddd").PadRight(3);
            
            var coloredBar = day.TotalHours > 6 ? $"[green]{bar}[/]" 
                           : day.TotalHours > 3 ? $"[yellow]{bar}[/]" 
                           : $"[red]{bar}[/]";

            AnsiConsole.MarkupLine($"{dayLabel} │{coloredBar} {day.TotalHours:F1}h");
        }
    }

    private async Task GenerateMonthViewAsync(CalendarReportResult calendarData)
    {
        var month = calendarData.StartDate;
        AnsiConsole.MarkupLine($"[bold]{month:MMMM yyyy}[/]");
        AnsiConsole.WriteLine();

        // Calendar grid
        var calendar = new Table();
        calendar.AddColumn("[bold]Sun[/]");
        calendar.AddColumn("[bold]Mon[/]");
        calendar.AddColumn("[bold]Tue[/]");
        calendar.AddColumn("[bold]Wed[/]");
        calendar.AddColumn("[bold]Thu[/]");
        calendar.AddColumn("[bold]Fri[/]");
        calendar.AddColumn("[bold]Sat[/]");

        var firstDayOfMonth = new DateTime(month.Year, month.Month, 1);
        var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
        var startCalendar = firstDayOfMonth.AddDays(-(int)firstDayOfMonth.DayOfWeek);

        var currentWeek = new List<string>();
        var currentDate = startCalendar;

        while (currentDate <= lastDayOfMonth.AddDays(6 - (int)lastDayOfMonth.DayOfWeek))
        {
            var day = calendarData.Days.FirstOrDefault(d => d.Date.Date == currentDate.Date);
            var cellContent = FormatCalendarCell(currentDate, day, month.Month);
            
            currentWeek.Add(cellContent);

            if (currentWeek.Count == 7)
            {
                calendar.AddRow(currentWeek.ToArray());
                currentWeek.Clear();
            }

            currentDate = currentDate.AddDays(1);
        }

        AnsiConsole.Write(calendar);

        // Monthly statistics
        AnsiConsole.WriteLine();
        GenerateMonthlyStatistics(calendarData);
    }

    private string FormatCalendarCell(DateTime date, CalendarDay? day, int currentMonth)
    {
        var dayNumber = date.Day.ToString();
        
        if (date.Month != currentMonth)
        {
            return $"[dim]{dayNumber}[/]";
        }

        if (day == null || day.TotalHours == 0)
        {
            return date == DateTime.Today ? $"[bold yellow]{dayNumber}[/]" : dayNumber;
        }

        var hoursIndicator = day.TotalHours switch
        {
            > 8 => "🟢",
            > 4 => "🟡",
            > 0 => "🔴",
            _ => ""
        };

        var cellText = $"{dayNumber}\n[dim]{day.TotalHours:F1}h[/]";
        
        if (date == DateTime.Today)
        {
            cellText = $"[bold yellow]{cellText}[/]";
        }

        return $"{hoursIndicator}{cellText}";
    }

    private void GenerateMonthlyStatistics(CalendarReportResult calendarData)
    {
        var statsTable = new Table();
        statsTable.AddColumn("Metric");
        statsTable.AddColumn("Value");

        var workingDays = calendarData.Days.Count(d => d.TotalHours > 0);
        var weekdays = calendarData.Days.Count(d => !d.IsWeekend);
        var weekdaysWorked = calendarData.Days.Count(d => !d.IsWeekend && d.TotalHours > 0);

        statsTable.AddRow("📅 Total Days", calendarData.Days.Count.ToString());
        statsTable.AddRow("⏰ Working Days", workingDays.ToString());
        statsTable.AddRow("📊 Weekday Coverage", $"{weekdaysWorked}/{weekdays} ({(weekdays > 0 ? (double)weekdaysWorked / weekdays * 100 : 0):F1}%)");
        statsTable.AddRow("⏱️ Total Hours", $"{calendarData.Summary.TotalHours:F2}h");
        statsTable.AddRow("📈 Avg Hours/Working Day", $"{(workingDays > 0 ? calendarData.Summary.TotalHours / workingDays : 0):F2}h");

        if (calendarData.Summary.MostProductiveDay.HasValue)
        {
            statsTable.AddRow("🏆 Most Productive Day", 
                $"{calendarData.Summary.MostProductiveDay.Value:MMM dd} ({calendarData.Summary.MostProductiveDayHours:F2}h)");
        }

        AnsiConsole.Write(statsTable);

        // Category breakdown
        if (calendarData.Summary.HoursByCategory.Any())
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Time by Category:[/]");
            
            var categoryTable = new Table();
            categoryTable.AddColumn("Category");
            categoryTable.AddColumn("Hours");
            categoryTable.AddColumn("Percentage");

            foreach (var category in calendarData.Summary.HoursByCategory.OrderByDescending(c => c.Value))
            {
                var percentage = calendarData.Summary.TotalHours > 0 
                    ? (category.Value / calendarData.Summary.TotalHours) * 100 
                    : 0;
                
                categoryTable.AddRow(
                    category.Key,
                    $"{category.Value:F2}h",
                    $"{percentage:F1}%"
                );
            }

            AnsiConsole.Write(categoryTable);
        }
    }

    private void DisplayCalendarSummary(CalendarSummary summary)
    {
        var summaryPanel = new Panel(
            $"[bold]Summary[/]\n" +
            $"🕒 Total Time: [green]{summary.TotalHours:F2}h[/]\n" +
            $"📋 Total Tasks: [blue]{summary.TotalTasks}[/]\n" +
            $"✅ Completed: [green]{summary.CompletedTasks}[/]\n" +
            $"📊 Daily Average: [yellow]{summary.AverageHoursPerDay:F2}h[/]" +
            (summary.ActiveTimers > 0 ? $"\n🔴 Active Timers: [red]{summary.ActiveTimers}[/]" : "")
        )
        {
            Header = new PanelHeader("[bold blue]📊 Overview[/]"),
            Border = BoxBorder.Rounded
        };

        AnsiConsole.Write(summaryPanel);
    }

    private string GetWeekRange(DateTime startDate, DateTime endDate)
    {
        return $"{startDate:MMM dd} - {endDate:MMM dd, yyyy}";
    }

    public async Task GenerateKanbanReportAsync(string? categoryFilter = null, Priority? priorityFilter = null, TaskStatus? statusFilter = null)
    {
        var query = new GetKanbanBoardQuery(categoryFilter, priorityFilter, false);
        var board = await _mediator.Send(query);

        Console.Clear();

        // Title
        var filterText = "";
        if (!string.IsNullOrEmpty(categoryFilter) || priorityFilter.HasValue || statusFilter.HasValue)
        {
            var filters = new List<string>();
            if (!string.IsNullOrEmpty(categoryFilter)) filters.Add($"Category: {categoryFilter}");
            if (priorityFilter.HasValue) filters.Add($"Priority: {priorityFilter}");
            if (statusFilter.HasValue) filters.Add($"Status: {statusFilter}");
            filterText = $" ({string.Join(", ", filters)})";
        }

        var titleRule = new Rule($"[bold blue]📋 Kanban Board{filterText}[/]")
        {
            Style = Style.Parse("blue"),
            Justification = Justify.Center
        };
        AnsiConsole.Write(titleRule);
        AnsiConsole.WriteLine();

        // Create columns layout
        var columns = new Columns(board.Columns.Select(col => CreateColumnPanel(col)).ToArray())
        {
            Expand = true
        };

        AnsiConsole.Write(columns);
        AnsiConsole.WriteLine();

        // Statistics
        DisplayStatistics(board.Statistics);
    }

    private Panel CreateColumnPanel(KanbanColumn column)
    {
        var table = new Table();
        table.AddColumn(new TableColumn("Task").Centered());
        table.Border = TableBorder.None;

        foreach (var card in column.Cards)
        {
            var priorityColor = card.Priority switch
            {
                Priority.Critical => "red",
                Priority.High => "orange1",
                Priority.Medium => "yellow",
                Priority.Low => "green",
                _ => "white"
            };

            var cardText = $"[{priorityColor}]{card.Title}[/]";
            if (!string.IsNullOrEmpty(card.Description))
            {
                cardText += $"\n[dim]{card.Description[..Math.Min(50, card.Description.Length)]}...[/]";
            }
            
            table.AddRow(cardText);
        }

        return new Panel(table)
        {
            Header = new PanelHeader($"[bold {column.Color}]{column.Icon} {column.Name}[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(GetColorFromString(column.Color))
        };
    }

    private void DisplayStatistics(KanbanStatistics stats)
    {
        var statsTable = new Table();
        statsTable.AddColumn("Metric");
        statsTable.AddColumn("Value");
        statsTable.Border = TableBorder.Rounded;

        statsTable.AddRow("📊 Total Tasks", stats.TotalCards.ToString());
        statsTable.AddRow("✅ Completed", stats.CompletedCards.ToString());
        statsTable.AddRow("🔄 In Progress", stats.InProgressCards.ToString());
        statsTable.AddRow("📋 Pending", stats.PendingCards.ToString());
        statsTable.AddRow("📈 Completion Rate", $"{stats.CompletionPercentage:F1}%");
        statsTable.AddRow("⚡ High Priority", stats.CardsByPriority.GetValueOrDefault(Priority.High, 0).ToString());

        var summaryPanel = new Panel(statsTable)
        {
            Header = new PanelHeader("[bold blue]📈 Board Statistics[/]"),
            Border = BoxBorder.Rounded
        };

        AnsiConsole.Write(summaryPanel);
    }

    private static Color GetColorFromString(string colorName)
    {
        return colorName?.ToLower() switch
        {
            "red" => Color.Red,
            "green" => Color.Green,
            "blue" => Color.Blue,
            "yellow" => Color.Yellow,
            "cyan" => Color.Aqua,
            "magenta" => Color.Fuchsia,
            "white" => Color.White,
            "grey" or "gray" => Color.Grey,
            "orange" => Color.Orange1,
            "purple" => Color.Purple,
            _ => Color.Default
        };
    }
}