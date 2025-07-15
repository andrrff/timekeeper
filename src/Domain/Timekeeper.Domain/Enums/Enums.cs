namespace Timekeeper.Domain.Enums;

public enum TaskStatus
{
    Pending,
    InProgress,
    Completed,
    Cancelled,
    OnHold
}

public enum Priority
{
    Low,
    Medium,
    High,
    Critical
}

public enum LogType
{
    Created,
    Updated,
    StatusChanged,
    PriorityChanged,
    Deleted,
    TimeTracked
}
