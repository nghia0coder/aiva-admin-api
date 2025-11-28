using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aiva.Admin.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddColumnStorageTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsContainerProvisioned",
                table: "Storages",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsContainerProvisioned",
                table: "Storages");
        }
    }
}
