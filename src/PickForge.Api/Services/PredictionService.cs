using PickForge.Api.Models;

namespace PickForge.Api.Services;

public class PredictionService
{
    public GamePrediction Predict(GameFeatures game, TeamSeasonStats? home, TeamSeasonStats? away, int recentGames)
    {
        if (home is null || away is null || home.GamesPlayed == 0 || away.GamesPlayed == 0)
        {
            return new GamePrediction(game.GameId, game.HomeTeam, game.AwayTeam, game.HomeTeam, 0.55,
                "Fallback: insufficient stats; pick home.");
        }

        double hOff = home.PointsForPerGame, aOff = away.PointsForPerGame;
        double hDef = -home.PointsAgainstPerGame, aDef = -away.PointsAgainstPerGame;

        double hRO = home.RecentPointsForPerGame(recentGames);
        double aRO = away.RecentPointsForPerGame(recentGames);
        double hRD = -home.RecentPointsAgainstPerGame(recentGames);
        double aRD = -away.RecentPointsAgainstPerGame(recentGames);

        double hTrend = (hRO - hOff) + (hRD - hDef);
        double aTrend = (aRO - aOff) + (aRD - aDef);

        const double offenseW = 0.5, defenseW = 0.4, trendW = 0.1;

        double hRating = hOff * offenseW + hDef * defenseW + hTrend * trendW;
        double aRating = aOff * offenseW + aDef * defenseW + aTrend * trendW;

        var pick = hRating >= aRating ? game.HomeTeam : game.AwayTeam;
        double diff = Math.Abs(hRating - aRating);
        double confidence = 0.5 + (Math.Tanh(diff / 10.0) * 0.4);

        string notes =
            $"Season Off/Def plus last {recentGames} games trend. " +
            $"Home PPG {home.PointsForPerGame:F1}, PAPG {home.PointsAgainstPerGame:F1}. " +
            $"Away PPG {away.PointsForPerGame:F1}, PAPG {away.PointsAgainstPerGame:F1}.";

        return new GamePrediction(game.GameId, game.HomeTeam, game.AwayTeam, pick, confidence, notes);
    }
}
