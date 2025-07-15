using Timekeeper.Domain.Common;

namespace Timekeeper.Domain.Entities;

public class ProviderIntegration : BaseEntity
{
    public string Provider { get; set; } = string.Empty; // "AzureDevOps", "GitHub", etc.
    public string OrganizationUrl { get; set; } = string.Empty;
    public string PersonalAccessToken { get; set; } = string.Empty;
    public string? ProjectName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastSyncAt { get; set; }
}
