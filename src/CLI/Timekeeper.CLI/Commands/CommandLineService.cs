using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Timekeeper.CLI.Services;
using Timekeeper.CLI.UI;
using Timekeeper.Domain.Enums;
using Timekeeper.Domain.Interfaces;
using Timekeeper.Application.Reports.Queries.GetCalendarReport;

namespace Timekeeper.CLI.Commands;

public class CommandLineService
{
    private readonly IServiceProvider _serviceProvider;

    public CommandLineService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public RootCommand CreateRootCommand()
    {
        var rootCommand = new RootCommand("Timekeeper - Advanced Time Management System");
        
        // Add all command groups
        rootCommand.AddCommand(CreateTodoCommands());
        rootCommand.AddCommand(CreateTimeCommands());
        rootCommand.AddCommand(CreateReportCommands());
        rootCommand.AddCommand(CreateProviderCommands());
        rootCommand.AddCommand(CreateConfigCommands());

        // Add interactive mode command
        var interactiveCommand = new Command("interactive", "Start interactive mode (default behavior)");
        interactiveCommand.SetHandler(async () =>
        {
            var mainMenu = _serviceProvider.GetRequiredService<MainMenuUI>();
            await mainMenu.ShowAsync();
        });
        rootCommand.AddCommand(interactiveCommand);

        // Add about command
        var aboutCommand = new Command("about", "Show information about Timekeeper and its creator");
        aboutCommand.SetHandler(() =>
        {
            ShowAboutInfo();
        });
        rootCommand.AddCommand(aboutCommand);

        return rootCommand;
    }

    private Command CreateTodoCommands()
    {
        var todoCommand = new Command("todo", "Manage todo items");

        // todo add
        var addCommand = new Command("add", "Add a new todo item");
        var titleOption = new Option<string>("--title", "Title of the todo item") { IsRequired = true };
        var descriptionOption = new Option<string?>("--description", "Description of the todo item");
        var priorityOption = new Option<Priority>("--priority", () => Priority.Medium, "Priority level");
        var categoryOption = new Option<string?>("--category", "Category of the todo item");
        var tagsOption = new Option<string?>("--tags", "Tags (comma-separated)");
        var dueDateOption = new Option<DateTime?>("--due-date", "Due date (yyyy-MM-dd)");

        addCommand.AddOption(titleOption);
        addCommand.AddOption(descriptionOption);
        addCommand.AddOption(priorityOption);
        addCommand.AddOption(categoryOption);
        addCommand.AddOption(tagsOption);
        addCommand.AddOption(dueDateOption);

        addCommand.SetHandler(async (string title, string? description, Priority priority, string? category, string? tags, DateTime? dueDate) =>
        {
            var todoService = _serviceProvider.GetRequiredService<ITodoService>();
            try
            {
                await todoService.CreateTodoAsync(title, description, priority, category, tags, dueDate);
                AnsiConsole.MarkupLine($"[green]✅ Todo '{title}' created successfully![/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Error creating todo: {ex.Message}[/]");
            }
        }, titleOption, descriptionOption, priorityOption, categoryOption, tagsOption, dueDateOption);

        // todo list
        var listCommand = new Command("list", "List all todo items");
        var statusFilterOption = new Option<Timekeeper.Domain.Enums.TaskStatus?>("--status", "Filter by status");
        var categoryFilterOption = new Option<string?>("--category", "Filter by category");
        var limitOption = new Option<int>("--limit", () => 10, "Maximum number of items to show");

        listCommand.AddOption(statusFilterOption);
        listCommand.AddOption(categoryFilterOption);
        listCommand.AddOption(limitOption);

        listCommand.SetHandler(async (Timekeeper.Domain.Enums.TaskStatus? status, string? category, int limit) =>
        {
            var todoService = _serviceProvider.GetRequiredService<ITodoService>();
            try
            {
                await todoService.ListTodosAsync(status, category, limit);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Error listing todos: {ex.Message}[/]");
            }
        }, statusFilterOption, categoryFilterOption, limitOption);

        // todo complete
        var completeCommand = new Command("complete", "Mark a todo as completed");
        var idOption = new Option<int>("--id", "ID of the todo item") { IsRequired = true };

        completeCommand.AddOption(idOption);
        completeCommand.SetHandler(async (int id) =>
        {
            var todoService = _serviceProvider.GetRequiredService<ITodoService>();
            try
            {
                await todoService.CompleteTodoAsync(id);
                AnsiConsole.MarkupLine($"[green]✅ Todo {id} completed![/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Error completing todo: {ex.Message}[/]");
            }
        }, idOption);

        // todo delete
        var deleteCommand = new Command("delete", "Delete a todo item");
        deleteCommand.AddOption(idOption);
        deleteCommand.SetHandler(async (int id) =>
        {
            var todoService = _serviceProvider.GetRequiredService<ITodoService>();
            try
            {
                await todoService.DeleteTodoAsync(id);
                AnsiConsole.MarkupLine($"[green]✅ Todo {id} deleted![/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Error deleting todo: {ex.Message}[/]");
            }
        }, idOption);

        // todo update
        var updateCommand = new Command("update", "Update a todo item");
        var updateIdOption = new Option<int>("--id", "ID of the todo item") { IsRequired = true };
        var updateTitleOption = new Option<string?>("--title", "New title");
        var updateDescriptionOption = new Option<string?>("--description", "New description");
        var updatePriorityOption = new Option<Priority?>("--priority", "New priority");
        var updateCategoryOption = new Option<string?>("--category", "New category");
        var updateTagsOption = new Option<string?>("--tags", "New tags (comma-separated)");

        updateCommand.AddOption(updateIdOption);
        updateCommand.AddOption(updateTitleOption);
        updateCommand.AddOption(updateDescriptionOption);
        updateCommand.AddOption(updatePriorityOption);
        updateCommand.AddOption(updateCategoryOption);
        updateCommand.AddOption(updateTagsOption);

        updateCommand.SetHandler(async (int id, string? title, string? description, Priority? priority, string? category, string? tags) =>
        {
            var todoService = _serviceProvider.GetRequiredService<ITodoService>();
            try
            {
                await todoService.UpdateTodoAsync(id, title, description, priority, category, tags);
                AnsiConsole.MarkupLine($"[green]✅ Todo {id} updated![/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Error updating todo: {ex.Message}[/]");
            }
        }, updateIdOption, updateTitleOption, updateDescriptionOption, updatePriorityOption, updateCategoryOption, updateTagsOption);

        todoCommand.AddCommand(addCommand);
        todoCommand.AddCommand(listCommand);
        todoCommand.AddCommand(completeCommand);
        todoCommand.AddCommand(deleteCommand);
        todoCommand.AddCommand(updateCommand);

        return todoCommand;
    }

