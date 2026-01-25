using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aiva.Admin.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDeleteAtColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "SystemPrompts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Storages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Folders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Files",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "FileMetadata",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Conversations",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "SystemPrompts");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Storages");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Folders");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "FileMetadata");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Conversations");
        }
    }
}
