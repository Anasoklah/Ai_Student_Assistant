using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SyrianStudyBot.Migrations
{
    /// <inheritdoc />
    public partial class Addingpg_trgmIndexForOptimizeSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Mode",
                table: "ChatSessions");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "ChatSessions",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300,
                oldNullable: true);

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
                name: "Mode",
                table: "ChatMessages",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Explain");
                    }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChapterFilter",
                table: "ChatSessions");

            migrationBuilder.DropColumn(
                name: "SectionFilter",
                table: "ChatSessions");

            migrationBuilder.DropColumn(
                name: "Mode",
                table: "ChatMessages");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "ChatSessions",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300);

            migrationBuilder.AddColumn<string>(
                name: "Mode",
                table: "ChatSessions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Explain");
        }
    }
}
