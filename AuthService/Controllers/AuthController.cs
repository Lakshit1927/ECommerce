using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using AuthService.Models;
using AuthService.Dtos;
using AuthService.Services;

namespace AuthService.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    // In-memory store (temporary; replace with DB later)
    private static List<User> _users = new();

    private readonly IPasswordHasher<User> _hasher;
    private readonly TokenService _tokenService;

    public AuthController(IPasswordHasher<User> hasher, TokenService tokenService)
    {
        _hasher = hasher;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    public IActionResult Register(RegisterUserDto dto)
    {
        if (_users.Any(u => u.Username == dto.Username))
        {
            return BadRequest("Username already exists");
        }

        var user = new User { Username = dto.Username };
        user.PasswordHash = _hasher.HashPassword(user, dto.Password);
        user.Id = _users.Count + 1;

        _users.Add(user);
        return Ok("User registered successfully");
    }

    [HttpPost("login")]
    public IActionResult Login(LoginUserDto dto)
    {
        var user = _users.FirstOrDefault(u => u.Username == dto.Username);
        if (user is null)
        {
            return Unauthorized("Invalid credentials");
        }

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            return Unauthorized("Invalid credentials");
        }

        var token = _tokenService.CreateToken(user);
        return Ok(new { token });
    }
}
