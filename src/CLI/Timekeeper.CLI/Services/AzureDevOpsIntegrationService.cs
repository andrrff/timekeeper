using Spectre.Console;
using Timekeeper.Domain.Entities;
using Timekeeper.Domain.Interfaces;
using Timekeeper.Infrastructure.DevOps.AzureDevOps;
using Microsoft.Extensions.DependencyInjection;

namespace Timekeeper.CLI.Services;

public class AzureProviderIntegrationService : IIntegrationService
{
    private readonly IProvidersIntegrationRepository _integrationRepository;
    private readonly IAzureDevOpsAuthService _devOpsAuthService;
    private readonly IDevOpsService _devOpsService;
    private readonly IServiceProvider _serviceProvider;

    public string ProviderName => "AzureDevOps";

    public AzureProviderIntegrationService(
        IProvidersIntegrationRepository integrationRepository,
        IAzureDevOpsAuthService devOpsAuthService,
        IDevOpsService devOpsService,
        IServiceProvider serviceProvider)
    {
        _integrationRepository = integrationRepository;
        _devOpsAuthService = devOpsAuthService;
        _devOpsService = devOpsService;
        _serviceProvider = serviceProvider;
    }

    public async Task<bool> ConfigureIntegrationAsync()
    {
        try
        {
            AnsiConsole.MarkupLine("[bold blue]Setting up Azure DevOps Integration[/]");
            AnsiConsole.WriteLine();

            var organizationUrl = AnsiConsole.Ask<string>("[green]Enter your Azure DevOps organization URL:[/]");
            var personalAccessToken = AnsiConsole.Prompt(
                new TextPrompt<string>("[green]Enter your Personal Access Token:[/]")
                    .Secret());

            AnsiConsole.MarkupLine("[dim]Testing connection...[/]");
            
            bool isValid = await TestConnectionAsync(organizationUrl, personalAccessToken);
            if (!isValid)
            {
                AnsiConsole.MarkupLine("[red]❌ Connection failed. Please check your credentials.[/]");
                return false;
            }

            var projectName = AnsiConsole.Ask<string>("[green]Enter project name (or press Enter for all projects):[/]", string.Empty);
            if (string.IsNullOrWhiteSpace(projectName))
            {
                projectName = null;
            }

            // Save integration
            var integration = new ProviderIntegration
            {
                Id = Guid.NewGuid(),
                Provider = ProviderName,
                OrganizationUrl = organizationUrl,
                PersonalAccessToken = personalAccessToken,
                ProjectName = projectName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _integrationRepository.AddAsync(integration);

            AnsiConsole.MarkupLine("[green]✓ Azure DevOps integration configured successfully![/]");
            AnsiConsole.MarkupLine("[blue]You can now sync Azure DevOps work items with your tasks![/]");

            return true;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error configuring Azure DevOps integration: {ex.Message}[/]");
            return false;
        }
    }

    public async Task<bool> TestConnectionAsync()
    {
        var integration = await GetActiveProviderIntegrationAsync();
        if (integration == null)
        {
            AnsiConsole.MarkupLine("[yellow]No active Azure DevOps integration found.[/]");
            return false;
        }
        
        return await TestConnectionAsync(integration);
    }

    public async Task SyncAsync()
    {
        var integrations = await GetActiveIntegrationsAsync();
        foreach (var integration in integrations)
        {
            await SyncAsync(integration);
        }
    }

    public async Task<IEnumerable<ProviderIntegration>> GetConfiguredIntegrationsAsync()
    {
        return await GetActiveIntegrationsAsync();
    }

    public async Task<bool> TestConnectionAsync(ProviderIntegration integration)
    {
        if (integration.Provider != ProviderName)
            return false;
            
        return await _devOpsAuthService.ValidateConnectionAsync(integration.OrganizationUrl, integration.PersonalAccessToken);
    }

    public async Task<bool> TestConnectionAsync(string organizationUrl, string personalAccessToken)
    {
        return await _devOpsAuthService.ValidateConnectionAsync(organizationUrl, personalAccessToken);
    }

    public async Task<IEnumerable<ProviderIntegration>> GetActiveIntegrationsAsync()
    {
        return await _integrationRepository.GetActiveByProviderAsync(ProviderName);
    }

    public async Task<ProviderIntegration?> GetIntegrationByIdAsync(Guid integrationId)
    {
        var integration = await _integrationRepository.GetByIdAsync(integrationId);
        return integration?.Provider == ProviderName ? integration : null;
    }

    public async Task<bool> RemoveIntegrationAsync(Guid integrationId)
    {
        try
        {
            var integration = await GetIntegrationByIdAsync(integrationId);
            if (integration == null)
            {
                AnsiConsole.MarkupLine("[yellow]Azure DevOps integration not found.[/]");
                return false;
            }

            await _integrationRepository.DeleteAsync(integrationId);
            AnsiConsole.MarkupLine("[green]Azure DevOps integration removed successfully![/]");
            return true;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error removing Azure DevOps integration: {ex.Message}[/]");
            return false;
        }
    }

    public async Task<bool> SyncAsync(ProviderIntegration integration)
    {
        if (integration.Provider != ProviderName)
            return false;
            
        try
        {
            AnsiConsole.MarkupLine($"[blue]Syncing Azure DevOps integration: {integration.OrganizationUrl}[/]");
            
            // Get the DevOps sync service via service provider to avoid circular dependency
            var devOpsSyncService = _serviceProvider.GetService<DevOpsSyncService>();
            if (devOpsSyncService != null)
            {
                var syncResult = await devOpsSyncService.SyncWorkItemsToTodosAsync();
                
                if (syncResult.IsSuccess)
                {
                    AnsiConsole.MarkupLine($"[green]{syncResult.Message}[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[yellow]{syncResult.Message}[/]");
                }
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]DevOpsSyncService not available[/]");
            }
            
            // Update last sync time
            integration.LastSyncAt = DateTime.UtcNow;
            await _integrationRepository.UpdateAsync(integration);
            
            return true;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error syncing Azure DevOps integration: {ex.Message}[/]");
            return false;
        }
    }

    /// <summary>
    /// Gets the first active Azure DevOps integration (for backward compatibility)
    /// </summary>
    public async Task<ProviderIntegration?> GetActiveProviderIntegrationAsync()
    {
        var integrations = await GetActiveIntegrationsAsync();
        return integrations.FirstOrDefault();
    }

    // TODO: Implement when DevOpsService has GetWorkItemsAsync method
    /*
    public async Task<IEnumerable<object>> GetWorkItemsAsync(string? projectName = null)
    {
        var integration = await GetActiveProviderIntegrationAsync();
        if (integration == null)
        {
            throw new InvalidOperationException("No active Azure DevOps integration found");
        }

        return await _devOpsService.GetWorkItemsAsync(
            integration.OrganizationUrl,
            integration.PersonalAccessToken,
            projectName ?? integration.ProjectName);
    }
    */
}
