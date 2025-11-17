namespace PickForge.Api.Models;

public record SeasonContext(
    int CurrentYear,
    int CurrentWeek,
    int SeasonType,
    int ActivePicksWeek,
    int MaxWeek
);

public record GameFeatures(
    string GameId,
    string HomeTeam,
    string AwayTeam,
    DateTime Kickoff,
    double? HomeMoneyline,
    double? AwayMoneyline,
    double? HomeSpread,
    int? HomeScore,
    int? AwayScore,
    string Status,
    bool IsFinal,
    bool IsInProgress
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
    bool IsUpcomingWeek,
    int SeasonYear,
    int SeasonType
);
