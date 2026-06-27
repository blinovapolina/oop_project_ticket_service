using BookingService.BackgroundServices;
using BookingService.Data;
using BookingService.Middleware;
using BookingService.PaymentMock;
using BookingService.Services;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("X-User-Id", new OpenApiSecurityScheme
    {
        Name = "X-User-Id",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "User ID for authentication (e.g., 33333333-3333-3333-3333-333333333333)"
    });

    c.AddSecurityDefinition("X-Mock-Payment", new OpenApiSecurityScheme
    {
        Name = "X-Mock-Payment",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "Mock payment mode: 'success' (default) or 'fail'"
    });

    c.AddSecurityDefinition("X-User-Role", new OpenApiSecurityScheme
    {
        Name = "X-User-Role",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "User role: 'Admin' for admin endpoints"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "X-User-Id"
                }
            },
            new List<string>()
        }
    });
});


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