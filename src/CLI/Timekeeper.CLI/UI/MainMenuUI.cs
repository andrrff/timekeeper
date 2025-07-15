using Spectre.Console;
using Timekeeper.CLI.Services;
using System.Text;

namespace Timekeeper.CLI.UI;

public class MainMenuUI
{
    private readonly TodoItemUI _todoItemUI;
    private readonly TimeTrackingUI _timeTrackingUI;
    private readonly ReportsUI _reportsUI;
    private readonly ConfigurationUI _configurationUI;
    private readonly ProviderIntegrationService _devOpsIntegrationService;
    private readonly DevOpsSyncService _devOpsSyncService;
    private readonly IntegrationsUI _integrationsUI;
    private readonly CalendarUI _calendarUI;
    private readonly KanbanUI _kanbanUI;
    private readonly ITodoService _todoService;

    public MainMenuUI(TodoItemUI todoItemUI, TimeTrackingUI timeTrackingUI, ReportsUI reportsUI, ConfigurationUI configurationUI, 
                     ProviderIntegrationService devOpsIntegrationService, DevOpsSyncService devOpsSyncService, 
                     IntegrationsUI integrationsUI, CalendarUI calendarUI, KanbanUI kanbanUI, ITodoService todoService)
    {
        _todoItemUI = todoItemUI;
        _timeTrackingUI = timeTrackingUI;
        _reportsUI = reportsUI;
        _configurationUI = configurationUI;
        _devOpsIntegrationService = devOpsIntegrationService;
        _devOpsSyncService = devOpsSyncService;
        _integrationsUI = integrationsUI;
        _calendarUI = calendarUI;
        _kanbanUI = kanbanUI;
        _todoService = todoService;
    }

    public async Task ShowAsync()
    {
        var navigationStack = new Stack<string>();
        navigationStack.Push("main");
        
        while (navigationStack.Count > 0)
        {
            var currentPage = navigationStack.Peek();
            
            switch (currentPage)
            { 
                case "main":
                    var mainResult = await ShowMainMenuAsync();
                    if (mainResult == "exit")
                    {
                        navigationStack.Clear();
                    }
                    else if (mainResult == "back")
                    {
                        navigationStack.Pop();
                    }
                    else if (!string.IsNullOrEmpty(mainResult))
                    {
                        navigationStack.Push(mainResult);
                    }
                    break;
                    
                case "manage_todo":
                    await _todoItemUI.ShowAsync();
                    navigationStack.Pop();
                    break;
                    
                case "time_tracking":
                    await _timeTrackingUI.ShowAsync();
                    navigationStack.Pop();
                    break;
                    
                case "view_reports":
                    await _reportsUI.ShowAsync();
                    navigationStack.Pop();
                    break;
                    
                case "configuration":
                    await _configurationUI.ShowAsync();
                    navigationStack.Pop();
                    break;
                    
                case "about":
                    ShowAbout();
                    navigationStack.Pop();
                    break;
                    
                default:
                    navigationStack.Pop();
                    break;
            }
        }
        
        ShowGoodbye();
    }

    private async Task<string> ShowMainMenuAsync()
    {
        
        
        var menuItems = new List<(string key, string icon, string value, string description)>
        {
            ("1", "📋", "manage_todo", "Manage Todo Items - Create, edit, and organize your tasks"),
            ("2", "⏱️", "time_tracking", "Time Tracking - Monitor and track your productivity"),
            ("3", "📊", "view_reports", "View Reports - Analyze your time and productivity data"),
            ("4", "⚙️", "configuration", "Configuration - Customize your Timekeeper settings"),
            ("5", "ℹ️", "about", "About - Information about Timekeeper and its creator"),
            ("0", "❌", "exit", "Exit - Close Timekeeper application")
        };

        return await ShowInteractiveMenuAsync("🏠 Main Menu", menuItems, "What would you like to do today?");
    }

