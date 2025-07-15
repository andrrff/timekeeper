using Spectre.Console;
using Timekeeper.CLI.Services;

namespace Timekeeper.CLI.UI;

public class ReportsUI
{
    private readonly IReportService _reportService;
    private readonly ITodoService _todoService;
    private readonly CalendarUI _calendarUI;
    private readonly KanbanUI _kanbanUI;

    public ReportsUI(IReportService reportService, ITodoService todoService, CalendarUI calendarUI, KanbanUI kanbanUI)
    {
        _reportService = reportService;
        _todoService = todoService;
        _calendarUI = calendarUI;
        _kanbanUI = kanbanUI;
    }
    public async Task ShowAsync()
    {        
        while (true)
        {
            var menuItems = new List<(string key, string icon, string value, string description)>
            {
                ("1", "📊", "summary", "View comprehensive task summary report"),
                ("2", "⏱️", "timetracking", "Analyze time spent on tasks"),
                ("3", "📈", "productivity", "Track productivity trends over time"),
                ("4", "🏷️", "categories", "Analyze tasks by category distribution"),
                ("5", "📅", "duedates", "Review upcoming and overdue tasks"),
                ("6", "📋", "status", "View task status distribution"),
                ("7", "📆", "calendar_view", "Calendar View - Visual timeline of your activities"),
                ("8", "🗃️", "kanban_board", "Kanban Board - Visual task management and workflow"),
                ("0", "⬅️", "back", "Return to main menu")
            };

            var choice = await ShowInteractiveMenuAsync("📊 Reports & Analytics", menuItems,
                "Analyze your productivity and task management patterns");

            switch (choice)
            {
                case "summary":
                    await ShowTaskSummaryAsync();
                    break;
                case "timetracking":
                    await ShowTimeTrackingAsync();
                    break;
                case "productivity":
                    await ShowProductivityTrendsAsync();
                    break;
                case "categories":
                    await ShowCategoryAnalysisAsync();
                    break;
                case "duedates":
                    await ShowDueDateOverviewAsync();
                    break;
                case "status":
                    await ShowStatusDistributionAsync();
                    break;
                case "calendar_view":
                    await _calendarUI.ShowAsync();
                    break;
                case "kanban_board":
                    await _kanbanUI.ShowAsync();
                    break;
                case "back":
                    return;
            }
        }
    }

    private async Task ShowTaskSummaryAsync()
    {
        AnsiConsole.MarkupLine("[bold blue]Task Summary Report[/]");
        AnsiConsole.WriteLine();

        await AnsiConsole.Status()
            .Start("Generating report...", async ctx =>
            {
                var taskSummary = await _reportService.GetTaskSummaryAsync();
                var allTodos = await _todoService.GetAllTodosAsync();
                var todosList = allTodos.ToList();

                if (!todosList.Any())
                {
                    AnsiConsole.MarkupLine("[yellow]No tasks found. Create some tasks first to see reports.[/]");
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine("Press [blue]any key[/] to continue...");
                    Console.ReadKey();
                    return;
                }

                // Calculate real statistics
                var totalTasks = todosList.Count;
                var completedTasks = todosList.Count(t => t.Status == Timekeeper.Domain.Enums.TaskStatus.Completed);
                var inProgressTasks = todosList.Count(t => t.Status == Timekeeper.Domain.Enums.TaskStatus.InProgress);
                var pendingTasks = todosList.Count(t => t.Status == Timekeeper.Domain.Enums.TaskStatus.Pending);
                var onHoldTasks = todosList.Count(t => t.Status == Timekeeper.Domain.Enums.TaskStatus.OnHold);
                var cancelledTasks = todosList.Count(t => t.Status == Timekeeper.Domain.Enums.TaskStatus.Cancelled);

                var chart = new BreakdownChart()
                    .Width(60)
                    .ShowPercentage();

                if (completedTasks > 0)
                    chart.AddItem("Completed", completedTasks, Color.Green);
                if (inProgressTasks > 0)
                    chart.AddItem("In Progress", inProgressTasks, Color.Yellow);
                if (pendingTasks > 0)
                    chart.AddItem("Pending", pendingTasks, Color.Grey);
                if (onHoldTasks > 0)
                    chart.AddItem("On Hold", onHoldTasks, Color.Orange1);
                if (cancelledTasks > 0)
                    chart.AddItem("Cancelled", cancelledTasks, Color.Red);

                AnsiConsole.Write(chart);

                var table = new Table();
                table.AddColumn("Metric");
                table.AddColumn("Value");
                table.AddRow("Total Tasks", totalTasks.ToString());
                table.AddRow("Completed", $"[green]{completedTasks} ({(totalTasks > 0 ? (completedTasks * 100.0 / totalTasks) : 0):F1}%)[/]");
                table.AddRow("In Progress", $"[yellow]{inProgressTasks} ({(totalTasks > 0 ? (inProgressTasks * 100.0 / totalTasks) : 0):F1}%)[/]");
                table.AddRow("Pending", $"[grey]{pendingTasks} ({(totalTasks > 0 ? (pendingTasks * 100.0 / totalTasks) : 0):F1}%)[/]");
                table.AddRow("On Hold", $"[orange1]{onHoldTasks} ({(totalTasks > 0 ? (onHoldTasks * 100.0 / totalTasks) : 0):F1}%)[/]");
                table.AddRow("Cancelled", $"[red]{cancelledTasks} ({(totalTasks > 0 ? (cancelledTasks * 100.0 / totalTasks) : 0):F1}%)[/]");

                AnsiConsole.Write(table);
            });

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("Press [blue]any key[/] to continue...");
        Console.ReadKey();
    }

