namespace PickForge.Client.Models;

public class PredictResponse
{
    public int Week { get; set; }
    public int LastCompletedWeek { get; set; }
    public int RecentGames { get; set; }
    public int Count { get; set; }
    public List<PredictGame> Games { get; set; } = new();
}

public class PredictGame
{
    public string GameId { get; set; } = "";
    public DateTime Kickoff { get; set; }
    public string AwayTeam { get; set; } = "";
    public string HomeTeam { get; set; } = "";
    public string Pick { get; set; } = "";
    public double Confidence { get; set; }
    public TeamStats? Home { get; set; }
    public TeamStats? Away { get; set; }
    public string Notes { get; set; } = "";
}

public class TeamStats
{
    public int Wins { get; set; }
    public int Losses { get; set; }
    public double PPG { get; set; }
    public double PAPG { get; set; }
}
