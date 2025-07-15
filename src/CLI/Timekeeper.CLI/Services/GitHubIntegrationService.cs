using Spectre.Console;
using Timekeeper.Domain.Entities;
using Timekeeper.Domain.Interfaces;
using Timekeeper.Infrastructure.DevOps.GitHub;
using Microsoft.Extensions.DependencyInjection;

namespace Timekeeper.CLI.Services;

public class GitHubIntegrationService : IIntegrationService
{
    private readonly IProvidersIntegrationRepository _integrationRepository;
    private readonly IGitHubAuthService _gitHubAuthService;
    private readonly Timekeeper.Infrastructure.DevOps.GitHub.GitHubService _gitHubService;
    private readonly IServiceProvider _serviceProvider;

    public string ProviderName => "GitHub";

    public GitHubIntegrationService(
        IProvidersIntegrationRepository integrationRepository,
        IGitHubAuthService gitHubAuthService,
        Timekeeper.Infrastructure.DevOps.GitHub.GitHubService gitHubService,
        IServiceProvider serviceProvider)
    {
        _integrationRepository = integrationRepository;
        _gitHubAuthService = gitHubAuthService;
        _gitHubService = gitHubService;
        _serviceProvider = serviceProvider;
    }

    // Interface IIntegrationService methods
    
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
                AnsiConsole.MarkupLine("[yellow]GitHub integration not found.[/]");
                return false;
            }

            await _integrationRepository.DeleteAsync(integrationId);
            AnsiConsole.MarkupLine("[green]GitHub integration removed successfully![/]");
            return true;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error removing GitHub integration: {ex.Message}[/]");
            return false;
        }
    }

    public async Task<bool> TestConnectionAsync(ProviderIntegration integration)
    {
        if (integration.Provider != ProviderName)
            return false;
            
        return await _gitHubAuthService.ValidateConnectionAsync(integration.OrganizationUrl, integration.PersonalAccessToken);
    }

    public async Task<bool> SyncAsync(ProviderIntegration integration)
    {
        if (integration.Provider != ProviderName)
            return false;
            
        try
        {
            AnsiConsole.MarkupLine($"[blue]Syncing GitHub integration: {integration.OrganizationUrl}[/]");
            
            // Get the GitHub sync service via service provider to avoid circular dependency
            var gitHubSyncService = _serviceProvider.GetService<GitHubSyncService>();
            if (gitHubSyncService != null)
            {
                var issues = await gitHubSyncService.SyncIssuesAsync();
                AnsiConsole.MarkupLine($"[green]Synchronized {issues.Count()} GitHub issues[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]GitHubSyncService not available[/]");
            }
            
            // Update last sync time
            integration.LastSyncAt = DateTime.UtcNow;
            await _integrationRepository.UpdateAsync(integration);
            
            return true;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error syncing GitHub integration: {ex.Message}[/]");
            return false;
        }
    }

    /// <summary>
    /// Gets the first active GitHub integration (for backward compatibility)
    /// </summary>
    public async Task<ProviderIntegration?> GetActiveGitHubIntegrationAsync()
    {
        var integrations = await GetActiveIntegrationsAsync();
        return integrations.FirstOrDefault();
    }

    public async Task<bool> TestConnectionAsync()
    {
        var integration = await GetActiveGitHubIntegrationAsync();
        if (integration == null)
        {
            AnsiConsole.MarkupLine("[red]No active GitHub integration found.[/]");
            return false;
        }

        return await _gitHubAuthService.ValidateConnectionAsync(integration.OrganizationUrl, integration.PersonalAccessToken);
    }

    public async Task<bool> TestConnectionAsync(string organizationUrl, string personalAccessToken)
    {
        return await _gitHubAuthService.ValidateConnectionAsync(organizationUrl, personalAccessToken);
    }

    public async Task<string> GetCurrentUserAsync(string organizationUrl, string personalAccessToken)
    {
        return await _gitHubAuthService.GetCurrentUserAsync(organizationUrl, personalAccessToken);
    }

    public async Task<IEnumerable<string>> GetRepositoriesAsync(string organizationUrl, string personalAccessToken)
    {
        return await _gitHubService.GetProjectsAsync(organizationUrl, personalAccessToken);
    }

    public async Task<bool> SaveIntegrationAsync(string organizationUrl, string personalAccessToken, string? repositoryName = null)
    {
        try
        {
            Console.WriteLine($"Attempting to save GitHub integration: OrgUrl={organizationUrl}, Repository={repositoryName}");
            
            // Validate connection first
            var isValid = await _gitHubAuthService.ValidateConnectionAsync(organizationUrl, personalAccessToken);
            if (!isValid)
            {
                Console.WriteLine("GitHub connection validation failed");
                return false;
            }

            // Deactivate all existing integrations
            Console.WriteLine("Deactivating existing integrations...");
            await _integrationRepository.DeactivateAllAsync();
            Console.WriteLine("Existing integrations deactivated.");

            // Create new GitHub integration
            var integration = new ProviderIntegration
            {
                Id = Guid.NewGuid(),
                Provider = "GitHub",
                OrganizationUrl = organizationUrl,
                PersonalAccessToken = personalAccessToken,
                ProjectName = repositoryName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            Console.WriteLine($"Adding new GitHub integration with ID: {integration.Id}");
            await _integrationRepository.AddAsync(integration);
            Console.WriteLine("GitHub integration saved successfully!");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving GitHub integration: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    public async Task<bool> ConfigureIntegrationAsync(string organizationUrl, string pat)
    {
        try
        {
            // Test connection first
            var isValid = await TestConnectionAsync(organizationUrl, pat);
            if (!isValid)
            {
                AnsiConsole.MarkupLine("[red]Failed to connect to GitHub.[/]");
                return false;
            }

            AnsiConsole.MarkupLine("[green]Connection successful![/]");

            // Get available repositories
            var repositories = await GetRepositoriesAsync(organizationUrl, pat);
            var repoList = repositories.ToList();

            string? selectedRepository = null;

            if (repoList.Any())
            {
                AnsiConsole.MarkupLine($"[grey]Found {repoList.Count} repositories[/]");
                
                var includeRepo = AnsiConsole.Confirm("Do you want to specify a repository?", false);
                if (includeRepo)
                {
                    var repoChoices = repoList.ToList();
                    repoChoices.Add("All Repositories");
                    
                    selectedRepository = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("Select a repository:")
                            .PageSize(10)
                            .AddChoices(repoChoices.ToArray()));
                            
                    if (selectedRepository == "All Repositories")
                        selectedRepository = null;
                }
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]No repositories found or insufficient permissions.[/]");
            }

            // Save integration
            var saved = await SaveIntegrationAsync(organizationUrl, pat, selectedRepository);
            
            if (saved)
            {
                if (!string.IsNullOrEmpty(selectedRepository))
                {
                    AnsiConsole.MarkupLine($"[green]GitHub integration configured for repository: {selectedRepository}[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine("[green]GitHub integration configured for all repositories![/]");
                }
            }
            else
            {
                AnsiConsole.MarkupLine("[red]Failed to save GitHub integration.[/]");
            }

            return saved;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error configuring GitHub integration: {ex.Message}[/]");
            return false;
        }
    }

    public async Task<bool> RemoveIntegrationAsync()
    {
        try
        {
            var integration = await GetActiveGitHubIntegrationAsync();
            if (integration == null)
            {
                AnsiConsole.MarkupLine("[yellow]No active GitHub integration found.[/]");
                return false;
            }

            await _integrationRepository.DeleteAsync(integration.Id);
            AnsiConsole.MarkupLine("[green]GitHub integration removed successfully![/]");
            return true;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error removing GitHub integration: {ex.Message}[/]");
            return false;
        }
    }

    public async Task<IEnumerable<object>> GetIssuesAsync(string? repositoryName = null)
    {
        var integration = await GetActiveGitHubIntegrationAsync();
        if (integration == null)
        {
            throw new InvalidOperationException("No active GitHub integration found");
        }

        return await _gitHubService.GetWorkItemsAsync(
            integration.OrganizationUrl,
            integration.PersonalAccessToken,
            repositoryName ?? integration.ProjectName);
    }

    // Interface IIntegrationService methods
    
    public async Task<bool> ConfigureIntegrationAsync()
    {
        try
        {
            AnsiConsole.MarkupLine("[bold blue]Setting up GitHub Integration[/]");
            AnsiConsole.WriteLine();

            var organizationUrl = AnsiConsole.Ask<string>("[green]Enter your GitHub organization URL or username:[/]");
            var personalAccessToken = AnsiConsole.Prompt(
                new TextPrompt<string>("[green]Enter your Personal Access Token:[/]")
                    .Secret());

            return await ConfigureIntegrationAsync(organizationUrl, personalAccessToken);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error configuring GitHub integration: {ex.Message}[/]");
            return false;
        }
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

    public async Task<string> DebugIssuesAsync()
    {
        var integration = await GetActiveGitHubIntegrationAsync();
        if (integration == null)
        {
            return "❌ No active GitHub integration found.";
        }

        try
        {
            return await _gitHubService.DebugIssuesAsync(
                integration.OrganizationUrl,
                integration.PersonalAccessToken,
                integration.ProjectName);
        }
        catch (Exception ex)
        {
            return $"❌ Debug failed: {ex.Message}";
        }
    }
}
