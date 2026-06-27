using BookingService.Dtos;
using BookingService.Exceptions;
using BookingService.Services;
using Microsoft.AspNetCore.Mvc;

namespace BookingService.Controllers;

[ApiController]
public class BookingsController : ControllerBase
{
    private readonly BookingManager _bookingManager;

    public BookingsController(BookingManager bookingManager)
    {
        _bookingManager = bookingManager;
    }

    [HttpPost("/api/bookings")]
    public async Task<ActionResult<BookingDto>> CreateBooking([FromBody] CreateBookingRequest request)
    {
        var booking = await _bookingManager.CreateBookingAsync(request);
        return CreatedAtAction(nameof(GetBooking), new { bookingId = booking.Id }, booking);
    }

    [HttpGet("/api/bookings/{bookingId:guid}")]
    public async Task<ActionResult<BookingDto>> GetBooking(Guid bookingId)
    {
        var userId = GetRequiredUserId();
        var booking = await _bookingManager.GetBookingAsync(bookingId, userId);
        return Ok(booking);
    }

    [HttpGet("/api/users/{userId:guid}/bookings")]
    public async Task<ActionResult<IReadOnlyList<BookingDto>>> GetUserBookings(Guid userId)
    {
        var requestUserId = GetRequiredUserId();
        if (requestUserId != userId)
        {
            throw new AppException("AccessDenied", "You do not have access to these bookings.", StatusCodes.Status403Forbidden);
        }

        var bookings = await _bookingManager.GetUserBookingsAsync(userId);
        return Ok(bookings);
    }

    [HttpPost("/api/bookings/{bookingId:guid}/pay")]
    public async Task<ActionResult<PayBookingResponseDto>> PayBooking(Guid bookingId)
    {
        var userId = GetRequiredUserId();
        Request.Headers.TryGetValue("X-Mock-Payment", out var mockPaymentHeader);
        var response = await _bookingManager.PayBookingAsync(bookingId, userId, mockPaymentHeader.ToString());
        return Ok(response);
    }

    [HttpPost("/api/bookings/{bookingId:guid}/cancel")]
    public async Task<ActionResult<BookingDto>> CancelBooking(Guid bookingId)
    {
        var userId = GetRequiredUserId();
        var booking = await _bookingManager.CancelBookingAsync(bookingId, userId);
        return Ok(booking);
    }

    private Guid GetRequiredUserId()
    {
        if (!Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) ||
            !Guid.TryParse(userIdHeader.ToString(), out var userId))
        {
            throw new AppException("ValidationError", "X-User-Id header is required.", StatusCodes.Status400BadRequest);
        }

        return userId;
    }
}
