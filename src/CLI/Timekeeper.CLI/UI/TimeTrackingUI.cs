using Spectre.Console;
using System.Linq;
using MediatR;
using Timekeeper.CLI.Services;
using Timekeeper.Domain.Entities;
using Timekeeper.Domain.Enums;
using Timekeeper.Application.TimeEntries.Queries;

namespace Timekeeper.CLI.UI;

public class TimeTrackingUI
{
    private readonly ITimeTrackingService _timeTrackingService;
    private readonly ITodoService _todoService;
    private readonly IMediator _mediator;

    public TimeTrackingUI(ITimeTrackingService timeTrackingService, ITodoService todoService, IMediator mediator)
    {
        _timeTrackingService = timeTrackingService;
        _todoService = todoService;
        _mediator = mediator;
    }

    public async Task ShowAsync()
    {        
        while (true)
        {
            var menuItems = new List<(string key, string icon, string value, string description)>
            {
                ("1", "▶️", "start", "Start timing a new task"),
                ("2", "⏹️", "stop", "Stop currently running timer"),
                ("3", "⏱️", "active", "View all active timers"),
                ("4", "📊", "summary", "View time tracking summary"),
                ("5", "➕", "manual", "Add manual time entry"),
                ("6", "📝", "logs", "View detailed time logs"),
                ("0", "⬅️", "back", "Return to main menu")
            };

            var choice = await ShowInteractiveMenuAsync("⏱️ Time Tracking", menuItems,
                "Track and manage your time spent on tasks");

            switch (choice)
            {
                case "start":
                    await StartTimerAsync();
                    break;
                case "stop":
                    await StopTimerAsync();
                    break;
                case "active":
                    await ViewActiveTimersAsync();
                    break;
                case "summary":
                    await ShowTimeSummaryAsync();
                    break;
                case "manual":
                    await AddManualTimeAsync();
                    break;
                case "logs":
                    await ViewTimeLogsAsync();
                    break;
                case "back":
                    return;
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("Press [blue]any key[/] to continue...");
            Console.ReadKey();
        }
    }

    private async Task StartTimerAsync()
    {
        AnsiConsole.MarkupLine("[bold blue]Start Timer[/]");
        AnsiConsole.WriteLine();

        // Buscar todos os todos não completados
        var pendingTodos = await _todoService.GetTodosByStatusAsync(Timekeeper.Domain.Enums.TaskStatus.Pending);
        var inProgressTodos = await _todoService.GetTodosByStatusAsync(Timekeeper.Domain.Enums.TaskStatus.InProgress);
        var todos = pendingTodos.Concat(inProgressTodos).ToList();
        
        if (!todos.Any())
        {
            AnsiConsole.MarkupLine("[red]❌ No pending or in-progress tasks found.[/]");
            return;
        }

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<TodoItem>()
                .Title("Select task to start timer:")
                .AddChoices(todos)
                .UseConverter(todo => $"{todo.Title.EscapeMarkup()} ({todo.Status})"));

        // Verificar se já existe timer ativo
        var isActive = await _timeTrackingService.IsTimerActiveAsync(selected.Id);
        if (isActive)
        {
            AnsiConsole.MarkupLine($"[yellow]⚠️ Timer is already active for this task![/]");
            
            var elapsed = await _timeTrackingService.GetElapsedTimeAsync(selected.Id);
            AnsiConsole.MarkupLine($"[grey]Current elapsed time: {FormatTimeSpan(elapsed)}[/]");
            return;
        }

        var description = AnsiConsole.Ask<string>("Timer description (optional):", "");

        // Get the index of the selected todo
        var allTodos = await _todoService.GetAllTodosAsync();
        var todoList = allTodos.ToList();
        var selectedIndex = todoList.FindIndex(t => t.Id == selected.Id) + 1;

        await _timeTrackingService.StartTimerAsync(selectedIndex, description);
        AnsiConsole.MarkupLine($"[green]✅ Timer started for: {selected.Title.EscapeMarkup()}[/]");
    }

    private async Task StopTimerAsync()
    {
        AnsiConsole.MarkupLine("[bold red]Stop Timer[/]");
        AnsiConsole.WriteLine();

        var activeTimers = await _timeTrackingService.GetAllActiveTimersAsync();
        if (!activeTimers.Any())
        {
            AnsiConsole.MarkupLine("[yellow]⚠️ No active timers found.[/]");
            return;
        }

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<TimeEntry>()
                .Title("Select timer to stop:")
                .AddChoices(activeTimers)
                .UseConverter(timer => 
                {
                    var elapsed = DateTime.UtcNow - timer.StartTime;
                    return $"{timer.TodoItem?.Title?.EscapeMarkup()} (Running: {FormatTimeSpan(elapsed)})";
                }));

