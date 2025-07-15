using MediatR;
using Timekeeper.Application.TimeEntries.Commands;
using Timekeeper.Application.TimeEntries.Queries;
using Timekeeper.Domain.Entities;
using Timekeeper.Domain.Interfaces;

namespace Timekeeper.Application.TimeEntries.Handlers;

public class CreateTimeEntryCommandHandler : IRequestHandler<CreateTimeEntryCommand, Guid?>
{
    private readonly ITimeEntryRepository _timeEntryRepository;

    public CreateTimeEntryCommandHandler(ITimeEntryRepository timeEntryRepository)
    {
        _timeEntryRepository = timeEntryRepository;
    }

    public async Task<Guid?> Handle(CreateTimeEntryCommand request, CancellationToken cancellationToken)
    {
        var timeEntry = new TimeEntry
        {
            Id = Guid.NewGuid(),
            TodoItemId = request.TodoItemId,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            DurationMinutes = request.DurationMinutes,
            Description = request.Description,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createdTimeEntry = await _timeEntryRepository.CreateAsync(timeEntry, cancellationToken);
        return createdTimeEntry?.Id;
    }
}

public class UpdateTimeEntryCommandHandler : IRequestHandler<UpdateTimeEntryCommand, bool>
{
    private readonly ITimeEntryRepository _timeEntryRepository;

    public UpdateTimeEntryCommandHandler(ITimeEntryRepository timeEntryRepository)
    {
        _timeEntryRepository = timeEntryRepository;
    }

    public async Task<bool> Handle(UpdateTimeEntryCommand request, CancellationToken cancellationToken)
    {
        var timeEntry = await _timeEntryRepository.GetByIdAsync(request.Id, cancellationToken);
        if (timeEntry == null)
        {
            return false;
        }

        timeEntry.TodoItemId = request.TodoItemId;
        timeEntry.StartTime = request.StartTime;
        timeEntry.EndTime = request.EndTime;
        timeEntry.DurationMinutes = request.DurationMinutes;
        timeEntry.Description = request.Description;
        timeEntry.IsActive = request.IsActive;
        timeEntry.UpdatedAt = DateTime.UtcNow;

        await _timeEntryRepository.UpdateAsync(timeEntry, cancellationToken);
        return true;
    }
}

public class DeleteTimeEntryCommandHandler : IRequestHandler<DeleteTimeEntryCommand, bool>
{
    private readonly ITimeEntryRepository _timeEntryRepository;

    public DeleteTimeEntryCommandHandler(ITimeEntryRepository timeEntryRepository)
    {
        _timeEntryRepository = timeEntryRepository;
    }

    public async Task<bool> Handle(DeleteTimeEntryCommand request, CancellationToken cancellationToken)
    {
        return await _timeEntryRepository.DeleteAsync(request.Id, cancellationToken);
    }
}

public class GetTimeEntryByIdQueryHandler : IRequestHandler<GetTimeEntryByIdQuery, TimeEntry?>
{
    private readonly ITimeEntryRepository _timeEntryRepository;

    public GetTimeEntryByIdQueryHandler(ITimeEntryRepository timeEntryRepository)
    {
        _timeEntryRepository = timeEntryRepository;
    }

    public async Task<TimeEntry?> Handle(GetTimeEntryByIdQuery request, CancellationToken cancellationToken)
    {
        return await _timeEntryRepository.GetByIdAsync(request.Id, cancellationToken);
    }
}

public class GetTimeEntriesByTodoItemQueryHandler : IRequestHandler<GetTimeEntriesByTodoItemQuery, List<TimeEntry>>
{
    private readonly ITimeEntryRepository _timeEntryRepository;

    public GetTimeEntriesByTodoItemQueryHandler(ITimeEntryRepository timeEntryRepository)
    {
        _timeEntryRepository = timeEntryRepository;
    }

    public async Task<List<TimeEntry>> Handle(GetTimeEntriesByTodoItemQuery request, CancellationToken cancellationToken)
    {
        return await _timeEntryRepository.GetByTodoItemIdAsync(request.TodoItemId, cancellationToken);
    }
}

public class GetActiveTimeEntryQueryHandler : IRequestHandler<GetActiveTimeEntryQuery, TimeEntry?>
{
    private readonly ITimeEntryRepository _timeEntryRepository;

    public GetActiveTimeEntryQueryHandler(ITimeEntryRepository timeEntryRepository)
    {
        _timeEntryRepository = timeEntryRepository;
    }

    public async Task<TimeEntry?> Handle(GetActiveTimeEntryQuery request, CancellationToken cancellationToken)
    {
        var timeEntries = await _timeEntryRepository.GetByTodoItemIdAsync(request.TodoItemId, cancellationToken);
        return timeEntries.FirstOrDefault(te => te.IsActive);
    }
}

public class GetAllActiveTimeEntriesQueryHandler : IRequestHandler<GetAllActiveTimeEntriesQuery, List<TimeEntry>>
{
    private readonly ITimeEntryRepository _timeEntryRepository;

    public GetAllActiveTimeEntriesQueryHandler(ITimeEntryRepository timeEntryRepository)
    {
        _timeEntryRepository = timeEntryRepository;
    }

    public async Task<List<TimeEntry>> Handle(GetAllActiveTimeEntriesQuery request, CancellationToken cancellationToken)
    {
        return await _timeEntryRepository.GetActiveTimeEntriesAsync(cancellationToken);
    }
}

public class GetTimeEntriesInDateRangeQueryHandler : IRequestHandler<GetTimeEntriesInDateRangeQuery, List<TimeEntry>>
{
    private readonly ITimeEntryRepository _timeEntryRepository;

    public GetTimeEntriesInDateRangeQueryHandler(ITimeEntryRepository timeEntryRepository)
    {
        _timeEntryRepository = timeEntryRepository;
    }

    public async Task<List<TimeEntry>> Handle(GetTimeEntriesInDateRangeQuery request, CancellationToken cancellationToken)
    {
        return await _timeEntryRepository.GetTimeEntriesInDateRangeAsync(
            request.StartDate, 
            request.EndDate, 
            request.TodoItemId, 
            cancellationToken);
    }
}
