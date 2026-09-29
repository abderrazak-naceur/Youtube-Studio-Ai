using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YoutubeStudio.Api.Controllers;
using YoutubeStudio.Api.Data;
using YoutubeStudio.Api.Models;
using YoutubeStudio.Api.Services.Auth;

namespace YoutubeStudio.Api.Tests;

public sealed class AuthControllerTests
{
    [Fact]
    public async Task Register_creates_user_and_returns_token()
    {
        await using var db = CreateDb();
        var controller = CreateController(db);

        var result = await controller.Register(new RegisterRequest("Creator@Example.com", "password123", "Creator"), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AuthResponse>(ok.Value);
        Assert.Equal("creator@example.com", response.Email);
        Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));
        Assert.Single(db.Users);
        Assert.NotEqual("password123", db.Users.Single().PasswordHash);
    }

    [Fact]
    public async Task Register_rejects_short_password()
    {
        await using var db = CreateDb();
        var result = await CreateController(db).Register(new RegisterRequest("a@b.com", "short", null), CancellationToken.None);

        Assert.IsType<ObjectResult>(result.Result);
        Assert.Empty(db.Users);
    }

    [Fact]
    public async Task Register_rejects_duplicate_email()
    {
        await using var db = CreateDb();
        await CreateController(db).Register(new RegisterRequest("dup@example.com", "password123", null), CancellationToken.None);

        var result = await CreateController(db).Register(new RegisterRequest("dup@example.com", "password123", null), CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task Login_succeeds_with_correct_password()
    {
        await using var db = CreateDb();
        await CreateController(db).Register(new RegisterRequest("user@example.com", "password123", null), CancellationToken.None);

        var result = await CreateController(db).Login(new LoginRequest("user@example.com", "password123"), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AuthResponse>(ok.Value);
        Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));
    }

    [Fact]
    public async Task Login_fails_with_wrong_password()
    {
        await using var db = CreateDb();
        await CreateController(db).Register(new RegisterRequest("user@example.com", "password123", null), CancellationToken.None);

        var result = await CreateController(db).Login(new LoginRequest("user@example.com", "wrongpassword"), CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task Login_fails_for_unknown_email()
    {
        await using var db = CreateDb();
        var result = await CreateController(db).Login(new LoginRequest("nobody@example.com", "password123"), CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    private static AuthController CreateController(YoutubeStudioDbContext db)
    {
        var options = new JwtOptions { SigningKey = "test-signing-key-that-is-long-enough-for-hmacsha256" };
        return new AuthController(db, new PasswordHasher(), new JwtTokenService(options));
    }

    private static YoutubeStudioDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<YoutubeStudioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YoutubeStudioDbContext(opts);
    }
}
