using System.ComponentModel.DataAnnotations;
using Api.Domain;

namespace Api.Dtos;

public class CreateQueueRequest
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = "Queue";
    public QueueMode Mode { get; set; } = QueueMode.Singles;
}

public class QueueSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Mode { get; set; } = "";
    public bool IsOpen { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class QueueEntryDto
{
    public int Id { get; set; }
    public int Position { get; set; }
    public int PlayerId { get; set; }
    public string DisplayName { get; set; } = "";
    public int GamesPlayed { get; set; }
    public DateTime JoinedAt { get; set; }
}

public class EnqueueRequest
{
    [Required]
    public int PlayerId { get; set; }
}

public class RemoveRequest
{
    [Required]
    public int PlayerId { get; set; }
}

public class StartMatchRequest
{
    public string? ScoreText { get; set; }
}

public class FinishMatchRequest
{
    [Required]
    public int MatchId { get; set; }
    public string? ScoreText { get; set; }
}
