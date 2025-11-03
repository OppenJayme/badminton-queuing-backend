namespace Api.Dtos;

public class OpenCloseQueueRequest
{
    public string Mode { get; set; } = "Singles"; // "Singles" | "Doubles"
    public bool IsOpen { get; set; } = true;
}

public class EnqueueRequest
{
    // Use exactly one:
    public int? UserId { get; set; }           // for admin/QM to enqueue a specific user
    public int? GuestSessionId { get; set; }   // guest support (later)
}

public class LeaveRequest
{
    // If Player calls, server will infer from token; Admin/QM can target someone:
    public int? UserId { get; set; }
}
