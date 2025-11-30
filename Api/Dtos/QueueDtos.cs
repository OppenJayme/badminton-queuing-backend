using System.ComponentModel.DataAnnotations;
using Api.Domain;

namespace Api.Dtos;

public class CreateQueueRequest
{
    [MaxLength(200)]
    public string? Name { get; set; }
    // Accept any casing/string; controller will parse/fallback
    public string? Mode { get; set; }
    public int? SessionId { get; set; }
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
    public string? Mode { get; set; }
}

public class StartMatchManualRequest
{
    public List<int> PlayerIds { get; set; } = new();
    public string? ScoreText { get; set; }
    public string? Mode { get; set; }
}

public class SetScoreDto
{
    [Range(0, 99)]
    public int A { get; set; }

    [Range(0, 99)]
    public int B { get; set; }
}

public class FinishMatchRequest
{
    [Required]
    public int MatchId { get; set; }

    [Required]
    public int WinnerId { get; set; }

    [MinLength(1)]
    [MaxLength(3)]
    public List<SetScoreDto> Sets { get; set; } = new();
}
