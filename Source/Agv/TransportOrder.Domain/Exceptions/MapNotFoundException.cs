using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Domain.Models.Graphs;

namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>
/// Represents errors that occur when the map is not found in the e.g. repository
/// </summary>
public class MapNotFoundException : NotFoundException
{
    /// <inheritdoc cref="MapNotFoundException"/>
    public MapNotFoundException(Guid correlationId) : base(nameof(Map), nameof(Map.CorrelationId), correlationId)
    {
    }
}