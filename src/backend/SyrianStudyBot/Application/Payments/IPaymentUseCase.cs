using SyrianStudyBot.Domain.Enums;
using SyrianStudyBot.Application.Payments.Dtos;

namespace SyrianStudyBot.Application.Payments;

public interface IPaymentUseCase
{
    Task<PaymentResponseDto> CreatePaymentAsync(Guid userId, CreatePaymentRequestDto request, CancellationToken cancellationToken = default);
    Task<PaymentResponseDto> SubmitProofAsync(Guid userId, Guid paymentId, SubmitPaymentProofRequestDto request, CancellationToken cancellationToken = default);
    Task<PagedResponse<PaymentResponseDto>> GetMyPaymentsAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResponse<PaymentResponseDto>> GetPaymentsForAdminAsync(PaymentStatus? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PaymentResponseDto> ReviewPaymentAsync(Guid paymentId, ReviewPaymentRequestDto request, CancellationToken cancellationToken = default);
}
