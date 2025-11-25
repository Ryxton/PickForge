using PickForge.Api.Services;

namespace PickForge.Api.Endpoints;

public static class ScoreboardEndpoints
{
    public static void MapScoreboardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/scoreboard/context", GetContext)
            .RequireRateLimiting("fixed");

        app.MapGet("/api/scoreboard/current", GetCurrent)
            .RequireRateLimiting("fixed");

        app.MapGet("/api/scoreboard/week/{week:int}", GetWeek)
            .RequireRateLimiting("fixed");
    }

    private static async Task<IResult> GetContext(ScoreboardService scoreboards)
    {
        var context = await scoreboards.GetSeasonContextAsync();
        return Results.Ok(context);
    }

    private static async Task<IResult> GetCurrent(ScoreboardService scoreboards)
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
    }

    private static async Task<IResult> GetWeek(
        int week,
        ScoreboardService scoreboards)
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
    }
}