namespace Api.Dtos;

public class LoginRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public class LoginResponse
{
    public string Token { get; set; } = "";
    public string Role { get; set; } = "";
    public string DisplayName { get; set; } = "";
}

public class RegisterRequest
{
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Password { get; set; } = "";
}

public class RegisterResponse
{
    public int UserId { get; set; }
    public string Token { get; set; } = "";
    public string Role { get; set; } = "";
    public string DisplayName { get; set; } = "";
}
