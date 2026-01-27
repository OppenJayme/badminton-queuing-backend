using Api.Data;
using Api.Domain;
using Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = nameof(Role.Admin))]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AdminController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<AdminUserSummary>>> GetUsers()
        {
            var users = await _db.Users
                .AsNoTracking()
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new AdminUserSummary
                {
                    Id = u.Id,
                    Email = u.Email,
                    DisplayName = u.DisplayName,
                    Role = u.Role.ToString(),
                    IsSoftDeleted = u.IsSoftDeleted,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();
            return Ok(users);
        }

        [HttpPost("users/{id}/soft-delete")]
        public async Task<IActionResult> SoftDeleteUser(int id)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();
            user.IsSoftDeleted = true;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("users/{id}/restore")]
        public async Task<IActionResult> RestoreUser(int id)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();
            user.IsSoftDeleted = false;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("totals")]
        public async Task<ActionResult<AdminTotalsResponse>> GetTotals()
        {
            var totals = new AdminTotalsResponse
            {
            TotalUsers = await _db.Users.CountAsync(u => !u.IsSoftDeleted),
            TotalPlayers = await _db.Players.CountAsync(),
            TotalQueues = await _db.Queues.CountAsync(),
            TotalSessions = await _db.Sessions.CountAsync(),
            TotalMatches = await _db.Matches.CountAsync()
        };
        return Ok(totals);
    }

    [HttpGet("history")]
    public async Task<ActionResult<IEnumerable<AdminMatchSummary>>> GetHistory([FromQuery] int take = 50, [FromQuery] int page = 1)
    {
        take = Math.Clamp(take, 1, 200);
        page = Math.Max(page, 1);
        var skip = (page - 1) * take;

        var data = await _db.Matches
            .AsNoTracking()
            .Include(m => m.Queue)
                .ThenInclude(q => q!.Session)
            .OrderByDescending(m => m.FinishTime ?? m.StartTime ?? DateTime.MinValue)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        var items = data.Select(m => new AdminMatchSummary
        {
            Id = m.Id,
            QueueId = m.QueueId,
            QueueName = m.Queue != null ? m.Queue.Name : $"Queue {m.QueueId}",
            SessionName = m.Queue != null
                ? (m.Queue.Session != null ? m.Queue.Session.Name : null)
                : null,
            OwnerUserId = m.Queue != null ? m.Queue.OwnerUserId : null,
            Mode = m.Mode.ToString(),
            Status = m.Status.ToString(),
            StartTime = m.StartTime,
            FinishTime = m.FinishTime,
            DurationSeconds = (m.StartTime != null && m.FinishTime != null)
                ? (m.FinishTime.Value - m.StartTime.Value).TotalSeconds
                : null,
            ScoreText = m.ScoreText
        }).ToList();

        return Ok(items);
    }
}
