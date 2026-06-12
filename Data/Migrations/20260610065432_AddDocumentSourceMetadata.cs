using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SyrianStudyBot.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentSourceMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Edition",
                table: "Documents",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "Documents",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceName",
                table: "Documents",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ChapterTitle",
                table: "DocumentChunks",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EndWord",
                table: "DocumentChunks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PageNumber",
                table: "DocumentChunks",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SectionTitle",
                table: "DocumentChunks",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StartWord",
                table: "DocumentChunks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_SourceName",
                table: "Documents",
                column: "SourceName");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_Subject",
                table: "Documents",
                column: "Subject");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentChunks_PageNumber",
                table: "DocumentChunks",
                column: "PageNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Documents_SourceName",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_Subject",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_DocumentChunks_PageNumber",
                table: "DocumentChunks");

            migrationBuilder.DropColumn(
                name: "Edition",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "SourceName",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ChapterTitle",
                table: "DocumentChunks");

            migrationBuilder.DropColumn(
                name: "EndWord",
                table: "DocumentChunks");

            migrationBuilder.DropColumn(
                name: "PageNumber",
                table: "DocumentChunks");

            migrationBuilder.DropColumn(
                name: "SectionTitle",
                table: "DocumentChunks");

            migrationBuilder.DropColumn(
                name: "StartWord",
                table: "DocumentChunks");
        }
    }
}
