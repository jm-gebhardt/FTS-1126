namespace Gebhardt.Acc.Common.Services;

/// <summary>
///  IPollable interface used by <see cref="PollingBackgroundService{T}"/>
/// </summary>
public interface IPollable
{
    /// <summary>
    /// Polling interval time in milliseconds
    /// Default: 1000
    /// </summary>
    public int PollingInterval => 1000;

    /// <summary>
    /// This method is called each <see cref="PollingInterval"/>
    /// </summary>
    public Task Poll(CancellationToken cancellationToken = default);
}