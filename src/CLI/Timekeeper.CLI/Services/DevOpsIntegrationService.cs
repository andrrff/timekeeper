using Spectre.Console;
using Timekeeper.Domain.Entities;
using Timekeeper.Domain.Interfaces;

namespace Timekeeper.CLI.Services;

public class ProviderIntegrationService : IDevOpsService
{
    private readonly IProvidersIntegrationRepository _integrationRepository;
    private readonly Timekeeper.Domain.Interfaces.IDevOpsService _devOpsService;

    public ProviderIntegrationService(
        IProvidersIntegrationRepository integrationRepository,
        Timekeeper.Domain.Interfaces.IDevOpsService devOpsService)
    {
        _integrationRepository = integrationRepository;
        _devOpsService = devOpsService;
    }

    public async Task<ProviderIntegration?> GetActiveIntegrationAsync()
    {
        var integrations = await _integrationRepository.GetActiveByProviderAsync("AzureDevOps");
        return integrations.FirstOrDefault();
    }

    public async Task<bool> TestConnectionAsync()
    {
        var integration = await GetActiveIntegrationAsync();
        if (integration == null)
        {
            AnsiConsole.MarkupLine("[red]No active DevOps integration found.[/]");
            return false;
        }

        return await _devOpsService.TestConnectionAsync(integration.OrganizationUrl, integration.PersonalAccessToken);
    }

    public async Task<bool> TestConnectionAsync(string organizationUrl, string personalAccessToken)
    {
        return await _devOpsService.TestConnectionAsync(organizationUrl, personalAccessToken);
    }

    public async Task<IEnumerable<string>> GetProjectsAsync(string organizationUrl, string personalAccessToken)
    {
        return await _devOpsService.GetProjectsAsync(organizationUrl, personalAccessToken);
    }

    public async Task<bool> ConfigureIntegrationAsync(string organizationUrl, string pat)
    {
        try
        {
            AnsiConsole.Status()
                .Spinner(Spinner.Known.Star)
                .Start("Testing connection...", ctx =>
                {
                    // Test connection first
                    var isValid = _devOpsService.TestConnectionAsync(organizationUrl, pat).Result;
                    if (!isValid)
                    {
                        AnsiConsole.MarkupLine("[red]Failed to connect to DevOps.[/]");
                        return;
                    }

                    AnsiConsole.MarkupLine("[green]Connection successful![/]");

                    // Get available projects
                    var projects = _devOpsService.GetProjectsAsync(organizationUrl, pat).Result;
                    var projectList = projects.ToList();

                    if (projectList.Any())
                    {
                        var selectedProject = AnsiConsole.Prompt(
                            new SelectionPrompt<string>()
                                .Title("Select a project:")
                                .PageSize(10)
                                .AddChoices(projectList));

                        // Save integration
                        SaveIntegrationAsync("Azure DevOps", organizationUrl, pat, selectedProject).Wait();
                        AnsiConsole.MarkupLine($"[green]Integration configured for project: {selectedProject}[/]");
                    }
                    else
                    {
                        // Save integration without project
                        SaveIntegrationAsync("Azure DevOps", organizationUrl, pat).Wait();
                        AnsiConsole.MarkupLine("[green]Integration configured![/]");
                    }
                });

            return true;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error configuring integration: {ex.Message}[/]");
            return false;
        }
    }

