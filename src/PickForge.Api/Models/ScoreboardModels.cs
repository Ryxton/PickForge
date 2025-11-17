using System.Text.Json.Serialization;

namespace PickForge.Api.Models;

public class ScoreboardResponse
{
    [JsonPropertyName("week")] public WeekInfo? Week { get; set; }
    [JsonPropertyName("events")] public List<Event>? Events { get; set; }
}

public class WeekInfo
{
    [JsonPropertyName("number")] public int Number { get; set; }
    [JsonPropertyName("text")] public string? Text { get; set; }
}

public class Event
{
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("date")] public DateTime Date { get; set; }
    [JsonPropertyName("competitions")] public List<Competition>? Competitions { get; set; }
}

public class Competition
{
    [JsonPropertyName("competitors")] public List<Competitor>? Competitors { get; set; }
    [JsonPropertyName("status")] public CompetitionStatus? Status { get; set; }
}

public class CompetitionStatus
{
    [JsonPropertyName("type")] public StatusType? Type { get; set; }
}

public class StatusType
{
    [JsonPropertyName("state")] public string? State { get; set; }
    [JsonPropertyName("completed")] public bool Completed { get; set; }
}

public class Competitor
{
    [JsonPropertyName("homeAway")] public string? HomeAway { get; set; }
    [JsonPropertyName("team")] public TeamInfo? Team { get; set; }
    [JsonPropertyName("score")] public string? Score { get; set; }
}

public class TeamInfo
{
    [JsonPropertyName("displayName")] public string? DisplayName { get; set; }
    [JsonPropertyName("abbreviation")] public string? Abbreviation { get; set; }
}
