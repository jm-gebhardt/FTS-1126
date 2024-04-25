using Gebhardt.Acc.Common.Domain.Exceptions;

namespace Gebhardt.Acc.Statistics.Domain.Exceptions;

/// <summary>
/// Represents the exception thrown when a grid agv position does not exist.
/// </summary>
public class GridAgvPositionNotFoundException : NotFoundException
{
    /// <inheritdoc cref="GridAgvPositionNotFoundException"/>
    /// <param name="paramName">The parameter name to identify the grid position that caused the exception.</param>
    public GridAgvPositionNotFoundException(string paramName) : base(paramName)
    {
    }
}