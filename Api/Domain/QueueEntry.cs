namespace Api.Domain;

public class QueueEntry
{
    public int Id { get; set; }
    public int QueueId { get; set; }
    public Queue? Queue { get; set; }
    public int PlayerId { get; set; }
    public Player? Player { get; set; }
    public int Position { get; set; }
    public DateTime EnqueuedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
