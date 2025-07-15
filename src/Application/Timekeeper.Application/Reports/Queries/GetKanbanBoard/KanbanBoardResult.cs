using Timekeeper.Domain.Enums;

namespace Timekeeper.Application.Reports.Queries.GetKanbanBoard;

public class KanbanBoardResult
{
    public List<KanbanColumn> Columns { get; set; } = new();
    public KanbanStatistics Statistics { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string? CategoryFilter { get; set; }
    public Priority? PriorityFilter { get; set; }
}

public class KanbanColumn
{
    public string Name { get; set; } = string.Empty;
    public Timekeeper.Domain.Enums.TaskStatus Status { get; set; }
    public List<KanbanCard> Cards { get; set; } = new();
    public int CardCount => Cards.Count;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}

public class KanbanCard
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Priority Priority { get; set; }
    public string? Category { get; set; }
    public string? Tags { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public double EstimatedHours { get; set; }
    public double ActualHours { get; set; }
    public string? ProviderWorkItemId { get; set; }
    public string? ProviderUrl { get; set; }
    public bool HasActiveTimer { get; set; }
    public bool IsOverdue { get; set; }
    public int DaysUntilDue { get; set; }
    public string PriorityIcon => Priority switch
    {
        Priority.Critical => "🔴",
        Priority.High => "🟠", 
        Priority.Medium => "🟡",
        Priority.Low => "🟢",
        _ => "⚪"
    };
}

public class KanbanStatistics
{
    public int TotalCards { get; set; }
    public int PendingCards { get; set; }
    public int InProgressCards { get; set; }
    public int CompletedCards { get; set; }
    public int OnHoldCards { get; set; }
    public int CancelledCards { get; set; }
    public int OverdueCards { get; set; }
    public int CardsWithActiveTimers { get; set; }
    public double TotalEstimatedHours { get; set; }
    public double TotalActualHours { get; set; }
    public double CompletionPercentage { get; set; }
    public Dictionary<Priority, int> CardsByPriority { get; set; } = new();
    public Dictionary<string, int> CardsByCategory { get; set; } = new();
}