    private async Task<string> ShowInteractiveMenuAsync(string title, List<(string key, string icon, string value, string description)> items, string subtitle = "")
    {
        var searchTerm = "";
        var selectedIndex = 0;
        var filteredItems = items;
        
        while (true)
        {
            Console.Clear();
            await ShowWelcomeAnimatedAsync();
            
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
                    selectedIndex = (selectedIndex - 1 + filteredItems.Count) % filteredItems.Count;
                    break;
                    
                case ConsoleKey.DownArrow:
                    selectedIndex = (selectedIndex + 1) % filteredItems.Count;
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
                        return "exit";
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
                    // Handle number shortcuts
                    if (char.IsDigit(key.KeyChar))
                    {
                        var numberChoice = key.KeyChar.ToString();
                        var shortcutItem = filteredItems.FirstOrDefault(i => i.key == numberChoice);
                        if (shortcutItem != default)
                        {
                            return shortcutItem.value;
                        }
                    }
                    // Handle text search
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
            "manage_todo" => "Manage Todo Items",
            "time_tracking" => "Time Tracking",
            "view_reports" => "View Reports",
            "configuration" => "Configuration",
            "sync_devops" => "Sync with Azure DevOps",
            "about" => "About",
            "exit" => "Exit",
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

    private async Task ShowWelcomeAnimatedAsync()
    {
        Console.Clear();
        
        // Animated title effect
        var title = new FigletText("Timekeeper")
            .LeftJustified()
            .Color(Color.Blue);

        AnsiConsole.Write(title);

        // Get task summary
        var taskSummary = await GetTaskSummaryAsync();

        // Animated welcome panel with gradient and task summary
        var welcomeContent = new StringBuilder();
        welcomeContent.AppendLine("[bold yellow]Welcome to Timekeeper![/]\n");
        welcomeContent.AppendLine("[grey]Your personal productivity and time tracking companion.[/]");
        welcomeContent.AppendLine("[grey]Manage tasks, track time, and integrate with Azure DevOps.[/]\n");
        
        // Add task summary
        welcomeContent.AppendLine("[bold cyan]📊 Quick Summary:[/]");
        welcomeContent.AppendLine($"[green]✅ Completed:[/] {taskSummary.CompletedTasks}");
        welcomeContent.AppendLine($"[yellow]🔄 In Progress:[/] {taskSummary.InProgressTasks}");
        welcomeContent.AppendLine($"[grey]📋 Pending:[/] {taskSummary.PendingTasks}");
        if (taskSummary.OverdueTasks > 0)
        {
            welcomeContent.AppendLine($"[red]⚠️ Overdue:[/] {taskSummary.OverdueTasks}");
        }
        welcomeContent.AppendLine($"[blue]📈 Total Tasks:[/] {taskSummary.TotalTasks}\n");
        
        welcomeContent.AppendLine("[dim]Created by [link=https://github.com/andrrff]andrrff[/] | [link=https://github.com/andrrff]GitHub[/][/]");

        var welcomePanel = new Panel(new Markup(welcomeContent.ToString()))
        {
            Border = BoxBorder.Double,
            BorderStyle = new Style(Color.Blue),
            Header = new PanelHeader(" 🚀 Welcome "),
            Padding = new Padding(2, 1)
        };

        AnsiConsole.Write(welcomePanel);
        AnsiConsole.WriteLine();
        
        // Show loading animation
        AnsiConsole.Status()
            .Spinner(Spinner.Known.Star)
            .SpinnerStyle(Style.Parse("blue"))
            .Start("Loading Timekeeper...", ctx =>
            {
            });
    }

    private async Task<TaskSummary> GetTaskSummaryAsync()
    {
        try
        {
            var todos = await _todoService.GetAllTodosAsync();
            var todosList = todos.ToList();

            var completedTasks = todosList.Count(t => t.Status == Timekeeper.Domain.Enums.TaskStatus.Completed);
            var inProgressTasks = todosList.Count(t => t.Status == Timekeeper.Domain.Enums.TaskStatus.InProgress);
            var pendingTasks = todosList.Count(t => t.Status == Timekeeper.Domain.Enums.TaskStatus.Pending);
            var overdueTasks = todosList.Count(t => t.DueDate.HasValue && t.DueDate.Value < DateTime.Now && 
                                               t.Status != Timekeeper.Domain.Enums.TaskStatus.Completed);

            return new TaskSummary
            {
                TotalTasks = todosList.Count,
                CompletedTasks = completedTasks,
                InProgressTasks = inProgressTasks,
                PendingTasks = pendingTasks,
                OverdueTasks = overdueTasks
            };
        }
        catch
        {
            // Return empty summary if there's an error
            return new TaskSummary();
        }
    }

    private class TaskSummary
    {
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int PendingTasks { get; set; }
        public int OverdueTasks { get; set; }
    }

    private static bool SupportsEmojis()
    {
        try
        {
            // Test if console can properly display emojis
            var testEmoji = "📋";
            var encoded = Console.OutputEncoding.GetBytes(testEmoji);
            var decoded = Console.OutputEncoding.GetString(encoded);
            return decoded == testEmoji && Console.OutputEncoding.CodePage == 65001; // UTF-8
        }
        catch
        {
            return false;
        }
    }

    private static void ShowWelcome()
    {
        var title = new FigletText("Timekeeper")
            .LeftJustified()
            .Color(Color.Blue);

        AnsiConsole.Write(title);

        var panel = new Panel(
            new Markup("[bold yellow]Welcome to Timekeeper![/]\n\n" +
                      "[grey]Your personal productivity and time tracking companion.[/]\n" +
                      "[grey]Manage tasks, track time, and integrate with Azure DevOps.[/]\n\n" +
                      "[dim]Created by [link=https://github.com/andrrff]andrrff[/] | [link=https://github.com/andrrff]GitHub[/][/]"))
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Blue)
        };

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    private static void ShowGoodbye()
    {
        var panel = new Panel(
            new Markup("[bold green]Thank you for using Timekeeper![/]\n\n" +
                      "[grey]Stay productive! 🚀[/]\n\n" +
                      "[dim]Created with ❤️ by [link=https://github.com/andrrff]andrrff[/][/]"))
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Green)
        };

