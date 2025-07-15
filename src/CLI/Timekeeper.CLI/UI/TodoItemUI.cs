using MediatR;
using Spectre.Console;
using Timekeeper.Application.TodoItems.Commands;
using Timekeeper.Application.TodoItems.Queries;
using Timekeeper.Domain.Entities;
using Timekeeper.Domain.Enums;
using TaskStatus = Timekeeper.Domain.Enums.TaskStatus;

namespace Timekeeper.CLI.UI;

public class TodoItemUI
{
    private readonly IMediator _mediator;

    public TodoItemUI(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task ShowAsync()
    {        
        var menuItems = new List<(string key, string icon, string value, string description)>
        {
            ("1", "📝", "create", "Create New Todo - Add a new task to your list"),
            ("2", "📋", "list_all", "List All Todos - View all your tasks"),
            ("3", "🔍", "search", "Search Todos - Find specific tasks"),
            ("4", "✅", "complete", "Mark as Complete - Complete a task"),
            ("5", "✏️", "edit", "Edit Todo - Modify an existing task"),
            ("6", "🗑️", "delete", "Delete Todo - Remove a task permanently"),
            ("7", "🏷️", "filter_status", "Filter by Status - View tasks by status"),
            ("8", "⭐", "filter_priority", "Filter by Priority - View tasks by priority"),
            ("9", "📂", "filter_category", "Filter by Category - View tasks by category"),
            ("0", "⬅️", "back", "Back to Main Menu - Return to the main menu")
        };

        while (true)
        {
            var choice = await ShowInteractiveMenuAsync("📋 Todo Items Management", menuItems, "Manage your tasks and todos");
            
            switch (choice)
            {
                case "create":
                    await CreateTodoAsync();
                    break;
                case "list_all":
                    await ListAllTodosAsync();
                    break;
                case "search":
                    await SearchTodosAsync();
                    break;
                case "complete":
                    await CompleteTodoAsync();
                    break;
                case "edit":
                    await EditTodoAsync();
                    break;
                case "delete":
                    await DeleteTodoAsync();
                    break;
                case "filter_status":
                    await FilterByStatusAsync();
                    break;
                case "filter_priority":
                    await FilterByPriorityAsync();
                    break;
                case "filter_category":
                    await FilterByCategoryAsync();
                    break;
                case "back":
                    return;
            }
        }
    }

    private async Task CreateTodoAsync()
    {
        AnsiConsole.MarkupLine("[bold green]Create New Todo Item[/]");
        AnsiConsole.WriteLine();

        var title = AnsiConsole.Ask<string>("Enter [green]title[/]:");
        var description = AnsiConsole.Ask<string>("Enter [grey]description[/] (optional):", string.Empty);
        
        var priority = AnsiConsole.Prompt(
            new SelectionPrompt<Priority>()
                .Title("Select [yellow]priority[/]:")
                .AddChoices(Enum.GetValues<Priority>()));

        var category = AnsiConsole.Ask<string>("Enter [blue]category[/] (optional):", string.Empty);
        var tags = AnsiConsole.Ask<string>("Enter [cyan]tags[/] (optional):", string.Empty);

        var hasDueDate = AnsiConsole.Confirm("Set due date?");
        DateTime? dueDate = null;
        if (hasDueDate)
        {
            dueDate = AnsiConsole.Ask<DateTime>("Enter [red]due date[/] (yyyy-MM-dd):");
        }

        var estimatedTime = AnsiConsole.Ask<int>("Estimated time in [yellow]minutes[/]:", 0);

        var command = new CreateTodoItemCommand(
            title,
            string.IsNullOrWhiteSpace(description) ? null : description,
            priority,
            dueDate,
            string.IsNullOrWhiteSpace(category) ? null : category,
            string.IsNullOrWhiteSpace(tags) ? null : tags,
            estimatedTime);

        var result = await _mediator.Send(command);

        AnsiConsole.MarkupLine($"[green]✓ Todo item created with ID: {result.Id}[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("Press [blue]any key[/] to continue...");
        Console.ReadKey();
    }

    private async Task ListAllTodosAsync()
    {
        var todos = await _mediator.Send(new GetAllTodoItemsQuery());
        DisplayTodos(todos, "All Todo Items");
    }

    private async Task SearchTodosAsync()
    {
        var searchTerm = AnsiConsole.Ask<string>("Enter search term:");
        var todos = await _mediator.Send(new SearchTodoItemsQuery(searchTerm));
        DisplayTodos(todos, $"Search Results for '{searchTerm}'");
    }

    private async Task FilterByStatusAsync()
    {
        var status = AnsiConsole.Prompt(
            new SelectionPrompt<TaskStatus>()
                .Title("Select status to filter by:")
                .AddChoices(Enum.GetValues<TaskStatus>()));

        var todos = await _mediator.Send(new GetTodoItemsByStatusQuery(status));
        DisplayTodos(todos, $"Todos with Status: {status}");
    }

    private async Task FilterByPriorityAsync()
    {
        var priority = AnsiConsole.Prompt(
            new SelectionPrompt<Priority>()
                .Title("Select priority to filter by:")
                .AddChoices(Enum.GetValues<Priority>()));

        var todos = await _mediator.Send(new GetTodoItemsByPriorityQuery(priority));
        DisplayTodos(todos, $"Todos with Priority: {priority}");
    }

    private async Task FilterByCategoryAsync()
    {
        var category = AnsiConsole.Ask<string>("Enter category to filter by:");
        var todos = await _mediator.Send(new GetTodoItemsByCategoryQuery(category));
        DisplayTodos(todos, $"Todos in Category: {category}");
    }

    private async Task CompleteTodoAsync()
    {
        var todos = await _mediator.Send(new GetAllTodoItemsQuery());
        var pendingTodos = todos.Where(t => t.Status != TaskStatus.Completed).ToList();

        if (!pendingTodos.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No pending todos found.[/]");
            AnsiConsole.MarkupLine("Press [blue]any key[/] to continue...");
            Console.ReadKey();
            return;
        }

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<TodoItem>()
                .Title("Select todo to complete:")
                .AddChoices(pendingTodos)
                .UseConverter(todo => $"{todo.Title.EscapeMarkup()} ({todo.Priority})"));

        var success = await _mediator.Send(new CompleteTodoItemCommand(selected.Id));

        if (success)
        {
            AnsiConsole.MarkupLine("[green]✓ Todo marked as completed![/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[red]❌ Failed to complete todo.[/]");
        }

        AnsiConsole.MarkupLine("Press [blue]any key[/] to continue...");
        Console.ReadKey();
    }

    private async Task EditTodoAsync()
    {
        Console.Clear();
        
        // Header with style
        var rule = new Rule("[bold yellow]📝 Edit Todo Item[/]")
        {
            Style = Style.Parse("yellow"),
            Justification = Justify.Center
        };
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();

        var todos = await _mediator.Send(new GetAllTodoItemsQuery());
        
        if (!todos.Any())
        {
            var emptyPanel = new Panel("[yellow]📭 No todos found to edit[/]\n\n[dim]Create some todos first to edit them.[/]")
            {
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Yellow),
                Header = new PanelHeader(" ⚠️ Empty List "),
                Padding = new Padding(1)
            };
            AnsiConsole.Write(emptyPanel);
            
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
            Console.ReadKey();
            return;
        }

        // Enhanced todo selection with search and details
        var selected = await SelectTodoInteractiveAsync(todos, "Select a todo to edit:");
        if (selected == null) return;

        await ShowEditTodoMenuAsync(selected);
    }

