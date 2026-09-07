using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetFlow.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckoutLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CheckoutRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    AssetRequestId = table.Column<int>(type: "int", nullable: true),
                    EmployeeName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EmployeeEmail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Department = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CheckedOutOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReturnedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CheckoutNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ConditionOnReturn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckoutRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CheckoutRecords_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CheckoutRecords_AssetId_ReturnedOn",
                table: "CheckoutRecords",
                columns: new[] { "AssetId", "ReturnedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_CheckoutRecords_CheckedOutOn",
                table: "CheckoutRecords",
                column: "CheckedOutOn");

            migrationBuilder.CreateIndex(
                name: "IX_CheckoutRecords_Department",
                table: "CheckoutRecords",
                column: "Department");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CheckoutRecords");
        }
    }
}
