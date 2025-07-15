using Spectre.Console;
using Timekeeper.Domain.Entities;
using Timekeeper.Domain.Interfaces;

namespace Timekeeper.CLI.Services;

/// <summary>
/// Gerenciador central para todas as integrações com processo de sync avançado
/// </summary>
public class IntegrationManager
{
    private readonly IProvidersIntegrationRepository _integrationRepository;
    private readonly Dictionary<string, IIntegrationService> _integrationServices;

    public IntegrationManager(
        IProvidersIntegrationRepository integrationRepository,
        IEnumerable<IIntegrationService> integrationServices)
    {
        _integrationRepository = integrationRepository;
        _integrationServices = integrationServices.ToDictionary(s => s.ProviderName, s => s);
    }

    #region Provider Management

    /// <summary>
    /// Obtém todos os provedores disponíveis
    /// </summary>
    public IEnumerable<string> GetAvailableProviders()
    {
        return _integrationServices.Keys;
    }

    /// <summary>
    /// Obtém um serviço de integração específico
    /// </summary>
    public IIntegrationService? GetIntegrationService(string provider)
    {
        return _integrationServices.TryGetValue(provider, out var service) ? service : null;
    }

    /// <summary>
    /// Obtém estatísticas de todos os provedores
    /// </summary>
    public async Task<Dictionary<string, int>> GetProviderStatisticsAsync()
    {
        return await _integrationRepository.GetActiveCountByAllProvidersAsync();
    }

    #endregion

    #region Integration Management

    /// <summary>
    /// Obtém todas as integrações ativas de todos os provedores
    /// </summary>
    public async Task<IEnumerable<ProviderIntegration>> GetAllActiveIntegrationsAsync()
    {
        return await _integrationRepository.GetAllActiveAsync();
    }

    /// <summary>
    /// Remove uma integração específica
    /// </summary>
    public async Task<bool> RemoveIntegrationAsync(Guid integrationId)
    {
        var integration = await _integrationRepository.GetByIdAsync(integrationId);
        if (integration == null)
        {
            AnsiConsole.MarkupLine("[yellow]Integration not found.[/]");
            return false;
        }

        var service = GetIntegrationService(integration.Provider);
        if (service != null)
        {
            return await service.RemoveIntegrationAsync(integrationId);
        }

        // Se o serviço não estiver disponível, remove diretamente
        await _integrationRepository.DeleteAsync(integrationId);
        AnsiConsole.MarkupLine("[green]Integration removed successfully![/]");
        return true;
    }

    #endregion

    #region Advanced Sync Operations

    /// <summary>
    /// Executa sync inteligente baseado em prioridades e frequência
    /// </summary>
    public async Task<MultiProviderSyncResult> ExecuteSmartSyncAsync(SyncOptions? options = null)
    {
        options ??= new SyncOptions();
        var result = new MultiProviderSyncResult();
        
        AnsiConsole.MarkupLine("[bold blue]🔄 Iniciando Sync Inteligente de Múltiplos Provedores[/]");
        
        try
        {
            var integrationsToSync = await GetIntegrationsDueForSyncAsync(options);
            
            if (!integrationsToSync.Any())
            {
                AnsiConsole.MarkupLine("[green]✓ Todas as integrações estão atualizadas[/]");
                result.Success = true;
                return result;
            }

            var groupedByProvider = integrationsToSync
                .GroupBy(i => i.Provider)
                .OrderBy(g => GetProviderPriority(g.Key, options))
                .ToList();

            foreach (var providerGroup in groupedByProvider)
            {
                var providerResult = await SyncProviderIntegrationsAsync(
                    providerGroup.Key, 
                    providerGroup.ToList(), 
                    options);
                    
                result.Merge(providerResult);
            }

            await UpdateSyncTimestampsAsync(result.SuccessfulSyncs);
            result.Success = result.SuccessfulSyncs.Any() || !result.FailedSyncs.Any();
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]❌ Erro durante sync inteligente: {ex.Message}[/]");
            result.Success = false;
            result.GlobalError = ex.Message;
        }

