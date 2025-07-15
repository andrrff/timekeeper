using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Timekeeper.CLI.Services;
using Timekeeper.CLI.UI;
using Timekeeper.Infrastructure.Persistence;
using Timekeeper.Infrastructure.Repositories;
using Timekeeper.Infrastructure.DevOps.AzureDevOps;
using Timekeeper.Infrastructure.DevOps.GitHub;
using Timekeeper.Domain.Interfaces;
using MediatR;
using System.Text;
using System.CommandLine;

// Configure console encoding for emoji support
Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

// Additional Windows console configuration for better emoji support
if (OperatingSystem.IsWindows())
{
    try
    {
        // Enable UTF-8 support in Windows console
        Console.OutputEncoding = new UTF8Encoding(false);
        Console.InputEncoding = new UTF8Encoding(false);
    }
    catch
    {
        // Fallback if UTF-8 encoding fails
        AnsiConsole.MarkupLine("[yellow]Warning: Could not set UTF-8 encoding. Some emojis may not display correctly.[/]");
    }
}

var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        // Disable EF Core debug logging
        logging.SetMinimumLevel(LogLevel.Warning);
        logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
        logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.None);
    })
    .ConfigureServices((context, services) =>
    {
        // Database - Disable sensitive data logging and detailed errors for production
        services.AddDbContext<TimekeeperDbContext>(options =>
        {
            options.UseSqlite("Data Source=timekeeper.db");
            
            // Disable debug features for better performance
            options.EnableSensitiveDataLogging(false);
            options.EnableDetailedErrors(false);
            options.ConfigureWarnings(warnings => 
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.MultipleCollectionIncludeWarning));
        });

        // Repositories
        services.AddScoped<ITodoItemRepository, TodoItemRepository>();
        services.AddScoped<ITimeEntryRepository, TimeEntryRepository>();
        services.AddScoped<IActivityLogRepository, ActivityLogRepository>();
        services.AddScoped<IProvidersIntegrationRepository, ProvidersIntegrationRepository>();

        // Azure DevOps
        services.AddScoped<IAzureDevOpsAuthService, AzureDevOpsAuthService>();
        services.AddScoped<Timekeeper.Domain.Interfaces.IDevOpsService, AzureDevOpsService>();

        // GitHub
        services.AddScoped<IGitHubAuthService, GitHubAuthService>();
        services.AddScoped<Timekeeper.Infrastructure.DevOps.GitHub.GitHubService>();
        services.AddScoped<Timekeeper.CLI.Services.IGitHubService, Timekeeper.CLI.Services.GitHubService>();
        services.AddScoped<GitHubIntegrationService>();
        services.AddScoped<GitHubSyncService>();

        // Integration Services
        services.AddScoped<IIntegrationService, GitHubIntegrationService>();
        services.AddScoped<IIntegrationService, AzureProviderIntegrationService>();
        services.AddScoped<AzureProviderIntegrationService>();
        services.AddScoped<IntegrationManager>();

        // MediatR
        services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssembly(typeof(Timekeeper.Application.TodoItems.Commands.CreateTodoItemCommand).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(Timekeeper.Application.TimeEntries.Commands.CreateTimeEntryCommand).Assembly);
        });

        // CLI Services
        services.AddScoped<ICommandService, CommandService>();
        services.AddScoped<ITodoService, TodoService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<ITimeTrackingService, Timekeeper.CLI.Services.TimeTrackingService>();
        services.AddScoped<Timekeeper.CLI.Services.IDevOpsService, Timekeeper.CLI.Services.DevOpsService>();
        services.AddScoped<Timekeeper.CLI.Services.IGitHubService, Timekeeper.CLI.Services.GitHubService>();
        services.AddScoped<ProviderIntegrationService>();
        services.AddScoped<DevOpsSyncService>();

        // Command Line Service
        services.AddScoped<Timekeeper.CLI.Commands.CommandLineService>();

        // UI
        services.AddScoped<MainMenuUI>();
        services.AddScoped<TodoItemUI>();
        services.AddScoped<TimeTrackingUI>();
        services.AddScoped<ReportsUI>();
        services.AddScoped<ConfigurationUI>();
        services.AddScoped<IntegrationsUI>();
        services.AddScoped<CalendarUI>();
        services.AddScoped<KanbanUI>();
    })
    .Build();

// Ensure database is created
using (var scope = host.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TimekeeperDbContext>();
    await context.Database.EnsureCreatedAsync();
}

// Handle command line arguments or start interactive mode
if (args.Length > 0)
{
    // Command line mode
    var commandLineService = host.Services.GetRequiredService<Timekeeper.CLI.Commands.CommandLineService>();
    var rootCommand = commandLineService.CreateRootCommand();
    await rootCommand.InvokeAsync(args);
}
else
{
    // Interactive mode (default)
    var mainMenu = host.Services.GetRequiredService<MainMenuUI>();
    await mainMenu.ShowAsync();
}