    private async Task ShowTimeTrackingAsync()
    {
        AnsiConsole.MarkupLine("[bold blue]Time Tracking Report[/]");
        AnsiConsole.WriteLine();

        await AnsiConsole.Status()
            .Start("Calculating time entries...", async ctx =>
            {
                var timeTrackingSummary = await _reportService.GetTimeTrackingSummaryAsync();
                var allTodos = await _todoService.GetAllTodosAsync();
                var todosList = allTodos.ToList();

                if (!todosList.Any())
                {
                    AnsiConsole.MarkupLine("[yellow]No tasks found. Create some tasks with time estimates to see time tracking reports.[/]");
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine("Press [blue]any key[/] to continue...");
                    Console.ReadKey();
                    return;
                }

                // Calculate real time tracking data
                var totalEstimatedTime = todosList.Sum(t => t.EstimatedTimeMinutes);
                var totalActualTime = todosList.Sum(t => t.ActualTimeMinutes);
                var completedTasks = todosList.Where(t => t.Status == Timekeeper.Domain.Enums.TaskStatus.Completed).ToList();
                var avgTimePerTask = completedTasks.Any() ? completedTasks.Average(t => t.ActualTimeMinutes) : 0;

                var table = new Table();
                table.AddColumn("Metric");
                table.AddColumn("Value");

                table.AddRow("Total Estimated Time", $"[blue]{totalEstimatedTime / 60.0:F1}h ({totalEstimatedTime} min)[/]");
                table.AddRow("Total Actual Time", $"[green]{totalActualTime / 60.0:F1}h ({totalActualTime} min)[/]");
                table.AddRow("Tasks Completed", $"[yellow]{completedTasks.Count}[/]");
                table.AddRow("Avg. Time per Completed Task", $"[grey]{avgTimePerTask / 60.0:F1}h ({avgTimePerTask:F0} min)[/]");
                
                if (totalEstimatedTime > 0)
                {
                    var efficiency = (totalActualTime / (double)totalEstimatedTime) * 100;
                    var efficiencyColor = efficiency <= 100 ? "green" : efficiency <= 150 ? "yellow" : "red";
                    table.AddRow("Time Efficiency", $"[{efficiencyColor}]{efficiency:F1}%[/]");
                }

                AnsiConsole.Write(table);

                // Show breakdown by priority
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[bold yellow]Time by Priority:[/]");
                
                var priorityTable = new Table();
                priorityTable.AddColumn("Priority");
                priorityTable.AddColumn("Tasks");
                priorityTable.AddColumn("Estimated Time");
                priorityTable.AddColumn("Actual Time");

                var priorities = Enum.GetValues<Timekeeper.Domain.Enums.Priority>();
                foreach (var priority in priorities)
                {
                    var priorityTasks = todosList.Where(t => t.Priority == priority).ToList();
                    if (priorityTasks.Any())
                    {
                        var estTime = priorityTasks.Sum(t => t.EstimatedTimeMinutes);
                        var actTime = priorityTasks.Sum(t => t.ActualTimeMinutes);
                        var priorityColor = priority switch
                        {
                            Timekeeper.Domain.Enums.Priority.High => "red",
                            Timekeeper.Domain.Enums.Priority.Medium => "yellow",
                            Timekeeper.Domain.Enums.Priority.Low => "green",
                            _ => "grey"
                        };

                        priorityTable.AddRow(
                            $"[{priorityColor}]{priority}[/]",
                            priorityTasks.Count.ToString(),
                            $"{estTime / 60.0:F1}h",
                            $"{actTime / 60.0:F1}h"
                        );
                    }
                }

                AnsiConsole.Write(priorityTable);
            });

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("Press [blue]any key[/] to continue...");
        Console.ReadKey();
    }

