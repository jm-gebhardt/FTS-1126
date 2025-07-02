namespace Gebhardt.Acc.Statistics.Domain.Models;

/// <summary>
/// Defines an entity with a timestamp indicating when the data was created.
/// </summary>
public interface ITimestamped
{
    /// <summary>
    /// The time the data was created.
    /// </summary>
    DateTime Timestamp { get; }
}