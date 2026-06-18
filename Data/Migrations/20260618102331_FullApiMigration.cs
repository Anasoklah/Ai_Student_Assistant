using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SyrianStudyBot.Data.Migrations
{
    /// <inheritdoc />
    public partial class FullApiMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserSessions");

            migrationBuilder.DropIndex(
                name: "IX_QuizSessions_TelegramUserId",
                table: "QuizSessions");

            migrationBuilder.DropIndex(
                name: "IX_Documents_SourceName",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_DocumentChunks_PageNumber",
                table: "DocumentChunks");

            migrationBuilder.DropColumn(
                name: "TelegramUserId",
                table: "QuizSessions");

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "QuizSessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GradeLevel",
                table: "QuizSessions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "QuizSessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxScore",
                table: "QuizSessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Subject",
                table: "QuizSessions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalQuestions",
                table: "QuizSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "QuizSessions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UserId1",
                table: "QuizSessions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Documents",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Subject",
                table: "Documents",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "SourceName",
                table: "Documents",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Edition",
                table: "Documents",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DocumentType",
                table: "Documents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                table: "Documents",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FileSizeBytes",
                table: "Documents",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "GradeLevel",
                table: "Documents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "Documents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "UploadedByUserId",
                table: "Documents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SectionTitle",
                table: "DocumentChunks",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "DocumentChunks",
                type: "character varying(10000)",
                maxLength: 10000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ChapterTitle",
                table: "DocumentChunks",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GradeLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PreferredLanguage = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true, defaultValue: "ar"),
                    SubscriptionTier = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Free"),
                    SubscriptionExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MessagesToday = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastMessageReset = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UploadsThisMonth = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastUploadReset = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChatSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Subject = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Mode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastActiveAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DailyUsageLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId1 = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MessageCount = table.Column<int>(type: "integer", nullable: false),
                    UploadCount = table.Column<int>(type: "integer", nullable: false),
                    TokensUsed = table.Column<long>(type: "bigint", nullable: false),
                    EstimatedCost = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyUsageLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyUsageLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DailyUsageLogs_Users_UserId1",
                        column: x => x.UserId1,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetTier = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Method = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ProviderTransactionId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ProviderResponse = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuizResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuizSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Subject = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    MaxScore = table.Column<int>(type: "integer", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuizResults_QuizSessions_QuizSessionId",
                        column: x => x.QuizSessionId,
                        principalTable: "QuizSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuizResults_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "character varying(50000)", maxLength: 50000, nullable: false),
                    SourcesJson = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PromptTokens = table.Column<int>(type: "integer", nullable: true),
                    CompletionTokens = table.Column<int>(type: "integer", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMessages_ChatSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "ChatSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuizSessions_IsCompleted",
                table: "QuizSessions",
                column: "IsCompleted");

            migrationBuilder.CreateIndex(
                name: "IX_QuizSessions_Subject",
                table: "QuizSessions",
                column: "Subject");

            migrationBuilder.CreateIndex(
                name: "IX_QuizSessions_UserId",
                table: "QuizSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizSessions_UserId1",
                table: "QuizSessions",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_DocumentType",
                table: "Documents",
                column: "DocumentType");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_GradeLevel",
                table: "Documents",
                column: "GradeLevel");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_IsApproved",
                table: "Documents",
                column: "IsApproved");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_UploadedByUserId",
                table: "Documents",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentChunks_Embedding",
                table: "DocumentChunks",
                column: "Embedding")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_Role",
                table: "ChatMessages",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SessionId",
                table: "ChatMessages",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_Timestamp",
                table: "ChatMessages",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_IsActive",
                table: "ChatSessions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_LastActiveAt",
                table: "ChatSessions",
                column: "LastActiveAt");

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_Subject",
                table: "ChatSessions",
                column: "Subject");

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_UserId",
                table: "ChatSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyUsageLogs_Date",
                table: "DailyUsageLogs",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_DailyUsageLogs_UserId_Date",
                table: "DailyUsageLogs",
                columns: new[] { "UserId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyUsageLogs_UserId1",
                table: "DailyUsageLogs",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ProviderTransactionId",
                table: "Payments",
                column: "ProviderTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Status",
                table: "Payments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_UserId",
                table: "Payments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizResults_CompletedAt",
                table: "QuizResults",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_QuizResults_QuizSessionId",
                table: "QuizResults",
                column: "QuizSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizResults_Subject",
                table: "QuizResults",
                column: "Subject");

            migrationBuilder.CreateIndex(
                name: "IX_QuizResults_UserId",
                table: "QuizResults",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_GradeLevel",
                table: "Users",
                column: "GradeLevel");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsActive",
                table: "Users",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Users_LastMessageReset",
                table: "Users",
                column: "LastMessageReset");

            migrationBuilder.CreateIndex(
                name: "IX_Users_SubscriptionTier",
                table: "Users",
                column: "SubscriptionTier");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Users_UploadedByUserId",
                table: "Documents",
                column: "UploadedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_QuizSessions_Users_UserId",
                table: "QuizSessions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_QuizSessions_Users_UserId1",
                table: "QuizSessions",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Users_UploadedByUserId",
                table: "Documents");

            migrationBuilder.DropForeignKey(
                name: "FK_QuizSessions_Users_UserId",
                table: "QuizSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_QuizSessions_Users_UserId1",
                table: "QuizSessions");

            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "DailyUsageLogs");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "QuizResults");

            migrationBuilder.DropTable(
                name: "ChatSessions");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropIndex(
                name: "IX_QuizSessions_IsCompleted",
                table: "QuizSessions");

            migrationBuilder.DropIndex(
                name: "IX_QuizSessions_Subject",
                table: "QuizSessions");

            migrationBuilder.DropIndex(
                name: "IX_QuizSessions_UserId",
                table: "QuizSessions");

            migrationBuilder.DropIndex(
                name: "IX_QuizSessions_UserId1",
                table: "QuizSessions");

            migrationBuilder.DropIndex(
                name: "IX_Documents_DocumentType",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_GradeLevel",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_IsApproved",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_UploadedByUserId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_DocumentChunks_Embedding",
                table: "DocumentChunks");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "QuizSessions");

            migrationBuilder.DropColumn(
                name: "GradeLevel",
                table: "QuizSessions");

            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "QuizSessions");

            migrationBuilder.DropColumn(
                name: "MaxScore",
                table: "QuizSessions");

            migrationBuilder.DropColumn(
                name: "Subject",
                table: "QuizSessions");

            migrationBuilder.DropColumn(
                name: "TotalQuestions",
                table: "QuizSessions");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "QuizSessions");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "QuizSessions");

            migrationBuilder.DropColumn(
                name: "DocumentType",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "FilePath",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "FileSizeBytes",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "GradeLevel",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "UploadedByUserId",
                table: "Documents");

            migrationBuilder.AddColumn<long>(
                name: "TelegramUserId",
                table: "QuizSessions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Documents",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Subject",
                table: "Documents",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "SourceName",
                table: "Documents",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300);

            migrationBuilder.AlterColumn<string>(
                name: "Edition",
                table: "Documents",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SectionTitle",
                table: "DocumentChunks",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "DocumentChunks",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10000)",
                oldMaxLength: 10000);

            migrationBuilder.AlterColumn<string>(
                name: "ChapterTitle",
                table: "DocumentChunks",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "UserSessions",
                columns: table => new
                {
                    TelegramUserId = table.Column<long>(type: "bigint", nullable: false),
                    CurrentMode = table.Column<string>(type: "text", nullable: false),
                    CurrentSubject = table.Column<string>(type: "text", nullable: true),
                    LastActive = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessions", x => x.TelegramUserId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuizSessions_TelegramUserId",
                table: "QuizSessions",
                column: "TelegramUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_SourceName",
                table: "Documents",
                column: "SourceName");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentChunks_PageNumber",
                table: "DocumentChunks",
                column: "PageNumber");
        }
    }
}
