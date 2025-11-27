using Api.Data;
using Api.Dtos;
using Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Api.Controllers;

[ApiController]
[Route("api/players")]
[Authorize]
public class PlayersController : ControllerBase
{
    private readonly AppDbContext _db;
    public PlayersController(AppDbContext db) { _db = db; }

    private int? CurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return int.TryParse(sub, out var id) ? id : (int?)null;
    }

    [HttpGet]
    public async Task<IActionResult> GetPlayers()
    {
        var userId = CurrentUserId();
        if (userId == null) return Unauthorized();

        var list = await _db.Players
            .Where(p => p.OwnerUserId == userId)
            .OrderBy(p => p.DisplayName)
            .Select(p => new PlayerDto
            {
                Id = p.Id,
                DisplayName = p.DisplayName,
                IsRegistered = p.IsRegistered,
                GamesPlayed = p.GamesPlayed
            })
            .ToListAsync();
        return Ok(list);
    }

    [HttpPost]
    public async Task<IActionResult> CreatePlayer([FromBody] CreatePlayerRequest req)
    {
        var userId = CurrentUserId();
        if (userId == null) return Unauthorized();
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        if (req.UserId.HasValue)
        {
            var userExists = await _db.Users.AnyAsync(u => u.Id == req.UserId.Value);
            if (!userExists) return BadRequest(new { message = "User not found" });
        }

        var p = new Player
        {
            DisplayName = req.DisplayName,
            IsRegistered = req.IsRegistered,
            UserId = req.UserId,
            OwnerUserId = userId
        };
        _db.Players.Add(p);
        await _db.SaveChangesAsync();

        return Ok(new PlayerDto
        {
            Id = p.Id,
            DisplayName = p.DisplayName,
            IsRegistered = p.IsRegistered,
            GamesPlayed = p.GamesPlayed
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePlayer(int id)
    {
        var userId = CurrentUserId();
        if (userId == null) return Unauthorized();

        var player = await _db.Players.FirstOrDefaultAsync(p => p.Id == id && p.OwnerUserId == userId);
        if (player == null) return NotFound(new { message = "Player not found" });

        // Remove queue entries for this player's owner queues
        var queueIds = await _db.Queues.Where(q => q.OwnerUserId == userId).Select(q => q.Id).ToListAsync();
        var entries = await _db.QueueEntries.Where(e => queueIds.Contains(e.QueueId) && e.PlayerId == id).ToListAsync();
        _db.QueueEntries.RemoveRange(entries);

        _db.Players.Remove(player);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Player removed", id });
    }
}
