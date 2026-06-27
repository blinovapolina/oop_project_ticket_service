using EventsService.Data;
using EventsService.Middleware;
using EventsService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();


builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("X-User-Role", new OpenApiSecurityScheme
    {
        Name = "X-User-Role",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "User role: 'Admin' for admin endpoints (e.g., POST /api/events)"
    });

    c.AddSecurityDefinition("X-User-Id", new OpenApiSecurityScheme
    {
        Name = "X-User-Id",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "User ID for authentication"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "X-User-Role"
                }
            },
            new List<string>()
        }
    });
});

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