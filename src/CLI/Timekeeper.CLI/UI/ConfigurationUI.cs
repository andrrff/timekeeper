using Spectre.Console;
using Timekeeper.CLI.Services;
using Timekeeper.Infrastructure.DevOps.AzureDevOps;
using Timekeeper.Infrastructure.DevOps.GitHub;

namespace Timekeeper.CLI.UI;

public class ConfigurationUI
{
    private readonly IAzureDevOpsAuthService _azureDevOpsAuth;
    private readonly IGitHubAuthService _gitHubAuth;
    private readonly ProviderIntegrationService _devOpsIntegrationService;
    private readonly DevOpsSyncService _devOpsSyncService;
    private readonly GitHubIntegrationService _gitHubIntegrationService;
    private readonly GitHubSyncService _gitHubSyncService;
    private readonly IntegrationsUI _integrationsUI;

    public ConfigurationUI(
        IAzureDevOpsAuthService azureDevOpsAuth, 
        IGitHubAuthService gitHubAuth, 
        ProviderIntegrationService devOpsIntegrationService, 
        DevOpsSyncService devOpsSyncService,
        GitHubIntegrationService gitHubIntegrationService,
        GitHubSyncService gitHubSyncService,
        IntegrationsUI integrationsUI)
    {
        _azureDevOpsAuth = azureDevOpsAuth;
        _gitHubAuth = gitHubAuth;
        _devOpsIntegrationService = devOpsIntegrationService;
        _devOpsSyncService = devOpsSyncService;
        _gitHubIntegrationService = gitHubIntegrationService;
        _gitHubSyncService = gitHubSyncService;
        _integrationsUI = integrationsUI;
    }

    public async Task ShowAsync()
    {
        while (true)
        {
            var menuItems = new List<(string key, string icon, string value, string description)>
            {
                ("1", "🔧", "general", "Configure general application settings"),
                ("2", "🔗", "integrations", "Manage all integrations (Azure DevOps, GitHub, etc.)"),
                ("3", "📊", "categories", "Manage default task categories"),
                ("4", "⏰", "timetracking", "Configure time tracking preferences"),
                ("5", "🎨", "theme", "Customize UI theme and appearance"),
                ("6", "📁", "data", "Data backup and management tools"),
                ("0", "⬅️", "back", "Return to main menu")
            };

            var choice = await ShowInteractiveMenuAsync("⚙️ Configuration Settings", menuItems, 
                "Customize your Timekeeper experience");

            switch (choice)
            {
                case "general":
                    await ShowGeneralSettingsAsync();
                    break;
                case "integrations":
                    await _integrationsUI.ShowAsync();
                    break;
                case "categories":
                    await ShowDefaultCategoriesAsync();
                    break;
                case "timetracking":
                    await ShowTimeTrackingPreferencesAsync();
                    break;
                case "theme":
                    await ShowUIThemeSettingsAsync();
                    break;
                case "data":
                    await ShowDataManagementAsync();
                    break;
                case "back":
                    return;
            }
        }
    }

