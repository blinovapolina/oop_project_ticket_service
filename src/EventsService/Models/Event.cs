namespace EventsService.Models;

public class Event
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string ArtistName { get; set; } = string.Empty;

    public Guid VenueId { get; set; }

    public DateTime StartDateTime { get; set; }

    public EventStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public Venue Venue { get; set; } = null!;

    public ICollection<EventSeat> EventSeats { get; set; } = new List<EventSeat>();
}
