using System;

namespace Api.Domain;

public enum GroupRole { Owner, QueueMaster, Player }

public class GroupMember
{
    public int GroupId { get; set; }
    public Group? Group { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public GroupRole Role { get; set; } = GroupRole.Player;
    public string DisplayName { get; set; } = default!;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