    private async Task ShowGeneralSettingsAsync()
    {
        Console.Clear();
        ShowAnimatedHeader("⚙️ Configuration Settings", "General Settings");
        
        var table = new Table();
        table.AddColumn("Setting");
        table.AddColumn("Current Value");
        table.AddColumn("Description");

        table.AddRow("Auto-save", "[green]Enabled[/]", "Automatically save changes");
        table.AddRow("Notifications", "[yellow]Enabled[/]", "Show due date reminders");
        table.AddRow("Language", "[blue]English[/]", "Application language");
        table.AddRow("Date Format", "[grey]yyyy-MM-dd[/]", "Date display format");

        AnsiConsole.Write(table);

        if (AnsiConsole.Confirm("Would you like to modify any settings?"))
        {
            Console.Clear();
            ShowAnimatedHeader("⚙️ Configuration Settings", "Modify General Settings");
            
            var setting = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select setting to modify:")
                    .AddChoices("Auto-save",
                               "Notifications",
                               "Language",
                               "Date Format"));

            Console.Clear();
            ShowAnimatedHeader("⚙️ Configuration Settings", "General Settings Updated");
            AnsiConsole.MarkupLine($"[green]✓ {setting} setting updated![/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("Press [blue]any key[/] to continue...");
        Console.ReadKey();
    }

    private async Task ShowAzureDevOpsConfigAsync()
    {
        AnsiConsole.MarkupLine("[bold blue]Azure DevOps Integration[/]");
        AnsiConsole.WriteLine();

        var menuItems = new List<(string key, string icon, string value, string description)>
        {
            ("1", "🔗", "setup", "Configure new Azure DevOps connection"),
            ("2", "✅", "test", "Test current connection status"),
            ("3", "🔄", "sync", "Sync work items now"),
            ("4", "📝", "view", "View current integration settings"),
            ("5", "🗑️", "remove", "Remove existing connection"),
            ("0", "⬅️", "back", "Return to configuration menu")
        };

        var choice = await ShowInteractiveMenuAsync("� Azure DevOps Integration", menuItems,
            "Manage your Azure DevOps work item synchronization");

        switch (choice)
        {
            case "setup":
                await SetupAzureDevOpsConnectionAsync();
                break;
            case "test":
                await TestAzureDevOpsConnectionAsync();
                break;
            case "sync":
                await SyncWorkItemsAsync();
                break;
            case "view":
                await ViewAzureDevOpsSettingsAsync();
                break;
            case "remove":
                await RemoveAzureDevOpsConnectionAsync();
                break;
            case "back":
                return;
        }

        AnsiConsole.MarkupLine("Press [blue]any key[/] to continue...");
        Console.ReadKey();
    }

    private async Task SetupAzureDevOpsConnectionAsync()
    {
        AnsiConsole.MarkupLine("[bold green]Setup Azure DevOps Connection[/]");
        AnsiConsole.WriteLine();

        var organizationUrl = AnsiConsole.Ask<string>("Enter your Azure DevOps [blue]organization URL[/]:");
        var pat = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter your [red]Personal Access Token[/]:")
                .Secret());

        AnsiConsole.WriteLine();
        
        await AnsiConsole.Status()
            .Start("Validating connection...", async ctx =>
            {
                var isValid = await _devOpsIntegrationService.TestConnectionAsync(organizationUrl, pat);
                
                if (isValid)
                {
                    ctx.Status("Getting projects...");
                    var projects = await _devOpsIntegrationService.GetProjectsAsync(organizationUrl, pat);
                    var projectList = projects.ToList();
                    
                    AnsiConsole.MarkupLine($"[green]✓ Connection successful![/]");
                    
                    string? selectedProject = null;
                    if (projectList.Any())
                    {
                        AnsiConsole.MarkupLine($"[grey]Found {projectList.Count} projects[/]");
                        
                        var includeProject = AnsiConsole.Confirm("Do you want to specify a project?", false);
                        if (includeProject)
                        {
                            var projectChoices = projectList.ToList();
                            projectChoices.Add("All Projects");
                            
                            selectedProject = AnsiConsole.Prompt(
                                new SelectionPrompt<string>()
                                    .Title("Select a project:")
                                    .AddChoices(projectChoices.ToArray()));
                                    
                            if (selectedProject == "All Projects")
                                selectedProject = null;
                        }
                    }
                    
                    ctx.Status("Saving configuration...");
                    var saved = await _devOpsIntegrationService.SaveIntegrationAsync("AzureDevOps", organizationUrl, pat, selectedProject);
                    
                    if (saved)
                    {
                        AnsiConsole.MarkupLine("[blue]✓ Configuration saved successfully![/]");
                        if (!string.IsNullOrEmpty(selectedProject))
                            AnsiConsole.MarkupLine($"[grey]Project: {selectedProject}[/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[red]✗ Failed to save configuration[/]");
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]❌ Connection failed. Please check your credentials.[/]");
                }
            });
    }

