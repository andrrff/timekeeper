using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timekeeper.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDevOpsAndEstimatedHoursToTodoItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DevOpsUrl",
                table: "TodoItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DevOpsWorkItemId",
                table: "TodoItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstimatedHours",
                table: "TodoItems",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DevOpsUrl",
                table: "TodoItems");

            migrationBuilder.DropColumn(
                name: "DevOpsWorkItemId",
                table: "TodoItems");

            migrationBuilder.DropColumn(
                name: "EstimatedHours",
                table: "TodoItems");
        }
    }
}
