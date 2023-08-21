using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Commands;

/// <summary>
/// Represents the LockAgv request
/// </summary>
public record LockAgvCommand : CorrelatedBy<Guid>
{
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public Guid CorrelationId { get; }

    /// <summary>
    /// <inheritdoc cref="LockAgvCommand"/>
    /// </summary>
    /// <param name="correlationId"></param>
    public LockAgvCommand(Guid correlationId)
    {
        CorrelationId = correlationId;
    }
}