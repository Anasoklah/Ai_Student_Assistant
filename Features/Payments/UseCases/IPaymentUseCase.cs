using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Domain.Enums;
using SyrianStudyBot.Features.Payments.Dtos;
using SyrianStudyBot.Features.Common.Dtos;

namespace SyrianStudyBot.Features.Payments.UseCases;

public interface IPaymentUseCase
{
    Task<PaymentResponseDto> CreatePaymentAsync(Guid userId, CreatePaymentRequestDto request, CancellationToken cancellationToken = default);
    Task<PaymentResponseDto> SubmitProofAsync(Guid userId, Guid paymentId, SubmitPaymentProofRequestDto request, CancellationToken cancellationToken = default);
    Task<PagedResponse<PaymentResponseDto>> GetMyPaymentsAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResponse<PaymentResponseDto>> GetPaymentsForAdminAsync(PaymentStatus? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PaymentResponseDto> ReviewPaymentAsync(Guid paymentId, ReviewPaymentRequestDto request, CancellationToken cancellationToken = default);
}
