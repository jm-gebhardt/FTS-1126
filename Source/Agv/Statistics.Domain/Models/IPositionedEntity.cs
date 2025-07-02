namespace Gebhardt.Acc.Statistics.Domain.Models;

/// <summary>
/// Defines an entity with a spatial position and timestamp.
/// </summary>
public interface IPositionedEntity : ITimestamped
{
    /// <summary>
    /// The agv grid position object.
    /// </summary>
    GridAgvPosition? Position { get; }
}