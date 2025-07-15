using Timekeeper.Domain.Entities;

namespace Timekeeper.CLI.Services.Integrations;

/// <summary>
/// Interface base para todos os serviços de integração
/// </summary>
public interface IIntegrationService
{
    /// <summary>
    /// Nome do provedor (ex: "AzureDevOps", "GitHub", "Jira")
    /// </summary>
    string ProviderName { get; }
    
    /// <summary>
    /// Configura uma nova integração
    /// </summary>
    Task<bool> ConfigureIntegrationAsync();
    
    /// <summary>
    /// Testa a conexão com a integração
    /// </summary>
    Task<bool> TestConnectionAsync(ProviderIntegration integration);
    
    /// <summary>
    /// Obtém todas as integrações ativas deste provedor
    /// </summary>
    Task<IEnumerable<ProviderIntegration>> GetActiveIntegrationsAsync();
    
    /// <summary>
    /// Obtém uma integração específica por ID
    /// </summary>
    Task<ProviderIntegration?> GetIntegrationByIdAsync(Guid integrationId);
    
    /// <summary>
    /// Remove uma integração
    /// </summary>
    Task<bool> RemoveIntegrationAsync(Guid integrationId);
    
    /// <summary>
    /// Sincroniza dados desta integração
    /// </summary>
    Task<bool> SyncAsync(ProviderIntegration integration);
}
