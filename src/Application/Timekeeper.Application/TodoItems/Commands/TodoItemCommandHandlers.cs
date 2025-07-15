using MediatR;
using Timekeeper.Application.TodoItems.Commands;
using Timekeeper.Domain.Entities;
using Timekeeper.Domain.Interfaces;
using TaskStatus = Timekeeper.Domain.Enums.TaskStatus;

namespace Timekeeper.Application.TodoItems.Commands;

public class CreateTodoItemCommandHandler : IRequestHandler<CreateTodoItemCommand, TodoItem>
{
    private readonly ITodoItemRepository _repository;

    public CreateTodoItemCommandHandler(ITodoItemRepository repository)
    {
        _repository = repository;
    }

    public async Task<TodoItem> Handle(CreateTodoItemCommand request, CancellationToken cancellationToken)
    {
        var todoItem = new TodoItem
        {
            Title = request.Title,
            Description = request.Description,
            Priority = request.Priority,
            DueDate = request.DueDate,
            Category = request.Category,
            Tags = request.Tags,
            EstimatedTimeMinutes = request.EstimatedTimeMinutes,
            Status = TaskStatus.Pending
        };

        return await _repository.AddAsync(todoItem, cancellationToken);
    }
}

public class UpdateTodoItemCommandHandler : IRequestHandler<UpdateTodoItemCommand, bool>
{
    private readonly ITodoItemRepository _repository;

    public UpdateTodoItemCommandHandler(ITodoItemRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(UpdateTodoItemCommand request, CancellationToken cancellationToken)
    {
        var todoItem = await _repository.GetByIdWithoutNavigationPropertiesAsync(request.Id, cancellationToken);
        if (todoItem == null)
            return false;

        if (request.Title != null) todoItem.Title = request.Title;
        if (request.Description != null) todoItem.Description = request.Description;
        if (request.Status != null) todoItem.Status = request.Status.Value;
        if (request.Priority != null) todoItem.Priority = request.Priority.Value;
        if (request.DueDate != null) todoItem.DueDate = request.DueDate;
        if (request.Category != null) todoItem.Category = request.Category;
        if (request.Tags != null) todoItem.Tags = request.Tags;
        if (request.EstimatedTimeMinutes != null) todoItem.EstimatedTimeMinutes = request.EstimatedTimeMinutes.Value;
        if (request.EstimatedHours != null) todoItem.EstimatedHours = request.EstimatedHours.Value;
        if (request.DevOpsWorkItemId != null) todoItem.DevOpsWorkItemId = request.DevOpsWorkItemId;
        if (request.DevOpsUrl != null) todoItem.DevOpsUrl = request.DevOpsUrl;

        todoItem.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(todoItem, cancellationToken);
        return true;
    }
}

public class DeleteTodoItemCommandHandler : IRequestHandler<DeleteTodoItemCommand, bool>
{
    private readonly ITodoItemRepository _repository;

    public DeleteTodoItemCommandHandler(ITodoItemRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(DeleteTodoItemCommand request, CancellationToken cancellationToken)
    {
        var todoItem = await _repository.GetByIdWithoutNavigationPropertiesAsync(request.Id, cancellationToken);
        if (todoItem == null)
            return false;

        await _repository.DeleteAsync(request.Id, cancellationToken);
        return true;
    }
}

public class CompleteTodoItemCommandHandler : IRequestHandler<CompleteTodoItemCommand, bool>
{
    private readonly ITodoItemRepository _repository;

    public CompleteTodoItemCommandHandler(ITodoItemRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(CompleteTodoItemCommand request, CancellationToken cancellationToken)
    {
        var todoItem = await _repository.GetByIdWithoutNavigationPropertiesAsync(request.Id, cancellationToken);
        if (todoItem == null)
            return false;

        todoItem.Status = TaskStatus.Completed;
        todoItem.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(todoItem, cancellationToken);
        return true;
    }
}
