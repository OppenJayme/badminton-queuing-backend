namespace Api.Dtos;

public class CreateLocationRequest
{
    public string Name { get; set; } = default!;
    public string City { get; set; } = "Cebu City";
    public string? Address { get; set; }
}

public class UpdateLocationRequest
{
    public string? Name { get; set; }
    public string? City { get; set; }
    public string? Address { get; set; }
    public bool? IsActive { get; set; }
}

public class CreateCourtRequest
{
    public int CourtNumber { get; set; }
    public string? Name { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateCourtRequest
{
    public int? CourtNumber { get; set; }
    public string? Name { get; set; }
    public bool? IsActive { get; set; }
}
