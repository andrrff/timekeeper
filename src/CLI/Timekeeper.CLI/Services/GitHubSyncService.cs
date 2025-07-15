using Spectre.Console;
using Timekeeper.Domain.Entities;
using Timekeeper.Domain.Interfaces;
using Timekeeper.Domain.Enums;
using Timekeeper.Infrastructure.DevOps.GitHub;
using Timekeeper.Infrastructure.DevOps.GitHub.Models;

namespace Timekeeper.CLI.Services;

public class GitHubSyncService
{
    private readonly ITimeEntryRepository _timeEntryRepository;
    private readonly ITodoItemRepository _todoItemRepository;
    private readonly IActivityLogRepository _activityLogRepository;
    private readonly GitHubIntegrationService _gitHubIntegrationService;
    private readonly Timekeeper.Infrastructure.DevOps.GitHub.GitHubService _gitHubService;

    public GitHubSyncService(
        ITimeEntryRepository timeEntryRepository,
        ITodoItemRepository todoItemRepository,
        IActivityLogRepository activityLogRepository,
        GitHubIntegrationService gitHubIntegrationService,
        Timekeeper.Infrastructure.DevOps.GitHub.GitHubService gitHubService)
    {
        _timeEntryRepository = timeEntryRepository;
        _todoItemRepository = todoItemRepository;
        _activityLogRepository = activityLogRepository;
        _gitHubIntegrationService = gitHubIntegrationService;
        _gitHubService = gitHubService;
    }

    public async Task<IEnumerable<object>> SyncIssuesAsync()
    {
        var integration = await _gitHubIntegrationService.GetActiveGitHubIntegrationAsync();
        if (integration == null)
        {
            AnsiConsole.MarkupLine("[red]No active GitHub integration found.[/]");
            return new List<object>();
        }

        try
        {
            var issues = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Star)
                .StartAsync("Syncing GitHub issues...", async ctx =>
                {
                    return await _gitHubService.GetWorkItemsAsync(
                        integration.OrganizationUrl,
                        integration.PersonalAccessToken,
                        integration.ProjectName);
                });

            var issueList = issues.ToList();
            
            if (issueList.Any())
            {
                AnsiConsole.MarkupLine($"[green]Found {issueList.Count} GitHub issues[/]");
                
                await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Star)
                    .StartAsync("Creating todo items from issues...", async ctx =>
                    {
                        await CreateTodoItemsFromIssuesAsync(issueList);
                    });

                // TODO: Create a separate logging system for sync activities that don't relate to specific TodoItems
                // For now, we'll log to console only to avoid foreign key constraint issues
                
                AnsiConsole.MarkupLine("[blue]✓ GitHub sync completed successfully![/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]No GitHub issues found to sync.[/]");
            }

            return issueList;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error during GitHub sync: {ex.Message}[/]");
            
            // TODO: Create a separate logging system for sync errors that don't relate to specific TodoItems
            // For now, we'll log to console only to avoid foreign key constraint issues
            
