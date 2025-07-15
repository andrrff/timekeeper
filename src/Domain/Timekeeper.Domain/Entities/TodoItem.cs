using Timekeeper.Domain.Common;
using Timekeeper.Domain.Enums;
using TaskStatus = Timekeeper.Domain.Enums.TaskStatus;

namespace Timekeeper.Domain.Entities;

public class TodoItem : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskStatus Status { get; set; } = TaskStatus.Pending;
    public Priority Priority { get; set; } = Priority.Medium;
    public DateTime? DueDate { get; set; }
    public string? Category { get; set; }
    public string? Tags { get; set; }
    public int EstimatedTimeMinutes { get; set; }
    public int? EstimatedHours { get; set; }
    public int ActualTimeMinutes { get; set; }
    public string? DevOpsWorkItemId { get; set; }
    public string? DevOpsUrl { get; set; }
    public bool IsCompleted => Status == TaskStatus.Completed;

    // Navigation properties
    public virtual ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();
    public virtual ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();
}
