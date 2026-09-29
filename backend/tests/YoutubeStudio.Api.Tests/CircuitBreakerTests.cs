using YoutubeStudio.Api.Services.Resilience;

namespace YoutubeStudio.Api.Tests;

public sealed class CircuitBreakerTests
{
    [Fact]
    public async Task Opens_after_threshold_consecutive_failures()
    {
        var breaker = new CircuitBreaker(failureThreshold: 2, openDuration: TimeSpan.FromSeconds(30));

        await FailOnce(breaker);
        Assert.Equal(CircuitState.Closed, breaker.State);
        await FailOnce(breaker);
        Assert.Equal(CircuitState.Open, breaker.State);
    }

    [Fact]
    public async Task Short_circuits_when_open()
    {
        var breaker = new CircuitBreaker(failureThreshold: 1);
        await FailOnce(breaker);

        var called = false;
        await Assert.ThrowsAsync<CircuitOpenException>(() => breaker.ExecuteAsync<int>(_ => { called = true; return Task.FromResult(1); }, CancellationToken.None));
        Assert.False(called); // operation not invoked
    }

    [Fact]
    public async Task Half_open_after_cooldown_then_closes_on_success()
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var breaker = new CircuitBreaker(failureThreshold: 1, openDuration: TimeSpan.FromSeconds(10), clock: () => now);
        await FailOnce(breaker);
        Assert.Equal(CircuitState.Open, breaker.State);

        now = now.AddSeconds(11); // cooldown elapsed
        Assert.Equal(CircuitState.HalfOpen, breaker.State);

        var result = await breaker.ExecuteAsync(_ => Task.FromResult(99), CancellationToken.None);
        Assert.Equal(99, result);
        Assert.Equal(CircuitState.Closed, breaker.State);
    }

    [Fact]
    public async Task Success_resets_failure_count()
    {
        var breaker = new CircuitBreaker(failureThreshold: 2);
        await FailOnce(breaker);
        await breaker.ExecuteAsync(_ => Task.FromResult(1), CancellationToken.None); // success resets
        await FailOnce(breaker);
        Assert.Equal(CircuitState.Closed, breaker.State); // only 1 failure since reset
    }

    private static async Task FailOnce(CircuitBreaker breaker)
    {
        try { await breaker.ExecuteAsync<int>(_ => throw new InvalidOperationException("boom"), CancellationToken.None); }
        catch (InvalidOperationException) { /* expected */ }
    }
}
