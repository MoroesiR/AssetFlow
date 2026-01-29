using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetFlow.Migrations
{
    /// <inheritdoc />
    public partial class AddMaintenanceTrackingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastMaintenanceDate",
                table: "Assets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaintenanceNotes",
                table: "Assets",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastMaintenanceDate",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "MaintenanceNotes",
                table: "Assets");
        }
    }
}
