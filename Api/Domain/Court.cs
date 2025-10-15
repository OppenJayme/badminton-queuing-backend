namespace Api.Domain;

public class Court
{
    public int Id { get; set; }
    public int LocationId { get; set; }
    public Location? Location { get; set; }
    public int CourtNumber { get; set; }
    public bool IsActive { get; set; } = true;
}
