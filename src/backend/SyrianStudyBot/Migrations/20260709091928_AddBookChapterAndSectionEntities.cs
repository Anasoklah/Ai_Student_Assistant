using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SyrianStudyBot.Migrations
{
    /// <inheritdoc />
    public partial class AddBookChapterAndSectionEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BookChapters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChapterNumber = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    NormalizedTitle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    StartPage = table.Column<int>(type: "integer", nullable: true),
                    EndPage = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookChapters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookChapters_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BookSections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChapterId = table.Column<Guid>(type: "uuid", nullable: false),
                    SectionNumber = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    NormalizedTitle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    StartPage = table.Column<int>(type: "integer", nullable: true),
                    EndPage = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookSections_BookChapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "BookChapters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookChapters_DocumentId",
                table: "BookChapters",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_BookChapters_DocumentId_ChapterNumber",
                table: "BookChapters",
                columns: new[] { "DocumentId", "ChapterNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookSections_ChapterId",
                table: "BookSections",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_BookSections_ChapterId_SectionNumber",
                table: "BookSections",
                columns: new[] { "ChapterId", "SectionNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookSections");

            migrationBuilder.DropTable(
                name: "BookChapters");
        }
    }
}
