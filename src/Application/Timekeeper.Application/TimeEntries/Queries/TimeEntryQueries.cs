using MediatR;
using Timekeeper.Domain.Entities;

namespace Timekeeper.Application.TimeEntries.Queries;

public record GetTimeEntryByIdQuery(Guid Id) : IRequest<TimeEntry?>;

public record GetTimeEntriesByTodoItemQuery(Guid TodoItemId) : IRequest<List<TimeEntry>>;

public record GetActiveTimeEntryQuery(Guid TodoItemId) : IRequest<TimeEntry?>;

public record GetAllActiveTimeEntriesQuery() : IRequest<List<TimeEntry>>;

public record GetTimeEntriesInDateRangeQuery(
    DateTime StartDate,
    DateTime EndDate,
    Guid? TodoItemId = null
) : IRequest<List<TimeEntry>>;
