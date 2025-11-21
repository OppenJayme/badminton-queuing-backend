using System;
using System.Collections.Generic;

namespace Api.Domain;

public class Group
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public int OwnerId { get; set; }
    public User? Owner { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
    public ICollection<GroupSession> Sessions { get; set; } = new List<GroupSession>();
}
