using BookingService.BackgroundServices;
using BookingService.Data;
using BookingService.Middleware;
using BookingService.PaymentMock;
using BookingService.Services;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var postgresConnection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=bookings_db;Username=ticketuser;Password=ticketpass";

builder.Services.AddDbContext<BookingDbContext>(options =>
    options.UseNpgsql(postgresConnection));

var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnection;
});

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(redisConnection));

builder.Services.AddHttpClient<EventsServiceClient>(client =>
{
    var baseUrl = builder.Configuration["EventsService:BaseUrl"] ?? "http://localhost:5001";
    client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddScoped<BookingManager>();
builder.Services.AddScoped<BookingRedisService>();
builder.Services.AddSingleton<MockPaymentService>();
builder.Services.AddHostedService<BookingExpirationService>();

var app = builder.Build();

await DatabaseInitializer.InitializeAsync<BookingDbContext>(app.Services);

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