        DisplaySyncSummary(result);
        return result;
    }

    /// <summary>
    /// Sync específico por provedor
    /// </summary>
    public async Task<MultiProviderSyncResult> SyncProviderAsync(string provider, SyncOptions? options = null)
    {
        options ??= new SyncOptions();
        
        var service = GetIntegrationService(provider);
        if (service == null)
        {
            var result = new MultiProviderSyncResult
            {
                Success = false,
                GlobalError = $"Provedor {provider} não encontrado"
            };
            return result;
        }

        var integrations = await _integrationRepository.GetActiveByProviderAsync(provider);
        return await SyncProviderIntegrationsAsync(provider, integrations.ToList(), options);
    }

    /// <summary>
    /// Sync de emergência
    /// </summary>
    public async Task<MultiProviderSyncResult> ExecuteEmergencySyncAsync()
    {
        AnsiConsole.MarkupLine("[bold red]🚨 Executando Sync de Emergência[/]");
        
        var emergencyOptions = new SyncOptions
        {
            MaxAge = TimeSpan.FromMinutes(5),
            ConcurrentSyncs = 1,
            SkipTestConnection = false,
            RetryFailedConnections = true
        };

        return await ExecuteSmartSyncAsync(emergencyOptions);
    }

    /// <summary>
    /// Sync em lote para integrações específicas
    /// </summary>
    public async Task<MultiProviderSyncResult> SyncSpecificIntegrationsAsync(IEnumerable<Guid> integrationIds, SyncOptions? options = null)
    {
        options ??= new SyncOptions();
        var result = new MultiProviderSyncResult();
        
        var integrations = new List<ProviderIntegration>();
        foreach (var id in integrationIds)
        {
            var integration = await _integrationRepository.GetByIdAsync(id);
            if (integration != null && integration.IsActive)
            {
                integrations.Add(integration);
            }
        }

        var groupedByProvider = integrations.GroupBy(i => i.Provider);
        
        foreach (var providerGroup in groupedByProvider)
        {
            var providerResult = await SyncProviderIntegrationsAsync(
                providerGroup.Key, 
                providerGroup.ToList(), 
                options);
                
            result.Merge(providerResult);
        }

        await UpdateSyncTimestampsAsync(result.SuccessfulSyncs);
        return result;
    }

    #endregion

    #region Compatibility Methods (Legacy Support)

    /// <summary>
    /// Configura uma nova integração para o provedor especificado
    /// </summary>
    public async Task<bool> ConfigureIntegrationAsync(string provider)
    {
        var service = GetIntegrationService(provider);
        if (service == null)
        {
            AnsiConsole.MarkupLine($"[red]❌ Provedor {provider} não encontrado[/]");
            return false;
        }

        return await service.ConfigureIntegrationAsync();
    }

    /// <summary>
    /// Mostra o status de todas as integrações
    /// </summary>
    public async Task ShowIntegrationsStatusAsync()
    {
        var integrations = await GetAllActiveIntegrationsAsync();
        var groupedByProvider = integrations.GroupBy(i => i.Provider);

        foreach (var providerGroup in groupedByProvider)
        {
            AnsiConsole.MarkupLine($"[bold blue]{providerGroup.Key}:[/]");
            
            foreach (var integration in providerGroup)
            {
                var status = integration.IsActive ? "[green]✓ Active[/]" : "[red]❌ Inactive[/]";
                var lastSync = integration.LastSyncAt?.ToString("yyyy-MM-dd HH:mm") ?? "Never";
                
                AnsiConsole.MarkupLine($"  • {integration.OrganizationUrl} - {status} - Last sync: {lastSync}");
            }
            
            AnsiConsole.WriteLine();
        }
    }

    /// <summary>
    /// Testa todas as conexões ativas
    /// </summary>
    public async Task TestAllConnectionsAsync()
    {
        var integrations = await GetAllActiveIntegrationsAsync();
        var groupedByProvider = integrations.GroupBy(i => i.Provider);

        foreach (var providerGroup in groupedByProvider)
        {
            var service = GetIntegrationService(providerGroup.Key);
            if (service == null) continue;

            AnsiConsole.MarkupLine($"[bold blue]Testing {providerGroup.Key} connections:[/]");

            foreach (var integration in providerGroup)
            {
                try
                {
                    var result = await service.TestConnectionAsync(integration);
                    var status = result ? "[green]✓ Connected[/]" : "[red]❌ Failed[/]";
                    AnsiConsole.MarkupLine($"  • {integration.OrganizationUrl}: {status}");
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"  • {integration.OrganizationUrl}: [red]❌ Error: {ex.Message}[/]");
                }
            }
            
            AnsiConsole.WriteLine();
        }
    }

    /// <summary>
    /// Sincroniza todas as integrações (método legado - usa smart sync)
    /// </summary>
    public async Task SyncAllIntegrationsAsync()
    {
        await ExecuteSmartSyncAsync();
    }

    #endregion

    #region Private Helper Methods

    private async Task<IEnumerable<ProviderIntegration>> GetIntegrationsDueForSyncAsync(SyncOptions options)
    {
        if (options.ForceSync)
        {
            return await _integrationRepository.GetAllActiveAsync();
        }

        return await _integrationRepository.GetDueForSyncAsync(options.MaxAge);
    }

    private async Task<MultiProviderSyncResult> SyncProviderIntegrationsAsync(
        string provider, 
        List<ProviderIntegration> integrations, 
        SyncOptions options)
    {
        var result = new MultiProviderSyncResult();
        var service = GetIntegrationService(provider);
        
        if (service == null)
        {
            result.Success = false;
            result.GlobalError = $"Serviço para provedor {provider} não encontrado";
            return result;
        }

        AnsiConsole.MarkupLine($"[yellow]📡 Sincronizando {integrations.Count} integração(ões) do {provider}[/]");

        var semaphore = new SemaphoreSlim(options.ConcurrentSyncs, options.ConcurrentSyncs);
        var tasks = integrations.Select(async integration =>
        {
            await semaphore.WaitAsync();
            try
            {
                return await SyncSingleIntegrationAsync(service, integration, options);
            }
            finally
            {
                semaphore.Release();
            }
        });

        var syncResults = await Task.WhenAll(tasks);
        
        foreach (var syncResult in syncResults)
        {
            if (syncResult.Success)
            {
                result.SuccessfulSyncs.Add(syncResult.Integration);
            }
            else
            {
                result.FailedSyncs.Add((syncResult.Integration, syncResult.Error));
            }
        }

        result.Success = result.SuccessfulSyncs.Any() || !result.FailedSyncs.Any();
        return result;
    }

    private async Task<(bool Success, ProviderIntegration Integration, string? Error)> SyncSingleIntegrationAsync(
        IIntegrationService service, 
        ProviderIntegration integration, 
        SyncOptions options)
    {
        try
        {
            if (!options.SkipTestConnection)
            {
                var connectionOk = await service.TestConnectionAsync(integration);
                if (!connectionOk)
                {
                    return (false, integration, "Falha na conexão");
                }
            }

            var syncSuccess = await service.SyncAsync(integration);
            return (syncSuccess, integration, syncSuccess ? null : "Falha no sync");
        }
        catch (Exception ex)
        {
            return (false, integration, ex.Message);
        }
    }

    private async Task UpdateSyncTimestampsAsync(List<ProviderIntegration> successfulSyncs)
    {
        if (successfulSyncs.Any())
        {
            var ids = successfulSyncs.Select(i => i.Id);
            await _integrationRepository.UpdateLastSyncBulkAsync(ids, DateTime.UtcNow);
        }
    }

    private int GetProviderPriority(string provider, SyncOptions options)
    {
        return options.ProviderPriorities.TryGetValue(provider, out var priority) ? priority : 
               provider switch
               {
                   "GitHub" => 1,
                   "AzureDevOps" => 2,
                   _ => 99
               };
    }

    private void DisplaySyncSummary(MultiProviderSyncResult result)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]📊 Resumo do Sync:[/]");
        
        if (result.SuccessfulSyncs.Any())
        {
            AnsiConsole.MarkupLine($"[green]✓ {result.SuccessfulSyncs.Count} integração(ões) sincronizada(s) com sucesso[/]");
        }
        
        if (result.FailedSyncs.Any())
        {
            AnsiConsole.MarkupLine($"[red]✗ {result.FailedSyncs.Count} integração(ões) falharam[/]");
            foreach (var (integration, error) in result.FailedSyncs)
            {
                AnsiConsole.MarkupLine($"  [red]• {integration.Provider} ({integration.OrganizationUrl}): {error}[/]");
            }
        }

        if (!string.IsNullOrEmpty(result.GlobalError))
        {
            AnsiConsole.MarkupLine($"[red]❌ Erro global: {result.GlobalError}[/]");
        }
    }

    #endregion

    /// <summary>
    /// Debug GitHub Issues - método para diagnosticar problemas de sincronização
    /// </summary>
    public async Task<string> DebugGitHubIssuesAsync()
    {
        try
        {
            var gitHubService = GetIntegrationService("GitHub");
            if (gitHubService == null)
            {
                return "❌ GitHub integration service not found";
            }

            if (gitHubService is GitHubIntegrationService githubIntegrationService)
            {
                return await githubIntegrationService.DebugIssuesAsync();
            }
            else
            {
                return "❌ Could not cast to GitHubIntegrationService";
            }
        }
        catch (Exception ex)
        {
            return $"❌ Debug failed: {ex.Message}";
        }
    }
}

