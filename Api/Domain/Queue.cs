namespace Api.Domain;

public enum QueueMode { Singles, Doubles }

public class Queue
{
    public int Id { get; set; }
    public string Name { get; set; } = "Queue";
    public int? OwnerUserId { get; set; }
    public int? SessionId { get; set; }
    public Session? Session { get; set; }
    public QueueMode Mode { get; set; } = QueueMode.Singles;
    public bool IsOpen { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<QueueEntry> Entries { get; set; } = new List<QueueEntry>();
}
