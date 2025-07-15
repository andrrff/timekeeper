namespace Timekeeper.Infrastructure.DevOps.GitHub;

public interface IGitHubService
{
    Task<bool> TestConnectionAsync(string organizationUrl, string personalAccessToken);
    Task<IEnumerable<object>> GetIssuesAsync(string organizationUrl, string personalAccessToken, string? repositoryName = null);
    Task<object?> GetIssueByIdAsync(string organizationUrl, string personalAccessToken, string repository, int issueNumber);
    Task<bool> UpdateIssueAsync(string organizationUrl, string personalAccessToken, string repository, int issueNumber, object updates);
    Task<IEnumerable<string>> GetRepositoriesAsync(string organizationUrl, string personalAccessToken);
}
