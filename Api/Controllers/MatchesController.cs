using Api.Data;
using Api.Domain;
using Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/matches")]
public class MatchesController : ControllerBase
{
    private readonly AppDbContext _db;
    public MatchesController(AppDbContext db) { _db = db; }

    // ========== Start Match ==========
    [HttpPost("start")]
    [Authorize(Roles = "QueueMaster,Admin")]
    public async Task<IActionResult> StartMatch([FromBody] StartMatchRequest req)
    {
        var mode = Enum.TryParse<QueueMode>(req.Mode, true, out var parsedMode) ? parsedMode : QueueMode.Singles;

        var queue = await _db.Queues
            .Include(q => q.Entries.Where(e => e.IsActive))
            .FirstOrDefaultAsync(q => q.CourtId == req.CourtId && q.Mode == mode);

        if (queue == null || !queue.IsOpen)
            return BadRequest(new { message = "Queue is closed or missing" });

        int needed = mode == QueueMode.Singles ? 2 : 4;
        var ready = queue.Entries.OrderBy(e => e.Position).Take(needed).ToList();

        if (ready.Count < needed)
            return BadRequest(new { message = $"Not enough players for {mode}" });

        var match = new Match
        {
            CourtId = req.CourtId,
            Mode = mode,
            Status = MatchStatus.Ongoing,
            StartTime = DateTime.UtcNow,
            PlayersCsv = string.Join(",", ready.Select(r => r.UserId?.ToString() ?? $"G{r.GuestSessionId}"))
        };

        // mark entries inactive
        foreach (var e in ready) e.IsActive = false;

        _db.Matches.Add(match);
        await _db.SaveChangesAsync(); // ensures match.Id is generated

        // NEW: write MatchPlayer snapshots
        foreach (var e in ready)
        {
            _db.MatchPlayers.Add(new MatchPlayer
            {
                MatchId = match.Id,
                UserId = e.UserId,
                GuestSessionId = e.GuestSessionId,
                EnqueuedAtSnapshot = e.EnqueuedAt
            });
        }
        await _db.SaveChangesAsync();;

        // Repack remaining positions
        var remaining = queue.Entries.Where(e => e.IsActive).OrderBy(e => e.Position).ToList();
        for (int i = 0; i < remaining.Count; i++) remaining[i].Position = i + 1;
        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Match started",
            matchId = match.Id,
            court = req.CourtId,
            players = match.PlayersCsv
        });
    }

    // ========== Finish Match ==========
    [HttpPost("finish")]
    [Authorize(Roles = "QueueMaster,Admin")]
    public async Task<IActionResult> FinishMatch([FromBody] FinishMatchRequest req)
    {
        var match = await _db.Matches.FirstOrDefaultAsync(m => m.Id == req.MatchId);
        if (match == null) return NotFound(new { message = "Match not found" });
        if (match.Status != MatchStatus.Ongoing)
            return BadRequest(new { message = "Match not ongoing" });

        match.Status = MatchStatus.Finished;
        match.FinishTime = DateTime.UtcNow;
        match.ScoreText = req.ScoreText;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Match finished", match });
    }

    // ========== List all matches ==========
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAllMatches()
    {
        var list = await _db.Matches
            .Include(m => m.Court)
            .OrderByDescending(m => m.StartTime)
            .ToListAsync();

        return Ok(list);
    }
}