/// <summary>
/// Opções para configurar o processo de sync
/// </summary>
public class SyncOptions
{
    /// <summary>
    /// Idade máxima do último sync para considerar uma integração como "devido para sync"
    /// </summary>
    public TimeSpan MaxAge { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Forçar sync mesmo se recentemente sincronizado
    /// </summary>
    public bool ForceSync { get; set; } = false;

    /// <summary>
    /// Número máximo de syncs concorrentes por provedor
    /// </summary>
    public int ConcurrentSyncs { get; set; } = 3;

    /// <summary>
    /// Pular teste de conexão antes do sync (mais rápido mas menos seguro)
    /// </summary>
    public bool SkipTestConnection { get; set; } = false;

    /// <summary>
    /// Tentar reconectar em caso de falha de conexão
    /// </summary>
    public bool RetryFailedConnections { get; set; } = true;

    /// <summary>
    /// Prioridades por provedor (menor número = maior prioridade)
    /// </summary>
    public Dictionary<string, int> ProviderPriorities { get; set; } = new();

    /// <summary>
    /// Timeout para cada operação de sync individual
    /// </summary>
    public TimeSpan SyncTimeout { get; set; } = TimeSpan.FromMinutes(5);
}

/// <summary>
/// Resultado de uma operação de sync para múltiplos providers
/// </summary>
public class MultiProviderSyncResult
{
    public bool Success { get; set; }
    public List<ProviderIntegration> SuccessfulSyncs { get; set; } = new();
    public List<(ProviderIntegration Integration, string? Error)> FailedSyncs { get; set; } = new();
    public string? GlobalError { get; set; }
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? EndTime { get; set; }

    public TimeSpan Duration => EndTime?.Subtract(StartTime) ?? DateTime.UtcNow.Subtract(StartTime);

    public void Merge(MultiProviderSyncResult other)
    {
        SuccessfulSyncs.AddRange(other.SuccessfulSyncs);
        FailedSyncs.AddRange(other.FailedSyncs);
        
        if (!other.Success && string.IsNullOrEmpty(GlobalError))
        {
            GlobalError = other.GlobalError;
        }
    }

    public void Complete()
    {
        EndTime = DateTime.UtcNow;
    }
}