        // Find the index of the selected timer in the active timers list
        var timers = (await _timeTrackingService.GetAllActiveTimersAsync()).ToList();
        var selectedTimerIndex = timers.FindIndex(t => t.TodoItemId == selected.TodoItemId);
        
        await _timeTrackingService.StopTimerAsync(selectedTimerIndex >= 0 ? selectedTimerIndex + 1 : null);
        AnsiConsole.MarkupLine($"[green]✅ Timer stopped![/]");
    }

    private async Task ViewActiveTimersAsync()
    {
        AnsiConsole.MarkupLine("[bold cyan]Active Timers[/]");
        AnsiConsole.WriteLine();

        var activeTimers = await _timeTrackingService.GetAllActiveTimersAsync();
        if (!activeTimers.Any())
        {
            AnsiConsole.MarkupLine("[yellow]⚠️ No active timers found.[/]");
            return;
        }

        var table = new Table();
        table.AddColumn("Task");
        table.AddColumn("Started At");
        table.AddColumn("Elapsed Time");
        table.AddColumn("Description");

        foreach (var timer in activeTimers)
        {
            var elapsed = DateTime.UtcNow - timer.StartTime;
            table.AddRow(
                timer.TodoItem?.Title?.EscapeMarkup() ?? "Unknown",
                timer.StartTime.ToString("HH:mm:ss"),
                FormatTimeSpan(elapsed),
                timer.Description?.EscapeMarkup() ?? "-"
            );
        }

        AnsiConsole.Write(table);
    }

    private async Task ShowTimeSummaryAsync()
    {
        AnsiConsole.MarkupLine("[bold cyan]Time Summary[/]");
        AnsiConsole.WriteLine();

        // Buscar todos os todos
        var allTodos = await _todoService.GetAllTodosAsync();
        if (!allTodos.Any())
        {
            AnsiConsole.MarkupLine("[yellow]⚠️ No tasks found.[/]");
            return;
        }

        var table = new Table();
        table.AddColumn("Task");
        table.AddColumn("Estimated");
        table.AddColumn("Time Spent");
        table.AddColumn("Remaining");
        table.AddColumn("Progress");
        table.AddColumn("Status");

        foreach (var todo in allTodos.Take(10)) // Limitar para 10 tarefas
        {
            var timeSpent = await _timeTrackingService.GetTotalTimeSpentAsync(todo.Id);
            var remaining = await _timeTrackingService.GetRemainingTimeAsync(todo.Id);
            var estimated = TimeSpan.FromMinutes(todo.EstimatedTimeMinutes);
            var isActive = await _timeTrackingService.IsTimerActiveAsync(todo.Id);

            var progress = estimated.TotalMinutes > 0 
                ? (timeSpent.TotalMinutes / estimated.TotalMinutes) * 100 
                : 0;

            var progressColor = progress switch
            {
                < 50 => "green",
                < 80 => "yellow",
                < 100 => "orange1",
                _ => "red"
            };

            var statusText = isActive ? "[green]▶️ Active[/]" : todo.Status.ToString();

            table.AddRow(
                todo.Title.Length > 25 ? todo.Title[..22].EscapeMarkup() + "..." : todo.Title.EscapeMarkup(),
                FormatTimeSpan(estimated),
                FormatTimeSpan(timeSpent),
                FormatTimeSpan(remaining),
                $"[{progressColor}]{progress:F1}%[/]",
                statusText
            );
        }

        AnsiConsole.Write(table);
    }

    private async Task AddManualTimeAsync()
    {
        AnsiConsole.MarkupLine("[bold blue]Add Manual Time[/]");
        AnsiConsole.WriteLine();

        var todos = await _todoService.GetAllTodosAsync();
        if (!todos.Any())
        {
            AnsiConsole.MarkupLine("[red]❌ No tasks found.[/]");
            return;
        }

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<TodoItem>()
                .Title("Select task to add time:")
                .AddChoices(todos)
                .UseConverter(todo => $"{todo.Title.EscapeMarkup()} ({todo.Status})"));

        var minutes = AnsiConsole.Ask<int>("Minutes to add:", 0);
        if (minutes <= 0)
        {
            AnsiConsole.MarkupLine("[red]❌ Invalid time amount.[/]");
            return;
        }

        var description = AnsiConsole.Ask<string>("Description:", "Manual time entry");

        // Create a time entry that ends now and started 'minutes' ago
        var endTime = DateTime.Now;
        var startTime = endTime.AddMinutes(-minutes);

        await _timeTrackingService.UpdateManualTimeAsync(selected.Id, startTime, endTime);
        AnsiConsole.MarkupLine($"[green]✅ Added {minutes} minutes to: {selected.Title.EscapeMarkup()}[/]");
        
        var totalTime = await _timeTrackingService.GetTotalTimeSpentAsync(selected.Id);
        AnsiConsole.MarkupLine($"[grey]Total time spent: {FormatTimeSpan(totalTime)}[/]");
    }

    private async Task ViewTimeLogsAsync()
    {
        AnsiConsole.MarkupLine("[bold cyan]Time Logs[/]");
        AnsiConsole.WriteLine();

        var viewOption = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select view option:")
                .AddChoices(
                    "📋 Logs by Task",
                    "📅 All Logs (Last 30 days)",
                    "🗓️ Logs by Date Range",
                    "📊 Today's Summary",
                    "📈 Weekly Summary"));

        switch (viewOption)
        {
            case "📋 Logs by Task":
                await ViewLogsByTaskAsync();
                break;
            case "📅 All Logs (Last 30 days)":
                await ViewRecentLogsAsync();
                break;
            case "🗓️ Logs by Date Range":
                await ViewLogsByDateRangeAsync();
                break;
            case "📊 Today's Summary":
                await ViewTodaySummaryAsync();
                break;
            case "📈 Weekly Summary":
                await ViewWeeklySummaryAsync();
                break;
        }
    }

    private async Task ViewLogsByTaskAsync()
    {
        var todos = await _todoService.GetAllTodosAsync();
        if (!todos.Any())
        {
            AnsiConsole.MarkupLine("[red]❌ No tasks found.[/]");
            return;
        }

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<TodoItem>()
                .Title("Select task to view logs:")
                .AddChoices(todos)
                .UseConverter(todo => $"{todo.Title.EscapeMarkup()} ({todo.Status})"));

        var query = new GetTimeEntriesByTodoItemQuery(selected.Id);
        var timeEntries = await _mediator.Send(query);

        if (!timeEntries.Any())
        {
            AnsiConsole.MarkupLine($"[yellow]⚠️ No time logs found for: {selected.Title.EscapeMarkup()}[/]");
            return;
        }

        AnsiConsole.MarkupLine($"[bold]Time logs for: {selected.Title.EscapeMarkup()}[/]");
        AnsiConsole.WriteLine();

        var table = new Table();
        table.AddColumn("Date");
        table.AddColumn("Start Time");
        table.AddColumn("End Time");
        table.AddColumn("Duration");
        table.AddColumn("Description");
        table.AddColumn("Status");

        var totalMinutes = 0;
        foreach (var entry in timeEntries.OrderByDescending(e => e.StartTime))
        {
            var startTime = entry.StartTime.ToString("HH:mm:ss");
            var endTime = entry.EndTime?.ToString("HH:mm:ss") ?? "Active";
            var duration = entry.EndTime.HasValue 
                ? FormatTimeSpan(TimeSpan.FromMinutes(entry.DurationMinutes))
                : FormatTimeSpan(DateTime.UtcNow - entry.StartTime);
            var status = entry.IsActive ? "[green]Active[/]" : "[grey]Completed[/]";
            
            totalMinutes += entry.DurationMinutes;

            table.AddRow(
                entry.StartTime.ToString("dd/MM/yyyy"),
                startTime,
                endTime,
                duration,
                entry.Description?.EscapeMarkup() ?? "-",
                status
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold]Total time spent: {FormatTimeSpan(TimeSpan.FromMinutes(totalMinutes))}[/]");
        
        if (selected.EstimatedHours.HasValue)
        {
            var estimatedMinutes = selected.EstimatedHours.Value * 60;
            var remainingMinutes = estimatedMinutes - totalMinutes;
            var percentage = (totalMinutes * 100.0) / estimatedMinutes;
            
            AnsiConsole.MarkupLine($"[bold]Estimated time: {selected.EstimatedHours}h[/]");
            AnsiConsole.MarkupLine($"[bold]Progress: {percentage:F1}%[/]");
            
            if (remainingMinutes > 0)
            {
                AnsiConsole.MarkupLine($"[yellow]Remaining: {FormatTimeSpan(TimeSpan.FromMinutes(remainingMinutes))}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Over budget by: {FormatTimeSpan(TimeSpan.FromMinutes(-remainingMinutes))}[/]");
            }
        }
    }

    private async Task ViewRecentLogsAsync()
    {
        var endDate = DateTime.Today.AddDays(1);
        var startDate = endDate.AddDays(-30);
        
        var query = new GetTimeEntriesInDateRangeQuery(startDate, endDate);
        var timeEntries = await _mediator.Send(query);

        if (!timeEntries.Any())
        {
            AnsiConsole.MarkupLine("[yellow]⚠️ No time logs found in the last 30 days.[/]");
            return;
        }

        AnsiConsole.MarkupLine("[bold]All Time Logs (Last 30 days)[/]");
        AnsiConsole.WriteLine();

        await DisplayTimeEntriesTable(timeEntries);
    }

    private async Task ViewLogsByDateRangeAsync()
    {
        var startDateInput = AnsiConsole.Ask<string>("Enter start date (yyyy-MM-dd):");
        var endDateInput = AnsiConsole.Ask<string>("Enter end date (yyyy-MM-dd):");

        if (!DateTime.TryParseExact(startDateInput, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var startDate) ||
            !DateTime.TryParseExact(endDateInput, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var endDate))
        {
            AnsiConsole.MarkupLine("[red]❌ Invalid date format. Please use yyyy-MM-dd.[/]");
            return;
        }

        if (startDate > endDate)
        {
            AnsiConsole.MarkupLine("[red]❌ Start date cannot be after end date.[/]");
            return;
        }

        var query = new GetTimeEntriesInDateRangeQuery(startDate, endDate.AddDays(1));
        var timeEntries = await _mediator.Send(query);

        if (!timeEntries.Any())
        {
            AnsiConsole.MarkupLine($"[yellow]⚠️ No time logs found between {startDate:dd/MM/yyyy} and {endDate:dd/MM/yyyy}.[/]");
            return;
        }

        AnsiConsole.MarkupLine($"[bold]Time Logs: {startDate:dd/MM/yyyy} to {endDate:dd/MM/yyyy}[/]");
        AnsiConsole.WriteLine();

        await DisplayTimeEntriesTable(timeEntries);
    }

    private async Task ViewTodaySummaryAsync()
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        
        var query = new GetTimeEntriesInDateRangeQuery(today, tomorrow);
        var timeEntries = await _mediator.Send(query);

        AnsiConsole.MarkupLine($"[bold]Today's Summary - {today:dd/MM/yyyy}[/]");
        AnsiConsole.WriteLine();

        if (!timeEntries.Any())
        {
            AnsiConsole.MarkupLine("[yellow]⚠️ No time logs found for today.[/]");
            return;
        }

        // Agrupar por tarefa
        var groupedByTask = timeEntries
            .Where(e => e.TodoItem != null)
            .GroupBy(e => e.TodoItem)
            .ToList();

        var table = new Table();
        table.AddColumn("Task");
        table.AddColumn("Time Spent");
        table.AddColumn("Entries");
        table.AddColumn("Status");

        var totalMinutes = 0;
        foreach (var group in groupedByTask.OrderByDescending(g => g.Sum(e => e.DurationMinutes)))
        {
            var task = group.Key!;
            var taskMinutes = group.Sum(e => e.DurationMinutes);
            var entryCount = group.Count();
            var hasActive = group.Any(e => e.IsActive);
            
            totalMinutes += taskMinutes;

            var status = hasActive ? "[green]Active[/]" : "[grey]Completed[/]";
            
            table.AddRow(
                task.Title.EscapeMarkup(),
                FormatTimeSpan(TimeSpan.FromMinutes(taskMinutes)),
                entryCount.ToString(),
                status
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold]Total time today: {FormatTimeSpan(TimeSpan.FromMinutes(totalMinutes))}[/]");
        AnsiConsole.MarkupLine($"[grey]Total entries: {timeEntries.Count}[/]");
    }

    private async Task ViewWeeklySummaryAsync()
    {
        var today = DateTime.Today;
        var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
        var endOfWeek = startOfWeek.AddDays(7);
        
        var query = new GetTimeEntriesInDateRangeQuery(startOfWeek, endOfWeek);
        var timeEntries = await _mediator.Send(query);

        AnsiConsole.MarkupLine($"[bold]Weekly Summary - {startOfWeek:dd/MM} to {endOfWeek.AddDays(-1):dd/MM/yyyy}[/]");
        AnsiConsole.WriteLine();

        if (!timeEntries.Any())
        {
            AnsiConsole.MarkupLine("[yellow]⚠️ No time logs found for this week.[/]");
            return;
        }

        // Agrupar por dia
        var groupedByDay = timeEntries
            .GroupBy(e => e.StartTime.Date)
            .OrderBy(g => g.Key)
            .ToList();

        var table = new Table();
        table.AddColumn("Day");
        table.AddColumn("Time Spent");
        table.AddColumn("Entries");
        table.AddColumn("Main Tasks");

        var totalMinutes = 0;
        foreach (var group in groupedByDay)
        {
            var day = group.Key;
            var dayMinutes = group.Sum(e => e.DurationMinutes);
            var entryCount = group.Count();
            var mainTasks = group
                .Where(e => e.TodoItem != null)
                .GroupBy(e => e.TodoItem!.Title)
                .OrderByDescending(g => g.Sum(e => e.DurationMinutes))
                .Take(2)
                .Select(g => g.Key)
                .ToList();
            
            totalMinutes += dayMinutes;

            var dayName = day.ToString("dddd (dd/MM)");
            var mainTasksText = mainTasks.Any() ? string.Join(", ", mainTasks.Select(t => t.EscapeMarkup())) : "-";
            
            table.AddRow(
                dayName,
                FormatTimeSpan(TimeSpan.FromMinutes(dayMinutes)),
                entryCount.ToString(),
                mainTasksText
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold]Total time this week: {FormatTimeSpan(TimeSpan.FromMinutes(totalMinutes))}[/]");
        AnsiConsole.MarkupLine($"[grey]Daily average: {FormatTimeSpan(TimeSpan.FromMinutes(totalMinutes / 7))}[/]");
    }

    private async Task DisplayTimeEntriesTable(List<TimeEntry> timeEntries)
    {
        var table = new Table();
        table.AddColumn("Date");
        table.AddColumn("Task");
        table.AddColumn("Start");
        table.AddColumn("End");
        table.AddColumn("Duration");
        table.AddColumn("Description");

        var totalMinutes = 0;
        foreach (var entry in timeEntries.OrderByDescending(e => e.StartTime))
        {
            var startTime = entry.StartTime.ToString("HH:mm");
            var endTime = entry.EndTime?.ToString("HH:mm") ?? "Active";
            var duration = entry.EndTime.HasValue 
                ? FormatTimeSpan(TimeSpan.FromMinutes(entry.DurationMinutes))
                : FormatTimeSpan(DateTime.UtcNow - entry.StartTime);
            
            totalMinutes += entry.DurationMinutes;

            table.AddRow(
                entry.StartTime.ToString("dd/MM/yyyy"),
                entry.TodoItem?.Title?.EscapeMarkup() ?? "Unknown",
                startTime,
                endTime,
                duration,
                entry.Description?.EscapeMarkup() ?? "-"
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold]Total time: {FormatTimeSpan(TimeSpan.FromMinutes(totalMinutes))}[/]");
        AnsiConsole.MarkupLine($"[grey]Total entries: {timeEntries.Count}[/]");
    }

    private static string FormatTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalDays >= 1)
            return $"{(int)timeSpan.TotalDays}d {timeSpan.Hours}h {timeSpan.Minutes}m";
        if (timeSpan.TotalHours >= 1)
            return $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes}m";
        return $"{timeSpan.Minutes}m";
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
            "start" => "Start Timer",
            "stop" => "Stop Timer",
            "active" => "View Active Timers",
            "summary" => "Time Summary",
            "manual" => "Add Manual Time",
            "logs" => "View Time Logs",
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
        var figlet = new FigletText("Time Tracking")
            .LeftJustified()
            .Color(Color.Yellow);

        AnsiConsole.Write(figlet);
        
        // Welcome panel with animation
        var welcomePanel = new Panel(
            new Markup("[bold yellow]Welcome to Time Tracking![/]\n\n" +
                      "Track time spent on your tasks efficiently.\n" +
                      "Monitor productivity and generate reports.\n\n" +
                      "[dim]Created by [link=https://github.com/andrrff]andrrff[/] | [link=https://github.com/andrrff]GitHub[/][/]"))
        {
            Border = BoxBorder.Double,
            BorderStyle = new Style(Color.Yellow),
            Header = new PanelHeader(" ⏱️ Time Tracking "),
            Padding = new Padding(2, 1)
        };

        AnsiConsole.Write(welcomePanel);
        
        // Loading animation
        AnsiConsole.Status()
            .Start("Loading Time Tracking...", ctx =>
            {
                var frames = new[] { "⏱️", "⏰", "⏲️", "⏳", "⏲️", "⏰", "⏱️" };
                for (int i = 0; i < 7; i++)
                {
                    ctx.Status($"{frames[i]} Loading Time Tracking...");
                }
            });
            
    }
}
