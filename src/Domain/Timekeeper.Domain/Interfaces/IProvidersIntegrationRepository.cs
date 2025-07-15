using Timekeeper.Domain.Entities;

namespace Timekeeper.Domain.Interfaces;

public interface IProvidersIntegrationRepository
{
    // Basic CRUD operations
    Task<ProviderIntegration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProviderIntegration>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(ProviderIntegration integration, CancellationToken cancellationToken = default);
    Task UpdateAsync(ProviderIntegration integration, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    // Provider-specific operations
    Task<IEnumerable<ProviderIntegration>> GetByProviderAsync(string provider, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProviderIntegration>> GetActiveByProviderAsync(string provider, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProviderIntegration>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    
    // Status management
    Task DeactivateAllAsync(CancellationToken cancellationToken = default);
    Task DeactivateByProviderAsync(string provider, CancellationToken cancellationToken = default);
    Task ActivateIntegrationAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeactivateIntegrationAsync(Guid id, CancellationToken cancellationToken = default);

    // Sync management
    Task UpdateLastSyncAsync(Guid id, DateTime lastSync, CancellationToken cancellationToken = default);
    Task UpdateLastSyncBulkAsync(IEnumerable<Guid> ids, DateTime lastSync, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProviderIntegration>> GetDueForSyncAsync(TimeSpan? maxAge = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProviderIntegration>> GetByProviderDueForSyncAsync(string provider, TimeSpan? maxAge = null, CancellationToken cancellationToken = default);

    // Statistics and monitoring
    Task<int> GetActiveCountByProviderAsync(string provider, CancellationToken cancellationToken = default);
    Task<Dictionary<string, int>> GetActiveCountByAllProvidersAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<ProviderIntegration>> GetRecentlyFailedAsync(TimeSpan? timeFrame = null, CancellationToken cancellationToken = default);
}
