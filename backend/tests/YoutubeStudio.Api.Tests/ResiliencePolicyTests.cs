using YoutubeStudio.Api.Services.Resilience;

namespace YoutubeStudio.Api.Tests;

public sealed class ResiliencePolicyTests
{
    private static readonly Func<TimeSpan, CancellationToken, Task> NoDelay = (_, _) => Task.CompletedTask;

    [Fact]
    public async Task Succeeds_on_first_attempt()
    {
        var calls = 0;
        var result = await ResiliencePolicy.ExecuteAsync(
            _ => { calls++; return Task.FromResult(42); },
            _ => true, ResilienceOptions.Default, CancellationToken.None, NoDelay);

        Assert.Equal(42, result);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Retries_transient_then_succeeds()
    {
        var calls = 0;
        var result = await ResiliencePolicy.ExecuteAsync<int>(
            _ =>
            {
                calls++;
                if (calls < 3) throw new InvalidOperationException("transient");
                return Task.FromResult(7);
            },
            _ => true,
            new ResilienceOptions(MaxAttempts: 3, PerAttemptTimeout: TimeSpan.FromSeconds(1), BaseDelay: TimeSpan.Zero),
            CancellationToken.None, NoDelay);

        Assert.Equal(7, result);
        Assert.Equal(3, calls);
    }

    [Fact]
    public async Task Does_not_retry_non_transient()
    {
        var calls = 0;
        await Assert.ThrowsAsync<InvalidOperationException>(() => ResiliencePolicy.ExecuteAsync<int>(
            _ => { calls++; throw new InvalidOperationException("permanent"); },
            _ => false,
            ResilienceOptions.Default, CancellationToken.None, NoDelay));

        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Throws_last_exception_after_exhausting_attempts()
    {
        var calls = 0;
        await Assert.ThrowsAsync<InvalidOperationException>(() => ResiliencePolicy.ExecuteAsync<int>(
            _ => { calls++; throw new InvalidOperationException("still failing"); },
            _ => true,
            new ResilienceOptions(MaxAttempts: 3, PerAttemptTimeout: TimeSpan.FromSeconds(1), BaseDelay: TimeSpan.Zero),
            CancellationToken.None, NoDelay));

        Assert.Equal(3, calls);
    }

    [Fact]
    public async Task Caller_cancellation_is_not_retried()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(() => ResiliencePolicy.ExecuteAsync<int>(
            token => Task.FromCanceled<int>(token),
            _ => true, ResilienceOptions.Default, cts.Token, NoDelay));
    }
}
