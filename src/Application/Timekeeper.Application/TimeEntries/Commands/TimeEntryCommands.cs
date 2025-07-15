using MediatR;

namespace Timekeeper.Application.TimeEntries.Commands;

public record CreateTimeEntryCommand(
    Guid TodoItemId,
    DateTime StartTime,
    DateTime? EndTime = null,
    int DurationMinutes = 0,
    string? Description = null,
    bool IsActive = true
) : IRequest<Guid?>;

public record UpdateTimeEntryCommand(
    Guid Id,
    Guid TodoItemId,
    DateTime StartTime,
    DateTime? EndTime,
    int DurationMinutes,
    string? Description,
    bool IsActive
) : IRequest<bool>;

public record DeleteTimeEntryCommand(Guid Id) : IRequest<bool>;
