using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PickForge.Api.Data;
using PickForge.Api.Models;
using PickForge.Api.Services;

namespace PickForge.Api.Endpoints;

public static class PicksEndpoints
{
    public static void MapPicksEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/picks", SavePredictions)
            .RequireRateLimiting("fixed");

        app.MapGet("/api/picks/week/{week:int}", GetWeekPredictions)
            .RequireRateLimiting("fixed");

        app.MapGet("/api/picks/history", GetHistory)
            .RequireRateLimiting("fixed");
    }

    private static async Task<IResult> SavePredictions(
        PickForgeDbContext db,
        [FromBody] List<PredictionDto> predictions)
    {
        if (!predictions.Any())
            return Results.BadRequest(new { error = "No predictions provided" });

        var week = predictions.First().Week;
        var year = predictions.First().SeasonYear;

        // Get existing predictions for this week
        var existingPredictions = await db.Predictions
            .Where(p => p.Week == week && p.SeasonYear == year)
            .ToListAsync();

        int updated = 0;
        int inserted = 0;

        foreach (var dto in predictions)
        {
            var existing = existingPredictions.FirstOrDefault(p => p.GameId == dto.GameId);

            if (existing != null)
            {
                // Update existing prediction
                existing.PredictedWinner = dto.PredictedWinner;
                existing.Confidence = dto.Confidence;
                existing.Notes = dto.Notes;
                existing.WasCorrect = null; // Reset correctness when prediction changes
                updated++;
            }
            else
            {
                // Insert new prediction
                db.Predictions.Add(new Prediction
                {
                    Week = dto.Week,
                    SeasonYear = dto.SeasonYear,
                    GameId = dto.GameId,
                    HomeTeam = dto.HomeTeam,
                    AwayTeam = dto.AwayTeam,
                    PredictedWinner = dto.PredictedWinner,
                    Confidence = dto.Confidence,
                    CreatedUtc = DateTime.UtcNow,
                    Notes = dto.Notes
                });
                inserted++;
            }
        }

        await db.SaveChangesAsync();

        return Results.Ok(new { updated, inserted, total = updated + inserted });
    }

    private static async Task<IResult> GetWeekPredictions(
        int week,
        PickForgeDbContext db,
        ScoreboardService scoreboards,
        int? year)
    {
        // Default to current year if not specified
        int seasonYear = year ?? DateTime.Now.Year;

        // Fetch predictions from database
        var predictions = await db.Predictions
            .Where(p => p.Week == week && p.SeasonYear == seasonYear)
            .ToListAsync();

        if (!predictions.Any())
        {
            return Results.Ok(new
            {
                Week = week,
                SeasonYear = seasonYear,
                Predictions = Array.Empty<object>(),
                CorrectCount = 0,
                TotalFinished = 0
            });
        }

        // Fetch scoreboard data for the week to get actual results
        var weekData = await scoreboards.GetWeekAsync(week);

        // Track if we need to save updates
        bool hasUpdates = false;
        int correctCount = 0;
        int totalFinished = 0;

        // Process each prediction
        foreach (var prediction in predictions)
        {
            // Find the corresponding game
            var game = weekData.Games.FirstOrDefault(g => g.GameId == prediction.GameId);

            if (game != null && game.IsFinal && game.HomeScore.HasValue && game.AwayScore.HasValue)
            {
                totalFinished++;

                // Calculate actual winner
                var actualWinner = game.HomeScore.Value > game.AwayScore.Value
                    ? game.HomeTeam
                    : game.AwayTeam;

                // If WasCorrect is not yet calculated, calculate and update DB
                if (!prediction.WasCorrect.HasValue)
                {
                    prediction.WasCorrect = prediction.PredictedWinner == actualWinner;
                    hasUpdates = true;
                }

                // Count correct predictions
                if (prediction.WasCorrect == true)
                {
                    correctCount++;
                }
            }
        }

        // Save updates if any predictions were calculated
        if (hasUpdates)
        {
            await db.SaveChangesAsync();
        }

        // Return predictions with game data
        var result = predictions.Select(p =>
        {
            var game = weekData.Games.FirstOrDefault(g => g.GameId == p.GameId);
            return new
            {
                p.GameId,
                p.HomeTeam,
                p.AwayTeam,
                PredictedWinner = p.PredictedWinner,
                p.Confidence,
                p.WasCorrect,
                p.Notes,
                p.CreatedUtc,
                // Include game status for UI
                Game = game == null ? null : new
                {
                    game.Kickoff,
                    game.HomeScore,
                    game.AwayScore,
                    game.IsFinal,
                    game.IsInProgress,
                    game.Status
                }
            };
        }).ToList();

        return Results.Ok(new
        {
            Week = week,
            SeasonYear = seasonYear,
            Predictions = result,
            CorrectCount = correctCount,
            TotalFinished = totalFinished
        });
    }

    private static async Task<IResult> GetHistory(
        PickForgeDbContext db,
        int? week,
        int? year)
    {
        var query = db.Predictions.AsQueryable();

        if (week.HasValue)
            query = query.Where(p => p.Week == week.Value);
        if (year.HasValue)
            query = query.Where(p => p.SeasonYear == year.Value);

        var predictions = await query
            .OrderByDescending(p => p.CreatedUtc)
            .ToListAsync();

        return Results.Ok(predictions);
    }
}