using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetFlow.Migrations
{
    /// <inheritdoc />
    public partial class AddNextMaintenanceDueField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "NextMaintenanceDue",
                table: "Assets",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NextMaintenanceDue",
                table: "Assets");
        }
    }
}
