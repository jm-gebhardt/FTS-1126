namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

/// <summary>
/// ParkerState to store hasParkers flag value.
/// </summary>
// ReSharper disable UnusedMember.Global
public static class ParkerState
{
    private static long _hasParkers;

    /// <summary>
    /// Returns hasParkers flag value.
    /// </summary>
    public static bool HasParkers
    {
        get => Interlocked.Read(ref _hasParkers) == 1;
        set => Interlocked.CompareExchange(ref _hasParkers, value ? 1 : 0, _hasParkers);
    }

    /// <summary>
    /// Updates hasParkers flag to newValue.
    /// </summary>
    /// <param name="newValue"></param>
    public static void UpdateHasParkers(bool newValue)
    {
        Interlocked.Exchange(ref _hasParkers, newValue ? 1 : 0);
    }
}