namespace Gebhardt.Acc.Common.Time;

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