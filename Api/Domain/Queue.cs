namespace Api.Domain;

public enum QueueMode { Singles, Doubles }

public class Queue
{
    public int Id { get; set; }
    public int CourtId { get; set; }
    public Court? Court { get; set; }
    public QueueMode Mode { get; set; } = QueueMode.Singles;
    public bool IsOpen { get; set; } = true;
    public ICollection<QueueEntry> Entries { get; set; } = new List<QueueEntry>();
}
