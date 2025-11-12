namespace PickForge.Api.Models;

public record GameFeatures(
    string GameId,
    string HomeTeam,
    string AwayTeam,
    DateTime Kickoff,
    double? HomeMoneyline,
    double? AwayMoneyline,
    double? HomeSpread
);

public record GamePrediction(
    string GameId,
    string HomeTeam,
    string AwayTeam,
    string PredictedWinner,
    double Confidence,
    string Notes
);

public record ScoreboardResult(
    string RawJson,
    ScoreboardResponse Scoreboard,
    List<GameFeatures> Games,
    int WeekNumber,
    bool IsUpcomingWeek
);
