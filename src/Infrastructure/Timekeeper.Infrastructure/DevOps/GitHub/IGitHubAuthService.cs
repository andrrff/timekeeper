namespace Timekeeper.Infrastructure.DevOps.GitHub;

public interface IGitHubAuthService : IDisposable
{
    Task<bool> ValidateConnectionAsync(string organizationUrl, string personalAccessToken);
    Task<string> GetCurrentUserAsync(string organizationUrl, string personalAccessToken);
}