    private Command CreateTimeCommands()
    {
        var timeCommand = new Command("time", "Manage time tracking");

        // time start
        var startCommand = new Command("start", "Start time tracking for a todo");
        var todoIdOption = new Option<int>("--todo-id", "ID of the todo item") { IsRequired = true };
        var descriptionOption = new Option<string?>("--description", "Description of the time entry");

        startCommand.AddOption(todoIdOption);
        startCommand.AddOption(descriptionOption);

        startCommand.SetHandler(async (int todoId, string? description) =>
        {
            var timeService = _serviceProvider.GetRequiredService<ITimeTrackingService>();
            try
            {
                await timeService.StartTimerAsync(todoId, description);
                AnsiConsole.MarkupLine($"[green]⏱️ Timer started for todo {todoId}![/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Error starting timer: {ex.Message}[/]");
            }
        }, todoIdOption, descriptionOption);

        // time stop
        var stopCommand = new Command("stop", "Stop time tracking");
        var entryIdOption = new Option<int?>("--entry-id", "ID of the time entry to stop (optional, stops latest if not provided)");

        stopCommand.AddOption(entryIdOption);
        stopCommand.SetHandler(async (int? entryId) =>
        {
            var timeService = _serviceProvider.GetRequiredService<ITimeTrackingService>();
            try
            {
                await timeService.StopTimerAsync(entryId);
                AnsiConsole.MarkupLine("[green]⏹️ Timer stopped![/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Error stopping timer: {ex.Message}[/]");
            }
        }, entryIdOption);

        // time add
        var addTimeCommand = new Command("add", "Add manual time entry");
        var addTodoIdOption = new Option<int>("--todo-id", "ID of the todo item") { IsRequired = true };
        var startTimeOption = new Option<DateTime>("--start", "Start time (yyyy-MM-dd HH:mm)") { IsRequired = true };
        var endTimeOption = new Option<DateTime>("--end", "End time (yyyy-MM-dd HH:mm)") { IsRequired = true };
        var addDescriptionOption = new Option<string?>("--description", "Description of the time entry");

        addTimeCommand.AddOption(addTodoIdOption);
        addTimeCommand.AddOption(startTimeOption);
        addTimeCommand.AddOption(endTimeOption);
        addTimeCommand.AddOption(addDescriptionOption);

        addTimeCommand.SetHandler(async (int todoId, DateTime start, DateTime end, string? description) =>
        {
            var timeService = _serviceProvider.GetRequiredService<ITimeTrackingService>();
            try
            {
                await timeService.AddManualTimeEntryAsync(todoId, start, end, description);
                var duration = end - start;
                AnsiConsole.MarkupLine($"[green]✅ Time entry added: {duration.TotalHours:F2} hours![/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Error adding time entry: {ex.Message}[/]");
            }
        }, addTodoIdOption, startTimeOption, endTimeOption, addDescriptionOption);

        // time list
        var listTimeCommand = new Command("list", "List time entries");
        var dateOption = new Option<DateTime?>("--date", "Filter by date (yyyy-MM-dd)");
        var timeRangeOption = new Option<int>("--days", () => 7, "Number of days to show");

        listTimeCommand.AddOption(dateOption);
        listTimeCommand.AddOption(timeRangeOption);

        listTimeCommand.SetHandler(async (DateTime? date, int days) =>
        {
            var timeService = _serviceProvider.GetRequiredService<ITimeTrackingService>();
            try
            {
                await timeService.ListTimeEntriesAsync(date, days);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Error listing time entries: {ex.Message}[/]");
            }
        }, dateOption, timeRangeOption);

        // time active
        var activeCommand = new Command("active", "Show active timers");
        activeCommand.SetHandler(async () =>
        {
            var timeService = _serviceProvider.GetRequiredService<ITimeTrackingService>();
            try
            {
                await timeService.ShowActiveTimersAsync();
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Error showing active timers: {ex.Message}[/]");
            }
        });

        timeCommand.AddCommand(startCommand);
        timeCommand.AddCommand(stopCommand);
        timeCommand.AddCommand(addTimeCommand);
        timeCommand.AddCommand(listTimeCommand);
        timeCommand.AddCommand(activeCommand);

        return timeCommand;
    }

