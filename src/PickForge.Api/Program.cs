using System.Net.Http.Headers;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using PickForge.Api.Data;
using PickForge.Api.Endpoints;
using PickForge.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// --- CORS ---
const string CorsPolicy = "PickForgeCors";

// Read CORS origins from configuration (environment-specific)
var corsOrigins = builder.Configuration.GetSection("CorsOrigins").Get<string[]>()
    ?? new[] { "https://localhost:7256" }; // Fallback to localhost if not configured

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
    {
        policy
            .WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
        // If you later use cookies/auth:
        // .AllowCredentials();
    });
});

// Add DbContext
builder.Services.AddDbContext<PickForgeDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PickForgeDb")));

// Add MemoryCache
builder.Services.AddMemoryCache();

// Minimal dependencies
builder.Services.AddRateLimiter(o =>
{
    o.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.PermitLimit = 60;           // 60 req/min/IP
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
});

builder.Services.AddHttpClient<ScoreboardService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("PickForge", "1.0"));
});

builder.Services.AddSingleton<StatsService>();
builder.Services.AddSingleton<PredictionService>();

var app = builder.Build();

// Apply pending migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var db = scope.ServiceProvider.GetRequiredService<PickForgeDbContext>();
        
        var pendingMigrations = db.Database.GetPendingMigrations().ToList();
        
        if (pendingMigrations.Any())
        {
            logger.LogInformation("Applying {Count} pending database migration(s)", pendingMigrations.Count);
            db.Database.Migrate();
            logger.LogInformation("Database migrations applied successfully");
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Failed to apply database migrations");
        throw; // Re-throw to prevent app from starting with broken DB
    }
}

// --- Middleware order matters ---
app.UseHttpsRedirection();
app.UseCors(CorsPolicy);      // CORS must run before endpoints
app.UseRateLimiter();

// Simple health check endpoint
app.MapGet("/ping", () => Results.Ok(new { ok = true }));

// Map endpoint groups
app.MapScoreboardEndpoints();
app.MapPredictionEndpoints();
app.MapPicksEndpoints();

app.Run();
