namespace EventsService.Data;

public static class SeedIds
{
    public static readonly Guid VenueId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid EventId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid CustomerUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid AdminUserId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    public static readonly Guid[] SeatIds = Enumerable.Range(1, 12)
        .Select(i => Guid.Parse($"aaaaaaaa-aaaa-aaaa-aaaa-{i:D12}"))
        .ToArray();

    public static readonly Guid[] EventSeatIds = Enumerable.Range(1, 12)
        .Select(i => Guid.Parse($"bbbbbbbb-bbbb-bbbb-bbbb-{i:D12}"))
        .ToArray();
}