    private async Task TestAzureDevOpsConnectionAsync()
    {
        AnsiConsole.MarkupLine("[bold yellow]Testing Azure DevOps Connection[/]");
        AnsiConsole.WriteLine();

        var integration = await _devOpsIntegrationService.GetActiveIntegrationAsync();
        
        if (integration == null)
        {
            AnsiConsole.MarkupLine("[red]❌ No Azure DevOps integration configured.[/]");
            AnsiConsole.MarkupLine("[grey]Please setup a connection first.[/]");
            return;
        }

        await AnsiConsole.Status()
            .Start("Testing connection...", async ctx =>
            {
                var isValid = await _devOpsIntegrationService.TestConnectionAsync(
                    integration.OrganizationUrl, 
                    integration.PersonalAccessToken);
                
                if (isValid)
                {
                    ctx.Status("Getting projects...");
                    var projects = await _devOpsIntegrationService.GetProjectsAsync(
                        integration.OrganizationUrl, 
                        integration.PersonalAccessToken);
                    
                    AnsiConsole.MarkupLine("[green]✓ Connection is active and working![/]");
                    AnsiConsole.MarkupLine($"[grey]Organization: {integration.OrganizationUrl}[/]");
                    AnsiConsole.MarkupLine($"[grey]Project: {integration.ProjectName ?? "All Projects"}[/]");
                    AnsiConsole.MarkupLine($"[grey]Last Sync: {integration.LastSyncAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never"}[/]");
                    AnsiConsole.MarkupLine($"[grey]Available Projects: {projects.Count()}[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]❌ Connection failed. Please check your credentials.[/]");
                }
            });
    }

    private async Task ViewAzureDevOpsSettingsAsync()
    {
        AnsiConsole.MarkupLine("[bold blue]Current Azure DevOps Settings[/]");
        AnsiConsole.WriteLine();

        var integration = await _devOpsIntegrationService.GetActiveIntegrationAsync();
        
        if (integration == null)
        {
            AnsiConsole.MarkupLine("[red]❌ No Azure DevOps integration configured.[/]");
            AnsiConsole.MarkupLine("[grey]Please setup a connection first.[/]");
            return;
        }

        var table = new Table();
        table.AddColumn("Setting");
        table.AddColumn("Value");

        table.AddRow("Provider", $"[blue]{integration.Provider}[/]");
        table.AddRow("Organization URL", $"[blue]{integration.OrganizationUrl}[/]");
        table.AddRow("Project", $"[grey]{integration.ProjectName ?? "All Projects"}[/]");
        table.AddRow("Status", integration.IsActive ? "[green]Active[/]" : "[red]Inactive[/]");
        table.AddRow("Created", $"[grey]{integration.CreatedAt:yyyy-MM-dd HH:mm:ss}[/]");
        table.AddRow("Last Sync", $"[grey]{integration.LastSyncAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never"}[/]");

        AnsiConsole.Write(table);
    }

    private async Task RemoveAzureDevOpsConnectionAsync()
    {
        var integration = await _devOpsIntegrationService.GetActiveIntegrationAsync();
        
        if (integration == null)
        {
            AnsiConsole.MarkupLine("[red]❌ No Azure DevOps integration configured.[/]");
            return;
        }

        if (AnsiConsole.Confirm("[red]Are you sure you want to remove the Azure DevOps connection?[/]"))
        {
            await AnsiConsole.Status()
                .Start("Removing connection...", async ctx =>
                {
                    var removed = await _devOpsIntegrationService.RemoveIntegrationAsync();
                    
                    if (removed)
                    {
                        AnsiConsole.MarkupLine("[green]✓ Azure DevOps connection removed.[/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[red]❌ Failed to remove connection.[/]");
                    }
                });
        }
    }

