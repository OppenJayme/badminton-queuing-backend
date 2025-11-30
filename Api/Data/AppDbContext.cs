using Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Queue> Queues => Set<Queue>();
    public DbSet<QueueEntry> QueueEntries => Set<QueueEntry>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<MatchPlayer> MatchPlayers => Set<MatchPlayer>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<SessionMember> SessionMembers => Set<SessionMember>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.UseCollation("utf8mb4_0900_ai_ci");
        b.HasCharSet("utf8mb4");

        b.Entity<Match>().ToTable("matches"); // avoid MySQL keyword
        b.Entity<MatchPlayer>().ToTable("match_players");

        b.Entity<MatchPlayer>()
            .HasIndex(mp => mp.MatchId);

        b.Entity<Player>()
            .HasIndex(p => new { p.OwnerUserId, p.UserId })
            .IsUnique();
        b.Entity<Player>()
            .HasIndex(p => p.OwnerUserId);

        b.Entity<Queue>()
            .HasIndex(q => q.OwnerUserId);

        b.Entity<QueueEntry>()
            .HasIndex(qe => new { qe.QueueId, qe.PlayerId })
            .IsUnique();

        b.Entity<Session>()
            .HasIndex(s => new { s.OwnerUserId, s.Name })
            .IsUnique();

        b.Entity<SessionMember>()
            .HasIndex(sm => new { sm.SessionId, sm.UserId })
            .IsUnique();

        // Stable seed values to avoid churn across migrations
        var adminHash = "$2a$11$vDD1rC1DcUjQCyxRVjVHg.lcdAf22V402D1f4nlsGn1l0LkBwrQ/a";
        var qmHash = "$2a$11$XTFE3C2SCF62mrQjcA7SMOyrjujVxz9cNbIWbPyiP0rom44bib8wm";
        var plHash = "$2a$11$8EgMvAL828k4cyBm9rVZPOM2o9qL4s8onIJOP1ZGlzOtJfsykBcUS";
        var adminCreatedAt = new DateTime(2025, 11, 21, 17, 31, 57, 897, DateTimeKind.Utc).AddTicks(3423);
        var qmCreatedAt = new DateTime(2025, 11, 21, 17, 31, 57, 897, DateTimeKind.Utc).AddTicks(6648);
        var playerCreatedAt = new DateTime(2025, 11, 21, 17, 31, 57, 897, DateTimeKind.Utc).AddTicks(6655);

        b.Entity<User>().HasData(
            new User { Id = 1, Email = "admin@example.com", DisplayName = "Admin", PasswordHash = adminHash, Role = Role.Admin, CreatedAt = adminCreatedAt },
            new User { Id = 2, Email = "qm@example.com", DisplayName = "QueueMaster", PasswordHash = qmHash, Role = Role.QueueMaster, CreatedAt = qmCreatedAt },
            new User { Id = 3, Email = "player@example.com", DisplayName = "Player One", PasswordHash = plHash, Role = Role.Player, CreatedAt = playerCreatedAt }
        );
    }
}
