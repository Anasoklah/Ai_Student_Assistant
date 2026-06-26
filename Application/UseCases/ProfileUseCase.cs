using Microsoft.AspNetCore.Identity;
using SyrianStudyBot.Common.Mappers;
using SyrianStudyBot.Common.Services;
using SyrianStudyBot.Domain;
using SyrianStudyBot.Dtos;

namespace SyrianStudyBot.Application.UseCases;

public interface IProfileUseCase
{
    Task<ProfileResponseDto> GetMeAsync(ApplicationUser user);
    Task<ProfileResponseDto> UpdateMeAsync(ApplicationUser user, UpdateProfileRequestDto request);
}

public class ProfileUseCase : IProfileUseCase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUsageTrackingService _usageTrackingService;

    public ProfileUseCase(UserManager<ApplicationUser> userManager, IUsageTrackingService usageTrackingService)
    {
        _userManager = userManager;
        _usageTrackingService = usageTrackingService;
    }

    public async Task<ProfileResponseDto> GetMeAsync(ApplicationUser user)
    {
        _usageTrackingService.ResetMessageCounterIfNeeded(user);
        await _userManager.UpdateAsync(user);
        return ProfileMappers.MapProfile(user);
    }

    public async Task<ProfileResponseDto> UpdateMeAsync(ApplicationUser user, UpdateProfileRequestDto request)
    {
        if (request.FullName is not null)
            user.FullName = request.FullName.Trim();
        if (request.PhoneNumber is not null)
            user.PhoneNumber = request.PhoneNumber.Trim();
        if (request.GradeLevel.HasValue)
            user.GradeLevel = request.GradeLevel;
        if (request.PreferredLanguage is not null)
            user.PreferredLanguage = request.PreferredLanguage.Trim();

        await _userManager.UpdateAsync(user);
        return ProfileMappers.MapProfile(user);
    }
}
