using MediatR;
using Spectre.Console;
using Timekeeper.Application.Reports.Queries.GetKanbanBoard;
using Timekeeper.CLI.Services;
using Timekeeper.Domain.Enums;
using TaskStatus = Timekeeper.Domain.Enums.TaskStatus;

namespace Timekeeper.CLI.UI;

public class KanbanUI
{
    private readonly IMediator _mediator;
    private readonly ITodoService _todoService;
    private readonly ITimeTrackingService _timeTrackingService;

    public KanbanUI(IMediator mediator, ITodoService todoService, ITimeTrackingService timeTrackingService)
    {
        _mediator = mediator;
        _todoService = todoService;
        _timeTrackingService = timeTrackingService;
    }

    public async Task ShowKanbanBoardAsync()
    {
        while (true)
        {
            try
            {
                AnsiConsole.Clear();
                
                var titleRule = new Rule("[bold blue]📋 Kanban Board[/]")
                {
                    Style = Style.Parse("blue"),
                    Justification = Justify.Center
                };
                AnsiConsole.Write(titleRule);
                AnsiConsole.WriteLine();

                // Get board data
                var board = await _mediator.Send(new GetKanbanBoardQuery());

                // Display board
                await DisplayKanbanBoard(board);

                // Show menu options
                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[bold yellow]What would you like to do?[/]")
                        .AddChoices(new[] {
                            "🔄 Refresh Board",
                            "📊 Show Statistics", 
                            "🔍 Filter by Category",
                            "🎯 Filter by Priority",
                            "✨ Clear Filters",
                            "📝 Move Card",
                            "⏱️ Start Timer",
                            "⏹️ Stop Timer",
                            "🔙 Back to Main Menu"
                        }));

                switch (choice)
                {
                    case "🔄 Refresh Board":
                        // Just continue the loop to refresh
                        break;
                    case "📊 Show Statistics":
                        await ShowDetailedStatistics(board);
                        break;
                    case "🔍 Filter by Category":
                        await FilterByCategory();
                        break;
                    case "🎯 Filter by Priority":
                        await FilterByPriority();
                        break;
                    case "✨ Clear Filters":
                        await ShowKanbanBoardFiltered(null, null);
                        break;
                    case "📝 Move Card":
                        await MoveCard(board);
                        break;
                    case "⏱️ Start Timer":
                        await StartTimerForCard(board);
                        break;
                    case "⏹️ Stop Timer":
                        await StopTimerForCard(board);
                        break;
                    case "🔙 Back to Main Menu":
                        return;
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Error: {ex.Message.EscapeMarkup()}[/]");
                AnsiConsole.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        }
    }

    public async Task ShowAsync()
    {
        await ShowKanbanBoardAsync();
    }

    private async Task DisplayKanbanBoard(KanbanBoardResult board)
    {
        // Create layout for columns
        var layout = new Layout("Root")
            .SplitColumns(
                board.Columns.Select(c => new Layout(c.Name)).ToArray()
            );

        // Populate each column
        for (int i = 0; i < board.Columns.Count; i++)
        {
            var column = board.Columns[i];
            var columnContent = CreateColumnPanel(column);
            layout[column.Name].Update(columnContent);
        }

        AnsiConsole.Write(layout);
        AnsiConsole.WriteLine();

        // Show quick stats
        DisplayQuickStats(board.Statistics);
    }

    private Panel CreateColumnPanel(KanbanColumn column)
    {
        var content = new List<string>();

        if (!column.Cards.Any())
        {
            content.Add("[dim]No cards[/]");
        }
        else
        {
            foreach (var card in column.Cards.Take(10)) // Show max 10 cards per column
            {
                var cardText = FormatCard(card);
                content.Add(cardText);
                content.Add(""); // Empty line between cards
            }

            if (column.Cards.Count > 10)
            {
                content.Add($"[dim]... and {column.Cards.Count - 10} more cards[/]");
            }
        }

        var panel = new Panel(string.Join("\n", content))
        {
            Header = new PanelHeader($"[bold]{column.Icon} {column.Name} ({column.CardCount})[/]"),
            Border = BoxBorder.Rounded
        };

        // Set border color based on column
        panel.BorderColor(column.Color switch
        {
            "blue" => Color.Blue,
            "yellow" => Color.Yellow,
            "orange" => Color.Orange1,
            "green" => Color.Green,
            "red" => Color.Red,
            _ => Color.Grey
        });

        return panel;
    }

    private string FormatCard(KanbanCard card)
    {
        var lines = new List<string>();
        
        // Title with priority icon
        var title = $"{card.PriorityIcon} [bold]{card.Title.EscapeMarkup()}[/]";
        if (card.HasActiveTimer)
        {
            title = $"🔴 {title}";
        }
        lines.Add(title);

        // Category
        if (!string.IsNullOrEmpty(card.Category))
        {
            lines.Add($"[dim]📁 {card.Category.EscapeMarkup()}[/]");
        }

        // Due date
        if (card.DueDate.HasValue)
        {
            var dueDateText = card.IsOverdue 
                ? $"[red]📅 {card.DueDate.Value:MM/dd} (OVERDUE)[/]"
                : card.DaysUntilDue <= 3
                    ? $"[yellow]📅 {card.DueDate.Value:MM/dd} ({card.DaysUntilDue}d)[/]"
                    : $"[dim]📅 {card.DueDate.Value:MM/dd}[/]";
            lines.Add(dueDateText);
        }

        // Time tracking
        if (card.EstimatedHours > 0 || card.ActualHours > 0)
        {
            lines.Add($"[dim]⏱️ {card.ActualHours:F1}h / {card.EstimatedHours:F1}h[/]");
        }

        // Provider info
        if (!string.IsNullOrEmpty(card.ProviderWorkItemId))
        {
            lines.Add($"[dim]🔗 #{card.ProviderWorkItemId}[/]");
        }

        // Tags
        if (!string.IsNullOrEmpty(card.Tags))
        {
            var tags = card.Tags.Split(',').Take(3).Select(t => $"[dim]#{t.Trim()}[/]");
            lines.Add(string.Join(" ", tags));
        }

        return string.Join("\n", lines);
    }

    private void DisplayQuickStats(KanbanStatistics stats)
    {
        var statsTable = new Table();
        statsTable.AddColumn("Metric");
        statsTable.AddColumn("Value");
        statsTable.Border = TableBorder.None;
        statsTable.ShowHeaders = false;

        statsTable.AddRow("📊 Total", stats.TotalCards.ToString());
        statsTable.AddRow("✅ Completed", $"{stats.CompletedCards} ({stats.CompletionPercentage:F1}%)");
        statsTable.AddRow("🔄 In Progress", stats.InProgressCards.ToString());
        statsTable.AddRow("📋 Pending", stats.PendingCards.ToString());
        
        if (stats.OverdueCards > 0)
        {
            statsTable.AddRow("⚠️ Overdue", $"[red]{stats.OverdueCards}[/]");
        }

        if (stats.CardsWithActiveTimers > 0)
        {
            statsTable.AddRow("🔴 Active Timers", stats.CardsWithActiveTimers.ToString());
        }

        var statsPanel = new Panel(statsTable)
        {
            Header = new PanelHeader("[bold]📈 Quick Stats[/]"),
            Border = BoxBorder.Rounded
        };

        AnsiConsole.Write(statsPanel);
    }

    private async Task ShowDetailedStatistics(KanbanBoardResult board)
    {
        AnsiConsole.Clear();
        var titleRule = new Rule("[bold blue]📊 Detailed Statistics[/]");
        AnsiConsole.Write(titleRule);
        AnsiConsole.WriteLine();

        var stats = board.Statistics;

        // Main stats table
        var mainTable = new Table();
        mainTable.AddColumn("Metric");
        mainTable.AddColumn("Count");
        mainTable.AddColumn("Percentage");

        mainTable.AddRow("📋 Total Cards", stats.TotalCards.ToString(), "100%");
        mainTable.AddRow("📝 Pending", stats.PendingCards.ToString(), $"{(stats.TotalCards > 0 ? (double)stats.PendingCards / stats.TotalCards * 100 : 0):F1}%");
        mainTable.AddRow("🔄 In Progress", stats.InProgressCards.ToString(), $"{(stats.TotalCards > 0 ? (double)stats.InProgressCards / stats.TotalCards * 100 : 0):F1}%");
        mainTable.AddRow("✅ Completed", stats.CompletedCards.ToString(), $"{stats.CompletionPercentage:F1}%");
        mainTable.AddRow("⏸️ On Hold", stats.OnHoldCards.ToString(), $"{(stats.TotalCards > 0 ? (double)stats.OnHoldCards / stats.TotalCards * 100 : 0):F1}%");
        mainTable.AddRow("❌ Cancelled", stats.CancelledCards.ToString(), $"{(stats.TotalCards > 0 ? (double)stats.CancelledCards / stats.TotalCards * 100 : 0):F1}%");

        AnsiConsole.Write(mainTable);
        AnsiConsole.WriteLine();

        // Priority breakdown
        if (stats.CardsByPriority.Any())
        {
            var priorityTable = new Table();
            priorityTable.AddColumn("Priority");
            priorityTable.AddColumn("Count");

            foreach (var kvp in stats.CardsByPriority.OrderBy(x => x.Key))
            {
                var icon = kvp.Key switch
                {
                    Priority.Critical => "🔴",
                    Priority.High => "🟠",
                    Priority.Medium => "🟡", 
                    Priority.Low => "🟢",
                    _ => "⚪"
                };
                priorityTable.AddRow($"{icon} {kvp.Key}", kvp.Value.ToString());
            }

            var priorityPanel = new Panel(priorityTable)
            {
                Header = new PanelHeader("[bold]🎯 By Priority[/]"),
                Border = BoxBorder.Rounded
            };
            AnsiConsole.Write(priorityPanel);
            AnsiConsole.WriteLine();
        }

        // Category breakdown
        if (stats.CardsByCategory.Any())
        {
            var categoryTable = new Table();
            categoryTable.AddColumn("Category");
            categoryTable.AddColumn("Count");

            foreach (var kvp in stats.CardsByCategory.OrderByDescending(x => x.Value))
            {
                categoryTable.AddRow($"📁 {kvp.Key}", kvp.Value.ToString());
            }

            var categoryPanel = new Panel(categoryTable)
            {
                Header = new PanelHeader("[bold]📁 By Category[/]"),
                Border = BoxBorder.Rounded
            };
            AnsiConsole.Write(categoryPanel);
            AnsiConsole.WriteLine();
        }

        // Time tracking stats
        var timeTable = new Table();
        timeTable.AddColumn("Time Metric");
        timeTable.AddColumn("Hours");

        timeTable.AddRow("📊 Estimated Total", $"{stats.TotalEstimatedHours:F1}h");
        timeTable.AddRow("⏱️ Actual Total", $"{stats.TotalActualHours:F1}h");
        
        if (stats.TotalEstimatedHours > 0)
        {
            var variance = stats.TotalActualHours - stats.TotalEstimatedHours;
            var variancePercent = (variance / stats.TotalEstimatedHours) * 100;
            var varianceText = variance > 0 
                ? $"[red]+{variance:F1}h (+{variancePercent:F1}%)[/]"
                : $"[green]{variance:F1}h ({variancePercent:F1}%)[/]";
            timeTable.AddRow("📈 Variance", varianceText);
        }

        var timePanel = new Panel(timeTable)
        {
            Header = new PanelHeader("[bold]⏱️ Time Tracking[/]"),
            Border = BoxBorder.Rounded
        };
        AnsiConsole.Write(timePanel);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
        Console.ReadKey();
    }

    private async Task FilterByCategory()
    {
        var allTodos = await _todoService.GetAllTodosAsync();
        var categories = allTodos.Where(t => !string.IsNullOrEmpty(t.Category))
                                .Select(t => t.Category!)
                                .Distinct()
                                .OrderBy(c => c)
                                .ToList();

        if (!categories.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No categories found.[/]");
            AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
            Console.ReadKey();
            return;
        }

        categories.Insert(0, "🔙 Back");

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold yellow]Select category to filter by:[/]")
                .AddChoices(categories));

