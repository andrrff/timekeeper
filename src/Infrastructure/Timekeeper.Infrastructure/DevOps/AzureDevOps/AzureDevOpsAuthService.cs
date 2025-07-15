using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace Timekeeper.Infrastructure.DevOps.AzureDevOps;

public class AzureDevOpsAuthService : IAzureDevOpsAuthService
{
    public async Task<bool> ValidateConnectionAsync(string organizationUrl, string personalAccessToken)
    {
        try
        {
            var credentials = new VssBasicCredential(string.Empty, personalAccessToken);
            using var connection = new VssConnection(new Uri(organizationUrl), credentials);
            
            var projectClient = connection.GetClient<ProjectHttpClient>();
            await projectClient.GetProjects();
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string> GetCurrentUserAsync(string organizationUrl, string personalAccessToken)
    {
        try
        {
            var credentials = new VssBasicCredential(string.Empty, personalAccessToken);
            using var connection = new VssConnection(new Uri(organizationUrl), credentials);
            
            // For now, return a simple success message since detailed user info requires additional packages
            return "Connected User";
        }
        catch
        {
            return "Unknown";
        }
    }
}
