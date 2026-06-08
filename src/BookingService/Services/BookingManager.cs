using BookingService.Data;
using BookingService.Dtos;
using BookingService.Exceptions;
using BookingService.Models;
using BookingService.PaymentMock;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Services;

public class BookingManager
{
    private static readonly TimeSpan BookingLifetime = TimeSpan.FromMinutes(10);

    private readonly BookingDbContext _db;
    private readonly EventsServiceClient _eventsServiceClient;
    private readonly MockPaymentService _mockPaymentService;
    private readonly BookingRedisService _bookingRedisService;

    public BookingManager(
        BookingDbContext db,
        EventsServiceClient eventsServiceClient,
        MockPaymentService mockPaymentService,
        BookingRedisService bookingRedisService)
    {
        _db = db;
        _eventsServiceClient = eventsServiceClient;
        _mockPaymentService = mockPaymentService;
        _bookingRedisService = bookingRedisService;
    }

    public async Task<BookingDto> CreateBookingAsync(CreateBookingRequest request)
    {
        if (request.EventSeatIds.Count == 0)
        {
            throw new AppException("ValidationError", "At least one seat must be selected.", StatusCodes.Status400BadRequest);
        }

        var concertEvent = await _eventsServiceClient.GetEventAsync(request.EventId);
        if (!string.Equals(concertEvent.Status, "Published", StringComparison.OrdinalIgnoreCase))
        {
            throw new AppException("ValidationError", "Event is not available for booking.", StatusCodes.Status400BadRequest);
        }

        var bookingId = Guid.NewGuid();
        var holdResponse = await _eventsServiceClient.HoldSeatsAsync(
            request.EventId,
            bookingId,
            request.EventSeatIds);

        var totalAmount = holdResponse.HeldSeats.Sum(s => s.Price);
        var booking = new Booking
        {
            Id = bookingId,
            UserId = request.UserId,
            EventId = request.EventId,
            Status = BookingStatus.WaitingForPayment,
            TotalAmount = totalAmount,
            ExpiresAt = DateTime.UtcNow.Add(BookingLifetime),
            CreatedAt = DateTime.UtcNow,
            Items = holdResponse.HeldSeats.Select(seat => new BookingItem
            {
                Id = Guid.NewGuid(),
                EventSeatId = seat.EventSeatId,
                Price = seat.Price
            }).ToList()
        };

        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();
        await _bookingRedisService.ScheduleExpirationAsync(booking.Id, booking.ExpiresAt);

        return ToBookingDto(booking);
    }

    public async Task<BookingDto> GetBookingAsync(Guid bookingId, Guid userId)
    {
        var booking = await LoadBookingAsync(bookingId);
        EnsureOwner(booking, userId);
        return ToBookingDto(booking);
    }

    public async Task<IReadOnlyList<BookingDto>> GetUserBookingsAsync(Guid userId)
    {
        var bookings = await _db.Bookings
            .AsNoTracking()
            .Include(b => b.Items)
            .Include(b => b.Tickets)
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        return bookings.Select(ToBookingDto).ToList();
    }

    public async Task<PayBookingResponseDto> PayBookingAsync(Guid bookingId, Guid userId, string? mockPaymentHeader)
    {
        var booking = await LoadBookingAsync(bookingId);
        EnsureOwner(booking, userId);
        EnsureNotExpired(booking);

        if (booking.Status == BookingStatus.Paid)
        {
            throw new AppException("ValidationError", "Booking is already paid.", StatusCodes.Status409Conflict);
        }

        if (booking.Status != BookingStatus.WaitingForPayment)
        {
            throw new AppException("ValidationError", "Booking cannot be paid in its current status.", StatusCodes.Status409Conflict);
        }

        var paymentResult = _mockPaymentService.ProcessPayment(booking.TotalAmount, mockPaymentHeader);
        var now = DateTime.UtcNow;

        if (!paymentResult.IsSuccess)
        {
            _db.Payments.Add(new Payment
            {
                Id = Guid.NewGuid(),
                BookingId = booking.Id,
                Amount = booking.TotalAmount,
                Status = PaymentStatus.Failed,
                ProviderTransactionId = string.Empty,
                CreatedAt = now,
                UpdatedAt = now
            });

            booking.Status = BookingStatus.PaymentFailed;
            await _db.SaveChangesAsync();
            await _bookingRedisService.RemoveExpirationAsync(booking.Id);

            throw new AppException(
                "PaymentProviderError",
                "Payment was declined by the provider.",
                StatusCodes.Status502BadGateway);
        }

        var eventSeatIds = booking.Items.Select(i => i.EventSeatId).ToList();
        await _eventsServiceClient.ConfirmSaleAsync(booking.Id, eventSeatIds);

        booking.Status = BookingStatus.Paid;
        booking.PaidAt = now;

        _db.Payments.Add(new Payment
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            Amount = booking.TotalAmount,
            Status = PaymentStatus.Succeeded,
            ProviderTransactionId = paymentResult.ProviderTransactionId ?? string.Empty,
            CreatedAt = now,
            UpdatedAt = now
        });

