using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Enums;
using SyrianStudyBot.Dtos;

namespace SyrianStudyBot.Common.Services;

public interface IDocumentRequestService
{
    DocumentIngestionRequestDto CreateAdminRequest(DocumentIngestionRequestDto request);
    DocumentIngestionRequestDto CreateStudentRequest(DocumentIngestionRequestDto request, Guid userId);
}
