using System.ComponentModel.DataAnnotations;

namespace Api.Dtos;

public class CreateSessionRequest
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    [MaxLength(500)]
    public string? Description { get; set; }
    public bool IsPublic { get; set; } = true;
}

public class SessionListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPublic { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public int Members { get; set; }
    public string? MyStatus { get; set; }
    public bool IsMine { get; set; }
}

public class SessionMemberDto
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "Joined";
    public string Role { get; set; } = "Member";
}

public class SessionDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPublic { get; set; }
    public int OwnerUserId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public List<SessionMemberDto> Members { get; set; } = new();
}

public class SessionCheckRequest
{
    [Required]
    public int UserId { get; set; }
}
