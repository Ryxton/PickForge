using System.Net.Http.Json;
using PickForge.Client.Models;

namespace PickForge.Client.Services;

public class ScoreboardService
{
    private readonly HttpClient _http;

    public ScoreboardService(HttpClient http) => _http = http;

    public async Task<SeasonContextResponse?> GetSeasonContextAsync()
    {
        return await _http.GetFromJsonAsync<SeasonContextResponse>("/api/scoreboard/context");
    }

    public async Task<ScoreboardWeekResponse?> GetCurrentWeekAsync()
    {
        return await _http.GetFromJsonAsync<ScoreboardWeekResponse>("/api/scoreboard/current");
    }

    public async Task<ScoreboardWeekResponse?> GetWeekAsync(int week)
    {
        return await _http.GetFromJsonAsync<ScoreboardWeekResponse>($"/api/scoreboard/week/{week}");
    }
}