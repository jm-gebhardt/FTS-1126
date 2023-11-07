namespace Gebhardt.StoreWare.Agv.SmartLinkCommunication.Models;

/// <summary>
/// Tracks the state of an IO and executes a timer when the state has been stable for a certain amount of time
/// </summary>
public class IoStateTracker : IDisposable, IAsyncDisposable
{
    private bool _currentState;
    private TimeSpan? _timerDuration;

    /// <summary>
    /// The timer to execute when the state has been stable for a certain amount of time
    /// </summary>
    private Timer? _timer;

    /// <summary>
    /// The current state of the IO
    /// </summary>
    public bool CurrentState
    {
        get => _currentState;
        set
        {
            if (_currentState != value)
            {
                _currentState = value;
                ResetTimer();
            }
        }
    }

    /// <summary>
    /// Indicates whether the action has already been executed. If the timer is running it is still scheduled.
    /// </summary>
    public bool IsTimerRunning { get; private set; }

    private void ResetTimer()
    {
        if (_timerDuration != null && _timer != null)
        {
            _timer.Change((TimeSpan) _timerDuration, Timeout.InfiniteTimeSpan);
            IsTimerRunning = true;
        }
    }

    /// <summary>
    /// Sets up a timer that will execute the specified callback action if the <see cref="CurrentState"/> remains the same as inState" as long as specified in duration>.
    /// </summary>
    /// <param name="duration">How long the state must remain the same before the action is executed.</param>
    /// <param name="inState">The state he has to be in.</param>
    /// <param name="callback">The callback action that will be executed if the <see cref="CurrentState"/> remains the same as inState as long as specified in duration.</param>
    public IoStateTracker SetupTimer(TimeSpan duration, bool inState, Action callback)
    {
        _timerDuration = duration;
        _timer?.Dispose();
        _timer = new Timer(_ =>
        {
            IsTimerRunning = false;

            if (inState == CurrentState)
            {
                callback();
            }
        }, null, (TimeSpan) _timerDuration, Timeout.InfiniteTimeSpan);
        IsTimerRunning = true;

        return this;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _timer?.Dispose();
        _timer = null;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_timer != null)
        {
            await _timer.DisposeAsync();
        }
    }
}