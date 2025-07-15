using Microsoft.EntityFrameworkCore;
using Timekeeper.Domain.Entities;
using Timekeeper.Domain.Interfaces;
using Timekeeper.Infrastructure.Persistence;

namespace Timekeeper.Infrastructure.Repositories;

public class ProvidersIntegrationRepository : IProvidersIntegrationRepository
{
    private readonly TimekeeperDbContext _context;

    public ProvidersIntegrationRepository(TimekeeperDbContext context)
    {
        _context = context;
    }

    #region Basic CRUD Operations

    public async Task<ProviderIntegration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ProviderIntegrations
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<ProviderIntegration>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ProviderIntegrations
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ProviderIntegration integration, CancellationToken cancellationToken = default)
    {
        integration.CreatedAt = DateTime.UtcNow;
        integration.UpdatedAt = DateTime.UtcNow;
        _context.ProviderIntegrations.Add(integration);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ProviderIntegration integration, CancellationToken cancellationToken = default)
    {
        integration.UpdatedAt = DateTime.UtcNow;
        _context.ProviderIntegrations.Update(integration);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var integration = await GetByIdAsync(id, cancellationToken);
        if (integration != null)
        {
            _context.ProviderIntegrations.Remove(integration);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    #endregion

    #region Provider-specific Operations

    public async Task<IEnumerable<ProviderIntegration>> GetByProviderAsync(string provider, CancellationToken cancellationToken = default)
    {
        return await _context.ProviderIntegrations
            .Where(x => x.Provider == provider)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ProviderIntegration>> GetActiveByProviderAsync(string provider, CancellationToken cancellationToken = default)
    {
        return await _context.ProviderIntegrations
            .Where(x => x.Provider == provider && x.IsActive)
            .OrderByDescending(x => x.LastSyncAt ?? x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ProviderIntegration>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ProviderIntegrations
            .Where(x => x.IsActive)
            .OrderBy(x => x.Provider)
            .ThenByDescending(x => x.LastSyncAt ?? x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Status Management

    public async Task DeactivateAllAsync(CancellationToken cancellationToken = default)
    {
        var activeIntegrations = await _context.ProviderIntegrations
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var integration in activeIntegrations)
        {
            integration.IsActive = false;
            integration.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateByProviderAsync(string provider, CancellationToken cancellationToken = default)
    {
        var integrations = await _context.ProviderIntegrations
            .Where(x => x.Provider == provider && x.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var integration in integrations)
        {
            integration.IsActive = false;
            integration.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ActivateIntegrationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var integration = await GetByIdAsync(id, cancellationToken);
        if (integration != null)
        {
            integration.IsActive = true;
            integration.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeactivateIntegrationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var integration = await GetByIdAsync(id, cancellationToken);
        if (integration != null)
        {
            integration.IsActive = false;
            integration.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    #endregion

    #region Sync Management

    public async Task UpdateLastSyncAsync(Guid id, DateTime lastSync, CancellationToken cancellationToken = default)
    {
        var integration = await GetByIdAsync(id, cancellationToken);
        if (integration != null)
        {
            integration.LastSyncAt = lastSync;
            integration.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task UpdateLastSyncBulkAsync(IEnumerable<Guid> ids, DateTime lastSync, CancellationToken cancellationToken = default)
    {
        var integrations = await _context.ProviderIntegrations
            .Where(x => ids.Contains(x.Id))
            .ToListAsync(cancellationToken);

        foreach (var integration in integrations)
        {
            integration.LastSyncAt = lastSync;
            integration.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<ProviderIntegration>> GetDueForSyncAsync(TimeSpan? maxAge = null, CancellationToken cancellationToken = default)
    {
        var cutoffTime = DateTime.UtcNow - (maxAge ?? TimeSpan.FromHours(1));
        
        return await _context.ProviderIntegrations
            .Where(x => x.IsActive && (x.LastSyncAt == null || x.LastSyncAt < cutoffTime))
            .OrderBy(x => x.LastSyncAt ?? DateTime.MinValue)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ProviderIntegration>> GetByProviderDueForSyncAsync(string provider, TimeSpan? maxAge = null, CancellationToken cancellationToken = default)
    {
        var cutoffTime = DateTime.UtcNow - (maxAge ?? TimeSpan.FromHours(1));
        
        return await _context.ProviderIntegrations
            .Where(x => x.Provider == provider && x.IsActive && (x.LastSyncAt == null || x.LastSyncAt < cutoffTime))
            .OrderBy(x => x.LastSyncAt ?? DateTime.MinValue)
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Statistics and Monitoring

    public async Task<int> GetActiveCountByProviderAsync(string provider, CancellationToken cancellationToken = default)
    {
        return await _context.ProviderIntegrations
            .CountAsync(x => x.Provider == provider && x.IsActive, cancellationToken);
    }

    public async Task<Dictionary<string, int>> GetActiveCountByAllProvidersAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ProviderIntegrations
            .Where(x => x.IsActive)
            .GroupBy(x => x.Provider)
            .Select(g => new { Provider = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Provider, x => x.Count, cancellationToken);
    }

    public async Task<IEnumerable<ProviderIntegration>> GetRecentlyFailedAsync(TimeSpan? timeFrame = null, CancellationToken cancellationToken = default)
    {
        var cutoffTime = DateTime.UtcNow - (timeFrame ?? TimeSpan.FromDays(1));
        
        // Note: This assumes we'll add a LastSyncError or similar field to track sync failures
        // For now, we'll return integrations that are active but haven't synced in a while
        return await _context.ProviderIntegrations
            .Where(x => x.IsActive && x.LastSyncAt != null && x.LastSyncAt < cutoffTime)
            .OrderByDescending(x => x.LastSyncAt)
            .ToListAsync(cancellationToken);
    }

    #endregion
}
