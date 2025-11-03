using Api.Data;
using Api.Domain;
using Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Api.Controllers;

[ApiController]
[Route("api/queues")]
public class QueuesController : ControllerBase
{
    private readonly AppDbContext _db;
    public QueuesController(AppDbContext db) { _db = db; }

    private static QueueMode ParseMode(string mode)
        => Enum.TryParse<QueueMode>(mode, true, out var m) ? m : QueueMode.Singles;

    // Helper: find or create a queue for a court+mode
    private async Task<Queue> GetOrCreateQueueAsync(int courtId, QueueMode mode)
    {
        var q = await _db.Queues
            .Include(x => x.Entries.Where(e => e.IsActive))
            .FirstOrDefaultAsync(x => x.CourtId == courtId && x.Mode == mode);

        if (q == null)
        {
            q = new Queue { CourtId = courtId, Mode = mode, IsOpen = false };
            _db.Queues.Add(q);
            await _db.SaveChangesAsync();
            // re-load with entries
            q = await _db.Queues.Include(x => x.Entries.Where(e => e.IsActive))
                                .FirstAsync(x => x.Id == q.Id);
        }
        return q;
    }

    // ===== Read-only (already had) =====
    // GET /api/queues/{courtId}?mode=Singles|Doubles
    [HttpGet("{courtId}")]
    [Authorize]
    public async Task<IActionResult> GetQueue(int courtId, [FromQuery] string mode = "Singles")
    {
        var q = await GetOrCreateQueueAsync(courtId, ParseMode(mode));
        var result = new
        {
            id = q.Id,
            isOpen = q.IsOpen,
            mode = q.Mode.ToString(),
            entries = q.Entries
                .OrderBy(e => e.Position)
                .Select(e => new { e.Id, e.Position, e.UserId, e.GuestSessionId, e.EnqueuedAt })
        };
        return Ok(result);
    }

    // ===== QM: open/close =====
    // POST /api/queues/{courtId}/status
    [HttpPost("{courtId}/status")]
    [Authorize(Roles = "QueueMaster,Admin")]
    public async Task<IActionResult> SetStatus(int courtId, [FromBody] OpenCloseQueueRequest req)
    {
        var court = await _db.Courts.FindAsync(courtId);
        if (court == null || !court.IsActive) return NotFound(new { message = "Court not found" });

        var q = await GetOrCreateQueueAsync(courtId, ParseMode(req.Mode));
        q.IsOpen = req.IsOpen;
        await _db.SaveChangesAsync();
        return Ok(new { q.Id, q.IsOpen, mode = q.Mode.ToString() });
    }

    // ===== Enqueue (Player self or QM/Admin on behalf) =====
    // POST /api/queues/{courtId}/enqueue?mode=Singles|Doubles
    [HttpPost("{courtId}/enqueue")]
    [Authorize] // Player/QM/Admin
    public async Task<IActionResult> Enqueue(int courtId, [FromQuery] string mode, [FromBody] EnqueueRequest body)
    {
        var q = await GetOrCreateQueueAsync(courtId, ParseMode(mode));
        if (!q.IsOpen) return BadRequest(new { message = "Queue is closed" });

        // Identify who to enqueue
        int? userId = body.UserId;
        int? guestSessionId = body.GuestSessionId;

        // If Player calls with no explicit user, infer from token
        if (userId == null && guestSessionId == null)
        {
            var claimSub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (int.TryParse(claimSub, out var currentUserId))
                userId = currentUserId;
        }

        if (userId == null && guestSessionId == null)
            return BadRequest(new { message = "Specify userId or guestSessionId, or call as a logged-in Player." });

        // Enforce one active queue per user (simple check)
        if (userId != null)
        {
            bool alreadyQueued = await _db.QueueEntries
                .AnyAsync(e => e.IsActive && e.UserId == userId);
            if (alreadyQueued) return Conflict(new { message = "User already in an active queue" });
        }

        var nextPos = q.Entries.Any() ? q.Entries.Max(e => e.Position) + 1 : 1;
        var entry = new QueueEntry
        {
            QueueId = q.Id,
            UserId = userId,
            GuestSessionId = guestSessionId,
            Position = nextPos,
            IsActive = true
        };
        _db.QueueEntries.Add(entry);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Enqueued",
            entry = new { entry.Id, entry.Position, entry.UserId, entry.GuestSessionId },
            queueLength = await _db.QueueEntries.CountAsync(e => e.QueueId == q.Id && e.IsActive)
        });
    }

    // ===== Leave (Player removes self) or QM/Admin removes by userId =====
    // POST /api/queues/{courtId}/leave?mode=Singles|Doubles
    [HttpPost("{courtId}/leave")]
    [Authorize] // Player/QM/Admin
    public async Task<IActionResult> Leave(int courtId, [FromQuery] string mode, [FromBody] LeaveRequest body)
    {
        var q = await GetOrCreateQueueAsync(courtId, ParseMode(mode));

        int? targetUserId = body.UserId;
        // If not provided, infer from token for Player self-leave
        if (targetUserId == null)
        {
            var claimSub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (int.TryParse(claimSub, out var currentUserId))
                targetUserId = currentUserId;
        }

        if (targetUserId == null)
            return BadRequest(new { message = "No user specified" });

        var entry = await _db.QueueEntries
            .Where(e => e.QueueId == q.Id && e.IsActive && e.UserId == targetUserId)
            .OrderBy(e => e.Position)
            .FirstOrDefaultAsync();

        if (entry == null) return NotFound(new { message = "Not in queue" });

        entry.IsActive = false;
        await _db.SaveChangesAsync();

        // Re-pack positions (simple)
        var remaining = await _db.QueueEntries
            .Where(e => e.QueueId == q.Id && e.IsActive)
            .OrderBy(e => e.Position)
            .ToListAsync();
        for (int i = 0; i < remaining.Count; i++) remaining[i].Position = i + 1;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Left queue", queueLength = remaining.Count });
    }
}
