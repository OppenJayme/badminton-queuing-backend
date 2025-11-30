using Api.Data;
using Api.Domain;
using Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Api.Controllers;

[ApiController]
[Route("api/sessions")]
[Authorize]
public class SessionsController : ControllerBase
{
    private readonly AppDbContext _db;
    public SessionsController(AppDbContext db) { _db = db; }

    private int? CurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return int.TryParse(sub, out var id) ? id : (int?)null;
    }

    private async Task<bool> IsOwnerOrCoHost(int sessionId, int userId)
    {
        var session = await _db.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId);
        if (session == null) return false;
        if (session.OwnerUserId == userId) return true;
        var mem = await _db.SessionMembers.FirstOrDefaultAsync(m => m.SessionId == sessionId && m.UserId == userId);
        return mem != null && mem.Role == SessionMemberRole.CoHost;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? search = null)
    {
        var userId = CurrentUserId();
        if (userId == null) return Unauthorized();

        var query = _db.Sessions.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s => s.Name.Contains(search));
        }

        var sessions = await query.OrderBy(s => s.Name).Take(100).ToListAsync();
        var sessionIds = sessions.Select(s => s.Id).ToList();
        var memberCounts = await _db.SessionMembers
            .Where(m => sessionIds.Contains(m.SessionId))
            .GroupBy(m => m.SessionId)
            .ToDictionaryAsync(g => g.Key, g => g.Count());
        var owners = await _db.Users.Where(u => sessions.Select(s => s.OwnerUserId).Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName);
        var myMemberships = await _db.SessionMembers.Where(m => m.UserId == userId && sessionIds.Contains(m.SessionId))
            .ToDictionaryAsync(m => m.SessionId, m => m);

        var result = sessions.Select(s => new SessionListDto
        {
            Id = s.Id,
            Name = s.Name,
            Description = s.Description,
            IsPublic = s.IsPublic,
            OwnerName = owners.TryGetValue(s.OwnerUserId, out var n) ? n : "Owner",
            Members = memberCounts.TryGetValue(s.Id, out var c) ? c : 0,
            MyStatus = myMemberships.TryGetValue(s.Id, out var mem) ? mem.Status.ToString() : null,
            IsMine = s.OwnerUserId == userId
        });

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Detail(int id)
    {
        var userId = CurrentUserId();
        if (userId == null) return Unauthorized();

        var session = await _db.Sessions.FirstOrDefaultAsync(s => s.Id == id);
        if (session == null) return NotFound(new { message = "Session not found" });

        var membership = await _db.SessionMembers.FirstOrDefaultAsync(m => m.SessionId == id && m.UserId == userId);
        if (!session.IsPublic && session.OwnerUserId != userId && membership == null)
        {
            return Forbid();
        }

        var members = await _db.SessionMembers.Where(m => m.SessionId == id).ToListAsync();
        var userIds = members.Select(m => m.UserId).Append(session.OwnerUserId).Distinct().ToList();
        var users = await _db.Users.Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.DisplayName);

        var dto = new SessionDetailDto
        {
            Id = session.Id,
            Name = session.Name,
            Description = session.Description,
            IsPublic = session.IsPublic,
            OwnerUserId = session.OwnerUserId,
            OwnerName = users.TryGetValue(session.OwnerUserId, out var n) ? n : "Owner",
            Members = members.Select(m => new SessionMemberDto
            {
                UserId = m.UserId,
                Name = users.TryGetValue(m.UserId, out var dn) ? dn : $"User {m.UserId}",
                Status = m.Status.ToString(),
                Role = m.Role.ToString()
            }).ToList()
        };

        return Ok(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSessionRequest req)
    {
        var userId = CurrentUserId();
        if (userId == null) return Unauthorized();
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var exists = await _db.Sessions.AnyAsync(s => s.OwnerUserId == userId && s.Name == req.Name);
        if (exists) return BadRequest(new { message = "You already have a session with this name" });

        var session = new Session
        {
            Name = req.Name,
            Description = req.Description,
            IsPublic = req.IsPublic,
            OwnerUserId = userId.Value,
            CreatedAt = DateTime.UtcNow
        };
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();

        return Ok(new { session.Id, session.Name, session.Description, session.IsPublic });
    }

    [HttpPost("{id}/join")]
    public async Task<IActionResult> Join(int id)
    {
        var userId = CurrentUserId();
        if (userId == null) return Unauthorized();

        var session = await _db.Sessions.FirstOrDefaultAsync(s => s.Id == id);
        if (session == null) return NotFound(new { message = "Session not found" });

        var member = await _db.SessionMembers.FirstOrDefaultAsync(m => m.SessionId == id && m.UserId == userId);
        if (member != null) return Ok(new { message = "Already a member", status = member.Status.ToString() });

        _db.SessionMembers.Add(new SessionMember
        {
            SessionId = id,
            UserId = userId.Value,
            Status = SessionMemberStatus.Joined,
            Role = SessionMemberRole.Member,
            JoinedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return Ok(new { message = "Joined session" });
    }

    [HttpPost("{id}/leave")]
    public async Task<IActionResult> Leave(int id)
    {
        var userId = CurrentUserId();
        if (userId == null) return Unauthorized();

        var member = await _db.SessionMembers.FirstOrDefaultAsync(m => m.SessionId == id && m.UserId == userId);
        if (member == null) return Ok(new { message = "Not a member" });

        _db.SessionMembers.Remove(member);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Left session" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = CurrentUserId();
        if (userId == null) return Unauthorized();

        var session = await _db.Sessions.Include(s => s.Members).FirstOrDefaultAsync(s => s.Id == id);
        if (session == null) return NotFound(new { message = "Session not found" });
        if (session.OwnerUserId != userId) return Forbid();

        var queues = await _db.Queues.Where(q => q.SessionId == id).ToListAsync();
        foreach (var q in queues)
        {
            q.SessionId = null;
        }

        _db.SessionMembers.RemoveRange(session.Members);
        _db.Sessions.Remove(session);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Session deleted", id });
    }

    // Hyphenated route for consistency with client
    [HttpPost("{id}/check-in")]
    public async Task<IActionResult> CheckIn(int id, [FromBody] SessionCheckRequest req)
    {
        var userId = CurrentUserId();
        if (userId == null) return Unauthorized();
        if (!await IsOwnerOrCoHost(id, userId.Value)) return Forbid();

        var member = await _db.SessionMembers.FirstOrDefaultAsync(m => m.SessionId == id && m.UserId == req.UserId);
        if (member == null)
        {
            return NotFound(new { message = "Member not found" });
        }
        member.Status = SessionMemberStatus.CheckedIn;
        member.CheckedInAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Checked in" });
    }

    [HttpPost("{id}/check-out")]
    public async Task<IActionResult> CheckOut(int id, [FromBody] SessionCheckRequest req)
    {
        var userId = CurrentUserId();
        if (userId == null) return Unauthorized();
        if (!await IsOwnerOrCoHost(id, userId.Value)) return Forbid();

        var member = await _db.SessionMembers.FirstOrDefaultAsync(m => m.SessionId == id && m.UserId == req.UserId);
        if (member == null)
        {
            return NotFound(new { message = "Member not found" });
        }
        member.Status = SessionMemberStatus.Joined;
        member.CheckedInAt = null;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Checked out" });
    }

    [HttpPost("{id}/role")]
    public async Task<IActionResult> UpdateRole(int id, [FromBody] SessionCheckRequest req, [FromQuery] string role = "Member")
    {
        var userId = CurrentUserId();
        if (userId == null) return Unauthorized();
        var session = await _db.Sessions.FirstOrDefaultAsync(s => s.Id == id);
        if (session == null) return NotFound(new { message = "Session not found" });
        if (session.OwnerUserId != userId) return Forbid();
        var member = await _db.SessionMembers.FirstOrDefaultAsync(m => m.SessionId == id && m.UserId == req.UserId);
        if (member == null) return NotFound(new { message = "Member not found" });
        member.Role = Enum.TryParse<SessionMemberRole>(role, true, out var parsed) ? parsed : SessionMemberRole.Member;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Role updated", role = member.Role.ToString() });
    }
}
