namespace Timekeeper.Domain.Interfaces;

public interface IDevOpsService
{
    Task<bool> TestConnectionAsync(string organizationUrl, string personalAccessToken);
    Task<IEnumerable<object>> GetWorkItemsAsync(string organizationUrl, string personalAccessToken, string? projectName = null);
    Task<object?> GetWorkItemByIdAsync(string organizationUrl, string personalAccessToken, int workItemId);
    Task<bool> UpdateWorkItemAsync(string organizationUrl, string personalAccessToken, int workItemId, object updates);
    Task<IEnumerable<string>> GetProjectsAsync(string organizationUrl, string personalAccessToken);
}
