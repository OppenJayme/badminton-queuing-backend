namespace Api.Domain;

public class GuestSession
{
    public int Id { get; set; }
    public string TempName { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
}
