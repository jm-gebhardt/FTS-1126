using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>
/// The Graph of the Agv is null.
/// </summary>
public class AgvGraphNotDefinedException : NotFoundException
{
    /// <see cref="AgvGraphNotDefinedException"/>>
    public AgvGraphNotDefinedException(Models.Agv.Agv agv)
        : base(nameof(AgvGraph), nameof(agv.CorrelationId), agv.CorrelationId)
    {
    }

    /// <summary>
    /// Agv Graph with Agv correlationId was not found
    /// </summary>
    /// <param name="correlationId">The correlation Id of the Agv</param>
    [UsedImplicitly]
    public AgvGraphNotDefinedException(Guid correlationId)
        : base(nameof(AgvGraph), nameof(correlationId), correlationId)
    {
    }
}