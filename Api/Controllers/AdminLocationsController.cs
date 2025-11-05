using Api.Data;
using Api.Domain;
using Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/admin/locations")]
[Authorize(Roles = "Admin")]
public class AdminLocationsController : ControllerBase
{
    private readonly AppDbContext _db;
    public AdminLocationsController(AppDbContext db) { _db = db; }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
    {
        var q = _db.Locations.AsQueryable();
        if (!includeInactive) q = q.Where(l => l.IsActive);
        var list = await q.OrderBy(l => l.City).ThenBy(l => l.Name).ToListAsync();
        return Ok(list);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLocationRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new { message = "Name is required" });

        var loc = new Location { Name = req.Name.Trim(), City = req.City.Trim(), Address = req.Address, IsActive = true };
        _db.Locations.Add(loc);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), new { id = loc.Id }, loc);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateLocationRequest req)
    {
        var loc = await _db.Locations.FindAsync(id);
        if (loc == null) return NotFound(new { message = "Location not found" });

        if (req.Name != null) loc.Name = req.Name.Trim();
        if (req.City != null) loc.City = req.City.Trim();
        if (req.Address != null) loc.Address = req.Address;
        if (req.IsActive.HasValue) loc.IsActive = req.IsActive.Value;

        await _db.SaveChangesAsync();
        return Ok(loc);
    }

    // Soft deactivate instead of hard delete
    [HttpDelete("{id}")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var loc = await _db.Locations.Include(l => l.Courts).FirstOrDefaultAsync(l => l.Id == id);
        if (loc == null) return NotFound(new { message = "Location not found" });

        loc.IsActive = false;
        foreach (var c in loc.Courts) c.IsActive = false; // cascade deactivate courts
        await _db.SaveChangesAsync();

        return Ok(new { message = "Location deactivated" });
    }
}
