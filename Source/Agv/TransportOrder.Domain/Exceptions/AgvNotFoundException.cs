using Gebhardt.StoreWare.Agv.Common.Exceptions;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;

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