    private async Task<TodoItem?> SelectTodoInteractiveAsync(IEnumerable<TodoItem> todos, string title)
    {
        var todoList = todos.ToList();
        var searchTerm = "";
        var selectedIndex = 0;
        var filteredTodos = todoList;
        
        while (true)
        {
            Console.Clear();
            
            // Show header
            var rule = new Rule($"[bold blue]{title}[/]")
            {
                Style = Style.Parse("blue"),
                Justification = Justify.Center
            };
            AnsiConsole.Write(rule);
            AnsiConsole.WriteLine();
            
            // Filter todos based on search
            filteredTodos = string.IsNullOrEmpty(searchTerm) 
                ? todoList 
                : todoList.Where(todo => 
                    todo.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    (todo.Description?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (todo.Category?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    todo.Status.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    todo.Priority.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
            
            if (filteredTodos.Count == 0)
            {
                filteredTodos = todoList;
                searchTerm = "";
            }
            
            // Ensure selected index is valid
            selectedIndex = Math.Max(0, Math.Min(selectedIndex, filteredTodos.Count - 1));
            
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
            
            // Create and display todo selection table
            var table = CreateTodoSelectionTable(filteredTodos, selectedIndex);
            AnsiConsole.Write(table);
            
            // Show selected todo details
            if (filteredTodos.Any())
            {
                ShowTodoPreview(filteredTodos[selectedIndex]);
            }
            
            // Show navigation help
            ShowTodoSelectionHelp();
            
            // Handle input
            var key = Console.ReadKey(true);
            
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    selectedIndex = selectedIndex > 0 ? selectedIndex - 1 : filteredTodos.Count - 1;
                    break;
                    
                case ConsoleKey.DownArrow:
                    selectedIndex = selectedIndex < filteredTodos.Count - 1 ? selectedIndex + 1 : 0;
                    break;
                    
                case ConsoleKey.LeftArrow:
                case ConsoleKey.Escape:
                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        searchTerm = "";
                        selectedIndex = 0;
                    }
                    else
                    {
                        return null; // Cancel selection
                    }
                    break;
                    
                case ConsoleKey.RightArrow:
                case ConsoleKey.Enter:
                    if (filteredTodos.Count > 0)
                        return filteredTodos[selectedIndex];
                    break;
                    
                case ConsoleKey.Backspace:
                    if (searchTerm.Length > 0)
                    {
                        searchTerm = searchTerm[..^1];
                        selectedIndex = 0;
                    }
                    break;
                    
                default:
                    if (char.IsLetter(key.KeyChar) || char.IsWhiteSpace(key.KeyChar) || char.IsDigit(key.KeyChar))
                    {
                        searchTerm += key.KeyChar;
                        selectedIndex = 0;
                    }
                    break;
            }
        }
    }
    private static Table CreateTodoSelectionTable(List<TodoItem> todos, int selectedIndex)
    {
        var table = new Table()
        {
            Border = TableBorder.Rounded,
            BorderStyle = new Style(Color.Blue),
            ShowHeaders = true
        };
        
        table.AddColumn(new TableColumn("").Centered().Width(3));
        table.AddColumn(new TableColumn("Title").Width(30));
        table.AddColumn(new TableColumn("Status").Centered().Width(12));
        table.AddColumn(new TableColumn("Priority").Centered().Width(10));
        table.AddColumn(new TableColumn("Due Date").Centered().Width(12));
        
        for (int i = 0; i < todos.Count; i++)
        {
            var todo = todos[i];
            var isSelected = i == selectedIndex;
            
            var selector = isSelected ? "[bold blue]►[/]" : " ";
            var titleStyle = isSelected ? "[bold white]" : "[dim]";
            var statusIcon = GetStatusIcon(todo.Status);
            var statusColor = GetStatusColor(todo.Status);
            var priorityIcon = GetPriorityIcon(todo.Priority);
            var priorityColor = GetPriorityColor(todo.Priority);
            var dueDateDisplay = GetDueDateDisplay(todo.DueDate);
            
            table.AddRow(
                selector,
                $"{titleStyle}{todo.Title.EscapeMarkup().Truncate(28)}[/]",
                $"{statusIcon} [{statusColor}]{todo.Status}[/]",
                $"{priorityIcon} [{priorityColor}]{todo.Priority}[/]",
                dueDateDisplay
            );
        }
        
        return table;
    }

    private static void ShowTodoPreview(TodoItem todo)
    {
        AnsiConsole.WriteLine();
        
        var previewTable = new Table()
        {
            Border = TableBorder.None,
            ShowHeaders = false
        };
        
        previewTable.AddColumn("Property");
        previewTable.AddColumn("Value");
        
        previewTable.AddRow("[bold]ID:[/]", $"[cyan]{todo.Id.ToString()[..8]}...[/]");
        previewTable.AddRow("[bold]Description:[/]", string.IsNullOrEmpty(todo.Description) ? "[dim italic]No description[/]" : todo.Description.EscapeMarkup().Truncate(50));
        previewTable.AddRow("[bold]Category:[/]", todo.Category ?? "[dim]No category[/]");
        previewTable.AddRow("[bold]Tags:[/]", todo.Tags ?? "[dim]No tags[/]");
        previewTable.AddRow("[bold]Estimated Time:[/]", todo.EstimatedTimeMinutes > 0 ? $"{todo.EstimatedTimeMinutes} minutes" : "[dim]Not set[/]");
        previewTable.AddRow("[bold]Created:[/]", GetCreatedDateDisplay(todo.CreatedAt));
        previewTable.AddRow("[bold]Source:[/]", GetSourceInfo(todo));
        
        var previewPanel = new Panel(previewTable)
        {
            Header = new PanelHeader(" 👁️ Preview "),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Grey),
            Padding = new Padding(1, 0)
        };
        
        AnsiConsole.Write(previewPanel);
    }

