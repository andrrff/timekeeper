using Timekeeper.Domain.Common;
using Timekeeper.Domain.Enums;

namespace Timekeeper.Domain.Entities;

public class ActivityLog : BaseEntity
{
    public Guid TodoItemId { get; set; }
    public LogType LogType { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }

    // Navigation properties
    public virtual TodoItem TodoItem { get; set; } = null!;
}
