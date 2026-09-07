using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetFlow.Migrations
{
    /// <inheritdoc />
    public partial class AddMaintenanceInterval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaintenanceIntervalDays",
                table: "Assets",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaintenanceIntervalDays",
                table: "Assets");
        }
    }
}
