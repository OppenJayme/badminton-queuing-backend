namespace Api.Domain;

public enum SessionMemberStatus
{
    Joined = 0,
    CheckedIn = 1
}

public enum SessionMemberRole
{
    Member = 0,
    CoHost = 1
}

public class Session
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPublic { get; set; } = true;
    public int OwnerUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<SessionMember> Members { get; set; } = new List<SessionMember>();
    public ICollection<Queue> Queues { get; set; } = new List<Queue>();
}

public class SessionMember
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public Session? Session { get; set; }
    public int UserId { get; set; }
    public SessionMemberStatus Status { get; set; } = SessionMemberStatus.Joined;
    public SessionMemberRole Role { get; set; } = SessionMemberRole.Member;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CheckedInAt { get; set; }
}
