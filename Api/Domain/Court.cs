namespace Api.Domain;

public class Court
{
    public int Id { get; set; }
    public int LocationId { get; set; }
    public Location? Location { get; set; }
    public int CourtNumber { get; set; }           // 1,2,3...
    public string? Name { get; set; }              // Optional label (e.g., "Court A")
    public bool IsActive { get; set; } = true;
}
