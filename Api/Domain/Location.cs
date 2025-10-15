namespace Api.Domain;

public class Location
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
    public ICollection<Court> Courts { get; set; } = new List<Court>();
}
