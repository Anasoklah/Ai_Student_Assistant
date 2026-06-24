
using SyrianStudyBot.interfaces.Auth;

namespace SyrianStudyBot.Data.BackgroundJobs;

public class TokenCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TokenCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(6);

    public TokenCleanupService(
        IServiceProvider serviceProvider,
        ILogger<TokenCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var refreshTokenService = scope.ServiceProvider
                    .GetRequiredService<IRefreshTokenService>();

                var deletedCount = await refreshTokenService.CleanupExpiredTokensAsync();
                
                _logger.LogDebug(
                    "Token cleanup completed. Removed {Count} tokens", 
                    deletedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token cleanup");
            }

            await Task.Delay(_cleanupInterval, stoppingToken);
        }
    }
}