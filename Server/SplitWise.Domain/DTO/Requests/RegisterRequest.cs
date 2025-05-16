namespace SplitWise.Domain.DTO.Requests;

public class RegisterRequest
{
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Phone { get; set; } = null!;
    public string Password { get; set; } = null!;
}
