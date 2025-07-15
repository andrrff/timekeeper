using System.Net.Http.Headers;

namespace Timekeeper.Infrastructure.DevOps.GitHub;

public class GitHubAuthService : IGitHubAuthService
{
    private readonly SimpleGitHubService _simpleGitHubService;

    public GitHubAuthService()
    {
        _simpleGitHubService = new SimpleGitHubService();
    }

    public async Task<bool> ValidateConnectionAsync(string organizationUrl, string personalAccessToken)
    {
        return await _simpleGitHubService.TestConnectionAsync(organizationUrl, personalAccessToken);
    }

    public async Task<string> GetCurrentUserAsync(string organizationUrl, string personalAccessToken)
    {
        return await _simpleGitHubService.GetCurrentUserAsync(organizationUrl, personalAccessToken);
    }

    public void Dispose()
    {
        _simpleGitHubService?.Dispose();
    }
}