    private async Task ShowDefaultCategoriesAsync()
    {
        AnsiConsole.MarkupLine("[bold blue]Default Categories[/]");
        AnsiConsole.WriteLine();

        var categories = new[] { "Work", "Personal", "Learning", "Health", "Finance", "Travel" };

        var table = new Table();
        table.AddColumn("Category");
        table.AddColumn("Color");
        table.AddColumn("Tasks Count");

        foreach (var category in categories)
        {
            var color = category switch
            {
                "Work" => "blue",
                "Personal" => "green",
                "Learning" => "purple",
                "Health" => "red",
                "Finance" => "yellow",
                "Travel" => "cyan",
                _ => "white"
            };
            
            table.AddRow(category, $"[{color}]●[/]", "5");
        }

        AnsiConsole.Write(table);

        if (AnsiConsole.Confirm("Would you like to add a new category?"))
        {
            var newCategory = AnsiConsole.Ask<string>("Enter new category name:");
            AnsiConsole.MarkupLine($"[green]✓ Category '{newCategory}' added![/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("Press [blue]any key[/] to continue...");
        Console.ReadKey();
    }

    private async Task ShowTimeTrackingPreferencesAsync()
    {
        Console.Clear();
        ShowAnimatedHeader("⚙️ Configuration Settings", "Time Tracking Preferences");

        var table = new Table();
        table.AddColumn("Preference");
        table.AddColumn("Current Value");

        table.AddRow("Default time unit", "[yellow]Minutes[/]");
        table.AddRow("Auto-start timer", "[green]Enabled[/]");
        table.AddRow("Reminder interval", "[blue]15 minutes[/]");
        table.AddRow("Round time entries", "[grey]Disabled[/]");
        table.AddRow("Work day start", "[cyan]09:00[/]");
        table.AddRow("Work day end", "[cyan]17:00[/]");

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("Press [blue]any key[/] to continue...");
        Console.ReadKey();
    }

    private async Task ShowUIThemeSettingsAsync()
    {
        Console.Clear();
        ShowAnimatedHeader("⚙️ Configuration Settings", "UI Theme Settings");

        var theme = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a theme:")
                .AddChoices("🌟 Default",
                           "🌙 Dark",
                           "☀️  Light",
                           "🌈 Colorful",
                           "💼 Professional"));

        Console.Clear();
        ShowAnimatedHeader("⚙️ Configuration Settings", "Theme Updated");
        AnsiConsole.MarkupLine($"[green]✓ Theme changed to {theme}[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("Press [blue]any key[/] to continue...");
        Console.ReadKey();
    }

    private async Task ShowDataManagementAsync()
    {
        Console.Clear();
        ShowAnimatedHeader("⚙️ Configuration Settings", "Data Management");

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("What would you like to do?")
                .AddChoices("📤 Export Data",
                           "📥 Import Data",
                           "🗑️  Clear All Data",
                           "🔄 Reset to Defaults",
                           "💾 Backup Database"));

        switch (choice)
        {
            case "📤 Export Data":
                await ExportDataAsync();
                break;
            case "📥 Import Data":
                await ImportDataAsync();
                break;
            case "🗑️  Clear All Data":
                await ClearDataAsync();
                break;
            case "🔄 Reset to Defaults":
                await ResetToDefaultsAsync();
                break;
            case "💾 Backup Database":
                await BackupDatabaseAsync();
                break;
        }

        AnsiConsole.MarkupLine("Press [blue]any key[/] to continue...");
        Console.ReadKey();
    }

    private async Task ExportDataAsync()
    {
        Console.Clear();
        ShowAnimatedHeader("⚙️ Configuration Settings", "Exporting Data");
        
        await AnsiConsole.Status()
            .Start("Exporting data...", async ctx =>
            {
                await Task.Delay(1500); // Simulate export process
            });

        Console.Clear();
        ShowAnimatedHeader("⚙️ Configuration Settings", "Export Complete");
        AnsiConsole.MarkupLine("[green]✓ Data exported to timekeeper_export.json[/]");
    }

    private async Task ImportDataAsync()
    {
        Console.Clear();
        ShowAnimatedHeader("⚙️ Configuration Settings", "Import Data");
        
        var fileName = AnsiConsole.Ask<string>("Enter the import file name:");
        
        Console.Clear();
        ShowAnimatedHeader("⚙️ Configuration Settings", "Importing Data");
        
        await AnsiConsole.Status()
            .Start($"Importing data from {fileName}...", async ctx =>
            {
                await Task.Delay(2000); // Simulate import process
            });

        Console.Clear();
        ShowAnimatedHeader("⚙️ Configuration Settings", "Import Complete");
        AnsiConsole.MarkupLine("[green]✓ Data imported successfully![/]");
    }

    private async Task ClearDataAsync()
    {
        Console.Clear();
        ShowAnimatedHeader("⚙️ Configuration Settings", "Clear All Data");
        
        if (AnsiConsole.Confirm("[red]Are you sure you want to clear ALL data? This cannot be undone![/]"))
        {
            Console.Clear();
            ShowAnimatedHeader("⚙️ Configuration Settings", "Clearing Data");
            
            await AnsiConsole.Status()
                .Start("Clearing all data...", async ctx =>
                {
                    await Task.Delay(1500); // Simulate clearing process
                });

            Console.Clear();
            ShowAnimatedHeader("⚙️ Configuration Settings", "Data Cleared");
            AnsiConsole.MarkupLine("[green]✓ All data cleared.[/]");
        }
    }

    private async Task ResetToDefaultsAsync()
    {
        Console.Clear();
        ShowAnimatedHeader("⚙️ Configuration Settings", "Reset to Defaults");
        
        if (AnsiConsole.Confirm("Reset all settings to default values?"))
        {
            Console.Clear();
            ShowAnimatedHeader("⚙️ Configuration Settings", "Resetting Settings");
            
            await AnsiConsole.Status()
                .Start("Resetting to defaults...", async ctx =>
                {
                    await Task.Delay(1500); // Simulate reset process
                });

            Console.Clear();
            ShowAnimatedHeader("⚙️ Configuration Settings", "Settings Reset");
            AnsiConsole.MarkupLine("[green]✓ Settings reset to defaults.[/]");
        }
    }

    private async Task BackupDatabaseAsync()
    {
        Console.Clear();
        ShowAnimatedHeader("⚙️ Configuration Settings", "Database Backup");
        
        await AnsiConsole.Status()
            .Start("Creating database backup...", async ctx =>
            {
                await Task.Delay(2000); // Simulate backup process
            });

        Console.Clear();
        ShowAnimatedHeader("⚙️ Configuration Settings", "Backup Complete");
        AnsiConsole.MarkupLine("[green]✓ Database backup created: timekeeper_backup_20250715.db[/]");
    }

    private async Task SyncWorkItemsAsync()
    {
        AnsiConsole.MarkupLine("[bold yellow]Syncing Work Items to Todos[/]");
        AnsiConsole.WriteLine();

        var integration = await _devOpsIntegrationService.GetActiveIntegrationAsync();
        
        if (integration == null)
        {
            AnsiConsole.MarkupLine("[red]❌ No Azure DevOps integration configured.[/]");
            return;
        }

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("What type of sync would you like to perform?")
                .AddChoices("🆕 Import New Work Items as Todos",
                           "🔄 Update Existing Todos from Work Items",
                           "❌ Cancel"));

        if (choice == "❌ Cancel") return;

        SyncResult? result = null;
        
        if (choice == "🆕 Import New Work Items as Todos")
        {
            await AnsiConsole.Status()
                .Start("Syncing new work items...", async ctx =>
                {
                    ctx.Status("Fetching work items from Azure DevOps...");
                    
                    ctx.Status("Creating todos from work items...");
                    result = await _devOpsSyncService.SyncWorkItemsToTodosAsync();
                });
        }
        else if (choice == "🔄 Update Existing Todos from Work Items")
        {
            await AnsiConsole.Status()
                .Start("Updating existing todos...", async ctx =>
                {
                    ctx.Status("Finding DevOps-synced todos...");
                    
                    ctx.Status("Updating todo statuses...");
                    result = await _devOpsSyncService.UpdateTodosFromWorkItemsAsync();
                });
        }

        if (result != null)
        {
            DisplaySyncResult(result);
        }
    }