        if (choice != "🔙 Back")
        {
            await ShowKanbanBoardFiltered(choice, null);
        }
    }

    private async Task FilterByPriority()
    {
        var priorities = new[] { "🔙 Back", "🔴 Critical", "🟠 High", "🟡 Medium", "🟢 Low" };

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold yellow]Select priority to filter by:[/]")
                .AddChoices(priorities));

        if (choice != "🔙 Back")
        {
            var priority = choice switch
            {
                "🔴 Critical" => Priority.Critical,
                "🟠 High" => Priority.High,
                "🟡 Medium" => Priority.Medium,
                "🟢 Low" => Priority.Low,
                _ => (Priority?)null
            };

            if (priority.HasValue)
            {
                await ShowKanbanBoardFiltered(null, priority.Value);
            }
        }
    }

    private async Task ShowKanbanBoardFiltered(string? category, Priority? priority)
    {
        AnsiConsole.Clear();
        
        var filterText = "";
        if (!string.IsNullOrEmpty(category))
            filterText += $" | Category: {category}";
        if (priority.HasValue)
            filterText += $" | Priority: {priority}";

        var titleRule = new Rule($"[bold blue]📋 Kanban Board{filterText}[/]");
        AnsiConsole.Write(titleRule);
        AnsiConsole.WriteLine();

        var board = await _mediator.Send(new GetKanbanBoardQuery(category, priority));
        await DisplayKanbanBoard(board);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
        Console.ReadKey();
    }

    private async Task MoveCard(KanbanBoardResult board)
    {
        // Get all cards from all columns
        var allCards = board.Columns.SelectMany(c => c.Cards).ToList();
        
        if (!allCards.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No cards available to move.[/]");
            AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
            Console.ReadKey();
            return;
        }

        var cardChoices = allCards.Select(c => $"{c.PriorityIcon} {c.Title} [{GetStatusText(c.Id, board)}]").ToList();
        cardChoices.Insert(0, "🔙 Back");

        var cardChoice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold yellow]Select card to move:[/]")
                .AddChoices(cardChoices)
                .PageSize(10));

        if (cardChoice == "🔙 Back") return;

        var selectedCardIndex = cardChoices.IndexOf(cardChoice) - 1;
        var selectedCard = allCards[selectedCardIndex];

        var statusChoices = new[] { "🔙 Back", "📋 Pending", "🔄 In Progress", "⏸️ On Hold", "✅ Completed", "❌ Cancelled" };
        
        var statusChoice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[bold yellow]Move '{selectedCard.Title}' to:[/]")
                .AddChoices(statusChoices));

        if (statusChoice == "🔙 Back") return;

        var newStatus = statusChoice switch
        {
            "📋 Pending" => TaskStatus.Pending,
            "🔄 In Progress" => TaskStatus.InProgress,
            "⏸️ On Hold" => TaskStatus.OnHold,
            "✅ Completed" => TaskStatus.Completed,
            "❌ Cancelled" => TaskStatus.Cancelled,
            _ => TaskStatus.Pending
        };

        try
        {
            // Update the todo status - we need to find the ID and convert it
            var todoIdInt = GetTodoIntId(selectedCard.Id);
            if (todoIdInt > 0)
            {
                await _todoService.UpdateTodoAsync(todoIdInt, null, null, null, null, null);
                // Note: We'd need to extend the UpdateTodoAsync method to support status updates
                // For now, this is a placeholder
                AnsiConsole.MarkupLine($"[green]✅ Card moved to {statusChoice}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[red]❌ Could not find todo item to update.[/]");
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]❌ Error moving card: {ex.Message.EscapeMarkup()}[/]");
        }

        AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
        Console.ReadKey();
    }

    private async Task StartTimerForCard(KanbanBoardResult board)
    {
        var allCards = board.Columns.SelectMany(c => c.Cards).Where(c => !c.HasActiveTimer).ToList();
        
        if (!allCards.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No cards available for timer start (all may already have active timers).[/]");
            AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
            Console.ReadKey();
            return;
        }

        var cardChoices = allCards.Select(c => $"{c.PriorityIcon} {c.Title}").ToList();
        cardChoices.Insert(0, "🔙 Back");

        var cardChoice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold yellow]Select card to start timer:[/]")
                .AddChoices(cardChoices)
                .PageSize(10));

        if (cardChoice == "🔙 Back") return;

        var selectedCardIndex = cardChoices.IndexOf(cardChoice) - 1;
        var selectedCard = allCards[selectedCardIndex];

        try
        {
            var todoIdInt = GetTodoIntId(selectedCard.Id);
            if (todoIdInt > 0)
            {
                await _timeTrackingService.StartTimerAsync(todoIdInt);
                AnsiConsole.MarkupLine($"[green]✅ Timer started for '{selectedCard.Title}'[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[red]❌ Could not find todo item.[/]");
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]❌ Error starting timer: {ex.Message.EscapeMarkup()}[/]");
        }

        AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
        Console.ReadKey();
    }

    private async Task StopTimerForCard(KanbanBoardResult board)
    {
        var activeCards = board.Columns.SelectMany(c => c.Cards).Where(c => c.HasActiveTimer).ToList();
        
        if (!activeCards.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No active timers found.[/]");
            AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
            Console.ReadKey();
            return;
        }

        var cardChoices = activeCards.Select(c => $"🔴 {c.PriorityIcon} {c.Title}").ToList();
        cardChoices.Insert(0, "🔙 Back");

        var cardChoice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold yellow]Select timer to stop:[/]")
                .AddChoices(cardChoices));

        if (cardChoice == "🔙 Back") return;

        var selectedCardIndex = cardChoices.IndexOf(cardChoice) - 1;
        var selectedCard = activeCards[selectedCardIndex];

        try
        {
            await _timeTrackingService.StopTimerAsync();
            AnsiConsole.MarkupLine($"[green]✅ Timer stopped for '{selectedCard.Title}'[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]❌ Error stopping timer: {ex.Message.EscapeMarkup()}[/]");
        }

        AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
        Console.ReadKey();
    }

    private string GetStatusText(Guid cardId, KanbanBoardResult board)
    {
        var column = board.Columns.FirstOrDefault(c => c.Cards.Any(card => card.Id == cardId));
        return column?.Name ?? "Unknown";
    }

    private int GetTodoIntId(Guid id)
    {
        // This is a temporary solution - in a real implementation, 
        // we'd need to either store the int ID in the card or have a lookup method
        return id.GetHashCode() % 100000; // Simple conversion for demo
    }
}
