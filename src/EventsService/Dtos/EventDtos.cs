namespace EventsService.Dtos;

public record EventDto(
    Guid Id,
    string Title,
    string Description,
    string ArtistName,
    Guid VenueId,
    DateTime StartDateTime,
    string Status);

public record EventSeatDto(
    Guid Id,
    int RowNumber,
    int SeatNumber,
    string Zone,
    decimal Price,
    string Status);

public record EventSeatsResponseDto(IReadOnlyList<EventSeatDto> Seats);

public record CreateEventRequest(
    string Title,
    string Description,
    string ArtistName,
    Guid VenueId,
    DateTime StartDateTime,
    string Status);

public record HoldSeatsRequest(
    IReadOnlyList<Guid> EventSeatIds,
    Guid BookingId);

public record HeldSeatDto(Guid EventSeatId, decimal Price);

public record HoldSeatsResponseDto(IReadOnlyList<HeldSeatDto> HeldSeats);

public record ConfirmSaleRequest(
    Guid BookingId,
    IReadOnlyList<Guid> EventSeatIds);

public record ReleaseSeatsRequest(
    Guid BookingId,
    IReadOnlyList<Guid> EventSeatIds);

public record ApiErrorDto(string Code, string Message, string TraceId);
