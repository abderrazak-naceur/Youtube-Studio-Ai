using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;
using YoutubeStudio.Api.Services.Auth;

namespace YoutubeStudio.Api.Controllers;

/// <summary>
/// Self-issued authentication for the MVP: register/login with a password, returning a
/// short-lived JWT access token. Swappable for OIDC later (AUTHENTICATION.md).
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(
    YoutubeStudioDbContext db,
    IPasswordHasher passwordHasher,
    IJwtTokenService tokenService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return ValidationProblem("A valid email is required.");
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            return ValidationProblem("Password must be at least 8 characters.");
        if (await db.Users.AnyAsync(x => x.Email == email, cancellationToken))
            return Conflict("An account with this email already exists.");

        var user = new User
        {
            Email = email,
            PasswordHash = passwordHasher.Hash(request.Password),
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? null : request.DisplayName.Trim()
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        var (token, expiresAt) = tokenService.CreateAccessToken(user);
        return Ok(new AuthResponse(user.Id, user.Email, token, expiresAt));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Password))
            return ValidationProblem("Email and password are required.");

        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == email, cancellationToken);
        // Verify even when the user is missing to avoid leaking which emails exist.
        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
            return Unauthorized();

        var (token, expiresAt) = tokenService.CreateAccessToken(user);
        return Ok(new AuthResponse(user.Id, user.Email, token, expiresAt));
    }
}

public sealed record RegisterRequest(string Email, string Password, string? DisplayName);
public sealed record LoginRequest(string Email, string Password);
public sealed record AuthResponse(Guid UserId, string Email, string AccessToken, DateTime ExpiresAtUtc);