    private void DisplaySyncResult(SyncResult result)
    {
        AnsiConsole.WriteLine();
        
        if (result.IsSuccess)
        {
            AnsiConsole.MarkupLine($"[green]✓ {result.Message}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]❌ {result.Message}[/]");
        }

        if (result.CreatedCount > 0 || result.UpdatedCount > 0 || result.SkippedCount > 0 || result.ErrorCount > 0)
        {
            AnsiConsole.WriteLine();
            var table = new Table();
            table.AddColumn("Result");
            table.AddColumn("Count");

            if (result.CreatedCount > 0)
                table.AddRow("[green]Created Todos[/]", result.CreatedCount.ToString());
            if (result.UpdatedCount > 0)
                table.AddRow("[yellow]Updated Todos[/]", result.UpdatedCount.ToString());
            if (result.SkippedCount > 0)
                table.AddRow("[grey]Skipped Items[/]", result.SkippedCount.ToString());
            if (result.ErrorCount > 0)
                table.AddRow("[red]Errors[/]", result.ErrorCount.ToString());

            AnsiConsole.Write(table);
        }

        if (result.CreatedItems.Any())
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold green]Created Todos:[/]");
            foreach (var item in result.CreatedItems.Take(3))
            {
                AnsiConsole.MarkupLine($"[green]• {item}[/]");
            }
            if (result.CreatedItems.Count > 3)
                AnsiConsole.MarkupLine($"[grey]... and {result.CreatedItems.Count - 3} more[/]");
        }

        if (result.Errors.Any())
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold red]Errors:[/]");
            foreach (var error in result.Errors.Take(2))
            {
                AnsiConsole.MarkupLine($"[red]• {error}[/]");
            }
            if (result.Errors.Count > 2)
                AnsiConsole.MarkupLine($"[grey]... and {result.Errors.Count - 2} more errors[/]");
        }
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
                    if (char.IsDigit(key.KeyChar))
                    {
                        var shortcutItem = filteredItems.FirstOrDefault(item => item.key == key.KeyChar.ToString());
                        if (shortcutItem != default)
                        {
                            return shortcutItem.value;
                        }
                    }
                    else if (char.IsLetter(key.KeyChar) || char.IsWhiteSpace(key.KeyChar))
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
        
        // Small delay to ensure header is visible
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
            
            var keyText = isSelected ? $"> {item.key}" : $"  {item.key}";
            var optionText = $"{item.icon} {GetMenuTitle(item.value)}";
            var descText = item.description;
            
            table.AddRow(keyText, optionText, descText);
        }
        
        return table;
    }

    private static string GetMenuTitle(string value)
    {
        return value switch
        {
            "general" => "General Settings",
            "devops" => "Azure DevOps Integration",
            "categories" => "Default Categories",
            "timetracking" => "Time Tracking Preferences",
            "theme" => "UI Theme Settings",
            "data" => "Data Management",
            "setup" => "Setup New Connection",
            "test" => "Test Current Connection",
            "sync" => "Sync Work Items Now",
            "view" => "View Current Settings",
            "remove" => "Remove Connection",
            "back" => "Back to Previous Menu",
            _ => value
        };
    }

    private static void ShowNavigationHelp()
    {
        AnsiConsole.WriteLine();
        
        var helpPanel = new Panel(
            "[dim]Navigation:[/] [blue]↑↓[/] Select  [blue]←[/] Back  [blue]→ Enter[/] Confirm  [blue]0-9[/] Quick Select  [blue]Type[/] Search  [blue]Esc[/] Clear/Exit"
        )
        {
            Border = BoxBorder.None,
            BorderStyle = new Style(Color.Grey),
            Padding = new Padding(1, 0)
        };
        
        AnsiConsole.Write(helpPanel);
    }

    private static void ShowWelcomeAnimated()
    {
        Console.Clear();
        
        // ASCII art header
        var figlet = new FigletText("Configuration")
            .LeftJustified()
            .Color(Color.Blue);

        AnsiConsole.Write(figlet);
        
        // Welcome panel with animation
        var welcomePanel = new Panel(
            new Markup("[bold blue]Welcome to Configuration Center![/]\n\n" +
                      "Customize your Timekeeper experience.\n" +
                      "Configure integrations, preferences, and settings.\n\n" +
                      "[dim]Created by [link=https://github.com/andrrff]andrrff[/] | [link=https://github.com/andrrff]GitHub[/][/]"))
        {
            Border = BoxBorder.Double,
            BorderStyle = new Style(Color.Blue),
            Header = new PanelHeader(" ⚙️ Configuration Settings "),
            Padding = new Padding(2, 1)
        };

        AnsiConsole.Write(welcomePanel);
        
        // Loading animation
        AnsiConsole.Status()
            .Start("Loading Configuration...", ctx =>
            {
                var frames = new[] { "⚙️", "🔧", "⚡", "✨", "⚡", "🔧", "⚙️" };
                for (int i = 0; i < 7; i++)
                {
                    ctx.Status($"{frames[i]} Loading Configuration...");
                }
            });
            
    }

    // GitHub Integration Methods
    private async Task ShowGitHubConfigAsync()
    {
        AnsiConsole.MarkupLine("[bold blue]GitHub Integration[/]");
        AnsiConsole.WriteLine();

        var menuItems = new List<(string key, string icon, string value, string description)>
        {
            ("1", "🔗", "setup", "Configure new GitHub connection"),
            ("2", "✅", "test", "Test current connection status"),
            ("3", "🔄", "sync", "Sync issues now"),
            ("4", "📝", "view", "View current integration settings"),
            ("5", "🗑️", "remove", "Remove existing connection"),
            ("0", "⬅️", "back", "Return to configuration menu")
        };

        var choice = await ShowInteractiveMenuAsync("🐙 GitHub Integration", menuItems,
            "Manage your GitHub issue synchronization");

        switch (choice)
        {
            case "setup":
                await SetupGitHubConnectionAsync();
                break;
            case "test":
                await TestGitHubConnectionAsync();
                break;
            case "sync":
                await SyncGitHubIssuesAsync();
                break;
            case "view":
                await ViewGitHubSettingsAsync();
                break;
            case "remove":
                await RemoveGitHubConnectionAsync();
                break;
            case "back":
                return;
        }

        AnsiConsole.MarkupLine("Press [blue]any key[/] to continue...");
        Console.ReadKey();
    }

    private async Task SetupGitHubConnectionAsync()
    {
        AnsiConsole.MarkupLine("[bold green]Setup GitHub Connection[/]");
        AnsiConsole.WriteLine();

        // Show instructions for getting a GitHub token
        var panel = new Panel(
            "[yellow]To connect to GitHub, you need a Personal Access Token (PAT).[/]\n\n" +
            "[cyan]Steps to create a GitHub PAT:[/]\n" +
            "1. Go to GitHub.com → Settings → Developer settings → Personal access tokens → Tokens (classic)\n" +
            "2. Click 'Generate new token' → 'Generate new token (classic)'\n" +
            "3. Select scopes: [green]repo[/] (for private repos) and [green]read:user[/]\n" +
            "4. Copy the generated token\n\n" +
            "[red]Note:[/] For organizations, use the organization name (e.g., 'microsoft')\n" +
            "For personal repositories, use your username (e.g., 'andrrff')")
        {
            Header = new PanelHeader("📋 GitHub Setup Instructions"),
            Border = BoxBorder.Rounded
        };

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        var organizationUrl = AnsiConsole.Ask<string>("Enter your GitHub [blue]username or organization[/]:");
        var pat = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter your [red]Personal Access Token[/]:")
                .Secret());

        AnsiConsole.WriteLine();
        
        await AnsiConsole.Status()
            .Start("Configuring GitHub integration...", async ctx =>
            {
                var success = await _gitHubIntegrationService.ConfigureIntegrationAsync(organizationUrl, pat);
                
                if (success)
                {
                    AnsiConsole.MarkupLine("[blue]✓ GitHub integration configured successfully![/]");
                    AnsiConsole.MarkupLine("[grey]You can now sync GitHub issues with your tasks![/]");
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]❌ Failed to configure GitHub integration.[/]");
                }
            });
    }

    private async Task TestGitHubConnectionAsync()
    {
        AnsiConsole.MarkupLine("[bold blue]Testing GitHub Connection[/]");
        AnsiConsole.WriteLine();

        await AnsiConsole.Status()
            .Start("Testing connection...", async ctx =>
            {
                var success = await _gitHubIntegrationService.TestConnectionAsync();
                
                if (success)
                {
                    var integration = await _gitHubIntegrationService.GetActiveGitHubIntegrationAsync();
                    if (integration != null)
                    {
                        AnsiConsole.MarkupLine("[green]✓ GitHub connection is working![/]");
                        
                        var user = await _gitHubIntegrationService.GetCurrentUserAsync(
                            integration.OrganizationUrl, 
                            integration.PersonalAccessToken);
                        AnsiConsole.MarkupLine($"[grey]Connected as: {user}[/]");
                        AnsiConsole.MarkupLine($"[grey]Organization/User: {integration.OrganizationUrl}[/]");
                        if (!string.IsNullOrEmpty(integration.ProjectName))
                        {
                            AnsiConsole.MarkupLine($"[grey]Repository: {integration.ProjectName}[/]");
                        }
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]❌ GitHub connection test failed.[/]");
                }
            });
    }

    private async Task SyncGitHubIssuesAsync()
    {
        AnsiConsole.MarkupLine("[bold blue]Syncing GitHub Issues[/]");
        AnsiConsole.WriteLine();

        var issues = await _gitHubSyncService.SyncIssuesAsync();
        var issueList = issues.ToList();
        
        if (issueList.Any())
        {
            AnsiConsole.MarkupLine($"[green]✓ Successfully synced {issueList.Count} GitHub issues![/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]No issues found to sync.[/]");
        }
    }

    private async Task ViewGitHubSettingsAsync()
    {
        AnsiConsole.MarkupLine("[bold blue]GitHub Integration Settings[/]");
        AnsiConsole.WriteLine();

        var integration = await _gitHubIntegrationService.GetActiveGitHubIntegrationAsync();
        
        if (integration == null)
        {
            AnsiConsole.MarkupLine("[red]❌ No GitHub integration configured.[/]");
            return;
        }

        var table = new Table();
        table.AddColumn("Setting");
        table.AddColumn("Value");

        table.AddRow("Provider", "🐙 GitHub");
        table.AddRow("Organization/User", integration.OrganizationUrl);
        table.AddRow("Repository", integration.ProjectName ?? "All repositories");
        table.AddRow("Status", integration.IsActive ? "[green]Active[/]" : "[red]Inactive[/]");
        table.AddRow("Created", integration.CreatedAt.ToString("yyyy-MM-dd HH:mm"));
        table.AddRow("Last Sync", integration.LastSyncAt?.ToString("yyyy-MM-dd HH:mm") ?? "Never");

        AnsiConsole.Write(table);
    }

    private async Task RemoveGitHubConnectionAsync()
    {
        var integration = await _gitHubIntegrationService.GetActiveGitHubIntegrationAsync();
        
        if (integration == null)
        {
            AnsiConsole.MarkupLine("[red]❌ No GitHub integration configured.[/]");
            return;
        }

        if (AnsiConsole.Confirm("[red]Are you sure you want to remove the GitHub connection?[/]"))
        {
            await AnsiConsole.Status()
                .Start("Removing GitHub connection...", async ctx =>
                {
                    var success = await _gitHubIntegrationService.RemoveIntegrationAsync();
                    
                    if (success)
                    {
                        AnsiConsole.MarkupLine("[green]✓ GitHub connection removed successfully![/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[red]❌ Failed to remove GitHub connection.[/]");
                    }
                });
        }
    }
}
