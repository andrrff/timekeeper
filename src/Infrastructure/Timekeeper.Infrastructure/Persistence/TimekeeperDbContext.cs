using Microsoft.EntityFrameworkCore;
using Timekeeper.Domain.Entities;

namespace Timekeeper.Infrastructure.Persistence;

public class TimekeeperDbContext : DbContext
{
    public TimekeeperDbContext(DbContextOptions<TimekeeperDbContext> options) : base(options)
    {
    }

    public DbSet<TodoItem> TodoItems { get; set; }
    public DbSet<TimeEntry> TimeEntries { get; set; }
    public DbSet<ActivityLog> ActivityLogs { get; set; }
    public DbSet<ProviderIntegration> ProviderIntegrations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // TodoItem configuration
        modelBuilder.Entity<TodoItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.Tags).HasMaxLength(500);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.Priority).HasConversion<string>();
            
            entity.HasMany(e => e.TimeEntries)
                  .WithOne(e => e.TodoItem)
                  .HasForeignKey(e => e.TodoItemId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasMany(e => e.ActivityLogs)
                  .WithOne(e => e.TodoItem)
                  .HasForeignKey(e => e.TodoItemId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // TimeEntry configuration
        modelBuilder.Entity<TimeEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).HasMaxLength(500);
            // TodoItemId is required for TimeEntry entities
            entity.Property(e => e.TodoItemId).IsRequired();
        });

        // ActivityLog configuration
        modelBuilder.Entity<ActivityLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Details).HasMaxLength(1000);
            entity.Property(e => e.LogType).HasConversion<string>();
            // TodoItemId is required for ActivityLog entities
            entity.Property(e => e.TodoItemId).IsRequired();
        });

        // ProviderIntegration configuration
        modelBuilder.Entity<ProviderIntegration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Provider).IsRequired().HasMaxLength(50);
            entity.Property(e => e.OrganizationUrl).IsRequired().HasMaxLength(500);
            entity.Property(e => e.PersonalAccessToken).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ProjectName).HasMaxLength(200);
        });
    }
}
