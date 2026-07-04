using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Features.Profile.Dtos;

namespace SyrianStudyBot.Features.Profile.UseCases;

public interface IProfileUseCase
{
    Task<ProfileResponseDto> GetMeAsync(ApplicationUser user);
    Task<ProfileResponseDto> UpdateMeAsync(ApplicationUser user, UpdateProfileRequestDto request);
}
