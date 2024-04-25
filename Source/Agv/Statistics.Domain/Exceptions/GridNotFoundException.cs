using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Statistics.Domain.Models;

namespace Gebhardt.Acc.Statistics.Domain.Exceptions;

/// <summary>
/// Represents the exception thrown when a grid does not exist.
/// </summary>
public class GridNotFoundException : NotFoundException
{
    /// <inheritdoc cref="GridNotFoundException"/>
    /// <param name="gridId">The id of the grid.</param>
    public GridNotFoundException(Guid gridId)
        : base(nameof(Grid), "GridId", gridId)
    {
    }
}