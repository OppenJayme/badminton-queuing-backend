using Api.Data;
using Api.Dtos;
using Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/players")]
[Authorize]
public class PlayersController : ControllerBase
{
    private readonly AppDbContext _db;
    public PlayersController(AppDbContext db) { _db = db; }

    [HttpGet]
    public async Task<IActionResult> GetPlayers()
    {
        var list = await _db.Players
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
            UserId = req.UserId
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
}
