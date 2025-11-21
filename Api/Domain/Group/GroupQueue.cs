using System.Collections.Generic;

namespace Api.Domain;

public class GroupQueue
{
    public int Id { get; set; }
    public int GroupSessionId { get; set; }
    public GroupSession? GroupSession { get; set; }

    public QueueMode Mode { get; set; } = QueueMode.Singles;
    public bool IsOpen { get; set; } = true;

    public ICollection<GroupQueueEntry> Entries { get; set; } = new List<GroupQueueEntry>();
}