    private static void ShowTodoSelectionHelp()
    {
        AnsiConsole.WriteLine();
        var helpPanel = new Panel(
            "[dim]Use [white]↑↓[/] to navigate, [white]Enter[/] to select, [white]←/Esc[/] to cancel\n" +
            "Type to search by title, description, category, status, or priority[/]")
        {
            Border = BoxBorder.None,
            Padding = new Padding(0, 0, 0, 1)
        };
        
        AnsiConsole.Write(helpPanel);
    }

    private async Task ShowEditTodoMenuAsync(TodoItem todo)
    {
        var selected = todo;
        
        // Title
        var changeTitle = AnsiConsole.Confirm("Do you want to change the title?", false);
        string newTitle = selected.Title;
        
        if (changeTitle)
        {
            newTitle = AnsiConsole.Ask<string>("New title:", selected.Title);
        }

        // Description
        var changeDescription = AnsiConsole.Confirm("Do you want to change the description?", false);
        string? newDescription = selected.Description;
        
        if (changeDescription)
        {
            newDescription = AnsiConsole.Ask<string>("New description (leave empty to remove):", selected.Description ?? "");
            if (string.IsNullOrWhiteSpace(newDescription))
            {
                newDescription = null;
            }
        }

        // Status
        var changeStatus = AnsiConsole.Confirm("Do you want to change the status?", false);
        TaskStatus newStatus = selected.Status;
        
        if (changeStatus)
        {
            var statusOptions = new[] { TaskStatus.Pending, TaskStatus.InProgress, TaskStatus.Completed, TaskStatus.Cancelled, TaskStatus.OnHold };
            newStatus = AnsiConsole.Prompt(
                new SelectionPrompt<TaskStatus>()
                    .Title("Select new status:")
                    .AddChoices(statusOptions));
        }

        // Priority
        var changePriority = AnsiConsole.Confirm("Do you want to change the priority?", false);
        Priority newPriority = selected.Priority;
        
        if (changePriority)
        {
            var priorityOptions = new[] { Priority.Low, Priority.Medium, Priority.High, Priority.Critical };
            newPriority = AnsiConsole.Prompt(
                new SelectionPrompt<Priority>()
                    .Title("Select new priority:")
                    .AddChoices(priorityOptions));
        }

        // Due Date
        var changeDueDate = AnsiConsole.Confirm("Do you want to change the due date?", false);
        DateTime? newDueDate = selected.DueDate;
        
        if (changeDueDate)
        {
            var dueDateOptions = new[] { "Set to today", "Set custom date", "Remove due date", "Keep current" };
            var dueDateChoice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Due date option:")
                    .AddChoices(dueDateOptions));

            switch (dueDateChoice)
            {
                case "Set to today":
                    newDueDate = DateTime.Today;
                    break;
                case "Set custom date":
                    var dateInput = AnsiConsole.Ask<string>("Enter date (yyyy-MM-dd):");
                    if (string.IsNullOrWhiteSpace(dateInput))
                    {
                        newDueDate = DateTime.Today;
                    }
                    else if (DateTime.TryParseExact(dateInput, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var parsedDate))
                    {
                        newDueDate = parsedDate;
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[red]Invalid date format. Keeping current due date.[/]");
                    }
                    break;
                case "Remove due date":
                    newDueDate = null;
                    break;
                case "Keep current":
                default:
                    // Keep existing due date
                    break;
            }
        }

        // Estimated Hours
        var changeEstimatedHours = AnsiConsole.Confirm("Do you want to change the estimated hours?", false);
        int? newEstimatedHours = selected.EstimatedHours;
        
        if (changeEstimatedHours)
        {
            var hoursInput = AnsiConsole.Ask<string>($"Estimated hours (current: {selected.EstimatedHours?.ToString() ?? "not set"}):");
            if (string.IsNullOrWhiteSpace(hoursInput))
            {
                newEstimatedHours = null;
            }
            else if (int.TryParse(hoursInput, out var hours) && hours >= 0)
            {
                newEstimatedHours = hours;
            }
            else
            {
                AnsiConsole.MarkupLine("[red]Invalid hours format. Keeping current estimated hours.[/]");
            }
        }        // Provider Integration
        var changeProvider = AnsiConsole.Confirm("Do you want to change provider integration?", false);
        string? newProviderWorkItemId = selected.DevOpsWorkItemId;
        string? newProviderUrl = selected.DevOpsUrl;

        if (changeProvider)
        {
            newProviderWorkItemId = AnsiConsole.Ask<string>("Provider Work Item ID (leave empty to remove):", selected.DevOpsWorkItemId ?? "");
            if (string.IsNullOrWhiteSpace(newProviderWorkItemId))
            {
                newProviderWorkItemId = null;
                newProviderUrl = null;
            }
            else
            {
                newProviderUrl = AnsiConsole.Ask<string>("Provider URL (leave empty to remove):", selected.DevOpsUrl ?? "");
                if (string.IsNullOrWhiteSpace(newProviderUrl))
                {
                    newProviderUrl = null;
                }
            }
        }

