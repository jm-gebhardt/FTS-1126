using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Services.Filters;

/// <summary>
/// Provides an in memory filter
/// </summary>
/// <typeparam name="T">Message type</typeparam>
[UsedImplicitly]
public class UnTimedInMemoryFilter<T>(ISemaphoreProvider semaphoreProvider, ISignificanceChecker<T> significanceChecker) where T : class
{
    /// <summary>
    /// Holds the previously evaluated object for comparison with new incoming objects.
    /// </summary>
    private T? _previousValue;

    /// <inheritdoc cref="IMessageFilter{T}"/>>
    public async Task<bool> ShouldActAsync(T value)
    {
        return await semaphoreProvider.ExecuteLockedAsync(() =>
        {
            bool isDataSignificant = _previousValue == null || significanceChecker.HasSignificantChanges(value, _previousValue);
            if (_previousValue == null || isDataSignificant)
            {
                _previousValue = value;
                return true;
            }

            return false;
        });
    }
}