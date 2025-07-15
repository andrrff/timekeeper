using MediatR;
using Timekeeper.Domain.Entities;
using Timekeeper.Domain.Enums;
using TaskStatus = Timekeeper.Domain.Enums.TaskStatus;

namespace Timekeeper.Application.TodoItems.Queries;

public record GetTodoItemByIdQuery(Guid Id) : IRequest<TodoItem?>;

public record GetAllTodoItemsQuery() : IRequest<IEnumerable<TodoItem>>;

public record GetTodoItemsByStatusQuery(TaskStatus Status) : IRequest<IEnumerable<TodoItem>>;

public record GetTodoItemsByPriorityQuery(Priority Priority) : IRequest<IEnumerable<TodoItem>>;

public record SearchTodoItemsQuery(string SearchTerm) : IRequest<IEnumerable<TodoItem>>;

public record GetTodoItemsByDueDateQuery(DateTime StartDate, DateTime EndDate) : IRequest<IEnumerable<TodoItem>>;

public record GetTodoItemsByCategoryQuery(string Category) : IRequest<IEnumerable<TodoItem>>;
