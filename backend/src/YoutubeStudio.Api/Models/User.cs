namespace YoutubeStudio.Api.Models;

/// <summary>
/// An authenticated person (DATA-MODEL User entity). Credentials are stored as a salted
/// password hash; the identity layer can later be swapped for OIDC behind the same model.
/// </summary>
public sealed class User : Entity
{
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public string? DisplayName { get; set; }

    public ICollection<Membership> Memberships { get; set; } = [];
}
