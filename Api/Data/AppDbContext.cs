using Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Court> Courts => Set<Court>();
    public DbSet<Queue> Queues => Set<Queue>();
    public DbSet<QueueEntry> QueueEntries => Set<QueueEntry>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<GuestSession> GuestSessions => Set<GuestSession>();
    
    public DbSet<MatchPlayer> MatchPlayers => Set<MatchPlayer>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.UseCollation("utf8mb4_0900_ai_ci");
        b.HasCharSet("utf8mb4");

        b.Entity<Match>().ToTable("matches"); // avoid MySQL keyword
        b.Entity<MatchPlayer>().ToTable("match_players");

        b.Entity<MatchPlayer>()
            .HasIndex(mp => mp.MatchId);

        var adminHash = BCrypt.Net.BCrypt.HashPassword("Admin123!");
        var qmHash = BCrypt.Net.BCrypt.HashPassword("Qm123!");
        var plHash = BCrypt.Net.BCrypt.HashPassword("Player123!");

        b.Entity<User>().HasData(
            new User { Id = 1, Email = "admin@example.com", DisplayName = "Admin", PasswordHash = adminHash, Role = Role.Admin },
            new User { Id = 2, Email = "qm@example.com", DisplayName = "QueueMaster", PasswordHash = qmHash, Role = Role.QueueMaster },
            new User { Id = 3, Email = "player@example.com", DisplayName = "Player One", PasswordHash = plHash, Role = Role.Player }
        );

        b.Entity<Location>().HasData(new Location { Id = 1, Name = "SmashPoint Badminton Center", IsActive = true });
        b.Entity<Court>().HasData(
            new Court { Id = 1, LocationId = 1, CourtNumber = 1, IsActive = true },
            new Court { Id = 2, LocationId = 1, CourtNumber = 2, IsActive = true }
        );
    }
}
