using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using PickForge.Api.Models;

namespace PickForge.Api.Services;

public class ScoreboardService
{
    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private const string BaseUrl = "https://site.api.espn.com/apis/site/v2/sports/football/nfl/scoreboard";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public ScoreboardService(HttpClient http, IMemoryCache cache)
    {
        _http = http;
        _cache = cache;
    }

    public async Task<(string RawJson, ScoreboardResponse Scoreboard)> GetScoreboardAsync(int? week = null)
    {
        var cacheKey = week.HasValue ? $"scoreboard:week:{week.Value}" : "scoreboard:current";
        
        if (_cache.TryGetValue(cacheKey, out (string Json, ScoreboardResponse Scoreboard)? cached) && cached.HasValue)
        {
            return cached.Value;
        }
        
        var url = week is null ? BaseUrl : $"{BaseUrl}?week={week.Value}&seasontype=2";
        var json = await _http.GetStringAsync(url);
        var sb = JsonSerializer.Deserialize<ScoreboardResponse>(json, JsonOptions)
                 ?? throw new InvalidOperationException("Failed to parse scoreboard JSON.");
        
        var result = (json, sb);
        _cache.Set(cacheKey, result, CacheDuration);
        
        return result;
    }

    public async Task<SeasonContext> GetSeasonContextAsync()
    {
        var (_, sb) = await GetScoreboardAsync(null);
        var currentWeek = Math.Clamp(sb.Week?.Number ?? 1, 1, 18);
        var nowUtc = DateTime.UtcNow;
        
        // Check if there are any games that haven't finished yet (future kickoff OR in progress)
        bool hasActiveGames = sb.Events?.Any(e =>
            e.Date.ToUniversalTime() > nowUtc ||
            !(e.Competitions?.FirstOrDefault()?.Status?.Type?.Completed ?? true)
        ) ?? false;
        
        int activePicksWeek = hasActiveGames ? currentWeek : Math.Clamp(currentWeek + 1, 1, 18);
        
        return new SeasonContext(
            CurrentYear: DateTime.Now.Year,
            CurrentWeek: currentWeek,
            SeasonType: 2,
            ActivePicksWeek: activePicksWeek,
            MaxWeek: 18
        );
    }

    public async Task<ScoreboardResult> GetWeekAsync(int week, int? year = null, int seasonType = 2)
    {
        week = Math.Clamp(week, 1, 18);
        var (raw, sb) = await GetScoreboardAsync(week);
        var games = MapGamesWithScores(sb);
        
        return new ScoreboardResult(
            raw,
            sb,
            games,
            week,
            false,
            year ?? DateTime.Now.Year,
            seasonType
        );
    }

    public async Task<ScoreboardResult> GetUpcomingGamesAsync()
    {
        var context = await GetSeasonContextAsync();
        var result = await GetWeekAsync(context.ActivePicksWeek);
        return result with { IsUpcomingWeek = true };
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

            games.Add(new GameFeatures(gameId, homeName, awayName, ev.Date, null, null, null, null, null, "pre", false, false));
        }
        return games;
    }

    private static List<GameFeatures> MapGamesWithScores(ScoreboardResponse sb)
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

            // Parse scores
            int? homeScore = int.TryParse(home.Score, out int hs) ? hs : null;
            int? awayScore = int.TryParse(away.Score, out int ascore) ? ascore : null;

            // Parse status
            var status = comp.Status?.Type?.State ?? "pre";
            var isFinal = comp.Status?.Type?.Completed ?? false;
            var isInProgress = status == "in";

            games.Add(new GameFeatures(
                gameId, homeName, awayName, ev.Date,
                null, null, null,
                homeScore, awayScore, status, isFinal, isInProgress
            ));
        }
        return games;
    }
}
