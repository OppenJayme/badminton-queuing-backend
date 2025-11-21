using System;

namespace Api.Domain;

public class GroupSessionStat
{
    public int GroupSessionId { get; set; }
    public GroupSession? GroupSession { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public int GamesPlayed { get; set; } = 0;
}
