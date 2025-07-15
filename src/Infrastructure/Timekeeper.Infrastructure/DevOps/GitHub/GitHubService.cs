using System.Net.Http.Headers;
using System.Text;
using Timekeeper.Domain.Interfaces;

namespace Timekeeper.Infrastructure.DevOps.GitHub;

public class GitHubService : IDevOpsService
{
    private readonly SimpleGitHubService _simpleGitHubService;

    public GitHubService()
    {
        _simpleGitHubService = new SimpleGitHubService();
    }

    public async Task<bool> TestConnectionAsync(string organizationUrl, string personalAccessToken)
    {
        return await _simpleGitHubService.TestConnectionAsync(organizationUrl, personalAccessToken);
    }

    public async Task<IEnumerable<object>> GetWorkItemsAsync(string organizationUrl, string personalAccessToken, string? projectName = null)
    {
        try
        {
            // Get GitHub issues assigned to the current user
            var issues = await _simpleGitHubService.GetAssignedIssuesAsync(organizationUrl, personalAccessToken, projectName);
            
            // Convert to objects for the interface
            return issues.Cast<object>().ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get GitHub issues: {ex.Message}");
            return new List<object>();
        }
    }

    public async Task<object?> GetWorkItemByIdAsync(string organizationUrl, string personalAccessToken, int workItemId)
    {
        // Not implemented for now
        return null;
    }

    public async Task<bool> UpdateWorkItemAsync(string organizationUrl, string personalAccessToken, int workItemId, object updates)
    {
        // Not implemented for now
        return false;
    }

    public async Task<IEnumerable<string>> GetProjectsAsync(string organizationUrl, string personalAccessToken)
    {
        return await _simpleGitHubService.GetRepositoriesAsync(organizationUrl, personalAccessToken);
    }

    // GitHub-specific methods
    public async Task<IEnumerable<object>> GetIssuesAsync(string organizationUrl, string personalAccessToken, string? repositoryName = null)
    {
        return await GetWorkItemsAsync(organizationUrl, personalAccessToken, repositoryName);
    }

    public async Task<object?> GetIssueByIdAsync(string organizationUrl, string personalAccessToken, string repository, int issueNumber)
    {
        return null;
    }

    public async Task<bool> UpdateIssueAsync(string organizationUrl, string personalAccessToken, string repository, int issueNumber, object updates)
    {
        return false;
    }

    public async Task<IEnumerable<string>> GetRepositoriesAsync(string organizationUrl, string personalAccessToken)
    {
        return await GetProjectsAsync(organizationUrl, personalAccessToken);
    }

    public async Task<string> DebugIssuesAsync(string organizationUrl, string personalAccessToken, string? repositoryName = null)
    {
        return await _simpleGitHubService.DebugGitHubIssuesAsync(organizationUrl, personalAccessToken, repositoryName);
    }

    public void Dispose()
    {
        _simpleGitHubService?.Dispose();
    }
}
