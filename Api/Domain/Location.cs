namespace Api.Domain;

public class Location
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;   // Venue name (e.g., "Metro Sports")
    public string City { get; set; } = "Cebu City"; // City/Area (e.g., "Cebu City") // scale later
    public string? Address { get; set; }           // Optional address string
    public bool IsActive { get; set; } = true;
    public ICollection<Court> Courts { get; set; } = new List<Court>();
}
