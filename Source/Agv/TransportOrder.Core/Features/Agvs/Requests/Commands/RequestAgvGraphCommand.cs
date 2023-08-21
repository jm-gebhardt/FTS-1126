using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Commands;

/// <summary>
/// Represents the EmitRequestGraph request which returns AgvData
/// </summary>
public record RequestAgvGraphCommand : CorrelatedBy<Guid>
{
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public Guid CorrelationId { get; }

    /// <summary>
    /// <inheritdoc cref="RequestAgvGraphCommand"/>
    /// </summary>
    /// <param name="correlationId"></param>
    public RequestAgvGraphCommand(Guid correlationId)
    {
        CorrelationId = correlationId;
    }
}