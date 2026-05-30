namespace EventsService.Models;

public enum EventStatus
{
    Draft,
    Published,
    Cancelled,
    Finished
}

public enum EventSeatStatus
{
    Available,
    Held,
    Sold,
    Disabled
}
