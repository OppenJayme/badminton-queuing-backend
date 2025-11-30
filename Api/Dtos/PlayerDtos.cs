using System.ComponentModel.DataAnnotations;

namespace Api.Dtos;

public class CreatePlayerRequest
{
    [Required, MaxLength(200)]
    public string DisplayName { get; set; } = "";
    public bool IsRegistered { get; set; } = false;
    public int? UserId { get; set; }
}

public class PlayerDto
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = "";
    public bool IsRegistered { get; set; }
    public int GamesPlayed { get; set; }
    public int? UserId { get; set; }
}