        var command = new UpdateTodoItemCommand(
            Id: selected.Id,
            Title: newTitle,
            Description: newDescription,
            Status: newStatus,
            Priority: newPriority,
            DueDate: newDueDate,
            Category: null,
            Tags: null,
            EstimatedTimeMinutes: null,
            EstimatedHours: newEstimatedHours,
            DevOpsWorkItemId: newProviderWorkItemId,
            DevOpsUrl: newProviderUrl);

        var success = await _mediator.Send(command);

        if (success)
        {
            AnsiConsole.MarkupLine("[green]✓ Todo updated successfully![/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[red]❌ Failed to update todo.[/]");
        }

        AnsiConsole.MarkupLine("Press [blue]any key[/] to continue...");
        Console.ReadKey();
    }

    private async Task DeleteTodoAsync()
    {
        var todos = await _mediator.Send(new GetAllTodoItemsQuery());
        
        if (!todos.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No todos found.[/]");
            AnsiConsole.MarkupLine("Press [blue]any key[/] to continue...");
            Console.ReadKey();
            return;
        }

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<TodoItem>()
                .Title("Select todo to delete:")
                .AddChoices(todos)
                .UseConverter(todo => $"{todo.Title.EscapeMarkup()} ({todo.Status})"));

        if (AnsiConsole.Confirm($"Are you sure you want to delete '{selected.Title.EscapeMarkup()}'?"))
        {
            var success = await _mediator.Send(new DeleteTodoItemCommand(selected.Id));

            if (success)
            {
                AnsiConsole.MarkupLine("[green]✓ Todo deleted successfully![/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[red]❌ Failed to delete todo.[/]");
            }
        }

        AnsiConsole.MarkupLine("Press [blue]any key[/] to continue...");
        Console.ReadKey();
    }

    private static void DisplayTodos(IEnumerable<TodoItem> todos, string title)
    {
        var todoList = todos.ToList();
        
        // Header with statistics
        AnsiConsole.Write(new Rule($"[bold blue]{title}[/]")
            .RuleStyle("blue")
            .LeftJustified());
        AnsiConsole.WriteLine();

        if (!todoList.Any())
        {
            var emptyPanel = new Panel("[yellow]📭 No todos found[/]")
                .Header("[bold]Empty List[/]")
                .BorderColor(Color.Yellow)
                .Padding(2, 1);
            AnsiConsole.Write(emptyPanel);
        }
        else
        {
            // Statistics panel
            var stats = GetTodoStatistics(todoList);
            var statsTable = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Grey)
                .AddColumn("[bold]Metric[/]")
                .AddColumn("[bold]Count[/]")
                .AddColumn("[bold]Percentage[/]");

            statsTable.AddRow("📋 [cyan]Total[/]", stats.Total.ToString(), "100%");
            statsTable.AddRow("✅ [green]Completed[/]", stats.Completed.ToString(), $"{stats.CompletedPercentage:F1}%");
            statsTable.AddRow("🔄 [yellow]In Progress[/]", stats.InProgress.ToString(), $"{stats.InProgressPercentage:F1}%");
            statsTable.AddRow("⏳ [grey]Pending[/]", stats.Pending.ToString(), $"{stats.PendingPercentage:F1}%");
            statsTable.AddRow("🔥 [red]Critical[/]", stats.Critical.ToString(), $"{stats.CriticalPercentage:F1}%");
            statsTable.AddRow("⚡ [orange1]High Priority[/]", stats.High.ToString(), $"{stats.HighPercentage:F1}%");

            var statsPanel = new Panel(statsTable)
                .Header("[bold]📊 Summary Statistics[/]")
                .BorderColor(Color.Blue)
                .Padding(1, 0);
            
            AnsiConsole.Write(statsPanel);
            AnsiConsole.WriteLine();

            // Enhanced main table with essential information
            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Blue)
                .Expand();

            table.AddColumn(new TableColumn("[bold]🆔 ID[/]").Centered().Width(10));
            table.AddColumn(new TableColumn("[bold]📝 Title[/]").Width(22));
            table.AddColumn(new TableColumn("[bold]📄 Description[/]").Width(25));
            table.AddColumn(new TableColumn("[bold]📊 Status[/]").Centered().Width(12));
            table.AddColumn(new TableColumn("[bold]⭐ Priority[/]").Centered().Width(10));
            table.AddColumn(new TableColumn("[bold]📂 Category[/]").Width(12));
            table.AddColumn(new TableColumn("[bold]📅 Due Date[/]").Centered().Width(12));
            table.AddColumn(new TableColumn("[bold]⏱️ Time[/]").Centered().Width(10));
            table.AddColumn(new TableColumn("[bold]🔗 Source[/]").Centered().Width(8));
            table.AddColumn(new TableColumn("[bold]📅 Created[/]").Centered().Width(10));
            table.AddColumn(new TableColumn("[bold]� Updated[/]").Centered().Width(10));

            foreach (var todo in todoList.OrderByDescending(t => t.CreatedAt))
            {
                var statusIcon = GetStatusIcon(todo.Status);
                var statusColor = GetStatusColor(todo.Status);
                var priorityIcon = GetPriorityIcon(todo.Priority);
                var priorityColor = GetPriorityColor(todo.Priority);
                
                // ID (short version)
                var shortId = todo.Id.ToString()[..8];
                
                // Description (truncated)
                var descriptionDisplay = string.IsNullOrEmpty(todo.Description) 
                    ? "[dim italic]No description[/]" 
                    : todo.Description.EscapeMarkup().Truncate(22);
                
                // Due date with urgency color
                var dueDateDisplay = GetDueDateDisplay(todo.DueDate);
                
                // Time information
                var timeInfo = GetTimeInfo(todo);
                
                // Source information (Provider integration)
                var sourceInfo = GetSourceInfo(todo);
                
                // Creation date
                var createdDisplay = GetCreatedDateDisplay(todo.CreatedAt);
                
                // Last updated date
                var updatedDisplay = GetUpdatedDateDisplay(todo.UpdatedAt);

                table.AddRow(
                    $"[cyan]{shortId}[/]",
                    $"{todo.Title.EscapeMarkup().Truncate(20)}",
                    descriptionDisplay,
                    $"{statusIcon} [{statusColor}]{todo.Status}[/]",
                    $"{priorityIcon} [{priorityColor}]{todo.Priority}[/]",
                    (todo.Category ?? "[dim]-[/]").Truncate(10),
                    dueDateDisplay,
                    timeInfo,
                    sourceInfo,
                    createdDisplay,
                    updatedDisplay
                );
            }

            AnsiConsole.Write(table);
            
            // Additional detailed information section
            if (todoList.Any())
            {
                AnsiConsole.WriteLine();
                
                // Tags overview
                var allTags = todoList
                    .Where(t => !string.IsNullOrEmpty(t.Tags))
                    .SelectMany(t => t.Tags!.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    .Select(tag => tag.Trim())
                    .GroupBy(tag => tag)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .ToList();

                if (allTags.Any())
                {
                    var tagsText = string.Join(" ", allTags.Select(g => $"[cyan]{g.Key.EscapeMarkup()}[/]([dim]{g.Count()}[/])"));
                    
                    var tagsPanel = new Panel(tagsText)
                        .Header("[bold]🏷️ Popular Tags[/]")
                        .BorderColor(Color.Aqua)
                        .Padding(1, 0);
                    
                    AnsiConsole.Write(tagsPanel);
                    AnsiConsole.WriteLine();
                }
                  // Provider integrations overview
                var providerItems = todoList.Where(t => !string.IsNullOrEmpty(t.DevOpsWorkItemId)).ToList();
                if (providerItems.Any())
                {
                    var providerText = string.Join("\n", providerItems.Take(3).Select(t => 
                        $"[blue]#{t.DevOpsWorkItemId}[/] - {t.Title.EscapeMarkup().Truncate(40)}"));
                    
                    if (providerItems.Count > 3)
                    {
                        providerText += $"\n[dim]... and {providerItems.Count - 3} more[/]";
                    }

                    var providerPanel = new Panel(providerText)
                        .Header($"[bold]� Provider Items ({providerItems.Count})[/]")
                        .BorderColor(Color.Blue)
                        .Padding(1, 0);
                    
                    AnsiConsole.Write(providerPanel);
                    AnsiConsole.WriteLine();
                }
            }
            
            // Enhanced info panel with keyboard shortcuts
            var infoText = "[dim]💡 Tips:[/]\n" +
                          "[grey]• Use[/] [cyan]filters[/] [grey]to narrow down your view[/]\n" +
                          "[grey]• Items are sorted by creation date (newest first)[/]\n" +
                          "[grey]• ID column shows first 8 characters for reference[/]\n" +
                          "[grey]• Source shows integration origin (Local/GitHub/Providers)[/]";
            
            var infoPanel = new Panel(infoText)
                .BorderColor(Color.Grey)
                .Header("[bold]ℹ️ Information[/]")
                .Padding(1, 0);
            
            AnsiConsole.Write(infoPanel);
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("Press [blue]any key[/] to continue...");
        Console.ReadKey();
    }

    private static TodoStatistics GetTodoStatistics(List<TodoItem> todos)
    {
        var total = todos.Count;
        return new TodoStatistics
        {
            Total = total,
            Completed = todos.Count(t => t.Status == TaskStatus.Completed),
            InProgress = todos.Count(t => t.Status == TaskStatus.InProgress),
            Pending = todos.Count(t => t.Status == TaskStatus.Pending),
            Critical = todos.Count(t => t.Priority == Priority.Critical),
            High = todos.Count(t => t.Priority == Priority.High),
            CompletedPercentage = total > 0 ? (double)todos.Count(t => t.Status == TaskStatus.Completed) / total * 100 : 0,
            InProgressPercentage = total > 0 ? (double)todos.Count(t => t.Status == TaskStatus.InProgress) / total * 100 : 0,
            PendingPercentage = total > 0 ? (double)todos.Count(t => t.Status == TaskStatus.Pending) / total * 100 : 0,
            CriticalPercentage = total > 0 ? (double)todos.Count(t => t.Priority == Priority.Critical) / total * 100 : 0,
            HighPercentage = total > 0 ? (double)todos.Count(t => t.Priority == Priority.High) / total * 100 : 0
        };
    }

    private static string GetStatusIcon(TaskStatus status) => status switch
    {
        TaskStatus.Completed => "✅",
        TaskStatus.InProgress => "🔄",
        TaskStatus.Pending => "⏳",
        TaskStatus.Cancelled => "❌",
        TaskStatus.OnHold => "⏸️",
        _ => "❓"
    };

    private static string GetStatusColor(TaskStatus status) => status switch
    {
        TaskStatus.Completed => "green",
        TaskStatus.InProgress => "yellow",
        TaskStatus.Pending => "grey",
        TaskStatus.Cancelled => "red",
        TaskStatus.OnHold => "orange1",
        _ => "white"
    };

    private static string GetPriorityIcon(Priority priority) => priority switch
    {
        Priority.Critical => "🔥",
        Priority.High => "⚡",
        Priority.Medium => "🟡",
        Priority.Low => "🔵",
        _ => "⚪"
    };

    private static string GetPriorityColor(Priority priority) => priority switch
    {
        Priority.Critical => "red",
        Priority.High => "orange1",
        Priority.Medium => "yellow",
        Priority.Low => "blue",
        _ => "white"
    };

    private static string GetDueDateDisplay(DateTime? dueDate)
    {
        if (!dueDate.HasValue) return "[dim]-[/]";
        
        var days = (dueDate.Value.Date - DateTime.Today).Days;
        
        return days switch
        {
            < 0 => $"[red]{dueDate.Value:MM/dd} ⚠️[/]", // Overdue
            0 => $"[orange1]{dueDate.Value:MM/dd} 📅[/]", // Today
            1 => $"[yellow]{dueDate.Value:MM/dd} ⏰[/]", // Tomorrow
            <= 7 => $"[yellow]{dueDate.Value:MM/dd}[/]", // This week
            _ => $"[grey]{dueDate.Value:MM/dd}[/]" // Future
        };
    }

    private static string GetTimeInfo(TodoItem todo)
    {
        var estimated = todo.EstimatedTimeMinutes;
        var actual = todo.ActualTimeMinutes;
        
        if (estimated == 0 && actual == 0) return "[dim]-[/]";
        
        var estimatedHours = estimated / 60.0;
        var actualHours = actual / 60.0;
        
        if (estimated > 0 && actual > 0)
        {
            var efficiency = actual <= estimated ? "✅" : "⚠️";
            return $"{actualHours:F1}h/{estimatedHours:F1}h {efficiency}";
        }
        else if (estimated > 0)
        {
            return $"~{estimatedHours:F1}h";
        }
        else
        {
            return $"{actualHours:F1}h";
        }
    }

    private static string GetSourceInfo(TodoItem todo)
    {
        if (!string.IsNullOrEmpty(todo.DevOpsWorkItemId))
        {
            return "[blue]� Provider[/]"; // Provider
        }
        
        // Check for GitHub integration (you could extend this with a GitHubWorkItemId field)
        if (todo.Title.Contains("#") || todo.Description?.Contains("github.com") == true)
        {
            return "[green]🐙 GH[/]"; // GitHub
        }
        
        return "[grey]📝 Local[/]"; // Created locally
    }

    private static string GetCreatedDateDisplay(DateTime createdAt)
    {
        var days = (DateTime.Today - createdAt.Date).Days;
        
        return days switch
        {
            0 => "[green]Today[/]",
            1 => "[yellow]Yesterday[/]",
            <= 7 => $"[grey]{days}d ago[/]",
            <= 30 => $"[dim]{days}d ago[/]",
            _ => $"[dim]{createdAt:MM/dd}[/]"
        };
    }

    private static string GetUpdatedDateDisplay(DateTime? updatedAt)
    {
        if (!updatedAt.HasValue) return "[dim]Never[/]";
        
        var days = (DateTime.Today - updatedAt.Value.Date).Days;
        
        return days switch
        {
            0 => "[green]Today[/]",
            1 => "[yellow]Yesterday[/]",
            <= 7 => $"[grey]{days}d ago[/]",
            <= 30 => $"[dim]{days}d ago[/]",
            _ => $"[dim]{updatedAt.Value:MM/dd}[/]"
        };
    }

    private static string GetTagsDisplay(string? tags)
    {
        if (string.IsNullOrEmpty(tags)) return "[dim]-[/]";
        
        var tagList = tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                         .Select(t => t.Trim())
                         .Take(2)
                         .ToList();
        
        var display = string.Join(", ", tagList.Select(t => $"[cyan]{t.EscapeMarkup()}[/]"));
        if (tags.Split(',').Length > 2)
        {
            display += "[dim]...[/]";
        }
        
        return display.Truncate(13);
    }

    private static string GetProgressInfo(TodoItem todo)
    {
        var progressBar = todo.Status switch
        {
            TaskStatus.Completed => "[green]████████████[/] 100%",
            TaskStatus.InProgress => "[yellow]██████░░░░░░[/] 50%",
            TaskStatus.Pending => "[grey]░░░░░░░░░░░░[/] 0%",
            TaskStatus.OnHold => "[orange1]███░░░░░░░░░[/] 25%",
            TaskStatus.Cancelled => "[red]▓▓▓▓▓▓▓▓▓▓▓▓[/] ❌",
            _ => "[dim]░░░░░░░░░░░░[/] ?%"
        };
        
        return progressBar;
    }

    private record TodoStatistics
    {
        public int Total { get; init; }
        public int Completed { get; init; }
        public int InProgress { get; init; }
        public int Pending { get; init; }
        public int Critical { get; init; }
        public int High { get; init; }
        public double CompletedPercentage { get; init; }
        public double InProgressPercentage { get; init; }
        public double PendingPercentage { get; init; }
        public double CriticalPercentage { get; init; }
        public double HighPercentage { get; init; }
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
            "create" => "Create New Todo",
            "list_all" => "List All Todos",
            "search" => "Search Todos",
            "complete" => "Mark as Complete",
            "edit" => "Edit Todo",
            "delete" => "Delete Todo",
            "filter_status" => "Filter by Status",
            "filter_priority" => "Filter by Priority",
            "filter_category" => "Filter by Category",
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
        var figlet = new FigletText("Todo Manager")
            .LeftJustified()
            .Color(Color.Green);

        AnsiConsole.Write(figlet);
        
        // Welcome panel with animation
        var welcomePanel = new Panel(
            new Markup("[bold green]Welcome to Todo Manager![/]\n\n" +
                      "Your personal task management companion.\n" +
                      "Create, organize, and track your todos efficiently.\n\n" +
                      "[dim]Created by [link=https://github.com/andrrff]andrrff[/] | [link=https://github.com/andrrff]GitHub[/][/]"))
        {
            Border = BoxBorder.Double,
            BorderStyle = new Style(Color.Green),
            Header = new PanelHeader(" 📋 Todo Management "),
            Padding = new Padding(2, 1)
        };

        AnsiConsole.Write(welcomePanel);
        
        // Loading animation
        AnsiConsole.Status()
            .Start("Loading Todo Manager...", ctx =>
            {
                var frames = new[] { "✶", "✸", "✹", "✺", "✹", "✷", "✶" };
                for (int i = 0; i < 7; i++)
                {
                    ctx.Status($"{frames[i]} Loading Todo Manager...");
                }
            });
            
    }

    private async Task EditTitleAsync(TodoItem todo, Dictionary<string, object> changes)
    {
        Console.Clear();
        
        var rule = new Rule("[bold blue]📝 Edit Title[/]")
        {
            Style = Style.Parse("blue"),
            Justification = Justify.Center
        };
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();

        var currentTitle = changes.ContainsKey("Title") ? changes["Title"]?.ToString() : todo.Title;
        
        var infoPanel = new Panel($"[bold]Current Title:[/]\n{currentTitle?.EscapeMarkup()}")
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Blue),
            Padding = new Padding(1)
        };
        AnsiConsole.Write(infoPanel);
        AnsiConsole.WriteLine();

        var newTitle = AnsiConsole.Ask<string>("[yellow]Enter new title:[/]", currentTitle ?? "");
        
        if (newTitle != todo.Title && !string.IsNullOrWhiteSpace(newTitle))
        {
            changes["Title"] = newTitle;
            
            var successPanel = new Panel($"[green]✓ Title updated[/]")
            {
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Green),
                Padding = new Padding(1)
            };
            AnsiConsole.Write(successPanel);
        }
        else if (changes.ContainsKey("Title"))
        {
            changes.Remove("Title");
            AnsiConsole.MarkupLine("[yellow]Title reverted to original value.[/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
        Console.ReadKey();
    }

    private async Task EditDescriptionAsync(TodoItem todo, Dictionary<string, object> changes)
    {
        Console.Clear();
        
        var rule = new Rule("[bold blue]📄 Edit Description[/]")
        {
            Style = Style.Parse("blue"),
            Justification = Justify.Center
        };
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();

        var currentDescription = changes.ContainsKey("Description") ? changes["Description"]?.ToString() : todo.Description;
        
        var infoPanel = new Panel($"[bold]Current Description:[/]\n{currentDescription?.EscapeMarkup() ?? "[dim italic]No description[/]"}")
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Blue),
            Padding = new Padding(1)
        };
        AnsiConsole.Write(infoPanel);
        AnsiConsole.WriteLine();

        var newDescription = AnsiConsole.Ask<string>("[yellow]Enter new description (leave empty to remove):[/]", currentDescription ?? "");
        
        if (newDescription != todo.Description)
        {
            changes["Description"] = string.IsNullOrWhiteSpace(newDescription) ? null : newDescription;
            
            var message = string.IsNullOrWhiteSpace(newDescription) 
                ? "[green]✓ Description removed[/]"
                : $"[green]✓ Description updated[/]";
                
            var successPanel = new Panel(message)
            {
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Green),
                Padding = new Padding(1)
            };
            AnsiConsole.Write(successPanel);
        }
        else if (changes.ContainsKey("Description"))
        {
            changes.Remove("Description");
            AnsiConsole.MarkupLine("[yellow]Description reverted to original value.[/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
        Console.ReadKey();
    }

    private async Task EditStatusAsync(TodoItem todo, Dictionary<string, object> changes)
    {
        Console.Clear();
        
        var rule = new Rule("[bold blue]📊 Change Status[/]")
        {
            Style = Style.Parse("blue"),
            Justification = Justify.Center
        };
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();

        var currentStatus = changes.ContainsKey("Status") 
            ? (Timekeeper.Domain.Enums.TaskStatus)changes["Status"] 
            : todo.Status;
            
        var statusChoices = Enum.GetValues<Timekeeper.Domain.Enums.TaskStatus>()
            .Select(status => $"{GetStatusIcon(status)} {status}")
            .ToList();
        statusChoices.Add("⬅️ Cancel");

        var selectedStatus = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[bold blue]Current Status:[/] {GetStatusIcon(currentStatus)} {currentStatus}\n[yellow]Select new status:[/]")
                .PageSize(10)
                .HighlightStyle(new Style(Color.Blue))
                .AddChoices(statusChoices));

        if (selectedStatus != "⬅️ Cancel")
        {
            var statusName = selectedStatus.Split(' ', 2)[1]; // Remove icon
            var newStatus = Enum.Parse<Timekeeper.Domain.Enums.TaskStatus>(statusName);
            
            if (newStatus != todo.Status)
            {
                changes["Status"] = newStatus;
                
                var successPanel = new Panel($"[green]✓ Status changed to: {GetStatusIcon(newStatus)} {newStatus}[/]")
                {
                    Border = BoxBorder.Rounded,
                    BorderStyle = new Style(Color.Green),
                    Padding = new Padding(1)
                };
                AnsiConsole.Write(successPanel);
                
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
                Console.ReadKey();
            }
        }
    }

    private async Task EditPriorityAsync(TodoItem todo, Dictionary<string, object> changes)
    {
        Console.Clear();
        
        var rule = new Rule("[bold blue]⭐ Change Priority[/]")
        {
            Style = Style.Parse("blue"),
            Justification = Justify.Center
        };
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();

        var currentPriority = changes.ContainsKey("Priority") 
            ? (Priority)changes["Priority"] 
            : todo.Priority;
            
        var priorityChoices = Enum.GetValues<Priority>()
            .Select(priority => $"{GetPriorityIcon(priority)} {priority}")
            .ToList();
        priorityChoices.Add("⬅️ Cancel");

        var selectedPriority = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[bold blue]Current Priority:[/] {GetPriorityIcon(currentPriority)} {currentPriority}\n[yellow]Select new priority:[/]")
                .PageSize(10)
                .HighlightStyle(new Style(Color.Blue))
                .AddChoices(priorityChoices));

        if (selectedPriority != "⬅️ Cancel")
        {
            var priorityName = selectedPriority.Split(' ', 2)[1]; // Remove icon
            var newPriority = Enum.Parse<Priority>(priorityName);
            
            if (newPriority != todo.Priority)
            {
                changes["Priority"] = newPriority;
                
                var successPanel = new Panel($"[green]✓ Priority changed to: {GetPriorityIcon(newPriority)} {newPriority}[/]")
                {
                    Border = BoxBorder.Rounded,
                    BorderStyle = new Style(Color.Green),
                    Padding = new Padding(1)
                };
                AnsiConsole.Write(successPanel);
                
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
                Console.ReadKey();
            }
        }
    }

    private async Task EditMultipleFieldsAsync(TodoItem todo, Dictionary<string, object> changes)
    {
        Console.Clear();
        
        var rule = new Rule("[bold blue]📋 Edit Multiple Fields[/]")
        {
            Style = Style.Parse("blue"),
            Justification = Justify.Center
        };
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();

        var fieldChoices = new[]
        {
            "📝 Title",
            "📄 Description", 
            "📊 Status",
            "⭐ Priority",
            "📅 Due Date",
            "📂 Category",
            "🏷️ Tags",
            "⏱️ Time Estimates"
        };

        var selectedFields = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("[yellow]Select fields to edit:[/]")
                .PageSize(10)
                .HighlightStyle(new Style(Color.Blue))
                .InstructionsText("[grey](Use [blue]<space>[/] to select, [green]<enter>[/] to confirm)[/]")
                .AddChoices(fieldChoices));

        foreach (var field in selectedFields)
        {
            switch (field)
            {
                case "📝 Title":
                    await EditTitleAsync(todo, changes);
                    break;
                case "📄 Description":
                    await EditDescriptionAsync(todo, changes);
                    break;
                case "📊 Status":
                    await EditStatusAsync(todo, changes);
                    break;
                case "⭐ Priority":
                    await EditPriorityAsync(todo, changes);
                    break;
                case "📅 Due Date":
                    await EditDueDateAsync(todo, changes);
                    break;
                case "📂 Category":
                    await EditCategoryAsync(todo, changes);
                    break;
                case "🏷️ Tags":
                    await EditTagsAsync(todo, changes);
                    break;
                case "⏱️ Time Estimates":
                    await EditTimeEstimatesAsync(todo, changes);
                    break;
            }
        }
    }

    private async Task<bool> SaveChangesAsync(TodoItem todo, Dictionary<string, object> changes)
    {
        Console.Clear();
        
        var rule = new Rule("[bold green]💾 Save Changes[/]")
        {
            Style = Style.Parse("green"),
            Justification = Justify.Center
        };
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();

        // Show summary of changes
        await ShowChangePreviewAsync(todo, changes);
        
        var confirm = AnsiConsole.Confirm("[yellow]Save these changes?[/]");
        
        if (!confirm)
        {
            AnsiConsole.MarkupLine("[yellow]Changes not saved.[/]");
            AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
            Console.ReadKey();
            return false;
        }

        try
        {
            var command = new UpdateTodoItemCommand(
                Id: todo.Id,
                Title: changes.ContainsKey("Title") ? changes["Title"].ToString()! : todo.Title,
                Description: changes.ContainsKey("Description") ? changes["Description"]?.ToString() : todo.Description,
                Status: changes.ContainsKey("Status") ? (Timekeeper.Domain.Enums.TaskStatus)changes["Status"] : todo.Status,
                Priority: changes.ContainsKey("Priority") ? (Priority)changes["Priority"] : todo.Priority,
                DueDate: changes.ContainsKey("DueDate") ? (DateTime?)changes["DueDate"] : todo.DueDate,
                Category: changes.ContainsKey("Category") ? changes["Category"]?.ToString() : todo.Category,
                Tags: changes.ContainsKey("Tags") ? changes["Tags"]?.ToString() : todo.Tags,
                EstimatedTimeMinutes: changes.ContainsKey("EstimatedTimeMinutes") ? (int?)changes["EstimatedTimeMinutes"] : todo.EstimatedTimeMinutes,
                EstimatedHours: changes.ContainsKey("EstimatedHours") ? (int?)changes["EstimatedHours"] : todo.EstimatedHours,
                DevOpsWorkItemId: changes.ContainsKey("DevOpsWorkItemId") ? changes["DevOpsWorkItemId"]?.ToString() : todo.DevOpsWorkItemId,
                DevOpsUrl: changes.ContainsKey("DevOpsUrl") ? changes["DevOpsUrl"]?.ToString() : todo.DevOpsUrl);

            var success = await _mediator.Send(command);

            if (success)
            {
                var successPanel = new Panel("[green]✅ Todo updated successfully![/]")
                {
                    Border = BoxBorder.Rounded,
                    BorderStyle = new Style(Color.Green),
                    Header = new PanelHeader(" Success "),
                    Padding = new Padding(1)
                };
                AnsiConsole.Write(successPanel);
            }
            else
            {
                var errorPanel = new Panel("[red]❌ Failed to update todo![/]")
                {
                    Border = BoxBorder.Rounded,
                    BorderStyle = new Style(Color.Red),
                    Header = new PanelHeader(" Error "),
                    Padding = new Padding(1)
                };
                AnsiConsole.Write(errorPanel);
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
            Console.ReadKey();
            return success;
        }
        catch (Exception ex)
        {
            var errorPanel = new Panel($"[red]❌ Error saving changes: {ex.Message.EscapeMarkup()}[/]")
            {
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Red),
                Header = new PanelHeader(" Error "),
                Padding = new Padding(1)
            };
            AnsiConsole.Write(errorPanel);
            
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
            Console.ReadKey();
            return false;
        }
    }

    // Placeholder methods for the remaining edit functions
    private async Task EditDueDateAsync(TodoItem todo, Dictionary<string, object> changes) 
    {
        // Implementation similar to other edit methods
        await Task.CompletedTask;
    }
    
    private async Task EditCategoryAsync(TodoItem todo, Dictionary<string, object> changes) 
    {
        // Implementation similar to other edit methods
        await Task.CompletedTask;
    }
    
    private async Task EditTagsAsync(TodoItem todo, Dictionary<string, object> changes) 
    {
        // Implementation similar to other edit methods
        await Task.CompletedTask;
    }
    
    private async Task EditTimeEstimatesAsync(TodoItem todo, Dictionary<string, object> changes) 
    {
        // Implementation similar to other edit methods
        await Task.CompletedTask;
    }
    
    private async Task EditProviderIntegrationAsync(TodoItem todo, Dictionary<string, object> changes) 
    {
        // Implementation similar to other edit methods
        await Task.CompletedTask;
    }
    
    private async Task ShowChangePreviewAsync(TodoItem todo, Dictionary<string, object> changes)
    {
        // Implementation to show preview of changes
        await Task.CompletedTask;
    }
    
    private void ShowNoChangesMessage()
    {
        AnsiConsole.MarkupLine("[yellow]No changes to save.[/]");
        AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
        Console.ReadKey();
    }
    
    private bool ConfirmDiscardChanges()
    {
        return AnsiConsole.Confirm("[red]You have unsaved changes. Discard them?[/]");
    }
}

public static class StringExtensions
{
    public static string Truncate(this string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value.Substring(0, maxLength - 3) + "...";
    }
}
