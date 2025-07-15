using MediatR;

namespace Timekeeper.Application.Reports.Queries.GetCalendarReport;

public record GetCalendarReportQuery(
    DateTime StartDate,
    DateTime EndDate,
    CalendarReportType ReportType
) : IRequest<CalendarReportResult>;

public enum CalendarReportType
{
    Daily,
    Weekly,
    Monthly
}
