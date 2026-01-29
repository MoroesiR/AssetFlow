using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetFlow.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckoutAndBusinessFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignedToUserId",
                table: "Assets");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "Assets",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActualReturnDate",
                table: "Assets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CheckedOutToEmployee",
                table: "Assets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckoutDate",
                table: "Assets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CheckoutNotes",
                table: "Assets",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConditionNotes",
                table: "Assets",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmployeeDepartment",
                table: "Assets",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmployeeEmail",
                table: "Assets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpectedReturnDate",
                table: "Assets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresMaintenance",
                table: "Assets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Vendor",
                table: "Assets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WarrantyExpiry",
                table: "Assets",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualReturnDate",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "CheckedOutToEmployee",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "CheckoutDate",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "CheckoutNotes",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "ConditionNotes",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "EmployeeDepartment",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "EmployeeEmail",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "ExpectedReturnDate",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "RequiresMaintenance",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "Vendor",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "WarrantyExpiry",
                table: "Assets");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "Assets",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignedToUserId",
                table: "Assets",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