            return new List<object>();
        }
    }

    public async Task<bool> SyncSpecificIssueAsync(string repository, int issueNumber)
    {
        var integration = await _gitHubIntegrationService.GetActiveGitHubIntegrationAsync();
        if (integration == null)
        {
            AnsiConsole.MarkupLine("[red]No active GitHub integration found.[/]");
            return false;
        }

        try
        {
            var issue = await _gitHubService.GetIssueByIdAsync(
                integration.OrganizationUrl,
                integration.PersonalAccessToken,
                repository,
                issueNumber);

            if (issue != null)
            {
                await CreateTodoItemFromIssueAsync(issue);
                AnsiConsole.MarkupLine($"[green]✓ Issue #{issueNumber} synced successfully![/]");
                return true;
            }
            else
            {
                AnsiConsole.MarkupLine($"[yellow]Issue #{issueNumber} not found in repository {repository}.[/]");
                return false;
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error syncing issue #{issueNumber}: {ex.Message}[/]");
            return false;
        }
    }

    private async Task CreateTodoItemsFromIssuesAsync(List<object> issues)
    {
        var createdCount = 0;
        var skippedCount = 0;

        foreach (var issueObj in issues)
        {
            try
            {
                string issueTitle = "";
                string issueNumber = "";
                
                // Handle GitHubIssue type
                if (issueObj is Timekeeper.Infrastructure.DevOps.GitHub.Models.GitHubIssue gitHubIssue)
                {
                    issueTitle = gitHubIssue.Title;
                    issueNumber = gitHubIssue.Number.ToString();
                }
                else
                {
                    // Fallback for dynamic objects
                    dynamic issue = issueObj;
                    issueTitle = issue.Title?.ToString() ?? "";
                    issueNumber = issue.Id?.ToString() ?? "";
                }
                
                // Check if todo item already exists based on title and external reference
                var existingTodos = await _todoItemRepository.GetByTitleForDuplicateCheckAsync(issueTitle);
                
                TodoItem? existingTodo = null;
                foreach (var todo in existingTodos)
                {
                    if (todo.Description?.Contains($"GitHub Issue #{issueNumber}") == true ||
                        todo.Tags?.Contains($"GitHub:{issueNumber}") == true)
                    {
                        existingTodo = todo;
                        break;
                    }
                }

                if (existingTodo == null)
                {
                    await CreateTodoItemFromIssueAsync(issueObj);
                    createdCount++;
                }
                else
                {
                    skippedCount++;
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error creating todo for issue: {ex.Message}[/]");
            }
        }

        AnsiConsole.MarkupLine($"[green]Created {createdCount} new todo items, skipped {skippedCount} existing items[/]");
    }

    private async Task CreateTodoItemFromIssueAsync(dynamic issue)
    {
        // Cast to GitHubIssue if it's that type, otherwise use dynamic access
        if (issue is Timekeeper.Infrastructure.DevOps.GitHub.Models.GitHubIssue gitHubIssue)
        {
            var todoItem = new TodoItem
            {
                Id = Guid.NewGuid(),
                Title = gitHubIssue.Title,
                Description = $"GitHub Issue #{gitHubIssue.Number}\n" +
                             $"State: {gitHubIssue.State}\n" +
                             $"URL: {gitHubIssue.HtmlUrl}\n" +
                             $"Body: {gitHubIssue.Body}",
                Status = string.Equals(gitHubIssue.State, "closed", StringComparison.OrdinalIgnoreCase) 
                    ? Domain.Enums.TaskStatus.Completed 
                    : Domain.Enums.TaskStatus.Pending,
                Priority = Domain.Enums.Priority.Medium,
                CreatedAt = DateTime.UtcNow,
                DueDate = null,
                Tags = $"GitHub:{gitHubIssue.Number}"
            };

            await _todoItemRepository.AddAsync(todoItem);
        }
        else
        {
            // Fallback for other issue types
            var todoItem = new TodoItem
            {
                Id = Guid.NewGuid(),
                Title = issue.Title?.ToString() ?? "GitHub Issue",
                Description = $"GitHub Issue #{issue.Id}\n" +
                             $"State: {issue.State}\n" +
                             $"Assigned to: {issue.AssignedTo ?? "Unassigned"}\n" +
                             $"URL: {issue.Url}\n" +
                             $"Created: {issue.CreatedDate}\n" +
                             $"Updated: {issue.UpdatedDate}",
                Status = string.Equals(issue.State?.ToString(), "closed", StringComparison.OrdinalIgnoreCase) 
                    ? Domain.Enums.TaskStatus.Completed 
                    : Domain.Enums.TaskStatus.Pending,
                Priority = Domain.Enums.Priority.Medium,
                CreatedAt = DateTime.UtcNow,
                DueDate = null
            };

            await _todoItemRepository.AddAsync(todoItem);
        }
    }

    public async Task<IEnumerable<object>> GetSyncedIssuesAsync()
    {
        try
        {
            return await _gitHubIntegrationService.GetIssuesAsync();
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error getting synced issues: {ex.Message}[/]");
            return new List<object>();
        }
    }

    public async Task<bool> UpdateIssueFromTimeEntryAsync(int issueId, string repository, TimeEntry timeEntry)
    {
        var integration = await _gitHubIntegrationService.GetActiveGitHubIntegrationAsync();
        if (integration == null)
        {
            return false;
        }

        try
        {
            var comment = new
            {
                body = $"⏱️ Time logged: {timeEntry.DurationMinutes / 60.0:F2} hours\n" +
                       $"📝 Description: {timeEntry.Description}\n" +
                       $"📅 Date: {timeEntry.StartTime:yyyy-MM-dd}\n" +
                       $"🤖 Logged via Timekeeper"
            };

            var success = await _gitHubService.UpdateIssueAsync(
                integration.OrganizationUrl,
                integration.PersonalAccessToken,
                repository,
                issueId,
                comment);

            if (success)
            {
                // TODO: Associate this activity log with the specific TodoItem created from the time entry
                // For now, we'll log to console only to avoid foreign key constraint issues
                AnsiConsole.MarkupLine($"[green]✓ Updated issue #{issueId} with time entry - Duration: {timeEntry.DurationMinutes / 60.0:F2}h[/]");
            }

            return success;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error updating GitHub issue: {ex.Message}[/]");
            return false;
        }
    }

    public async Task<bool> CloseIssueAsync(int issueId, string repository)
    {
        var integration = await _gitHubIntegrationService.GetActiveGitHubIntegrationAsync();
        if (integration == null)
        {
            return false;
        }

        try
        {
            var update = new { state = "closed" };

            var success = await _gitHubService.UpdateIssueAsync(
                integration.OrganizationUrl,
                integration.PersonalAccessToken,
                repository,
                issueId,
                update);

            if (success)
            {
                AnsiConsole.MarkupLine($"[green]✓ Issue #{issueId} closed successfully![/]");
                
                // TODO: Associate this activity log with the specific TodoItem related to this issue
                // For now, we'll log to console only to avoid foreign key constraint issues
            }

            return success;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error closing GitHub issue: {ex.Message}[/]");
            return false;
        }
    }
}
