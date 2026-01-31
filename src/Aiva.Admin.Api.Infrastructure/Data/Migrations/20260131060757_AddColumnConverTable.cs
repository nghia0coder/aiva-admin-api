using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aiva.Admin.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddColumnConverTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResponseType",
                table: "ChatMessages",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Text");

            migrationBuilder.AddColumn<string>(
                name: "StructuredDataJson",
                table: "ChatMessages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ResponseType",
                table: "ChatMessages",
                column: "ResponseType",
                filter: "[ResponseType] <> 'Text'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChatMessages_ResponseType",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "ResponseType",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "StructuredDataJson",
                table: "ChatMessages");
        }
    }
}
