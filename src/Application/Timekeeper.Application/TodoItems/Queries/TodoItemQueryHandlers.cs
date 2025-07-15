using MediatR;
using Timekeeper.Application.TodoItems.Queries;
using Timekeeper.Domain.Entities;
using Timekeeper.Domain.Interfaces;

namespace Timekeeper.Application.TodoItems.Queries;

public class GetTodoItemByIdQueryHandler : IRequestHandler<GetTodoItemByIdQuery, TodoItem?>
{
    private readonly ITodoItemRepository _repository;

    public GetTodoItemByIdQueryHandler(ITodoItemRepository repository)
    {
        _repository = repository;
    }

    public async Task<TodoItem?> Handle(GetTodoItemByIdQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetByIdAsync(request.Id, cancellationToken);
    }
}

public class GetAllTodoItemsQueryHandler : IRequestHandler<GetAllTodoItemsQuery, IEnumerable<TodoItem>>
{
    private readonly ITodoItemRepository _repository;

    public GetAllTodoItemsQueryHandler(ITodoItemRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<TodoItem>> Handle(GetAllTodoItemsQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetAllAsync(cancellationToken);
    }
}

public class GetTodoItemsByStatusQueryHandler : IRequestHandler<GetTodoItemsByStatusQuery, IEnumerable<TodoItem>>
{
    private readonly ITodoItemRepository _repository;

    public GetTodoItemsByStatusQueryHandler(ITodoItemRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<TodoItem>> Handle(GetTodoItemsByStatusQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetByStatusAsync(request.Status, cancellationToken);
    }
}

public class GetTodoItemsByPriorityQueryHandler : IRequestHandler<GetTodoItemsByPriorityQuery, IEnumerable<TodoItem>>
{
    private readonly ITodoItemRepository _repository;

    public GetTodoItemsByPriorityQueryHandler(ITodoItemRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<TodoItem>> Handle(GetTodoItemsByPriorityQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetByPriorityAsync(request.Priority, cancellationToken);
    }
}

public class SearchTodoItemsQueryHandler : IRequestHandler<SearchTodoItemsQuery, IEnumerable<TodoItem>>
{
    private readonly ITodoItemRepository _repository;

    public SearchTodoItemsQueryHandler(ITodoItemRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<TodoItem>> Handle(SearchTodoItemsQuery request, CancellationToken cancellationToken)
    {
        return await _repository.SearchAsync(request.SearchTerm, cancellationToken);
    }
}

public class GetTodoItemsByDueDateQueryHandler : IRequestHandler<GetTodoItemsByDueDateQuery, IEnumerable<TodoItem>>
{
    private readonly ITodoItemRepository _repository;

    public GetTodoItemsByDueDateQueryHandler(ITodoItemRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<TodoItem>> Handle(GetTodoItemsByDueDateQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetByDueDateRangeAsync(request.StartDate, request.EndDate, cancellationToken);
    }
}

public class GetTodoItemsByCategoryQueryHandler : IRequestHandler<GetTodoItemsByCategoryQuery, IEnumerable<TodoItem>>
{
    private readonly ITodoItemRepository _repository;

    public GetTodoItemsByCategoryQueryHandler(ITodoItemRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<TodoItem>> Handle(GetTodoItemsByCategoryQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetByCategoryAsync(request.Category, cancellationToken);
    }
}
