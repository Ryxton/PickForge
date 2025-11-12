using System.Text.Json;
using PickForge.Api.Models;

namespace PickForge.Api.Services;

public class ScoreboardService
{
    private readonly HttpClient _http;
    private const string BaseUrl = "https://site.api.espn.com/apis/site/v2/sports/football/nfl/scoreboard";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ScoreboardService(HttpClient http) => _http = http;

    public async Task<(string RawJson, ScoreboardResponse Scoreboard)> GetScoreboardAsync(int? week = null)
    {
        var url = week is null ? BaseUrl : $"{BaseUrl}?week={week.Value}&seasontype=2";
        var json = await _http.GetStringAsync(url);
        var sb = JsonSerializer.Deserialize<ScoreboardResponse>(json, JsonOptions)
                 ?? throw new InvalidOperationException("Failed to parse scoreboard JSON.");
        return (json, sb);
    }

    public async Task<ScoreboardResult> GetUpcomingGamesAsync()
    {
        var (raw, baseSb) = await GetScoreboardAsync(null);
        var rawWeek = baseSb.Week?.Number ?? 1;
        var baseWeek = Math.Clamp(rawWeek, 1, 18);

        var nowUtc = DateTime.UtcNow;
        bool hasFuture = baseSb.Events?.Any(e => e.Date.ToUniversalTime() > nowUtc) ?? false;

        int targetWeek;
        string usedRaw;
        ScoreboardResponse usedSb;

        if (hasFuture)
        {
            targetWeek = baseWeek;
            usedRaw = raw;
            usedSb = baseSb;
        }
        else
        {
            targetWeek = Math.Clamp(baseWeek + 1, 1, 18);
            var (rawNext, sbNext) = await GetScoreboardAsync(targetWeek);
            usedRaw = rawNext;
            usedSb = sbNext;
        }

        return new ScoreboardResult(usedRaw, usedSb, MapGames(usedSb), targetWeek, true);
    }

    private static List<GameFeatures> MapGames(ScoreboardResponse sb)
    {
        var games = new List<GameFeatures>();
        if (sb.Events is null) return games;

        foreach (var ev in sb.Events)
        {
            var comp = ev.Competitions?.FirstOrDefault();
            if (comp?.Competitors is null) continue;

            var home = comp.Competitors.FirstOrDefault(c => string.Equals(c.HomeAway, "home", StringComparison.OrdinalIgnoreCase));
            var away = comp.Competitors.FirstOrDefault(c => string.Equals(c.HomeAway, "away", StringComparison.OrdinalIgnoreCase));
            if (home?.Team is null || away?.Team is null) continue;

            var homeName = home.Team.Abbreviation ?? home.Team.DisplayName ?? "HOME";
            var awayName = away.Team.Abbreviation ?? away.Team.DisplayName ?? "AWAY";
            var gameId = ev.Id ?? $"{homeName}-{awayName}-{ev.Date:O}";

            games.Add(new GameFeatures(gameId, homeName, awayName, ev.Date, null, null, null));
        }
        return games;
    }
}
