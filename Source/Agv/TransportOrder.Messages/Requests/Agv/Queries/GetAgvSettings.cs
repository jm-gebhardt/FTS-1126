using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;

/// <summary>
/// Represents the GetAgvSettings request which returns AgvSettings
/// </summary>
public record GetAgvSettings : CorrelatedBy<Guid>
{
    /// <inheritdoc/>
    public Guid CorrelationId { get; }

    /// <inheritdoc cref="GetAgvSettings"/>
    /// <param name="correlationId"></param>
    public GetAgvSettings(Guid correlationId)
    {
        CorrelationId = correlationId;
    }
}