using Api.Data;
using Api.Domain;
using Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminCourtsController : ControllerBase
{
    private readonly AppDbContext _db;
    public AdminCourtsController(AppDbContext db) { _db = db; }

    // Create a court under a location
    // POST /api/admin/locations/{locationId}/courts
    [HttpPost("locations/{locationId}/courts")]
    public async Task<IActionResult> CreateCourt(int locationId, [FromBody] CreateCourtRequest req)
    {
        var loc = await _db.Locations.FindAsync(locationId);
        if (loc == null || !loc.IsActive) return NotFound(new { message = "Location not found or inactive" });

        // enforce unique (LocationId, CourtNumber)
        var exists = await _db.Courts.AnyAsync(c => c.LocationId == locationId && c.CourtNumber == req.CourtNumber);
        if (exists) return Conflict(new { message = "Court number already exists for this location" });

        var court = new Court { LocationId = locationId, CourtNumber = req.CourtNumber, Name = req.Name, IsActive = req.IsActive };
        _db.Courts.Add(court);
        await _db.SaveChangesAsync();
        return Ok(court);
    }

    // Update a court
    // PUT /api/admin/courts/{courtId}
    [HttpPut("courts/{courtId}")]
    public async Task<IActionResult> UpdateCourt(int courtId, [FromBody] UpdateCourtRequest req)
    {
        var c = await _db.Courts.FindAsync(courtId);
        if (c == null) return NotFound(new { message = "Court not found" });

        if (req.CourtNumber.HasValue)
        {
            var exists = await _db.Courts.AnyAsync(x => x.LocationId == c.LocationId && x.CourtNumber == req.CourtNumber && x.Id != c.Id);
            if (exists) return Conflict(new { message = "Court number already exists for this location" });
            c.CourtNumber = req.CourtNumber.Value;
        }
        if (req.Name != null) c.Name = req.Name;
        if (req.IsActive.HasValue) c.IsActive = req.IsActive.Value;

        await _db.SaveChangesAsync();
        return Ok(c);
    }
}