        AnsiConsole.Write(panel);
    }

    private async Task ShowProviderSyncAsync()
    {
        // Open the integrations UI directly for better provider management
        await _integrationsUI.ShowAsync();
    }

    private static void ShowErrorMessage(string title, string message)
    {
        Console.Clear();
        
        var errorPanel = new Panel(
            new Markup($"[red bold]{title}[/]\n\n[white]{message.EscapeMarkup()}[/]"))
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Red),
            Header = new PanelHeader(" ❌ Error "),
            Padding = new Padding(2, 1)
        };

        AnsiConsole.Write(errorPanel);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("Press [blue]any key[/] to continue...");
        Console.ReadKey();
    }

    private static void ShowSuccessMessage(string title, string message)
    {
        var successPanel = new Panel(
            new Markup($"[green bold]{title}[/]\n\n[white]{message.EscapeMarkup()}[/]"))
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Green),
            Header = new PanelHeader(" ✅ Success "),
            Padding = new Padding(2, 1)
        };

        AnsiConsole.Write(successPanel);
    }

    private async Task PerformNewItemsSyncAsync()
    {
        Console.Clear();
        
        var titleRule = new Rule("[bold cyan]🔄 Syncing New Work Items to Todos[/]")
        {
            Style = Style.Parse("cyan"),
            Justification = Justify.Center
        };
        AnsiConsole.Write(titleRule);
        AnsiConsole.WriteLine();

        SyncResult? result = null;
        
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Star2)
            .SpinnerStyle(Style.Parse("cyan"))
            .StartAsync("Initializing sync process...", async ctx =>
            {
                ctx.Status("[yellow]🔍 Testing Azure DevOps connection...[/]");
                
                ctx.Status("[yellow]📡 Fetching work items from DevOps...[/]");
                
                ctx.Status("[yellow]⚡ Creating todos from work items...[/]");
                result = await _devOpsSyncService.SyncWorkItemsToTodosAsync();
                
                ctx.Status("[green]✅ Sync completed![/]");
            });

        AnsiConsole.WriteLine();
        
        if (result != null)
        {
            DisplayEnhancedSyncResult(result);
        }
    }

    private async Task PerformUpdateSyncAsync()
    {
        Console.Clear();
        
        var titleRule = new Rule("[bold yellow]🔁 Updating Existing Todos from Work Items[/]")
        {
            Style = Style.Parse("yellow"),
            Justification = Justify.Center
        };
        AnsiConsole.Write(titleRule);
        AnsiConsole.WriteLine();

        SyncResult? result = null;
        
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots12)
            .SpinnerStyle(Style.Parse("yellow"))
            .StartAsync("Preparing update process...", async ctx =>
            {
                ctx.Status("[yellow]🔍 Scanning existing todos...[/]");
                
                ctx.Status("[yellow]📡 Checking work item status in DevOps...[/]");
                
                ctx.Status("[yellow]🔄 Updating todo status...[/]");
                result = await _devOpsSyncService.UpdateTodosFromWorkItemsAsync();
                
                ctx.Status("[green]✅ Update completed![/]");
            });

        AnsiConsole.WriteLine();
        
        if (result != null)
        {
            DisplayEnhancedSyncResult(result);
        }
    }

    private async Task ViewSyncStatusAsync()
    {
        Console.Clear();
        
        var titleRule = new Rule("[bold blue]📊 Azure DevOps Sync Status[/]")
        {
            Style = Style.Parse("blue"),
            Justification = Justify.Center
        };
        AnsiConsole.Write(titleRule);
        AnsiConsole.WriteLine();

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Line)
            .SpinnerStyle(Style.Parse("blue"))
            .StartAsync("Loading sync status...", async ctx =>
            {
                await _devOpsIntegrationService.ShowStatusAsync();
            });
    }

    private static void DisplayEnhancedSyncResult(SyncResult result)
    {
        // Create summary table
        var summaryTable = new Table()
        {
            Border = TableBorder.Rounded,
            BorderStyle = new Style(result.IsSuccess ? Color.Green : Color.Red)
        };
        
        summaryTable.AddColumn(new TableColumn("[bold]Action[/]").Centered());
        summaryTable.AddColumn(new TableColumn("[bold]Count[/]").Centered());
        
        if (result.CreatedCount > 0)
            summaryTable.AddRow("[green]Created[/]", $"[green bold]{result.CreatedCount}[/]");
        if (result.UpdatedCount > 0)
            summaryTable.AddRow("[yellow]Updated[/]", $"[yellow bold]{result.UpdatedCount}[/]");
        if (result.SkippedCount > 0)
            summaryTable.AddRow("[dim]Skipped[/]", $"[dim]{result.SkippedCount}[/]");
        if (result.ErrorCount > 0)
            summaryTable.AddRow("[red]Errors[/]", $"[red bold]{result.ErrorCount}[/]");
        
        var summaryPanel = new Panel(summaryTable)
        {
            Header = new PanelHeader($" 📊 Sync Results "),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(result.IsSuccess ? Color.Green : Color.Red)
        };
        
        AnsiConsole.Write(summaryPanel);
        AnsiConsole.WriteLine();

        // Show message
        var statusIcon = result.IsSuccess ? "✅" : "❌";
        var messageStyle = result.IsSuccess ? "green" : "red";
        AnsiConsole.MarkupLine($"[{messageStyle}]{statusIcon} {result.Message.EscapeMarkup()}[/]");

        // Show created items with animation
        if (result.CreatedItems.Any())
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold green]🎉 Created Items:[/]");
            
            foreach (var item in result.CreatedItems.Take(5))
            {
                AnsiConsole.MarkupLine($"[green]  ▶ {item.EscapeMarkup()}[/]");
            }
            
            if (result.CreatedItems.Count > 5)
                AnsiConsole.MarkupLine($"[grey]  ... and {result.CreatedItems.Count - 5} more items[/]");
        }

        // Show updated items
        if (result.UpdatedItems.Any())
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold yellow]🔄 Updated Items:[/]");
            
            foreach (var item in result.UpdatedItems.Take(5))
            {
                AnsiConsole.MarkupLine($"[yellow]  ▶ {item.EscapeMarkup()}[/]");
            }
            
            if (result.UpdatedItems.Count > 5)
                AnsiConsole.MarkupLine($"[grey]  ... and {result.UpdatedItems.Count - 5} more items[/]");
        }

        // Show errors
        if (result.Errors.Any())
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold red]❌ Errors:[/]");
            
            foreach (var error in result.Errors.Take(3))
            {
                AnsiConsole.MarkupLine($"[red]  ▶ {error.EscapeMarkup()}[/]");
            }
            
            if (result.Errors.Count > 3)
                AnsiConsole.MarkupLine($"[grey]  ... and {result.Errors.Count - 3} more errors[/]");
        }
    }

    private static void ShowAbout()
    {
        Console.Clear();
        
        // Animated title with rainbow effect
        var title = new FigletText("About")
            .LeftJustified()
            .Color(Color.Blue);

        AnsiConsole.Write(title);

        // Main info panel with enhanced styling
        var aboutPanel = new Panel(
            new Markup("[bold cyan]Timekeeper[/] - Personal Productivity & Time Tracking\n\n" +
                      "[yellow]Version:[/] [bold]1.0.0[/]\n" +
                      "[yellow]Platform:[/] [bold].NET 9.0 Cross-platform[/]\n" +
                      "[yellow]Description:[/] A comprehensive CLI tool for managing tasks,\n" +
                      "                tracking time, and integrating with Azure DevOps.\n\n" +
                      "[bold green]🧑‍💻 Created by:[/] [link=https://github.com/andrrff][bold cyan]andrrff[/][/]\n" +
                      "[bold green]🌐 GitHub:[/] [link=https://github.com/andrrff][bold blue]https://github.com/andrrff[/][/]\n\n" +
                      "[dim]Built with ❤️ using .NET, Entity Framework, MediatR, and Spectre.Console[/]"))
        {
            Border = BoxBorder.Double,
            BorderStyle = new Style(Color.Blue),
            Header = new PanelHeader(" 📋 About Timekeeper "),
            Padding = new Padding(2, 1)
        };

        AnsiConsole.Write(aboutPanel);
        AnsiConsole.WriteLine();

        // Features showcase
        var featuresTable = new Table()
        {
            Border = TableBorder.Rounded,
            BorderStyle = new Style(Color.Green)
        };
        
        featuresTable.AddColumn(new TableColumn("[bold]Feature[/]").LeftAligned());
        featuresTable.AddColumn(new TableColumn("[bold]Description[/]").LeftAligned());
        
        featuresTable.AddRow("[green]📋 Todo Management[/]", "Create, organize, and track your tasks with priorities");
        featuresTable.AddRow("[green]⏱️ Time Tracking[/]", "Monitor time spent on activities with detailed reports");
        featuresTable.AddRow("[green]🔄 Azure DevOps Integration[/]", "Sync work items with your personal todos");
        featuresTable.AddRow("[green]📊 Reports & Analytics[/]", "Get insights into your productivity patterns");
        featuresTable.AddRow("[green]🌐 Cross-platform[/]", "Works seamlessly on Windows, macOS, and Linux");
        featuresTable.AddRow("[green]⚡ Enhanced UX[/]", "Modern CLI with animations, search, and keyboard shortcuts");

        var featuresPanel = new Panel(featuresTable)
        {
            Header = new PanelHeader(" 🚀 Features "),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Green)
        };
        
        AnsiConsole.Write(featuresPanel);
        AnsiConsole.WriteLine();

        // Navigation help
        var helpPanel = new Panel(
            "[dim]Navigation:[/] [blue]←[/] Back  [blue]Esc[/] Main Menu  [blue]Any Key[/] Continue"
        )
        {
            Border = BoxBorder.None,
            Padding = new Padding(1, 0)
        };
        
        AnsiConsole.Write(helpPanel);

        // Handle navigation
        var key = Console.ReadKey(true);
        if (key.Key == ConsoleKey.LeftArrow || key.Key == ConsoleKey.Escape)
        {
            return;
        }
    }
}
