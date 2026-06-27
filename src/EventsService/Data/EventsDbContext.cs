using EventsService.Models;
using Microsoft.EntityFrameworkCore;

namespace EventsService.Data;

public class EventsDbContext : DbContext
{
    public EventsDbContext(DbContextOptions<EventsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Venue> Venues => Set<Venue>();

    public DbSet<Seat> Seats => Set<Seat>();

    public DbSet<Event> Events => Set<Event>();

    public DbSet<EventSeat> EventSeats => Set<EventSeat>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Venue>(entity =>
        {
            entity.HasKey(v => v.Id);
            entity.Property(v => v.Name).HasMaxLength(200);
            entity.Property(v => v.Address).HasMaxLength(300);
            entity.Property(v => v.City).HasMaxLength(100);
        });

        modelBuilder.Entity<Seat>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Zone).HasMaxLength(50);
            entity.HasOne(s => s.Venue)
                .WithMany(v => v.Seats)
                .HasForeignKey(s => s.VenueId);
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.ArtistName).HasMaxLength(200);
            entity.HasOne(e => e.Venue)
                .WithMany(v => v.Events)
                .HasForeignKey(e => e.VenueId);
        });

        modelBuilder.Entity<EventSeat>(entity =>
        {
            entity.HasKey(es => es.Id);
            entity.Property(es => es.Price).HasPrecision(18, 2);
            entity.HasOne(es => es.Event)
                .WithMany(e => e.EventSeats)
                .HasForeignKey(es => es.EventId);
            entity.HasOne(es => es.Seat)
                .WithMany()
                .HasForeignKey(es => es.SeatId);
        });
    }
}
