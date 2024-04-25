using Gebhardt.Acc.Common.Domain.Exceptions;

namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>
/// Agv was not found
/// </summary>
public class AgvNotFoundException : NotFoundException
{
    /// <summary>
    /// Agv was not found with AgvCorrelationId
    /// </summary>
    /// <param name="correlationId">Correlation Id of Agv</param>
    public AgvNotFoundException(Guid? correlationId)
        : base("Agv", nameof(correlationId), correlationId)
    {
    }
}