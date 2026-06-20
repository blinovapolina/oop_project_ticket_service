using EventsService.Data;
using EventsService.Middleware;
using EventsService.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var postgresConnection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=events_db;Username=ticketuser;Password=ticketpass";

builder.Services.AddDbContext<EventsDbContext>(options =>
    options.UseNpgsql(postgresConnection));

var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnection;
});

builder.Services.AddScoped<EventSeatService>();
builder.Services.AddScoped<EventCacheService>();

var app = builder.Build();

await DatabaseInitializer.InitializeAsync<EventsDbContext>(
    app.Services,
    async db => await DbSeeder.SeedAsync(db));

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
