using Timekeeper.Infrastructure.DevOps.GitHub;

namespace Timekeeper.CLI.Services;

public class GitHubService : IGitHubService
{
    private readonly IGitHubAuthService _authService;
    private readonly Timekeeper.Infrastructure.DevOps.GitHub.GitHubService _gitHubService;

    public GitHubService(IGitHubAuthService authService, Timekeeper.Infrastructure.DevOps.GitHub.GitHubService gitHubService)
    {
        _authService = authService;
        _gitHubService = gitHubService;
    }

    public async Task<bool> TestConnectionAsync(string organizationUrl, string personalAccessToken)
    {
        return await _authService.ValidateConnectionAsync(organizationUrl, personalAccessToken);
    }

    public async Task<IEnumerable<object>> GetIssuesAsync(string organizationUrl, string personalAccessToken, string? repositoryName = null)
    {
        return await _gitHubService.GetIssuesAsync(organizationUrl, personalAccessToken, repositoryName);
    }

    public async Task<object?> GetIssueByIdAsync(string organizationUrl, string personalAccessToken, string repository, int issueNumber)
    {
        return await _gitHubService.GetIssueByIdAsync(organizationUrl, personalAccessToken, repository, issueNumber);
    }

    public async Task<bool> UpdateIssueAsync(string organizationUrl, string personalAccessToken, string repository, int issueNumber, object updates)
    {
        return await _gitHubService.UpdateIssueAsync(organizationUrl, personalAccessToken, repository, issueNumber, updates);
    }

    public async Task<IEnumerable<string>> GetRepositoriesAsync(string organizationUrl, string personalAccessToken)
    {
        return await _gitHubService.GetRepositoriesAsync(organizationUrl, personalAccessToken);
    }

    public async Task<bool> ConfigureIntegrationAsync(string organizationUrl, string pat)
    {
        var isValid = await _authService.ValidateConnectionAsync(organizationUrl, pat);
        
        if (isValid)
        {
            // In a real app, you would save these settings to configuration
            // For now, we'll just return success
            return true;
        }

        return false;
    }
}
