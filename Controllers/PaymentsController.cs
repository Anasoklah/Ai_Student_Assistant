using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyrianStudyBot.Application.UseCases;
using SyrianStudyBot.Domain.Enums;
using SyrianStudyBot.Dtos;

namespace SyrianStudyBot.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize(Policy = "StudentOnly")]
public class PaymentsController(
    IPaymentUseCase paymentUseCase) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequestDto request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(new { message = "User not authenticated" });

        if (request.TargetTier == SubscriptionTier.Free)
            return BadRequest(new { message = "Free tier does not require payment" });

        if (request.Amount <= 0)
            return BadRequest(new { message = "Amount must be greater than zero" });

        var payment = await paymentUseCase.CreatePaymentAsync(userId, request, cancellationToken);
        return Ok(payment);
    }

    [HttpPost("{paymentId:guid}/proof")]
    public async Task<IActionResult> SubmitProof(Guid paymentId, [FromBody] SubmitPaymentProofRequestDto request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(new { message = "User not authenticated" });

        if (string.IsNullOrWhiteSpace(request.ProviderTransactionId))
            return BadRequest(new { message = "Provider transaction id is required" });

        try
        {
            var payment = await paymentUseCase.SubmitProofAsync(userId, paymentId, request, cancellationToken);
            return payment is null ? NotFound(new { message = "Payment not found" }) : Ok(payment);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("mine")]
    public async Task<ActionResult<PagedResponse<PaymentResponseDto>>> GetMyPayments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(new { message = "User not authenticated" });

        var response = await paymentUseCase.GetMyPaymentsAsync(userId, page, pageSize, cancellationToken);
        return Ok(response);
    }

    [HttpGet("admin")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<PagedResponse<PaymentResponseDto>>> GetPaymentsForAdmin(
        [FromQuery] PaymentStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var response = await paymentUseCase.GetPaymentsForAdminAsync(status, page, pageSize, cancellationToken);
        return Ok(response);
    }

    [HttpPost("admin/{paymentId:guid}/review")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ReviewPayment(Guid paymentId, [FromBody] ReviewPaymentRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var payment = await paymentUseCase.ReviewPaymentAsync(paymentId, request, cancellationToken);
            return payment is null ? NotFound(new { message = "Payment not found" }) : Ok(payment);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
