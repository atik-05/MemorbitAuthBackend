using Memorbit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Memorbit.Infrastructure.Authentication;

/// <summary>
/// Periodically purges stale refresh tokens and removes guest accounts that were
/// never upgraded and have had no active session for a while.
/// </summary>
public class RefreshTokenCleanupService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);
    private static readonly TimeSpan RetentionAfterExpiry = TimeSpan.FromDays(7);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RefreshTokenCleanupService> _logger;

    public RefreshTokenCleanupService(IServiceScopeFactory scopeFactory, ILogger<RefreshTokenCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        do
        {
            try
            {
                await CleanupAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Refresh token cleanup run failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task CleanupAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var now = DateTime.UtcNow;
        var cutoff = now - RetentionAfterExpiry;

        var deletedTokens = await db.RefreshTokens
            .Where(x => x.ExpiresAtUtc < cutoff || (x.RevokedAtUtc != null && x.RevokedAtUtc < cutoff))
            .ExecuteDeleteAsync(ct);

        if (deletedTokens > 0)
        {
            _logger.LogInformation("Purged {Count} expired/revoked refresh tokens.", deletedTokens);
        }

        var abandonedGuests = await db.Users
            .Where(u => u.IsGuest && u.CreatedAtUtc < cutoff)
            .Where(u => !db.RefreshTokens.Any(rt => rt.UserId == u.Id && rt.ExpiresAtUtc >= now && rt.RevokedAtUtc == null))
            .ToListAsync(ct);

        if (abandonedGuests.Count > 0)
        {
            db.Users.RemoveRange(abandonedGuests);
            await db.SaveChangesAsync(ct);
            _logger.LogInformation("Removed {Count} abandoned guest accounts.", abandonedGuests.Count);
        }
    }
}
