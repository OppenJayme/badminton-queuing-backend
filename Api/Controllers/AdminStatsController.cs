using Api.Data;
using Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/admin/stats")]
[Authorize(Roles = "Admin")]
public class AdminStatsController : ControllerBase
{
    private readonly AppDbContext _db;
    public AdminStatsController(AppDbContext db) { _db = db; }

    // helper: parse date range, default last 7 days (UTC)
    private static (DateTime fromUtc, DateTime toUtc) ParseRange(string? from, string? to)
    {
        var toUtc   = string.IsNullOrWhiteSpace(to)   ? DateTime.UtcNow : DateTime.SpecifyKind(DateTime.Parse(to), DateTimeKind.Utc);
        var fromUtc = string.IsNullOrWhiteSpace(from) ? toUtc.AddDays(-7) : DateTime.SpecifyKind(DateTime.Parse(from), DateTimeKind.Utc);
        return (fromUtc, toUtc);
    }

    // -------- 1) Summary --------
    // GET /api/admin/stats/summary?from=YYYY-MM-DD&to=YYYY-MM-DD
    [HttpGet("summary")]
    public async Task<IActionResult> Summary([FromQuery] string? from, [FromQuery] string? to)
    {
        var (fromUtc, toUtc) = ParseRange(from, to);

        var totalUsers = await _db.Users.CountAsync(u => !u.IsSoftDeleted);
        var newUsers   = await _db.Users.CountAsync(u => !u.IsSoftDeleted && u.CreatedAt >= fromUtc && u.CreatedAt < toUtc);
        var totalMatches = await _db.Matches.CountAsync(m => m.Status == MatchStatus.Finished && m.StartTime >= fromUtc && m.StartTime < toUtc);
        var activeCourtsNow = await _db.Matches.Where(m => m.Status == MatchStatus.Ongoing).Select(m => m.CourtId).Distinct().CountAsync();

        // avg wait mins: compute from matches that started in range
        var waitsQuery =
            from mp in _db.MatchPlayers
            join m in _db.Matches on mp.MatchId equals m.Id
            where m.StartTime != null && m.StartTime >= fromUtc && m.StartTime < toUtc
            select EF.Functions.DateDiffSecond(mp.EnqueuedAtSnapshot, m.StartTime!.Value);

        // EF.Functions.DateDiffSecond is provider-specific; if null, fallback by materializing:
        double avgWaitSeconds;
        try
        {
            avgWaitSeconds = await waitsQuery.AverageAsync();
        }
        catch
        {
            var waits = await (
                from mp in _db.MatchPlayers
                join m in _db.Matches on mp.MatchId equals m.Id
                where m.StartTime != null && m.StartTime >= fromUtc && m.StartTime < toUtc
                select (m.StartTime!.Value - mp.EnqueuedAtSnapshot).TotalSeconds
            ).ToListAsync();
            avgWaitSeconds = waits.Count == 0 ? 0 : waits.Average();
        }

        var peakHour = await _db.Matches
            .Where(m => m.StartTime != null && m.StartTime >= fromUtc && m.StartTime < toUtc)
            .GroupBy(m => m.StartTime!.Value.Hour)
            .Select(g => new { Hour = g.Key, Cnt = g.Count() })
            .OrderByDescending(x => x.Cnt).ThenBy(x => x.Hour)
            .FirstOrDefaultAsync();

        return Ok(new
        {
            totalUsers,
            newUsers,
            totalMatches,
            activeCourtsNow,
            avgWaitMinutes = Math.Round(avgWaitSeconds / 60.0, 2),
            peakHour = peakHour?.Hour is int h ? $"{h:00}:00" : null
        });
    }

    // -------- 2) Courts usage --------
    // GET /api/admin/stats/courts-usage?from=...&to=...&locationId=1
    [HttpGet("courts-usage")]
    public async Task<IActionResult> CourtsUsage([FromQuery] string? from, [FromQuery] string? to, [FromQuery] int? locationId)
    {
        var (fromUtc, toUtc) = ParseRange(from, to);

        var q = _db.Matches
            .Include(m => m.Court)
            .Where(m => m.StartTime != null && m.StartTime >= fromUtc && m.StartTime < toUtc);

        if (locationId is int loc)
            q = q.Where(m => m.Court!.LocationId == loc);

        var now = DateTime.UtcNow;

        var data = await q
            .GroupBy(m => new { m.CourtId, m.Court!.CourtNumber })
            .Select(g => new
            {
                g.Key.CourtId,
                g.Key.CourtNumber,
                matches = g.Count(),
                avgMatchMinutes = g.Average(m => (double)((m.FinishTime ?? now) - m.StartTime!.Value).TotalMinutes),
                // utilization: time with ongoing match / time window length (rough, without venue open hours)
                busyMinutes = g.Sum(m => ((m.FinishTime ?? now) - m.StartTime!.Value).TotalMinutes)
            })
            .ToListAsync();

        var windowMinutes = (toUtc - fromUtc).TotalMinutes;
        var result = data.Select(d => new
        {
            d.CourtId,
            courtNumber = d.CourtNumber,
            matches = d.matches,
            avgMatchMinutes = Math.Round(d.avgMatchMinutes, 2),
            utilizationPercent = windowMinutes <= 0 ? 0 : Math.Round(Math.Min(100, 100.0 * d.busyMinutes / windowMinutes), 1)
        });

        return Ok(result);
    }

