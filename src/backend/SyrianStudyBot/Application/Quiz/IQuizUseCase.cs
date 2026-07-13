using SyrianStudyBot.Application.Quiz.Dtos;

namespace SyrianStudyBot.Application.Quiz;

public interface IQuizUseCase
{
    Task<QuizSessionResponseDto> GenerateQuizAsync(Guid userId, GenerateQuizRequestDto request, CancellationToken cancellationToken = default);
    Task<PagedResponse<QuizSessionResponseDto>> GetHistoryAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<QuizSessionResponseDto?> GetQuizAsync(Guid userId, Guid quizSessionId, CancellationToken cancellationToken = default);
    Task<QuizResultResponseDto?> SubmitQuizAsync(Guid userId, Guid quizSessionId, SubmitQuizRequestDto request, CancellationToken cancellationToken = default);
}
