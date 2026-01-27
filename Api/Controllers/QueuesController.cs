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
[Authorize]
public class QueuesController : ControllerBase
{
    private readonly AppDbContext _db;
    public QueuesController(AppDbContext db) { _db = db; }

    private int? CurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return int.TryParse(sub, out var id) ? id : (int?)null;
    }

    private QueueMode ParseMode(string mode)
    {
        return Enum.TryParse<QueueMode>(mode, true, out var parsed) ? parsed : QueueMode.Singles;
    }

    private async Task<bool> IsOwnerOrCoHost(int? sessionId, int userId)
    {
        if (sessionId == null) return false;
        var session = await _db.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId.Value);
        if (session == null) return false;
        if (session.OwnerUserId == userId) return true;
        var mem = await _db.SessionMembers.FirstOrDefaultAsync(m => m.SessionId == sessionId && m.UserId == userId);
        return mem != null && mem.Role == SessionMemberRole.CoHost;
    }

    [HttpPost("{queueId}/mode")]
    public async Task<IActionResult> SetMode(int queueId, [FromBody] SetQueueModeRequest req)
    {
        var userId = CurrentUserId();
        if (userId == null) return Unauthorized();
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var q = await _db.Queues.FirstOrDefaultAsync(x => x.Id == queueId);
        if (q == null) return NotFound(new { message = "Queue not found" });
        if (!await CanManageQueue(q, userId.Value)) return Forbid();

        q.Mode = ParseMode(req.Mode);
        await _db.SaveChangesAsync();

        return Ok(new
        {
          id = q.Id,
          name = q.Name,
          mode = q.Mode.ToString(),
          isOpen = q.IsOpen,
          sessionId = q.SessionId
        });
    }

    private async Task<bool> IsSessionMember(int? sessionId, int userId)
    {
        if (sessionId == null) return false;
        var mem = await _db.SessionMembers.FirstOrDefaultAsync(m => m.SessionId == sessionId && m.UserId == userId);
        return mem != null;
    }

    private async Task<bool> IsCheckedInMember(int? sessionId, int userId)
    {
        if (sessionId == null) return false;
        var mem = await _db.SessionMembers.FirstOrDefaultAsync(m => m.SessionId == sessionId && m.UserId == userId);
        return mem != null && mem.Status == SessionMemberStatus.CheckedIn;
    }

    private async Task<bool> CanReadQueue(Queue queue, int userId)
    {
        if (queue.OwnerUserId == userId) return true;
        if (queue.SessionId != null)
        {
            return await IsSessionMember(queue.SessionId, userId) || await IsOwnerOrCoHost(queue.SessionId, userId);
        }
        return false;
    }

    private async Task<bool> CanManageQueue(Queue queue, int userId)
    {
        if (queue.OwnerUserId == userId) return true;
        if (queue.SessionId != null)
        {
            return await IsOwnerOrCoHost(queue.SessionId, userId);
        }
        return false;
    }

    [HttpGet("{queueId}/matches")]
    public async Task<IActionResult> GetMatches(int queueId, [FromQuery] string? status = null)
    {
        var userId = CurrentUserId();
        if (userId == null) return Unauthorized();

        var queue = await _db.Queues.FirstOrDefaultAsync(q => q.Id == queueId);
        if (queue == null) return NotFound(new { message = "Queue not found" });
        if (!await CanReadQueue(queue, userId.Value)) return Forbid();

        var query = _db.Matches
            .Where(m => m.QueueId == queueId)
            .OrderByDescending(m => m.StartTime ?? DateTime.MinValue)
            .ThenByDescending(m => m.Id)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<MatchStatus>(status, true, out var s))
        {
            query = query.Where(m => m.Status == s);
        }

        var matches = await query.Take(50).ToListAsync();
        var matchIds = matches.Select(m => m.Id).ToList();
        var matchPlayers = await _db.MatchPlayers.Where(mp => matchIds.Contains(mp.MatchId)).ToListAsync();
        var playerIds = matchPlayers.Select(mp => mp.PlayerId).Distinct().ToList();
        var players = await _db.Players.Where(p => playerIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p);

        var result = matches.Select(m => new
        {
            id = m.Id,
            status = m.Status.ToString(),
            mode = m.Mode.ToString(),
            startTime = m.StartTime,
            finishTime = m.FinishTime,
            scoreText = m.ScoreText,
            players = matchPlayers
                .Where(mp => mp.MatchId == m.Id)
                .Select(mp => new
                {
                    id = mp.PlayerId,
                    name = players.TryGetValue(mp.PlayerId, out var p) ? p.DisplayName : $"Player {mp.PlayerId}"
                })
                .ToList()
        });

        return Ok(result);
    }

    [HttpGet("{queueId}/ongoing-matches")]
    public async Task<IActionResult> GetOngoingMatches(int queueId)
    {
        var userId = CurrentUserId();
        if (userId == null) return Unauthorized();

        var queue = await _db.Queues.FirstOrDefaultAsync(q => q.Id == queueId);
        if (queue == null) return NotFound(new { message = "Queue not found" });
        if (!await CanReadQueue(queue, userId.Value)) return Forbid();

        var matches = await _db.Matches
            .Where(m => m.QueueId == queueId && m.Status == MatchStatus.Ongoing)
            .OrderBy(m => m.StartTime)
            .ToListAsync();

        var matchIds = matches.Select(m => m.Id).ToList();
        var matchPlayers = await _db.MatchPlayers.Where(mp => matchIds.Contains(mp.MatchId)).ToListAsync();
        var playerIds = matchPlayers.Select(mp => mp.PlayerId).Distinct().ToList();
        var players = await _db.Players.Where(p => playerIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p);

        var result = matches.Select(m => new
        {
            id = m.Id,
            startedAt = m.StartTime,
            players = matchPlayers
                .Where(mp => mp.MatchId == m.Id)
                .Select(mp => new
                {
                    id = mp.PlayerId,
                    name = players.TryGetValue(mp.PlayerId, out var p) ? p.DisplayName : $"Player {mp.PlayerId}"
                })
                .ToList()
        });

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateQueue([FromBody] CreateQueueRequest req)
    {
        var userId = CurrentUserId();
        if (userId == null) return Unauthorized();
        // Be forgiving with payload; default name/mode if missing
        var name = string.IsNullOrWhiteSpace(req?.Name) ? "Queue" : req!.Name!;
        var modeString = string.IsNullOrWhiteSpace(req?.Mode) ? "Singles" : req!.Mode!;

        int? sessionId = req?.SessionId;
        if (sessionId != null)
        {
            var session = await _db.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId);
            if (session == null) return NotFound(new { message = "Session not found" });
            var canManage = await IsOwnerOrCoHost(sessionId, userId.Value);
            if (!canManage) return Forbid();
        }

        var q = new Queue
        {
            Name = name,
            Mode = ParseMode(modeString),
            IsOpen = true,
            OwnerUserId = userId,
            SessionId = sessionId
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
        var userId = CurrentUserId();
        if (userId == null) return Unauthorized();

        var q = await _db.Queues.Include(x => x.Entries.Where(e => e.IsActive)).FirstOrDefaultAsync(x => x.Id == queueId);
        if (q == null) return NotFound(new { message = "Queue not found" });
        if (!await CanReadQueue(q, userId.Value)) return Forbid();

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
            sessionId = q.SessionId,
            entries
        });
    }

    [HttpPost("{queueId}/status")]
    public async Task<IActionResult> SetStatus(int queueId, [FromBody] bool isOpen)
    {
        var userId = CurrentUserId();
        if (userId == null) return Unauthorized();
        var q = await _db.Queues.FirstOrDefaultAsync(q => q.Id == queueId);
        if (q == null) return NotFound(new { message = "Queue not found" });
        if (!await CanManageQueue(q, userId.Value)) return Forbid();
        q.IsOpen = isOpen;
        await _db.SaveChangesAsync();
        return Ok(new { q.Id, q.IsOpen });
    }

    [HttpPost("{queueId}/enqueue")]
    public async Task<IActionResult> Enqueue(int queueId, [FromBody] EnqueueRequest req)
    {
        var userId = CurrentUserId();
        if (userId == null) return Unauthorized();
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var q = await _db.Queues.Include(x => x.Entries.Where(e => e.IsActive)).FirstOrDefaultAsync(x => x.Id == queueId);
        if (q == null) return NotFound(new { message = "Queue not found" });
        if (!await CanManageQueue(q, userId.Value)) return Forbid();
        if (!q.IsOpen) return BadRequest(new { message = "Queue is closed" });

        var player = await _db.Players.FirstOrDefaultAsync(p => p.Id == req.PlayerId);
        if (player == null) return NotFound(new { message = "Player not found" });

        if (q.SessionId != null)
        {
            if (player.UserId == null)
            {
                return BadRequest(new { message = "Only registered, checked-in members can be queued in a session" });
            }
            var checkedIn = await IsCheckedInMember(q.SessionId, player.UserId.Value);
            if (!checkedIn) return BadRequest(new { message = "Player must be a checked-in session member" });
        }

        bool exists = q.Entries.Any(e => e.PlayerId == req.PlayerId && e.IsActive);
        if (exists) return Conflict(new { message = "Player already in queue" });

        var inactive = await _db.QueueEntries
            .Where(e => e.QueueId == queueId && e.PlayerId == req.PlayerId && !e.IsActive)
            .OrderByDescending(e => e.EnqueuedAt)
            .FirstOrDefaultAsync();

        var nextPos = q.Entries.Any() ? q.Entries.Max(e => e.Position) + 1 : 1;
        if (inactive != null)
        {
            inactive.IsActive = true;
            inactive.Position = nextPos;
            inactive.EnqueuedAt = DateTime.UtcNow;
            _db.QueueEntries.Update(inactive);
        }
        else
        {
            var entry = new QueueEntry
            {
                QueueId = q.Id,
                PlayerId = req.PlayerId,
                Position = nextPos,
                IsActive = true
            };
            _db.QueueEntries.Add(entry);
        }
        await _db.SaveChangesAsync();
        return await GetQueue(queueId);
    }

    [HttpPost("{queueId}/remove")]
    public async Task<IActionResult> Remove(int queueId, [FromBody] RemoveRequest req)
    {
        var userId = CurrentUserId();
        if (userId == null) return Unauthorized();
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var q = await _db.Queues.Include(x => x.Entries.Where(e => e.IsActive)).FirstOrDefaultAsync(x => x.Id == queueId);
        if (q == null) return NotFound(new { message = "Queue not found" });
        if (!await CanManageQueue(q, userId.Value)) return Forbid();

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
    public async Task<IActionResult> StartMatch(int queueId, [FromBody] StartMatchRequest? req)
    {
        var userId = CurrentUserId();
        if (userId == null) return Unauthorized();

        var q = await _db.Queues.Include(x => x.Entries.Where(e => e.IsActive)).FirstOrDefaultAsync(x => x.Id == queueId);
        if (q == null) return NotFound(new { message = "Queue not found" });
        if (!await CanManageQueue(q, userId.Value)) return Forbid();

        var playerIds = q.Entries.Select(e => e.PlayerId).ToList();
        var players = await _db.Players.Where(p => playerIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p);

        var mode = req?.Mode != null ? ParseMode(req.Mode) : q.Mode;
        int needed = mode == QueueMode.Singles ? 2 : 4;
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
            Mode = mode,
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

    [HttpPost("{queueId}/start-match-manual")]
    public async Task<IActionResult> StartMatchManual(int queueId, [FromBody] StartMatchManualRequest req)
    {
        var userId = CurrentUserId();
        if (userId == null) return Unauthorized();
        if (req?.PlayerIds == null || req.PlayerIds.Count == 0)
            return BadRequest(new { message = "playerIds required" });

        var distinctIds = req.PlayerIds.Distinct().ToList();

        var q = await _db.Queues.Include(x => x.Entries.Where(e => e.IsActive)).FirstOrDefaultAsync(x => x.Id == queueId);
        if (q == null) return NotFound(new { message = "Queue not found" });
        if (!await CanManageQueue(q, userId.Value)) return Forbid();

        var mode = req.Mode != null ? ParseMode(req.Mode) : q.Mode;
        int needed = mode == QueueMode.Singles ? 2 : 4;
        if (distinctIds.Count != needed)
            return BadRequest(new { message = $"Exactly {needed} players required for {mode}" });

        var activeEntries = q.Entries.Where(e => distinctIds.Contains(e.PlayerId) && e.IsActive).ToList();
        if (activeEntries.Count != needed)
            return BadRequest(new { message = "One or more selected players are not in the active queue" });

        var playerIds = activeEntries.Select(e => e.PlayerId).ToList();
        var players = await _db.Players.Where(p => playerIds.Contains(p.Id) && p.OwnerUserId == userId).ToDictionaryAsync(p => p.Id, p => p);

        var match = new Match
        {
            QueueId = queueId,
            Mode = mode,
            Status = MatchStatus.Ongoing,
            StartTime = DateTime.UtcNow,
            ScoreText = req.ScoreText
        };
        _db.Matches.Add(match);
        await _db.SaveChangesAsync();

        foreach (var e in activeEntries.OrderBy(e => e.Position))
        {
            e.IsActive = false;
            if (players.TryGetValue(e.PlayerId, out var p)) p.GamesPlayed += 1;
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

        return Ok(new { matchId = match.Id, message = "Manual match started" });
    }

    [HttpPost("{queueId}/finish-match")]
    public async Task<IActionResult> FinishMatch(int queueId, [FromBody] FinishMatchRequest req)
    {
        var userId = CurrentUserId();
        if (userId == null) return Unauthorized();
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var match = await _db.Matches
            .Include(m => m.Queue)
            .FirstOrDefaultAsync(m => m.Id == req.MatchId && m.QueueId == queueId);
        if (match == null) return NotFound(new { message = "Match not found" });
        if (!await CanManageQueue(match.Queue!, userId.Value)) return Forbid();
        if (match.Status != MatchStatus.Ongoing) return BadRequest(new { message = "Match not ongoing" });

        var queue = await _db.Queues.Include(q => q.Entries).FirstOrDefaultAsync(q => q.Id == queueId);
        if (queue == null) return NotFound(new { message = "Queue not found" });

        var matchPlayers = await _db.MatchPlayers.Where(mp => mp.MatchId == match.Id).ToListAsync();
        var playerIds = matchPlayers.Select(mp => mp.PlayerId).ToList();
        var players = await _db.Players.Where(p => playerIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p);

        // Validate winner is part of the match
        if (!players.ContainsKey(req.WinnerId))
        {
            return BadRequest(new { message = "Winner must be part of the match" });
        }

        // Validate sets structure
        if (req.Sets == null || req.Sets.Count == 0 || req.Sets.Count > 3)
        {
            return BadRequest(new { message = "Provide 1 to 3 sets with scores" });
        }

        foreach (var (set, idx) in req.Sets.Select((s, i) => (s, i)))
        {
            if (set == null) return BadRequest(new { message = $"Set {idx + 1} is required" });
            if (set.A < 0 || set.B < 0) return BadRequest(new { message = $"Set {idx + 1} scores must be non-negative" });
            if (set.A == 0 && set.B == 0) return BadRequest(new { message = $"Set {idx + 1} cannot be 0-0" });
        }

        // For bo3 we expect at least 2 sets; for single we accept 1 set
        var minSets = match.Mode == QueueMode.Singles || match.Mode == QueueMode.Doubles ? 1 : 1;
        if (req.Sets.Count < minSets)
        {
            return BadRequest(new { message = $"At least {minSets} set(s) required" });
        }

        var winnerName = players.TryGetValue(req.WinnerId, out var win) ? win.DisplayName : $"Player {req.WinnerId}";
        var setText = string.Join(", ", req.Sets.Select((s, i) => $"Set {i + 1}: {s.A}-{s.B}"));
        var scoreText = $"Winner: {winnerName} | {setText}";

        match.Status = MatchStatus.Finished;
        match.FinishTime = DateTime.UtcNow;
        match.ScoreText = scoreText;

        var activeEntries = queue.Entries.Where(e => e.IsActive).ToList();
        var nextPos = activeEntries.Any() ? activeEntries.Max(e => e.Position) : 0;

        foreach (var mp in matchPlayers)
        {
            var entry = queue.Entries.FirstOrDefault(e => e.PlayerId == mp.PlayerId);
            if (entry == null)
            {
                entry = new QueueEntry
                {
                    QueueId = queue.Id,
                    PlayerId = mp.PlayerId
                };
                _db.QueueEntries.Add(entry);
                queue.Entries.Add(entry);
            }

            entry.IsActive = true;
            entry.EnqueuedAt = DateTime.UtcNow;
            entry.Position = ++nextPos;
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Match finished", durationSeconds = match.DurationSeconds });
    }
}
