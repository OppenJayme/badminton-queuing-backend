using Api.Data;
using Api.Domain;
using Api.Dtos;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly JwtService _jwt;

    public AuthController(AppDbContext db, JwtService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == req.Email);
        if (user == null || user.IsSoftDeleted)
            return Unauthorized(new { message = "Invalid credentials" });

        if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid credentials" });

        var token = _jwt.CreateToken(user);
        return Ok(new LoginResponse { Token = token, Role = user.Role.ToString(), DisplayName = user.DisplayName });
    }
}
