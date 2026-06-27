using System.Net;
using System.Text;
using System.Text.Json;
using BookingService.Dtos;
using BookingService.Exceptions;

namespace BookingService.Services;

public class EventsServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EventsServiceClient> _logger;

    public EventsServiceClient(HttpClient httpClient, ILogger<EventsServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<EventDto> GetEventAsync(Guid eventId)
    {
        return await SendAsync<EventDto>(HttpMethod.Get, $"/api/events/{eventId}");
    }

    public async Task<HoldSeatsResponseDto> HoldSeatsAsync(Guid eventId, Guid bookingId, IReadOnlyList<Guid> eventSeatIds)
    {
        var payload = new
        {
            eventSeatIds,
            bookingId
        };

        return await SendAsync<HoldSeatsResponseDto>(
            HttpMethod.Post,
            $"/api/events/{eventId}/seats/hold",
            payload);
    }

    public async Task ConfirmSaleAsync(Guid bookingId, IReadOnlyList<Guid> eventSeatIds)
    {
        var payload = new
        {
            bookingId,
            eventSeatIds
        };

        await SendAsync<object>(HttpMethod.Post, "/api/events/seats/confirm-sale", payload);
    }

    public async Task ReleaseSeatsAsync(Guid bookingId, IReadOnlyList<Guid> eventSeatIds)
    {
        var payload = new
        {
            bookingId,
            eventSeatIds
        };

        await SendAsync<object>(HttpMethod.Post, "/api/events/seats/release", payload);
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string path, object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);

        if (body != null)
        {
            var json = JsonSerializer.Serialize(body);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EventsService is unavailable");
            throw new AppException(
                "EventsServiceUnavailable",
                "Events service is unavailable.",
                StatusCodes.Status502BadGateway);
        }

        if (response.IsSuccessStatusCode)
        {
            if (typeof(T) == typeof(object) || response.StatusCode == HttpStatusCode.NoContent)
            {
                return default!;
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<T>(content, JsonOptions());
            return result!;
        }

        var errorContent = await response.Content.ReadAsStringAsync();
        var error = JsonSerializer.Deserialize<ApiErrorResponseDto>(errorContent, JsonOptions());

        throw new AppException(
            error?.Code ?? "EventsServiceError",
            error?.Message ?? "Events service returned an error.",
            (int)response.StatusCode);
    }

    private static JsonSerializerOptions JsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }
}
