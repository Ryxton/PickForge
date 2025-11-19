namespace PickForge.Api.Models;

public class Prediction
{
    public int Id { get; set; }
    public int Week { get; set; }
    public int SeasonYear { get; set; }
    public string GameId { get; set; } = "";
    public string HomeTeam { get; set; } = "";
    public string AwayTeam { get; set; } = "";
    public string PredictedWinner { get; set; } = "";
    public double Confidence { get; set; }
    public bool? WasCorrect { get; set; }  // null until game finishes
    public DateTime CreatedUtc { get; set; }
    public string Notes { get; set; } = "";
}

public record PredictionDto(
    int Week,
    int SeasonYear,
    string GameId,
    string HomeTeam,
    string AwayTeam,
    string PredictedWinner,
    double Confidence,
    string Notes
);