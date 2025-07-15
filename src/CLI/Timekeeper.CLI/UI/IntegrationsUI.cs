using Spectre.Console;
using Timekeeper.CLI.Services;

namespace Timekeeper.CLI.UI;

public class IntegrationsUI
{
    private readonly IntegrationManager _integrationManager;

    public IntegrationsUI(IntegrationManager integrationManager)
    {
        _integrationManager = integrationManager;
    }

    public async Task ShowAsync()
    {
        var menuItems = new List<(string key, string icon, string value, string description)>
        {
            ("1", "🆕", "add_integration", "Add New Integration - Connect a new provider to your workflow"),
            ("2", "📊", "view_all", "View All Integrations - See all your connected providers"),
            ("3", "📈", "statistics", "Provider Statistics - View detailed analytics and metrics"),
            ("4", "✅", "test_all", "Test All Connections - Verify connectivity to all providers"),
            ("5", "🔄", "smart_sync", "Smart Sync - Intelligent synchronization across providers"),
            ("6", "⚡", "emergency_sync", "Emergency Sync - Force sync all integrations immediately"),
            ("7", "🎯", "sync_provider", "Sync by Provider - Synchronize specific provider only"),
            ("8", "🔧", "sync_specific", "Sync Specific Integrations - Choose exact integrations to sync"),
            ("9", "⚙️", "manage_specific", "Manage Specific Integration - Configure individual integration"),
            ("r", "🗑️", "remove", "Remove Integration - Disconnect a provider permanently"),
            ("d", "🐛", "debug", "Debug GitHub Issues - Troubleshoot GitHub connectivity"),
            ("0", "⬅️", "back", "Back to Main Menu - Return to the previous screen")
        };

        while (true)
        {
            var choice = await ShowInteractiveMenuAsync("🔗 Integration Manager", menuItems, "Connect and synchronize with external providers");
            
            switch (choice)
            {
                case "add_integration":
                    await AddNewIntegrationAsync();
                    break;
                case "view_all":
                    await ShowAllIntegrationsAsync();
                    break;
                case "statistics":
                    await ShowProviderStatisticsAsync();
                    break;
                case "test_all":
                    await TestAllConnectionsAsync();
                    break;
                case "smart_sync":
                    await ExecuteSmartSyncAsync();
                    break;
                case "emergency_sync":
                    await ExecuteEmergencySyncAsync();
                    break;
                case "sync_provider":
                    await SyncByProviderAsync();
                    break;
                case "sync_specific":
                    await SyncSpecificIntegrationsAsync();
                    break;
                case "manage_specific":
                    await ManageSpecificIntegrationAsync();
                    break;
                case "remove":
                    await RemoveIntegrationAsync();
                    break;
                case "debug":
                    await DebugGitHubIssuesAsync();
                    break;
                case "back":
                    return;
            }
        }
    }

    private async Task AddNewIntegrationAsync()
    {
        Console.Clear();
        
        // Header with style
        var rule = new Rule("[bold green]Add New Integration[/]")
        {
            Style = Style.Parse("green"),
            Justification = Justify.Center
        };
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();

        var providers = _integrationManager.GetAvailableProviders().ToList();
        
        if (!providers.Any())
        {
            var errorPanel = new Panel("[red]No integration providers available.[/]")
            {
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Red),
                Header = new PanelHeader(" ❌ Error "),
                Padding = new Padding(1)
            };
            AnsiConsole.Write(errorPanel);
            
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
            Console.ReadKey();
            return;
        }

        var providerChoices = providers.Select(p => $"{GetProviderIcon(p)} {p}").ToList();
        providerChoices.Add("⬅️ Back");

