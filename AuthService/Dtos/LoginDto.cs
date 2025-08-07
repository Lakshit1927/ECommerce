using System.ComponentModel.DataAnnotations;

namespace AuthService.Dtos;

public record LoginUserDto
{
    [Required]
    public string Username { get; init; } = string.Empty;
    
    [Required]
    public string Password { get; init; } = string.Empty;
}
