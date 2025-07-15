using MediatR;
using Timekeeper.Domain.Enums;

namespace Timekeeper.Application.Reports.Queries.GetKanbanBoard;

public record GetKanbanBoardQuery(
    string? CategoryFilter = null,
    Priority? PriorityFilter = null,
    bool IncludeCompleted = false
) : IRequest<KanbanBoardResult>;
