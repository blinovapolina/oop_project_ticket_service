namespace EventsService.Models;

public class Venue
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public ICollection<Seat> Seats { get; set; } = new List<Seat>();

    public ICollection<Event> Events { get; set; } = new List<Event>();
}
