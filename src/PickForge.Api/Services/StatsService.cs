using PickForge.Api.Models;

namespace PickForge.Api.Services;

public class StatsService
{
    private readonly ScoreboardService _scoreboards;
    public StatsService(ScoreboardService scoreboards) => _scoreboards = scoreboards;

    public async Task<Dictionary<string, TeamSeasonStats>> BuildSeasonStatsAsync(int lastCompletedWeek)
    {
        var stats = new Dictionary<string, TeamSeasonStats>(StringComparer.OrdinalIgnoreCase);
        lastCompletedWeek = Math.Clamp(lastCompletedWeek, 1, 18);
        if (lastCompletedWeek < 1) return stats;

        for (int week = 1; week <= lastCompletedWeek; week++)
        {
            var (_, sb) = await _scoreboards.GetScoreboardAsync(week);
            if (sb.Events is null) continue;

            foreach (var ev in sb.Events)
            {
                var comp = ev.Competitions?.FirstOrDefault();
                if (comp?.Competitors is null) continue;

                var home = comp.Competitors.FirstOrDefault(c => string.Equals(c.HomeAway, "home", StringComparison.OrdinalIgnoreCase));
                var away = comp.Competitors.FirstOrDefault(c => string.Equals(c.HomeAway, "away", StringComparison.OrdinalIgnoreCase));
                if (home?.Team is null || away?.Team is null) continue;

                if (!int.TryParse(home.Score, out int hs)) hs = 0;
                if (!int.TryParse(away.Score, out int ascore)) ascore = 0;

                var homeName = home.Team.Abbreviation ?? home.Team.DisplayName ?? "HOME";
                var awayName = away.Team.Abbreviation ?? away.Team.DisplayName ?? "AWAY";

                var h = stats.TryGetValue(homeName, out var hsAgg) ? hsAgg : stats[homeName] = new TeamSeasonStats(homeName);
                var a = stats.TryGetValue(awayName, out var asAgg) ? asAgg : stats[awayName] = new TeamSeasonStats(awayName);

                h.AddGame(hs, ascore);
                a.AddGame(ascore, hs);
            }
        }
        return stats;
    }
}
