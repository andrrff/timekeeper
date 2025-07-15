using MediatR;
using Spectre.Console;
using Timekeeper.Application.TimeEntries.Commands;
using Timekeeper.Application.TimeEntries.Queries;
using Timekeeper.Application.TodoItems.Queries;
using Timekeeper.Domain.Entities;
using Timekeeper.CLI.Services;

namespace Timekeeper.CLI.Services;

public class TimeTrackingService : ITimeTrackingService
{
    private readonly IMediator _mediator;

    public TimeTrackingService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task StartTimerAsync(int todoId, string? description = null)
    {
        // Convert int to Guid - assuming this is an ID lookup
        var todos = await _mediator.Send(new GetAllTodoItemsQuery());
        var todo = todos.Skip(todoId - 1).FirstOrDefault();
        
        if (todo == null)
        {
            AnsiConsole.MarkupLine("[red]Todo not found[/]");
            return;
        }

        var command = new CreateTimeEntryCommand(
            TodoItemId: todo.Id,
            Description: description,
            StartTime: DateTime.Now
        );

        await _mediator.Send(command);
        AnsiConsole.MarkupLine($"[green]Timer started for: {todo.Title}[/]");
    }

    public async Task StopTimerAsync(int? entryId = null)
    {
        // Get active timers
        var activeEntries = await _mediator.Send(new GetAllActiveTimeEntriesQuery());
        
        if (!activeEntries.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No active timers found[/]");
            return;
        }

        TimeEntry? entryToStop = null;
        if (entryId.HasValue && entryId.Value > 0 && entryId.Value <= activeEntries.Count)
        {
            entryToStop = activeEntries.Skip(entryId.Value - 1).First();
        }
        else
        {
            entryToStop = activeEntries.First();
        }

        var command = new UpdateTimeEntryCommand(
            Id: entryToStop.Id,
            TodoItemId: entryToStop.TodoItemId,
            StartTime: entryToStop.StartTime,
            EndTime: DateTime.Now,
            DurationMinutes: (int)(DateTime.Now - entryToStop.StartTime).TotalMinutes,
            Description: entryToStop.Description,
            IsActive: false
        );

        await _mediator.Send(command);
        AnsiConsole.MarkupLine("[green]Timer stopped[/]");
    }

    public async Task AddManualTimeEntryAsync(int todoId, DateTime start, DateTime end, string? description = null)
    {
        var todos = await _mediator.Send(new GetAllTodoItemsQuery());
        var todo = todos.Skip(todoId - 1).FirstOrDefault();
        
        if (todo == null)
        {
            AnsiConsole.MarkupLine("[red]Todo not found[/]");
            return;
        }

        var command = new CreateTimeEntryCommand(
            TodoItemId: todo.Id,
            Description: description,
            StartTime: start,
            EndTime: end
        );

        await _mediator.Send(command);
        AnsiConsole.MarkupLine("[green]Manual time entry added[/]");
    }

    public async Task ListTimeEntriesAsync(DateTime? date = null, int days = 7)
    {
        var startDate = date ?? DateTime.Today.AddDays(-days);
        var endDate = startDate.AddDays(days);
        
        var entries = await _mediator.Send(new GetTimeEntriesInDateRangeQuery(startDate, endDate));
        var todos = await _mediator.Send(new GetAllTodoItemsQuery());

        var table = new Table();
        table.AddColumn("Todo");
        table.AddColumn("Description");
        table.AddColumn("Start");
        table.AddColumn("End");
        table.AddColumn("Duration");

        foreach (var entry in entries.OrderByDescending(e => e.StartTime))
        {
            var todo = todos.FirstOrDefault(t => t.Id == entry.TodoItemId);
            var duration = entry.EndTime.HasValue 
                ? (entry.EndTime.Value - entry.StartTime).ToString(@"hh\:mm\:ss")
                : "Running";

            table.AddRow(
                todo?.Title ?? "Unknown",
                entry.Description ?? "-",
                entry.StartTime.ToString("MMM dd HH:mm"),
                entry.EndTime?.ToString("HH:mm") ?? "Running",
                duration
            );
        }

        AnsiConsole.Write(table);
    }

