using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SyrianStudyBot.Migrations
{
    /// <inheritdoc />
    public partial class ResolvetheDocumentEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FilePath",
                table: "Documents");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                table: "Documents",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }
    }
}
