using Spectre.Console;
using Timekeeper.CLI.Services;
using Timekeeper.Application.Reports.Queries.GetCalendarReport;

namespace Timekeeper.CLI.UI;

public class CalendarUI
{
    private readonly IReportService _reportService;

    public CalendarUI(IReportService reportService)
    {
        _reportService = reportService;
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
                    var mainResult = await ShowCalendarMenuAsync();
                    if (mainResult == "exit" || mainResult == "back")
                    {
                        navigationStack.Clear();
                    }
                    else if (!string.IsNullOrEmpty(mainResult))
                    {
                        navigationStack.Push(mainResult);
                    }
                    break;
                    
                case "daily":
                    await ShowDailyCalendarAsync();
                    navigationStack.Pop();
                    break;
                    
                case "weekly":
                    await ShowWeeklyCalendarAsync();
                    navigationStack.Pop();
                    break;
                    
                case "monthly":
                    await ShowMonthlyCalendarAsync();
                    navigationStack.Pop();
                    break;
                    
                default:
                    navigationStack.Pop();
                    break;
            }
        }
    }

    private async Task<string> ShowCalendarMenuAsync()
    {
        Console.Clear();
        
        var panel = new Panel("[bold blue]📅 Calendar View[/]\n\n[dim]Choose how you want to view your time activities[/]")
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Blue),
            Padding = new Padding(2, 1)
        };
        
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[green]Select calendar view:[/]")
                .PageSize(10)
                .AddChoices(new[]
                {
                    "📅 Daily View - Today's timeline",
                    "📆 Weekly View - This week's overview",
                    "🗓️ Monthly View - This month's calendar",
                    "🔙 Back to Main Menu"
                }));

        return choice switch
        {
            "📅 Daily View - Today's timeline" => "daily",
            "📆 Weekly View - This week's overview" => "weekly",
            "🗓️ Monthly View - This month's calendar" => "monthly",
            "🔙 Back to Main Menu" => "back",
            _ => "back"
        };
    }

    private async Task ShowDailyCalendarAsync()
    {
        var targetDate = DateTime.Today;
        
        // Ask user for specific date if needed
        if (AnsiConsole.Confirm("[yellow]Do you want to view a specific date?[/]", false))
        {
            var dateInput = AnsiConsole.Ask<string>("[green]Enter date (yyyy-MM-dd) or press Enter for today:[/]");
            if (!string.IsNullOrWhiteSpace(dateInput) && DateTime.TryParse(dateInput, out var parsed))
            {
                targetDate = parsed;
            }
        }

        try
        {
            await _reportService.GenerateCalendarReportAsync(targetDate, targetDate, CalendarReportType.Daily);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]❌ Error generating daily calendar: {ex.Message}[/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
        Console.ReadKey(true);
    }

    private async Task ShowWeeklyCalendarAsync()
    {
        var startOfWeek = GetStartOfWeek(DateTime.Today);
        
        // Ask user for specific week if needed
        if (AnsiConsole.Confirm("[yellow]Do you want to view a specific week?[/]", false))
        {
            var dateInput = AnsiConsole.Ask<string>("[green]Enter start of week (yyyy-MM-dd) or press Enter for this week:[/]");
            if (!string.IsNullOrWhiteSpace(dateInput) && DateTime.TryParse(dateInput, out var parsed))
            {
                startOfWeek = GetStartOfWeek(parsed);
            }
        }

        var endOfWeek = startOfWeek.AddDays(6);

        try
        {
            await _reportService.GenerateCalendarReportAsync(startOfWeek, endOfWeek, CalendarReportType.Weekly);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]❌ Error generating weekly calendar: {ex.Message}[/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
        Console.ReadKey(true);
    }

    private async Task ShowMonthlyCalendarAsync()
    {
        var targetMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        
        // Ask user for specific month if needed
        if (AnsiConsole.Confirm("[yellow]Do you want to view a specific month?[/]", false))
        {
            var dateInput = AnsiConsole.Ask<string>("[green]Enter month (yyyy-MM) or press Enter for this month:[/]");
            if (!string.IsNullOrWhiteSpace(dateInput) && DateTime.TryParse($"{dateInput}-01", out var parsed))
            {
                targetMonth = new DateTime(parsed.Year, parsed.Month, 1);
            }
        }

        var monthStart = targetMonth;
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        try
        {
            await _reportService.GenerateCalendarReportAsync(monthStart, monthEnd, CalendarReportType.Monthly);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]❌ Error generating monthly calendar: {ex.Message}[/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
        Console.ReadKey(true);
    }

    private static DateTime GetStartOfWeek(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-1 * diff).Date;
    }
}
