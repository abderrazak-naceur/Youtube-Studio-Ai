namespace YoutubeStudio.Api.Services.Resilience;

public sealed record ResilienceOptions(
    int MaxAttempts = 3,
    TimeSpan? PerAttemptTimeout = null,
    TimeSpan? BaseDelay = null)
{
    public static ResilienceOptions Default { get; } = new(
        MaxAttempts: 3,
        PerAttemptTimeout: TimeSpan.FromSeconds(30),
        BaseDelay: TimeSpan.FromMilliseconds(200));
}

/// <summary>
/// Retries a transient operation with exponential backoff and a per-attempt timeout
/// (SYSTEM-ARCHITECTURE §8: retry transient provider failures, use timeouts). Non-transient
/// exceptions are not retried. Delays are supplied by an injectable delay function so tests
/// run without real waiting.
/// </summary>
public static class ResiliencePolicy
{
    public static async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        Func<Exception, bool> isTransient,
        ResilienceOptions? options = null,
        CancellationToken cancellationToken = default,
        Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        options ??= ResilienceOptions.Default;
        delay ??= Task.Delay;
        var maxAttempts = Math.Max(1, options.MaxAttempts);
        var perAttemptTimeout = options.PerAttemptTimeout ?? TimeSpan.FromSeconds(30);
        var baseDelay = options.BaseDelay ?? TimeSpan.FromMilliseconds(200);

        Exception? last = null;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(perAttemptTimeout);
            try
            {
                return await operation(timeoutCts.Token);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw; // caller cancelled — do not retry
            }
            catch (Exception exception) when (IsRetryable(exception, isTransient, cancellationToken) && attempt < maxAttempts)
            {
                last = exception;
                var backoff = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
                await delay(backoff, cancellationToken);
            }
        }

        throw last ?? new InvalidOperationException("Resilience policy exhausted without an exception.");
    }

    private static bool IsRetryable(Exception exception, Func<Exception, bool> isTransient, CancellationToken cancellationToken)
    {
        // A per-attempt timeout surfaces as OperationCanceledException without the caller
        // cancelling; treat that as transient and retryable.
        if (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested) return true;
        return isTransient(exception);
    }
}