    private Command CreateReportCommands()
    {
        var reportCommand = new Command("report", "Generate reports");

        // report daily
        var dailyCommand = new Command("daily", "Generate daily report");
        var dailyDateOption = new Option<DateTime?>("--date", () => DateTime.Today, "Date for the report (yyyy-MM-dd)");

        dailyCommand.AddOption(dailyDateOption);
        dailyCommand.SetHandler(async (DateTime? date) =>
        {
            var reportService = _serviceProvider.GetRequiredService<IReportService>();
            try
            {
                await reportService.GenerateDailyReportAsync(date ?? DateTime.Today);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Error generating daily report: {ex.Message}[/]");
            }
        }, dailyDateOption);

        // report weekly
        var weeklyCommand = new Command("weekly", "Generate weekly report");
        var weekStartOption = new Option<DateTime?>("--start", "Start of the week (yyyy-MM-dd)");

        weeklyCommand.AddOption(weekStartOption);
        weeklyCommand.SetHandler(async (DateTime? startDate) =>
        {
            var reportService = _serviceProvider.GetRequiredService<IReportService>();
            try
            {
                await reportService.GenerateWeeklyReportAsync(startDate);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Error generating weekly report: {ex.Message}[/]");
            }
        }, weekStartOption);

        // report summary
        var summaryCommand = new Command("summary", "Generate time summary");
        var summaryDaysOption = new Option<int>("--days", () => 30, "Number of days to include");

        summaryCommand.AddOption(summaryDaysOption);
        summaryCommand.SetHandler(async (int days) =>
        {
            var reportService = _serviceProvider.GetRequiredService<IReportService>();
            try
            {
                await reportService.GenerateTimeSummaryAsync(days);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Error generating summary: {ex.Message}[/]");
            }
        }, summaryDaysOption);

        // report calendar-day
        var calendarDayCommand = new Command("calendar-day", "Generate daily calendar view");
        var calendarDayDateOption = new Option<DateTime?>("--date", () => DateTime.Today, "Date for the calendar (yyyy-MM-dd)");

        calendarDayCommand.AddOption(calendarDayDateOption);
        calendarDayCommand.SetHandler(async (DateTime? date) =>
        {
            var reportService = _serviceProvider.GetRequiredService<IReportService>();
            try
            {
                var targetDate = date ?? DateTime.Today;
                await reportService.GenerateCalendarReportAsync(targetDate, targetDate, CalendarReportType.Daily);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Error generating calendar day report: {ex.Message}[/]");
            }
        }, calendarDayDateOption);

        // report calendar-week
        var calendarWeekCommand = new Command("calendar-week", "Generate weekly calendar view");
        var calendarWeekStartOption = new Option<DateTime?>("--start", () => GetStartOfWeek(DateTime.Today), "Start of the week (yyyy-MM-dd)");

        calendarWeekCommand.AddOption(calendarWeekStartOption);
        calendarWeekCommand.SetHandler(async (DateTime? startDate) =>
        {
            var reportService = _serviceProvider.GetRequiredService<IReportService>();
            try
            {
                var weekStart = startDate ?? GetStartOfWeek(DateTime.Today);
                var weekEnd = weekStart.AddDays(6);
                await reportService.GenerateCalendarReportAsync(weekStart, weekEnd, CalendarReportType.Weekly);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Error generating calendar week report: {ex.Message}[/]");
            }
        }, calendarWeekStartOption);

        // report calendar-month
        var calendarMonthCommand = new Command("calendar-month", "Generate monthly calendar view");
        var calendarMonthOption = new Option<DateTime?>("--month", () => new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1), "Month for the calendar (yyyy-MM-dd)");

