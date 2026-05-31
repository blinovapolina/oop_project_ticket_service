namespace BookingService.Models;

public class Ticket
{
    public Guid Id { get; set; }

    public Guid BookingId { get; set; }

    public Guid EventSeatId { get; set; }

    public string QrCode { get; set; } = string.Empty;

    public TicketStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public Booking Booking { get; set; } = null!;
}
