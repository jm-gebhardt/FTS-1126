using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;

/// <summary>
/// Represents the GetAgvData request which returns AgvData
/// </summary>
public record GetAgvData : CorrelatedBy<Guid>
{
    /// <inheritdoc/>
    public Guid CorrelationId { get; }

    /// <summary>
    /// <inheritdoc cref="GetAgvData"/>
    /// </summary>
    /// <param name="correlationId"></param>
    public GetAgvData(Guid correlationId)
    {
        CorrelationId = correlationId;
    }
}