        var selectedProvider = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold blue]Select integration provider:[/]")
                .PageSize(10)
                .HighlightStyle(new Style(Color.Blue))
                .AddChoices(providerChoices));

        if (selectedProvider == "⬅️ Back")
            return;

        // Extract the actual provider name (remove icon)
        var actualProvider = providers[providerChoices.IndexOf(selectedProvider)];

        AnsiConsole.WriteLine();
        
        // Show loading
        bool success = false;
        await AnsiConsole.Status()
            .StartAsync("Configuring integration...", async ctx =>
            {
                success = await _integrationManager.ConfigureIntegrationAsync(actualProvider);
            });
        
        // Show result
        var resultPanel = new Panel(success 
            ? "[green]✓ Integration configured successfully![/]" 
            : "[red]❌ Failed to configure integration.[/]")
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(success ? Color.Green : Color.Red),
            Header = new PanelHeader(success ? " ✅ Success " : " ❌ Error "),
            Padding = new Padding(1)
        };
        AnsiConsole.Write(resultPanel);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
        Console.ReadKey();
    }

    private async Task ShowAllIntegrationsAsync()
    {
        Console.Clear();
        
        // Header with style
        var rule = new Rule("[bold blue]All Integrations Overview[/]")
        {
            Style = Style.Parse("blue"),
            Justification = Justify.Center
        };
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();

        // Show loading
        await AnsiConsole.Status()
            .StartAsync("Loading integrations...", async ctx =>
            {
                await _integrationManager.ShowIntegrationsStatusAsync();
            });

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
        Console.ReadKey();
    }

    private async Task TestAllConnectionsAsync()
    {
        Console.Clear();
        
        // Header with style
        var rule = new Rule("[bold green]Testing All Connections[/]")
        {
            Style = Style.Parse("green"),
            Justification = Justify.Center
        };
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();

        // Show testing progress
        await AnsiConsole.Status()
            .StartAsync("Testing connections to all providers...", async ctx =>
            {
                await _integrationManager.TestAllConnectionsAsync();
            });

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
        Console.ReadKey();
    }

    private async Task SyncAllIntegrationsAsync()
    {
        Console.Clear();
        AnsiConsole.MarkupLine("[bold blue]Syncing All Integrations[/]");
        AnsiConsole.WriteLine();

        await _integrationManager.SyncAllIntegrationsAsync();

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("Press any key to continue...");
        Console.ReadKey();
    }

    private async Task ManageSpecificIntegrationAsync()
    {
        Console.Clear();
        
        // Header with style
        var rule = new Rule("[bold blue]Manage Specific Integration[/]")
        {
            Style = Style.Parse("blue"),
            Justification = Justify.Center
        };
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();

        var integrations = await _integrationManager.GetAllActiveIntegrationsAsync();
        var allIntegrations = integrations.ToList();

        if (!allIntegrations.Any())
        {
            var warningPanel = new Panel("[yellow]No integrations found.[/]\n\n[dim]Add some integrations first to manage them.[/]")
            {
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Yellow),
                Header = new PanelHeader(" ⚠️ No Integrations "),
                Padding = new Padding(1)
            };
            AnsiConsole.Write(warningPanel);
            
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
            Console.ReadKey();
            return;
        }

        var integrationChoices = allIntegrations.Select(i => 
            $"{GetProviderIcon(i.Provider)} {i.Provider}: {i.OrganizationUrl} ({i.ProjectName ?? "All projects"})").ToList();
        integrationChoices.Add("⬅️ Back");

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold blue]Select integration to manage:[/]")
                .PageSize(10)
                .HighlightStyle(new Style(Color.Blue))
                .AddChoices(integrationChoices));

        if (selected == "⬅️ Back")
            return;

        var selectedIndex = integrationChoices.IndexOf(selected);
        var selectedIntegration = allIntegrations[selectedIndex];

        await ManageIntegrationAsync(selectedIntegration.Provider, selectedIntegration.Id);
    }

    private async Task ManageIntegrationAsync(string provider, Guid integrationId)
    {
        var menuItems = new List<(string key, string icon, string value, string description)>
        {
            ("1", "✅", "test", "Test Connection - Verify connectivity to this provider"),
            ("2", "🔄", "sync", "Sync Now - Synchronize data with this provider"),
            ("3", "📝", "details", "View Details - Show integration configuration and status"),
            ("4", "🗑️", "remove", "Remove Integration - Disconnect this provider permanently"),
            ("0", "⬅️", "back", "Back to Integration List - Return to previous menu")
        };

        while (true)
        {
            var choice = await ShowInteractiveMenuAsync($"🔗 Manage {provider} Integration", menuItems, "Configure and control this specific integration");
            
            var service = _integrationManager.GetIntegrationService(provider);
            if (service == null)
            {
                var errorPanel = new Panel("[red]Integration service not found.[/]")
                {
                    Border = BoxBorder.Rounded,
                    BorderStyle = new Style(Color.Red),
                    Header = new PanelHeader(" ❌ Error "),
                    Padding = new Padding(1)
                };
                AnsiConsole.Write(errorPanel);
                
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
                Console.ReadKey();
                break;
            }

            switch (choice)
            {
                case "test":
                    await TestIntegrationConnectionAsync(service, integrationId);
                    break;

                case "sync":
                    await SyncIntegrationAsync(service, integrationId);
                    break;

                case "details":
                    await ShowIntegrationDetailsAsync(service, integrationId);
                    break;

                case "remove":
                    var shouldRemove = await ConfirmRemoveIntegrationAsync(provider);
                    if (shouldRemove)
                    {
                        await RemoveIntegrationAsync(service, integrationId);
                        return; // Exit this menu after removal
                    }
                    break;

                case "back":
                    return;
            }
        }
    }

    private async Task TestIntegrationConnectionAsync(IIntegrationService service, Guid integrationId)
    {
        Console.Clear();
        
        var rule = new Rule("[bold green]Testing Connection[/]")
        {
            Style = Style.Parse("green"),
            Justification = Justify.Center
        };
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();

        var integration = await service.GetIntegrationByIdAsync(integrationId);
        if (integration != null)
        {
            bool isConnected = false;
            
            await AnsiConsole.Status()
                .StartAsync("Testing connection...", async ctx =>
                {
                    isConnected = await service.TestConnectionAsync(integration);
                });

            var resultPanel = new Panel(isConnected 
                ? "[green]✓ Connection successful![/]" 
                : "[red]✗ Connection failed![/]")
            {
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(isConnected ? Color.Green : Color.Red),
                Header = new PanelHeader(isConnected ? " ✅ Success " : " ❌ Failed "),
                Padding = new Padding(1)
            };
            AnsiConsole.Write(resultPanel);
        }
        else
        {
            var errorPanel = new Panel("[red]Integration not found.[/]")
            {
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Red),
                Header = new PanelHeader(" ❌ Error "),
                Padding = new Padding(1)
            };
            AnsiConsole.Write(errorPanel);
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
        Console.ReadKey();
    }

    private async Task SyncIntegrationAsync(IIntegrationService service, Guid integrationId)
    {
        Console.Clear();
        
        var rule = new Rule("[bold blue]Synchronizing Integration[/]")
        {
            Style = Style.Parse("blue"),
            Justification = Justify.Center
        };
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();

        var integration = await service.GetIntegrationByIdAsync(integrationId);
        if (integration != null)
        {
            await AnsiConsole.Status()
                .StartAsync("Synchronizing data...", async ctx =>
                {
                    await service.SyncAsync(integration);
                });

            var successPanel = new Panel("[green]✓ Synchronization completed![/]")
            {
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Green),
                Header = new PanelHeader(" ✅ Sync Complete "),
                Padding = new Padding(1)
            };
            AnsiConsole.Write(successPanel);
        }
        else
        {
            var errorPanel = new Panel("[red]Integration not found.[/]")
            {
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Red),
                Header = new PanelHeader(" ❌ Error "),
                Padding = new Padding(1)
            };
            AnsiConsole.Write(errorPanel);
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
        Console.ReadKey();
    }

    private async Task<bool> ConfirmRemoveIntegrationAsync(string provider)
    {
        Console.Clear();
        
        var rule = new Rule("[bold red]Remove Integration[/]")
        {
            Style = Style.Parse("red"),
            Justification = Justify.Center
        };
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();

        var warningPanel = new Panel($"[red]⚠️ Warning![/]\n\n" +
                                   $"You are about to permanently remove the [bold]{provider}[/] integration.\n" +
                                   $"This action cannot be undone and will disconnect the provider.\n\n" +
                                   $"[dim]All sync history will be preserved, but you will need to reconfigure\n" +
                                   $"the integration if you want to reconnect later.[/]")
        {
            Border = BoxBorder.Double,
            BorderStyle = new Style(Color.Red),
            Header = new PanelHeader(" 🗑️ Confirm Removal "),
            Padding = new Padding(1)
        };
        AnsiConsole.Write(warningPanel);

        AnsiConsole.WriteLine();
        return AnsiConsole.Confirm($"[red]Are you sure you want to remove the {provider} integration?[/]");
    }

    private async Task RemoveIntegrationAsync(IIntegrationService service, Guid integrationId)
    {
        Console.Clear();
        
        var rule = new Rule("[bold red]Removing Integration[/]")
        {
            Style = Style.Parse("red"),
            Justification = Justify.Center
        };
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();

        await AnsiConsole.Status()
            .StartAsync("Removing integration...", async ctx =>
            {
                await service.RemoveIntegrationAsync(integrationId);
            });

        var successPanel = new Panel("[green]✓ Integration removed successfully![/]")
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Green),
            Header = new PanelHeader(" ✅ Removed "),
            Padding = new Padding(1)
        };
        AnsiConsole.Write(successPanel);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
        Console.ReadKey();
    }

    private async Task ShowIntegrationDetailsAsync(IIntegrationService service, Guid integrationId)
    {
        Console.Clear();
        
        var rule = new Rule("[bold blue]Integration Details[/]")
        {
            Style = Style.Parse("blue"),
            Justification = Justify.Center
        };
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();

        var integration = await service.GetIntegrationByIdAsync(integrationId);
        if (integration == null)
        {
            var errorPanel = new Panel("[red]Integration not found.[/]")
            {
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Red),
                Header = new PanelHeader(" ❌ Error "),
                Padding = new Padding(1)
            };
            AnsiConsole.Write(errorPanel);
            
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
            Console.ReadKey();
            return;
        }

        // Create styled details table
        var table = new Table()
        {
            Border = TableBorder.Rounded,
            BorderStyle = new Style(Color.Blue)
        };
        
        table.AddColumn(new TableColumn("Property").Centered());
        table.AddColumn(new TableColumn("Value").LeftAligned());

        table.AddRow("[bold]Provider[/]", $"{GetProviderIcon(integration.Provider)} {integration.Provider}");
        table.AddRow("[bold]Organization/URL[/]", $"[link]{integration.OrganizationUrl}[/]");
        table.AddRow("[bold]Project[/]", integration.ProjectName ?? "[dim italic]All projects[/]");
        table.AddRow("[bold]Status[/]", integration.IsActive ? "[green]✓ Active[/]" : "[red]✗ Inactive[/]");
        table.AddRow("[bold]Created[/]", $"[yellow]{integration.CreatedAt:dd/MM/yyyy HH:mm}[/]");
        table.AddRow("[bold]Last Sync[/]", integration.LastSyncAt?.ToString("dd/MM/yyyy HH:mm") ?? "[dim italic]Never synchronized[/]");

        var detailsPanel = new Panel(table)
        {
            Header = new PanelHeader($" 📊 {integration.Provider} Integration Details "),
            Border = BoxBorder.Double,
            BorderStyle = new Style(Color.Blue),
            Padding = new Padding(1)
        };

        AnsiConsole.Write(detailsPanel);
        
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
        Console.ReadKey();
    }

    private async Task RemoveIntegrationAsync()
    {
        Console.Clear();
        AnsiConsole.MarkupLine("[bold red]Remove Integration[/]");
        AnsiConsole.WriteLine();

        var integrations = await _integrationManager.GetAllActiveIntegrationsAsync();
        var allIntegrations = integrations.ToList();

        if (!allIntegrations.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No integrations found.[/]");
            AnsiConsole.MarkupLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        var choices = allIntegrations.Select(i => 
            $"{i.Provider}: {i.OrganizationUrl} ({i.ProjectName ?? "All projects"})").ToList();
        choices.Add("⬅️ Back");

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select integration to remove:")
                .AddChoices(choices));

        if (selected == "⬅️ Back")
            return;

        var selectedIndex = choices.IndexOf(selected);
        var selectedIntegration = allIntegrations[selectedIndex];

        var confirm = AnsiConsole.Confirm($"Are you sure you want to remove the {selectedIntegration.Provider} integration?");
        if (confirm)
        {
            await _integrationManager.RemoveIntegrationAsync(selectedIntegration.Id);
        }

        AnsiConsole.MarkupLine("Press any key to continue...");
        Console.ReadKey();
    }

    private async Task ShowProviderStatisticsAsync()
    {
        AnsiConsole.MarkupLine("[bold blue]📈 Provider Statistics[/]");
        AnsiConsole.WriteLine();

        var statistics = await _integrationManager.GetProviderStatisticsAsync();

        if (!statistics.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No active integrations found.[/]");
        }
        else
        {
            var table = new Table();
            table.AddColumn("Provider");
            table.AddColumn("Active Integrations");

            foreach (var (provider, count) in statistics)
            {
                table.AddRow(provider, count.ToString());
            }

            AnsiConsole.Write(table);
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("Press any key to continue...");
        Console.ReadKey();
    }

    private async Task ExecuteSmartSyncAsync()
    {
        AnsiConsole.MarkupLine("[bold blue]🔄 Smart Sync Configuration[/]");
        AnsiConsole.WriteLine();

        var forceSync = AnsiConsole.Confirm("Force sync even for recently synced integrations?");
        var maxAgeHours = AnsiConsole.Ask<int>("Maximum hours since last sync (default 1):", 1);
        var concurrentSyncs = AnsiConsole.Ask<int>("Maximum concurrent syncs per provider (default 3):", 3);
        var skipConnectionTest = AnsiConsole.Confirm("Skip connection test (faster but less safe)?");

        var options = new SyncOptions
        {
            ForceSync = forceSync,
            MaxAge = TimeSpan.FromHours(maxAgeHours),
            ConcurrentSyncs = concurrentSyncs,
            SkipTestConnection = skipConnectionTest
        };

        await _integrationManager.ExecuteSmartSyncAsync(options);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("Press any key to continue...");
        Console.ReadKey();
    }

    private async Task ExecuteEmergencySyncAsync()
    {
        var confirm = AnsiConsole.Confirm("[bold red]⚠️ Execute Emergency Sync? This will sync all integrations immediately.[/]");
        
        if (confirm)
        {
            await _integrationManager.ExecuteEmergencySyncAsync();
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("Press any key to continue...");
        Console.ReadKey();
    }

    private async Task SyncByProviderAsync()
    {
        var providers = _integrationManager.GetAvailableProviders().ToList();
        
        if (!providers.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No providers available.[/]");
            AnsiConsole.MarkupLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        providers.Add("⬅️ Back");

        var selectedProvider = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select provider to sync:")
                .AddChoices(providers));

        if (selectedProvider == "⬅️ Back")
            return;

        var options = new SyncOptions
        {
            ForceSync = AnsiConsole.Confirm("Force sync for this provider?"),
            ConcurrentSyncs = AnsiConsole.Ask<int>("Maximum concurrent syncs (default 3):", 3)
        };

        await _integrationManager.SyncProviderAsync(selectedProvider, options);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("Press any key to continue...");
        Console.ReadKey();
    }

    private async Task SyncSpecificIntegrationsAsync()
    {
        var integrations = await _integrationManager.GetAllActiveIntegrationsAsync();
        var integrationsList = integrations.ToList();

        if (!integrationsList.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No active integrations found.[/]");
            AnsiConsole.MarkupLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        var choices = integrationsList.Select(i => 
            $"{i.Provider}: {i.OrganizationUrl} ({i.ProjectName ?? "All projects"})").ToList();

        var selectedIntegrations = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("Select integrations to sync:")
                .PageSize(10)
                .InstructionsText("[grey](Press [blue]<space>[/] to toggle, [green]<enter>[/] to accept)[/]")
                .AddChoices(choices));

        if (!selectedIntegrations.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No integrations selected.[/]");
            AnsiConsole.MarkupLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        var selectedIds = selectedIntegrations
            .Select(selected => integrationsList[choices.IndexOf(selected)].Id)
            .ToList();

        var options = new SyncOptions
        {
            ForceSync = true, // Always force for specific selections
            ConcurrentSyncs = AnsiConsole.Ask<int>("Maximum concurrent syncs (default 2):", 2)
        };

        await _integrationManager.SyncSpecificIntegrationsAsync(selectedIds, options);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("Press any key to continue...");
        Console.ReadKey();
    }

    private async Task DebugGitHubIssuesAsync()
    {
        Console.Clear();
        AnsiConsole.MarkupLine("[bold yellow]🐛 GitHub Issues Debug[/]");
        AnsiConsole.WriteLine();

        try
        {
            AnsiConsole.MarkupLine("[blue]📡 Running GitHub issues debug...[/]");
            AnsiConsole.WriteLine();

            var debugInfo = await _integrationManager.DebugGitHubIssuesAsync();
            
            // Display debug info with proper formatting
            var lines = debugInfo.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains("==="))
                {
                    AnsiConsole.MarkupLine($"[bold yellow]{line.Trim()}[/]");
                }
                else if (line.Contains("❌"))
                {
                    AnsiConsole.MarkupLine($"[red]{line.Trim()}[/]");
                }
                else if (line.Contains("✅"))
                {
                    AnsiConsole.MarkupLine($"[green]{line.Trim()}[/]");
                }
                else if (line.Contains("⚠️"))
                {
                    AnsiConsole.MarkupLine($"[yellow]{line.Trim()}[/]");
                }
                else if (line.Contains("🔍") || line.Contains("📡") || line.Contains("📊"))
                {
                    AnsiConsole.MarkupLine($"[blue]{line.Trim()}[/]");
                }
                else if (!string.IsNullOrWhiteSpace(line))
                {
                    AnsiConsole.MarkupLine($"[grey]{line.Trim()}[/]");
                }
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]❌ Debug failed: {ex.Message}[/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
        Console.ReadKey();
    }

    private static void ShowWelcomeAnimated()
    {
        Console.Clear();
        
        // ASCII art header
        var figlet = new FigletText("Integrations")
            .LeftJustified()
            .Color(Color.Blue);

        AnsiConsole.Write(figlet);
        
        // Welcome panel with animation
        var welcomePanel = new Panel(
            new Markup("[bold blue]Welcome to Integration Manager![/]\n\n" +
                      "Connect and synchronize with external providers.\n" +
                      "Manage GitHub and provider integrations.\n\n" +
                      "[dim]Streamline your workflow with seamless integrations[/]"))
        {
            Border = BoxBorder.Double,
            BorderStyle = new Style(Color.Blue),
            Header = new PanelHeader(" 🔗 Integration Management "),
            Padding = new Padding(2, 1)
        };

        AnsiConsole.Write(welcomePanel);
        
        // Loading animation
        AnsiConsole.Status()
            .Start("Loading Integration Manager...", ctx =>
            {
                var frames = new[] { "🔄", "🔃", "🔁", "↻", "↺", "⟲", "⟳" };
                for (int i = 0; i < 7; i++)
                {
                    ctx.Status($"{frames[i]} Loading Integration Manager...");
                }
            });
    }

    private async Task<string> ShowInteractiveMenuAsync(string title, List<(string key, string icon, string value, string description)> items, string subtitle = "")
    {
        var searchTerm = "";
        var selectedIndex = 0;
        var filteredItems = items;
        
        while (true)
        {
            Console.Clear();
            ShowWelcomeAnimated();
            
            // Show animated header
            ShowAnimatedHeader(title, subtitle);
            
            // Filter items based on search
            filteredItems = string.IsNullOrEmpty(searchTerm) 
                ? items 
                : items.Where(item => 
                    item.value.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    item.description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    item.key == searchTerm).ToList();
            
            if (filteredItems.Count == 0)
            {
                filteredItems = items;
                searchTerm = "";
            }
            
            // Ensure selected index is valid
            selectedIndex = Math.Max(0, Math.Min(selectedIndex, filteredItems.Count - 1));
            
            // Show search bar if there's a search term
            if (!string.IsNullOrEmpty(searchTerm))
            {
                var searchPanel = new Panel($"🔍 Search: [yellow]{searchTerm}[/]")
                {
                    Border = BoxBorder.Rounded,
                    BorderStyle = new Style(Color.Yellow)
                };
                AnsiConsole.Write(searchPanel);
                AnsiConsole.WriteLine();
            }
            
            // Create and display menu
            var table = CreateStyledMenuTable(filteredItems, selectedIndex);
            AnsiConsole.Write(table);
            
            // Show navigation help
            ShowNavigationHelp();
            
            // Handle input
            var key = Console.ReadKey(true);
            
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    selectedIndex = selectedIndex > 0 ? selectedIndex - 1 : filteredItems.Count - 1;
                    break;
                    
                case ConsoleKey.DownArrow:
                    selectedIndex = selectedIndex < filteredItems.Count - 1 ? selectedIndex + 1 : 0;
                    break;
                    
                case ConsoleKey.LeftArrow:
                    return "back";
                    
                case ConsoleKey.RightArrow:
                case ConsoleKey.Enter:
                    if (filteredItems.Count > 0)
                        return filteredItems[selectedIndex].value;
                    break;
                    
                case ConsoleKey.Escape:
                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        searchTerm = "";
                        selectedIndex = 0;
                    }
                    else
                    {
                        return "back";
                    }
                    break;
                    
                case ConsoleKey.Backspace:
                    if (searchTerm.Length > 0)
                    {
                        searchTerm = searchTerm[..^1];
                        selectedIndex = 0;
                    }
                    break;
                    
                default:
                    if (char.IsDigit(key.KeyChar) || char.IsLetter(key.KeyChar))
                    {
                        var shortcutItem = filteredItems.FirstOrDefault(item => item.key == key.KeyChar.ToString());
                        if (shortcutItem != default)
                        {
                            return shortcutItem.value;
                        }
                    }
                    if (char.IsLetter(key.KeyChar) || char.IsWhiteSpace(key.KeyChar))
                    {
                        searchTerm += key.KeyChar;
                        selectedIndex = 0;
                    }
                    break;
            }
        }
    }

    private static void ShowAnimatedHeader(string title, string subtitle)
    {
        // Animated title with gradient effect
        var rule = new Rule($"[bold blue]{title}[/]")
        {
            Style = Style.Parse("blue"),
            Justification = Justify.Center
        };
        AnsiConsole.Write(rule);
        
        if (!string.IsNullOrEmpty(subtitle))
        {
            AnsiConsole.MarkupLine($"[dim italic]{subtitle.EscapeMarkup()}[/]");
        }
        
        AnsiConsole.WriteLine();
    }

    private static Table CreateStyledMenuTable(List<(string key, string icon, string value, string description)> items, int selectedIndex)
    {
        var table = new Table()
        {
            Border = TableBorder.Rounded,
            ShowHeaders = false
        };
        
        table.AddColumn("Key");
        table.AddColumn("Option");
        table.AddColumn("Description");
        
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var isSelected = i == selectedIndex;
            
            var keyText = isSelected ? $"[bold blue]> {item.key}[/]" : $"  [dim]{item.key}[/]";
            var optionText = isSelected ? $"[bold]{item.icon} {GetMenuTitle(item.value)}[/]" : $"{item.icon} {GetMenuTitle(item.value)}";
            var descText = isSelected ? $"[bold white]{item.description}[/]" : $"[dim]{item.description}[/]";
            
            table.AddRow(keyText, optionText, descText);
        }
        
        return table;
    }

    private static string GetMenuTitle(string value)
    {
        return value switch
        {
            "add_integration" => "Add New Integration",
            "view_all" => "View All Integrations",
            "statistics" => "Provider Statistics",
            "test_all" => "Test All Connections",
            "smart_sync" => "Smart Sync",
            "emergency_sync" => "Emergency Sync",
            "sync_provider" => "Sync by Provider",
            "sync_specific" => "Sync Specific Integrations",
            "manage_specific" => "Manage Specific Integration",
            "remove" => "Remove Integration",
            "debug" => "Debug GitHub Issues",
            "back" => "Back to Main Menu",
            _ => value
        };
    }

    private static string GetProviderIcon(string provider)
    {
        return provider?.ToLower() switch
        {
            "github" => "🐙",
            "azuredevops" => "🔵",
            "azure" => "☁️",
            "jira" => "🟦",
            "gitlab" => "🦊",
            "bitbucket" => "🪣",
            _ => "🔗"
        };
    }

    private static void ShowNavigationHelp()
    {
        var helpPanel = new Panel(
            new Markup("[dim]Use [white]↑↓[/] to navigate, [white]Enter[/] to select, [white]←[/] to go back, [white]Esc[/] to cancel\n" +
                      "Type letters/numbers for shortcuts or search[/]"))
        {
            Border = BoxBorder.None,
            Padding = new Padding(0, 0, 0, 1)
        };
        
        AnsiConsole.Write(helpPanel);
    }
}
