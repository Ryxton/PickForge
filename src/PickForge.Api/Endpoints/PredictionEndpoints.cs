using PickForge.Api.Services;

namespace PickForge.Api.Endpoints;

public static class PredictionEndpoints
{
    public static void MapPredictionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/predict", Predict)
            .RequireRateLimiting("fixed");
    }

    private static async Task<IResult> Predict(
        int? recent,
        ScoreboardService scoreboards,
        StatsService stats,
        PredictionService predictor)
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
    }
}