        var tickets = booking.Items.Select(item => new Ticket
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            EventSeatId = item.EventSeatId,
            QrCode = $"QR-{Guid.NewGuid():N}",
            Status = TicketStatus.Active,
            CreatedAt = now
        }).ToList();

        _db.Tickets.AddRange(tickets);
        await _db.SaveChangesAsync();
        await _bookingRedisService.RemoveExpirationAsync(booking.Id);

        return new PayBookingResponseDto(
            booking.Id,
            booking.Status.ToString(),
            booking.PaidAt,
            tickets.Select(ToTicketDto).ToList());
    }

    public async Task<BookingDto> CancelBookingAsync(Guid bookingId, Guid userId)
    {
        var booking = await LoadBookingAsync(bookingId);
        EnsureOwner(booking, userId);

        if (booking.Status == BookingStatus.Paid)
        {
            throw new AppException("ValidationError", "Paid booking cannot be cancelled.", StatusCodes.Status400BadRequest);
        }

        if (booking.Status != BookingStatus.WaitingForPayment)
        {
            throw new AppException("ValidationError", "Booking cannot be cancelled in its current status.", StatusCodes.Status409Conflict);
        }

        var eventSeatIds = booking.Items.Select(i => i.EventSeatId).ToList();
        await _eventsServiceClient.ReleaseSeatsAsync(booking.Id, eventSeatIds);

        booking.Status = BookingStatus.Cancelled;
        await _db.SaveChangesAsync();
        await _bookingRedisService.RemoveExpirationAsync(booking.Id);

        return ToBookingDto(booking);
    }

    public async Task ExpireBookingAsync(Booking booking)
    {
        if (booking.Status != BookingStatus.WaitingForPayment)
        {
            return;
        }

        var eventSeatIds = booking.Items.Select(i => i.EventSeatId).ToList();
        await _eventsServiceClient.ReleaseSeatsAsync(booking.Id, eventSeatIds);

        booking.Status = BookingStatus.Expired;
        await _db.SaveChangesAsync();
        await _bookingRedisService.RemoveExpirationAsync(booking.Id);
    }

    private async Task<Booking> LoadBookingAsync(Guid bookingId)
    {
        var booking = await _db.Bookings
            .Include(b => b.Items)
            .Include(b => b.Tickets)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null)
        {
            throw new AppException("ValidationError", "Booking was not found.", StatusCodes.Status404NotFound);
        }

        return booking;
    }

    private static void EnsureOwner(Booking booking, Guid userId)
    {
        if (booking.UserId != userId)
        {
            throw new AppException("AccessDenied", "You do not have access to this booking.", StatusCodes.Status403Forbidden);
        }
    }

    private static void EnsureNotExpired(Booking booking)
    {
        if (booking.Status == BookingStatus.WaitingForPayment && booking.ExpiresAt <= DateTime.UtcNow)
        {
            throw new AppException("BookingExpired", "Booking has expired.", StatusCodes.Status409Conflict);
        }
    }

    private static BookingDto ToBookingDto(Booking booking)
    {
        return new BookingDto(
            booking.Id,
            booking.UserId,
            booking.EventId,
            booking.Status.ToString(),
            booking.TotalAmount,
            booking.ExpiresAt,
            booking.CreatedAt,
            booking.PaidAt,
            booking.Items.Select(i => new BookingItemDto(i.EventSeatId, i.Price)).ToList(),
            booking.Tickets.Select(ToTicketDto).ToList());
    }

    private static TicketDto ToTicketDto(Ticket ticket)
    {
        return new TicketDto(ticket.Id, ticket.EventSeatId, ticket.QrCode, ticket.Status.ToString());
    }
}
