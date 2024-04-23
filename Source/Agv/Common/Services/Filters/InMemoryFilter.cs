using Gebhardt.Acc.Common.Messages;
using Gebhardt.Acc.Common.Services.Filters.Configurations;

namespace Gebhardt.Acc.Common.Services.Filters;

/// <summary>
/// Manages an in-memory comparison of sequential objects to determine if the latest object meets specified criteria for action, based on its content and creation time relative to the previously stored object.
/// </summary>
/// <typeparam name="T">The type of the object being monitored, which must implement IMessageCreatedDate to provide a creation timestamp.</typeparam>
public abstract class InMemoryFilter<T> : IMessageFilter<T> where T : class, IMessageCreatedDate
{
    /// <summary>
    /// Holds the previously evaluated object for comparison with new incoming objects.
    /// </summary>
    private T? _previousData;

    private readonly FilterConfiguration _configuration;

    private readonly ISemaphoreProvider _semaphoreProvider;

    /// <summary>
    /// Evaluates whether the newly provided object significantly differs from the previously stored object, warranting further processing.
    /// </summary>
    /// <param name="currentData">The new object under consideration.</param>
    /// <param name="previousData">The last object that was stored and considered for processing.</param>
    /// <returns>True if the new object meets the defined criteria for being significantly different or important compared to the previous object, thus requiring further action.</returns>
    protected abstract bool ShouldActOnData(T currentData, T previousData);

    /// <summary>
    /// Specifies the minimum elapsed time that should be considered before a new object is eligible for processing, regardless of whether it meets the usual criteria for significance or difference.
    /// </summary>
    public virtual TimeSpan MinInterval => _configuration.MinInterval;

    /// <summary>
    /// Defines the maximum time that can elapse before a new object must be processed, regardless of whether it meets the usual criteria for significance or difference.
    /// </summary>
    public virtual TimeSpan MaxInterval => _configuration.MaxInterval;

    /// <inheritdoc cref="InMemoryFilter{T}"/>
    /// <param name="semaphoreProvider"></param>
    /// <param name="currentData"></param>
    /// <param name="configuration">You can specify a configuration for the Min/MaxInterval. If not set a default instance of <see cref="FilterConfiguration"/> will be used.</param>
    protected InMemoryFilter(ISemaphoreProvider semaphoreProvider, T? currentData = null, FilterConfiguration? configuration = null)
    {
        _semaphoreProvider = semaphoreProvider;
        _previousData = currentData;
        _configuration = configuration ?? new FilterConfiguration();
    }

    /// <summary>
    /// Determines if the newly provided object meets the established criteria for processing, taking into account both its content differences and the timing of its arrival relative to the previously processed object.
    /// </summary>
    /// <param name="currentData">The object currently being evaluated for processing.</param>
    /// <returns>True if the object is deemed significant enough to warrant further action, based on its differences from the previous object and the timing of its presentation.</returns>
    public async Task<bool> ShouldActAsync(T currentData)
    {
        return await _semaphoreProvider.ExecuteLockedAsync(() =>
        {
            var timeDifference = currentData.MessageCreatedDate - (_previousData?.MessageCreatedDate ?? DateTime.MinValue);

            bool isMaxIntervalExceeded = timeDifference > MaxInterval;

            bool isMinIntervalExceeded = timeDifference > MinInterval || MinInterval == TimeSpan.Zero;

            bool isDataSignificant = _previousData == null || ShouldActOnData(currentData, _previousData);

            if (_previousData == null || isMaxIntervalExceeded || (isMinIntervalExceeded && isDataSignificant))
            {
                _previousData = currentData;
                return true;
            }

            return false;
        });
    }
}