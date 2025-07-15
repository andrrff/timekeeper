using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timekeeper.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameDevOpsIntegrationsToProviderIntegrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "DevOpsIntegrations",
                newName: "ProviderIntegrations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "ProviderIntegrations",
                newName: "DevOpsIntegrations");
        }
    }
}
