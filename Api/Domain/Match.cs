using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Domain;

public enum MatchStatus { Created, Ongoing, Finished, Canceled }

public class Match
{
    public int Id { get; set; }
    public int CourtId { get; set; }
    public Court? Court { get; set; }

    // optional link to group session
    public int? GroupSessionId { get; set; }
    public GroupSession? GroupSession { get; set; }

    public QueueMode Mode { get; set; }
    public MatchStatus Status { get; set; } = MatchStatus.Created;
    public string PlayersCsv { get; set; } = "";
    public DateTime? StartTime { get; set; }
    public DateTime? FinishTime { get; set; }
    public string? ScoreText { get; set; }

    [NotMapped]
    public double? DurationSeconds =>
        (StartTime != null && FinishTime != null)
            ? (FinishTime.Value - StartTime.Value).TotalSeconds
            : null;
}
