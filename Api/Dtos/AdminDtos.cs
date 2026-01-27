namespace Api.Dtos;

public class AdminTotalsResponse
{
    public int TotalUsers { get; set; }
    public int TotalPlayers { get; set; }
    public int TotalQueues { get; set; }
    public int TotalSessions { get; set; }
    public int TotalMatches { get; set; }
}

public class AdminMatchSummary
{
    public int Id { get; set; }
    public int QueueId { get; set; }
    public string QueueName { get; set; } = "";
    public string? SessionName { get; set; }
    public int? OwnerUserId { get; set; }
    public string Mode { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime? StartTime { get; set; }
    public DateTime? FinishTime { get; set; }
    public double? DurationSeconds { get; set; }
    public string? ScoreText { get; set; }
}

public class AdminUserSummary
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Role { get; set; } = "";
    public bool IsSoftDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
}
