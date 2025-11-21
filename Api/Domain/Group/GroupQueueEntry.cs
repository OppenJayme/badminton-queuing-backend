using System;

namespace Api.Domain;

public class GroupQueueEntry
{
    public int Id { get; set; }
    public int GroupQueueId { get; set; }
    public GroupQueue? GroupQueue { get; set; }

    public int? UserId { get; set; }
    public User? User { get; set; }
    public int? GuestSessionId { get; set; }

    public int Position { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
