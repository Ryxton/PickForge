namespace PickForge.Client.Models;

public class SeasonContextResponse
{
    public int CurrentYear { get; set; }
    public int CurrentWeek { get; set; }
    public int SeasonType { get; set; }
    public int ActivePicksWeek { get; set; }
    public int MaxWeek { get; set; }
}

public class ScoreboardWeekResponse
{
    public int WeekNumber { get; set; }
    public int SeasonYear { get; set; }
    public int SeasonType { get; set; }
    public bool IsActivePicksWeek { get; set; }
    public List<GameViewModel> Games { get; set; } = new();
}

public class GameViewModel
{
    public string GameId { get; set; } = "";
    public string HomeTeam { get; set; } = "";
    public string AwayTeam { get; set; } = "";
    public DateTime Kickoff { get; set; }
    public int? HomeScore { get; set; }
    public int? AwayScore { get; set; }
    public string Status { get; set; } = "pre";
    public bool IsFinal { get; set; }
    public bool IsInProgress { get; set; }
}

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

public class SavePredictionRequest
{
    public int Week { get; set; }
    public int SeasonYear { get; set; }
    public string GameId { get; set; } = "";
    public string HomeTeam { get; set; } = "";
    public string AwayTeam { get; set; } = "";
    public string PredictedWinner { get; set; } = "";
    public double Confidence { get; set; }
    public string Notes { get; set; } = "";
}

public class PredictionHistoryItem
{
    public int Id { get; set; }
    public int Week { get; set; }
    public int SeasonYear { get; set; }
    public string GameId { get; set; } = "";
    public string HomeTeam { get; set; } = "";
    public string AwayTeam { get; set; } = "";
    public string PredictedWinner { get; set; } = "";
    public double Confidence { get; set; }
    public bool? WasCorrect { get; set; }
    public DateTime CreatedUtc { get; set; }
    public string Notes { get; set; } = "";
}
