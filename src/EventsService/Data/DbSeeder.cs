using EventsService.Models;
using Microsoft.EntityFrameworkCore;

namespace EventsService.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(EventsDbContext db)
    {
        if (await db.Events.AnyAsync())
        {
            return;
        }

        var venue = new Venue
        {
            Id = SeedIds.VenueId,
            Name = "Grand Arena",
            Address = "10 Music Street",
            City = "Moscow"
        };

        var seats = new List<Seat>();
        for (var i = 0; i < SeedIds.SeatIds.Length; i++)
        {
            var row = i / 4 + 1;
            var seatNumber = i % 4 + 1;
            var zone = row <= 2 ? "VIP" : "Standard";

            seats.Add(new Seat
            {
                Id = SeedIds.SeatIds[i],
                VenueId = venue.Id,
                RowNumber = row,
                SeatNumber = seatNumber,
                Zone = zone,
                IsActive = true
            });
        }

        var concertEvent = new Event
        {
            Id = SeedIds.EventId,
            Title = "Rock Night Live",
            Description = "An evening with top rock artists.",
            ArtistName = "The Rolling Notes",
            VenueId = venue.Id,
            StartDateTime = DateTime.UtcNow.AddDays(30),
            Status = EventStatus.Published,
            CreatedAt = DateTime.UtcNow
        };

        var eventSeats = new List<EventSeat>();
        for (var i = 0; i < SeedIds.EventSeatIds.Length; i++)
        {
            var price = seats[i].Zone == "VIP" ? 2500m : 1500m;
            eventSeats.Add(new EventSeat
            {
                Id = SeedIds.EventSeatIds[i],
                EventId = concertEvent.Id,
                SeatId = seats[i].Id,
                Price = price,
                Status = EventSeatStatus.Available
            });
        }

        db.Venues.Add(venue);
        db.Seats.AddRange(seats);
        db.Events.Add(concertEvent);
        db.EventSeats.AddRange(eventSeats);
        await db.SaveChangesAsync();
    }
}
