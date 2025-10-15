namespace Api.Domain;

public class QueueEntry
{
    public int Id { get; set; }
    public int QueueId { get; set; }
    public Queue? Queue { get; set; }
    public int? UserId { get; set; }
    public int? GuestSessionId { get; set; }
    public int Position { get; set; }
    public DateTime EnqueuedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
