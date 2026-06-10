using Pgvector;
using SyrianStudyBot.Data;
using SyrianStudyBot.Domain;
using SyrianStudyBot.interfaces;

namespace SyrianStudyBot.Services;

public class DocumentIngestionService(
    AppDbContext db,
    IEmbeddingService embeddingService,
    ILogger<DocumentIngestionService> logger) : IDocumentIngestionService
{
    // How many words per chunk — kept small to stay within Ollama's
    // default 2048 token context window (150 words ≈ 200 tokens)
    private const int ChunkSize = 150;

    // How many words overlap between consecutive chunks
    // so context is not lost at chunk boundaries
    private const int ChunkOverlap = 20;

    public async Task<Document> IngestAsync(string title, string subject, string rawText, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting ingestion for document '{Title}' (subject: {Subject})", title, subject);

        // Step 1: Split the raw text into overlapping chunks
        var chunks = SplitIntoChunks(rawText);
        logger.LogInformation("Split into {Count} chunks", chunks.Count);

        // Step 2: Embed all chunks in one batch call (much faster than one-by-one)
        var chunkTexts = chunks.Select(c => c.Text).ToList();
        var embeddings = await embeddingService.GenerateEmbeddingsAsync(chunkTexts, cancellationToken);
        logger.LogInformation("Generated {Count} embeddings", embeddings.Count);

        // Step 3: Build the Document record
        var document = new Document
        {
            Title = title,
            Subject = subject
        };

        // Step 4: Build a DocumentChunk for each piece of text + its embedding
        for (int i = 0; i < chunks.Count; i++)
        {
            document.Chunks.Add(new DocumentChunk
            {
                ChunkIndex = i,
                Content = chunks[i].Text,
                // Wrap the float[] in a Vector so pgvector can store it
                Embedding = new Vector(embeddings[i])
            });
        }

        // Step 5: Save the document and all its chunks to the database in one transaction
        db.Documents.Add(document);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Saved document '{Title}' with {Count} chunks to database", title, document.Chunks.Count);
        return document;
    }

    // Splits text into overlapping windows of words.
    // Example with ChunkSize=5 and ChunkOverlap=2:
    //   words: [A B C D E F G H]
    //   chunk1: [A B C D E]
    //   chunk2: [D E F G H]  ← starts 3 words back (overlap of D,E)
    private static List<(string Text, int StartWord)> SplitIntoChunks(string text)
    {
        // Split into individual words, removing empty entries
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var chunks = new List<(string Text, int StartWord)>();
        int step = ChunkSize - ChunkOverlap; // how far we advance each time
        int start = 0;

        while (start < words.Length)
        {
            int end = Math.Min(start + ChunkSize, words.Length);
            string chunkText = string.Join(' ', words[start..end]);
            chunks.Add((chunkText, start));
            start += step;
        }

        return chunks;
    }
}
