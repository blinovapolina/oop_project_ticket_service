using BookingService.Data;
using BookingService.Models;
using BookingService.Services;
using Microsoft.EntityFrameworkCore;

namespace BookingService.BackgroundServices;

public class BookingExpirationService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(10);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingExpirationService> _logger;

    public BookingExpirationService(
        IServiceScopeFactory scopeFactory,
        ILogger<BookingExpirationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExpireBookingsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to expire bookings");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task ExpireBookingsAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var bookingManager = scope.ServiceProvider.GetRequiredService<BookingManager>();
        var bookingRedisService = scope.ServiceProvider.GetRequiredService<BookingRedisService>();

        var expiredBookingIds = await bookingRedisService.GetExpiredBookingIdsAsync();
        foreach (var bookingId in expiredBookingIds)
        {
            var booking = await db.Bookings
                .Include(b => b.Items)
                .FirstOrDefaultAsync(b => b.Id == bookingId, stoppingToken);

            if (booking == null || booking.Status != BookingStatus.WaitingForPayment)
            {
                await bookingRedisService.RemoveExpirationAsync(bookingId);
                continue;
            }

            _logger.LogInformation("Expiring booking {BookingId} via Redis", bookingId);
            await bookingManager.ExpireBookingAsync(booking);
        }

        var expiredBookings = await db.Bookings
            .Include(b => b.Items)
            .Where(b => b.Status == BookingStatus.WaitingForPayment && b.ExpiresAt < DateTime.UtcNow)
            .ToListAsync(stoppingToken);

        foreach (var booking in expiredBookings)
        {
            _logger.LogInformation("Expiring booking {BookingId} via database fallback", booking.Id);
            await bookingManager.ExpireBookingAsync(booking);
        }
    }
}
