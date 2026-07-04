using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SyrianStudyBot.Migrations
{
    /// <inheritdoc />
    public partial class Addingpg_trgmIndexForOptimizeSearch2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
               migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

                // Trigram indexes for Arabic substring matching
                migrationBuilder.Sql(@"
                    CREATE INDEX IF NOT EXISTS ix_chunks_chapter_trgm
                    ON ""DocumentChunks"" USING gin (""ChapterTitle"" gin_trgm_ops);
                ");

                migrationBuilder.Sql(@"
                    CREATE INDEX IF NOT EXISTS ix_chunks_section_trgm
                    ON ""DocumentChunks"" USING gin (""SectionTitle"" gin_trgm_ops);
                ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
