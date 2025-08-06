using Microsoft.AspNetCore.Identity;
using AuthService.Dtos;
using AuthService.Models;
using AuthService.Services;

namespace AuthService.Endpoints;

public static class AuthEndpoints
{
    private static List<User> _users = new();

    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/register", (
            RegisterUserDto dto,
            IPasswordHasher<User> hasher) =>
        {
            if (_users.Any(u => u.Username == dto.Username))
                return Results.BadRequest(new { error = "Username already exists" });

            var user = new User { Username = dto.Username };
            user.PasswordHash = hasher.HashPassword(user, dto.Password);
            user.Id = _users.Count + 1;

            _users.Add(user);
            return Results.Ok(new { message = "User registered successfully" });
        });

        app.MapPost("/login", (
            LoginUserDto dto,
            IPasswordHasher<User> hasher,
            TokenService tokenService) =>
        {
            var user = _users.FirstOrDefault(u => u.Username == dto.Username);
            if (user is null)
                return Results.Unauthorized();

            var result = hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (result == PasswordVerificationResult.Failed)
                return Results.Unauthorized();

            var token = tokenService.CreateToken(user);
            return Results.Ok(new { token });
        });
    }
}
