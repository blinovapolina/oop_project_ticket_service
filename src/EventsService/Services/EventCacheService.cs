using System.Text.Json;
using EventsService.Dtos;
using Microsoft.Extensions.Caching.Distributed;

namespace EventsService.Services;

public class EventCacheService
{
    private const string PublishedEventsKey = "events:published";
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(5);

    private readonly IDistributedCache _cache;

    public EventCacheService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<IReadOnlyList<EventDto>?> GetPublishedEventsAsync()
    {
        var cached = await _cache.GetStringAsync(PublishedEventsKey);
        if (string.IsNullOrEmpty(cached))
        {
            return null;
        }

        return JsonSerializer.Deserialize<List<EventDto>>(cached);
    }

    public async Task SetPublishedEventsAsync(IReadOnlyList<EventDto> events)
    {
        var json = JsonSerializer.Serialize(events);
        await _cache.SetStringAsync(
            PublishedEventsKey,
            json,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheLifetime });
    }

    public async Task InvalidatePublishedEventsAsync()
    {
        await _cache.RemoveAsync(PublishedEventsKey);
    }
}
