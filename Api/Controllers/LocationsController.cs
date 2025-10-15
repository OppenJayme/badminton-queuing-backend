using Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/locations")]
public class LocationsController : ControllerBase
{
    private readonly AppDbContext _db;
    public LocationsController(AppDbContext db) { _db = db; }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetLocations()
    {
        var list = await _db.Locations.Where(l => l.IsActive).ToListAsync();
        return Ok(list);
    }

    [HttpGet("{locationId}/courts")]
    [Authorize]
    public async Task<IActionResult> GetCourts(int locationId)
    {
        var courts = await _db.Courts
            .Where(c => c.LocationId == locationId && c.IsActive)
            .OrderBy(c => c.CourtNumber)
            .ToListAsync();
        return Ok(courts);
    }
}
