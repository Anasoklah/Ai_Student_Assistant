using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SyrianStudyBot.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentChunkNeedsReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NeedsReview",
                table: "DocumentChunks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentChunks_NeedsReview",
                table: "DocumentChunks",
                column: "NeedsReview");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DocumentChunks_NeedsReview",
                table: "DocumentChunks");

            migrationBuilder.DropColumn(
                name: "NeedsReview",
                table: "DocumentChunks");
        }
    }
}