    private async Task ShowProductivityTrendsAsync()
    {
        AnsiConsole.MarkupLine("[bold blue]Productivity Trends[/]");
        AnsiConsole.WriteLine();

        await AnsiConsole.Status()
            .Start("Analyzing productivity patterns...", async ctx =>
            {
                var productivityTrends = await _reportService.GetProductivityTrendsAsync();
                var allTodos = await _todoService.GetAllTodosAsync();
                var todosList = allTodos.ToList();

                if (!todosList.Any())
                {
                    AnsiConsole.MarkupLine("[yellow]No tasks found. Create some tasks to see productivity trends.[/]");
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine("Press [blue]any key[/] to continue...");
                    Console.ReadKey();
                    return;
                }

                // Calculate productivity by creation date
                var last7Days = Enumerable.Range(0, 7)
                    .Select(i => DateTime.Today.AddDays(-i))
                    .Reverse()
                    .ToList();

                var barChart = new BarChart()
                    .Width(60)
                    .Label("[green bold underline]Tasks Created by Day (Last 7 Days)[/]")
                    .CenterLabel();

                var colors = new[] { Color.Red, Color.Yellow, Color.Green, Color.Blue, Color.Purple, Color.Orange1, Color.Cyan1 };
                var maxTasks = 0;
                var totalTasks = 0;

                for (int i = 0; i < last7Days.Count; i++)
                {
                    var day = last7Days[i];
                    var tasksForDay = todosList.Count(t => t.CreatedAt.Date == day.Date);
                    totalTasks += tasksForDay;
                    maxTasks = Math.Max(maxTasks, tasksForDay);
                    
                    barChart.AddItem(day.ToString("ddd"), tasksForDay, colors[i % colors.Length]);
                }

                AnsiConsole.Write(barChart);

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[bold]Key Insights:[/]");
                
                if (totalTasks > 0)
                {
                    var avgTasksPerDay = totalTasks / 7.0;
                    var mostProductiveDay = last7Days
                        .Select(d => new { Day = d, Count = todosList.Count(t => t.CreatedAt.Date == d.Date) })
                        .OrderByDescending(x => x.Count)
                        .First();

                    AnsiConsole.MarkupLine($"• Most productive day: [green]{mostProductiveDay.Day:dddd} ({mostProductiveDay.Count} tasks)[/]");
                    AnsiConsole.MarkupLine($"• Daily average: [yellow]{avgTasksPerDay:F1} tasks/day[/]");
                    AnsiConsole.MarkupLine($"• Total tasks (7 days): [blue]{totalTasks} tasks[/]");

                    // Calculate completion rate
                    var completedTasks = todosList.Count(t => t.Status == Timekeeper.Domain.Enums.TaskStatus.Completed);
                    var completionRate = (completedTasks * 100.0) / todosList.Count;
                    var completionColor = completionRate >= 70 ? "green" : completionRate >= 50 ? "yellow" : "red";
                    AnsiConsole.MarkupLine($"• Overall completion rate: [{completionColor}]{completionRate:F1}%[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine("• [yellow]No tasks created in the last 7 days[/]");
                }
            });

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("Press [blue]any key[/] to continue...");
        Console.ReadKey();
    }

    private async Task ShowCategoryAnalysisAsync()
    {
        AnsiConsole.MarkupLine("[bold blue]Category Analysis[/]");
        AnsiConsole.WriteLine();

        await AnsiConsole.Status()
            .Start("Analyzing categories...", async ctx =>
            {
                var categoryAnalysis = await _reportService.GetCategoryAnalysisAsync();
                var allTodos = await _todoService.GetAllTodosAsync();
                var todosList = allTodos.ToList();

                if (!todosList.Any())
                {
                    AnsiConsole.MarkupLine("[yellow]No tasks found. Create some tasks with categories to see category analysis.[/]");
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine("Press [blue]any key[/] to continue...");
                    Console.ReadKey();
                    return;
                }

                // Group by category and analyze
                var categoryGroups = todosList
                    .GroupBy(t => string.IsNullOrWhiteSpace(t.Category) ? "Uncategorized" : t.Category)
                    .OrderByDescending(g => g.Count())
                    .ToList();

                var table = new Table();
                table.AddColumn("Category");
                table.AddColumn("Total Tasks");
                table.AddColumn("Completed");
                table.AddColumn("Completion Rate");
                table.AddColumn("Avg. Estimated Time");

                foreach (var group in categoryGroups)
                {
                    var totalTasks = group.Count();
                    var completedTasks = group.Count(t => t.Status == Timekeeper.Domain.Enums.TaskStatus.Completed);
                    var completionRate = totalTasks > 0 ? (completedTasks * 100.0 / totalTasks) : 0;
                    var avgTime = group.Any() ? group.Average(t => t.EstimatedTimeMinutes) : 0;
                    
                    var completionColor = completionRate >= 70 ? "green" : completionRate >= 50 ? "yellow" : "red";

                    table.AddRow(
                        $"[blue]{group.Key}[/]",
                        totalTasks.ToString(),
                        completedTasks.ToString(),
                        $"[{completionColor}]{completionRate:F1}%[/]",
                        $"{avgTime / 60.0:F1}h"
                    );
                }

                AnsiConsole.Write(table);

                // Show category distribution chart
                if (categoryGroups.Count > 1)
                {
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine("[bold yellow]Category Distribution:[/]");
                    
                    var chart = new BreakdownChart()
                        .Width(60)
                        .ShowPercentage();

                    var colors = new[] { Color.Blue, Color.Green, Color.Yellow, Color.Red, Color.Purple, Color.Orange1, Color.Cyan1, Color.Pink1 };
                    for (int i = 0; i < Math.Min(categoryGroups.Count, colors.Length); i++)
                    {
                        chart.AddItem(categoryGroups[i].Key, categoryGroups[i].Count(), colors[i]);
                    }

                    AnsiConsole.Write(chart);
                }
            });

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("Press [blue]any key[/] to continue...");
        Console.ReadKey();
    }

    private async Task ShowDueDateOverviewAsync()
    {
        AnsiConsole.MarkupLine("[bold blue]Due Date Overview[/]");
        AnsiConsole.WriteLine();

        await AnsiConsole.Status()
            .Start("Checking due dates...", async ctx =>
            {
                var allTodos = await _todoService.GetAllTodosAsync();
                var todosList = allTodos.ToList();

                if (!todosList.Any())
                {
                    AnsiConsole.MarkupLine("[yellow]No tasks found. Create some tasks with due dates to see due date analysis.[/]");
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine("Press [blue]any key[/] to continue...");
                    Console.ReadKey();
                    return;
                }

                var today = DateTime.Today;
                var endOfWeek = today.AddDays(7 - (int)today.DayOfWeek);

                var tasksWithDueDates = todosList.Where(t => t.DueDate.HasValue).ToList();
                var overdueTasks = tasksWithDueDates.Where(t => t.DueDate!.Value.Date < today && 
                                                               t.Status != Timekeeper.Domain.Enums.TaskStatus.Completed).ToList();
                var dueTodayTasks = tasksWithDueDates.Where(t => t.DueDate!.Value.Date == today && 
                                                                t.Status != Timekeeper.Domain.Enums.TaskStatus.Completed).ToList();
                var dueThisWeekTasks = tasksWithDueDates.Where(t => t.DueDate!.Value.Date > today && 
                                                                   t.DueDate!.Value.Date <= endOfWeek &&
                                                                   t.Status != Timekeeper.Domain.Enums.TaskStatus.Completed).ToList();
                var onTrackTasks = tasksWithDueDates.Where(t => !overdueTasks.Contains(t) && 
                                                              !dueTodayTasks.Contains(t) && 
                                                              !dueThisWeekTasks.Contains(t)).ToList();

                AnsiConsole.MarkupLine($"[red bold]⚠️  Overdue Tasks: {overdueTasks.Count}[/]");
                AnsiConsole.MarkupLine($"[yellow bold]📅 Due Today: {dueTodayTasks.Count}[/]");
                AnsiConsole.MarkupLine($"[orange1 bold]⏰ Due This Week: {dueThisWeekTasks.Count}[/]");
                AnsiConsole.MarkupLine($"[green bold]✓ On Track: {onTrackTasks.Count}[/]");
                AnsiConsole.MarkupLine($"[grey bold]📋 No Due Date: {todosList.Count - tasksWithDueDates.Count}[/]");

                // Show detailed table of urgent tasks
                var urgentTasks = overdueTasks.Concat(dueTodayTasks).Concat(dueThisWeekTasks.Take(5)).ToList();
                
                if (urgentTasks.Any())
                {
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine("[bold yellow]Urgent Tasks:[/]");
                    
                    var table = new Table();
                    table.AddColumn("Task");
                    table.AddColumn("Due Date");
                    table.AddColumn("Status");
                    table.AddColumn("Priority");

                    foreach (var task in urgentTasks)
                    {
                        var dueStatus = task.DueDate!.Value.Date < today ? "[red]Overdue[/]" :
                                       task.DueDate!.Value.Date == today ? "[yellow]Due Today[/]" :
                                       "[orange1]Upcoming[/]";
                        
                        var priorityColor = task.Priority switch
                        {
                            Timekeeper.Domain.Enums.Priority.High => "red",
                            Timekeeper.Domain.Enums.Priority.Medium => "orange1",
                            Timekeeper.Domain.Enums.Priority.Low => "grey",
                            _ => "white"
                        };

                        table.AddRow(
                            (task.Title.Length > 30 ? task.Title[..27] + "..." : task.Title).EscapeMarkup(),
                            task.DueDate!.Value.ToString("yyyy-MM-dd"),
                            dueStatus,
                            $"[{priorityColor}]{task.Priority}[/]"
                        );
                    }

                    AnsiConsole.Write(table);
                }
            });

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("Press [blue]any key[/] to continue...");
        Console.ReadKey();
    }

    private async Task ShowStatusDistributionAsync()
    {
        AnsiConsole.MarkupLine("[bold blue]Status Distribution[/]");
        AnsiConsole.WriteLine();

        await AnsiConsole.Status()
            .Start("Analyzing task distribution...", async ctx =>
            {
                var allTodos = await _todoService.GetAllTodosAsync();
                var todosList = allTodos.ToList();

                if (!todosList.Any())
                {
                    AnsiConsole.MarkupLine("[yellow]No tasks found. Create some tasks to see status distribution.[/]");
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine("Press [blue]any key[/] to continue...");
                    Console.ReadKey();
                    return;
                }

                // Calculate status distribution
                var statusGroups = todosList
                    .GroupBy(t => t.Status)
                    .ToDictionary(g => g.Key, g => g.Count());

                var chart = new BreakdownChart()
                    .Width(60)
                    .ShowPercentage();

                foreach (var status in Enum.GetValues<Timekeeper.Domain.Enums.TaskStatus>())
                {
                    if (statusGroups.TryGetValue(status, out var count) && count > 0)
                    {
                        var color = GetStatusColor(status);
                        chart.AddItem(status.ToString(), count, color);
                    }
                }

                AnsiConsole.Write(chart);

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[bold]Summary:[/]");
                
                var totalTasks = todosList.Count;
                var activeTasks = todosList.Count(t => t.Status != Timekeeper.Domain.Enums.TaskStatus.Completed && 
                                                      t.Status != Timekeeper.Domain.Enums.TaskStatus.Cancelled);
                var completedTasks = statusGroups.GetValueOrDefault(Timekeeper.Domain.Enums.TaskStatus.Completed, 0);
                var completionRate = totalTasks > 0 ? (completedTasks * 100.0) / totalTasks : 0;
                var onHoldTasks = statusGroups.GetValueOrDefault(Timekeeper.Domain.Enums.TaskStatus.OnHold, 0);

                AnsiConsole.MarkupLine($"• Total tasks: [blue]{totalTasks}[/]");
                AnsiConsole.MarkupLine($"• Active tasks: [yellow]{activeTasks}[/]");
                
                var completionColor = completionRate >= 70 ? "green" : completionRate >= 50 ? "yellow" : "red";
                AnsiConsole.MarkupLine($"• Completion rate: [{completionColor}]{completionRate:F1}%[/]");
                
                if (onHoldTasks > 0)
                    AnsiConsole.MarkupLine($"• Tasks need attention: [orange1]{onHoldTasks}[/]");

                // Show detailed breakdown table
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[bold yellow]Detailed Breakdown:[/]");
                
                var table = new Table();
                table.AddColumn("Status");
                table.AddColumn("Count");
                table.AddColumn("Percentage");

                foreach (var status in Enum.GetValues<Timekeeper.Domain.Enums.TaskStatus>())
                {
                    if (statusGroups.TryGetValue(status, out var count) && count > 0)
                    {
                        var percentage = (count * 100.0) / totalTasks;
                        var color = GetStatusColor(status);
                        
                        var statusColor = GetColorName(color);

                        table.AddRow(
                            $"[{statusColor}]{status}[/]",
                            count.ToString(),
                            $"{percentage:F1}%"
                        );
                    }
                }

                AnsiConsole.Write(table);
            });

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("Press [blue]any key[/] to continue...");
        Console.ReadKey();
    }

    private static Color GetStatusColor(Timekeeper.Domain.Enums.TaskStatus status)
    {
        return status switch
        {
            Timekeeper.Domain.Enums.TaskStatus.Pending => Color.Grey,
            Timekeeper.Domain.Enums.TaskStatus.InProgress => Color.Yellow,
            Timekeeper.Domain.Enums.TaskStatus.Completed => Color.Green,
            Timekeeper.Domain.Enums.TaskStatus.OnHold => Color.Orange1,
            Timekeeper.Domain.Enums.TaskStatus.Cancelled => Color.Red,
            _ => Color.White
        };
    }

    private static string GetColorName(Color color)
    {
        if (color == Color.Green) return "green";
        if (color == Color.Yellow) return "yellow";
        if (color == Color.Red) return "red";
        if (color == Color.Orange1) return "orange1";
        return "grey";
    }

    private async Task<string> ShowInteractiveMenuAsync(string title, List<(string key, string icon, string value, string description)> items, string subtitle = "")
    {
        var searchTerm = "";
        var selectedIndex = 0;
        var filteredItems = items;
        
        while (true)
        {
            Console.Clear();
            ShowWelcomeAnimated();
            
            // Show animated header
            ShowAnimatedHeader(title, subtitle);
            
            // Filter items based on search
            filteredItems = string.IsNullOrEmpty(searchTerm) 
                ? items 
                : items.Where(item => 
                    item.value.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    item.description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    item.key == searchTerm).ToList();
            
            if (filteredItems.Count == 0)
            {
                filteredItems = items;
                searchTerm = "";
            }
            
            // Ensure selected index is valid
            selectedIndex = Math.Max(0, Math.Min(selectedIndex, filteredItems.Count - 1));
            
            // Show search bar if there's a search term
            if (!string.IsNullOrEmpty(searchTerm))
            {
                var searchPanel = new Panel($"🔍 Search: [yellow]{searchTerm}[/]")
                {
                    Border = BoxBorder.Rounded,
                    BorderStyle = new Style(Color.Yellow)
                };
                AnsiConsole.Write(searchPanel);
                AnsiConsole.WriteLine();
            }
            
            // Create and display menu
            var table = CreateStyledMenuTable(filteredItems, selectedIndex);
            AnsiConsole.Write(table);
            
            // Show navigation help
            ShowNavigationHelp();
            
            // Handle input
            var key = Console.ReadKey(true);
            
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    selectedIndex = selectedIndex > 0 ? selectedIndex - 1 : filteredItems.Count - 1;
                    break;
                    
                case ConsoleKey.DownArrow:
                    selectedIndex = selectedIndex < filteredItems.Count - 1 ? selectedIndex + 1 : 0;
                    break;
                    
                case ConsoleKey.LeftArrow:
                    return "back";
                    
                case ConsoleKey.RightArrow:
                case ConsoleKey.Enter:
                    if (filteredItems.Count > 0)
                        return filteredItems[selectedIndex].value;
                    break;
                    
                case ConsoleKey.Escape:
                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        searchTerm = "";
                        selectedIndex = 0;
                    }
                    else
                    {
                        return "back";
                    }
                    break;
                    
                case ConsoleKey.Backspace:
                    if (searchTerm.Length > 0)
                    {
                        searchTerm = searchTerm[..^1];
                        selectedIndex = 0;
                    }
                    break;
                    
                default:
                    if (char.IsDigit(key.KeyChar))
                    {
                        var shortcutItem = filteredItems.FirstOrDefault(item => item.key == key.KeyChar.ToString());
                        if (shortcutItem != default)
                        {
                            return shortcutItem.value;
                        }
                    }
                    else if (char.IsLetter(key.KeyChar) || char.IsWhiteSpace(key.KeyChar))
                    {
                        searchTerm += key.KeyChar;
                        selectedIndex = 0;
                    }
                    break;
            }
        }
    }

    private static void ShowAnimatedHeader(string title, string subtitle)
    {
        // Animated title with gradient effect
        var rule = new Rule($"[bold blue]{title}[/]")
        {
            Style = Style.Parse("blue"),
            Justification = Justify.Center
        };
        AnsiConsole.Write(rule);
        
        if (!string.IsNullOrEmpty(subtitle))
        {
            AnsiConsole.MarkupLine($"[dim italic]{subtitle.EscapeMarkup()}[/]");
        }
        
        AnsiConsole.WriteLine();
        
        // Small delay to ensure header is visible
    }

    private static Table CreateStyledMenuTable(List<(string key, string icon, string value, string description)> items, int selectedIndex)
    {
        var table = new Table()
        {
            Border = TableBorder.Rounded,
            ShowHeaders = false
        };
        
        table.AddColumn("Key");
        table.AddColumn("Option");
        table.AddColumn("Description");
        
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var isSelected = i == selectedIndex;
            
            var keyText = isSelected ? $"> {item.key}" : $"  {item.key}";
            var optionText = $"{item.icon} {GetMenuTitle(item.value)}";
            var descText = item.description;
            
            table.AddRow(keyText, optionText, descText);
        }
        
        return table;
    }

    private static string GetMenuTitle(string value)
    {
        return value switch
        {
            "summary" => "Task Summary Report",
            "timetracking" => "Time Tracking Report",
            "productivity" => "Productivity Trends",
            "categories" => "Category Analysis",
            "duedates" => "Due Date Overview",
            "status" => "Status Distribution",
            "back" => "Back to Main Menu",
            _ => value
        };
    }

    private static void ShowNavigationHelp()
    {
        AnsiConsole.WriteLine();
        
        var helpPanel = new Panel(
            "[dim]Navigation:[/] [blue]↑↓[/] Select  [blue]←[/] Back  [blue]→ Enter[/] Confirm  [blue]0-9[/] Quick Select  [blue]Type[/] Search  [blue]Esc[/] Clear/Exit"
        )
        {
            Border = BoxBorder.None,
            BorderStyle = new Style(Color.Grey),
            Padding = new Padding(1, 0)
        };
        
        AnsiConsole.Write(helpPanel);
    }

    private static void ShowWelcomeAnimated()
    {
        Console.Clear();
        
        // ASCII art header
        var figlet = new FigletText("Reports")
            .LeftJustified()
            .Color(Color.Blue3);

        AnsiConsole.Write(figlet);
        
        // Welcome panel with animation
        var welcomePanel = new Panel(
            new Markup("[bold blue3]Welcome to Reports & Analytics![/]\n\n" +
                      "Analyze your productivity and task patterns.\n" +
                      "Gain insights from your time tracking data.\n\n" +
                      "[dim]Created by [link=https://github.com/andrrff]andrrff[/] | [link=https://github.com/andrrff]GitHub[/][/]"))
        {
            Border = BoxBorder.Double,
            BorderStyle = new Style(Color.Blue3),
            Header = new PanelHeader(" 📊 Reports & Analytics "),
            Padding = new Padding(2, 1)
        };

        AnsiConsole.Write(welcomePanel);
        
        // Loading animation
        AnsiConsole.Status()
            .Start("Loading Reports...", ctx =>
            {
                var frames = new[] { "📊", "📈", "📉", "📋", "📉", "📈", "📊" };
                for (int i = 0; i < 7; i++)
                {
                    ctx.Status($"{frames[i]} Loading Reports...");
                }
            });
            
    }
}
