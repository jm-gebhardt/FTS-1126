using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Filters;

/// <summary>
/// Stores the previous published object and compares it with the current object.
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class InMemoryStorageFilter<T> : IHubFilter<T> where T : IMessageCreatedDate
{
    private readonly object _previousDataLock = new();

    /// <summary>
    /// The previous published object.
    /// </summary>
    private T? _previousData;

    /// <summary>
    /// Returns true if the current data is different from the previous data.
    /// </summary>
    /// <param name="currentData">The new data to consider if it should be published.</param>
    /// <param name="previousData">The previous published data.</param>
    /// <returns>True if the dto should be published.</returns>
    protected abstract bool ShouldPublishBasedOn(T currentData, T previousData);

    /// <summary>
    /// The minimum time to wait for a new message before publishing the previous message, based on the <see cref="IMessageCreatedDate.MessageCreatedDate"/>.
    /// </summary>
    public virtual TimeSpan MinSilence => TimeSpan.Zero;

    /// <summary>
    /// The maximum time to wait for a new message before publishing the previous message, regardless of the ShouldPublishBasedOn method.
    /// </summary>
    public virtual TimeSpan MaxSilence => TimeSpan.FromSeconds(10);

    /// <inheritdoc />
    public bool ShouldPublish(T currentData)
    {
        lock (_previousDataLock)
        {
            if (_previousData == null ||
                // After the max silence
                currentData.MessageCreatedDate > _previousData.MessageCreatedDate + MaxSilence ||
                // after the min silence is reached. We are checking if the min silence is zero because a message might be shortly before the previous message, based on the message created date.
                ((MinSilence == TimeSpan.Zero || currentData.MessageCreatedDate > _previousData.MessageCreatedDate + MinSilence) && ShouldPublishBasedOn(currentData, _previousData)))
            {
                _previousData = currentData;
                return true;
            }

            return false;
        }
    }
}