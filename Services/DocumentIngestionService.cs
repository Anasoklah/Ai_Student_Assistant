using Pgvector;
using SyrianStudyBot.Data;
using SyrianStudyBot.Domain;
using SyrianStudyBot.Dtos;
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

    public async Task<Document> IngestAsync(DocumentIngestionRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting ingestion for document '{Title}' (subject: {Subject})", requestDto.Title, requestDto.Subject);

        // step 1: Create Document without Chuncks 
        var document = new Document
        {
            Title = requestDto.Title,
            Subject = requestDto.Subject ,
            GradeLevel = requestDto.GradeLevel,
            SourceName = requestDto.SourceName ,
            Edition = requestDto.Edition,
            Language = requestDto.Language,
            DocumentType = requestDto.DocumentType,
            IsApproved = requestDto.DocumentType == Domain.Enums.DocumentType.OfficialBook
        };


        // Step 2: Split the raw text into overlapping chunks
        var chunks = requestDto.Pages 
        .SelectMany(page => SplitIntoChunks(page.Text)
        .Select(chunk => new PageChunk (
            page.PageNumber,
            chunk.Text ,
            chunk.StartWord,
            chunk.EndWord
            ))).ToList();
        logger.LogInformation("Split into {Count} chunks", chunks.Count);

     
        // step 3: take texts from each chunck to embed it 
        var chunkTexts = chunks.Select(c => c.Text).ToList();

        // Step 4: Embed all chunks in one batch call (much faster than one-by-one)
        var embeddings = await embeddingService.GenerateEmbeddingsAsync(chunkTexts, cancellationToken);
        logger.LogInformation("Generated {Count} embeddings", embeddings.Count);
    

        //step 5: add Chunks to Document 
        for (var i = 0; i < chunks.Count; i++)
        {
            document.Chunks.Add(new DocumentChunk
            {
                ChunkIndex = i,
                PageNumber = chunks[i].PageNumber,
                StartWord = chunks[i].StartWord,
                EndWord = chunks[i].EndWord,
                Content = chunks[i].Text,
                Embedding = new Vector(embeddings[i])
            });
            
        }

        // Step 6: Save the document and all its chunks to the database in one transaction
        db.Documents.Add(document);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Saved document '{Title}' with {Count} chunks to database", document.Title, document.Chunks.Count);
        return document;
    }

    // Splits text into overlapping windows of words.
    // Example with ChunkSize=5 and ChunkOverlap=2:
    //   words: [A B C D E F G H]
    //   chunk1: [A B C D E]
    //   chunk2: [D E F G H]  ← starts 3 words back (overlap of D,E)
    private static List<(string Text, int StartWord, int EndWord)> SplitIntoChunks(string text)
    {
        // Split into individual words, removing empty entries
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var chunks = new List<(string Text, int StartWord, int EndWord)>();
        int step = ChunkSize - ChunkOverlap; // how far we advance each time
        int start = 0;

        while (start < words.Length)
        {
            int end = Math.Min(start + ChunkSize, words.Length);
            string chunkText = string.Join(' ', words[start..end]);
            chunks.Add((chunkText, start , end -1));
            start += step;
        }

        return chunks;
    }

    private sealed record PageChunk(
    int PageNumber,
    string Text,
    int StartWord,
    int EndWord);
}
