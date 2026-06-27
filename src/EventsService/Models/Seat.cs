namespace EventsService.Models;

public class Seat
{
    public Guid Id { get; set; }

    public Guid VenueId { get; set; }

    public int RowNumber { get; set; }

    public int SeatNumber { get; set; }

    public string Zone { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public Venue Venue { get; set; } = null!;
}
