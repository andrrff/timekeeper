using MediatR;
using Timekeeper.Application.TodoItems.Commands;
using Timekeeper.Domain.Entities;
using Timekeeper.Domain.Enums;
using Timekeeper.Domain.Interfaces;

namespace Timekeeper.CLI.Services;

public class DevOpsSyncService
{
    private readonly ProviderIntegrationService _devOpsIntegrationService;
    private readonly ITodoItemRepository _todoItemRepository;
    private readonly IMediator _mediator;

    public DevOpsSyncService(
        ProviderIntegrationService devOpsIntegrationService,
        ITodoItemRepository todoItemRepository,
        IMediator mediator)
    {
        _devOpsIntegrationService = devOpsIntegrationService;
        _todoItemRepository = todoItemRepository;
        _mediator = mediator;
    }

    public async Task<SyncResult> SyncWorkItemsToTodosAsync()
    {
        var result = new SyncResult();
        
        try
        {
            // Get work items from Azure DevOps
            var workItems = await _devOpsIntegrationService.SyncWorkItemsAsync();
            var workItemsList = workItems.ToList();
            
            if (!workItemsList.Any())
            {
                result.Message = "No work items found to sync.";
                return result;
            }

            // Get existing todos to avoid duplicates
            var existingTodos = await _todoItemRepository.GetAllAsync();
            var existingDevOpsIds = existingTodos
                .Where(t => !string.IsNullOrEmpty(t.Tags) && t.Tags.Contains("DevOps:"))
                .Select(t => ExtractDevOpsIdFromTags(t.Tags))
                .Where(id => id.HasValue)
                .Select(id => id.Value)
                .ToHashSet();

            foreach (var workItemObj in workItemsList)
            {
                try
                {
                    // Check if the object has an Id property
                    if (workItemObj == null)
                    {
                        result.SkippedCount++;
                        continue;
                    }

                    // Use reflection to access anonymous object properties
                    var objectType = workItemObj.GetType();
                    var idProperty = objectType.GetProperty("Id");
                    var titleProperty = objectType.GetProperty("Title");
                    var workItemTypeProperty = objectType.GetProperty("WorkItemType");
                    
                    if (idProperty == null)
                    {
                        result.ErrorCount++;
                        result.Errors.Add($"Work item object does not have Id property. Available properties: {string.Join(", ", objectType.GetProperties().Select(p => p.Name))}");
                        continue;
                    }

                    var idValue = idProperty.GetValue(workItemObj);
                    if (idValue == null || !int.TryParse(idValue.ToString(), out int id))
                    {
                        result.ErrorCount++;
                        result.Errors.Add($"Failed to parse Id value: {idValue}");
                        continue;
                    }

                    if (existingDevOpsIds.Contains(id))
                    {
                        result.SkippedCount++;
                        continue;
                    }

                    string title = titleProperty?.GetValue(workItemObj)?.ToString() ?? string.Empty;
                    string workItemType = workItemTypeProperty?.GetValue(workItemObj)?.ToString() ?? "Unknown";
                    string? description = null; // Anonymous object might not have description

                    // Create TodoItem from work item
                    var createCommand = new CreateTodoItemCommand(
                        Title: $"[[DevOps]] {title}",
                        Description: description ?? $"Azure DevOps Work Item #{id}",
                        Priority: MapWorkItemPriorityToTodoPriority(workItemType),
                        Category: "DevOps Integration",
                        Tags: $"DevOps:{id},Azure,{workItemType}",
                        EstimatedTimeMinutes: EstimateTimeFromWorkItemType(workItemType)
                    );

                    var todoItem = await _mediator.Send(createCommand);
                    result.CreatedCount++;
                    result.CreatedItems.Add($"{id}: {title}");
                }
                catch (Exception ex)
                {
                    result.ErrorCount++;
                    result.Errors.Add($"Error syncing work item: {ex.Message}");
                }
            }

            result.IsSuccess = true;
            result.Message = $"Sync completed: {result.CreatedCount} created, {result.SkippedCount} skipped, {result.ErrorCount} errors.";
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.Message = $"Sync failed: {ex.Message}";
        }

        return result;
    }