        calendarMonthCommand.AddOption(calendarMonthOption);
        calendarMonthCommand.SetHandler(async (DateTime? month) =>
        {
            var reportService = _serviceProvider.GetRequiredService<IReportService>();
            try
            {
                var targetMonth = month ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                var monthStart = new DateTime(targetMonth.Year, targetMonth.Month, 1);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                await reportService.GenerateCalendarReportAsync(monthStart, monthEnd, CalendarReportType.Monthly);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Error generating calendar month report: {ex.Message}[/]");
            }
        }, calendarMonthOption);

        // report kanban
        var kanbanCommand = new Command("kanban", "Generate Kanban board view");
        var kanbanCategoryOption = new Option<string?>("--category", "Filter by category");
        var kanbanPriorityOption = new Option<Priority?>("--priority", "Filter by priority");
        var kanbanStatusOption = new Option<Timekeeper.Domain.Enums.TaskStatus?>("--status", "Filter by status");

        kanbanCommand.AddOption(kanbanCategoryOption);
        kanbanCommand.AddOption(kanbanPriorityOption);
        kanbanCommand.AddOption(kanbanStatusOption);
        
        kanbanCommand.SetHandler(async (string? category, Priority? priority, Timekeeper.Domain.Enums.TaskStatus? status) =>
        {
            var reportService = _serviceProvider.GetRequiredService<IReportService>();
            try
            {
                await reportService.GenerateKanbanReportAsync(category, priority, status);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Error generating kanban board: {ex.Message}[/]");
            }
        }, kanbanCategoryOption, kanbanPriorityOption, kanbanStatusOption);

        reportCommand.AddCommand(dailyCommand);
        reportCommand.AddCommand(weeklyCommand);
        reportCommand.AddCommand(summaryCommand);
        reportCommand.AddCommand(calendarDayCommand);
        reportCommand.AddCommand(calendarWeekCommand);
        reportCommand.AddCommand(calendarMonthCommand);
        reportCommand.AddCommand(kanbanCommand);