    public async Task ShowActiveTimersAsync()
    {
        var activeEntries = await _mediator.Send(new GetAllActiveTimeEntriesQuery());
        var todos = await _mediator.Send(new GetAllTodoItemsQuery());

        if (!activeEntries.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No active timers[/]");
            return;
        }

        var table = new Table();
        table.AddColumn("Todo");
        table.AddColumn("Description");
        table.AddColumn("Started");
        table.AddColumn("Elapsed");

        foreach (var entry in activeEntries)
        {
            var todo = todos.FirstOrDefault(t => t.Id == entry.TodoItemId);
            var elapsed = DateTime.Now - entry.StartTime;

            table.AddRow(
                todo?.Title ?? "Unknown",
                entry.Description ?? "-",
                entry.StartTime.ToString("HH:mm"),
                elapsed.ToString(@"hh\:mm\:ss")
            );
        }

        AnsiConsole.Write(table);
    }

    // Additional methods for TimeTrackingUI
    public async Task<bool> IsTimerActiveAsync(Guid todoId)
    {
        var activeEntries = await _mediator.Send(new GetAllActiveTimeEntriesQuery());
        return activeEntries.Any(e => e.TodoItemId == todoId);
    }

    public async Task<TimeSpan> GetElapsedTimeAsync(Guid todoId)
    {
        var activeEntries = await _mediator.Send(new GetAllActiveTimeEntriesQuery());
        var entry = activeEntries.FirstOrDefault(e => e.TodoItemId == todoId);
        
        if (entry == null)
            return TimeSpan.Zero;
            
        return DateTime.Now - entry.StartTime;
    }

    public async Task<IEnumerable<TimeEntry>> GetAllActiveTimersAsync()
    {
        return await _mediator.Send(new GetAllActiveTimeEntriesQuery());
    }

    public async Task<TimeSpan> GetTotalTimeSpentAsync(Guid todoId)
    {
        var entries = await _mediator.Send(new GetTimeEntriesByTodoItemQuery(todoId));
        var completedEntries = entries.Where(e => e.EndTime.HasValue);
        
        return completedEntries.Aggregate(
            TimeSpan.Zero,
            (total, entry) => total + (entry.EndTime!.Value - entry.StartTime)
        );
    }

    public async Task<TimeSpan> GetRemainingTimeAsync(Guid todoId)
    {
        var todos = await _mediator.Send(new GetAllTodoItemsQuery());
        var todo = todos.FirstOrDefault(t => t.Id == todoId);
        
        if (todo == null || todo.EstimatedTimeMinutes <= 0)
            return TimeSpan.Zero;
            
        var totalSpent = await GetTotalTimeSpentAsync(todoId);
        var estimated = TimeSpan.FromMinutes(todo.EstimatedTimeMinutes);
        
        return estimated - totalSpent;
    }

    public async Task UpdateManualTimeAsync(Guid todoId, DateTime start, DateTime end)
    {
        var entries = await _mediator.Send(new GetTimeEntriesByTodoItemQuery(todoId));
        var entry = entries.FirstOrDefault();
        
        if (entry == null)
        {
            // Create new entry
            var command = new CreateTimeEntryCommand(
                TodoItemId: todoId,
                StartTime: start,
                EndTime: end
            );
            await _mediator.Send(command);
        }
        else
        {
            // Update existing entry
            var command = new UpdateTimeEntryCommand(
                Id: entry.Id,
                TodoItemId: entry.TodoItemId,
                StartTime: start,
                EndTime: end,
                DurationMinutes: (int)(end - start).TotalMinutes,
                Description: entry.Description,
                IsActive: entry.IsActive
            );
            await _mediator.Send(command);
        }
    }
}
