namespace BookingService.Models;

public class Booking
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid EventId { get; set; }

    public BookingStatus Status { get; set; }

    public decimal TotalAmount { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? PaidAt { get; set; }

    public ICollection<BookingItem> Items { get; set; } = new List<BookingItem>();

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
