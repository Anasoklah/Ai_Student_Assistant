using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Application.Profile.Dtos;

namespace SyrianStudyBot.Application.Profile;

public interface IProfileUseCase
{
    Task<ProfileResponseDto> GetMeAsync(ApplicationUser user);
    Task<ProfileResponseDto> UpdateMeAsync(ApplicationUser user, UpdateProfileRequestDto request);
}
