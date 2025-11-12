namespace PickForge.Api.Models;

public record GameResult(int PointsFor, int PointsAgainst);

public class TeamSeasonStats
{
    public string Team { get; }
    public int GamesPlayed { get; private set; }
    public int Wins { get; private set; }
    public int Losses { get; private set; }
    public int PointsFor { get; private set; }
    public int PointsAgainst { get; private set; }
    public List<GameResult> Results { get; } = new();

    public TeamSeasonStats(string team) => Team = team;

    public void AddGame(int pf, int pa)
    {
        GamesPlayed++;
        PointsFor += pf; PointsAgainst += pa;
        Results.Add(new GameResult(pf, pa));
        if (pf > pa) Wins++; else if (pa > pf) Losses++;
    }

    public double PointsForPerGame => GamesPlayed == 0 ? 0.0 : (double)PointsFor / GamesPlayed;
    public double PointsAgainstPerGame => GamesPlayed == 0 ? 0.0 : (double)PointsAgainst / GamesPlayed;

    public double RecentPointsForPerGame(int n = 3)
    {
        if (Results.Count == 0) return 0.0;
        var last = Results.TakeLast(Math.Min(n, Results.Count));
        return last.Average(r => r.PointsFor);
    }

    public double RecentPointsAgainstPerGame(int n = 3)
    {
        if (Results.Count == 0) return 0.0;
        var last = Results.TakeLast(Math.Min(n, Results.Count));
        return last.Average(r => r.PointsAgainst);
    }
}
