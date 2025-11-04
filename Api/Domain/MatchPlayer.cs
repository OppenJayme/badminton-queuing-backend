namespace Api.Domain;

public class MatchPlayer
{
    public int Id { get; set; }
    public int MatchId { get; set; }
    public Match? Match { get; set; }

    public int? UserId { get; set; }
    public int? GuestSessionId { get; set; }

    // snapshot of the queue entry's enqueued time when the match started
    public DateTime EnqueuedAtSnapshot { get; set; }
}
