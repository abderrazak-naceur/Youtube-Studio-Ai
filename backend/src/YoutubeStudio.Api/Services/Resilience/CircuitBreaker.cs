namespace YoutubeStudio.Api.Services.Resilience;

public enum CircuitState { Closed, Open, HalfOpen }

/// <summary>
/// Thrown when the circuit is open and a call is short-circuited.
/// </summary>
public sealed class CircuitOpenException() : Exception("The circuit is open; the call was short-circuited.");

/// <summary>
/// A minimal thread-safe circuit breaker (SYSTEM-ARCHITECTURE §8). After
/// <paramref name="failureThreshold"/> consecutive failures the circuit opens and
/// short-circuits calls for <paramref name="openDuration"/>. It then allows one trial
/// (half-open); success closes it, failure reopens it. The clock is injectable for tests.
/// </summary>
public sealed class CircuitBreaker(int failureThreshold = 3, TimeSpan? openDuration = null, Func<DateTime>? clock = null)
{
    private readonly int _failureThreshold = Math.Max(1, failureThreshold);
    private readonly TimeSpan _openDuration = openDuration ?? TimeSpan.FromSeconds(30);
    private readonly Func<DateTime> _clock = clock ?? (() => DateTime.UtcNow);
    private readonly object _gate = new();

    private int _consecutiveFailures;
    private DateTime _openedAtUtc;
    private CircuitState _state = CircuitState.Closed;

    public CircuitState State
    {
        get { lock (_gate) return Evaluate(); }
    }

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (Evaluate() == CircuitState.Open)
                throw new CircuitOpenException();
        }

        try
        {
            var result = await operation(cancellationToken);
            lock (_gate) { _consecutiveFailures = 0; _state = CircuitState.Closed; }
            return result;
        }
        catch
        {
            lock (_gate)
            {
                _consecutiveFailures++;
                if (_consecutiveFailures >= _failureThreshold)
                {
                    _state = CircuitState.Open;
                    _openedAtUtc = _clock();
                }
            }
            throw;
        }
    }

    // Must be called under _gate. Transitions Open -> HalfOpen once the cooldown elapses.
    private CircuitState Evaluate()
    {
        if (_state == CircuitState.Open && _clock() - _openedAtUtc >= _openDuration)
            _state = CircuitState.HalfOpen;
        return _state;
    }
}
