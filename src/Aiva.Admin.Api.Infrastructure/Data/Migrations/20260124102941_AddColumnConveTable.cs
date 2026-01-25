using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aiva.Admin.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddColumnConveTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Conversations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOnUtc",
                table: "Conversations",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Conversations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "Conversations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedOnUtc",
                table: "Conversations",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "CreatedOnUtc",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "ModifiedOnUtc",
                table: "Conversations");
        }
    }
}
