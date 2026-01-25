using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aiva.Admin.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIsDeletedColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SystemPrompts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Storages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Folders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Files",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "FileMetadata",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SystemPrompts");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Storages");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Folders");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "FileMetadata");
        }
    }
}
