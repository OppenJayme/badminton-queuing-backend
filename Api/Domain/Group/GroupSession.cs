using System;
using System.Collections.Generic;

namespace Api.Domain;

public enum GroupSessionStatus { Draft, Active, Ended }

public class GroupSession
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public Group? Group { get; set; }

    public int LocationId { get; set; }
    public Location? Location { get; set; }

    public int Courts { get; set; } = 1;
    public int Hours { get; set; } = 2;
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public GroupSessionStatus Status { get; set; } = GroupSessionStatus.Draft;

    public ICollection<GroupQueue> Queues { get; set; } = new List<GroupQueue>();
    public ICollection<Match> Matches { get; set; } = new List<Match>();
}
