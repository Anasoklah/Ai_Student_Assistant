using SyrianStudyBot.Domain;
using SyrianStudyBot.interfaces;

namespace SyrianStudyBot.Services;

public class RagPipelineService(
    IEmbeddingService embeddingService,
    IVectorSearchService vectorSearch,
    IChatService chatService,
    ILogger<RagPipelineService> logger) : IRagPipelineService
{
    // How many chunks to retrieve from the database per question
    private const int TopK = 5;

    public async Task<string> QueryAsync(string question, string mode, string? subject, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("RAG query | mode={Mode} subject={Subject} question={Question}", mode, subject, question);

        // Step 1: Turn the student's question into a vector
        // so we can compare it against all stored chunk vectors
        var questionVector = await embeddingService.GenerateEmbeddingAsync(question, cancellationToken);

        // Step 2: Find the top-K most relevant chunks in the database
        var chunks = await vectorSearch.SearchAsync(questionVector, subject, TopK, cancellationToken);

        if (chunks.Count == 0)
        {
            logger.LogWarning("No relevant chunks found for question: {Question}", question);
            return "I couldn't find any relevant study material for your question. Make sure the topic has been uploaded.";
        }

        // Step 3: Combine all retrieved chunks into one context block
        // The LLM will use this as its "knowledge" to answer the question
        var context = BuildContextWithSources(chunks);

        // Step 4: Pick the right prompt template based on the mode
        var systemPrompt = BuildSystemPrompt(mode, context);

        // Step 5: Send the question + context to the LLM and return the answer
        var answer = await chatService.CompleteAsync(systemPrompt, question, cancellationToken);

        logger.LogInformation("RAG answer generated ({Length} chars)", answer.Length);
        return answer;
    }

    // Returns a different system prompt depending on what the student wants:
    // explain = clear explanation, summary = bullet points, quiz = MCQ questions
    private static string BuildSystemPrompt(string mode, string context)
    {
        // IMPORTANT: always reply in the same language the student used.
        const string languageRule = """
            IMPORTANT: Always respond in the same language the student used in their question. If they wrote in Arabic, respond fully in Arabic. If they wrote in English, respond in English.
            IMPORTANT: Never use LaTeX notation. Write equations in plain text only. For example write "F = m × a" not "\[ F = m \times a \]". Use × for multiplication, ÷ for division, ² for squared, ³ for cubed.
            """;

        const string sourceRule = """
            IMPORTANT: Use only the provided context.
            IMPORTANT: At the end of every answer, include a Sources section.
            IMPORTANT: Cite only sources that were actually used in the answer.
            IMPORTANT: Cite sources using this exact format: - [SourceId] Book, page PageNumber.
            IMPORTANT: If the answer is not found in the context, say that clearly and do not invent sources.
            """;

        return mode switch
        {
            "summary" => $"""
                You are a study assistant. Summarize the following content
                clearly and concisely for a student preparing for an exam.
                Use bullet points. Keep it structured and easy to review.
                Base your summary ONLY on the context below.
                If the answer is not in the context, say so clearly.
                {languageRule}
                {sourceRule}

                Format:
                Summary:
                [your summary]

                Sources:
                - [S1] Book name, page 12

                Context:
                {context}
                """,

            "quiz" => $"""
                You are a quiz generator. Based ONLY on the context below,
                generate 5 multiple choice questions with 4 options each.
                Mark the correct answer clearly.
                Do NOT use any knowledge outside the provided context.
                If the context is insufficient, generate fewer questions and say so.
                {languageRule}
                {sourceRule}

                Format each question as:
                Q: [question]
                A) [option]
                B) [option]
                C) [option]
                D) [option]
                Answer: [letter]

                Sources:
                - [S1] Book name, page 12

                Context:
                {context}
                """,

            // default = "explain"
            _ => $"""
                You are a helpful tutor. Explain the concept clearly for a student.
                Use simple language. Use examples where helpful.
                Base your explanation ONLY on the context below.
                If the answer is not in the context, say so clearly.
                {languageRule}
                {sourceRule}

                Format:
                Answer:
                [your explanation]

                Sources:
                - [S1] Book name, page 12

                Context:
                {context}
                """
        };
    }

    private static string BuildContextWithSources(IReadOnlyList<DocumentChunk> chunks)
    {
        return string.Join(
            "\n\n---\n\n",
            chunks.Select((chunk, index) => $"""
                SourceId: S{index + 1}
                Book: {GetSourceName(chunk)}
                Title: {chunk.Document.Title}
                Subject: {chunk.Document.Subject}
                Edition: {chunk.Document.Edition ?? "Unknown"}
                Page: {chunk.PageNumber?.ToString() ?? "Unknown"}
                Chapter: {chunk.ChapterTitle ?? "Unknown"}
                Section: {chunk.SectionTitle ?? "Unknown"}

                Content:
                {chunk.Content}
                """));
    }

    private static string GetSourceName(DocumentChunk chunk) =>
        string.IsNullOrWhiteSpace(chunk.Document.SourceName)
            ? chunk.Document.Title
            : chunk.Document.SourceName;
}
