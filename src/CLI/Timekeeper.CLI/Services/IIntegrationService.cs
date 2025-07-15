using Timekeeper.Domain.Entities;

namespace Timekeeper.CLI.Services;

public interface IIntegrationService
{
    string ProviderName { get; }
    Task<bool> ConfigureIntegrationAsync();
    Task<bool> TestConnectionAsync();
    Task SyncAsync();
    Task<IEnumerable<ProviderIntegration>> GetConfiguredIntegrationsAsync();
    Task<IEnumerable<ProviderIntegration>> GetActiveIntegrationsAsync();
    Task<ProviderIntegration?> GetIntegrationByIdAsync(Guid integrationId);
    Task<bool> RemoveIntegrationAsync(Guid integrationId);
    Task<bool> SyncAsync(ProviderIntegration integration);
    Task<bool> TestConnectionAsync(ProviderIntegration integration);
}
