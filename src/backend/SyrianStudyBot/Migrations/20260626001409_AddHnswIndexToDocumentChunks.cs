using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SyrianStudyBot.Migrations
{
    /// <inheritdoc />
    public partial class AddHnswIndexToDocumentChunks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
             migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS "idx_document_chunks_embedding_cosine"
            ON "DocumentChunks"
            USING hnsw ("Embedding" vector_cosine_ops)
            WITH (m = 16, ef_construction = 64);
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
              migrationBuilder.Sql("""
            DROP INDEX IF EXISTS "idx_document_chunks_embedding_cosine";
            """);
        }
    }
}
