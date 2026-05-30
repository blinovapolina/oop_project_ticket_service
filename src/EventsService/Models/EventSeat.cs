namespace EventsService.Models;

public class EventSeat
{
    public Guid Id { get; set; }

    public Guid EventId { get; set; }

    public Guid SeatId { get; set; }

    public decimal Price { get; set; }

    public EventSeatStatus Status { get; set; }

    public Guid? HeldByBookingId { get; set; }

    public Event Event { get; set; } = null!;

    public Seat Seat { get; set; } = null!;
}