    public async Task<SyncResult> UpdateTodosFromWorkItemsAsync()
    {
        var result = new SyncResult();
        
        try
        {
            // Get existing todos that were synced from DevOps
            var existingTodos = await _todoItemRepository.GetAllAsync();
            var devOpsTodos = existingTodos
                .Where(t => !string.IsNullOrEmpty(t.Tags) && t.Tags.Contains("DevOps:"))
                .ToList();

            if (!devOpsTodos.Any())
            {
                result.Message = "No DevOps-synced todos found to update.";
                return result;
            }

            var integration = await _devOpsIntegrationService.GetActiveIntegrationAsync();
            if (integration == null)
            {
                result.Message = "No active DevOps integration found.";
                return result;
            }

            foreach (var todo in devOpsTodos)
            {
                try
                {
                    var devOpsId = ExtractDevOpsIdFromTags(todo.Tags);
                    if (!devOpsId.HasValue) continue;

                    var workItem = await _devOpsIntegrationService.GetWorkItemByIdAsync(devOpsId.Value);
                    if (workItem == null) continue;

                    dynamic dynamicWorkItem = workItem;
                    string title = dynamicWorkItem.Title?.ToString() ?? string.Empty;
                    string state = dynamicWorkItem.State?.ToString() ?? "Unknown";

                    // Update todo if work item state changed
                    var newStatus = MapWorkItemStateToTodoStatus(state);
                    if (todo.Status != newStatus)
                    {
                        todo.Status = newStatus;
                        todo.UpdatedAt = DateTime.UtcNow;
                        await _todoItemRepository.UpdateAsync(todo);
                        result.UpdatedCount++;
                        result.UpdatedItems.Add($"{devOpsId}: {title} -> {newStatus}");
                    }
                    else
                    {
                        result.SkippedCount++;
                    }
                }
                catch (Exception ex)
                {
                    result.ErrorCount++;
                    result.Errors.Add($"Error updating todo: {ex.Message}");
                }
            }

            result.IsSuccess = true;
            result.Message = $"Update completed: {result.UpdatedCount} updated, {result.SkippedCount} unchanged, {result.ErrorCount} errors.";
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.Message = $"Update failed: {ex.Message}";
        }

        return result;
    }

    public async Task<SyncResult> SyncAllAsync()
    {
        return await SyncWorkItemsToTodosAsync();
    }

    private DevOpsWorkItem? ParseWorkItem(object workItemObj)
    {
        try
        {
            var workItemType = workItemObj.GetType();
            var id = workItemType.GetProperty("Id")?.GetValue(workItemObj);
            var title = workItemType.GetProperty("Title")?.GetValue(workItemObj)?.ToString();
            var state = workItemType.GetProperty("State")?.GetValue(workItemObj)?.ToString();
            var type = workItemType.GetProperty("WorkItemType")?.GetValue(workItemObj)?.ToString();
            var description = workItemType.GetProperty("Description")?.GetValue(workItemObj)?.ToString();

            if (id == null || string.IsNullOrEmpty(title)) return null;

            return new DevOpsWorkItem
            {
                Id = Convert.ToInt32(id),
                Title = title,
                Description = description,
                State = state ?? "Unknown",
                WorkItemType = type ?? "Unknown"
            };
        }
        catch
        {
            return null;
        }
    }

    private int? ExtractDevOpsIdFromTags(string? tags)
    {
        if (string.IsNullOrEmpty(tags)) return null;
        
        var tagArray = tags.Split(',');
        var devOpsTag = tagArray.FirstOrDefault(t => t.StartsWith("DevOps:"));
        if (devOpsTag == null) return null;

        var idPart = devOpsTag.Substring("DevOps:".Length);
        return int.TryParse(idPart, out var id) ? id : null;
    }

    private Priority MapWorkItemPriorityToTodoPriority(string workItemType)
    {
        return workItemType?.ToLower() switch
        {
            "bug" => Priority.High,
            "task" => Priority.Medium,
            "user story" => Priority.Medium,
            "feature" => Priority.Low,
            _ => Priority.Medium
        };
    }

    private int EstimateTimeFromWorkItemType(string workItemType)
    {
        return workItemType?.ToLower() switch
        {
            "bug" => 120, // 2 hours
            "task" => 240, // 4 hours
            "user story" => 480, // 8 hours
            "feature" => 960, // 16 hours
            _ => 240 // 4 hours default
        };
    }

    private Timekeeper.Domain.Enums.TaskStatus MapWorkItemStateToTodoStatus(string state)
    {
        return state?.ToLower() switch
        {
            "new" => Timekeeper.Domain.Enums.TaskStatus.Pending,
            "active" => Timekeeper.Domain.Enums.TaskStatus.InProgress,
            "resolved" => Timekeeper.Domain.Enums.TaskStatus.InProgress,
            "closed" => Timekeeper.Domain.Enums.TaskStatus.Completed,
            "done" => Timekeeper.Domain.Enums.TaskStatus.Completed,
            _ => Timekeeper.Domain.Enums.TaskStatus.Pending
        };
    }
}

public class SyncResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public int CreatedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int SkippedCount { get; set; }
    public int ErrorCount { get; set; }
    public List<string> CreatedItems { get; set; } = new();
    public List<string> UpdatedItems { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public class DevOpsWorkItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string State { get; set; } = string.Empty;
    public string WorkItemType { get; set; } = string.Empty;
}
