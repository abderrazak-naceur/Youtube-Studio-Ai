using YoutubeStudio.Api.Services.Auth;

namespace YoutubeStudio.Api.Tests;

public sealed class PasswordHasherTests
{
    [Fact]
    public void Hash_is_verifiable()
    {
        var hasher = new PasswordHasher();
        var hash = hasher.Hash("correct horse battery staple");

        Assert.True(hasher.Verify("correct horse battery staple", hash));
    }

    [Fact]
    public void Verify_fails_for_wrong_password()
    {
        var hasher = new PasswordHasher();
        var hash = hasher.Hash("correct password");

        Assert.False(hasher.Verify("wrong password", hash));
    }

    [Fact]
    public void Hash_is_salted_and_differs_each_time()
    {
        var hasher = new PasswordHasher();
        Assert.NotEqual(hasher.Hash("same"), hasher.Hash("same"));
    }

    [Fact]
    public void Verify_returns_false_for_malformed_hash()
    {
        var hasher = new PasswordHasher();
        Assert.False(hasher.Verify("password", "not-a-valid-hash"));
    }
}
