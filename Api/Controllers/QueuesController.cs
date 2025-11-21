using Api.Data;
using Api.Domain;
using Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/queues")]
[Authorize]
public class QueuesController : ControllerBase
{
    private readonly AppDbContext _db;
    public QueuesController(AppDbContext db) { _db = db; }

    private QueueMode ParseMode(QueueMode mode) => Enum.IsDefined(typeof(QueueMode), mode) ? mode : QueueMode.Singles;

    [HttpPost]
    public async Task<IActionResult> CreateQueue([FromBody] CreateQueueRequest req)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var q = new Queue
        {
            Name = req.Name,
            Mode = ParseMode(req.Mode),
            IsOpen = true
        };
        _db.Queues.Add(q);
        await _db.SaveChangesAsync();
        return Ok(new QueueSummaryDto
        {
            Id = q.Id,
            Name = q.Name,
            Mode = q.Mode.ToString(),
            IsOpen = q.IsOpen,
            CreatedAt = q.CreatedAt
        });
    }

    [HttpGet("{queueId}")]
    public async Task<IActionResult> GetQueue(int queueId)
    {
        var q = await _db.Queues.Include(x => x.Entries.Where(e => e.IsActive)).FirstOrDefaultAsync(x => x.Id == queueId);
        if (q == null) return NotFound(new { message = "Queue not found" });

        var playerIds = q.Entries.Select(e => e.PlayerId).ToList();
        var players = await _db.Players.Where(p => playerIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p);

        var entries = q.Entries
            .OrderBy(e => e.Position)
            .Select(e => new QueueEntryDto
            {
                Id = e.Id,
                Position = e.Position,
                PlayerId = e.PlayerId,
                DisplayName = players.TryGetValue(e.PlayerId, out var p) ? p.DisplayName : "Player",
                GamesPlayed = players.TryGetValue(e.PlayerId, out var p2) ? p2.GamesPlayed : 0,
                JoinedAt = e.EnqueuedAt
            })
            .ToList();

        return Ok(new
        {
            id = q.Id,
            name = q.Name,
            mode = q.Mode.ToString(),
            isOpen = q.IsOpen,
            entries
        });
    }

    [HttpPost("{queueId}/status")]
    public async Task<IActionResult> SetStatus(int queueId, [FromBody] bool isOpen)
    {
        var q = await _db.Queues.FindAsync(queueId);
        if (q == null) return NotFound(new { message = "Queue not found" });
        q.IsOpen = isOpen;
        await _db.SaveChangesAsync();
        return Ok(new { q.Id, q.IsOpen });
    }

    [HttpPost("{queueId}/enqueue")]
    public async Task<IActionResult> Enqueue(int queueId, [FromBody] EnqueueRequest req)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var q = await _db.Queues.Include(x => x.Entries.Where(e => e.IsActive)).FirstOrDefaultAsync(x => x.Id == queueId);
        if (q == null) return NotFound(new { message = "Queue not found" });
        if (!q.IsOpen) return BadRequest(new { message = "Queue is closed" });

        var player = await _db.Players.FindAsync(req.PlayerId);
        if (player == null) return NotFound(new { message = "Player not found" });

        bool exists = q.Entries.Any(e => e.PlayerId == req.PlayerId && e.IsActive);
        if (exists) return Conflict(new { message = "Player already in queue" });

        var nextPos = q.Entries.Any() ? q.Entries.Max(e => e.Position) + 1 : 1;
        var entry = new QueueEntry
        {
            QueueId = q.Id,
            PlayerId = req.PlayerId,
            Position = nextPos,
            IsActive = true
        };
        _db.QueueEntries.Add(entry);
        await _db.SaveChangesAsync();
        return await GetQueue(queueId);
    }

    [HttpPost("{queueId}/remove")]
    public async Task<IActionResult> Remove(int queueId, [FromBody] RemoveRequest req)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var q = await _db.Queues.Include(x => x.Entries.Where(e => e.IsActive)).FirstOrDefaultAsync(x => x.Id == queueId);
        if (q == null) return NotFound(new { message = "Queue not found" });

        var entry = q.Entries.FirstOrDefault(e => e.PlayerId == req.PlayerId && e.IsActive);
        if (entry == null) return NotFound(new { message = "Not in queue" });

        entry.IsActive = false;
        await _db.SaveChangesAsync();

        var remaining = q.Entries.Where(e => e.IsActive).OrderBy(e => e.Position).ToList();
        for (int i = 0; i < remaining.Count; i++) remaining[i].Position = i + 1;
        await _db.SaveChangesAsync();

        return await GetQueue(queueId);
    }

    [HttpPost("{queueId}/start-match")]
    public async Task<IActionResult> StartMatch(int queueId)
    {
        var q = await _db.Queues.Include(x => x.Entries.Where(e => e.IsActive)).FirstOrDefaultAsync(x => x.Id == queueId);
        if (q == null) return NotFound(new { message = "Queue not found" });

        var playerIds = q.Entries.Select(e => e.PlayerId).ToList();
        var players = await _db.Players.Where(p => playerIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p);

        int needed = q.Mode == QueueMode.Singles ? 2 : 4;
        var ready = q.Entries
            .Where(e => e.IsActive)
            .OrderBy(e => players.TryGetValue(e.PlayerId, out var p) ? p.GamesPlayed : 0)
            .ThenBy(e => e.EnqueuedAt)
            .Take(needed)
            .ToList();

        if (ready.Count < needed) return BadRequest(new { message = "Not enough players" });

        var match = new Match
        {
            QueueId = queueId,
            Mode = q.Mode,
            Status = MatchStatus.Ongoing,
            StartTime = DateTime.UtcNow
        };
        _db.Matches.Add(match);
        await _db.SaveChangesAsync();

        foreach (var e in ready)
        {
            e.IsActive = false;
            if (players.TryGetValue(e.PlayerId, out var p))
            {
                p.GamesPlayed += 1;
            }
            _db.MatchPlayers.Add(new MatchPlayer
            {
                MatchId = match.Id,
                PlayerId = e.PlayerId,
                EnqueuedAtSnapshot = e.EnqueuedAt
            });
        }
        await _db.SaveChangesAsync();

        var remaining = q.Entries.Where(e => e.IsActive).OrderBy(e => e.Position).ToList();
        for (int i = 0; i < remaining.Count; i++) remaining[i].Position = i + 1;
        await _db.SaveChangesAsync();

        return Ok(new { matchId = match.Id, message = "Match started" });
    }

    [HttpPost("{queueId}/finish-match")]
    public async Task<IActionResult> FinishMatch(int queueId, [FromBody] FinishMatchRequest req)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var match = await _db.Matches.FirstOrDefaultAsync(m => m.Id == req.MatchId && m.QueueId == queueId);
        if (match == null) return NotFound(new { message = "Match not found" });
        if (match.Status != MatchStatus.Ongoing) return BadRequest(new { message = "Match not ongoing" });

        match.Status = MatchStatus.Finished;
        match.FinishTime = DateTime.UtcNow;
        match.ScoreText = req.ScoreText;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Match finished", durationSeconds = match.DurationSeconds });
    }
}
