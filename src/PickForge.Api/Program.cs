using System.Net.Http.Headers;
using Microsoft.AspNetCore.RateLimiting;
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

// --- Middleware order matters ---
app.UseHttpsRedirection();
app.UseCors(CorsPolicy);      // CORS must run before endpoints
app.UseRateLimiter();

app.MapGet("/ping", () => Results.Ok(new { ok = true }));

// Season context endpoint
app.MapGet("/api/scoreboard/context", async (ScoreboardService scoreboards) =>
{
    var context = await scoreboards.GetSeasonContextAsync();
    return Results.Ok(context);
}).RequireRateLimiting("fixed");

// Current active picks week endpoint
app.MapGet("/api/scoreboard/current", async (ScoreboardService scoreboards) =>
{
    var result = await scoreboards.GetUpcomingGamesAsync();
    return Results.Ok(new
    {
        result.WeekNumber,
        result.SeasonYear,
        result.SeasonType,
        IsActivePicksWeek = true,
        Games = result.Games.Select(g => new
        {
            g.GameId,
            g.HomeTeam,
            g.AwayTeam,
            g.Kickoff,
            g.HomeScore,
            g.AwayScore,
            g.Status,
            g.IsFinal,
            g.IsInProgress
        })
    });
}).RequireRateLimiting("fixed");

// Specific week endpoint
app.MapGet("/api/scoreboard/week/{week:int}", async (
    int week,
    ScoreboardService scoreboards) =>
{
    var result = await scoreboards.GetWeekAsync(week);
    return Results.Ok(new
    {
        result.WeekNumber,
        result.SeasonYear,
        result.SeasonType,
        Games = result.Games.Select(g => new
        {
            g.GameId,
            g.HomeTeam,
            g.AwayTeam,
            g.Kickoff,
            g.HomeScore,
            g.AwayScore,
            g.Status,
            g.IsFinal,
            g.IsInProgress
        })
    });
}).RequireRateLimiting("fixed");

// Main endpoint: predict upcoming week using last N recent games
app.MapGet("/predict", async (
    int? recent,
    ScoreboardService scoreboards,
    StatsService stats,
    PredictionService predictor) =>
{
    int recentGames = Math.Clamp(recent.GetValueOrDefault(3), 1, 10);
    var upcoming = await scoreboards.GetUpcomingGamesAsync();

    int upcomingWeek = Math.Clamp(upcoming.WeekNumber, 1, 18);
    int lastCompletedWeek = Math.Max(1, upcomingWeek - 1);

    var seasonStats = await stats.BuildSeasonStatsAsync(lastCompletedWeek);

    var games = new List<object>();
    foreach (var g in upcoming.Games.OrderBy(x => x.Kickoff))
    {
        seasonStats.TryGetValue(g.HomeTeam, out var homeStats);
        seasonStats.TryGetValue(g.AwayTeam, out var awayStats);

        var p = predictor.Predict(g, homeStats, awayStats, recentGames);

        games.Add(new
        {
            g.GameId,
            g.Kickoff,
            g.AwayTeam,
            g.HomeTeam,
            Pick = p.PredictedWinner,
            Confidence = p.Confidence,
            Home = homeStats is null ? null : new { homeStats.Wins, homeStats.Losses, PPG = homeStats.PointsForPerGame, PAPG = homeStats.PointsAgainstPerGame },
            Away = awayStats is null ? null : new { awayStats.Wins, awayStats.Losses, PPG = awayStats.PointsForPerGame, PAPG = awayStats.PointsAgainstPerGame },
            Notes = p.Notes
        });
    }

    return Results.Ok(new
    {
        Week = upcomingWeek,
        LastCompletedWeek = lastCompletedWeek,
        RecentGames = recentGames,
        Count = games.Count,
        Games = games
    });
}).RequireRateLimiting("fixed");

app.Run();
