using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AuthService.Dtos;
using AuthService.Models;
using AuthService.Services;
using System.ComponentModel.DataAnnotations;

namespace AuthService.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var auth = app.MapGroup("/auth")
            .WithTags("Authentication")
            .WithOpenApi();

        auth.MapPost("/register", RegisterAsync)
            .WithName("Register")
            .WithSummary("Register a new user")
            .Produces<object>(200)
            .Produces<ValidationProblemDetails>(400)
            .Produces<object>(409);

        auth.MapPost("/login", LoginAsync)
            .WithName("Login")
            .WithSummary("Authenticate user and get JWT token")
            .Produces<object>(200)
            .Produces<ValidationProblemDetails>(400)
            .Produces(401);
    }

    private static async Task<IResult> RegisterAsync(
        [FromBody] RegisterUserDto dto,
        IUserRepository userRepository,
        IPasswordHasher<User> hasher,
        ILogger<Program> logger)
    {
        try
        {
            // Check if username already exists
            if (await userRepository.UsernameExistsAsync(dto.Username))
            {
                logger.LogWarning("Registration attempt with existing username: {Username}", dto.Username);
                return Results.Conflict(new { error = "Username already exists" });
            }

            // Check if email already exists
            if (await userRepository.EmailExistsAsync(dto.Email))
            {
                logger.LogWarning("Registration attempt with existing email: {Email}", dto.Email);
                return Results.Conflict(new { error = "Email already exists" });
            }

            // Create new user
            var user = new User
            {
                Username = dto.Username.Trim(),
                Email = dto.Email.Trim().ToLowerInvariant(),
                CreatedAt = DateTime.UtcNow
            };

            // Hash password
            user.PasswordHash = hasher.HashPassword(user, dto.Password);

            // Save user
            await userRepository.CreateAsync(user);

            logger.LogInformation("User registered successfully: {Username}", user.Username);

            return Results.Ok(new
            {
                message = "User registered successfully",
                userId = user.Id,
                username = user.Username
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during user registration for username: {Username}", dto.Username);
            return Results.Problem("An error occurred during registration");
        }
    }

    private static async Task<IResult> LoginAsync(
        [FromBody] LoginUserDto dto,
        IUserRepository userRepository,
        IPasswordHasher<User> hasher,
        TokenService tokenService,
        ILogger<Program> logger)
    {
        try
        {
            // Get user by username
            var user = await userRepository.GetByUsernameAsync(dto.Username);
            if (user == null)
            {
                logger.LogWarning("Login attempt with non-existent username: {Username}", dto.Username);
                return Results.Unauthorized();
            }

            // Verify password
            var result = hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (result == PasswordVerificationResult.Failed)
            {
                logger.LogWarning("Failed login attempt for username: {Username}", dto.Username);
                return Results.Unauthorized();
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await userRepository.UpdateAsync(user);

            // Generate JWT token
            var token = tokenService.CreateToken(user);

            logger.LogInformation("Successful login for username: {Username}", user.Username);

            return Results.Ok(new
            {
                token,
                user = new
                {
                    id = user.Id,
                    username = user.Username,
                    email = user.Email
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during login for username: {Username}", dto.Username);
            return Results.Problem("An error occurred during login");
        }
    }
}