    // -------- 3) Matches time-series --------
    // GET /api/admin/stats/matches-timeseries?from=...&to=...&bucket=day|hour
    [HttpGet("matches-timeseries")]
    public async Task<IActionResult> MatchesTimeSeries([FromQuery] string? from, [FromQuery] string? to, [FromQuery] string bucket = "day")
    {
        var (fromUtc, toUtc) = ParseRange(from, to);
        bucket = (bucket ?? "day").ToLowerInvariant();

        if (bucket == "hour")
        {
            var hourly = await _db.Matches
                .Where(m => m.StartTime != null && m.StartTime >= fromUtc && m.StartTime < toUtc)
                .GroupBy(m => new { m.StartTime!.Value.Year, m.StartTime!.Value.Month, m.StartTime!.Value.Day, m.StartTime!.Value.Hour })
                .Select(g => new { ts = new DateTime(g.Key.Year, g.Key.Month, g.Key.Day, g.Key.Hour, 0, 0, DateTimeKind.Utc), matches = g.Count() })
                .OrderBy(x => x.ts)
                .ToListAsync();
            return Ok(hourly);
        }
        else
        {
            var daily = await _db.Matches
                .Where(m => m.StartTime != null && m.StartTime >= fromUtc && m.StartTime < toUtc)
                .GroupBy(m => new { m.StartTime!.Value.Year, m.StartTime!.Value.Month, m.StartTime!.Value.Day })
                .Select(g => new { ts = new DateTime(g.Key.Year, g.Key.Month, g.Key.Day, 0, 0, 0, DateTimeKind.Utc), matches = g.Count() })
                .OrderBy(x => x.ts)
                .ToListAsync();
            return Ok(daily);
        }
    }

    // -------- 4) Queue wait times --------
    // GET /api/admin/stats/wait-times?from=...&to=...
    [HttpGet("wait-times")]
    public async Task<IActionResult> WaitTimes([FromQuery] string? from, [FromQuery] string? to)
    {
        var (fromUtc, toUtc) = ParseRange(from, to);

        var waits = await (
            from mp in _db.MatchPlayers
            join m in _db.Matches on mp.MatchId equals m.Id
            where m.StartTime != null && m.StartTime >= fromUtc && m.StartTime < toUtc
            select (m.StartTime!.Value - mp.EnqueuedAtSnapshot).TotalSeconds
        ).ToListAsync();

        if (waits.Count == 0) return Ok(new
        {
            overall = new { avg = 0, median = 0, p90 = 0 },
            byMode = new { Singles = new { avg = 0 }, Doubles = new { avg = 0 } }
        });

        double Avg(List<double> xs) => xs.Average();
        double Median(List<double> xs) { xs.Sort(); int n = xs.Count; return n % 2 == 1 ? xs[n/2] : (xs[n/2 - 1] + xs[n/2]) / 2.0; }
        double P(List<double> xs, double p) { xs.Sort(); var idx = Math.Ceiling(p * xs.Count) - 1; idx = Math.Clamp(idx, 0, xs.Count - 1); return xs[(int)idx]; }

        var overall = new { avg = Avg(waits), median = Median(waits), p90 = P(waits, 0.90) };

        // by mode
        var singles = await (
            from mp in _db.MatchPlayers
            join m in _db.Matches on mp.MatchId equals m.Id
            where m.Mode == QueueMode.Singles && m.StartTime != null && m.StartTime >= fromUtc && m.StartTime < toUtc
            select (m.StartTime!.Value - mp.EnqueuedAtSnapshot).TotalSeconds
        ).ToListAsync();

        var doubles = await (
            from mp in _db.MatchPlayers
            join m in _db.Matches on mp.MatchId equals m.Id
            where m.Mode == QueueMode.Doubles && m.StartTime != null && m.StartTime >= fromUtc && m.StartTime < toUtc
            select (m.StartTime!.Value - mp.EnqueuedAtSnapshot).TotalSeconds
        ).ToListAsync();

        var result = new
        {
            overall = new { avg = Math.Round(overall.avg, 1), median = Math.Round(overall.median, 1), p90 = Math.Round(overall.p90, 1) },
            byMode = new
            {
                Singles = new { avg = singles.Count == 0 ? 0 : Math.Round(singles.Average(), 1) },
                Doubles = new { avg = doubles.Count == 0 ? 0 : Math.Round(doubles.Average(), 1) }
            }
        };
        return Ok(result);
    }

    // -------- 5) Recent activity --------
    // GET /api/admin/stats/recent-activity?limit=50
    [HttpGet("recent-activity")]
    public async Task<IActionResult> RecentActivity([FromQuery] int limit = 50)
    {
        limit = Math.Clamp(limit, 1, 200);

        var items = await _db.Matches
            .OrderByDescending(m => m.StartTime ?? m.FinishTime ?? DateTime.MinValue)
            .Take(limit)
            .Select(m => new
            {
                ts = (m.FinishTime ?? m.StartTime),
                action = m.Status == MatchStatus.Finished ? "MATCH_FINISH" :
                         m.Status == MatchStatus.Ongoing ? "MATCH_START" :
                         "MATCH",
                courtId = m.CourtId,
                mode = m.Mode.ToString(),
                score = m.ScoreText
            })
            .ToListAsync();

        return Ok(items);
    }
}
