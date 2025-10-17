using JetBrains.Annotations;

namespace Gebhardt.Acc.Statistics.Domain.Models;

/// <summary>
/// Represents an agv field violation on a grid.
/// </summary>
public class AgvFieldViolation : IPositionedEntity
{
    /// <summary>
    /// The id of the violation.
    /// </summary>
    public Guid ViolationId { get; init; }

    /// <summary>
    /// The position id of the agv grid position.
    /// </summary>
    public Guid PositionId { get; init; }

    /// <inheritdoc />
    public GridAgvPosition? Position { get; init; }

    /// <inheritdoc />
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// The amount of times that a violation occured on the position.
    /// </summary>
    [PublicAPI]
    public int ViolationCount { get; } = 1;

    /// <inheritdoc cref="AgvFieldViolation"/>>.
    public AgvFieldViolation(Guid positionId, DateTime timestamp)
    {
        ViolationId = Guid.NewGuid();
        PositionId = positionId;
        Timestamp = timestamp;
    }

    /// <inheritdoc cref="AgvFieldViolation"/>>.
    public AgvFieldViolation()
    {
        ViolationId = Guid.NewGuid();
    }
}