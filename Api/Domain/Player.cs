namespace Api.Domain;

public class Player
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public User? User { get; set; }
    public string DisplayName { get; set; } = default!;
    public bool IsRegistered { get; set; } = false;
    public int GamesPlayed { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
