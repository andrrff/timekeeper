using Timekeeper.Infrastructure.DevOps.AzureDevOps;

namespace Timekeeper.CLI.Services;

public class DevOpsService : IDevOpsService
{
    private readonly IAzureDevOpsAuthService _authService;
    private readonly Timekeeper.Domain.Interfaces.IDevOpsService _azureDevOpsService;

    // Mock configuration - in real app this would come from settings
    private const string MockOrgUrl = "https://dev.azure.com/myorg";
    private const string MockPat = "mock-personal-access-token";

    public DevOpsService(IAzureDevOpsAuthService authService, Timekeeper.Domain.Interfaces.IDevOpsService azureDevOpsService)
    {
        _authService = authService;
        _azureDevOpsService = azureDevOpsService;
    }

    public async Task<bool> TestConnectionAsync()
    {
        return await _authService.ValidateConnectionAsync(MockOrgUrl, MockPat);
    }

    public async Task<IEnumerable<object>> SyncWorkItemsAsync()
    {
        return await _azureDevOpsService.GetWorkItemsAsync(MockOrgUrl, MockPat);
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

    public async Task ShowStatusAsync()
    {
        // Show basic connection status for CLI
        var isConnected = await TestConnectionAsync();
        if (isConnected)
        {
            Console.WriteLine("✅ DevOps connection is active");
        }
        else
        {
            Console.WriteLine("❌ DevOps connection is not available");
        }
    }
}