namespace BookingService.Dtos;

public record CreateBookingRequest(
    Guid UserId,
    Guid EventId,
    IReadOnlyList<Guid> EventSeatIds);

public record BookingItemDto(Guid EventSeatId, decimal Price);

public record BookingDto(
    Guid Id,
    Guid UserId,
    Guid EventId,
    string Status,
    decimal TotalAmount,
    DateTime ExpiresAt,
    DateTime CreatedAt,
    DateTime? PaidAt,
    IReadOnlyList<BookingItemDto> Items,
    IReadOnlyList<TicketDto> Tickets);

public record TicketDto(
    Guid Id,
    Guid EventSeatId,
    string QrCode,
    string Status);

public record PayBookingResponseDto(
    Guid BookingId,
    string Status,
    DateTime? PaidAt,
    IReadOnlyList<TicketDto> Tickets);

public record ApiErrorDto(string Code, string Message, string TraceId);

public record EventDto(
    Guid Id,
    string Title,
    string Description,
    string ArtistName,
    Guid VenueId,
    DateTime StartDateTime,
    string Status);

public record HeldSeatDto(Guid EventSeatId, decimal Price);

public record HoldSeatsResponseDto(IReadOnlyList<HeldSeatDto> HeldSeats);

public record ApiErrorResponseDto(string Code, string Message, string TraceId);
