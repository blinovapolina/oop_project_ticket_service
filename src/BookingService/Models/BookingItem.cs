namespace BookingService.Models;

public class BookingItem
{
    public Guid Id { get; set; }

    public Guid BookingId { get; set; }

    public Guid EventSeatId { get; set; }

    public decimal Price { get; set; }

    public Booking Booking { get; set; } = null!;
}
