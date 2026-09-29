using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using YoutubeStudio.Api.Models;

namespace YoutubeStudio.Api.Services.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "youtube-studio-ai";
    public string Audience { get; set; } = "youtube-studio-ai";
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 60;
}

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateAccessToken(User user);
}

/// <summary>
/// Issues short-lived signed JWT access tokens. Self-issued for the MVP; the same
/// contract can later front an external OIDC provider (AUTHENTICATION.md).
/// </summary>
public sealed class JwtTokenService(JwtOptions options) : IJwtTokenService
{
    public (string Token, DateTime ExpiresAtUtc) CreateAccessToken(User user)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(options.AccessTokenMinutes);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
