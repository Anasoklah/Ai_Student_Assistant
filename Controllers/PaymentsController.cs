using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SyrianStudyBot.Common.Extensions;
using SyrianStudyBot.Common.Mappers;
using SyrianStudyBot.Common.Services;
using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Enums;
using SyrianStudyBot.Dtos;

namespace SyrianStudyBot.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize(Policy = "StudentOnly")]
public class PaymentsController(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    IPagingService pagingService) : ControllerBase
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

        var payment = new Payment
        {
            UserId = userId,
            TargetTier = request.TargetTier,
            Amount = request.Amount,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "USD" : request.Currency.Trim().ToUpperInvariant(),
            Method = PaymentMethod.ShamCash,
            Status = PaymentStatus.Pending
        };

        db.Payments.Add(payment);
        await db.SaveChangesAsync(cancellationToken);

        return Ok(PaymentMappers.MapPayment(payment));
    }

    [HttpPost("{paymentId:guid}/proof")]
    public async Task<IActionResult> SubmitProof(Guid paymentId, [FromBody] SubmitPaymentProofRequestDto request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(new { message = "User not authenticated" });

        if (string.IsNullOrWhiteSpace(request.ProviderTransactionId))
            return BadRequest(new { message = "Provider transaction id is required" });

        var payment = await db.Payments
            .FirstOrDefaultAsync(p => p.Id == paymentId && p.UserId == userId, cancellationToken);

        if (payment is null)
            return NotFound(new { message = "Payment not found" });

        if (payment.Status is PaymentStatus.Completed or PaymentStatus.Refunded)
            return Conflict(new { message = "Payment can no longer be changed" });

        payment.ProviderTransactionId = request.ProviderTransactionId.Trim();
        payment.ProviderResponse = request.ProviderResponse;
        payment.Status = PaymentStatus.UnderReview;

        await db.SaveChangesAsync(cancellationToken);
        return Ok(PaymentMappers.MapPayment(payment));
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

        (page, pageSize) = pagingService.NormalizePaging(page, pageSize);

        var query = db.Payments
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => PaymentMappers.MapPayment(p))
            .ToListAsync(cancellationToken);

        return Ok(new PagedResponse<PaymentResponseDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }

    [HttpGet("admin")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<PagedResponse<PaymentResponseDto>>> GetPaymentsForAdmin(
        [FromQuery] PaymentStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = pagingService.NormalizePaging(page, pageSize);

        var query = db.Payments.AsQueryable();
        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        query = query.OrderByDescending(p => p.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => PaymentMappers.MapPayment(p))
            .ToListAsync(cancellationToken);

        return Ok(new PagedResponse<PaymentResponseDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }

    [HttpPost("admin/{paymentId:guid}/review")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ReviewPayment(Guid paymentId, [FromBody] ReviewPaymentRequestDto request, CancellationToken cancellationToken)
    {
        var payment = await db.Payments
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);

        if (payment is null)
            return NotFound(new { message = "Payment not found" });

        if (payment.Status == PaymentStatus.Completed)
            return Conflict(new { message = "Payment is already completed" });

        payment.ProviderResponse = request.Note ?? payment.ProviderResponse;

        if (!request.Approve)
        {
            payment.Status = PaymentStatus.Failed;
            await db.SaveChangesAsync(cancellationToken);
            return Ok(PaymentMappers.MapPayment(payment));
        }

        payment.Status = PaymentStatus.Completed;
        payment.CompletedAt = DateTime.UtcNow;

        payment.User.SubscriptionTier = payment.TargetTier;
        payment.User.SubscriptionExpiresAt = DateTime.UtcNow.AddDays(Math.Max(1, request.SubscriptionDays));
        await userManager.UpdateAsync(payment.User);
        await db.SaveChangesAsync(cancellationToken);

        return Ok(PaymentMappers.MapPayment(payment));
    }
}
