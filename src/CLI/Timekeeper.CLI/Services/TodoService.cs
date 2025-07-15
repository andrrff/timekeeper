using MediatR;
using Spectre.Console;
using Timekeeper.Application.TodoItems.Commands;
using Timekeeper.Application.TodoItems.Queries;
using Timekeeper.Domain.Entities;
using Timekeeper.Domain.Enums;

namespace Timekeeper.CLI.Services;

public class TodoService : ITodoService
{
    private readonly IMediator _mediator;

    public TodoService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IEnumerable<TodoItem>> GetAllTodosAsync()
    {
        return await _mediator.Send(new GetAllTodoItemsQuery());
    }

    public async Task<TodoItem?> GetTodoByIdAsync(Guid id)
    {
        return await _mediator.Send(new GetTodoItemByIdQuery(id));
    }

    public async Task<TodoItem?> GetTodoByIdAsync(int id)
    {
        var todos = await GetAllTodosAsync();
        return todos.FirstOrDefault(t => t.Id.GetHashCode() == id);
    }

    public async Task<IEnumerable<TodoItem>> SearchTodosAsync(string searchTerm)
    {
        return await _mediator.Send(new SearchTodoItemsQuery(searchTerm));
    }

    public async Task<IEnumerable<TodoItem>> GetTodosByStatusAsync(Timekeeper.Domain.Enums.TaskStatus status)
    {
        return await _mediator.Send(new GetTodoItemsByStatusQuery(status));
    }

    public async Task<IEnumerable<TodoItem>> GetTodosByPriorityAsync(Priority priority)
    {
        return await _mediator.Send(new GetTodoItemsByPriorityQuery(priority));
    }

    // CLI specific implementations
    public async Task CreateTodoAsync(string title, string? description, Priority priority, string? category, string? tags, DateTime? dueDate)
    {
        var command = new CreateTodoItemCommand(
            Title: title,
            Description: description,
            Priority: priority,
            DueDate: dueDate,
            Category: category,
            Tags: tags
        );

        await _mediator.Send(command);
    }

    public async Task ListTodosAsync(Timekeeper.Domain.Enums.TaskStatus? status = null, string? category = null, int limit = 10)
    {
        var todos = await GetAllTodosAsync();

        if (status.HasValue)
            todos = todos.Where(t => t.Status == status.Value);

        if (!string.IsNullOrEmpty(category))
            todos = todos.Where(t => t.Category?.Equals(category, StringComparison.OrdinalIgnoreCase) == true);

        todos = todos.Take(limit);

        var table = new Table();
        table.AddColumn("ID");
        table.AddColumn("Title");
        table.AddColumn("Status");
        table.AddColumn("Priority");
        table.AddColumn("Category");
        table.AddColumn("Due Date");

        foreach (var todo in todos)
        {
            var statusColor = todo.Status switch
            {
                Timekeeper.Domain.Enums.TaskStatus.Pending => "yellow",
                Timekeeper.Domain.Enums.TaskStatus.InProgress => "blue",
                Timekeeper.Domain.Enums.TaskStatus.Completed => "green",
                _ => "white"
            };

            var priorityColor = todo.Priority switch
            {
                Priority.High => "red",
                Priority.Medium => "yellow",
                Priority.Low => "green",
                _ => "white"
            };

            table.AddRow(
                todo.Id.GetHashCode().ToString(),
                todo.Title,
                $"[{statusColor}]{todo.Status}[/]",
                $"[{priorityColor}]{todo.Priority}[/]",
                todo.Category ?? "-",
                todo.DueDate?.ToString("yyyy-MM-dd") ?? "-"
            );
        }

        AnsiConsole.Write(table);
    }

    public async Task CompleteTodoAsync(int id)
    {
        var todo = await GetTodoByIdAsync(id);
        if (todo == null)
        {
            throw new ArgumentException($"Todo with ID {id} not found");
        }

        var command = new UpdateTodoItemCommand(
            Id: todo.Id,
            Status: Timekeeper.Domain.Enums.TaskStatus.Completed
        );

        await _mediator.Send(command);
    }

    public async Task DeleteTodoAsync(int id)
    {
        var todo = await GetTodoByIdAsync(id);
        if (todo == null)
        {
            throw new ArgumentException($"Todo with ID {id} not found");
        }

        var command = new DeleteTodoItemCommand(todo.Id);
        await _mediator.Send(command);
    }

    public async Task UpdateTodoAsync(int id, string? title, string? description, Priority? priority, string? category, string? tags)
    {
        var todo = await GetTodoByIdAsync(id);
        if (todo == null)
        {
            throw new ArgumentException($"Todo with ID {id} not found");
        }

        var command = new UpdateTodoItemCommand(
            Id: todo.Id,
            Title: title,
            Description: description,
            Priority: priority,
            Category: category,
            Tags: tags
        );

        await _mediator.Send(command);
    }
}