namespace BookingService.Models;

public enum BookingStatus
{
    Created,
    WaitingForPayment,
    Paid,
    Cancelled,
    Expired,
    PaymentFailed
}

public enum PaymentStatus
{
    Pending,
    Succeeded,
    Failed,
    Cancelled
}

public enum TicketStatus
{
    Active,
    Cancelled,
    Used
}
