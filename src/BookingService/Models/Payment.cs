namespace BookingService.Models;

public class Payment
{
    public Guid Id { get; set; }

    public Guid BookingId { get; set; }

    public decimal Amount { get; set; }

    public PaymentStatus Status { get; set; }

    public string ProviderTransactionId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Booking Booking { get; set; } = null!;
}