    public async Task<bool> SaveIntegrationAsync(string provider, string organizationUrl, string personalAccessToken, string? projectName = null)
    {
        try
        {
            Console.WriteLine($"Attempting to save integration: Provider={provider}, OrgUrl={organizationUrl}, ProjectName={projectName}");
            
            // Don't test connection here since it was already validated before calling this method
            // Different providers have different validation logic
            
            // Deactivate all existing integrations
            Console.WriteLine("Deactivating existing integrations...");
            await _integrationRepository.DeactivateAllAsync();
            Console.WriteLine("Existing integrations deactivated.");

            // Create new integration
            var integration = new ProviderIntegration
            {
                Id = Guid.NewGuid(),
                Provider = provider,
                OrganizationUrl = organizationUrl,
                PersonalAccessToken = personalAccessToken,
                ProjectName = projectName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            Console.WriteLine($"Adding new integration with ID: {integration.Id}");
            await _integrationRepository.AddAsync(integration);
            Console.WriteLine("Integration saved successfully!");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving integration: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    public async Task<IEnumerable<object>> SyncWorkItemsAsync()
    {
        var integration = await GetActiveIntegrationAsync();
        if (integration == null)
        {
            AnsiConsole.MarkupLine("[red]No active DevOps integration found.[/]");
            return new List<object>();
        }

        try
        {
            var workItems = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Star)
                .StartAsync("Syncing work items...", async ctx =>
                {
                    return await _devOpsService.GetWorkItemsAsync(
                        integration.OrganizationUrl,
                        integration.PersonalAccessToken,
                        integration.ProjectName);
                });

            // Update last sync time
            integration.LastSyncAt = DateTime.UtcNow;
            await _integrationRepository.UpdateAsync(integration);

            // Display results
            var workItemList = workItems.ToList();
            AnsiConsole.MarkupLine($"[green]Successfully synced {workItemList.Count} work items.[/]");

            if (workItemList.Any())
            {
                var table = new Table();
                table.AddColumn("ID");
                table.AddColumn("Title");
                table.AddColumn("Type");
                table.AddColumn("State");

                foreach (var item in workItemList.Take(10))
                {
                    try
                    {
                        // Use reflection to access anonymous object properties
                        var objectType = item.GetType();
                        var idProp = objectType.GetProperty("Id");
                        var titleProp = objectType.GetProperty("Title");
                        var typeProp = objectType.GetProperty("WorkItemType");
                        var stateProp = objectType.GetProperty("State");

                        string[] rowData = {
                            idProp?.GetValue(item)?.ToString() ?? "-",
                            titleProp?.GetValue(item)?.ToString() ?? "-",
                            typeProp?.GetValue(item)?.ToString() ?? "-",
                            stateProp?.GetValue(item)?.ToString() ?? "-"
                        };
                        table.AddRow(rowData);
                    }
                    catch (Exception itemEx)
                    {
                        // Skip items that can't be processed but don't fail the whole sync
                        table.AddRow("-", $"Error: {itemEx.Message}", "-", "-");
                    }
                }

                AnsiConsole.Write(table);

                if (workItemList.Count > 10)
                {
                    AnsiConsole.MarkupLine($"[yellow]... and {workItemList.Count - 10} more items.[/]");
                }
            }

            return workItems;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error syncing work items: {ex.Message}[/]");
            return new List<object>();
        }
    }

    public async Task ShowStatusAsync()
    {
        var integration = await GetActiveIntegrationAsync();
        
        var panel = new Panel("[bold blue]DevOps Integration Status[/]")
        {
            Border = BoxBorder.Rounded
        };
        AnsiConsole.Write(panel);

        if (integration == null)
        {
            AnsiConsole.MarkupLine("[red]❌ No active DevOps integration configured.[/]");
            AnsiConsole.MarkupLine("[yellow]Use 'tk devops config' to set up integration.[/]");
            return;
        }

        var table = new Table();
        table.AddColumn("Property");
        table.AddColumn("Value");

        table.AddRow("✅ Status", "[green]Active[/]");
        table.AddRow("🏢 Provider", integration.Provider);
        table.AddRow("🔗 Organization", integration.OrganizationUrl);
        table.AddRow("📁 Project", integration.ProjectName ?? "[grey]All Projects[/]");
        table.AddRow("📅 Configured", integration.CreatedAt.ToString("yyyy-MM-dd HH:mm"));
        table.AddRow("🔄 Last Sync", integration.LastSyncAt?.ToString("yyyy-MM-dd HH:mm") ?? "[grey]Never[/]");

        AnsiConsole.Write(table);

        // Test connection
        AnsiConsole.WriteLine();
        var connectionStatus = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Star)
            .StartAsync("Testing connection...", async ctx =>
            {
                return await _devOpsService.TestConnectionAsync(integration.OrganizationUrl, integration.PersonalAccessToken);
            });

        if (connectionStatus)
        {
            AnsiConsole.MarkupLine("[green]✅ Connection test successful![/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[red]❌ Connection test failed. Check your configuration.[/]");
        }
    }

    public async Task<bool> RemoveIntegrationAsync()
    {
        try
        {
            await _integrationRepository.DeactivateAllAsync();
            AnsiConsole.MarkupLine("[green]DevOps integration removed successfully.[/]");
            return true;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error removing integration: {ex.Message}[/]");
            return false;
        }
    }

    public async Task<object?> GetWorkItemByIdAsync(int workItemId)
    {
        var integration = await GetActiveIntegrationAsync();
        if (integration == null)
            return null;

        return await _devOpsService.GetWorkItemByIdAsync(
            integration.OrganizationUrl,
            integration.PersonalAccessToken,
            workItemId);
    }
}
