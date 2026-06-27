using Microsoft.EntityFrameworkCore;

namespace BookingService.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync<TContext>(IServiceProvider services)
        where TContext : DbContext
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        for (var attempt = 1; attempt <= 15; attempt++)
        {
            try
            {
                await db.Database.EnsureCreatedAsync();
                return;
            }
            catch (Exception ex) when (attempt < 15)
            {
                logger.LogWarning(ex, "Database not ready, retry {Attempt}/15", attempt);
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }
    }
}
