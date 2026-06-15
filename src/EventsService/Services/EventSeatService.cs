using EventsService.Data;
using EventsService.Dtos;
using EventsService.Exceptions;
using EventsService.Models;
using Microsoft.EntityFrameworkCore;

namespace EventsService.Services;

public class EventSeatService
{
    private readonly EventsDbContext _db;
    private readonly EventCacheService _cache;

    public EventSeatService(EventsDbContext db, EventCacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<IReadOnlyList<EventDto>> GetPublishedEventsAsync()
    {
        var cachedEvents = await _cache.GetPublishedEventsAsync();
        if (cachedEvents != null)
        {
            return cachedEvents;
        }

        var events = await _db.Events
            .AsNoTracking()
            .Where(e => e.Status == EventStatus.Published)
            .OrderBy(e => e.StartDateTime)
            .Select(e => ToEventDto(e))
            .ToListAsync();

        await _cache.SetPublishedEventsAsync(events);
        return events;
    }

    public async Task<EventDto> GetEventAsync(Guid eventId)
    {
        var concertEvent = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventId);
        if (concertEvent == null)
        {
            throw new AppException("EventNotFound", "Event was not found.", StatusCodes.Status404NotFound);
        }

        return ToEventDto(concertEvent);
    }

    public async Task<EventSeatsResponseDto> GetEventSeatsAsync(Guid eventId)
    {
        await EnsureEventExistsAsync(eventId);

        var seats = await _db.EventSeats
            .AsNoTracking()
            .Include(es => es.Seat)
            .Where(es => es.EventId == eventId)
            .OrderBy(es => es.Seat.RowNumber)
            .ThenBy(es => es.Seat.SeatNumber)
            .Select(es => new EventSeatDto(
                es.Id,
                es.Seat.RowNumber,
                es.Seat.SeatNumber,
                es.Seat.Zone,
                es.Price,
                es.Status.ToString()))
            .ToListAsync();

        return new EventSeatsResponseDto(seats);
    }

    public async Task<EventDto> CreateEventAsync(CreateEventRequest request)
    {
        if (!Enum.TryParse<EventStatus>(request.Status, true, out var status))
        {
            throw new AppException("ValidationError", "Invalid event status.", StatusCodes.Status400BadRequest);
        }

        var venueExists = await _db.Venues.AnyAsync(v => v.Id == request.VenueId);
        if (!venueExists)
        {
            throw new AppException("ValidationError", "Venue was not found.", StatusCodes.Status400BadRequest);
        }

        var concertEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            ArtistName = request.ArtistName,
            VenueId = request.VenueId,
            StartDateTime = request.StartDateTime,
            Status = status,
            CreatedAt = DateTime.UtcNow
        };

        _db.Events.Add(concertEvent);
        await _db.SaveChangesAsync();
        await _cache.InvalidatePublishedEventsAsync();
        return ToEventDto(concertEvent);
    }

    public async Task<HoldSeatsResponseDto> HoldSeatsAsync(Guid eventId, HoldSeatsRequest request)
    {
        var concertEvent = await _db.Events.FirstOrDefaultAsync(e => e.Id == eventId);
        if (concertEvent == null)
        {
            throw new AppException("EventNotFound", "Event was not found.", StatusCodes.Status404NotFound);
        }

        if (concertEvent.Status != EventStatus.Published)
        {
            throw new AppException("ValidationError", "Event is not available for booking.", StatusCodes.Status400BadRequest);
        }

        if (request.EventSeatIds.Count == 0)
        {
            throw new AppException("ValidationError", "At least one seat must be selected.", StatusCodes.Status400BadRequest);
        }

        var eventSeats = await _db.EventSeats
            .Where(es => es.EventId == eventId && request.EventSeatIds.Contains(es.Id))
            .ToListAsync();

        if (eventSeats.Count != request.EventSeatIds.Count)
        {
            throw new AppException("ValidationError", "One or more seats were not found.", StatusCodes.Status400BadRequest);
        }

        if (eventSeats.Any(es => es.Status != EventSeatStatus.Available))
        {
            throw new AppException(
                "SeatAlreadyReserved",
                "One or more selected seats are not available.",
                StatusCodes.Status409Conflict);
        }

        foreach (var eventSeat in eventSeats)
        {
            eventSeat.Status = EventSeatStatus.Held;
            eventSeat.HeldByBookingId = request.BookingId;
        }

        await _db.SaveChangesAsync();

        var heldSeats = eventSeats
            .Select(es => new HeldSeatDto(es.Id, es.Price))
            .ToList();

        return new HoldSeatsResponseDto(heldSeats);
    }

    public async Task ConfirmSaleAsync(ConfirmSaleRequest request)
    {
        var eventSeats = await _db.EventSeats
            .Where(es => request.EventSeatIds.Contains(es.Id))
            .ToListAsync();

        if (eventSeats.Count != request.EventSeatIds.Count)
        {
            throw new AppException("ValidationError", "One or more seats were not found.", StatusCodes.Status400BadRequest);
        }

        if (eventSeats.Any(es => es.Status != EventSeatStatus.Held || es.HeldByBookingId != request.BookingId))
        {
            throw new AppException(
                "SeatAlreadyReserved",
                "One or more selected seats are not held by this booking.",
                StatusCodes.Status409Conflict);
        }

        foreach (var eventSeat in eventSeats)
        {
            eventSeat.Status = EventSeatStatus.Sold;
            eventSeat.HeldByBookingId = null;
        }

        await _db.SaveChangesAsync();
    }

    public async Task ReleaseSeatsAsync(ReleaseSeatsRequest request)
    {
        var eventSeats = await _db.EventSeats
            .Where(es => request.EventSeatIds.Contains(es.Id))
            .ToListAsync();

        if (eventSeats.Count != request.EventSeatIds.Count)
        {
            throw new AppException("ValidationError", "One or more seats were not found.", StatusCodes.Status400BadRequest);
        }

        foreach (var eventSeat in eventSeats)
        {
            if (eventSeat.Status == EventSeatStatus.Held && eventSeat.HeldByBookingId == request.BookingId)
            {
                eventSeat.Status = EventSeatStatus.Available;
                eventSeat.HeldByBookingId = null;
            }
        }

        await _db.SaveChangesAsync();
    }

    private async Task EnsureEventExistsAsync(Guid eventId)
    {
        var exists = await _db.Events.AnyAsync(e => e.Id == eventId);
        if (!exists)
        {
            throw new AppException("EventNotFound", "Event was not found.", StatusCodes.Status404NotFound);
        }
    }

    private static EventDto ToEventDto(Event concertEvent)
    {
        return new EventDto(
            concertEvent.Id,
            concertEvent.Title,
            concertEvent.Description,
            concertEvent.ArtistName,
            concertEvent.VenueId,
            concertEvent.StartDateTime,
            concertEvent.Status.ToString());
    }
}
