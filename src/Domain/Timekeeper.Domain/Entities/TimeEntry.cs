using Timekeeper.Domain.Common;

namespace Timekeeper.Domain.Entities;

public class TimeEntry : BaseEntity
{
    public Guid TodoItemId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int DurationMinutes { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }

    // Navigation properties
    public virtual TodoItem TodoItem { get; set; } = null!;
}
