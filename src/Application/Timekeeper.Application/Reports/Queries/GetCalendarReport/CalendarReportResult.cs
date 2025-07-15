namespace Timekeeper.Application.Reports.Queries.GetCalendarReport;

public class CalendarReportResult
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public CalendarReportType ReportType { get; set; }
    public List<CalendarDay> Days { get; set; } = new();
    public CalendarSummary Summary { get; set; } = new();
}

public class CalendarDay
{
    public DateTime Date { get; set; }
    public List<CalendarActivity> Activities { get; set; } = new();
    public double TotalHours { get; set; }
    public int CompletedTasks { get; set; }
    public bool IsWeekend { get; set; }
    public bool IsToday { get; set; }
}

public class CalendarActivity
{
    public Guid TimeEntryId { get; set; }
    public Guid TodoItemId { get; set; }
    public string TodoTitle { get; set; } = string.Empty;
    public string? TodoCategory { get; set; }
    public string? Description { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public double DurationHours { get; set; }
    public bool IsActive { get; set; }
    public string? ProviderWorkItemId { get; set; }
    public string? Tags { get; set; }
}

public class CalendarSummary
{
    public double TotalHours { get; set; }
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int ActiveTimers { get; set; }
    public Dictionary<string, double> HoursByCategory { get; set; } = new();
    public Dictionary<DateTime, double> HoursByDay { get; set; } = new();
    public double AverageHoursPerDay { get; set; }
    public DateTime? MostProductiveDay { get; set; }
    public double MostProductiveDayHours { get; set; }
}
