using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Domain.Enums;
using SyrianStudyBot.Interfaces;

namespace SyrianStudyBot.Infrastructure.Ai.Rag;

public class RagPipelineService(
    IEmbeddingService embeddingService,
    IVectorSearchService vectorSearch,
    IChatService chatService,
    ILogger<RagPipelineService> logger) : IRagPipelineService
{
    private const int TopK = 5;

    public async Task<string> QueryAsync(string question, ChatMode mode, Subject? subject, string? sectionFilter = null, string? chapterFilter = null, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("RAG query | mode={Mode} subject={Subject} question={Question}", mode, subject, question);

        var questionVector = await embeddingService.GenerateEmbeddingAsync(question, cancellationToken);

        var chunks = await vectorSearch.SearchAsync(questionVector, subject, TopK, sectionFilter, chapterFilter, cancellationToken);

        if (chunks.Count == 0)
        {
            logger.LogWarning("No relevant chunks found for question: {Question}", question);
            return "I couldn't find any relevant study material for your question. Make sure the topic has been uploaded.";
        }

        var context = BuildContextWithSources(chunks);

        var systemPrompt = BuildSystemPrompt(mode, context);

        var answer = await chatService.CompleteAsync(systemPrompt, question, cancellationToken);

        logger.LogInformation("RAG answer generated ({Length} chars)", answer.Length);
        return answer;
    }

    private static string BuildSystemPrompt(ChatMode mode, string context)
    {
        const string languageRule = """
            IMPORTANT: Always respond in the same language the student used in their question. If they wrote in Arabic, respond fully in Arabic. If they wrote in English, respond in English.
            IMPORTANT: Always use LaTeX notation for math. Use $...$ for inline equations (e.g., $F = ma$) and $$...$$ for display equations (e.g., $$E = mc^2$$). Never use plain-text substitutes like × or ÷ for equations.
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
            ChatMode.Summary => $"""
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

            ChatMode.Quiz => $"""
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
