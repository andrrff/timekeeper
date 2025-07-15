using MediatR;
using Timekeeper.Domain.Entities;
using Timekeeper.Domain.Enums;
using TaskStatus = Timekeeper.Domain.Enums.TaskStatus;

namespace Timekeeper.Application.TodoItems.Commands;

public record CreateTodoItemCommand(
    string Title,
    string? Description = null,
    Priority Priority = Priority.Medium,
    DateTime? DueDate = null,
    string? Category = null,
    string? Tags = null,
    int EstimatedTimeMinutes = 0
) : IRequest<TodoItem>;

public record UpdateTodoItemCommand(
    Guid Id,
    string? Title = null,
    string? Description = null,
    TaskStatus? Status = null,
    Priority? Priority = null,
    DateTime? DueDate = null,
    string? Category = null,
    string? Tags = null,
    int? EstimatedTimeMinutes = null,
    int? EstimatedHours = null,
    string? DevOpsWorkItemId = null,
    string? DevOpsUrl = null
) : IRequest<bool>;

public record DeleteTodoItemCommand(Guid Id) : IRequest<bool>;

public record CompleteTodoItemCommand(Guid Id) : IRequest<bool>;
