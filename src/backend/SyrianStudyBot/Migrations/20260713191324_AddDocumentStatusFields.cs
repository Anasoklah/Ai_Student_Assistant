using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SyrianStudyBot.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentStatusFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Documents_IsApproved",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_Subject_GradeLevel_DocumentType_IsApproved",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_ChatSessions_Subject",
                table: "ChatSessions");

            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ChapterFilter",
                table: "ChatSessions");

            migrationBuilder.DropColumn(
                name: "SectionFilter",
                table: "ChatSessions");

            migrationBuilder.DropColumn(
                name: "Subject",
                table: "ChatSessions");

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessedAt",
                table: "Documents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Documents",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Processing");

            migrationBuilder.AddColumn<string>(
                name: "StatusMessage",
                table: "Documents",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ChapterId",
                table: "DocumentChunks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SectionId",
                table: "DocumentChunks",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_Status",
                table: "Documents",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_Subject_GradeLevel_DocumentType",
                table: "Documents",
                columns: new[] { "Subject", "GradeLevel", "DocumentType" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentChunks_ChapterId",
                table: "DocumentChunks",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentChunks_PageNumber",
                table: "DocumentChunks",
                column: "PageNumber");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentChunks_SectionId",
                table: "DocumentChunks",
                column: "SectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentChunks_BookChapters_ChapterId",
                table: "DocumentChunks",
                column: "ChapterId",
                principalTable: "BookChapters",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentChunks_BookSections_SectionId",
                table: "DocumentChunks",
                column: "SectionId",
                principalTable: "BookSections",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentChunks_BookChapters_ChapterId",
                table: "DocumentChunks");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentChunks_BookSections_SectionId",
                table: "DocumentChunks");

            migrationBuilder.DropIndex(
                name: "IX_Documents_Status",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_Subject_GradeLevel_DocumentType",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_DocumentChunks_ChapterId",
                table: "DocumentChunks");

            migrationBuilder.DropIndex(
                name: "IX_DocumentChunks_PageNumber",
                table: "DocumentChunks");

            migrationBuilder.DropIndex(
                name: "IX_DocumentChunks_SectionId",
                table: "DocumentChunks");

            migrationBuilder.DropColumn(
                name: "ProcessedAt",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "StatusMessage",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ChapterId",
                table: "DocumentChunks");

            migrationBuilder.DropColumn(
                name: "SectionId",
                table: "DocumentChunks");

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "Documents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ChapterFilter",
                table: "ChatSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SectionFilter",
                table: "ChatSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Subject",
                table: "ChatSessions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_IsApproved",
                table: "Documents",
                column: "IsApproved");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_Subject_GradeLevel_DocumentType_IsApproved",
                table: "Documents",
                columns: new[] { "Subject", "GradeLevel", "DocumentType", "IsApproved" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_Subject",
                table: "ChatSessions",
                column: "Subject");
        }
    }
}
