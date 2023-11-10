namespace Gebhardt.StoreWare.Agv.Common.Time;

/// <summary>
/// Provides the current time
/// </summary>
public interface ITimeProvider
{
    /// <summary>
    /// DateTime Now
    /// </summary>
    /// <returns></returns>
    DateTime Now();
}