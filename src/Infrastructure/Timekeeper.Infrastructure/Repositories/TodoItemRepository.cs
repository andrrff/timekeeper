using Microsoft.EntityFrameworkCore;
using Timekeeper.Domain.Entities;
using Timekeeper.Domain.Enums;
using Timekeeper.Domain.Interfaces;
using Timekeeper.Infrastructure.Persistence;
using TaskStatus = Timekeeper.Domain.Enums.TaskStatus;

namespace Timekeeper.Infrastructure.Repositories;

public class TodoItemRepository : ITodoItemRepository
{
    private readonly TimekeeperDbContext _context;

    public TodoItemRepository(TimekeeperDbContext context)
    {
        _context = context;
    }

    public async Task<TodoItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TodoItems
            .Include(t => t.TimeEntries)
            .Include(t => t.ActivityLogs)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<TodoItem?> GetByIdWithoutNavigationPropertiesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TodoItems
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<TodoItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TodoItems
            .Include(t => t.TimeEntries)
            .Include(t => t.ActivityLogs)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TodoItem>> GetByStatusAsync(TaskStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.TodoItems
            .Include(t => t.TimeEntries)
            .Include(t => t.ActivityLogs)
            .Where(t => t.Status == status)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TodoItem>> GetByPriorityAsync(Priority priority, CancellationToken cancellationToken = default)
    {
        return await _context.TodoItems
            .Include(t => t.TimeEntries)
            .Include(t => t.ActivityLogs)
            .Where(t => t.Priority == priority)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TodoItem>> GetByDueDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.TodoItems
            .Include(t => t.TimeEntries)
            .Include(t => t.ActivityLogs)
            .Where(t => t.DueDate.HasValue && t.DueDate >= startDate && t.DueDate <= endDate)
            .OrderBy(t => t.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TodoItem>> GetByTagsAsync(string tags, CancellationToken cancellationToken = default)
    {
        return await _context.TodoItems
            .Include(t => t.TimeEntries)
            .Include(t => t.ActivityLogs)
            .Where(t => t.Tags != null && t.Tags.Contains(tags))
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TodoItem>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        return await _context.TodoItems
            .Include(t => t.TimeEntries)
            .Include(t => t.ActivityLogs)
            .Where(t => t.Category == category)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TodoItem>> GetByTitleAsync(string title, CancellationToken cancellationToken = default)
    {
        return await _context.TodoItems
            .Include(t => t.TimeEntries)
            .Include(t => t.ActivityLogs)
            .Where(t => t.Title.Contains(title))
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TodoItem>> GetByTitleForDuplicateCheckAsync(string title, CancellationToken cancellationToken = default)
    {
        return await _context.TodoItems
            .AsNoTracking()
            .Where(t => t.Title.Contains(title))
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<TodoItem> AddAsync(TodoItem todoItem, CancellationToken cancellationToken = default)
    {
        // Ensure collections are initialized to avoid navigation property issues
        todoItem.TimeEntries = new List<TimeEntry>();
        todoItem.ActivityLogs = new List<ActivityLog>();
        
        _context.TodoItems.Add(todoItem);
        await _context.SaveChangesAsync(cancellationToken);
        return todoItem;
    }

    public async Task UpdateAsync(TodoItem todoItem, CancellationToken cancellationToken = default)
    {
        var existingEntity = await _context.TodoItems.FindAsync(new object[] { todoItem.Id }, cancellationToken);
        if (existingEntity == null)
        {
            throw new InvalidOperationException($"TodoItem with ID {todoItem.Id} not found.");
        }

        // Update the properties of the tracked entity
        existingEntity.Title = todoItem.Title;
        existingEntity.Description = todoItem.Description;
        existingEntity.Priority = todoItem.Priority;
        existingEntity.Status = todoItem.Status;
        existingEntity.Category = todoItem.Category;
        existingEntity.Tags = todoItem.Tags;
        existingEntity.DueDate = todoItem.DueDate;
        existingEntity.EstimatedTimeMinutes = todoItem.EstimatedTimeMinutes;
        existingEntity.ActualTimeMinutes = todoItem.ActualTimeMinutes;
        existingEntity.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException($"TodoItem with ID {todoItem.Id} could not be updated because it may have been modified or deleted by another process.");
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var todoItem = await _context.TodoItems.FindAsync(new object[] { id }, cancellationToken);
        if (todoItem != null)
        {
            _context.TodoItems.Remove(todoItem);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IEnumerable<TodoItem>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        return await _context.TodoItems
            .Include(t => t.TimeEntries)
            .Include(t => t.ActivityLogs)
            .Where(t => t.Title.Contains(searchTerm) || 
                       (t.Description != null && t.Description.Contains(searchTerm)) ||
                       (t.Tags != null && t.Tags.Contains(searchTerm)))
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
