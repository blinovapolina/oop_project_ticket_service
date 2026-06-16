using StackExchange.Redis;

namespace BookingService.Services;

public class BookingRedisService
{
    private const string ExpirationKey = "bookings:expiring";

    private readonly IConnectionMultiplexer _redis;

    public BookingRedisService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task ScheduleExpirationAsync(Guid bookingId, DateTime expiresAt)
    {
        var score = new DateTimeOffset(expiresAt).ToUnixTimeSeconds();
        await _redis.GetDatabase().SortedSetAddAsync(ExpirationKey, bookingId.ToString(), score);
    }

    public async Task RemoveExpirationAsync(Guid bookingId)
    {
        await _redis.GetDatabase().SortedSetRemoveAsync(ExpirationKey, bookingId.ToString());
    }

    public async Task<IReadOnlyList<Guid>> GetExpiredBookingIdsAsync()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var entries = await _redis.GetDatabase().SortedSetRangeByScoreAsync(
            ExpirationKey,
            stop: now);

        return entries
            .Select(entry => Guid.Parse(entry!))
            .ToList();
    }
}
