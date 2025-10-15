namespace Api.Domain;

public enum Role { Admin, QueueMaster, Player }

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public Role Role { get; set; } = Role.Player;
    public bool IsSoftDeleted { get; set; }
    public bool HideName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