        return reportCommand;
    }

    private Command CreateProviderCommands()
    {
        var providerCommand = new Command("provider", "Provider integration commands");

        // provider sync
        var syncCommand = new Command("sync", "Sync with integrated providers");
        syncCommand.SetHandler(async () =>
        {
            var devopsService = _serviceProvider.GetRequiredService<DevOpsSyncService>();
            try
            {
                await devopsService.SyncAllAsync();
                AnsiConsole.MarkupLine("[green]✅ Provider sync completed![/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Error during sync: {ex.Message}[/]");
            }
        });

        // provider status
        var statusCommand = new Command("status", "Show provider integration status");
        statusCommand.SetHandler(async () =>
        {
            var providerService = _serviceProvider.GetRequiredService<ProviderIntegrationService>();
            try
            {
                await providerService.ShowStatusAsync();
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Error getting status: {ex.Message}[/]");
            }
        });

        // provider list
        var listCommand = new Command("list", "List all configured providers");
        listCommand.SetHandler(async () =>
        {
            var integrationManager = _serviceProvider.GetRequiredService<IntegrationManager>();
            try
            {
                var integrationRepository = _serviceProvider.GetRequiredService<IProvidersIntegrationRepository>();
                var integrations = await integrationRepository.GetAllAsync();
                
                if (!integrations.Any())
                {
                    AnsiConsole.MarkupLine("[yellow]No providers configured.[/]");
                    return;
                }

                var table = new Table();
                table.AddColumn("Provider");
                table.AddColumn("Organization");
                table.AddColumn("Project");
                table.AddColumn("Status");
                table.AddColumn("Last Sync");

                foreach (var integration in integrations)
                {
                    var status = integration.IsActive ? "[green]Active[/]" : "[red]Inactive[/]";
                    var lastSync = integration.LastSyncAt?.ToString("yyyy-MM-dd HH:mm") ?? "Never";
                    
                    table.AddRow(
                        integration.Provider,
                        integration.OrganizationUrl,
                        integration.ProjectName ?? "N/A",
                        status,
                        lastSync
                    );
                }

                AnsiConsole.Write(table);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Error listing providers: {ex.Message}[/]");
            }
        });

        // provider manage
        var manageCommand = new Command("manage", "Open interactive provider management");
        manageCommand.SetHandler(async () =>
        {
            var integrationsUI = _serviceProvider.GetRequiredService<IntegrationsUI>();
            try
            {
                await integrationsUI.ShowAsync();
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Error opening provider management: {ex.Message}[/]");
            }
        });

        providerCommand.AddCommand(syncCommand);
        providerCommand.AddCommand(statusCommand);
        providerCommand.AddCommand(listCommand);
        providerCommand.AddCommand(manageCommand);

        return providerCommand;
    }

    private Command CreateConfigCommands()
    {
        var configCommand = new Command("config", "Configuration commands");

        // config show
        var showCommand = new Command("show", "Show current configuration");
        showCommand.SetHandler(() =>
        {
            AnsiConsole.MarkupLine("[yellow]Configuration commands will be implemented based on your settings structure[/]");
        });

        // config set
        var setCommand = new Command("set", "Set configuration value");
        var keyOption = new Option<string>("--key", "Configuration key") { IsRequired = true };
        var valueOption = new Option<string>("--value", "Configuration value") { IsRequired = true };

        setCommand.AddOption(keyOption);
        setCommand.AddOption(valueOption);
        setCommand.SetHandler((string key, string value) =>
        {
            AnsiConsole.MarkupLine($"[yellow]Setting {key} = {value} (implementation pending)[/]");
        }, keyOption, valueOption);

        configCommand.AddCommand(showCommand);
        configCommand.AddCommand(setCommand);

        return configCommand;
    }

    private static void ShowAboutInfo()
    {
        var title = new FigletText("Timekeeper")
            .LeftJustified()
            .Color(Color.Blue);

        AnsiConsole.Write(title);

        AnsiConsole.MarkupLine("[bold cyan]Timekeeper[/] - Personal Productivity & Time Tracking CLI");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]Version:[/] 1.0.0");
        AnsiConsole.MarkupLine("[yellow]Description:[/] A comprehensive CLI tool for managing tasks, tracking time, and integrating with external providers.");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold green]🧑‍💻 Created by:[/] [link=https://github.com/andrrff]andrrff[/]");
        AnsiConsole.MarkupLine("[bold green]🌐 GitHub:[/] [link=https://github.com/andrrff]https://github.com/andrrff[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Built with ❤️ using .NET, Entity Framework, MediatR, and Spectre.Console[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Features:[/]");
        AnsiConsole.MarkupLine("• [green]Todo Management[/] - Create, organize, and track your tasks");
        AnsiConsole.MarkupLine("• [green]Time Tracking[/] - Monitor time spent on activities");
        AnsiConsole.MarkupLine("• [green]Provider Integrations[/] - Sync with Azure DevOps, GitHub, and more");
        AnsiConsole.MarkupLine("• [green]Reports & Analytics[/] - Get insights into your productivity");
        AnsiConsole.MarkupLine("• [green]Cross-platform[/] - Works on Windows, macOS, and Linux");
    }

    private static DateTime GetStartOfWeek(DateTime date)
    {
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        var diff = (7 + (date.DayOfWeek - culture.DateTimeFormat.FirstDayOfWeek)) % 7;
        return date.AddDays(-1 * diff).Date;
    }
}
