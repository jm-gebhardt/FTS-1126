namespace Gebhardt.Acc.TransportOrder.TrafficManagement.Tick;

/// <summary>
/// Tracks when the last <see cref="TrafficManagementTick"/> was actually processed, so the consumer can
/// idempotently discard ticks that arrive too soon after the previous one (e.g. because of message backlog,
/// redelivery, or overlapping timers after a restart), preventing the cyclic task from running more often
/// than intended.
/// </summary>
internal sealed class TrafficManagementTickState(TimeProvider timeProvider)
{
    private long _lastProcessedTimestamp;

    /// <summary>
    /// Checks whether enough time has elapsed since the last processed tick and, if so, atomically marks
    /// "now" as the new last-processed timestamp.
    /// </summary>
    /// <param name="minimumInterval">The minimum time that must have elapsed since the last processed tick.</param>
    /// <returns><see langword="true"/> if the tick should be processed; <see langword="false"/> if it should be discarded as a duplicate.</returns>
    public bool TryMarkProcessed(TimeSpan minimumInterval)
    {
        long now = timeProvider.GetTimestamp();
        long previous = Interlocked.Read(ref _lastProcessedTimestamp);

        if (previous != 0 && timeProvider.GetElapsedTime(previous, now) < minimumInterval)
        {
            return false;
        }

        return Interlocked.CompareExchange(ref _lastProcessedTimestamp, now, previous) == previous;
    }
}
