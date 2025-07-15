using MediatR;
using Timekeeper.Application.TodoItems.Commands;
using Timekeeper.Domain.Entities;
using Timekeeper.Domain.Enums;

namespace Timekeeper.CLI.Services;

public class CommandService : ICommandService
{
    private readonly IMediator _mediator;

    public CommandService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<TodoItem> CreateTodoAsync(string title, string? description = null, Priority priority = Priority.Medium)
    {
        var command = new CreateTodoItemCommand(title, description, priority);
        return await _mediator.Send(command);
    }

    public async Task<bool> UpdateTodoAsync(Guid id, string? title = null, string? description = null)
    {
        var command = new UpdateTodoItemCommand(id, title, description);
        return await _mediator.Send(command);
    }

    public async Task<bool> DeleteTodoAsync(Guid id)
    {
        var command = new DeleteTodoItemCommand(id);
        return await _mediator.Send(command);
    }

    public async Task<bool> CompleteTodoAsync(Guid id)
    {
        var command = new CompleteTodoItemCommand(id);
        return await _mediator.Send(command);
    }
}