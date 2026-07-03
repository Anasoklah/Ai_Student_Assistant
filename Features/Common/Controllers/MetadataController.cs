using Microsoft.AspNetCore.Mvc;
using SyrianStudyBot.Domain.Enums;

namespace SyrianStudyBot.Features.Common.Controllers;

[ApiController]
[Route("api/metadata")]
public class MetadataController : ControllerBase
{
    [HttpGet("enums")]
    public IActionResult GetEnums()
    {
        return Ok(new
        {
            subscriptionTiers = Enum.GetNames<SubscriptionTier>(),
            subjects = Enum.GetNames<Subject>(),
            gradeLevels = Enum.GetNames<GradeLevel>(),
            paymentMethods = Enum.GetNames<PaymentMethod>(),
            paymentStatuses = Enum.GetNames<PaymentStatus>(),
            documentTypes = Enum.GetNames<DocumentType>(),
            chatModes = Enum.GetNames<ChatMode>(),
            chatMessageRoles = Enum.GetNames<ChatMessageRole>()
        });
    }
}
