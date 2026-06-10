using EventsService.Dtos;
using EventsService.Exceptions;
using EventsService.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventsService.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly EventSeatService _eventSeatService;

    public EventsController(EventSeatService eventSeatService)
    {
        _eventSeatService = eventSeatService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EventDto>>> GetEvents()
    {
        var events = await _eventSeatService.GetPublishedEventsAsync();
        return Ok(events);
    }

    [HttpGet("{eventId:guid}")]
    public async Task<ActionResult<EventDto>> GetEvent(Guid eventId)
    {
        var concertEvent = await _eventSeatService.GetEventAsync(eventId);
        return Ok(concertEvent);
    }

    [HttpPost]
    public async Task<ActionResult<EventDto>> CreateEvent([FromBody] CreateEventRequest request)
    {
        EnsureAdmin();
        var createdEvent = await _eventSeatService.CreateEventAsync(request);
        return CreatedAtAction(nameof(GetEvent), new { eventId = createdEvent.Id }, createdEvent);
    }

    [HttpGet("{eventId:guid}/seats")]
    public async Task<ActionResult<EventSeatsResponseDto>> GetEventSeats(Guid eventId)
    {
        var seats = await _eventSeatService.GetEventSeatsAsync(eventId);
        return Ok(seats);
    }

    [HttpPost("{eventId:guid}/seats/hold")]
    public async Task<ActionResult<HoldSeatsResponseDto>> HoldSeats(Guid eventId, [FromBody] HoldSeatsRequest request)
    {
        var response = await _eventSeatService.HoldSeatsAsync(eventId, request);
        return Ok(response);
    }

    [HttpPost("seats/confirm-sale")]
    public async Task<IActionResult> ConfirmSale([FromBody] ConfirmSaleRequest request)
    {
        await _eventSeatService.ConfirmSaleAsync(request);
        return Ok();
    }

    [HttpPost("seats/release")]
    public async Task<IActionResult> ReleaseSeats([FromBody] ReleaseSeatsRequest request)
    {
        await _eventSeatService.ReleaseSeatsAsync(request);
        return Ok();
    }

    private void EnsureAdmin()
    {
        if (!Request.Headers.TryGetValue("X-User-Role", out var role) ||
            !string.Equals(role.ToString(), "Admin", StringComparison.OrdinalIgnoreCase))
        {
            throw new AppException("AccessDenied", "Admin role is required.", StatusCodes.Status403Forbidden);
        }
    }
}
