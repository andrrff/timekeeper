namespace Timekeeper.Infrastructure.DevOps.AzureDevOps;

public interface IAzureDevOpsAuthService
{
    Task<bool> ValidateConnectionAsync(string organizationUrl, string personalAccessToken);
    Task<string> GetCurrentUserAsync(string organizationUrl, string personalAccessToken);
}
